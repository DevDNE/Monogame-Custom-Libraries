namespace MonoGame.GameFramework.Tools;

public static class Program
{
  public static int Main(string[] args)
  {
    if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
    {
      PrintHelp();
      return 0;
    }

    return args[0] switch
    {
      "lint-spritefont" => RunLint(args[1..]),
      "lint-all-samples" => RunLintAllSamples(args[1..]),
      "check-content-cache" => RunContentCache(args[1..]),
      "check-content-cache-all" => RunContentCacheAll(args[1..]),
      "check-versions" => RunVersions(args[1..]),
      "check-boot" => RunBoot(args[1..]),
      "check-boot-all" => RunBootAll(args[1..]),
      "check-sprites" => RunSprites(args[1..]),
      "check-sprites-all" => RunSpritesAll(args[1..]),
      _ => UnknownCommand(args[0]),
    };
  }

  static int UnknownCommand(string cmd)
  {
    Console.Error.WriteLine($"Unknown command: {cmd}");
    PrintHelp();
    return 2;
  }

  static void PrintHelp()
  {
    Console.WriteLine("mgf-tools — MonoGame.GameFramework dev tools");
    Console.WriteLine();
    Console.WriteLine("  lint-spritefont --spritefont <path> --project <dir>");
    Console.WriteLine("      Scan a single project for string literals containing characters");
    Console.WriteLine("      not covered by the spritefont's CharacterRegions.");
    Console.WriteLine();
    Console.WriteLine("  lint-all-samples [--repo <root>]");
    Console.WriteLine("      Lint each sample under src/MonoGame.GameFramework.* against its");
    Console.WriteLine("      own Content/fonts/Arial.spritefont. Exits non-zero if any sample");
    Console.WriteLine("      has uncovered characters.");
    Console.WriteLine();
    Console.WriteLine("  check-content-cache --project <dir>");
    Console.WriteLine("      Flag spritefont sources newer than their compiled .xnb artifact");
    Console.WriteLine("      (MGCB's incremental cache can miss schema-preserving edits).");
    Console.WriteLine();
    Console.WriteLine("  check-content-cache-all [--repo <root>]");
    Console.WriteLine("      Run the cache check across every sample under src/.");
    Console.WriteLine();
    Console.WriteLine("  check-versions [--repo <root>]");
    Console.WriteLine("      Diff TargetFramework and PackageReference versions across every");
    Console.WriteLine("      csproj under src/. Exits non-zero on any cross-project mismatch.");
    Console.WriteLine();
    Console.WriteLine("  check-boot --project <dir>");
    Console.WriteLine("      Verify Game1.cs wires up Primitives.Initialize, DebugOverlay, and");
    Console.WriteLine("      SmokeHarness per CLAUDE.md's boot conventions.");
    Console.WriteLine();
    Console.WriteLine("  check-boot-all [--repo <root>]");
    Console.WriteLine("      Run the boot check across every sample under src/.");
    Console.WriteLine();
    Console.WriteLine("  check-sprites --project <dir>");
    Console.WriteLine("      For projects that ship textures: verify TextureFormat=Color, no");
    Console.WriteLine("      power-of-two padding, and no bare SpriteBatch.Begin() (which");
    Console.WriteLine("      defaults to LinearClamp and blurs pixel art). Projects with no");
    Console.WriteLine("      textures are skipped.");
    Console.WriteLine();
    Console.WriteLine("  check-sprites-all [--repo <root>]");
    Console.WriteLine("      Run the sprite-convention check across every sample under src/.");
  }

  static int RunSprites(string[] args)
  {
    string project = ParseProject(args);
    if (project == null)
    {
      Console.Error.WriteLine("usage: check-sprites --project <dir>");
      return 2;
    }
    // ReportSprites returns a violation count so the -all variant can total
    // them; collapse it to 0/1 here to match the other single-project commands
    // (and because an exit status of exactly 256 would wrap to success).
    int violations = ReportSprites(Path.GetFileName(Path.GetFullPath(project)), SpriteConventionChecker.Check(project), verbose: true);
    return violations == 0 ? 0 : 1;
  }

