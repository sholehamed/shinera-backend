namespace Application.SharedKernel.Exceptions;

public sealed class TenantAccessException : Exception
{
    public TenantAccessException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
