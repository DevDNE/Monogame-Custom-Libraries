using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MonoGame.GameFramework.Tools;

/// <summary>
/// Slices an animation strip and checks the things about a cycle that are
/// mechanically decidable.
///
/// The motivating case: a four-frame walk was authored, looked plausible as a
/// static strip, and two separate treatments of the feet turned out to be
/// broken only once the frames moved. Some of that is judgment and belongs in
/// Aseprite with a human watching it (scripts/anim-preview.sh). But a
/// surprising amount is not — a frame identical to its neighbour does nothing
/// at all, an empty frame is a hole in the cycle, and a strip whose width does
/// not divide by its frame count silently shifts every frame after the first.
/// None of those need an eye, and none of them were being caught.
///
/// What is deliberately reported rather than failed: footing and per-frame
/// bounds. A walk keeps a sole on the last row; a jump, an explosion and a
/// spinning coin all correctly do not, and a gate that fires on three of those
/// four is a gate people mute.
/// </summary>
public static class AnimationChecker
{
  public sealed record FrameInfo(
    int Index,
    ImageDescriber.Bounds? Content,
    int OpaquePixels,
    bool SolesOnLastRow);

  public sealed record Transition(
    int From,
    int To,
    int ChangedPixels,
    double PercentChanged,
    ImageDescriber.Bounds? ChangedBounds);

  public sealed record Result(
    int FrameWidth,
    int FrameHeight,
    int FrameCount,
    bool BinaryAlpha,
    int Colors,
    IReadOnlyList<FrameInfo> Frames,
    IReadOnlyList<Transition> Transitions,
    IReadOnlyList<string> Problems)
  {
    public bool Ok => Problems.Count == 0;
  }

  public static Result Check(string path, int frameWidth, int? frameHeight = null)
  {
    using Image<Rgba32> loaded = Image.Load<Rgba32>(path);
    Rgba32[] all = ImageDescriber.ToArray(loaded);
    int W = loaded.Width, H = loaded.Height;
    int fh = frameHeight ?? H;

    List<string> problems = new();

    if (frameWidth <= 0)
      return new Result(frameWidth, fh, 0, true, 0, [], [],
        [$"frame width must be positive, got {frameWidth}."]);

    if (W % frameWidth != 0)
      problems.Add($"strip is {W}px wide, which does not divide by a frame width of {frameWidth} " +
                   $"({(double)W / frameWidth:0.##} frames). Every frame after the first would be offset.");

    int count = W / frameWidth;
    if (count == 0) count = 1;

    HashSet<byte> alphas = new();
    HashSet<Palette.Rgb> colors = new();
    foreach (Rgba32 p in all)
    {
      alphas.Add(p.A);
      if (p.A == 255) colors.Add(new Palette.Rgb(p.R, p.G, p.B));
    }
    bool binary = alphas.All(a => a is 0 or 255);
    if (!binary)
      problems.Add($"partial alpha present ({string.Join(", ", alphas.Where(a => a is not (0 or 255)).OrderBy(a => a).Take(6))}). " +
                   "assets/STYLE.md allows 0 or 255 only.");

    Rgba32 At(int frame, int x, int y) => all[y * W + frame * frameWidth + x];

    List<FrameInfo> frames = new();
    for (int f = 0; f < count; f++)
    {
      int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1, opaque = 0;
      for (int y = 0; y < fh; y++)
        for (int x = 0; x < frameWidth; x++)
        {
          if (At(f, x, y).A != 255) continue;
          opaque++;
          if (x < minX) minX = x;
          if (y < minY) minY = y;
          if (x > maxX) maxX = x;
          if (y > maxY) maxY = y;
        }

      ImageDescriber.Bounds? content = maxX < 0
        ? null
        : new ImageDescriber.Bounds(minX, minY, maxX - minX + 1, maxY - minY + 1);

      if (content == null)
        problems.Add($"frame {f} is entirely transparent — a hole in the cycle.");

      frames.Add(new FrameInfo(f, content, opaque, content?.Bottom == fh));
    }

    // Consecutive frames, wrapping, so the loop seam is checked too: a cycle
    // whose last frame equals its first stutters once per revolution, and that
    // is the one boundary a frame-by-frame read never looks at.
    List<Transition> transitions = new();
    for (int i = 0; i < count; i++)
    {
      int j = (i + 1) % count;
      if (count == 1) break;

      int changed = 0, minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
      for (int y = 0; y < fh; y++)
        for (int x = 0; x < frameWidth; x++)
        {
          if (At(i, x, y).Equals(At(j, x, y))) continue;
          changed++;
          if (x < minX) minX = x;
          if (y < minY) minY = y;
          if (x > maxX) maxX = x;
          if (y > maxY) maxY = y;
        }

      transitions.Add(new Transition(i, j, changed,
        100.0 * changed / (frameWidth * fh),
        maxX < 0 ? null : new ImageDescriber.Bounds(minX, minY, maxX - minX + 1, maxY - minY + 1)));

      if (changed == 0)
        problems.Add($"frames {i} and {j} are identical — frame {j} does nothing. " +
                     "A hold belongs in the frame duration, which a PNG strip cannot carry.");
    }

    return new Result(frameWidth, fh, count, binary, colors.Count, frames, transitions, problems);
  }
}
