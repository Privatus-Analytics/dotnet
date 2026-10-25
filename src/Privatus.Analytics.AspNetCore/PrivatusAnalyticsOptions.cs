using Microsoft.AspNetCore.Http;

namespace Privatus.Analytics;

/// <summary>
/// Settings for the Privatus Analytics script tag. Bound from the
/// <c>PrivatusAnalytics</c> configuration section, then from the action you
/// pass to <c>AddPrivatusAnalytics</c>.
/// </summary>
public sealed class PrivatusAnalyticsOptions
{
    /// <summary>The configuration section the options are read from.</summary>
    public const string SectionName = "PrivatusAnalytics";

    /// <summary>The script host used when nothing else is set.</summary>
    public const string DefaultScriptHost = "https://privatusstats.com";

    /// <summary>The HttpContext.Items key read for a CSP nonce by default.</summary>
    public const string DefaultNonceItemKey = "csp-nonce";

    /// <summary>The site id (starts with pa_). Falls back to the PRIVATUS_SITE_ID environment variable.</summary>
    public string? SiteId { get; set; }

    /// <summary>Origin that serves /js/pa.js. Falls back to PRIVATUS_SCRIPT_HOST, then https://privatusstats.com.</summary>
    public string? ScriptHost { get; set; }

    /// <summary>Tracker modules to load, for example engage and auto. null keeps the tracker default. An empty list loads none.</summary>
    public IList<string>? Modules { get; set; }

    /// <summary>Path globs the tracker never counts, for example /admin/*.</summary>
    public IList<string>? ExcludedPaths { get; set; }

    /// <summary>Hostnames to count. null counts every host.</summary>
    public IList<string>? Domains { get; set; }

    /// <summary>One of auto, history, hash or off. null keeps the tracker default (auto).</summary>
    public string? Spa { get; set; }

    /// <summary>true turns off the automatic pageview.</summary>
    public bool Manual { get; set; }

    /// <summary>
    /// Host environments that get the tag, compared with
    /// <c>IHostEnvironment.EnvironmentName</c>. null means Production only.
    /// Used when <see cref="Enabled"/> is null.
    /// </summary>
    public IList<string>? EnabledEnvironments { get; set; }

    /// <summary>true or false forces the tag on or off in every environment. null uses <see cref="EnabledEnvironments"/>.</summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Lets the auto inject middleware work. It only runs when you call
    /// <c>app.UsePrivatusAnalyticsAutoInject()</c>. Set false to switch it off from settings.
    /// </summary>
    public bool AutoInject { get; set; } = true;

    /// <summary>Extra data- attributes, for example data-mask.</summary>
    public IDictionary<string, string?> ExtraAttributes { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Page properties for each request, for example plan and role. Return a
    /// dictionary or an anonymous object, or null for none. Never return
    /// anything that identifies a person.
    /// </summary>
    public Func<HttpContext, object?>? PageProps { get; set; }

    /// <summary>true prints the official queue stub before the tag, so privatus.track works before the script loads.</summary>
    public bool QueueStub { get; set; }

    /// <summary>A fixed CSP nonce. Usually you set <see cref="NonceItemKey"/> instead.</summary>
    public string? Nonce { get; set; }

    /// <summary>The HttpContext.Items key that holds the CSP nonce for the request. Default csp-nonce.</summary>
    public string NonceItemKey { get; set; } = DefaultNonceItemKey;
}
