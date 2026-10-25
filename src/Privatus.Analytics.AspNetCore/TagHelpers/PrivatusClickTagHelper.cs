using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Privatus.Analytics.TagHelpers;

/// <summary>
/// Turns <c>privatus-click="pricing.cta"</c> on any element into
/// data-privatus-click, the name used in click maps. An invalid name prints
/// nothing (with a warning in the log in Development).
/// </summary>
[HtmlTargetElement(Attributes = "privatus-click")]
public sealed class PrivatusClickTagHelper : TagHelper
{
    /// <summary>The Razor view context. Set by Razor.</summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext? ViewContext { get; set; }

    /// <summary>The click map name: 1 to 100 of A-Z a-z 0-9 _ . : -.</summary>
    [HtmlAttributeName("privatus-click")]
    public string? Name { get; set; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (PrivatusAttributes.IsValidClickName(Name))
        {
            output.Attributes.SetAttribute(new TagHelperAttribute("data-privatus-click", new HtmlString(Text.Escape(Name)), HtmlAttributeValueStyle.DoubleQuotes));
            return;
        }

        ViewContext?.HttpContext.RequestServices.GetService<PrivatusAnalyticsService>()?.WarnInvalidClickName(Name);
    }
}
