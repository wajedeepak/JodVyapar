using System.Text.RegularExpressions;

namespace JodVyapar.Api.Tests;

/// <summary>Finds and splits the SQL scripts in the repo's /Database folder.</summary>
public static partial class DatabaseScripts
{
    /// <summary>01_..., 02_..., 03_... in order; 99_DropAll.sql is excluded.</summary>
    public static IEnumerable<string> SchemaAndSeed() =>
        Directory.GetFiles(Folder(), "*.sql")
            .Where(path => NumberedScript().IsMatch(Path.GetFileName(path)) && !Path.GetFileName(path).StartsWith("99_", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal);

    /// <summary>Splits a script on its GO lines, as SSMS and sqlcmd do.</summary>
    public static IEnumerable<string> Batches(string path) =>
        GoLine().Split(File.ReadAllText(path)).Where(batch => !string.IsNullOrWhiteSpace(batch));

    private static string Folder()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Database");
            if (File.Exists(Path.Combine(candidate, "01_CreateTables.sql")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Could not find the repo's Database folder.");
    }

    [GeneratedRegex(@"^\d{2}_.+\.sql$")]
    private static partial Regex NumberedScript();

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoLine();
}
