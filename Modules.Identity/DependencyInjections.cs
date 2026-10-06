using Application.SharedKernel;
using Application.SharedKernel.Abstractions;
using Infrastructure.SharedKernel;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.System.Identity.Application.Abstractions;
using Modules.System.Identity.Application.Authorization;
using Modules.System.Identity.Application.Features.ApiResources.Services;
using Modules.System.Identity.Application.Mapping;
using Modules.System.Identity.Application.Services;
using Modules.System.Identity.Domain.Entities;
using Modules.System.Identity.Infrastructure.Persistence;
using Modules.System.Identity.Infrastructure.Persistence.Contexts;
using Modules.System.Identity.Infrastructure.Persistence.Interceptors;
using Modules.System.Identity.Web.Authentication;
using Modules.System.Identity.Web.Authorization;
using Modules.System.Identity.Web.Endpoints;
using Modules.System.Identity.Web.Middlewares;
using Modules.System.Identity.Web.Util;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using System.Reflection;

namespace Modules.System.Identity;

public static class DependencyInjections
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(
            provider => provider.GetRequiredService<TenantContext>());
        services.AddScoped<ICurrentTenant>(
            provider => provider.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantAccessResolver, TenantAccessResolver>();
        services.AddScoped<TenantSaveChangesInterceptor>();

        services.AddBaseInfrastructureServices<IdentityDbContext>(
            configuration,
            "Identity",
            (sp, options) =>
            {
                options.AddInterceptors(sp.GetRequiredService<TenantSaveChangesInterceptor>());
                options.UseOpenIddict();
            });

        services.AddScoped<ApiResourceSyncService>();

        var assembly = Assembly.GetExecutingAssembly();
        services.AddCustomCqrs<AppMappingProfile>(assembly);

        services.AddDataProtection();

        var allowedOrigins = configuration
            .GetSection("Identity:OpenIddict:AllowedOrigins")
            .GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();

        services.AddCors(options =>
        {
            options.AddPolicy("shinera-web", policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
            });
        });

        services.AddScoped<CaptchaService>();
        services.AddScoped<IIdentityDbContext>(
            provider => provider.GetRequiredService<IdentityDbContext>());
        services.AddScoped<IPermissionAuthorizationService, PermissionAuthorizationService>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<IdentityDbContext>();
            })
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("/connect/authorize");
                options.SetTokenEndpointUris("/connect/token");
                options.SetEndSessionEndpointUris("/connect/logout");

                options.AllowAuthorizationCodeFlow();
                options.AllowRefreshTokenFlow();
                options.RequireProofKeyForCodeExchange();

                options.RegisterScopes("shinera_api");

                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

                // Rolling refresh tokens and sliding refresh-token expiration are
                // secure defaults in OpenIddict and deliberately remain enabled.
                // Token and authorization storage also remain enabled.

                options.AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            })
            .AddCookie(
                InteractiveAuthenticationDefaults.Scheme,
                options =>
                {
                    options.Cookie.Name = "__Host-Shinera.Interactive";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = true;
                });

        services.AddScoped<OpenIddictWebClientSeedContributor>();
        services.AddScoped<ISeedContributor>(
            provider => provider.GetRequiredService<OpenIddictWebClientSeedContributor>());
        services.AddScoped<ISeedContributor, DefaultIdentitySeedContributor>();
        services.AddAuthorization();

        return services;
    }

    public static WebApplication UseIdentityModule(
        this WebApplication app,
        IConfiguration configuration)
    {
        app.UseCors("shinera-web");
        app.UseAuthentication();
        app.UseMiddleware<TenantResolutionMiddleware>();
        app.UseAuthorization();

        app.MapOpenIddictProtocolEndpoints(configuration);
        app.MapEndpoints($"{configuration["BackendPrefix"]}System");

        return app;
    }
}
