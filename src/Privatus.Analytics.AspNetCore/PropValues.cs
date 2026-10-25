using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Privatus.Analytics;

/// <summary>Normalises event and page properties the same way the tracker does.</summary>
internal static class PropValues
{
    public const int MaxProps = 30;
    public const int MaxKeyLength = 64;
    public const int MaxValueLength = 500;
    public const int MaxListItems = 30;

    /// <summary>Lowercases a key, turns anything but a-z 0-9 _ into _, and cuts it to 64 characters.</summary>
    public static string? NormalizeKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var lower = key.Trim().ToLowerInvariant();
        var builder = new StringBuilder(lower.Length);
        foreach (var c in lower)
        {
            builder.Append(c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' ? c : '_');
        }

        var result = builder.ToString();
        return result.Length > MaxKeyLength ? result[..MaxKeyLength] : result;
    }

    /// <summary>Reads a dictionary, a list of pairs or the public properties of an object.</summary>
    public static IEnumerable<KeyValuePair<string, object?>> Read(object? props)
    {
        switch (props)
        {
            case null:
            case string:
                yield break;
            case IEnumerable<KeyValuePair<string, object?>> pairs:
                foreach (var pair in pairs)
                {
                    yield return pair;
                }

                yield break;
            case IEnumerable<KeyValuePair<string, string?>> stringPairs:
                foreach (var pair in stringPairs)
                {
                    yield return new KeyValuePair<string, object?>(pair.Key, pair.Value);
                }

                yield break;
            case IDictionary dictionary:
                foreach (DictionaryEntry entry in dictionary)
                {
                    yield return new KeyValuePair<string, object?>(Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty, entry.Value);
                }

                yield break;
        }

        foreach (var property in props.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.CanRead && property.GetIndexParameters().Length == 0)
            {
                yield return new KeyValuePair<string, object?>(property.Name, property.GetValue(props));
            }
        }
    }

    /// <summary>Merges sources in order (later keys win), keeping at most 30 keys.</summary>
    public static List<KeyValuePair<string, T>> Collect<T>(IEnumerable<object?> sources, Func<object, T?> convert)
        where T : class
    {
        var result = new List<KeyValuePair<string, T>>();
        var positions = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var source in sources)
        {
            foreach (var (rawKey, rawValue) in Read(source))
            {
                var key = NormalizeKey(rawKey);
                if (key is null || rawValue is null)
                {
                    continue;
                }

                var value = convert(rawValue);
                if (value is null)
                {
                    continue;
                }

                if (positions.TryGetValue(key, out var position))
                {
                    result[position] = new KeyValuePair<string, T>(key, value);
                }
                else if (result.Count < MaxProps)
                {
                    positions[key] = result.Count;
                    result.Add(new KeyValuePair<string, T>(key, value));
                }
            }
        }

        return result;
    }

    /// <summary>A value as attribute text: booleans true/false, numbers as text, lists joined with commas.</summary>
    public static string? ToAttributeText(object value)
    {
        var text = value switch
        {
            string s => s,
            IEnumerable list => string.Join(",", list.Cast<object?>().Where(item => item is not null).Select(item => Scalar(item!))),
            _ => Scalar(value),
        };
        return Text.Truncate(text, MaxValueLength);
    }

    /// <summary>A value as a JSON fragment: numbers and booleans stay as they are, lists become arrays.</summary>
    public static string? ToJson(object value)
    {
        if (value is string text)
        {
            return JsonString(Text.Truncate(text, MaxValueLength));
        }

        if (value is IEnumerable list)
        {
            var items = list.Cast<object?>()
                .Where(item => item is not null)
                .Select(item => item is IEnumerable and not string ? JsonString(Text.Truncate(ToAttributeText(item!) ?? string.Empty, MaxValueLength)) : JsonScalar(item!))
                .Where(item => item is not null)
                .Take(MaxListItems);
            return "[" + string.Join(",", items) + "]";
        }

        return JsonScalar(value);
    }

    /// <summary>Escapes a string for JSON that is safe inside a script element.</summary>
    public static string JsonString(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                case '\b': builder.Append("\\b"); break;
                case '\f': builder.Append("\\f"); break;
                case '<': builder.Append("\\u003c"); break;
                case '>': builder.Append("\\u003e"); break;
                case '&': builder.Append("\\u0026"); break;
                case '\u2028': builder.Append("\\u2028"); break;
                case '\u2029': builder.Append("\\u2029"); break;
                default:
                    if (c < 0x20)
                    {
                        builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(c);
                    }

                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }

    private static string? JsonScalar(object value) => value switch
    {
        bool b => b ? "true" : "false",
        float f => float.IsFinite(f) ? f.ToString("R", CultureInfo.InvariantCulture) : null,
        double d => double.IsFinite(d) ? d.ToString("R", CultureInfo.InvariantCulture) : null,
        decimal m => m.ToString(CultureInfo.InvariantCulture),
        sbyte or byte or short or ushort or int or uint or long or ulong => Convert.ToString(value, CultureInfo.InvariantCulture),
        _ => JsonString(Text.Truncate(Scalar(value), MaxValueLength)),
    };

    private static string Scalar(object value) => value switch
    {
        string s => s,
        bool b => b ? "true" : "false",
        DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };
}
