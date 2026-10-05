using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace Modules.System.Identity.Infrastructure.Persistence;

public sealed class OpenIddictWebClientSeedContributor : ISeedContributor
{
    public int Order => 5;

    public async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var configuration =
            serviceProvider.GetRequiredService<IConfiguration>();

        var manager =
            serviceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var descriptor =
            OpenIddictWebClientRegistration.CreateDescriptor(configuration);

        var application =
            await manager.FindByClientIdAsync(descriptor.ClientId!);

        if (application is null)
        {
            await manager.CreateAsync(descriptor);
            return;
        }

        await manager.UpdateAsync(application, descriptor);
    }
}

public static class OpenIddictWebClientRegistration
{
    private const string SectionPath =
        "Identity:OpenIddict:WebClient";

    public static OpenIddictApplicationDescriptor CreateDescriptor(
        IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionPath);

        var clientId = section["ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException(
                $"{SectionPath}:ClientId must be configured.");
        }

        var redirectUris =
            ReadRequiredUris(section.GetSection("RedirectUris"));

        var postLogoutRedirectUris =
            ReadRequiredUris(section.GetSection("PostLogoutRedirectUris"));

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            DisplayName = "Shinera Web"
        };

        foreach (var uri in redirectUris)
            descriptor.RedirectUris.Add(uri);

        foreach (var uri in postLogoutRedirectUris)
            descriptor.PostLogoutRedirectUris.Add(uri);

        descriptor.Permissions.Add(
            OpenIddictConstants.Permissions.Endpoints.Authorization);

        descriptor.Permissions.Add(
            OpenIddictConstants.Permissions.Endpoints.Token);

        descriptor.Permissions.Add(
            OpenIddictConstants.Permissions.Endpoints.EndSession);

        descriptor.Permissions.Add(
            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);

        descriptor.Permissions.Add(
            OpenIddictConstants.Permissions.GrantTypes.RefreshToken);

        descriptor.Permissions.Add(
            OpenIddictConstants.Permissions.ResponseTypes.Code);

        descriptor.Permissions.Add(
            OpenIddictConstants.Permissions.Scopes.Profile);

        descriptor.Permissions.Add(
            OpenIddictConstants.Permissions.Scopes.Email);

        descriptor.Permissions.Add(
            OpenIddictConstants.Permissions.Prefixes.Scope +
            "shinera_api");

        descriptor.Requirements.Add(
            OpenIddictConstants.Requirements.Features
                .ProofKeyForCodeExchange);

        return descriptor;
    }

    private static IReadOnlyList<Uri> ReadRequiredUris(
        IConfigurationSection section)
    {
        var values = section
            .GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value =>
            {
                if (!Uri.TryCreate(
                        value,
                        UriKind.Absolute,
                        out var uri))
                {
                    throw new InvalidOperationException(
                        $"{section.Path} contains an invalid absolute URI.");
                }

                return uri;
            })
            .ToArray();

        if (values.Length == 0)
        {
            throw new InvalidOperationException(
                $"{section.Path} must contain at least one URI.");
        }

        return values;
    }
}
