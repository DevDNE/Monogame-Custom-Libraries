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
      "check-palette" => RunPalette(args[1..]),
      "check-palette-all" => RunPaletteAll(args[1..]),
      "conform-sprite" => RunConform(args[1..]),
      "check-palettes" => RunPaletteFiles(args[1..]),
      "render-pix" => RunRenderPix(args[1..]),
      "render-pix-all" => RunRenderPixAll(args[1..]),
      "check-pix-all" => RunCheckPixAll(args[1..]),
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
    Console.WriteLine();
    Console.WriteLine("  check-palette --project <dir> [--palette <file>]");
    Console.WriteLine("      Verify every PNG under <dir>/Content/sprites (or <dir>/sprites)");
    Console.WriteLine("      uses only colours from assets/palette.gpl, with binary alpha.");
    Console.WriteLine("      Reports ramp collisions inside the palette as INFO. Projects with");
    Console.WriteLine("      no sprites are skipped.");
    Console.WriteLine();
    Console.WriteLine("  check-palette-all [--repo <root>]");
    Console.WriteLine("      Run the palette check across assets/ and every sample under src/.");
    Console.WriteLine();
    Console.WriteLine("  conform-sprite --input <png> --output <png> [--size NxM|N]");
    Console.WriteLine("                 [--palette <file>] [--alpha-threshold <0-255>]");
    Console.WriteLine("      Map an arbitrary image onto the palette: nearest colour in Oklab,");
    Console.WriteLine("      binary alpha, optional box-resample to a target size. Prints how");
    Console.WriteLine("      far the art had to move — a large mean delta means the source was");
    Console.WriteLine("      fighting the palette and should be regenerated, not accepted.");
    Console.WriteLine();
    Console.WriteLine("  render-pix --input <pix> [--output <png>] [--palette <file>]");
    Console.WriteLine("      Render a .pix text sprite to PNG. Output defaults to the .pix path");
    Console.WriteLine("      with a .png extension, which is where check-pix-all expects it.");
    Console.WriteLine();
    Console.WriteLine("  render-pix-all [--repo <root>]");
    Console.WriteLine("      Re-render every .pix under assets/, template/ and src/ in place.");
    Console.WriteLine();
    Console.WriteLine("  check-pix-all [--repo <root>]");
    Console.WriteLine("      Verify every committed PNG still matches the .pix beside it. The");
    Console.WriteLine("      .pix is the source; the PNG is a build artefact that happens to be");
    Console.WriteLine("      committed, and this is what keeps that claim true.");
  }

  static int RunPalette(string[] args)
  {
    string project = ParseProject(args);
    if (project == null)
    {
      Console.Error.WriteLine("usage: check-palette --project <dir> [--palette <file>]");
      return 2;
    }

    PaletteChecker.CheckResult result = PaletteChecker.Check(project, ParseFlag(args, "--palette"));
    if (!result.HasPalette)
    {
      Console.Error.WriteLine("No assets/palette.gpl found above " + Path.GetFullPath(project) + ".");
      return 2;
    }

    int violations = ReportPalette(Path.GetFileName(Path.GetFullPath(project)), result, verbose: true);
    ReportCollisions(result.Collisions);
    return violations == 0 ? 0 : 1;
  }

  static int RunPaletteAll(string[] args)
  {
    string repo = ParseRepo(args);
    string src = Path.Combine(repo, "src");
    if (!Directory.Exists(src))
    {
      Console.Error.WriteLine($"src/ directory not found at {Path.GetFullPath(src)}");
      return 2;
    }

    // assets/ leads: it holds the authoring sources the games export from, so
    // drift shows up there first. template/ is in the list because it seeds
    // every future sample — an off-palette placeholder there would be
    // inherited by game #10 and every game after it.
    List<string> targets = new();
    foreach (string extra in new[] { "assets", "template" })
    {
      string dir = Path.Combine(repo, extra);
      if (Directory.Exists(dir)) targets.Add(dir);
    }
    foreach (string dir in Directory.EnumerateDirectories(src, "MonoGame.GameFramework.*"))
    {
      string name = Path.GetFileName(dir);
      if (name.EndsWith(".Tests") || name.EndsWith(".Tools")) continue;
      targets.Add(dir);
    }

    int totalViolations = 0, scanned = 0, skipped = 0, sprites = 0;
    List<PaletteChecker.PaletteCollision> collisions = new();
    HashSet<string> collisionsSeen = new(StringComparer.Ordinal);
    bool foundPalette = false;

    foreach (string target in targets)
    {
      string name = Path.GetFileName(Path.GetFullPath(target));
      PaletteChecker.CheckResult result = PaletteChecker.Check(target);
      if (!result.HasPalette) continue;
      foundPalette = true;

      // Games sharing a palette would otherwise report its collisions once
      // each; the collision belongs to the file, not to the caller.
      foreach (PaletteChecker.PaletteCollision c in result.Collisions)
      {
        if (collisionsSeen.Add($"{c.PaletteFile}|{c.Collision.A.Name}|{c.Collision.B.Name}"))
          collisions.Add(c);
      }

      if (!result.HasSprites)
      {
        Console.WriteLine($"-- {name} (no sprites, skipped)");
        skipped++;
        continue;
      }
      scanned++;
      sprites += result.ScannedSprites.Count;
      totalViolations += ReportPalette(name, result, verbose: true);
    }

    if (!foundPalette)
    {
      Console.Error.WriteLine($"No assets/palette.gpl found under {Path.GetFullPath(repo)}.");
      return 2;
    }

    Console.WriteLine();
    Console.WriteLine($"{scanned} target(s) with sprites ({sprites} PNG(s)), {skipped} skipped, {totalViolations} violation(s) total.");
    ReportCollisions(collisions);
    return totalViolations == 0 ? 0 : 1;
  }

  static int ReportPalette(string name, PaletteChecker.CheckResult result, bool verbose)
  {
    if (!result.HasSprites)
    {
      if (verbose) Console.WriteLine($"{name}: no sprites, nothing to check.");
      return 0;
    }
    if (result.Violations.Count == 0)
    {
      if (verbose) Console.WriteLine($"ok {name}  ({result.ScannedSprites.Count} sprite(s), {DescribePalettes(result)})");
      return 0;
    }
    Console.WriteLine($"FAIL {name}  ({result.Violations.Count} violation(s), {DescribePalettes(result)})");
    foreach (PaletteChecker.Violation v in result.Violations)
    {
      Console.WriteLine($"  {v.Description}");
    }
    return result.Violations.Count;
  }

  /// <summary>
  /// Names the palette(s) a target was judged against. With per-game palettes
  /// "ok Puzzle (14 sprites)" is ambiguous — green against which thirteen
  /// colours? — and a sprite silently falling through to the repo-wide default
  /// because its own palette.gpl is missing is exactly the drift worth seeing.
  /// </summary>
  static string DescribePalettes(PaletteChecker.CheckResult result)
  {
    if (result.PalettesUsed.Count == 0) return "no palette";
    IEnumerable<string> names = result.PalettesUsed
      .Select(p => $"{Path.GetFileName(Path.GetDirectoryName(p))}/{Path.GetFileName(p)}");
    return "vs " + string.Join(" + ", names);
  }

  // Never a failure. Two colours sharing a ramp slot is real drift, but the
  // fix repaints committed art, so a human picks the winner.
  static void ReportCollisions(IReadOnlyList<PaletteChecker.PaletteCollision> collisions)
  {
    if (collisions.Count == 0) return;
    Console.WriteLine();
    Console.WriteLine($"{collisions.Count} ramp collision(s) in the palette(s) themselves:");
    foreach (PaletteChecker.PaletteCollision pc in collisions)
    {
      Palette.RampCollision c = pc.Collision;
      Console.WriteLine(
        $"  INFO  [{Path.GetFileName(Path.GetDirectoryName(pc.PaletteFile))}/{Path.GetFileName(pc.PaletteFile)}] " +
        $"{c.A.Name} {c.A.Color.Hex} and {c.B.Name} {c.B.Color.Hex} share a ramp slot " +
        $"(hue {c.HueDelta:F1}deg apart, lightness {c.LightnessDelta:F1} apart). Pick one and repaint the other out.");
    }
  }

  static int RunPaletteFiles(string[] args)
  {
    string repo = ParseRepo(args);
    List<string> files = PaletteRegistry.FindAll(repo).ToList();
    if (files.Count == 0)
    {
      Console.Error.WriteLine($"No palette.gpl found under {Path.GetFullPath(repo)}.");
      return 2;
    }

    int failures = 0;
    List<PaletteChecker.PaletteCollision> collisions = new();
    foreach (string file in files)
    {
      Palette palette = Palette.Load(file);
      string rel = Path.GetRelativePath(repo, file);
      string missing = PaletteRegistry.MissingSpine(palette);
      foreach (Palette.RampCollision c in palette.FindRampCollisions())
        collisions.Add(new PaletteChecker.PaletteCollision(file, c));

      if (missing != null)
      {
        Console.WriteLine($"FAIL {rel}  ({palette.Count} colours)");
        Console.WriteLine($"  {missing}");
        failures++;
        continue;
      }
      Console.WriteLine($"ok   {rel}  ({palette.Count} colours)");
    }

    Console.WriteLine();
    Console.WriteLine($"{files.Count} palette(s), {failures} failure(s).");
    ReportCollisions(collisions);
    return failures == 0 ? 0 : 1;
  }

  static int RunRenderPix(string[] args)
  {
    string input = ParseFlag(args, "--input");
    if (input == null)
    {
      Console.Error.WriteLine("usage: render-pix --input <pix> [--output <png>] [--palette <file>]");
      return 2;
    }
    if (!File.Exists(input))
    {
      Console.Error.WriteLine($"Input not found: {input}");
      return 2;
    }

    string output = ParseFlag(args, "--output") ?? PixRenderer.OutputPathFor(input);
    try
    {
      PixRenderer.RenderResult r = PixRenderer.RenderToFile(input, output, ParseFlag(args, "--palette"));
      Console.WriteLine($"{input} -> {output}  {r.Width}x{r.Height}, {r.OpaquePixels} opaque px");
      Console.WriteLine($"  palette {r.PaletteFile}");
      foreach (SpriteConformer.PaletteUsage u in r.Usage)
        Console.WriteLine($"    {u.Entry.Color.Hex}  {u.Entry.Name,-18} {u.PixelCount,6} px");
      return 0;
    }
    catch (PixDocument.ParseException e)
    {
      Console.Error.WriteLine(e.Message);
      return 1;
    }
  }

  static int RunRenderPixAll(string[] args)
  {
    string repo = ParseRepo(args);
    int rendered = 0, failed = 0;
    foreach (string pix in PixSourceRoots(repo).SelectMany(PixRenderer.FindSources))
    {
      string output = PixRenderer.OutputPathFor(pix);
      try
      {
        PixRenderer.RenderResult r = PixRenderer.RenderToFile(pix, output);
        Console.WriteLine($"ok   {Path.GetRelativePath(repo, pix)}  {r.Width}x{r.Height}");
        rendered++;
      }
      catch (PixDocument.ParseException e)
      {
        Console.WriteLine($"FAIL {Path.GetRelativePath(repo, pix)}");
        Console.WriteLine($"  {e.Message}");
        failed++;
      }
    }
    Console.WriteLine();
    Console.WriteLine($"{rendered} rendered, {failed} failed.");
    return failed == 0 ? 0 : 1;
  }

  static int RunCheckPixAll(string[] args)
  {
    string repo = ParseRepo(args);
    int ok = 0, drifted = 0;
    foreach (string pix in PixSourceRoots(repo).SelectMany(PixRenderer.FindSources))
    {
      string rel = Path.GetRelativePath(repo, pix);
      try
      {
        string diff = PixRenderer.Diff(pix, PixRenderer.OutputPathFor(pix));
        if (diff == null) { ok++; continue; }
        Console.WriteLine($"DRIFT {rel}");
        Console.WriteLine($"  {diff}");
        drifted++;
      }
      catch (PixDocument.ParseException e)
      {
        Console.WriteLine($"FAIL  {rel}");
        Console.WriteLine($"  {e.Message}");
        drifted++;
      }
    }

    if (ok + drifted == 0)
    {
      Console.WriteLine("No .pix sources found; nothing to check.");
      return 0;
    }
    Console.WriteLine();
    Console.WriteLine($"{ok} PNG(s) match their .pix, {drifted} out of date.");
    if (drifted > 0) Console.WriteLine("Fix with: dotnet run --project src/MonoGame.GameFramework.Tools -- render-pix-all");
    return drifted == 0 ? 0 : 1;
  }

  // assets/ and template/ alongside src/, for the same reason check-palette-all
  // covers them: the authoring sources and the seed for game #10 both count.
  static IEnumerable<string> PixSourceRoots(string repo)
  {
    foreach (string extra in new[] { "assets", "template", "src" })
    {
      string dir = Path.Combine(repo, extra);
      if (Directory.Exists(dir)) yield return dir;
    }
  }

  static int RunConform(string[] args)
  {
    string input = ParseFlag(args, "--input");
    string output = ParseFlag(args, "--output");
    if (input == null || output == null)
    {
      Console.Error.WriteLine("usage: conform-sprite --input <png> --output <png> [--size NxM|N] [--palette <file>] [--alpha-threshold <0-255>]");
      return 2;
    }
    if (!File.Exists(input))
    {
      Console.Error.WriteLine($"Input not found: {input}");
      return 2;
    }

    string paletteFile = ParseFlag(args, "--palette")
      ?? Palette.FindPaletteFile(Path.GetDirectoryName(Path.GetFullPath(input)))
      ?? Palette.FindPaletteFile(Directory.GetCurrentDirectory());
    if (paletteFile == null)
    {
      Console.Error.WriteLine("No assets/palette.gpl found. Pass --palette <file>.");
      return 2;
    }

    if (!TryParseSize(ParseFlag(args, "--size"), out int? w, out int? h))
    {
      Console.Error.WriteLine("--size expects N or NxM (e.g. 32 or 32x48).");
      return 2;
    }

    int alphaThreshold = SpriteConformer.DefaultAlphaThreshold;
    string rawThreshold = ParseFlag(args, "--alpha-threshold");
    if (rawThreshold != null && (!int.TryParse(rawThreshold, out alphaThreshold) || alphaThreshold is < 0 or > 255))
    {
      Console.Error.WriteLine("--alpha-threshold expects 0-255.");
      return 2;
    }

    Palette palette = Palette.Load(paletteFile);
    SpriteConformer.ConformResult r = SpriteConformer.Conform(input, output, palette, w, h, alphaThreshold);

    Console.WriteLine($"Palette: {paletteFile} ({palette.Count} colours)");
    Console.WriteLine($"  {input}  {r.SourceWidth}x{r.SourceHeight}");
    Console.WriteLine($"  {output}  {r.Width}x{r.Height}");
    Console.WriteLine();
    Console.WriteLine($"  opaque pixels     {r.OpaquePixels}");
    Console.WriteLine($"  recoloured        {r.PixelsChanged} ({Percent(r.PixelsChanged, r.OpaquePixels)})");
    Console.WriteLine($"  alpha flattened   {r.AlphaFlattened}");
    Console.WriteLine($"  mean delta        {r.MeanDelta:F4}  (Oklab; over ~0.05 means the source fought the palette)");
    Console.WriteLine($"  max delta         {r.MaxDelta:F4}");
    Console.WriteLine($"  palette coverage  {r.Usage.Count}/{palette.Count} colours used");
    foreach (SpriteConformer.PaletteUsage u in r.Usage)
    {
      Console.WriteLine($"    {u.Entry.Color.Hex}  {u.Entry.Name,-16} {u.PixelCount,6} px");
    }

    if (r.AspectDistorted)
    {
      Console.WriteLine();
      Console.WriteLine($"  WARNING  {r.SourceWidth}x{r.SourceHeight} -> {r.Width}x{r.Height} is a non-uniform scale. " +
                        "Pixel art does not survive that; re-author the source at the right aspect.");
    }
    if (r.MeanDelta > 0.05)
    {
      Console.WriteLine();
      Console.WriteLine("  WARNING  Large mean delta. The source is being forced onto the palette rather than");
      Console.WriteLine("           respecting it. Prefer regenerating with a palette-aware prompt or style");
      Console.WriteLine("           reference over shipping this.");
    }
    return 0;
  }

  static string Percent(int n, int total) => total == 0 ? "0%" : $"{100.0 * n / total:F1}%";

  static bool TryParseSize(string raw, out int? width, out int? height)
  {
    width = height = null;
    if (raw == null) return true;

    string[] parts = raw.Split('x', 'X');
    if (parts.Length == 1 && int.TryParse(parts[0], out int square) && square > 0)
    {
      width = height = square;
      return true;
    }
    if (parts.Length == 2
      && int.TryParse(parts[0], out int w) && w > 0
      && int.TryParse(parts[1], out int h) && h > 0)
    {
      width = w;
      height = h;
      return true;
    }
    return false;
  }

  static string ParseFlag(string[] args, string flag)
  {
    for (int i = 0; i < args.Length; i++)
    {
      if (args[i] == flag && i + 1 < args.Length) return args[i + 1];
    }
    return null;
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
