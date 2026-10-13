using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Versioning;

namespace NuGetCleaner;

public enum RetentionLevel
{
    Build,
    Minor,
    Major
}

internal static class PackageRetention
{
    private sealed record VersionDirectory(string Path, NuGetVersion Version);

    public static HashSet<string> GetProtectedVersionDirectories(
        IEnumerable<string> versionDirectories,
        RetentionLevel? level)
    {
        if (level is null)
        {
            return [];
        }

        var versions = versionDirectories
            .Select(ParseVersionDirectory)
            .OfType<VersionDirectory>()
            .ToList();

        var protectedVersions = level.Value switch
        {
            RetentionLevel.Major => versions
                .GroupBy(_ => 0)
                .Select(Newest),
            RetentionLevel.Minor => versions
                .GroupBy(candidate => candidate.Version.Major)
                .Select(Newest),
            RetentionLevel.Build => versions
                .GroupBy(candidate => (candidate.Version.Major, candidate.Version.Minor))
                .Select(Newest),
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };

        return protectedVersions
            .Select(candidate => candidate.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static VersionDirectory ParseVersionDirectory(string path)
    {
        return NuGetVersion.TryParse(Path.GetFileName(path), out var version)
            ? new VersionDirectory(path, version)
            : null;
    }

    private static VersionDirectory Newest<TKey>(IGrouping<TKey, VersionDirectory> versions)
    {
        return versions
            .OrderByDescending(candidate => candidate.Version, VersionComparer.VersionRelease)
            .First();
    }
}
