using Microsoft.AspNetCore.Http;

namespace Privatus.Analytics.AspNetCore.Tests;

public sealed class TagTests
{
    private const string Snippet = "<script defer src=\"https://privatusstats.com/js/pa.js\" data-site=\"pa_TEST123456\"></script>";

    private static PrivatusAnalyticsOptions Options(Action<PrivatusAnalyticsOptions>? configure = null)
    {
        var options = new PrivatusAnalyticsOptions { SiteId = "pa_TEST123456", Enabled = true };
        configure?.Invoke(options);
        return options;
    }

    private static string Render(PrivatusAnalyticsOptions options, PrivatusTagOverrides? overrides = null, HttpContext? context = null, string environment = "Production") =>
        PrivatusTag.Render(options, environment, overrides, context);

    [Fact]
    public void PrintsTheBasicSnippet() => Assert.Equal(Snippet, Render(Options()));

    [Fact]
    public void PrintsNothingWithoutASiteId() => Assert.Equal(string.Empty, Render(Options(o => o.SiteId = "  ")));

    [Fact]
    public void PrintsOnlyInProductionByDefault()
    {
        var options = Options(o => o.Enabled = null);

        Assert.Equal(Snippet, Render(options, environment: "Production"));
        Assert.Equal(string.Empty, Render(options, environment: "Development"));
        Assert.Equal(string.Empty, Render(options, environment: "Staging"));
    }

    [Fact]
    public void UsesTheEnabledEnvironmentsList()
    {
        var options = Options(o =>
        {
            o.Enabled = null;
            o.EnabledEnvironments = ["Production", "staging"];
        });

        Assert.Equal(Snippet, Render(options, environment: "Staging"));
        Assert.Equal(string.Empty, Render(options, environment: "Development"));
    }

    [Fact]
    public void EnabledFalseForcesTheTagOff() => Assert.Equal(string.Empty, Render(Options(o => o.Enabled = false)));

    [Fact]
    public void EnabledTrueForcesTheTagOnInDevelopment() => Assert.Equal(Snippet, Render(Options(), environment: "Development"));

    [Fact]
    public void AddsModulesAndExclusions()
    {
        var tag = Render(Options(o =>
        {
            o.Modules = ["engage", "auto"];
            o.ExcludedPaths = ["/admin/*", "/preview/*"];
        }));

        Assert.Contains("data-modules=\"engage,auto\"", tag);
        Assert.Contains("data-exclude=\"/admin/*,/preview/*\"", tag);
    }

    [Fact]
    public void AnEmptyModulesListLoadsNone() => Assert.Contains("data-modules=\"\"", Render(Options(o => o.Modules = [])));

    [Fact]
    public void AnEmptyExclusionListPrintsNothing() => Assert.DoesNotContain("data-exclude", Render(Options(o => o.ExcludedPaths = [])));

    [Fact]
    public void AddsDomainsSpaModeAndManualMode()
    {
        var tag = Render(Options(o =>
        {
            o.Domains = ["example.com, www.example.com"];
            o.Spa = "hash";
            o.Manual = true;
        }));

        Assert.Contains("data-domains=\"example.com,www.example.com\"", tag);
        Assert.Contains("data-spa=\"hash\"", tag);
        Assert.Contains("data-manual=\"true\"", tag);
    }

    [Theory]
    [InlineData("bogus")]
    [InlineData("auto")]
    public void IgnoresAnUnknownSpaModeAndTheAutoDefault(string spa) =>
        Assert.DoesNotContain("data-spa", Render(Options(o => o.Spa = spa)));

    [Fact]
    public void EscapesAttributeValues() =>
        Assert.Contains("data-exclude=\"/a&quot;b\"", Render(Options(o => o.ExcludedPaths = ["/a\"b"])));

    [Fact]
    public void PerCallOptionsOverrideTheConfiguration()
    {
        var options = Options(o => o.Modules = ["engage"]);

        Assert.Contains("data-modules=\"vitals\"", Render(options, new PrivatusTagOverrides { Modules = ["vitals"] }));
        Assert.Contains("data-site=\"pa_OTHER\"", Render(options, new PrivatusTagOverrides { SiteId = "pa_OTHER" }));
        Assert.Equal(string.Empty, Render(options, new PrivatusTagOverrides { Enabled = false }));
    }

    [Fact]
    public void AddsExtraAttributesAndANonce()
    {
        var tag = Render(Options(o => o.ExtraAttributes["data-mask"] = "/orders/*->/orders/:id"), new PrivatusTagOverrides { Nonce = "abc123" });

        Assert.Contains("data-mask=\"/orders/*-&gt;/orders/:id\"", tag);
        Assert.EndsWith(" nonce=\"abc123\"></script>", tag);
    }

