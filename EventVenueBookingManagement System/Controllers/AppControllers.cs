using System.Security.Cryptography;
using EventVenueBookingManagement_System.Data;
using EventVenueBookingManagement_System.Models;

namespace EventVenueBookingManagement_System.Controllers;

public sealed class AuthController
{
    private readonly UserRepository _users = new();
    private const int Iterations = 120_000;

    public User? Login(string email, string password)
    {
        var account = _users.FindByEmail(email.Trim());
        if (account is null)
            return null;

        var candidate = Rfc2898DeriveBytes.Pbkdf2(password, account.Salt, Iterations, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(candidate, account.Hash)
            ? account.User
            : null;
    }

    public void Register(string fullName, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Name and email are required.");
        if (password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters long.");

        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        _users.Register(fullName.Trim(), email.Trim(), Convert.ToHexString(salt), Convert.ToHexString(hash));
    }
}

public sealed class VenueController
{
    private readonly VenueRepository _venues = new();

    public IReadOnlyList<Venue> Search(string query = "", bool includeUnavailable = true) => _venues.GetAll(query.Trim(), includeUnavailable);

    public void Save(Venue venue)
    {
        if (string.IsNullOrWhiteSpace(venue.Name) || string.IsNullOrWhiteSpace(venue.Location))
            throw new ArgumentException("Venue name and location are required.");
        if (venue.Capacity < 1 || venue.PricePerDay < 0)
            throw new ArgumentException("Capacity must be positive and price cannot be negative.");
        _venues.Save(venue);
    }

    public bool Delete(int venueId) => _venues.Delete(venueId);
}

public sealed class BookingController
{
    private readonly BookingRepository _bookings = new();

    public IReadOnlyList<Booking> GetBookings(User user) => _bookings.GetFor(user);

    public void Create(User customer, int venueId, DateTime eventDate, TimeSpan startTime, int durationDays, string eventDetails)
    {
        if (eventDate.Date < DateTime.Today)
            throw new ArgumentException("The event date cannot be in the past.");
        if (durationDays is < 1 or > 30)
            throw new ArgumentException("Booking duration must be between 1 and 30 days.");
        _bookings.Create(customer.Id, venueId, eventDate.Date, startTime, durationDays, eventDetails.Trim());
    }

    public void SetStatus(int bookingId, string status) => _bookings.SetStatus(bookingId, status);
}

public sealed class PaymentController
{
    private readonly PaymentRepository _payments = new();

    public IReadOnlyList<PaymentSummary> GetPayments(User user) => _payments.GetFor(user);

    public void Record(int bookingId, decimal amount, string method)
    {
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(method))
            throw new ArgumentException("Select a payment method.");
        _payments.Record(bookingId, amount, method);
    }
}
