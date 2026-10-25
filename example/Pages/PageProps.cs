namespace Example.Pages;

/// <summary>The shared values from the conformance CONTRACT.md.</summary>
public static class PageProps
{
    public const string Nasty = "</script><script>window.__pwned=1</script>\u2028\u2029&<>\"'";

    public static Dictionary<string, object?> Default() => new()
    {
        ["plan"] = "pro",
        ["role"] = "admin",
        ["beta"] = true,
        ["seats"] = 3,
    };
}
