using EventVenueBookingManagement_System.Models;
using MySqlConnector;

namespace EventVenueBookingManagement_System.Data;

public sealed record AuthAccount(User User, byte[] Salt, byte[] Hash);

public sealed class UserRepository
{
    public AuthAccount? FindByEmail(string email)
    {
        using var connection = DbConnectionFactory.Create();
        connection.Open();
        using var command = new MySqlCommand(
            "SELECT id, full_name, email, password_salt, password_hash, role FROM users WHERE email = @email LIMIT 1",
            connection);
        command.Parameters.AddWithValue("@email", email);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return null;

        var user = new User(reader.GetInt32("id"), reader.GetString("full_name"), reader.GetString("email"), reader.GetString("role"));
        return new AuthAccount(user,
            Convert.FromHexString(reader.GetString("password_salt")),
            Convert.FromHexString(reader.GetString("password_hash")));
    }

    public void Register(string fullName, string email, string salt, string hash)
    {
        using var connection = DbConnectionFactory.Create();
        connection.Open();
        using var command = new MySqlCommand(
            "INSERT INTO users (full_name, email, password_salt, password_hash) VALUES (@name, @email, @salt, @hash)",
            connection);
        command.Parameters.AddWithValue("@name", fullName);
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@salt", salt);
        command.Parameters.AddWithValue("@hash", hash);
        command.ExecuteNonQuery();
    }
}

public sealed class VenueRepository
{
    public IReadOnlyList<Venue> GetAll(string query, bool includeUnavailable = true)
    {
        using var connection = DbConnectionFactory.Create();
        connection.Open();
        using var command = new MySqlCommand(
            "SELECT v.id, v.name, v.location, v.capacity, v.price_per_day, v.description, v.is_available FROM venues v " +
            "WHERE (@includeUnavailable = 1 OR (v.is_available = TRUE AND NOT EXISTS (" +
            "SELECT 1 FROM bookings b WHERE b.venue_id = v.id AND b.status = 'Approved' " +
            "AND (SELECT COALESCE(SUM(p.amount), 0) FROM payments p WHERE p.booking_id = b.id) >= b.quoted_price))) " +
            "AND (@query = '' OR v.name LIKE @pattern OR v.location LIKE @pattern OR v.description LIKE @pattern) " +
            "ORDER BY v.name", connection);
        command.Parameters.AddWithValue("@includeUnavailable", includeUnavailable);
        command.Parameters.AddWithValue("@query", query);
        command.Parameters.AddWithValue("@pattern", $"%{query}%");
        using var reader = command.ExecuteReader();
        var venues = new List<Venue>();
        while (reader.Read())
        {
            venues.Add(new Venue
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
                Location = reader.GetString("location"),
                Capacity = reader.GetInt32("capacity"),
                PricePerDay = reader.GetDecimal("price_per_day"),
                Description = reader.GetString("description"),
                IsAvailable = reader.GetBoolean("is_available")
            });
        }
        return venues;
    }

    public void Save(Venue venue)
    {
        using var connection = DbConnectionFactory.Create();
        connection.Open();
        using var command = venue.Id == 0
            ? new MySqlCommand("INSERT INTO venues (name, location, capacity, price_per_day, description, is_available) " +
                               "VALUES (@name, @location, @capacity, @price, @description, @available)", connection)
            : new MySqlCommand("UPDATE venues SET name = @name, location = @location, capacity = @capacity, " +
                               "price_per_day = @price, description = @description, is_available = @available WHERE id = @id", connection);
        AddVenueParameters(command, venue);
        if (venue.Id != 0)
            command.Parameters.AddWithValue("@id", venue.Id);
        command.ExecuteNonQuery();
    }

    public bool Delete(int venueId)
    {
        using var connection = DbConnectionFactory.Create();
        connection.Open();
        using var command = new MySqlCommand(
            "DELETE FROM venues WHERE id = @id AND NOT EXISTS (SELECT 1 FROM bookings WHERE venue_id = @id)", connection);
        command.Parameters.AddWithValue("@id", venueId);
        return command.ExecuteNonQuery() == 1;
    }

    private static void AddVenueParameters(MySqlCommand command, Venue venue)
    {
        command.Parameters.AddWithValue("@name", venue.Name);
        command.Parameters.AddWithValue("@location", venue.Location);
        command.Parameters.AddWithValue("@capacity", venue.Capacity);
        command.Parameters.AddWithValue("@price", venue.PricePerDay);
        command.Parameters.AddWithValue("@description", venue.Description);
        command.Parameters.AddWithValue("@available", venue.IsAvailable);
    }
}

