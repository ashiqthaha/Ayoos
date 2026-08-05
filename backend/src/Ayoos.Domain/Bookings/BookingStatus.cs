namespace Ayoos.Domain.Bookings;

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    CancelledByPatient = 2,
    CancelledByProvider = 3,
    Completed = 4,
    NoShow = 5
}