  static int RunSpritesAll(string[] args)
  {
    string repo = ParseRepo(args);
    string src = Path.Combine(repo, "src");
    if (!Directory.Exists(src))
    {
      Console.Error.WriteLine($"src/ directory not found at {Path.GetFullPath(src)}");
      return 2;
    }

    int totalViolations = 0;
    int withTextures = 0;
    int skipped = 0;
    foreach (string dir in Directory.EnumerateDirectories(src, "MonoGame.GameFramework.*"))
    {
      string name = Path.GetFileName(dir);
      if (name.EndsWith(".Tests") || name.EndsWith(".Tools")) continue;

      SpriteConventionChecker.CheckResult result = SpriteConventionChecker.Check(dir);
      if (!result.HasTextures)
      {
        Console.WriteLine($"-- {name} (no textures, skipped)");
        skipped++;
        continue;
      }
      withTextures++;
      totalViolations += ReportSprites(name, result, verbose: true);
    }

    Console.WriteLine();
    Console.WriteLine($"{withTextures} sample(s) with textures, {skipped} skipped, {totalViolations} violation(s) total.");
    return totalViolations == 0 ? 0 : 1;
  }

  static int ReportSprites(string name, SpriteConventionChecker.CheckResult result, bool verbose)
  {
    if (!result.HasTextures)
    {
      if (verbose) Console.WriteLine($"{name}: no textures in Content.mgcb, nothing to check.");
      return 0;
    }
    if (result.Violations.Count == 0)
    {
      if (verbose) Console.WriteLine($"ok {name}  ({result.TextureAssets.Count} texture(s))");
      return 0;
    }
    Console.WriteLine($"FAIL {name}  ({result.Violations.Count} violation(s))");
    foreach (SpriteConventionChecker.Violation v in result.Violations)
    {
      Console.WriteLine($"  {v.Description}");
    }
    return result.Violations.Count;
  }

  static int RunLint(string[] args)
  {
    string spritefont = null;
    string project = null;
    for (int i = 0; i < args.Length; i++)
    {
      if (args[i] == "--spritefont" && i + 1 < args.Length) spritefont = args[++i];
      else if (args[i] == "--project" && i + 1 < args.Length) project = args[++i];
    }
    if (spritefont == null || project == null)
    {
      Console.Error.WriteLine("lint-spritefont requires --spritefont <path> --project <dir>");
      return 2;
    }
    SpritefontLinter.LintResult result = SpritefontLinter.Lint(spritefont, project);
    return Report(result, spritefont, project);
  }

  static int RunLintAllSamples(string[] args)
  {
    string repo = ParseRepo(args);
    string src = Path.Combine(repo, "src");
    if (!Directory.Exists(src))
    {
      Console.Error.WriteLine($"src/ directory not found at {Path.GetFullPath(src)}");
      return 2;
    }

    int totalProblems = 0;
    int samplesScanned = 0;
    foreach (string dir in Directory.EnumerateDirectories(src, "MonoGame.GameFramework.*"))
    {
      string name = Path.GetFileName(dir);
      if (name.EndsWith(".Tests") || name.EndsWith(".Tools")) continue;

      string spritefont = Path.Combine(dir, "Content", "fonts", "Arial.spritefont");
      if (!File.Exists(spritefont))
      {
        Console.WriteLine($"-- {name} (no spritefont, skipped)");
        continue;
      }
      samplesScanned++;
      SpritefontLinter.LintResult result = SpritefontLinter.Lint(spritefont, dir);
      if (result.Problems.Count == 0)
      {
        Console.WriteLine($"ok {name}");
      }
      else
      {
        Console.WriteLine($"FAIL {name}  ({result.Problems.Count} problems)");
        foreach (SpritefontLinter.Problem p in result.Problems)
        {
          Console.WriteLine($"  {Path.GetRelativePath(dir, p.File)}:{p.Line}:{p.Column}  U+{(int)p.BadChar:X4} '{p.BadChar}'  in {Truncate(p.Literal, 60)}");
        }
        totalProblems += result.Problems.Count;
      }
    }

    Console.WriteLine();
    Console.WriteLine($"{samplesScanned} samples scanned, {totalProblems} problems total.");
    return totalProblems == 0 ? 0 : 1;
  }

