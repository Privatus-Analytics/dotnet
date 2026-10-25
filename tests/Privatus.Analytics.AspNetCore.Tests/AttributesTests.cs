
namespace Privatus.Analytics.AspNetCore.Tests;

public sealed class AttributesTests
{
    [Fact]
    public void RendersEventAttributes() =>
        Assert.Equal(
            "data-privatus-event=\"Export CSV\" data-privatus-prop-format=\"csv\" data-privatus-prop-source=\"toolbar\"",
            PrivatusAttributes.Event("Export CSV", new { format = "csv", source = "toolbar" }));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AnEmptyNameRendersNothing(string? name) => Assert.Equal(string.Empty, PrivatusAttributes.Event(name, new { a = 1 }));

    [Fact]
    public void TrimsAndCutsTheName()
    {
        Assert.Equal("data-privatus-event=\"Signup\"", PrivatusAttributes.Event("  Signup  "));
        Assert.Equal("data-privatus-event=\"" + new string('x', 120) + "\"", PrivatusAttributes.Event(new string('x', 200)));
    }

    [Fact]
    public void NormalisesPropKeys()
    {
        var html = PrivatusAttributes.Event("E", new Dictionary<string, object?> { ["Plan Type"] = "pro", ["a-b.c"] = "x", [new string('k', 80)] = "long" });

        Assert.Contains("data-privatus-prop-plan_type=\"pro\"", html);
        Assert.Contains("data-privatus-prop-a_b_c=\"x\"", html);
        Assert.Contains("data-privatus-prop-" + new string('k', 64) + "=\"long\"", html);
    }

    [Fact]
    public void TurnsValuesIntoText()
    {
        var html = PrivatusAttributes.Event("E", new Dictionary<string, object?>
        {
            ["yes"] = true,
            ["no"] = false,
            ["count"] = 42,
            ["price"] = 9.5m,
            ["tags"] = new[] { "a", "b" },
            ["gone"] = null,
        });

        Assert.Equal(
            "data-privatus-event=\"E\" data-privatus-prop-yes=\"true\" data-privatus-prop-no=\"false\" data-privatus-prop-count=\"42\" data-privatus-prop-price=\"9.5\" data-privatus-prop-tags=\"a,b\"",
            html);
    }

    [Fact]
    public void CutsValuesTo500Characters() =>
        Assert.Contains("=\"" + new string('v', 500) + "\"", PrivatusAttributes.Event("E", new { v = new string('v', 600) }));

    [Fact]
    public void KeepsAtMostThirtyProps()
    {
        var props = Enumerable.Range(1, 40).ToDictionary(i => "p" + i, i => (object?)i);

        var html = PrivatusAttributes.Event("E", props);

        Assert.Equal(30, html.Split("data-privatus-prop-").Length - 1);
        Assert.Contains("data-privatus-prop-p30=", html);
        Assert.DoesNotContain("data-privatus-prop-p31=", html);
    }

    [Fact]
    public void EscapesNamesAndValues() =>
        Assert.Equal(
            "data-privatus-event=\"Say &quot;hi&quot;\" data-privatus-prop-note=\"a&lt;b &amp; &#39;c&#39;\"",
            PrivatusAttributes.Event("Say \"hi\"", new { note = "a<b & 'c'" }));

    [Fact]
    public void RendersAClickAttribute() =>
        Assert.Equal("data-privatus-click=\"pricing.cta\"", PrivatusAttributes.Click("pricing.cta"));

    [Theory]
    [InlineData("nav:home_1-a")]
    [InlineData("a")]
    public void AcceptsValidClickNames(string name) => Assert.True(PrivatusAttributes.IsValidClickName(name));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("quote\"")]
    [InlineData("line\n")]
    [InlineData("caf\u00e9")]
    public void RejectsInvalidClickNames(string? name) => Assert.Equal(string.Empty, PrivatusAttributes.Click(name));

    [Fact]
    public void RejectsClickNamesOver100Characters()
    {
        Assert.NotEmpty(PrivatusAttributes.Click(new string('a', 100)));
        Assert.Empty(PrivatusAttributes.Click(new string('a', 101)));
    }
}