    [Fact]
    public void RemovesTheTrailingSlashFromTheScriptHost() =>
        Assert.Contains("src=\"https://cdn.example.com/js/pa.js\"", Render(Options(o => o.ScriptHost = "https://cdn.example.com/")));

    [Fact]
    public void ReadsTheNonceFromHttpContextItems()
    {
        var context = new DefaultHttpContext();
        context.Items["csp-nonce"] = "fromitems";

        Assert.Contains("nonce=\"fromitems\"", Render(Options(o => o.Nonce = "fixed"), context: context));
    }

    [Fact]
    public void UsesAConfigurableNonceKeyAndFallsBackToTheOption()
    {
        var context = new DefaultHttpContext();
        context.Items["my-nonce"] = "custom";

        Assert.Contains("nonce=\"custom\"", Render(Options(o => o.NonceItemKey = "my-nonce"), context: context));
        Assert.Contains("nonce=\"fixed\"", Render(Options(o => o.Nonce = "fixed"), context: new DefaultHttpContext()));
    }

    [Fact]
    public void PrintsTheQueueStubBeforeTheTag()
    {
        var tag = Render(Options(o => o.QueueStub = true), new PrivatusTagOverrides { Nonce = "n1" });

        Assert.StartsWith(
            "<script nonce=\"n1\">window.privatus=window.privatus||{q:[]};['track','pageview','props','optOut','optIn','ready'].forEach(function(m){window.privatus[m]=window.privatus[m]||function(){window.privatus.q.push([m,Array.prototype.slice.call(arguments)])}})</script>\n<script defer",
            tag);
    }

    [Fact]
    public void QueueStubHelperWorksAlone() =>
        Assert.StartsWith("<script>window.privatus=", PrivatusTag.QueueStub());

    [Fact]
    public void PrintsPagePropsBeforeTheTag()
    {
        var tag = Render(Options(), new PrivatusTagOverrides { Props = new { plan = "pro", role = "admin" } });

        Assert.Equal(
            "<script>window.privatusPageProps=function(){return {\"plan\":\"pro\",\"role\":\"admin\"}}</script>\n" +
            "<script defer src=\"https://privatusstats.com/js/pa.js\" data-site=\"pa_TEST123456\" data-props=\"privatusPageProps\"></script>",
            tag);
    }

    [Fact]
    public void PagePropsCarryTheNonce() =>
        Assert.StartsWith("<script nonce=\"n1\">window.privatusPageProps=", Render(Options(), new PrivatusTagOverrides { Props = new { plan = "pro" }, Nonce = "n1" }));

    [Fact]
    public void PagePropsKeepNumbersBooleansAndLists()
    {
        var tag = Render(Options(), new PrivatusTagOverrides
        {
            Props = new Dictionary<string, object?>
            {
                ["Seats"] = 5,
                ["ratio"] = 0.5,
                ["beta"] = true,
                ["flags"] = new[] { "a", "b" },
                ["gone"] = null,
                ["nan"] = double.NaN,
            },
        });

        Assert.Contains("{\"seats\":5,\"ratio\":0.5,\"beta\":true,\"flags\":[\"a\",\"b\"]}", tag);
    }

    [Fact]
    public void PagePropsJsonIsSafeInsideAScript()
    {
        var tag = Render(Options(), new PrivatusTagOverrides { Props = new { note = "</script><b>&\u2028\u2029\"" } });

        Assert.Contains("\"note\":\"\\u003c/script\\u003e\\u003cb\\u003e\\u0026\\u2028\\u2029\\\"\"", tag);
        Assert.DoesNotContain("</script><b>", tag);
    }

    [Fact]
    public void PagePropsComeFromTheConfiguredFunctionPerRequest()
    {
        var options = Options(o => o.PageProps = context => new { plan = context.Request.Query["plan"].ToString() });
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?plan=team");

        var tag = Render(options, new PrivatusTagOverrides { Props = new { role = "admin" } }, context);

        Assert.Contains("{\"plan\":\"team\",\"role\":\"admin\"}", tag);
    }

    [Fact]
    public void PerCallPropsReplaceConfiguredPropsWithTheSameKey()
    {
        var options = Options(o => o.PageProps = _ => new { plan = "free" });

        Assert.Contains("{\"plan\":\"pro\"}", Render(options, new PrivatusTagOverrides { Props = new { Plan = "pro" } }, new DefaultHttpContext()));
    }

    [Fact]
    public void PagePropsListsAreCappedAtThirtyItems()
    {
        var tag = Render(Options(), new PrivatusTagOverrides { Props = new { ids = Enumerable.Range(1, 40).ToArray() } });

        Assert.Contains("\"ids\":[" + string.Join(",", Enumerable.Range(1, 30)) + "]", tag);
    }

    [Fact]
    public void NoPropsMeansNoPropsScript() =>
        Assert.DoesNotContain("data-props", Render(Options(), new PrivatusTagOverrides { Props = new Dictionary<string, object?>() }));
}
