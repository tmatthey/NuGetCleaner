using CommandLine;
using System.Reflection;
using Xunit;

namespace NuGetCleaner.Tests;

public class OptionsTests
{
    [Fact]
    public void ParsesKeepNewestLevel()
    {
        Options? parsedOptions = null;

        Parser.Default
            .ParseArguments<Options>(["--days", "30", "--keep-newest", "minor"])
            .WithParsed(options => parsedOptions = options);

        Assert.NotNull(parsedOptions);
        Assert.True(RetentionLevelParser.TryParse(parsedOptions.KeepNewest, out var level));
        Assert.Equal(RetentionLevel.Minor, level);
    }

    [Fact]
    public void RejectsNumericKeepNewestLevel()
    {
        Options? parsedOptions = null;

        Parser.Default
            .ParseArguments<Options>(["--days", "30", "--keep-newest", "3"])
            .WithParsed(options => parsedOptions = options);

        Assert.NotNull(parsedOptions);
        Assert.False(RetentionLevelParser.TryParse(parsedOptions.KeepNewest, out _));
    }

    [Fact]
    public void KeepNewestHelpExplainsAgeFilter()
    {
        var option = typeof(Options)
            .GetProperty(nameof(Options.KeepNewest))!
            .GetCustomAttribute<OptionAttribute>();

        Assert.NotNull(option);
        Assert.Contains("--days still applies", option.HelpText);
        Assert.Contains("--days 0", option.HelpText);
    }
}
