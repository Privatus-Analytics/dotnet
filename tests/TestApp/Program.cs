using Privatus.Analytics;
using TestApp.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddRazorComponents();
builder.Services.AddPrivatusAnalytics();

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.Request.Query.ContainsKey("nonce"))
    {
        context.Items["csp-nonce"] = context.Request.Query["nonce"].ToString();
    }

    await next(context);
});
app.UsePrivatusAnalyticsAutoInject();
app.UseAntiforgery();

app.MapControllers();
app.MapGet("/plain", () => Results.Content("<html><head><title>Plain</title></head><body>Hi</body></html>", "text/html"));
app.MapGet("/data", () => Results.Json(new { ok = true }));
app.MapGet("/bare", () => Results.Content("<p>No head here</p>", "text/html"));
app.MapGet("/missing", () => Results.Content("<html><head></head><body>Gone</body></html>", "text/html", statusCode: 404));
app.MapRazorComponents<App>();

app.Run();

/// <summary>Entry point, public for WebApplicationFactory.</summary>
public partial class Program
{
}
