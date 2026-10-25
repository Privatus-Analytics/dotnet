// Conformance example app for Privatus.Analytics.AspNetCore (Razor Pages).
//
// Built by the conformance suite (https://github.com/Privatus-Analytics/conformance).
// See its CONTRACT.md for what each route must render. The site id and script
// host come from PRIVATUS_SITE_ID and PRIVATUS_SCRIPT_HOST.
using Privatus.Analytics;

const string Nonce = "testnonce";
const string AutoPage = """
    <!doctype html>
    <html lang="en">
    <head>
    <meta charset="utf-8">
    <title>Auto inject</title>
    </head>
    <body><h1>Auto inject</h1><p>No tag in the template. The middleware adds it.</p></body>
    </html>
    """;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
// Forced on, so the tag prints whatever the environment.
builder.Services.AddPrivatusAnalytics(options => options.Enabled = true);

var app = builder.Build();
var scriptHost = (Environment.GetEnvironmentVariable("PRIVATUS_SCRIPT_HOST") ?? PrivatusAnalyticsOptions.DefaultScriptHost).TrimEnd('/');

// The nonce goes in HttpContext.Items["csp-nonce"], where the package looks for it.
// A real app makes a new random nonce for each request.
app.Use(async (context, next) =>
{
    context.Items["csp-nonce"] = Nonce;
    context.Response.Headers.ContentSecurityPolicy = $"script-src 'nonce-{Nonce}' {scriptHost}";
    await next(context);
});

// Auto inject on /auto only, so the other routes test the tag helper alone.
app.UseWhen(context => context.Request.Path == "/auto", branch => branch.UsePrivatusAnalyticsAutoInject());
app.MapGet("/auto", () => Results.Content(AutoPage, "text/html; charset=utf-8"));

app.MapRazorPages();
app.Run();
