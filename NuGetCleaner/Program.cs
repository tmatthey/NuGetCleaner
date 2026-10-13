using System;
using System.IO;
using System.Linq;
using CommandLine;
using NuGet.Configuration;

namespace NuGetCleaner;

internal class Program
{

    public static int Main(string[] args)
    {
        var osNameAndVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        if (!osNameAndVersion.Contains("Windows"))
        {
            Console.WriteLine("This tool is currently only available for Windows, sorry for the inconvenience!\n");
            return 1;
        }

        return Parser.Default.ParseArguments<Options>(args)
            .MapResult(Execute, _ => 1);
    }

    public static int Execute(Options options)
    {
        if (!RetentionLevelParser.TryParse(options.KeepNewest, out var keepNewest))
        {
            Console.WriteLine("--keep-newest must be build, minor, or major.");
            return 1;
        }

        if (CheckDisableLastAccess() != 2)
        {
            Console.Write("\nYour Last Access updates are not currently enabled so this tool will not work. \n" +
                          "To enable Last Access updates, run powershell as administrator and input:\n\n" +
                          "\tfsutil behavior set disablelastaccess 2\n\n" +
                          "You may be asked to reboot for the settings change to take effect\n\n" +
                          "Note: Last usage time of packages will only be tracked once the setting is enabled.\n" +
                          "As such, last package usage will only be determined from the enable date onward.\n" +
                          "For further information view documentation at https://github.com/chgill-MSFT/NuGetCleaner \n");
            return 1;
        }

        var settings = Settings.LoadDefaultSettings(".");
        var gpfPath = SettingsUtility.GetGlobalPackagesFolder(settings);

        var days = options.Days;

        if (options.DryRun)
        {
            SearchAndPrint(gpfPath, days, keepNewest);
        }
        else {
            SearchAndDestroy(gpfPath, days, keepNewest);
        }

        return 0;
    }

    public static int CheckDisableLastAccess()
    {
        var process = new System.Diagnostics.Process();
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            FileName = "CMD.exe",
            Arguments = "/c fsutil behavior query disablelastaccess"
        };
        process.StartInfo = startInfo;
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var outputArray = output.Split(" ");
        var setting = Convert.ToInt32(outputArray[2]);

        return setting;
    }

    public static void SearchAndDestroy(string path, int days, RetentionLevel? keepNewest = null)
    {
        try
        {
            long deletedBytes = 0;

            Console.WriteLine("Deleted Packages: ");

            foreach (var pkg in Directory.GetDirectories(path))
            {
                var packageVersions = Directory.GetDirectories(pkg);
                var protectedVersions =
                    PackageRetention.GetProtectedVersionDirectories(packageVersions, keepNewest);

                foreach (var pkgVersion in packageVersions)
                {
                    if (protectedVersions.Contains(pkgVersion))
                    {
                        continue;
                    }

                    var dirAge = DateTime.Now - RecursiveFindLat(pkgVersion, DateTime.MinValue);

                    if (dirAge.TotalDays >= days)
                    {
                        Console.WriteLine(pkgVersion);
                        deletedBytes += RecursiveDelete(pkgVersion);

                        if (Directory.GetDirectories(pkg).Length == 0)
                        {
                            Directory.Delete(pkg);
                        }
                    }
                }
            }

            if (deletedBytes > 0)
            {
                var deletedMegabytes = deletedBytes / (1024d * 1024d);
                Console.WriteLine($"\nDisk space saved: {deletedMegabytes:F2} MB");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public static void SearchAndPrint(string path, int days, RetentionLevel? keepNewest = null)
    {
        try
        {
            Console.WriteLine("DRY RUN (NOTHING IS DELETED)");

            Console.WriteLine("Would-Be Deleted Packages: \n");

            foreach (var pkg in Directory.GetDirectories(path))
            {
                var packageVersions = Directory.GetDirectories(pkg);
                var protectedVersions =
                    PackageRetention.GetProtectedVersionDirectories(packageVersions, keepNewest);

                foreach (var pkgVersion in packageVersions)
                {
                    if (protectedVersions.Contains(pkgVersion))
                    {
                        continue;
                    }

                    var dirAge = DateTime.Now - RecursiveFindLat(pkgVersion, DateTime.MinValue);

                    if (dirAge.TotalDays >= days)
                    {
                        Console.WriteLine(pkgVersion.Substring(path.Length + 1) + " --- Last Access: " + RecursiveFindLat(pkgVersion, DateTime.MinValue));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public static long RecursiveDelete(string dir)
    {
        long deletedBytes = 0;

        foreach (var subDir in Directory.GetDirectories(dir))
        {
            deletedBytes += RecursiveDelete(subDir);
        }
        foreach (var f in Directory.GetFiles(dir))
        {
            deletedBytes += new FileInfo(f).Length;
            File.Delete(f);
        }
        Directory.Delete(dir);

        return deletedBytes;
    }

    public static DateTime RecursiveFindLat(string dir, DateTime dt)
    {
        dt = Directory.GetDirectories(dir).Aggregate(dt, (current, subDir) => RecursiveFindLat(subDir, current));
        foreach (var f in Directory.GetFiles(dir))
        {
            if (dt < File.GetLastAccessTime(f))
            {
                dt = File.GetLastAccessTime(f);
            }
        }

        return dt;
    }
}