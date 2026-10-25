using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Privatus.Analytics.AspNetCore.Tests;

/// <summary>The tag helpers, HTML helpers, Blazor component and middleware in a real app.</summary>
public sealed class WebAppTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Snippet = "<script defer src=\"https://privatusstats.com/js/pa.js\" data-site=\"pa_TEST123456\"></script>";

    private readonly WebApplicationFactory<Program> factory;

    public WebAppTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    private HttpClient Client(string environment = "Production", Action<PrivatusAnalyticsOptions>? configure = null) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("PrivatusAnalytics:SiteId", "pa_TEST123456");
            builder.ConfigureTestServices(services => services.Configure<PrivatusAnalyticsOptions>(o => configure?.Invoke(o)));
        }).CreateClient();

    private static int Count(string text, string part) => Regex.Matches(text, Regex.Escape(part)).Count;

    [Fact]
    public async Task TheTagHelperPrintsTheTagInTheHead()
    {
        var body = await Client().GetStringAsync("/");

        Assert.Contains(Snippet, body);
        Assert.True(body.IndexOf(Snippet, StringComparison.Ordinal) < body.IndexOf("</head>", StringComparison.Ordinal));
        Assert.DoesNotContain("<privatus-analytics", body);
    }

    [Fact]
    public async Task TheTagHelperPrintsNothingInDevelopmentByDefault()
    {
        var body = await Client("Development").GetStringAsync("/");

        Assert.DoesNotContain("pa.js", body);
        Assert.DoesNotContain("<privatus-analytics", body);
    }

    [Fact]
    public async Task TheTagHelperPrintsNothingWithoutASiteId()
    {
        var body = await Client(configure: o => o.SiteId = " ").GetStringAsync("/");

        Assert.DoesNotContain("pa.js", body);
    }

    [Fact]
    public async Task TheTagHelperTakesOverrides()
    {
        var body = await Client().GetStringAsync("/tag-overrides");

        Assert.Contains("<script nonce=\"n1\">window.privatus=window.privatus||{q:[]};", body);
        Assert.Contains("<script nonce=\"n1\">window.privatusPageProps=function(){return {\"seats\":5,\"beta\":true,\"plan\":\"pro\"}}</script>", body);
        Assert.Contains(
            "<script defer src=\"https://privatusstats.com/js/pa.js\" data-site=\"pa_TEST123456\" data-modules=\"engage,vitals\" data-spa=\"hash\" data-manual=\"true\" data-props=\"privatusPageProps\" data-debug=\"true\" nonce=\"n1\"></script>",
            body);
    }

    [Fact]
    public async Task TheTagHelperUsesTheNonceFromHttpContextItems()
    {
        var body = await Client().GetStringAsync("/?nonce=req123");

        Assert.Contains("data-site=\"pa_TEST123456\" nonce=\"req123\"></script>", body);
    }

    [Fact]
    public async Task TheTagHelperUsesConfiguredPagePropsPerRequest()
    {
        var client = Client(configure: o => o.PageProps = context => context.Request.Query.ContainsKey("plan") ? new { plan = context.Request.Query["plan"].ToString() } : null);

        Assert.Contains("return {\"plan\":\"team\"}", await client.GetStringAsync("/?plan=team"));
        Assert.DoesNotContain("privatusPageProps", await client.GetStringAsync("/"));
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public async Task EventAttributesPrintInEveryEnvironment(string environment)
    {
        var body = await Client(environment).GetStringAsync("/");

        Assert.Contains("<button data-privatus-event=\"Export CSV\" data-privatus-prop-format=\"csv\" data-privatus-prop-source=\"toolbar\">Export</button>", body);
        Assert.DoesNotContain("privatus-prop-format=\"csv\" privatus", body);
        Assert.Contains("<a href=\"/pricing\" data-privatus-click=\"pricing.cta\">Pricing</a>", body);
    }

    [Fact]
    public async Task EventAttributesWorkOnAFormWithProps()
    {
        var body = await Client().GetStringAsync("/");

        Assert.Matches("<form method=\"post\" action=\"/signup\" data-privatus-event=\"Signup\" data-privatus-prop-plan=\"pro\" data-privatus-prop-seats=\"3\">", body);
    }

    [Fact]
    public async Task EmptyEventNamesAndInvalidClickNamesPrintNothing()
    {
        var body = await Client().GetStringAsync("/");

        Assert.Contains("<span id=\"empty\">Nothing</span>", body);
        Assert.Contains("<a id=\"bad\" href=\"/x\">Bad</a>", body);
    }

    [Fact]
    public async Task EventAttributesAreEscaped()
    {
        var body = await Client().GetStringAsync("/");

        Assert.Contains("<p id=\"escaped\" data-privatus-event=\"Say &quot;hi&quot;\" data-privatus-prop-note=\"a&lt;b\">Hi</p>", body);
    }

    [Fact]
    public async Task TheHtmlHelpersPrintTheTagAndAttributes()
    {
        var body = await Client().GetStringAsync("/html-helpers");

        Assert.Contains(
            "<script>window.privatusPageProps=function(){return {\"role\":\"admin\"}}</script>\n<script defer src=\"https://privatusstats.com/js/pa.js\" data-site=\"pa_TEST123456\" data-modules=\"engage,auto\" data-props=\"privatusPageProps\"></script>",
            body);
        Assert.Contains("<button data-privatus-event=\"Report exported\" data-privatus-prop-format=\"csv\" data-privatus-prop-rows=\"10\">Export</button>", body);
        Assert.Contains("<a href=\"/\" data-privatus-click=\"nav.home\">Home</a>", body);
        Assert.Contains("<a id=\"bad\" href=\"/\" >Bad</a>", body);
    }

    [Fact]
    public async Task TheBlazorComponentPrintsTheTagInTheHead()
    {
        var body = await Client().GetStringAsync("/blazor");

        Assert.Contains("<script>window.privatus=window.privatus||{q:[]};", body);
        Assert.Contains("<script>window.privatusPageProps=function(){return {\"plan\":\"team\"}}</script>", body);
        Assert.Contains("data-site=\"pa_TEST123456\" data-props=\"privatusPageProps\"></script>", body);
        Assert.True(body.IndexOf("/js/pa.js", StringComparison.Ordinal) < body.IndexOf("</head>", StringComparison.Ordinal));
        Assert.Contains("Blazor page", body);
    }

    [Fact]
    public async Task TheBlazorComponentPrintsOnlyTheQueueStubInDevelopment()
    {
        var body = await Client("Development").GetStringAsync("/blazor");

        Assert.DoesNotContain("pa.js", body);
        Assert.DoesNotContain("privatusPageProps", body);
        Assert.Contains(PrivatusTag.QueueStub(), body);
    }

    [Fact]
    public async Task TheMiddlewareAddsTheTagToPagesWithoutIt()
    {
        var response = await Client().GetAsync("/plain");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains(Snippet + "\n</head>", body);
        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(body), response.Content.Headers.ContentLength);
    }

    [Fact]
    public async Task TheMiddlewareDoesNotAddASecondTag()
    {
        Assert.Equal(1, Count(await Client().GetStringAsync("/"), "/js/pa.js"));
        Assert.Equal(1, Count(await Client().GetStringAsync("/blazor"), "/js/pa.js"));
    }

    [Fact]
    public async Task TheMiddlewareLeavesOtherResponsesAlone()
    {
        var client = Client();

        Assert.Equal("{\"ok\":true}", await client.GetStringAsync("/data"));
        Assert.Equal("<p>No head here</p>", await client.GetStringAsync("/bare"));
        Assert.DoesNotContain("pa.js", await (await client.GetAsync("/missing")).Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TheMiddlewareCanBeSwitchedOffFromSettings()
    {
        var body = await Client(configure: o => o.AutoInject = false).GetStringAsync("/plain");

        Assert.DoesNotContain("pa.js", body);
    }

    [Fact]
    public async Task NothingIsAddedInDevelopment()
    {
        var body = await Client("Development").GetStringAsync("/plain");

        Assert.DoesNotContain("pa.js", body);
    }

    [Fact]
    public async Task NoCookiesAreSet()
    {
        var response = await Client().GetAsync("/plain");

        Assert.False(response.Headers.Contains("Set-Cookie"));
    }
}
