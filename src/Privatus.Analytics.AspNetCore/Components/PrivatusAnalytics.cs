using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;

namespace Privatus.Analytics;

/// <summary>
/// Blazor component that prints the script tag. Put
/// <c>&lt;PrivatusAnalytics /&gt;</c> in the head of App.razor. The head is
/// rendered on the server (static server rendering), so the nonce and page
/// props from the request work there.
/// </summary>
public sealed class PrivatusAnalytics : ComponentBase
{
    /// <summary>The current request during server rendering. Set by Blazor.</summary>
    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }

    /// <summary>The service that renders the tag. Set by Blazor.</summary>
    [Inject]
    public PrivatusAnalyticsService Service { get; set; } = default!;

    /// <summary>Overrides the site id.</summary>
    [Parameter]
    public string? SiteId { get; set; }

    /// <summary>Overrides the script host.</summary>
    [Parameter]
    public string? ScriptHost { get; set; }

    /// <summary>Tracker modules. An empty list loads none.</summary>
    [Parameter]
    public IEnumerable<string>? Modules { get; set; }

    /// <summary>Path globs the tracker never counts.</summary>
    [Parameter]
    public IEnumerable<string>? ExcludedPaths { get; set; }

    /// <summary>Hostnames to count.</summary>
    [Parameter]
    public IEnumerable<string>? Domains { get; set; }

    /// <summary>auto, history, hash or off.</summary>
    [Parameter]
    public string? Spa { get; set; }

    /// <summary>true turns off the automatic pageview.</summary>
    [Parameter]
    public bool? Manual { get; set; }

    /// <summary>Forces the tag on or off.</summary>
    [Parameter]
    public bool? Enabled { get; set; }

    /// <summary>true prints the queue stub before the tag.</summary>
    [Parameter]
    public bool? QueueStub { get; set; }

    /// <summary>The CSP nonce. Defaults to HttpContext.Items["csp-nonce"].</summary>
    [Parameter]
    public string? Nonce { get; set; }

    /// <summary>Page properties as a dictionary or an anonymous object.</summary>
    [Parameter]
    public object? Props { get; set; }

    /// <summary>Extra data- attributes for the script tag.</summary>
    [Parameter]
    public IDictionary<string, string?>? ExtraAttributes { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var html = Service.Render(HttpContext, new PrivatusTagOverrides
        {
            SiteId = SiteId,
            ScriptHost = ScriptHost,
            Modules = Modules,
            ExcludedPaths = ExcludedPaths,
            Domains = Domains,
            Spa = Spa,
            Manual = Manual,
            Enabled = Enabled,
            QueueStub = QueueStub,
            Nonce = Nonce,
            Props = Props,
            ExtraAttributes = ExtraAttributes,
        });

        if (html.Length > 0)
        {
            builder.AddMarkupContent(0, html);
        }
    }
}
