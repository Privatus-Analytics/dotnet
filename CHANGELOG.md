# Changelog

## 1.0.1 - 2026-10-26

- README: a new intro that starts with the problem Privatus Analytics solves.

## 1.0.0 - 2026-10-25

- Initial release.
- `services.AddPrivatusAnalytics()` with settings from the `PrivatusAnalytics` configuration section, code, or the `PRIVATUS_SITE_ID` environment variable.
- `<privatus-analytics />` tag helper, `@Html.PrivatusAnalytics()` HTML helper and `<PrivatusAnalytics />` Blazor component.
- `privatus-event`, `privatus-prop-*` and `privatus-click` tag helpers on any element, plus `@Html.PrivatusEvent()` and `@Html.PrivatusClick()`.
- Page properties (`PageProps`, `props`, `prop-*`) and the optional queue stub for `privatus.track`.
- Content Security Policy nonce from `HttpContext.Items["csp-nonce"]` (the key can be changed) or a setting.
- Optional middleware, `app.UsePrivatusAnalyticsAutoInject()`, that adds the tag to the head of HTML responses.
- Skips every environment except Production by default.
- Changed: with `QueueStub` on, a tag that is off (Development, `"Enabled": false` or no site id) now prints
  the queue stub and nothing else, so `privatus.track(...)` calls in your own JavaScript never fail. Before, it
  printed nothing. With `QueueStub` off it still prints nothing. The `PageProps` function is not called when
  the tag is off, and the middleware still adds nothing.
- Added `example/`, a small Razor Pages app for the conformance tests.
