using Microsoft.AspNetCore.Http;

namespace Privatus.Analytics;

/// <summary>
/// Builds the Privatus Analytics script tag. Most apps use the tag helper, the
/// HTML helper or the Blazor component instead, which call this for you.
/// </summary>
public static class PrivatusTag
{
    /// <summary>The path of the tracker script. The middleware skips pages that already contain it.</summary>
    public const string ScriptPath = "/js/pa.js";

    /// <summary>The name of the page props function the tracker reads through data-props.</summary>
    public const string PagePropsFunction = "privatusPageProps";

    private static readonly string[] SpaModes = ["auto", "history", "hash", "off"];

    private const string QueueStubScript =
        "window.privatus=window.privatus||{q:[]};['track','pageview','props','optOut','optIn','ready'].forEach(function(m){window.privatus[m]=window.privatus[m]||function(){window.privatus.q.push([m,Array.prototype.slice.call(arguments)])}})";

    /// <summary>The site id after the PRIVATUS_SITE_ID fallback, or null when there is none.</summary>
    public static string? ResolveSiteId(PrivatusAnalyticsOptions options, PrivatusTagOverrides? overrides = null)
    {
        return FirstValue(overrides?.SiteId, options.SiteId, Environment.GetEnvironmentVariable("PRIVATUS_SITE_ID"));
    }

    /// <summary>The script host after the PRIVATUS_SCRIPT_HOST fallback, without a trailing slash.</summary>
    public static string ResolveScriptHost(PrivatusAnalyticsOptions options, PrivatusTagOverrides? overrides = null)
    {
        var host = FirstValue(overrides?.ScriptHost, options.ScriptHost, Environment.GetEnvironmentVariable("PRIVATUS_SCRIPT_HOST"))
            ?? PrivatusAnalyticsOptions.DefaultScriptHost;
        return host.TrimEnd('/');
    }

