using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Domain.Entities;
using Modules.System.Identity.Web.Authentication;
using Modules.System.Identity.Web.Util;
using OpenIddict.Abstractions;
using System.Security.Claims;
using Web.SharedKernel.Attributes;

namespace Modules.System.Identity.Web.Endpoints;

public class Auth : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapPost(
                Login,
                "session/login",
                configure: x => x
                    .AllowAnonymous()
                    .RequireRateLimiting("shinera-auth-login"))
            .MapPost(
                LogoutSession,
                "session/logout",
                configure: x => x.AllowAnonymous())
            .MapGet(
                NewCaptcha,
                "captcha/new",
                configure: x => x.AllowAnonymous())
            .MapGet(
                GetCaptchaImage,
                "captcha/image",
                configure: x => x.AllowAnonymous())
            .MapGet(
                GetCurrentUser,
                "me",
                configure: x => x.RequireAuthorization());
    }

    public async Task<IResult> Login(
        InteractiveLoginRequest request,
        HttpContext httpContext,
        CaptchaService captchaService,
        IIdentityDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        global::System.TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!captchaService.Validate(request.CaptchaToken, request.CaptchaCode))
        {
            return Results.BadRequest(new
            {
                error = "authentication.captcha_invalid",
                message = "Captcha is invalid or expired."
            });
        }

        var identifier = request.Identifier?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(identifier) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return InvalidCredentials();
        }

        var candidates = await dbContext.Users
            .Where(user =>
                user.IsActive &&
                (user.NormalizedEmail == identifier ||
                 user.NormalizedUserName == identifier))
            .Take(2)
            .ToListAsync(cancellationToken);

        // User authentication is global. An ambiguous legacy username must not
        // be resolved by tenant context; the caller can use the unique email.
        if (candidates.Count != 1)
        {
            return InvalidCredentials();
        }

        var user = candidates[0];
        var now = timeProvider.GetUtcNow();

        if (user.IsLockedOut &&
            user.LockoutEndUtc.HasValue &&
            user.LockoutEndUtc.Value > now.UtcDateTime)
        {
            return InvalidCredentials();
        }

        var verification = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            user.AccessFailedCount++;

            if (user.AccessFailedCount >= 5)
            {
                user.IsLockedOut = true;
                user.LockoutEndUtc = now.AddMinutes(15).UtcDateTime;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return InvalidCredentials();
        }

        if (user.AccessFailedCount > 0 || user.IsLockedOut)
        {
            user.AccessFailedCount = 0;
            user.IsLockedOut = false;
            user.LockoutEndUtc = null;

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var identity = new ClaimsIdentity(
            InteractiveAuthenticationDefaults.Scheme,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        identity.AddClaim(new Claim(
            OpenIddictConstants.Claims.Subject,
            user.Id.ToString()));

        identity.AddClaim(new Claim(
            OpenIddictConstants.Claims.Name,
            user.UserName));

        identity.AddClaim(new Claim(
            OpenIddictConstants.Claims.Email,
            user.Email));

        await httpContext.SignInAsync(
            InteractiveAuthenticationDefaults.Scheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true,
                ExpiresUtc = now.AddHours(8)
            });

        return Results.Ok(new
        {
            success = true,
            returnUrl = NormalizeReturnUrl(request.ReturnUrl)
        });
    }

    public async Task<IResult> LogoutSession(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(
            InteractiveAuthenticationDefaults.Scheme);

        return Results.NoContent();
    }

    public async Task<IResult> GetCurrentUser(
        HttpContext httpContext,
        IIdentityDbContext dbContext,
        ITenantContext tenantContext,
        IPermissionAuthorizationService permissionAuthorizationService,
        CancellationToken cancellationToken)
    {
        var subject = httpContext.User.FindFirstValue(
            OpenIddictConstants.Claims.Subject);

        if (!Guid.TryParse(subject, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == userId && x.IsActive,
                cancellationToken);

        if (user is null)
        {
            return Results.Unauthorized();
        }

        var memberships = await dbContext.TenantMemberships
            .IgnoreQueryFilters(["tenant"])
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.IsActive &&
                x.Tenant.IsActive)
            .OrderBy(x => x.Tenant.Name)
            .Select(x => new
            {
                x.TenantId,
                x.Tenant.Name,
                x.Tenant.Slug
            })
            .ToListAsync(cancellationToken);

        var permissions =
            await permissionAuthorizationService.GetEffectivePermissionsAsync(
                userId,
                cancellationToken);

        return Results.Ok(new
        {
            user = new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.FirstName,
                user.LastName,
                user.ImageId
            },
            memberships,
            activeTenantId = tenantContext.ActiveTenantId,
            permissions
        });
    }

    [Resouce("", "newcaptcha")]
    public IResult NewCaptcha(CaptchaService captchaService)
    {
        var challenge = captchaService.CreateChallenge();
        var imageUrl =
            $"/System/Auth/captcha/image?token={Uri.EscapeDataString(challenge.Token)}";

        return Results.Ok(new
        {
            token = challenge.Token,
            imageUrl
        });
    }

    [Resouce("", "newcaptcha")]
    public IResult GetCaptchaImage(
        string token,
        CaptchaService captchaService)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.BadRequest();
        }

        try
        {
            var bytes = captchaService.GenerateImage(token);
            return Results.File(bytes, "image/png");
        }
        catch
        {
            return Results.BadRequest();
        }
    }

    private static IResult InvalidCredentials()
    {
        return Results.Json(
            new
            {
                error = "authentication.invalid_credentials",
                message = "Invalid credentials."
            },
            statusCode: StatusCodes.Status401Unauthorized);
    }

    private static string? NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return null;

        if (!returnUrl.StartsWith('/') ||
            returnUrl.StartsWith("//", StringComparison.Ordinal))
        {
            return null;
        }

        return returnUrl;
    }
}

public sealed record InteractiveLoginRequest(
    string Identifier,
    string Password,
    string CaptchaToken,
    string CaptchaCode,
    string? ReturnUrl);
