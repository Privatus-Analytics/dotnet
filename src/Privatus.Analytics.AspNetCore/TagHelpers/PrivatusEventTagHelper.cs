using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Privatus.Analytics.TagHelpers;

/// <summary>
/// Turns <c>privatus-event="Export CSV" privatus-prop-format="csv"</c> on any
/// element into the data-privatus-* attributes the tracker reads. The event
/// fires on click, or on submit for a form.
/// </summary>
[HtmlTargetElement(Attributes = "privatus-event")]
public sealed class PrivatusEventTagHelper : TagHelper
{
    /// <summary>The event name, for example "Report exported".</summary>
    [HtmlAttributeName("privatus-event")]
    public string? EventName { get; set; }

    /// <summary>Event properties as a dictionary or an anonymous object.</summary>
    [HtmlAttributeName("privatus-props")]
    public object? Props { get; set; }

    /// <summary>Event properties as text attributes, for example privatus-prop-format="csv".</summary>
    [HtmlAttributeName(DictionaryAttributePrefix = "privatus-prop-")]
    public IDictionary<string, string?> PropValues { get; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var props = Analytics.PropValues.Read(Props)
            .Concat(PropValues.Select(pair => new KeyValuePair<string, object?>(pair.Key, pair.Value)))
            .ToList();

        foreach (var (name, value) in PrivatusAttributes.EventPairs(EventName, props))
        {
            output.Attributes.SetAttribute(new TagHelperAttribute(name, new HtmlString(Text.Escape(value)), HtmlAttributeValueStyle.DoubleQuotes));
        }
    }
}
