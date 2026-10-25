namespace Privatus.Analytics;

/// <summary>
/// Per-call overrides for one tag. Any property left null uses the configured value.
/// </summary>
public sealed class PrivatusTagOverrides
{
    /// <summary>Overrides <see cref="PrivatusAnalyticsOptions.SiteId"/>.</summary>
    public string? SiteId { get; set; }

    /// <summary>Overrides <see cref="PrivatusAnalyticsOptions.ScriptHost"/>.</summary>
    public string? ScriptHost { get; set; }

    /// <summary>Overrides <see cref="PrivatusAnalyticsOptions.Modules"/>. An empty list loads none.</summary>
    public IEnumerable<string>? Modules { get; set; }

    /// <summary>Overrides <see cref="PrivatusAnalyticsOptions.ExcludedPaths"/>.</summary>
    public IEnumerable<string>? ExcludedPaths { get; set; }

    /// <summary>Overrides <see cref="PrivatusAnalyticsOptions.Domains"/>.</summary>
    public IEnumerable<string>? Domains { get; set; }

    /// <summary>Overrides <see cref="PrivatusAnalyticsOptions.Spa"/>.</summary>
    public string? Spa { get; set; }

    /// <summary>Overrides <see cref="PrivatusAnalyticsOptions.Manual"/>.</summary>
    public bool? Manual { get; set; }

    /// <summary>Forces this tag on or off.</summary>
    public bool? Enabled { get; set; }

    /// <summary>Extra data- attributes, added on top of the configured ones.</summary>
    public IDictionary<string, string?>? ExtraAttributes { get; set; }

    /// <summary>Page properties (dictionary or anonymous object), added on top of <see cref="PrivatusAnalyticsOptions.PageProps"/>.</summary>
    public object? Props { get; set; }

    /// <summary>Overrides <see cref="PrivatusAnalyticsOptions.QueueStub"/>.</summary>
    public bool? QueueStub { get; set; }

    /// <summary>The CSP nonce for this tag.</summary>
    public string? Nonce { get; set; }
}
