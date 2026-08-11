using System.Xml.Linq;

namespace MonoGame.GameFramework.Tools;

/// <summary>
/// Cross-project csproj drift detector. For every csproj under src/,
/// reports:
///   • TargetFramework disagreements (any project not matching majority)
///   • Packages that appear at more than one Version across projects
///   • Packages that appear in only one project (informational)
///
/// The third category catches e.g. BattleGrid's `dotenv.net` — fine as-is,
/// but worth surfacing so someone pulling it into a 10th sample knows. The
/// first two are regression-guards against version drift during
/// multi-sample refactors.
/// </summary>
public static class VersionChecker
{
  public readonly record struct ProjectInfo(
    string Name,
    string Path,
    string TargetFramework,
    IReadOnlyDictionary<string, string> Packages);

  public sealed record CheckResult(
    IReadOnlyList<ProjectInfo> Projects,
    IReadOnlyList<string> Failures,
    IReadOnlyList<string> Info);

  public static CheckResult Check(string repoRoot)
  {
    string src = Path.Combine(repoRoot, "src");
    List<ProjectInfo> projects = new();
    if (Directory.Exists(src))
    {
      foreach (string csproj in Directory.EnumerateFiles(src, "*.csproj", SearchOption.AllDirectories))
      {
        string rel = Path.GetRelativePath(src, csproj);
        if (rel.Contains("/bin/") || rel.Contains("/obj/") ||
            rel.Contains("\\bin\\") || rel.Contains("\\obj\\")) continue;
        projects.Add(LoadProject(csproj));
      }
    }

    List<string> failures = new();
    List<string> info = new();

    List<IGrouping<string, ProjectInfo>> tfmGroups = projects
      .Where(p => p.TargetFramework != "(unspecified)")
      .GroupBy(p => p.TargetFramework)
      .OrderByDescending(g => g.Count())
      .ToList();
    if (tfmGroups.Count > 1)
    {
      string majority = tfmGroups[0].Key;
      foreach (IGrouping<string, ProjectInfo> grp in tfmGroups.Skip(1))
      {
        foreach (ProjectInfo proj in grp)
          failures.Add($"TargetFramework mismatch: {proj.Name} uses '{proj.TargetFramework}' (majority is '{majority}')");
      }
    }

    Dictionary<string, Dictionary<string, List<string>>> byPackage = new();
    foreach (ProjectInfo proj in projects)
    {
      foreach (KeyValuePair<string, string> kv in proj.Packages)
      {
        if (!byPackage.TryGetValue(kv.Key, out Dictionary<string, List<string>> versions))
          byPackage[kv.Key] = versions = new Dictionary<string, List<string>>();
        if (!versions.TryGetValue(kv.Value, out List<string> users))
          versions[kv.Value] = users = new List<string>();
        users.Add(proj.Name);
      }
    }

    foreach (KeyValuePair<string, Dictionary<string, List<string>>> pkg in byPackage.OrderBy(kv => kv.Key, StringComparer.Ordinal))
    {
      if (pkg.Value.Count > 1)
      {
        string summary = string.Join("; ",
          pkg.Value.Select(v => $"{v.Key} in [{string.Join(", ", v.Value)}]"));
        failures.Add($"Package {pkg.Key} has multiple versions: {summary}");
      }
      else
      {
        KeyValuePair<string, List<string>> single = pkg.Value.Single();
        if (single.Value.Count == 1)
          info.Add($"Package {pkg.Key} {single.Key} used only by {single.Value[0]}");
      }
    }

    return new CheckResult(projects, failures, info);
  }

  static ProjectInfo LoadProject(string csprojPath)
  {
    string name = Path.GetFileNameWithoutExtension(csprojPath);
    XDocument doc = XDocument.Load(csprojPath);
    string tfm = doc.Descendants("TargetFramework").Select(e => e.Value).FirstOrDefault() ?? "(unspecified)";
    Dictionary<string, string> packages = new();
    foreach (XElement pr in doc.Descendants("PackageReference"))
    {
      string include = pr.Attribute("Include")?.Value;
      string version = pr.Attribute("Version")?.Value ?? pr.Element("Version")?.Value;
      if (string.IsNullOrEmpty(include) || string.IsNullOrEmpty(version)) continue;
      packages[include] = version;
    }
    return new ProjectInfo(name, csprojPath, tfm, packages);
  }
}
