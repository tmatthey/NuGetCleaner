using Xunit;

namespace NuGetCleaner.Tests;

public class ProgramTests
{
    [Fact]
    public void InvalidRetentionLevelIsRejectedBeforeCleanup()
    {
        var originalOutput = Console.Out;
        using var output = new StringWriter();

        try
        {
            Console.SetOut(output);

            var exitCode = Program.Execute(new Options
            {
                Days = 30,
                DryRun = true,
                KeepNewest = "3"
            });

            Assert.Equal(1, exitCode);
        }
        finally
        {
            Console.SetOut(originalOutput);
        }

        Assert.Contains("build, minor, or major", output.ToString());
    }

    [Fact]
    public void DryRunDoesNotPrintProtectedVersion()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "example.package", "1.0.0"));
        Directory.CreateDirectory(Path.Combine(root, "example.package", "2.0.0"));

        var originalOutput = Console.Out;
        using var output = new StringWriter();

        try
        {
            Console.SetOut(output);

            Program.SearchAndPrint(root, 0, RetentionLevel.Major);
        }
        finally
        {
            Console.SetOut(originalOutput);
            Directory.Delete(root, recursive: true);
        }

        Assert.Contains(Path.Combine("example.package", "1.0.0"), output.ToString());
        Assert.DoesNotContain(Path.Combine("example.package", "2.0.0"), output.ToString());
    }

    [Fact]
    public void CleanupDoesNotDeleteProtectedVersion()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var oldVersion = Directory.CreateDirectory(
            Path.Combine(root, "example.package", "1.0.0")).FullName;
        var newestVersion = Directory.CreateDirectory(
            Path.Combine(root, "example.package", "2.0.0")).FullName;

        try
        {
            Program.SearchAndDestroy(root, 0, RetentionLevel.Major);

            Assert.False(Directory.Exists(oldVersion));
            Assert.True(Directory.Exists(newestVersion));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void CleanupPrintsTotalDeletedSize()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var oldVersion = Directory.CreateDirectory(
            Path.Combine(root, "example.package", "1.0.0")).FullName;
        Directory.CreateDirectory(Path.Combine(root, "example.package", "2.0.0"));

        using (var packageFile = File.Create(Path.Combine(oldVersion, "package.bin")))
        {
            packageFile.SetLength(1_572_864);
        }

        var originalOutput = Console.Out;
        using var output = new StringWriter();

        try
        {
            Console.SetOut(output);

            Program.SearchAndDestroy(root, 0, RetentionLevel.Major);
        }
        finally
        {
            Console.SetOut(originalOutput);

            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }

        Assert.Contains("Disk space saved: 1.50 MB", output.ToString());
    }

    [Fact]
    public void CleanupDoesNotPrintSpaceSavedWhenNothingIsDeleted()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "example.package", "1.0.0"));

        var originalOutput = Console.Out;
        using var output = new StringWriter();

        try
        {
            Console.SetOut(output);

            Program.SearchAndDestroy(root, 0, RetentionLevel.Major);
        }
        finally
        {
            Console.SetOut(originalOutput);
            Directory.Delete(root, recursive: true);
        }

        Assert.DoesNotContain("Disk space saved:", output.ToString());
    }
}
