# Changelog

## 1.0.0 - 2026-10-25

- Initial release.
- `services.AddPrivatusAnalytics()` with settings from the `PrivatusAnalytics` configuration section, code, or the `PRIVATUS_SITE_ID` environment variable.
- `<privatus-analytics />` tag helper, `@Html.PrivatusAnalytics()` HTML helper and `<PrivatusAnalytics />` Blazor component.
- `privatus-event`, `privatus-prop-*` and `privatus-click` tag helpers on any element, plus `@Html.PrivatusEvent()` and `@Html.PrivatusClick()`.
- Page properties (`PageProps`, `props`, `prop-*`) and the optional queue stub for `privatus.track`.
- Content Security Policy nonce from `HttpContext.Items["csp-nonce"]` (the key can be changed) or a setting.
- Optional middleware, `app.UsePrivatusAnalyticsAutoInject()`, that adds the tag to the head of HTML responses.
- Skips every environment except Production by default.
