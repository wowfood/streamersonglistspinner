using System.Reflection;

namespace ServerSpinner;

public static class BuildInfo
{
    /// <summary>
    /// Build timestamp embedded at compile time via AssemblyMetadata.
    /// Used as a cache-busting query parameter for static CSS/JS assets.
    /// During development this returns the timestamp of the last build;
    /// in published output index.html is also rewritten with the same value.
    /// </summary>
    public static readonly string CacheBuster =
        typeof(BuildInfo).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "BuildTimestamp")?.Value
        ?? typeof(BuildInfo).Assembly.GetName().Version?.ToString()
        ?? "1";
}
