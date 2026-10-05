using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Domain.Entities;
using Modules.System.Identity.Web.Authentication;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;

namespace Modules.System.Identity.Web.Endpoints;

public static class OpenIddictProtocolEndpoints
{
    public static WebApplication MapOpenIddictProtocolEndpoints(
        this WebApplication app,
        IConfiguration configuration)
    {
        app.MapMethods(
                "/connect/authorize",
                ["GET", "POST"],
                Authorize)
            .AllowAnonymous();

        app.MapPost(
                "/connect/token",
                Exchange)
            .AllowAnonymous()
            .DisableAntiforgery();

        app.MapMethods(
                "/connect/logout",
                ["GET", "POST"],
                Logout)
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> Authorize(
        HttpContext httpContext,
        IIdentityDbContext dbContext,
        IConfiguration configuration,
        global::System.TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var request = httpContext.GetOpenIddictServerRequest();
        if (request is null)
        {
            return Results.BadRequest(new
            {
                error = OpenIddictConstants.Errors.InvalidRequest,
                error_description = "OpenID Connect authorization request is missing."
            });
        }

        var interactive = await httpContext.AuthenticateAsync(
            InteractiveAuthenticationDefaults.Scheme);

        if (!interactive.Succeeded || interactive.Principal is null)
        {
            return RedirectToLogin(httpContext, configuration);
        }

        var subject = interactive.Principal.FindFirstValue(
            OpenIddictConstants.Claims.Subject);

        if (!Guid.TryParse(subject, out var userId))
        {
            await httpContext.SignOutAsync(InteractiveAuthenticationDefaults.Scheme);
            return RedirectToLogin(httpContext, configuration);
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == userId && x.IsActive,
                cancellationToken);

        if (user is null || IsLockedOut(user, timeProvider.GetUtcNow()))
        {
            await httpContext.SignOutAsync(InteractiveAuthenticationDefaults.Scheme);
            return RedirectToLogin(httpContext, configuration);
        }

        var allowedScopes = request.GetScopes()
            .Where(scope =>
                scope is OpenIddictConstants.Scopes.OpenId
                    or OpenIddictConstants.Scopes.Profile
                    or OpenIddictConstants.Scopes.Email
                    or OpenIddictConstants.Scopes.OfflineAccess
                    or "shinera_api")
            .ToArray();

        var principal = CreateProtocolPrincipal(user, allowedScopes);

        return Results.SignIn(
            principal,
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> Exchange(
        HttpContext httpContext,
        IIdentityDbContext dbContext,
        global::System.TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var request = httpContext.GetOpenIddictServerRequest();
        if (request is null)
        {
            return Results.BadRequest(new
            {
                error = OpenIddictConstants.Errors.InvalidRequest,
                error_description = "OpenID Connect token request is missing."
            });
        }

        if (!request.IsAuthorizationCodeGrantType() &&
            !request.IsRefreshTokenGrantType())
        {
            return Results.BadRequest(new
            {
                error = OpenIddictConstants.Errors.UnsupportedGrantType,
                error_description = "Only authorization_code and refresh_token grants are supported."
            });
        }

        var result = await httpContext.AuthenticateAsync(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        if (!result.Succeeded || result.Principal is null)
        {
            return InvalidGrant("The authorization code or refresh token is invalid.");
        }

        var subject = result.Principal.GetClaim(OpenIddictConstants.Claims.Subject);

        if (!Guid.TryParse(subject, out var userId))
        {
            return InvalidGrant("The token subject is invalid.");
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null ||
            !user.IsActive ||
            IsLockedOut(user, timeProvider.GetUtcNow()))
        {
            return InvalidGrant("The user account is no longer available.");
        }

        // Reuse the principal restored by OpenIddict so protocol-private claims
        // (authorization id, presenters, scopes, token metadata, etc.) are preserved.
        return Results.SignIn(
            result.Principal,
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> Logout(HttpContext httpContext)
    {
        var request = httpContext.GetOpenIddictServerRequest();

        await httpContext.SignOutAsync(
            InteractiveAuthenticationDefaults.Scheme);

        return Results.SignOut(
            authenticationSchemes:
            [
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
            ],
            properties: new AuthenticationProperties
            {
                RedirectUri = request?.PostLogoutRedirectUri ?? "/"
            });
    }

    private static ClaimsPrincipal CreateProtocolPrincipal(
        User user,
        IEnumerable<string> scopes)
    {
        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        identity.AddClaim(new Claim(
            OpenIddictConstants.Claims.Subject,
            user.Id.ToString()));

        identity.AddClaim(new Claim(
            OpenIddictConstants.Claims.Name,
            user.UserName));

        identity.AddClaim(new Claim(
            OpenIddictConstants.Claims.Username,
            user.UserName));

        identity.AddClaim(new Claim(
            OpenIddictConstants.Claims.Email,
            user.Email));

        identity.AddClaim(new Claim(
            OpenIddictConstants.Claims.GivenName,
            $"{user.FirstName} {user.LastName}".Trim()));

        var principal = new ClaimsPrincipal(identity);

        principal.SetScopes(scopes);
        principal.SetResources("shinera_api");

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        return principal;
    }

    private static IEnumerable<string> GetDestinations(
        Claim claim,
        ClaimsPrincipal principal)
    {
        yield return OpenIddictConstants.Destinations.AccessToken;

        if (claim.Type == OpenIddictConstants.Claims.Subject &&
            principal.HasScope(OpenIddictConstants.Scopes.OpenId))
        {
            yield return OpenIddictConstants.Destinations.IdentityToken;
            yield break;
        }

        if ((claim.Type == OpenIddictConstants.Claims.Username ||
             claim.Type == OpenIddictConstants.Claims.GivenName) &&
            principal.HasScope(OpenIddictConstants.Scopes.Profile))
        {
            yield return OpenIddictConstants.Destinations.IdentityToken;
            yield break;
        }

        if (claim.Type == OpenIddictConstants.Claims.Email &&
            principal.HasScope(OpenIddictConstants.Scopes.Email))
        {
            yield return OpenIddictConstants.Destinations.IdentityToken;
        }
    }

    private static IResult RedirectToLogin(
        HttpContext httpContext,
        IConfiguration configuration)
    {
        var loginUri = configuration["Identity:OpenIddict:LoginUri"];

        if (string.IsNullOrWhiteSpace(loginUri))
        {
            return Results.Problem(
                title: "Interactive login is not configured.",
                detail: "Identity:OpenIddict:LoginUri must be configured.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var returnUrl =
            httpContext.Request.PathBase +
            httpContext.Request.Path +
            httpContext.Request.QueryString;

        var separator = loginUri.Contains('?') ? '&' : '?';

        return Results.Redirect(
            $"{loginUri}{separator}returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    private static IResult InvalidGrant(string description)
    {
        return Results.Forbid(
            authenticationSchemes:
            [
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
            ],
            properties: new AuthenticationProperties(
                new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                        OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        description
                }));
    }

    private static bool IsLockedOut(User user, DateTimeOffset now)
    {
        return user.IsLockedOut &&
               user.LockoutEndUtc.HasValue &&
               user.LockoutEndUtc.Value > now.UtcDateTime;
    }
}
