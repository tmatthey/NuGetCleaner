using NuGetCleaner;
using Xunit;

namespace NuGetCleaner.Tests;

public class PackageRetentionTests
{
    [Fact]
    public void NoRetentionLevelProtectsNoVersions()
    {
        var versions = VersionDirectories("1.0.0", "2.0.0");

        var protectedVersions = PackageRetention.GetProtectedVersionDirectories(versions, null);

        Assert.Empty(protectedVersions);
    }

    [Fact]
    public void MajorProtectsOnlyNewestPackageVersion()
    {
        var versions = VersionDirectories("1.9.0", "2.0.0-beta", "2.0.0");

        var protectedVersions = PackageRetention.GetProtectedVersionDirectories(
            versions,
            RetentionLevel.Major);

        Assert.Equal([VersionDirectory("2.0.0")], protectedVersions);
    }

    [Fact]
    public void MinorProtectsNewestVersionInEachMajorLine()
    {
        var versions = VersionDirectories("1.2.0", "1.5.0", "2.1.0", "2.3.0");

        var protectedVersions = PackageRetention.GetProtectedVersionDirectories(
            versions,
            RetentionLevel.Minor);

        Assert.Equal(
            [VersionDirectory("1.5.0"), VersionDirectory("2.3.0")],
            protectedVersions.Order());
    }

    [Fact]
    public void BuildProtectsNewestVersionInEachMajorMinorLine()
    {
        var versions = VersionDirectories("1.2.0", "1.2.2", "1.3.0", "2.0.0");

        var protectedVersions = PackageRetention.GetProtectedVersionDirectories(
            versions,
            RetentionLevel.Build);

        Assert.Equal(
            [VersionDirectory("1.2.2"), VersionDirectory("1.3.0"), VersionDirectory("2.0.0")],
            protectedVersions.Order());
    }

    private static string[] VersionDirectories(params string[] versions) =>
        versions.Select(VersionDirectory).ToArray();

    private static string VersionDirectory(string version) =>
        Path.Combine("packages", "example.package", version);
}
