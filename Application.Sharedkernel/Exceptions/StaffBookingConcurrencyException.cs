namespace Application.SharedKernel.Exceptions;

public sealed class StaffBookingConcurrencyException(
    string message,
    Exception? innerException = null)
    : Exception(message, innerException)
{
    public const string ErrorCode =
        "booking.concurrent_change";

    public string Code => ErrorCode;
}
