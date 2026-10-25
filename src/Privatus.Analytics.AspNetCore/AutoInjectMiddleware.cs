using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Privatus.Analytics;

/// <summary>
/// Adds the tag just before &lt;/head&gt; in HTML responses. Only status 200,
/// text/html, uncompressed, non-Ajax responses that do not already have the tag.
/// </summary>
public sealed partial class PrivatusAutoInjectMiddleware
{
    private readonly RequestDelegate next;
    private readonly PrivatusAnalyticsService service;

    /// <summary>Creates the middleware. Use <c>app.UsePrivatusAnalyticsAutoInject()</c> instead.</summary>
    public PrivatusAutoInjectMiddleware(RequestDelegate next, PrivatusAnalyticsService service)
    {
        this.next = next;
        this.service = service;
    }

    /// <summary>Runs the rest of the pipeline and adds the tag to the response when it fits.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!service.Options.AutoInject || !service.ShouldRender() || IsAjax(context.Request) || HttpMethods.IsHead(context.Request.Method))
        {
            await next(context);
            return;
        }

        var original = context.Response.Body;
        var stream = new InjectStream(original, context.Response);
        context.Response.Body = stream;
        try
        {
            await next(context);
            if (stream.Buffer is { } buffer)
            {
                var bytes = Inject(buffer.ToArray(), context);
                context.Response.ContentLength = bytes.Length;
                await original.WriteAsync(bytes, context.RequestAborted);
            }
        }
        finally
        {
            context.Response.Body = original;
            await stream.DisposeAsync();
        }
    }

    private byte[] Inject(byte[] body, HttpContext context)
    {
        var html = Encoding.UTF8.GetString(body);
        if (html.Contains(PrivatusTag.ScriptPath, StringComparison.Ordinal))
        {
            return body;
        }

        var match = HeadClose().Match(html);
        if (!match.Success)
        {
            return body;
        }

        var tag = service.Render(context);
        if (tag.Length == 0)
        {
            return body;
        }

        return Encoding.UTF8.GetBytes(string.Concat(html.AsSpan(0, match.Index), tag, "\n", html.AsSpan(match.Index)));
    }

    private static bool IsAjax(HttpRequest request) =>
        string.Equals(request.Headers["X-Requested-With"].ToString(), "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

    internal static bool IsCandidate(HttpResponse response)
    {
        if (response.StatusCode != StatusCodes.Status200OK || response.Headers.ContainsKey(HeaderNames.ContentEncoding))
        {
            return false;
        }

        if (!MediaTypeHeaderValue.TryParse(response.ContentType, out var type) || !type.MediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Only UTF-8 (or no charset) pages are changed. Anything else is left alone.
        return !type.Charset.HasValue || type.Charset.Equals("utf-8", StringComparison.OrdinalIgnoreCase)
            || type.Charset.Equals("utf8", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex("</head>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HeadClose();

    /// <summary>
    /// Decides on the first write. HTML is buffered so the tag can be added;
    /// anything else goes straight through.
    /// </summary>
    private sealed class InjectStream : Stream
    {
        private readonly Stream inner;
        private readonly HttpResponse response;
        private bool decided;

        public InjectStream(Stream inner, HttpResponse response)
        {
            this.inner = inner;
            this.response = response;
        }

        public MemoryStream? Buffer { get; private set; }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        private Stream Target
        {
            get
            {
                if (!decided)
                {
                    decided = true;
                    if (IsCandidate(response))
                    {
                        Buffer = new MemoryStream();
                    }
                }

                return Buffer ?? inner;
            }
        }

        public override void Flush()
        {
            if (Target != Buffer)
            {
                inner.Flush();
            }
        }

        public override Task FlushAsync(CancellationToken cancellationToken) =>
            Target == Buffer ? Task.CompletedTask : inner.FlushAsync(cancellationToken);

        public override void Write(byte[] buffer, int offset, int count) => Target.Write(buffer, offset, count);

        public override void Write(ReadOnlySpan<byte> buffer) => Target.Write(buffer);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Target.WriteAsync(buffer, offset, count, cancellationToken);

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            Target.WriteAsync(buffer, cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Buffer?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

/// <summary>Adds the auto inject middleware to the pipeline.</summary>
public static class PrivatusAnalyticsApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the tag before &lt;/head&gt; in HTML responses, so you do not need to
    /// touch your layouts. Call it after <c>UseResponseCompression</c>.
    /// </summary>
    public static IApplicationBuilder UsePrivatusAnalyticsAutoInject(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (app.ApplicationServices.GetService<PrivatusAnalyticsService>() is null)
        {
            throw new InvalidOperationException("Call builder.Services.AddPrivatusAnalytics() before app.UsePrivatusAnalyticsAutoInject().");
        }

        return app.UseMiddleware<PrivatusAutoInjectMiddleware>();
    }
}
