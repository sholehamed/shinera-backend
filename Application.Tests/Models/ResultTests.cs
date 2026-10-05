using Application.SharedKernel.Models;

namespace Application.Tests.Models;

public sealed class ResultTests
{
    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_ContainsStableError()
    {
        var error = Error.Conflict(
            "appointment.conflict",
            "The requested slot is no longer available.");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public void GenericSuccess_ExposesValue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_RejectsValueAccess()
    {
        var result = Result.Failure<int>(
            Error.NotFound("common.not_found", "Not found."));

        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }
}
