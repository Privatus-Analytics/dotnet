using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Privatus.Analytics.AspNetCore.Tests;

/// <summary>The middleware on a bare TestServer pipeline.</summary>
public sealed class MiddlewareTests
{
    private const string Snippet = "<script defer src=\"https://privatusstats.com/js/pa.js\" data-site=\"pa_TEST123456\"></script>";
    private const string Page = "<html><head><title>T</title></HEAD><body>Hi</body></html>";

    private static async Task<(WebApplication App, HttpClient Client)> Start(RequestDelegate handler, Action<PrivatusAnalyticsOptions>? configure = null, string environment = "Production")
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        builder.Services.AddPrivatusAnalytics(o =>
        {
            o.SiteId = "pa_TEST123456";
            configure?.Invoke(o);
        });
        var app = builder.Build();
        app.UsePrivatusAnalyticsAutoInject();
        app.Run(handler);
        await app.StartAsync();
        return (app, app.GetTestClient());
    }

    private static RequestDelegate Html(string body, string contentType = "text/html; charset=utf-8", int status = 200) => async context =>
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = contentType;
        context.Response.ContentLength = System.Text.Encoding.UTF8.GetByteCount(body);
        await context.Response.WriteAsync(body);
    };

    [Fact]
    public async Task AddsTheTagBeforeTheHeadCloseAndFixesContentLength()
    {
        var (app, client) = await Start(Html(Page));
        await using var _ = app;

        var response = await client.GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains(Snippet + "\n</HEAD>", body);
        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(body), response.Content.Headers.ContentLength);
    }

    [Fact]
    public async Task UsesTheNonceFromHttpContextItems()
    {
        var (app, client) = await Start(async context =>
        {
            context.Items["csp-nonce"] = "abc123";
            await Html(Page)(context);
        });
        await using var _ = app;

        Assert.Contains("nonce=\"abc123\"></script>\n</HEAD>", await client.GetStringAsync("/"));
    }

    [Fact]
    public async Task DoesNothingWhenAutoInjectIsOff()
    {
        var (app, client) = await Start(Html(Page), o => o.AutoInject = false);
        await using var _ = app;

        Assert.Equal(Page, await client.GetStringAsync("/"));
    }

    [Fact]
    public async Task DoesNothingOutsideProduction()
    {
        var (app, client) = await Start(Html(Page), environment: "Development");
        await using var _ = app;

        Assert.Equal(Page, await client.GetStringAsync("/"));
    }

    [Fact]
    public async Task LeavesJsonAlone()
    {
        var (app, client) = await Start(Html(Page, "application/json"));
        await using var _ = app;

        Assert.Equal(Page, await client.GetStringAsync("/"));
    }

    [Fact]
    public async Task LeavesErrorPagesAlone()
    {
        var (app, client) = await Start(Html(Page, status: 404));
        await using var _ = app;

        var response = await client.GetAsync("/");

        Assert.Equal(Page, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task LeavesCompressedResponsesAlone()
    {
        var (app, client) = await Start(async context =>
        {
            context.Response.Headers.ContentEncoding = "identity";
            await Html(Page)(context);
        });
        await using var _ = app;

        Assert.Equal(Page, await client.GetStringAsync("/"));
    }

    [Fact]
    public async Task LeavesAjaxRequestsAlone()
    {
        var (app, client) = await Start(Html(Page));
        await using var _ = app;
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");

        var response = await client.SendAsync(request);

        Assert.Equal(Page, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task LeavesPagesThatAlreadyHaveTheTagAlone()
    {
        var page = "<html><head>" + Snippet + "</head></html>";
        var (app, client) = await Start(Html(page));
        await using var _ = app;

        Assert.Equal(page, await client.GetStringAsync("/"));
    }

    [Fact]
    public async Task LeavesHtmlWithoutAHeadAlone()
    {
        var (app, client) = await Start(Html("<p>No head here</p>"));
        await using var _ = app;

        Assert.Equal("<p>No head here</p>", await client.GetStringAsync("/"));
    }

    [Fact]
    public async Task LeavesOtherCharsetsAlone()
    {
        var (app, client) = await Start(Html(Page, "text/html; charset=iso-8859-1"));
        await using var _ = app;

        Assert.Equal(Page, await client.GetStringAsync("/"));
    }

    [Fact]
    public async Task HandlesPagesWrittenInManyPieces()
    {
        var (app, client) = await Start(async context =>
        {
            context.Response.ContentType = "text/html";
            foreach (var piece in new[] { "<html><he", "ad></he", "ad><body>", new string('x', 100_000), "</body></html>" })
            {
                await context.Response.WriteAsync(piece);
                await context.Response.Body.FlushAsync();
            }
        });
        await using var _ = app;

        var response = await client.GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        Assert.StartsWith("<html><head>" + Snippet + "\n</head>", body);
        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(body), response.Content.Headers.ContentLength);
    }

    [Fact]
    public async Task PassesOtherResponsesStraightThrough()
    {
        var bytes = Enumerable.Range(0, 1000).Select(i => (byte)i).ToArray();
        var (app, client) = await Start(async context =>
        {
            context.Response.ContentType = "application/octet-stream";
            await context.Response.Body.WriteAsync(bytes);
        });
        await using var _ = app;

        var response = await client.GetAsync("/");

        Assert.Equal(bytes, await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(new MediaTypeHeaderValue("application/octet-stream"), response.Content.Headers.ContentType);
    }

    [Fact]
    public void ThrowsAClearErrorWithoutAddPrivatusAnalytics()
    {
        var app = WebApplication.CreateBuilder().Build();

        var error = Assert.Throws<InvalidOperationException>(() => app.UsePrivatusAnalyticsAutoInject());
        Assert.Contains("AddPrivatusAnalytics", error.Message);
    }
}
