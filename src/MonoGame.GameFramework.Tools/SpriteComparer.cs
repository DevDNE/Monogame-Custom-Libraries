using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MonoGame.GameFramework.Tools;

/// <summary>
/// How far is this sprite from that one?
///
/// Every gate in this repo answers "is this legal" — palette membership, binary
/// alpha, .pix/PNG agreement, texture format, sampler state. None of them
/// answers "is this good", and for a recreation the two are unrelated: art can
/// be perfectly legal and completely wrong. Without a number, "looks off" is
/// unfalsifiable, and an unfalsifiable complaint cannot drive an iteration.
///
/// The two headline figures measure different failures on purpose:
///
///   **Canvas IoU** compares silhouettes where they actually sit. It punishes
///   a sprite that is the right shape in the wrong place or at the wrong size,
///   which is the failure that matters once the art has to share a canvas
///   convention with a cast.
///
///   **Shape IoU** crops both to their content and normalises, so it ignores
///   canvas and scale entirely and asks only whether the silhouette is the
///   same silhouette. A big gap between the two is diagnostic in itself: right
///   shape, wrong size.
/// </summary>
public static class SpriteComparer
{
  /// <summary>
  /// Grid both silhouettes are resampled onto for the scale-free comparison.
  /// Large enough that a 32px sprite is upsampled rather than crushed, small
  /// enough that a one-pixel wobble does not dominate.
  /// </summary>
  const int ShapeGrid = 64;

  public sealed record Comparison(
    int WidthA, int HeightA, int WidthB, int HeightB,
    ImageDescriber.Bounds? ContentA, ImageDescriber.Bounds? ContentB,
    int ColorsA, int ColorsB,
    bool SameSize,
    int ComparedPixels,
    int ExactMatches,
    int CanvasIntersection, int CanvasUnion,
    int ShapeIntersection, int ShapeUnion,
    double MeanDelta, double MaxDelta)
  {
    public double ExactMatchRatio => ComparedPixels == 0 ? 0 : (double)ExactMatches / ComparedPixels;
    public double CanvasIou => CanvasUnion == 0 ? 0 : (double)CanvasIntersection / CanvasUnion;
    public double ShapeIou => ShapeUnion == 0 ? 0 : (double)ShapeIntersection / ShapeUnion;
  }

  public static Comparison Compare(string pathA, string pathB)
  {
    (Image<Rgba32> a, ImageDescriber.Description da) = LoadNative(pathA);
    (Image<Rgba32> b, ImageDescriber.Description db) = LoadNative(pathB);
    using (a)
    using (b)
    {
      Rgba32[] pa = ImageDescriber.ToArray(a);
      Rgba32[] pb = ImageDescriber.ToArray(b);
      bool sameSize = a.Width == b.Width && a.Height == b.Height;

      int compared = 0, exact = 0, inter = 0, union = 0;
      double total = 0, max = 0;

      if (sameSize)
      {
        for (int i = 0; i < pa.Length; i++)
        {
          bool oa = pa[i].A == 255, ob = pb[i].A == 255;
          if (oa || ob) union++;
          if (oa && ob) inter++;
          if (!oa || !ob) continue;

          compared++;
          if (pa[i].Equals(pb[i])) { exact++; continue; }

          double d = Palette.Oklab.FromRgb(new Palette.Rgb(pa[i].R, pa[i].G, pa[i].B))
            .DistanceTo(Palette.Oklab.FromRgb(new Palette.Rgb(pb[i].R, pb[i].G, pb[i].B)));
          total += d;
          if (d > max) max = d;
        }
        // Pixels identical in both contribute a delta of zero, and leaving
        // them out would report the mean of only the disagreements — which
        // rises as the sprite gets closer to correct.
        total += 0;
      }

      bool[] sa = ShapeMask(pa, a.Width, da.Content);
      bool[] sb = ShapeMask(pb, b.Width, db.Content);
      int si = 0, su = 0;
      for (int i = 0; i < sa.Length; i++)
      {
        if (sa[i] || sb[i]) su++;
        if (sa[i] && sb[i]) si++;
      }

      return new Comparison(
        a.Width, a.Height, b.Width, b.Height,
        da.Content, db.Content,
        da.Colors.Count, db.Colors.Count,
        sameSize, compared, exact,
        inter, union, si, su,
        compared == 0 ? 0 : total / compared, max);
    }
  }

  static (Image<Rgba32>, ImageDescriber.Description) LoadNative(string path)
  {
    using Image<Rgba32> loaded = Image.Load<Rgba32>(path);
    int block = ImageDescriber.DetectBlockSize(ImageDescriber.ToArray(loaded), loaded.Width, loaded.Height);
    Image<Rgba32> native = ImageDescriber.Reduce(loaded, block);
    return (native, ImageDescriber.Describe(native));
  }

  /// <summary>
  /// Content cropped to its bounding box and nearest-resampled onto a fixed
  /// grid, so silhouettes of different sizes and canvas placements can be
  /// compared as shapes.
  /// </summary>
  static bool[] ShapeMask(Rgba32[] px, int width, ImageDescriber.Bounds? content)
  {
    bool[] mask = new bool[ShapeGrid * ShapeGrid];
    if (content is not ImageDescriber.Bounds c || c.Width == 0 || c.Height == 0) return mask;

    for (int y = 0; y < ShapeGrid; y++)
    {
      int sy = c.Y + y * c.Height / ShapeGrid;
      for (int x = 0; x < ShapeGrid; x++)
      {
        int sx = c.X + x * c.Width / ShapeGrid;
        mask[y * ShapeGrid + x] = px[sy * width + sx].A == 255;
      }
    }
    return mask;
  }
}
