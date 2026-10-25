using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Privatus.Analytics.AspNetCore.Tests;

[CollectionDefinition("Environment variables", DisableParallelization = true)]
public sealed class EnvironmentCollection
{
}

[Collection("Environment variables")]
public sealed class ConfigurationTests
{
    private static PrivatusAnalyticsService Service(Dictionary<string, string?> settings, string environment = "Production", Action<PrivatusAnalyticsOptions>? configure = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment { EnvironmentName = environment });
        services.AddLogging();
        services.AddPrivatusAnalytics(configure);
        return services.BuildServiceProvider().GetRequiredService<PrivatusAnalyticsService>();
    }

    [Fact]
    public void ReadsThePrivatusAnalyticsSection()
    {
        var service = Service(new()
        {
            ["PrivatusAnalytics:SiteId"] = "pa_FROMCONFIG",
            ["PrivatusAnalytics:Modules:0"] = "engage",
            ["PrivatusAnalytics:Modules:1"] = "auto",
            ["PrivatusAnalytics:Spa"] = "history",
            ["PrivatusAnalytics:ExtraAttributes:data-debug"] = "true",
        });

        var tag = service.Render(null);

        Assert.Contains("data-site=\"pa_FROMCONFIG\"", tag);
        Assert.Contains("data-modules=\"engage,auto\"", tag);
        Assert.Contains("data-spa=\"history\"", tag);
        Assert.Contains("data-debug=\"true\"", tag);
    }

    [Fact]
    public void TheConfigureActionRunsAfterTheSection()
    {
        var service = Service(new() { ["PrivatusAnalytics:SiteId"] = "pa_FROMCONFIG" }, configure: o => o.SiteId = "pa_FROMCODE");

        Assert.Contains("data-site=\"pa_FROMCODE\"", service.Render(null));
    }

    [Fact]
    public void ReadsAGivenSection()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Analytics:SiteId"] = "pa_OTHERSECTION" }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment { EnvironmentName = "Production" });
        services.AddLogging();
        services.AddPrivatusAnalytics(configuration.GetSection("Analytics"));

        Assert.Contains("pa_OTHERSECTION", services.BuildServiceProvider().GetRequiredService<PrivatusAnalyticsService>().Render(null));
    }

    [Fact]
    public void IsOnlyEnabledInProductionByDefault()
    {
        var settings = new Dictionary<string, string?> { ["PrivatusAnalytics:SiteId"] = "pa_X" };

        Assert.True(Service(settings, "Production").ShouldRender());
        Assert.False(Service(settings, "Development").ShouldRender());
        Assert.True(Service(new(settings) { ["PrivatusAnalytics:Enabled"] = "true" }, "Development").ShouldRender());
    }

    [Fact]
    public void EnabledEnvironmentsFromConfigurationReplaceTheDefault()
    {
        var settings = new Dictionary<string, string?> { ["PrivatusAnalytics:SiteId"] = "pa_X", ["PrivatusAnalytics:EnabledEnvironments:0"] = "Staging" };

        Assert.True(Service(settings, "Staging").ShouldRender());
        Assert.False(Service(settings, "Production").ShouldRender());
    }

    [Fact]
    public void FallsBackToEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("PRIVATUS_SITE_ID", " pa_FROMENV ");
        Environment.SetEnvironmentVariable("PRIVATUS_SCRIPT_HOST", "https://stats.example.com/");
        try
        {
            var tag = Service(new()).Render(null);

            Assert.Equal("<script defer src=\"https://stats.example.com/js/pa.js\" data-site=\"pa_FROMENV\"></script>", tag);
            Assert.Contains("pa_SETTING", Service(new() { ["PrivatusAnalytics:SiteId"] = "pa_SETTING" }).Render(null));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PRIVATUS_SITE_ID", null);
            Environment.SetEnvironmentVariable("PRIVATUS_SCRIPT_HOST", null);
        }
    }
}
