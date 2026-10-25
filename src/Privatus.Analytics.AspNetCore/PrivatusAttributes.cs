using System.Text;
using System.Text.RegularExpressions;

namespace Privatus.Analytics;

/// <summary>
/// Builds the data-privatus-* attributes the tracker reads. These print even
/// when the tag is off, so your development and production HTML match.
/// </summary>
public static partial class PrivatusAttributes
{
    /// <summary>The longest event name. Longer names are cut.</summary>
    public const int MaxEventNameLength = 120;

    /// <summary>
    /// The attributes for a custom event, as name and value pairs (values not
    /// escaped). The event fires on click, or on submit for a form. An empty
    /// name gives no attributes.
    /// </summary>
    /// <param name="name">The event name, for example "Report exported".</param>
    /// <param name="props">Event properties as a dictionary or an anonymous object.</param>
    public static IReadOnlyList<KeyValuePair<string, string>> EventPairs(string? name, object? props = null)
    {
        var eventName = Text.Truncate(name?.Trim() ?? string.Empty, MaxEventNameLength);
        if (eventName.Length == 0)
        {
            return [];
        }

        var result = new List<KeyValuePair<string, string>> { new("data-privatus-event", eventName) };
        foreach (var (key, value) in PropValues.Collect([props], PropValues.ToAttributeText))
        {
            result.Add(new KeyValuePair<string, string>("data-privatus-prop-" + key, value));
        }

        return result;
    }

    /// <summary>
    /// The attributes for a custom event as escaped HTML, for example
    /// <c>data-privatus-event="Export CSV" data-privatus-prop-format="csv"</c>.
    /// </summary>
    /// <param name="name">The event name.</param>
    /// <param name="props">Event properties as a dictionary or an anonymous object.</param>
    public static string Event(string? name, object? props = null)
    {
        return Join(EventPairs(name, props));
    }

    /// <summary>Whether a click map name is valid: 1 to 100 of A-Z a-z 0-9 _ . : -.</summary>
    public static bool IsValidClickName(string? name)
    {
        return name is not null && ClickNamePattern().IsMatch(name);
    }

    /// <summary>The click map attribute, for example <c>data-privatus-click="pricing.cta"</c>. An invalid name gives an empty string.</summary>
    public static string Click(string? name)
    {
        return IsValidClickName(name) ? $"data-privatus-click=\"{Text.Escape(name)}\"" : string.Empty;
    }

    internal static string Join(IEnumerable<KeyValuePair<string, string>> pairs)
    {
        var builder = new StringBuilder();
        foreach (var (key, value) in pairs)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(Text.Escape(key)).Append("=\"").Append(Text.Escape(value)).Append('"');
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"\A[A-Za-z0-9_.:-]{1,100}\z")]
    private static partial Regex ClickNamePattern();
}
