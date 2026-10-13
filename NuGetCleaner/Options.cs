using CommandLine;

namespace NuGetCleaner;

// ReSharper disable once ClassNeverInstantiated.Global
public class Options
{
    [Option("dry-run", HelpText = "Show packages that would be deleted without actually deleting them.")]
    public bool DryRun { get; set; }

    [Option("days", Required = true, HelpText = "Specify cut off for number of days since last package access. \n" +
                                                "Packages last used in the number of days specified or greater will be deleted.")]
    public int Days { get; set; }

    [Option("keep-newest", HelpText = "Protect newest package versions by build, minor, or major version. " +
                                      "--days still applies to all other versions; use --days 0 to prune them regardless of last access.")]
    public string KeepNewest { get; set; }
}