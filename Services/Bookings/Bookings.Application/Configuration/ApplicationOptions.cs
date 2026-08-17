namespace Bookings.Application.Configuration;

public class ApplicationOptions
{
    public BookingSettings Booking { get; set; } = new();
}
