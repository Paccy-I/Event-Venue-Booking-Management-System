using MySqlConnector;

namespace EventVenueBookingManagement_System.Data;

public static class DbConnectionFactory
{
    private const string DefaultConnectionString = "Server=localhost;Port=3306;Database=event_venue_booking;User ID=root;Password=;";

    public static MySqlConnection Create() => new(
        Environment.GetEnvironmentVariable("EVENT_VENUE_DB") ?? DefaultConnectionString);
}