public sealed class BookingRepository
{
    public IReadOnlyList<Booking> GetFor(User user)
    {
        using var connection = DbConnectionFactory.Create();
        connection.Open();
        var administrator = user.Role.Equals("Administrator", StringComparison.OrdinalIgnoreCase);
        using var command = new MySqlCommand(
            "SELECT b.id, u.full_name AS customer_name, v.name AS venue_name, b.event_date, b.event_start_time, " +
            "b.duration_days, b.event_details, b.quoted_price, b.status FROM bookings b " +
            "JOIN users u ON u.id = b.customer_id JOIN venues v ON v.id = b.venue_id " +
            "WHERE (@administrator = 1 OR b.customer_id = @customerId) ORDER BY b.event_date DESC, b.id DESC", connection);
        command.Parameters.AddWithValue("@administrator", administrator);
        command.Parameters.AddWithValue("@customerId", user.Id);
        using var reader = command.ExecuteReader();
        var bookings = new List<Booking>();
        while (reader.Read())
        {
            bookings.Add(new Booking
            {
                Id = reader.GetInt32("id"),
                CustomerName = reader.GetString("customer_name"),
                VenueName = reader.GetString("venue_name"),
                EventDate = reader.GetDateTime("event_date"),
                StartTime = reader.GetTimeSpan("event_start_time"),
                DurationDays = reader.GetInt32("duration_days"),
                EventDetails = reader.GetString("event_details"),
                QuotedPrice = reader.GetDecimal("quoted_price"),
                Status = reader.GetString("status")
            });
        }
        return bookings;
    }

    public void Create(int customerId, int venueId, DateTime eventDate, TimeSpan startTime, int durationDays, string eventDetails)
    {
        using var connection = DbConnectionFactory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        using var venueCommand = new MySqlCommand(
            "SELECT price_per_day FROM venues WHERE id = @venueId AND is_available = TRUE FOR UPDATE", connection, transaction);
        venueCommand.Parameters.AddWithValue("@venueId", venueId);
        var dailyPrice = venueCommand.ExecuteScalar();
        if (dailyPrice is null)
            throw new InvalidOperationException("This venue is unavailable or no longer exists.");

        using var paidVenueCommand = new MySqlCommand(
            "SELECT COUNT(*) FROM bookings b WHERE b.venue_id = @venueId AND b.status = 'Approved' " +
            "AND COALESCE((SELECT SUM(p.amount) FROM payments p WHERE p.booking_id = b.id), 0) >= b.quoted_price", connection, transaction);
        paidVenueCommand.Parameters.AddWithValue("@venueId", venueId);
        if (Convert.ToInt32(paidVenueCommand.ExecuteScalar()) > 0)
            throw new InvalidOperationException("This venue has already been fully paid for and is no longer available.");

        using var overlapCommand = new MySqlCommand(
            "SELECT COUNT(*) FROM bookings b WHERE b.venue_id = @venueId AND b.status IN ('Pending', 'Approved') " +
            "AND @eventDate < DATE_ADD(b.event_date, INTERVAL b.duration_days DAY) " +
            "AND DATE_ADD(@eventDate, INTERVAL @durationDays DAY) > b.event_date", connection, transaction);
        overlapCommand.Parameters.AddWithValue("@venueId", venueId);
        overlapCommand.Parameters.AddWithValue("@eventDate", eventDate);
        overlapCommand.Parameters.AddWithValue("@durationDays", durationDays);
        if (Convert.ToInt32(overlapCommand.ExecuteScalar()) > 0)
            throw new InvalidOperationException("That venue is already booked for one or more of the selected dates.");

        var totalPrice = Convert.ToDecimal(dailyPrice) * durationDays;
        using var command = new MySqlCommand(
            "INSERT INTO bookings (customer_id, venue_id, event_date, event_start_time, duration_days, event_details, quoted_price) " +
            "VALUES (@customerId, @venueId, @eventDate, @startTime, @durationDays, @details, @price)", connection, transaction);
        command.Parameters.AddWithValue("@customerId", customerId);
        command.Parameters.AddWithValue("@venueId", venueId);
        command.Parameters.AddWithValue("@eventDate", eventDate);
        command.Parameters.AddWithValue("@startTime", startTime);
        command.Parameters.AddWithValue("@durationDays", durationDays);
        command.Parameters.AddWithValue("@details", eventDetails);
        command.Parameters.AddWithValue("@price", totalPrice);
        try
        {
            command.ExecuteNonQuery();
            transaction.Commit();
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            throw new InvalidOperationException("That venue already has a pending or approved booking on this date.", exception);
        }
    }

