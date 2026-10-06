using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Modules.System.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace Application.Tests.Authentication;

public sealed class OpenIddictServerConfigurationTests
{
    [Fact]
    public void Server_UsesAuthorizationCodePkceWithoutPasswordGrant()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["DBNAME"] = "shinera-tests"
                })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddIdentityModule(
            configuration,
            new TestHostEnvironment(
                Environments.Development));

        using var provider = services.BuildServiceProvider();

        var options = provider
            .GetRequiredService<IOptionsMonitor<OpenIddictServerOptions>>()
            .CurrentValue;

        Assert.Contains(
            OpenIddictConstants.GrantTypes.AuthorizationCode,
            options.GrantTypes);

        Assert.Contains(
            OpenIddictConstants.GrantTypes.RefreshToken,
            options.GrantTypes);

        Assert.DoesNotContain(
            OpenIddictConstants.GrantTypes.Password,
            options.GrantTypes);

        Assert.True(options.RequireProofKeyForCodeExchange);

        Assert.Equal(
            TimeSpan.FromMinutes(15),
            options.AccessTokenLifetime);

        Assert.Equal(
            TimeSpan.FromDays(30),
            options.RefreshTokenLifetime);

        Assert.False(options.DisableRollingRefreshTokens);
        Assert.False(options.DisableSlidingRefreshTokenExpiration);
        Assert.False(options.DisableTokenStorage);
        Assert.False(options.DisableAuthorizationStorage);
        Assert.False(options.DisableAccessTokenEncryption);

        Assert.Contains(
            options.AuthorizationEndpointUris,
            uri => uri.OriginalString == "/connect/authorize");

        Assert.Contains(
            options.TokenEndpointUris,
            uri => uri.OriginalString == "/connect/token");

        Assert.Contains(
            options.EndSessionEndpointUris,
            uri => uri.OriginalString == "/connect/logout");
    }

    [Fact]
    public void Production_WithoutDurableCertificates_FailsFast()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["DBNAME"] = "shinera-tests"
                })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddIdentityModule(
                configuration,
                new TestHostEnvironment(
                    Environments.Production)));

        Assert.Contains(
            "SigningCertificate:Path",
            exception.Message,
            StringComparison.Ordinal);
    }

    private sealed class TestHostEnvironment(
        string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } =
            environmentName;

        public string ApplicationName { get; set; } =
            "Application.Tests";

        public string ContentRootPath { get; set; } =
            AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}