  static int RunContentCache(string[] args)
  {
    string project = ParseProject(args);
    if (project == null)
    {
      Console.Error.WriteLine("check-content-cache requires --project <dir>");
      return 2;
    }
    ContentCacheChecker.CheckResult result = ContentCacheChecker.Check(project);
    return ReportContentCache(Path.GetFileName(project.TrimEnd(Path.DirectorySeparatorChar)), project, result, verbose: true);
  }

  static int RunContentCacheAll(string[] args)
  {
    string repo = ParseRepo(args);
    string src = Path.Combine(repo, "src");
    if (!Directory.Exists(src))
    {
      Console.Error.WriteLine($"src/ directory not found at {Path.GetFullPath(src)}");
      return 2;
    }

    int totalStale = 0;
    int samplesScanned = 0;
    int samplesSkipped = 0;
    foreach (string dir in Directory.EnumerateDirectories(src, "MonoGame.GameFramework.*"))
    {
      string name = Path.GetFileName(dir);
      if (name.EndsWith(".Tests") || name.EndsWith(".Tools")) continue;

      ContentCacheChecker.CheckResult result = ContentCacheChecker.Check(dir);
      if (!result.HadCompiledArtifacts)
      {
        Console.WriteLine($"-- {name} (no compiled Content/bin, skipped)");
        samplesSkipped++;
        continue;
      }
      samplesScanned++;
      if (result.StaleAssets.Count == 0)
      {
        Console.WriteLine($"ok {name}");
      }
      else
      {
        Console.WriteLine($"FAIL {name}  ({result.StaleAssets.Count} stale)");
        foreach (ContentCacheChecker.StaleAsset s in result.StaleAssets)
        {
          Console.WriteLine($"  {Path.GetRelativePath(dir, s.SourcePath)} ({s.SourceTime:u}) newer than {Path.GetRelativePath(dir, s.ArtifactPath)} ({s.ArtifactTime:u})");
        }
        totalStale += result.StaleAssets.Count;
      }
    }

    Console.WriteLine();
    Console.WriteLine($"{samplesScanned} samples scanned, {samplesSkipped} skipped (unbuilt), {totalStale} stale assets total.");
    if (totalStale > 0)
    {
      Console.WriteLine("Hint: rm -rf src/<sample>/Content/bin src/<sample>/Content/obj && dotnet build.");
    }
    return totalStale == 0 ? 0 : 1;
  }

  static int RunVersions(string[] args)
  {
    string repo = ParseRepo(args);
    VersionChecker.CheckResult result = VersionChecker.Check(repo);
    if (result.Projects.Count == 0)
    {
      Console.Error.WriteLine($"No .csproj files found under {Path.GetFullPath(Path.Combine(repo, "src"))}");
      return 2;
    }

    Console.WriteLine($"{result.Projects.Count} projects scanned.");
    foreach (VersionChecker.ProjectInfo proj in result.Projects)
    {
      Console.WriteLine($"  {proj.Name} [{proj.TargetFramework}] — {proj.Packages.Count} package(s)");
    }
    Console.WriteLine();

    if (result.Info.Count > 0)
    {
      Console.WriteLine("Info:");
      foreach (string line in result.Info) Console.WriteLine($"  INFO  {line}");
      Console.WriteLine();
    }

    if (result.Failures.Count == 0)
    {
      Console.WriteLine("No version drift.");
      return 0;
    }

    Console.WriteLine($"{result.Failures.Count} issue(s):");
    foreach (string line in result.Failures) Console.WriteLine($"  FAIL  {line}");
    return 1;
  }

  static int RunBoot(string[] args)
  {
    string project = ParseProject(args);
    if (project == null)
    {
      Console.Error.WriteLine("check-boot requires --project <dir>");
      return 2;
    }
    BootChecker.CheckResult result = BootChecker.Check(project);
    return ReportBoot(Path.GetFileName(project.TrimEnd(Path.DirectorySeparatorChar)), project, result);
  }

