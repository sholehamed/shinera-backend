namespace Application.SharedKernel.Exceptions;

public sealed class StaffBookingConcurrencyException : Exception
{
    public const string DefaultErrorCode =
        "booking.concurrent_change";

    public StaffBookingConcurrencyException(
        string message,
        Exception? innerException = null)
        : this(
            DefaultErrorCode,
            message,
            innerException)
    {
    }

    public StaffBookingConcurrencyException(
        string code,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
