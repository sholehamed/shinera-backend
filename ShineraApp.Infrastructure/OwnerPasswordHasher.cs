using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ShineraApp.Application.Features.Registration;
namespace ShineraApp.Infrastructure;
// Password hashing only: this does not add ASP.NET Identity users/stores or replace OpenIddict.
public sealed class OwnerPasswordHasher : IOwnerPasswordHasher
{
    private readonly PasswordHasher<object> hasher = new(Options.Create(new PasswordHasherOptions { IterationCount = 210_000 }));
    private readonly object subject = new();
    public string Hash(string password) => hasher.HashPassword(subject, password);
    public bool Verify(string hash, string password) => hasher.VerifyHashedPassword(subject, hash, password) != PasswordVerificationResult.Failed;
}