  static int RunBootAll(string[] args)
  {
    string repo = ParseRepo(args);
    string src = Path.Combine(repo, "src");
    if (!Directory.Exists(src))
    {
      Console.Error.WriteLine($"src/ directory not found at {Path.GetFullPath(src)}");
      return 2;
    }

    int totalMissing = 0;
    int samplesScanned = 0;
    foreach (string dir in Directory.EnumerateDirectories(src, "MonoGame.GameFramework.*"))
    {
      string name = Path.GetFileName(dir);
      if (name.EndsWith(".Tests") || name.EndsWith(".Tools")) continue;

      string game1 = Path.Combine(dir, "Game1.cs");
      if (!File.Exists(game1))
      {
        Console.WriteLine($"-- {name} (no Game1.cs, skipped)");
        continue;
      }
      samplesScanned++;
      BootChecker.CheckResult result = BootChecker.Check(dir);
      if (result.Missing.Count == 0)
      {
        Console.WriteLine($"ok {name}");
      }
      else
      {
        Console.WriteLine($"FAIL {name}  ({result.Missing.Count} missing)");
        foreach (BootChecker.MissingConvention m in result.Missing)
        {
          Console.WriteLine($"  {m.Description}");
        }
        totalMissing += result.Missing.Count;
      }
    }

    Console.WriteLine();
    Console.WriteLine($"{samplesScanned} samples scanned, {totalMissing} missing conventions total.");
    return totalMissing == 0 ? 0 : 1;
  }

  static int Report(SpritefontLinter.LintResult result, string spritefontPath, string projectPath)
  {
    Console.WriteLine($"Spritefont: {spritefontPath}");
    Console.WriteLine($"  ranges: {string.Join(", ", result.Ranges)}");
    Console.WriteLine($"Project:   {projectPath}");
    if (result.Problems.Count == 0)
    {
      Console.WriteLine("No problems.");
      return 0;
    }
    Console.WriteLine($"{result.Problems.Count} problem(s):");
    foreach (SpritefontLinter.Problem p in result.Problems)
    {
      Console.WriteLine($"  {Path.GetRelativePath(projectPath, p.File)}:{p.Line}:{p.Column}  U+{(int)p.BadChar:X4} '{p.BadChar}'  in {Truncate(p.Literal, 60)}");
    }
    return 1;
  }

  static int ReportContentCache(string name, string projectDir, ContentCacheChecker.CheckResult result, bool verbose)
  {
    if (!result.HadCompiledArtifacts)
    {
      if (verbose) Console.WriteLine($"{name}: no Content/bin (unbuilt), nothing to check.");
      return 0;
    }
    if (result.StaleAssets.Count == 0)
    {
      if (verbose) Console.WriteLine($"{name}: content cache fresh.");
      return 0;
    }
    Console.WriteLine($"{name}: {result.StaleAssets.Count} stale asset(s):");
    foreach (ContentCacheChecker.StaleAsset s in result.StaleAssets)
    {
      Console.WriteLine($"  {Path.GetRelativePath(projectDir, s.SourcePath)} ({s.SourceTime:u}) newer than {Path.GetRelativePath(projectDir, s.ArtifactPath)} ({s.ArtifactTime:u})");
    }
    Console.WriteLine("Hint: rm -rf Content/bin Content/obj && dotnet build.");
    return 1;
  }

  static int ReportBoot(string name, string projectDir, BootChecker.CheckResult result)
  {
    if (result.Missing.Count == 0)
    {
      Console.WriteLine($"{name}: all boot conventions satisfied.");
      return 0;
    }
    Console.WriteLine($"{name}: {result.Missing.Count} missing convention(s) in {Path.GetRelativePath(projectDir, result.Game1Path)}:");
    foreach (BootChecker.MissingConvention m in result.Missing)
    {
      Console.WriteLine($"  {m.Description}");
    }
    return 1;
  }

  static string ParseRepo(string[] args)
  {
    string repo = ".";
    for (int i = 0; i < args.Length; i++)
    {
      if (args[i] == "--repo" && i + 1 < args.Length) repo = args[++i];
    }
    return repo;
  }

  static string ParseProject(string[] args)
  {
    string project = null;
    for (int i = 0; i < args.Length; i++)
    {
      if (args[i] == "--project" && i + 1 < args.Length) project = args[++i];
    }
    return project;
  }

  static string Truncate(string s, int max)
    => s.Length <= max ? s : s[..(max - 1)] + "…";
}