    /// <summary>Whether the tag is on for this environment. Enabled wins, then the environment list (Production by default).</summary>
    public static bool IsEnabled(PrivatusAnalyticsOptions options, string? environmentName, PrivatusTagOverrides? overrides = null)
    {
        var forced = overrides?.Enabled ?? options.Enabled;
        if (forced.HasValue)
        {
            return forced.Value;
        }

        var environments = options.EnabledEnvironments ?? ["Production"];
        return environmentName is not null && environments.Any(name => string.Equals(name?.Trim(), environmentName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Whether a tag would print: there is a site id and the tag is enabled.</summary>
    public static bool ShouldRender(PrivatusAnalyticsOptions options, string? environmentName, PrivatusTagOverrides? overrides = null)
    {
        return ResolveSiteId(options, overrides) is not null && IsEnabled(options, environmentName, overrides);
    }

    /// <summary>
    /// Renders the tag (and the queue stub and page props script when used).
    /// Returns an empty string without a site id or when the tag is off.
    /// </summary>
    /// <param name="options">The settings.</param>
    /// <param name="environmentName">The host environment name, for example Production.</param>
    /// <param name="overrides">Per-call overrides.</param>
    /// <param name="httpContext">The request. Used for the nonce and <see cref="PrivatusAnalyticsOptions.PageProps"/>. May be null.</param>
    public static string Render(PrivatusAnalyticsOptions options, string? environmentName, PrivatusTagOverrides? overrides = null, HttpContext? httpContext = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var siteId = ResolveSiteId(options, overrides);
        if (siteId is null || !IsEnabled(options, environmentName, overrides))
        {
            return string.Empty;
        }

        var nonce = ResolveNonce(options, overrides, httpContext);
        var nonceAttribute = nonce is null ? string.Empty : $" nonce=\"{Text.Escape(nonce)}\"";
        var parts = new List<string>(3);

        if (overrides?.QueueStub ?? options.QueueStub)
        {
            parts.Add($"<script{nonceAttribute}>{QueueStubScript}</script>");
        }

        var props = PageProps(options, overrides, httpContext);
        if (props.Count > 0)
        {
            var json = "{" + string.Join(",", props.Select(pair => PropValues.JsonString(pair.Key) + ":" + pair.Value)) + "}";
            parts.Add($"<script{nonceAttribute}>window.{PagePropsFunction}=function(){{return {json}}}</script>");
        }

        var attributes = new Attributes();
        attributes.Set("defer", null);
        attributes.Set("src", ResolveScriptHost(options, overrides) + ScriptPath);
        attributes.Set("data-site", siteId);

        AddList(attributes, "data-modules", overrides?.Modules ?? options.Modules, keepEmpty: true);
        AddList(attributes, "data-exclude", overrides?.ExcludedPaths ?? options.ExcludedPaths, keepEmpty: false);
        AddList(attributes, "data-domains", overrides?.Domains ?? options.Domains, keepEmpty: false);

        var spa = (overrides?.Spa ?? options.Spa)?.Trim().ToLowerInvariant();
        if (spa is not null && spa != "auto" && SpaModes.Contains(spa))
        {
            attributes.Set("data-spa", spa);
        }

        if (overrides?.Manual ?? options.Manual)
        {
            attributes.Set("data-manual", "true");
        }

        if (props.Count > 0)
        {
            attributes.Set("data-props", PagePropsFunction);
        }

        AddExtra(attributes, options.ExtraAttributes);
        AddExtra(attributes, overrides?.ExtraAttributes);

        if (nonce is not null)
        {
            attributes.Set("nonce", nonce);
        }

        parts.Add($"<script {attributes}></script>");
        return string.Join("\n", parts);
    }

    /// <summary>The official queue stub script, so privatus.track calls made before the tracker loads are kept.</summary>
    public static string QueueStub(string? nonce = null)
    {
        var nonceAttribute = string.IsNullOrEmpty(nonce) ? string.Empty : $" nonce=\"{Text.Escape(nonce)}\"";
        return $"<script{nonceAttribute}>{QueueStubScript}</script>";
    }

    internal static string? ResolveNonce(PrivatusAnalyticsOptions options, PrivatusTagOverrides? overrides, HttpContext? httpContext)
    {
        if (!string.IsNullOrEmpty(overrides?.Nonce))
        {
            return overrides.Nonce;
        }

        if (httpContext is not null && !string.IsNullOrEmpty(options.NonceItemKey)
            && httpContext.Items.TryGetValue(options.NonceItemKey, out var item) && item is not null)
        {
            var fromItems = item.ToString();
            if (!string.IsNullOrEmpty(fromItems))
            {
                return fromItems;
            }
        }

        return string.IsNullOrEmpty(options.Nonce) ? null : options.Nonce;
    }

    private static List<KeyValuePair<string, string>> PageProps(PrivatusAnalyticsOptions options, PrivatusTagOverrides? overrides, HttpContext? httpContext)
    {
        var configured = httpContext is not null && options.PageProps is not null ? options.PageProps(httpContext) : null;
        return PropValues.Collect([configured, overrides?.Props], PropValues.ToJson);
    }

    private static void AddList(Attributes attributes, string name, IEnumerable<string>? value, bool keepEmpty)
    {
        var values = Text.List(value);
        // null keeps the tracker default. An empty modules list loads none.
        if (values is not null && (keepEmpty || values.Count > 0))
        {
            attributes.Set(name, string.Join(",", values));
        }
    }

    private static void AddExtra(Attributes attributes, IDictionary<string, string?>? extra)
    {
        if (extra is null)
        {
            return;
        }

        foreach (var (name, value) in extra)
        {
            if (value is not null && !string.IsNullOrWhiteSpace(name))
            {
                attributes.Set(name.Trim(), value);
            }
        }
    }

    private static string? FirstValue(params string?[] values)
    {
        foreach (var value in values)
        {
            var trimmed = value?.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                return trimmed;
            }
        }

        return null;
    }

    /// <summary>An ordered attribute list where setting a name again replaces its value in place.</summary>
    private sealed class Attributes
    {
        private readonly List<KeyValuePair<string, string?>> items = [];

        public void Set(string name, string? value)
        {
            var index = items.FindIndex(item => string.Equals(item.Key, name, StringComparison.OrdinalIgnoreCase));
            var pair = new KeyValuePair<string, string?>(name, value);
            if (index >= 0)
            {
                items[index] = pair;
            }
            else
            {
                items.Add(pair);
            }
        }

        public override string ToString()
        {
            return string.Join(" ", items.Select(item => item.Value is null ? Text.Escape(item.Key) : $"{Text.Escape(item.Key)}=\"{Text.Escape(item.Value)}\""));
        }
    }
}
