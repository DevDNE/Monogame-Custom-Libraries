using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MonoGame.GameFramework.Tools;

/// <summary>
/// Maps an arbitrary PNG onto the locked palette. This is the seam that keeps
/// the generation front-end swappable: hand-drawn, PixelLab, Retro Diffusion,
/// or a traced concept image all arrive here, and everything downstream sees
/// the same constrained output.
///
/// Two decisions worth knowing about:
///
/// **Box resampling, not nearest-neighbour.** Nearest is the reflex for pixel
/// art, but it is wrong for the case that actually matters — reducing a large
/// smooth generated image, where nearest throws away 99% of the pixels and
/// keeps whichever one happened to land on the sample point. Box averaging
/// uses all of them. For true pixel art at an integer downscale the two agree
/// exactly, since every source block is a single flat colour, so box is
/// correct in both situations and nearest is correct in only one.
///
/// **The report is the point.** <see cref="ConformResult.MeanDelta"/> measures
/// how far the image had to move to become legal. A low number means the art
/// already respected the palette; a high one means it is being forced, and
/// forced art looks forced. Conform tells you which you have — it cannot make
/// the second case good.
/// </summary>
public static class SpriteConformer
{
  public readonly record struct PaletteUsage(Palette.Entry Entry, int PixelCount);

  public sealed record ConformResult(
    int SourceWidth,
    int SourceHeight,
    int Width,
    int Height,
    int OpaquePixels,
    int PixelsChanged,
    int AlphaFlattened,
    double MeanDelta,
    double MaxDelta,
    bool AspectDistorted,
    IReadOnlyList<PaletteUsage> Usage);

  /// <summary>
  /// Alpha at or above this becomes fully opaque, below it becomes fully
  /// transparent. Pixel art has no middle ground (assets/STYLE.md), and box
  /// resampling across a silhouette edge is exactly where the middle ground
  /// would otherwise be invented.
  /// </summary>
  public const int DefaultAlphaThreshold = 128;

  public static ConformResult Conform(
    string inputPath,
    string outputPath,
    Palette palette,
    int? targetWidth = null,
    int? targetHeight = null,
    int alphaThreshold = DefaultAlphaThreshold)
  {
    using Image<Rgba32> image = Image.Load<Rgba32>(inputPath);
    int sourceWidth = image.Width;
    int sourceHeight = image.Height;

    int width = targetWidth ?? sourceWidth;
    int height = targetHeight ?? sourceHeight;
    bool aspectDistorted = false;

    if (width != sourceWidth || height != sourceHeight)
    {
      // Reported rather than silently corrected: forcing a 3:4 source into a
      // square canvas is sometimes exactly what the artist wants and sometimes
      // a mistake, and the tool cannot tell which.
      aspectDistorted = Math.Abs((double)sourceWidth / sourceHeight - (double)width / height) > 0.01;
      image.Mutate(ctx => ctx.Resize(new ResizeOptions
      {
        Size = new Size(width, height),
        Sampler = KnownResamplers.Box,
        Mode = ResizeMode.Stretch,
      }));
    }

    Dictionary<Palette.Rgb, int> usage = new();
    int opaque = 0, changed = 0, alphaFlattened = 0;
    double totalDelta = 0, maxDelta = 0;

    image.ProcessPixelRows(accessor =>
    {
      for (int y = 0; y < accessor.Height; y++)
      {
        Span<Rgba32> row = accessor.GetRowSpan(y);
        for (int x = 0; x < row.Length; x++)
        {
          Rgba32 p = row[x];
          if (p.A != 0 && p.A != 255) alphaFlattened++;

          if (p.A < alphaThreshold)
          {
            row[x] = new Rgba32(0, 0, 0, 0);
            continue;
          }

          Palette.Rgb source = new(p.R, p.G, p.B);
          (Palette.Entry entry, double distance) = palette.Nearest(source);

          opaque++;
          totalDelta += distance;
          if (distance > maxDelta) maxDelta = distance;
          if (!source.Equals(entry.Color) || p.A != 255) changed++;
          usage[entry.Color] = usage.GetValueOrDefault(entry.Color) + 1;

          row[x] = new Rgba32(entry.Color.R, entry.Color.G, entry.Color.B, 255);
        }
      }
    });

    string dir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    image.SaveAsPng(outputPath);

    List<PaletteUsage> usageList = palette.Entries
      .Where(e => usage.ContainsKey(e.Color))
      .Select(e => new PaletteUsage(e, usage[e.Color]))
      .OrderByDescending(u => u.PixelCount)
      .ToList();

    return new ConformResult(
      sourceWidth, sourceHeight, width, height,
      opaque, changed, alphaFlattened,
      opaque == 0 ? 0 : totalDelta / opaque,
      maxDelta,
      aspectDistorted,
      usageList);
  }
}
