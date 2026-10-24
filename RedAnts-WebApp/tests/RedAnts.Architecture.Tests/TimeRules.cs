using Xunit;
using System.Text.RegularExpressions;

namespace RedAnts.Architecture.Tests;

public class TimeRules
{
    private static readonly Regex MachineClock = new(
        @"\bDateTime\.Now\b|\bDateTimeOffset\.Now\b|\bToLocalTime\(\)|\.LocalDateTime\b|\.UtcDateTime\b",
        RegexOptions.Compiled);

    private static readonly string[] Projects = ["RedAnts.Ticketing", "RedAnts.Show", "RedAnts.Host", "RedAnts.Kernel"];

    private static readonly string[] Exempt = ["SwissTime.cs"];

    [Fact]
    public void The_web_modules_read_the_clock_only_through_SwissTime()
    {
        var offenders = new List<string>();
        foreach (var project in Projects)
        {
            var directory = Path.Combine(RedAntsArchitecture.RepoRoot(), "src", project);
            foreach (var file in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                if (RedAntsArchitecture.IsBuildOutput(file)) continue;
                if (!file.EndsWith(".cs") && !file.EndsWith(".razor") && !file.EndsWith(".cshtml")) continue;
                if (Exempt.Contains(Path.GetFileName(file))) continue;
                var lines = File.ReadAllLines(file);
                for (var i = 0; i < lines.Length; i++)
                    if (MachineClock.IsMatch(lines[i]))
                        offenders.Add($"{project}/{Path.GetRelativePath(directory, file).Replace(Path.DirectorySeparatorChar, '/')}:{i + 1}: {lines[i].Trim()}");
            }
        }
        Assert.True(offenders.Count == 0,
            "Azure runs in UTC, so the machine clock is never the Swiss clock: write instants with SwissTime.Timestamp, "
            + "read the current moment with SwissTime.Now or SwissTime.Today and display stored instants with SwissTime.ToSwiss. "
            + "Elapsed time and token lifetimes use DateTimeOffset.UtcNow:\n" + string.Join("\n", offenders));
    }
}
