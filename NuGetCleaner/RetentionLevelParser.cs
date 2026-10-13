using System;

namespace NuGetCleaner;

internal static class RetentionLevelParser
{
    public static bool TryParse(string value, out RetentionLevel? level)
    {
        if (value is null)
        {
            level = null;
            return true;
        }

        foreach (var candidate in Enum.GetValues<RetentionLevel>())
        {
            if (string.Equals(value, candidate.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                level = candidate;
                return true;
            }
        }

        level = null;
        return false;
    }
}
