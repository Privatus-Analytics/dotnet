using System.Text;

namespace Privatus.Analytics;

/// <summary>Small text helpers shared by every renderer.</summary>
internal static class Text
{
    /// <summary>Escapes &amp; &lt; &gt; " and ' for an HTML attribute value.</summary>
    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.AsSpan().IndexOfAny("&<>\"'") < 0)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 16);
        foreach (var c in value)
        {
            switch (c)
            {
                case '&': builder.Append("&amp;"); break;
                case '<': builder.Append("&lt;"); break;
                case '>': builder.Append("&gt;"); break;
                case '"': builder.Append("&quot;"); break;
                case '\'': builder.Append("&#39;"); break;
                default: builder.Append(c); break;
            }
        }

        return builder.ToString();
    }

    /// <summary>Cuts a string to at most <paramref name="max"/> characters, never splitting a surrogate pair.</summary>
    public static string Truncate(string value, int max)
    {
        if (value.Length <= max)
        {
            return value;
        }

        var index = 0;
        var count = 0;
        while (index < value.Length && count < max)
        {
            index += char.IsSurrogatePair(value, index) ? 2 : 1;
            count++;
        }

        return value[..index];
    }

    /// <summary>Turns a list setting into clean values. null stays null. Items may hold comma lists.</summary>
    public static List<string>? List(IEnumerable<string?>? values)
    {
        if (values is null)
        {
            return null;
        }

        return values
            .Where(item => item is not null)
            .SelectMany(item => item!.Split(','))
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .ToList();
    }
}
