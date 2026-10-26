# Privatus Analytics for ASP.NET Core

**Simple, private website analytics for your ASP.NET Core app.** No cookies,
honest numbers, and a dashboard you understand in seconds.

## Why Privatus Analytics

**Google Analytics is slow, confusing and hungry for data.** It sends what your
visitors do to Google. In many countries it means showing a cookie pop-up (the
box that asks visitors to accept cookies), and many people click "reject", so
you never see them. Bots pad the numbers that are left.

**Privatus Analytics fixes that.** It counts visits to your ASP.NET Core app
without cookies and never stores IP addresses. Bots are filtered out before they
are counted. Your data is never sold or shared.

What you get:

- **A dashboard you understand in seconds.** Visitors, top pages and where
  people came from, all on one screen.
- **No cookie pop-up needed for our analytics.** We set no cookies and store no
  personal data. Laws differ by country, so check your own legal advice.
- **Honest numbers.** Known bots and fake traffic are left out on every plan.
- **A fast app.** The tracking script is about 3 KB, tiny next to most tracking
  tags.
- **Data you own.** We never sell it or show you ads, and you can pull it out
  any time through the API.

**Start free.** The Free plan has 10,000 events a month and 3 sites, with no
card. Every new account also gets 14 days of Pro free, with no card. [Create
your account](https://privatusanalytics.com).

## What this package does

- Adds the Privatus Analytics tracking script with a tag helper, an HTML helper,
  a Blazor component, or an optional middleware that puts it in the `<head>` for
  you.
- Only prints the browser script and some HTML attributes. It never sends events
  from your server and sets no cookies.
- Never reads IP addresses, browser details or any visitor id.

You need a Privatus Analytics account to see reports, and there is a free plan.
Sign up at https://privatusanalytics.com.

## Requirements

* .NET 8 or .NET 9.
* ASP.NET Core: MVC, Razor Pages or Blazor.
* A Privatus Analytics site id (it starts with `pa_`). You find it under
  Site settings, Tracking.

## Install

```sh
dotnet add package Privatus.Analytics.AspNetCore
```

For the full setup guide and every tracker option, see the [Privatus Analytics
docs](https://privatusanalytics.com/docs).

Register the package in `Program.cs`:

```csharp
using Privatus.Analytics;

builder.Services.AddPrivatusAnalytics();
```

Set your site id with an environment variable:

```sh
PRIVATUS_SITE_ID=pa_YOURSITEID
```

or in `appsettings.json` (or `appsettings.Production.json`):

```json
{
  "PrivatusAnalytics": {
    "SiteId": "pa_YOURSITEID"
  }
}
```

## Add the tag

### Razor views and pages (tag helper)

Add the tag helpers once, in `Views/_ViewImports.cshtml` or
`Pages/_ViewImports.cshtml`:

```cshtml
@addTagHelper *, Privatus.Analytics.AspNetCore
```

Then put the tag in the `<head>` of your layout, for example
`Views/Shared/_Layout.cshtml`:

```cshtml
<head>
  <title>My app</title>
  <privatus-analytics />
</head>
```

In production it prints:

```html
<script defer src="https://privatusstats.com/js/pa.js" data-site="pa_YOURSITEID"></script>
```

The tag prints nothing when there is no site id, and nothing outside the
Production environment (see below).

### Razor without tag helpers (HTML helper)

```cshtml
@using Privatus.Analytics

<head>
  @Html.PrivatusAnalytics()
</head>
```

### Blazor

Add `@using Privatus.Analytics` to `Components/_Imports.razor`, then put the
component in the `<head>` of `Components/App.razor`:

```razor
<head>
    <PrivatusAnalytics />
    <HeadOutlet />
</head>
```

`App.razor` is rendered on the server (static server rendering), so the nonce
and page properties from the request work there.

### Or let the package add it

If you would rather not touch your layouts, add the middleware in
`Program.cs`:

```csharp
var app = builder.Build();
app.UsePrivatusAnalyticsAutoInject();
```

It adds the tag just before `</head>` in UTF-8 `text/html` responses with
status 200. It skips Ajax requests (requests sent by your own page's
JavaScript with the `X-Requested-With` header), pages without a `<head>`, and
pages that already have the tag. Responses that have a `Content-Encoding`
header are left alone. If you use `UseResponseCompression`, call it before
`UsePrivatusAnalyticsAutoInject`, so the tag is added before the page is
compressed. You can switch the middleware off from settings with
`"AutoInject": false`.

To add it to some paths only, put it in a branch:

```csharp
app.UseWhen(context => context.Request.Path.StartsWithSegments("/blog"),
    branch => branch.UsePrivatusAnalyticsAutoInject());
```

## Configuration

Every setting is optional except the site id. You can set them in
`appsettings.json`:

```json
{
  "PrivatusAnalytics": {
    "SiteId": "pa_YOURSITEID",
    "ScriptHost": "https://privatusstats.com",
    "Modules": [ "engage", "auto", "vitals", "clicks", "ask" ],
    "ExcludedPaths": [ "/admin/*", "/preview/*" ],
    "Domains": [ "example.com", "www.example.com" ],
    "Spa": "auto",
    "Manual": false,
    "EnabledEnvironments": [ "Production" ],
    "AutoInject": true,
    "QueueStub": false,
    "NonceItemKey": "csp-nonce",
    "ExtraAttributes": { "data-mask": "/orders/*->/orders/:id" }
  }
}
```

or in code. Code runs after `appsettings.json`, so it wins:

```csharp
builder.Services.AddPrivatusAnalytics(options =>
{
    options.SiteId = "pa_YOURSITEID";
    options.Modules = ["engage", "auto"];
    options.ExcludedPaths = ["/admin/*"];
    options.PageProps = context => new { plan = context.User.FindFirst("plan")?.Value };
});
```

To read a different section, pass it in:
`builder.Services.AddPrivatusAnalytics(builder.Configuration.GetSection("Analytics"))`.

| Setting | Default | What it does |
|---|---|---|
| `SiteId` | `PRIVATUS_SITE_ID` | The `data-site` attribute. Nothing prints without it |
| `ScriptHost` | `https://privatusstats.com` | Where `/js/pa.js` is served. Also read from `PRIVATUS_SCRIPT_HOST`. Change it only if you host your own copy |
| `Modules` | tracker default (`engage`) | The `data-modules` attribute. Setting it replaces the default, so keep `engage` for time on page and scroll depth. An empty list in code (`[]`) loads none |
| `ExcludedPaths` | none | The `data-exclude` attribute. Path patterns the tracker never counts |
| `Domains` | all hosts | The `data-domains` attribute. Count only these hostnames |
| `Spa` | `auto` | The `data-spa` attribute for single-page apps: `auto`, `history`, `hash` or `off` |
| `Manual` | `false` | The `data-manual` attribute. `true` turns off the automatic pageview |
| `EnabledEnvironments` | `["Production"]` | Host environments that get the tag |
| `Enabled` | `null` | `true` or `false` forces the tag on or off in every environment |
| `AutoInject` | `true` | Lets the middleware work. It only runs if you call `UsePrivatusAnalyticsAutoInject()` |
| `ExtraAttributes` | `{}` | Any other `data-*` attribute |
| `PageProps` | `null` | Code only. A function that gets the request and returns page properties (see below) |
| `QueueStub` | `false` | Prints a small script before the tag so `privatus.track` works before the tracker loads. It still prints when the tag is off, so those calls never fail |
| `Nonce` | `null` | A fixed nonce for the Content Security Policy |
| `NonceItemKey` | `csp-nonce` | The `HttpContext.Items` key that holds the nonce for each request |

Each tracker attribute is explained in the [script attributes
reference](https://privatusanalytics.com/docs/tracker/attributes), and the
modules in [Modules](https://privatusanalytics.com/docs/tracker/modules).

You can also override a setting for one page:

```cshtml
<privatus-analytics modules="engage,auto" excluded-paths="/preview/*" spa="hash" data-debug="true" />
```

The tag helper takes `site-id`, `script-host`, `modules`, `excluded-paths`,
`domains`, `spa`, `manual`, `enabled`, `queue-stub`, `nonce`, `props` and
`prop-*`. Lists are comma separated. Any `data-*` attribute goes onto the
script tag as it is. The HTML helper and the Blazor component take the same
overrides:

```cshtml
@Html.PrivatusAnalytics(new PrivatusTagOverrides { Modules = new[] { "engage", "auto" } })
```

```razor
<PrivatusAnalytics Modules="@(new[] { "engage", "auto" })" Spa="hash" />
```

### Content Security Policy nonce

A Content Security Policy (CSP) is a header that tells the browser which
scripts may run. If yours uses a nonce (a random value that changes on each
request), put it in `HttpContext.Items["csp-nonce"]` in your own middleware.
The tag, the queue stub and the page properties script all get it:

```csharp
app.Use(async (context, next) =>
{
    var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
    context.Items["csp-nonce"] = nonce;
    context.Response.Headers.ContentSecurityPolicy = $"script-src 'nonce-{nonce}'";
    await next(context);
});
```

If your nonce library uses another key, set `NonceItemKey`. You can also pass
`nonce="..."` to one tag.

### Development and test

Development, test and staging requests are not counted by default, so your own
clicking around never reaches your reports. To see the tag while you build, set
`"Enabled": true` in `appsettings.Development.json`. The tracker also ignores
`localhost` unless you pass `data-allow-local` or `data-debug` (see [Debug
mode](https://privatusanalytics.com/docs/tracker/debug)):

```json
{
  "PrivatusAnalytics": {
    "Enabled": true,
    "ExtraAttributes": { "data-debug": "true" }
  }
}
```

### Leaving people out

Use `data-exclude` through `ExcludedPaths` for admin areas. To leave out
signed-in staff, print the tag only for everyone else:

```cshtml
@if (!User.IsInRole("Admin"))
{
    <privatus-analytics />
}
```

## Product analytics: see which features people use

Pageviews tell you where people go. Events tell you what they do: which
buttons they press, which reports they export, which forms they send.

### Events on buttons, links and forms

Add `privatus-event` to any element. Add details with `privatus-prop-*`:

```cshtml
<button privatus-event="Report exported" privatus-prop-format="csv" privatus-prop-source="toolbar">
  Export CSV
</button>
```

It prints the attributes the tracker reads:

```html
<button data-privatus-event="Report exported" data-privatus-prop-format="csv" data-privatus-prop-source="toolbar">
```

The event is sent when someone clicks the element. On a `<form>`, it is sent
when the form is submitted:

```cshtml
<form method="post" privatus-event="Signup" privatus-props='new { plan = "pro", seats = 3 }'>
```

`privatus-props` takes a dictionary or an anonymous object. Without tag
helpers, use the HTML helper inside the tag:

```cshtml
<button @Html.PrivatusEvent("Report exported", new { format = "csv" })>Export CSV</button>
```

Use the tag helper form on `<form>` elements if you also use the built-in MVC
tag helpers, because Razor does not allow `@...` inside a `<form>` tag then.
In other code, `PrivatusAttributes.Event("Report exported", new { format = "csv" })`
returns the same text.

The rules, so you know what reaches your reports:

* The event name is trimmed and cut to 120 characters. An empty name prints
  nothing.
* Property names are made lowercase, and anything that is not a letter, a
  digit or `_` becomes `_` (so `Plan Type` becomes `plan_type`). They are cut
  to 64 characters. Up to 30 properties are kept.
* Values become text: `true` or `false`, numbers as they are, lists joined
  with commas. Empty (`null`) values are left out. Values are cut to 500
  characters.
* These attributes print in every environment, even when the tag is off, so
  your development and production pages are the same.

### Click maps

Click maps show where people click. Give important elements a stable name:

```cshtml
<a href="/pricing" privatus-click="pricing.cta">See pricing</a>
<a href="/pricing" @Html.PrivatusClick("pricing.cta")>See pricing</a>
```

Both print `data-privatus-click="pricing.cta"`. A name may use letters,
digits, `_`, `.`, `:` and `-`, and be up to 100 characters. Any other name
prints nothing, and in Development a warning is written to the log.

### Page properties: plan, role and more

Page properties are sent with every pageview and event on the page. Use them
to compare groups of people, for example free and paid plans:

```csharp
builder.Services.AddPrivatusAnalytics(options =>
{
    options.PageProps = context => new
    {
        plan = context.User.FindFirst("plan")?.Value,
        role = context.User.IsInRole("Admin") ? "admin" : "member",
        app_version = "4.2.0",
    };
});
```

The function runs for each request. You can also pass properties to one tag:
`<privatus-analytics prop-plan="pro" />` or
`<privatus-analytics props='new { plan = "pro", beta = true }' />`. The
package prints a small script before the tag and adds
`data-props="privatusPageProps"`:

```html
<script>window.privatusPageProps=function(){return {"plan":"pro","role":"admin"}}</script>
<script defer src="https://privatusstats.com/js/pa.js" data-site="pa_YOURSITEID" data-props="privatusPageProps"></script>
```

The same name rules apply. Numbers and `true` or `false` stay as they are,
and lists stay lists (up to 30 items).

**Never put names, email addresses, user ids or anything else that points to
one person in properties.** Privatus Analytics counts visitors without any id. Put
groups (plan, role, team size) in properties, not people. The server also
removes email addresses and long ids it finds.

### Track from your own JavaScript

For things that are not a click or a form, call the tracker from your
JavaScript, as described in the [JavaScript API
docs](https://privatusanalytics.com/docs/tracker/javascript-api):

```js
privatus.track("Feature used", { feature: "export" });
```

The tracker loads with `defer`, so it may not be ready when your code runs.
Turn on the queue stub (`"QueueStub": true`, or `queue-stub="true"` on the
tag). It prints this small script before the tag, and the tracker sends the
queued calls when it loads:

```html
<script>window.privatus=window.privatus||{q:[]};['track','pageview','props','optOut','optIn','ready'].forEach(function(m){window.privatus[m]=window.privatus[m]||function(){window.privatus.q.push([m,Array.prototype.slice.call(arguments)])}})</script>
```

The queue stub prints even where the tag is off (in Development, with
`"Enabled": false`, or with no site id). Then only the stub prints: no
tracker and no page properties. So you can always call `privatus.track(...)`.
In Development the calls just wait in the queue and are never sent. The
middleware adds nothing when the tag is off.

### Naming events

Pick one style and keep it. We suggest "Noun verb" in the past tense, like
`Report exported`, `Invite sent` or `Plan upgraded`. Put details in
properties instead of the name: `Report exported` with `format: csv`, not
`CSV report exported`. Short, stable names are easier to find and compare.

Then use your events in [funnels](https://privatusanalytics.com/docs/features/funnels),
[click maps](https://privatusanalytics.com/docs/features/click-maps) and
[goals](https://privatusanalytics.com/docs/features/goals). See
[Events](https://privatusanalytics.com/docs/events) for how events are counted.

## Privacy

The script sets no cookies and stores nothing in the browser. This package
does not send anything from your server. See the [privacy
documentation](https://privatusanalytics.com/docs/privacy) for what is
collected.

If you have a privacy policy, it should say which tools handle your visitors' data. You can paste this:

> This website uses [Privatus Analytics](https://privatusanalytics.com/privacy-analytics) to count visits. It
> does not use cookies and does not follow you from one website to another. Your IP address is used for a moment
> to count visits and is never stored.

Change it to fit your site. The [privacy policy generator](https://privatusanalytics.com/tools/privacy-policy-generator)
makes a longer version with your site's name in it.

## Development

The checks run in Docker so your local .NET does not matter:

```sh
./run-docker.sh
```

It runs `dotnet format --verify-no-changes`, the xUnit tests on .NET 8 and
.NET 9 (including a small test app with Razor views and a Blazor page, run
with `WebApplicationFactory`), and `dotnet pack`. It uses the `linux/amd64`
image by default, because the arm64 .NET 9 tools crash at random on newer
Apple chips. Set `DOCKER_PLATFORM=linux/arm64` to run natively.

## Contributing

Bug reports and pull requests are welcome at
https://github.com/Privatus-Analytics/dotnet.

## License

MIT. See [LICENSE](LICENSE). Privatus Analytics is a product of Two Phase LLC,
Wyoming, USA.
