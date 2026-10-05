using Microsoft.Extensions.Configuration;
using Modules.System.Identity.Infrastructure.Persistence;
using OpenIddict.Abstractions;

namespace Application.Tests.Authentication;

public sealed class OpenIddictWebClientRegistrationTests
{
    [Fact]
    public void ShineraWeb_IsPublicPkceClientWithoutPasswordGrantOrSecret()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Identity:OpenIddict:WebClient:ClientId"] = "shinera-web",
                    ["Identity:OpenIddict:WebClient:RedirectUris:0"] =
                        "https://app.example.test/auth/callback",
                    ["Identity:OpenIddict:WebClient:PostLogoutRedirectUris:0"] =
                        "https://app.example.test/"
                })
            .Build();

        var descriptor =
            OpenIddictWebClientRegistration.CreateDescriptor(
                configuration);

        Assert.Equal("shinera-web", descriptor.ClientId);

        Assert.Equal(
            OpenIddictConstants.ClientTypes.Public,
            descriptor.ClientType);

        Assert.Null(descriptor.ClientSecret);

        Assert.Contains(
            OpenIddictConstants.Requirements.Features
                .ProofKeyForCodeExchange,
            descriptor.Requirements);

        Assert.Contains(
            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
            descriptor.Permissions);

        Assert.Contains(
            OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
            descriptor.Permissions);

        Assert.DoesNotContain(
            OpenIddictConstants.Permissions.GrantTypes.Password,
            descriptor.Permissions);

        Assert.Contains(
            OpenIddictConstants.Permissions.ResponseTypes.Code,
            descriptor.Permissions);

        Assert.Contains(
            new Uri("https://app.example.test/auth/callback"),
            descriptor.RedirectUris);

        Assert.Contains(
            new Uri("https://app.example.test/"),
            descriptor.PostLogoutRedirectUris);
    }
}