    public void SetStatus(int bookingId, string status)
    {
        if (status is not ("Approved" or "Rejected"))
            throw new ArgumentException("Unsupported booking status.");

        using var connection = DbConnectionFactory.Create();
        connection.Open();
        using var command = new MySqlCommand(
            "UPDATE bookings SET status = @status WHERE id = @id AND status = 'Pending'", connection);
        command.Parameters.AddWithValue("@status", status);
        command.Parameters.AddWithValue("@id", bookingId);
        if (command.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("This booking is no longer pending.");
    }
}

public sealed class PaymentRepository
{
    public IReadOnlyList<PaymentSummary> GetFor(User user)
    {
        using var connection = DbConnectionFactory.Create();
        connection.Open();
        var administrator = user.Role.Equals("Administrator", StringComparison.OrdinalIgnoreCase);
        using var command = new MySqlCommand(
            "SELECT b.id AS booking_id, u.full_name AS customer_name, v.name AS venue_name, b.event_date, " +
            "b.quoted_price AS total_due, COALESCE(SUM(p.amount), 0) AS amount_paid " +
            "FROM bookings b JOIN users u ON u.id = b.customer_id JOIN venues v ON v.id = b.venue_id " +
            "LEFT JOIN payments p ON p.booking_id = b.id " +
            "WHERE b.status = 'Approved' AND (@administrator = 1 OR b.customer_id = @customerId) " +
            "GROUP BY b.id, u.full_name, v.name, b.event_date, b.quoted_price ORDER BY b.event_date DESC", connection);
        command.Parameters.AddWithValue("@administrator", administrator);
        command.Parameters.AddWithValue("@customerId", user.Id);
        using var reader = command.ExecuteReader();
        var summaries = new List<PaymentSummary>();
        while (reader.Read())
        {
            var due = reader.GetDecimal("total_due");
            var paid = reader.GetDecimal("amount_paid");
            summaries.Add(new PaymentSummary
            {
                BookingId = reader.GetInt32("booking_id"),
                CustomerName = reader.GetString("customer_name"),
                VenueName = reader.GetString("venue_name"),
                EventDate = reader.GetDateTime("event_date"),
                TotalDue = due,
                AmountPaid = paid,
                Balance = Math.Max(0, due - paid),
                Status = paid >= due ? "Paid" : paid > 0 ? "Partially paid" : "Unpaid"
            });
        }
        return summaries;
    }

    public void Record(int bookingId, decimal amount, string method)
    {
        using var connection = DbConnectionFactory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        using var bookingCommand = new MySqlCommand(
            "SELECT quoted_price FROM bookings WHERE id = @id AND status = 'Approved' FOR UPDATE", connection, transaction);
        bookingCommand.Parameters.AddWithValue("@id", bookingId);
        var dueValue = bookingCommand.ExecuteScalar();
        if (dueValue is null)
            throw new InvalidOperationException("Only approved bookings can receive payments.");

        var due = Convert.ToDecimal(dueValue);
        using var paidCommand = new MySqlCommand(
            "SELECT COALESCE(SUM(amount), 0) FROM payments WHERE booking_id = @id", connection, transaction);
        paidCommand.Parameters.AddWithValue("@id", bookingId);
        var paid = Convert.ToDecimal(paidCommand.ExecuteScalar());
        if (amount > due - paid)
            throw new InvalidOperationException($"Payment exceeds the remaining balance of {due - paid:0.00}.");

        using var insert = new MySqlCommand(
            "INSERT INTO payments (booking_id, amount, payment_method) VALUES (@id, @amount, @method)", connection, transaction);
        insert.Parameters.AddWithValue("@id", bookingId);
        insert.Parameters.AddWithValue("@amount", amount);
        insert.Parameters.AddWithValue("@method", method);
        insert.ExecuteNonQuery();

        if (paid + amount >= due)
        {
            using var venueCommand = new MySqlCommand(
                "UPDATE venues SET is_available = FALSE WHERE id = (SELECT venue_id FROM bookings WHERE id = @bookingId)",
                connection, transaction);
            venueCommand.Parameters.AddWithValue("@bookingId", bookingId);
            venueCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }
}
