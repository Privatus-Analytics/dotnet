using System.Globalization;
using System.Net;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Privatus.Analytics.TagHelpers;

/// <summary>
/// Prints the Privatus Analytics script tag. Put it in the head of your layout:
/// <c>&lt;privatus-analytics /&gt;</c>. Any data- attribute you add is passed to
/// the script tag. Without a site id or when the tag is off it prints only
/// the queue stub (when on), or nothing.
/// </summary>
[HtmlTargetElement("privatus-analytics", TagStructure = TagStructure.WithoutEndTag)]
public sealed class PrivatusAnalyticsTagHelper : TagHelper
{
    /// <summary>The Razor view context. Set by Razor.</summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    /// <summary>Overrides the site id.</summary>
    [HtmlAttributeName("site-id")]
    public string? SiteId { get; set; }

    /// <summary>Overrides the script host.</summary>
    [HtmlAttributeName("script-host")]
    public string? ScriptHost { get; set; }

    /// <summary>Tracker modules as a comma list, for example "engage,auto". An empty value loads none.</summary>
    [HtmlAttributeName("modules")]
    public string? Modules { get; set; }

    /// <summary>Path globs the tracker never counts, as a comma list.</summary>
    [HtmlAttributeName("excluded-paths")]
    public string? ExcludedPaths { get; set; }

    /// <summary>Hostnames to count, as a comma list.</summary>
    [HtmlAttributeName("domains")]
    public string? Domains { get; set; }

    /// <summary>auto, history, hash or off.</summary>
    [HtmlAttributeName("spa")]
    public string? Spa { get; set; }

    /// <summary>true turns off the automatic pageview.</summary>
    [HtmlAttributeName("manual")]
    public bool? Manual { get; set; }

    /// <summary>Forces the tag on or off.</summary>
    [HtmlAttributeName("enabled")]
    public bool? Enabled { get; set; }

    /// <summary>true prints the queue stub before the tag.</summary>
    [HtmlAttributeName("queue-stub")]
    public bool? QueueStub { get; set; }

    /// <summary>The CSP nonce. Defaults to HttpContext.Items["csp-nonce"].</summary>
    [HtmlAttributeName("nonce")]
    public string? Nonce { get; set; }

    /// <summary>Page properties as a dictionary or an anonymous object.</summary>
    [HtmlAttributeName("props")]
    public object? Props { get; set; }

    /// <summary>Page properties as text attributes, for example prop-plan="pro".</summary>
    [HtmlAttributeName(DictionaryAttributePrefix = "prop-")]
    public IDictionary<string, string?> PropValues { get; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var service = ViewContext.HttpContext.RequestServices.GetRequiredService<PrivatusAnalyticsService>();
        var overrides = new PrivatusTagOverrides
        {
            SiteId = SiteId,
            ScriptHost = ScriptHost,
            Modules = Modules is null ? null : [Modules],
            ExcludedPaths = ExcludedPaths is null ? null : [ExcludedPaths],
            Domains = Domains is null ? null : [Domains],
            Spa = Spa,
            Manual = Manual,
            Enabled = Enabled,
            QueueStub = QueueStub,
            Nonce = Nonce,
            Props = Analytics.PropValues.Read(Props)
                .Concat(PropValues.Select(pair => new KeyValuePair<string, object?>(pair.Key, pair.Value)))
                .ToList(),
            ExtraAttributes = DataAttributes(output),
        };

        output.TagName = null;
        var html = service.Render(ViewContext.HttpContext, overrides);
        if (html.Length == 0)
        {
            output.SuppressOutput();
            return;
        }

        output.Content.SetHtmlContent(html);
    }

    /// <summary>Any data- attribute on the element goes to the script tag as it is, for example data-debug="true".</summary>
    private static Dictionary<string, string?> DataAttributes(TagHelperOutput output)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in output.Attributes)
        {
            if (!attribute.Name.StartsWith("data-", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result[attribute.Name] = attribute.Value switch
            {
                null => string.Empty,
                IHtmlContent content => WebUtility.HtmlDecode(Write(content)),
                var value => Convert.ToString(value, CultureInfo.InvariantCulture),
            };
        }

        return result;
    }

    private static string Write(IHtmlContent content)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }
}
