namespace EventVenueBookingManagement_System.Models;

public sealed record User(int Id, string FullName, string Email, string Role);

public sealed class Venue
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Location { get; set; } = "";
    public int Capacity { get; set; }
    public decimal PricePerDay { get; set; }
    public string Description { get; set; } = "";
    public bool IsAvailable { get; set; }
}

public sealed class Booking
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public string VenueName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public int DurationDays { get; set; }
    public string EventDetails { get; set; } = "";
    public decimal QuotedPrice { get; set; }
    public string Status { get; set; } = "";
}

public sealed class PaymentSummary
{
    public int BookingId { get; set; }
    public string CustomerName { get; set; } = "";
    public string VenueName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public decimal TotalDue { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = "";
}
