using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Privatus.Analytics;

/// <summary>HTML helpers for Razor views and pages that do not use tag helpers.</summary>
public static class PrivatusAnalyticsHtmlHelperExtensions
{
    /// <summary>
    /// Prints the script tag: <c>@Html.PrivatusAnalytics()</c>. Prints nothing
    /// without a site id or when the tag is off.
    /// </summary>
    public static IHtmlContent PrivatusAnalytics(this IHtmlHelper html, PrivatusTagOverrides? overrides = null)
    {
        ArgumentNullException.ThrowIfNull(html);
        var context = html.ViewContext.HttpContext;
        return context.RequestServices.GetRequiredService<PrivatusAnalyticsService>().RenderHtml(context, overrides);
    }

    /// <summary>
    /// Prints event attributes inside an element:
    /// <c>&lt;button @Html.PrivatusEvent("Export CSV", new { format = "csv" })&gt;</c>.
    /// </summary>
    public static IHtmlContent PrivatusEvent(this IHtmlHelper html, string? name, object? props = null)
    {
        ArgumentNullException.ThrowIfNull(html);
        return new HtmlString(PrivatusAttributes.Event(name, props));
    }

    /// <summary>Prints the click map attribute: <c>&lt;a @Html.PrivatusClick("pricing.cta")&gt;</c>.</summary>
    public static IHtmlContent PrivatusClick(this IHtmlHelper html, string? name)
    {
        ArgumentNullException.ThrowIfNull(html);
        if (!PrivatusAttributes.IsValidClickName(name))
        {
            html.ViewContext.HttpContext.RequestServices.GetService<PrivatusAnalyticsService>()?.WarnInvalidClickName(name);
            return HtmlString.Empty;
        }

        return new HtmlString(PrivatusAttributes.Click(name));
    }
}
