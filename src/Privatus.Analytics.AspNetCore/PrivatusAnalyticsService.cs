using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Privatus.Analytics;

/// <summary>
/// Renders the tag with the current settings and host environment. Added by
/// <c>AddPrivatusAnalytics</c>. It only builds HTML: it never sends requests,
/// sets cookies or reads anything about the visitor.
/// </summary>
public sealed class PrivatusAnalyticsService
{
    private readonly IOptionsMonitor<PrivatusAnalyticsOptions> options;
    private readonly IHostEnvironment environment;
    private readonly ILogger<PrivatusAnalyticsService> logger;

    /// <summary>Creates the service. Use dependency injection instead of calling this.</summary>
    public PrivatusAnalyticsService(IOptionsMonitor<PrivatusAnalyticsOptions> options, IHostEnvironment environment, ILogger<PrivatusAnalyticsService> logger)
    {
        this.options = options;
        this.environment = environment;
        this.logger = logger;
    }

    /// <summary>The current settings.</summary>
    public PrivatusAnalyticsOptions Options => options.CurrentValue;

    /// <summary>The host environment name, for example Production.</summary>
    public string EnvironmentName => environment.EnvironmentName;

    /// <summary>Whether the tag is on in this environment.</summary>
    public bool IsEnabled(PrivatusTagOverrides? overrides = null) => PrivatusTag.IsEnabled(Options, EnvironmentName, overrides);

    /// <summary>Whether a tag would print: there is a site id and the tag is on.</summary>
    public bool ShouldRender(PrivatusTagOverrides? overrides = null) => PrivatusTag.ShouldRender(Options, EnvironmentName, overrides);

    /// <summary>The tag as a string. Empty without a site id or when the tag is off.</summary>
    public string Render(HttpContext? httpContext, PrivatusTagOverrides? overrides = null) =>
        PrivatusTag.Render(Options, EnvironmentName, overrides, httpContext);

    /// <summary>The tag as HTML content for Razor.</summary>
    public IHtmlContent RenderHtml(HttpContext? httpContext, PrivatusTagOverrides? overrides = null) =>
        new HtmlString(Render(httpContext, overrides));

    /// <summary>Logs a warning in Development when a click map name is not valid.</summary>
    internal void WarnInvalidClickName(string? name)
    {
        if (environment.IsDevelopment())
        {
            logger.LogWarning(
                "Privatus Analytics: click name \"{ClickName}\" was left out. Use 1 to 100 letters, digits, _ . : or -.",
                name);
        }
    }
}
