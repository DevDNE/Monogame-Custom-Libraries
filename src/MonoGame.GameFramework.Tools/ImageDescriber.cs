using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MonoGame.GameFramework.Tools;

/// <summary>
/// Measures an image instead of guessing at it.
///
/// This is the step the pipeline never had. assets/STYLE.md documents a
/// GENERATE -> CONFORM -> GATE path, and every tool in it assumes you already
/// know the answers to the questions that decide whether the result is any
/// good: how big is this really, how many colours does it actually use, where
/// does the content sit on the canvas. Those were left to the eye, and the eye
/// is bad at them — a 640x640 PNG of 32x32 pixel art looks exactly like a
/// 640x640 PNG of 40x40 pixel art until something counts the blocks.
///
/// Everything here is a measurement, not a judgment. Nothing fails; the point
/// is to put numbers in front of a decision that was previously made on vibes.
/// </summary>
public static class ImageDescriber
{
  public readonly record struct ColorCount(Palette.Rgb Color, int Count);

  public readonly record struct Bounds(int X, int Y, int Width, int Height)
  {
    public int Right => X + Width;
    public int Bottom => Y + Height;
  }

  public sealed record Description(
    int FileWidth,
    int FileHeight,
    int BlockSize,
    int Width,
    int Height,
    IReadOnlyList<ColorCount> Colors,
    int TransparentPixels,
    IReadOnlyList<byte> AlphaValues,
    Bounds? Content)
  {
    /// <summary>True when the file is an integer upscale of a smaller grid.</summary>
    public bool IsUpscaled => BlockSize > 1;

    public bool BinaryAlpha => AlphaValues.All(a => a is 0 or 255);

    public int OpaquePixels => Colors.Sum(c => c.Count);
  }

  public static Description Describe(string path)
  {
    using Image<Rgba32> image = Image.Load<Rgba32>(path);
    return Describe(image);
  }

  public static Description Describe(Image<Rgba32> image)
  {
    int fw = image.Width, fh = image.Height;
    Rgba32[] px = ToArray(image);

    int block = DetectBlockSize(px, fw, fh);
    int w = fw / block, h = fh / block;

    Dictionary<Palette.Rgb, int> counts = new();
    HashSet<byte> alphas = new();
    int transparent = 0;
    int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;

    for (int y = 0; y < h; y++)
    {
      for (int x = 0; x < w; x++)
      {
        // Top-left of each block. Lossless: DetectBlockSize only returns a
        // size at which every block is a single flat colour.
        Rgba32 p = px[(y * block) * fw + x * block];
        alphas.Add(p.A);
        if (p.A == 0) { transparent++; continue; }

        Palette.Rgb rgb = new(p.R, p.G, p.B);
        counts[rgb] = counts.GetValueOrDefault(rgb) + 1;
        if (x < minX) minX = x;
        if (y < minY) minY = y;
        if (x > maxX) maxX = x;
        if (y > maxY) maxY = y;
      }
    }

    Bounds? content = maxX < 0
      ? null
      : new Bounds(minX, minY, maxX - minX + 1, maxY - minY + 1);

    return new Description(
      fw, fh, block, w, h,
      counts.Select(kv => new ColorCount(kv.Key, kv.Value))
            .OrderByDescending(c => c.Count)
            .ThenBy(c => c.Color.Hex, StringComparer.Ordinal)
            .ToList(),
      transparent,
      alphas.OrderBy(a => a).ToList(),
      content);
  }

  /// <summary>
  /// Largest N dividing both dimensions for which every NxN block is one flat
  /// colour — i.e. the true pixel size of art that has been scaled up for
  /// viewing. 1 when the image is already at native resolution.
  ///
  /// Reducing by this factor is information-preserving by construction, which
  /// is why callers can do it without asking: if a block were not uniform, N
  /// would not have been returned.
  /// </summary>
  public static int DetectBlockSize(Rgba32[] px, int width, int height)
  {
    for (int b = Math.Min(width, height); b >= 2; b--)
    {
      if (width % b != 0 || height % b != 0) continue;
      if (IsBlockUniform(px, width, height, b)) return b;
    }
    return 1;
  }

  static bool IsBlockUniform(Rgba32[] px, int width, int height, int b)
  {
    for (int by = 0; by < height; by += b)
    {
      for (int bx = 0; bx < width; bx += b)
      {
        Rgba32 first = px[by * width + bx];
        for (int y = by; y < by + b; y++)
        {
          int row = y * width;
          for (int x = bx; x < bx + b; x++)
            if (!px[row + x].Equals(first)) return false;
        }
      }
    }
    return true;
  }

  /// <summary>Reduce an upscaled image back to its native grid. No-op at 1x.</summary>
  public static Image<Rgba32> Reduce(Image<Rgba32> image, int blockSize)
  {
    if (blockSize <= 1) return image.Clone();

    Rgba32[] px = ToArray(image);
    int w = image.Width / blockSize, h = image.Height / blockSize;
    Image<Rgba32> reduced = new(w, h);
    for (int y = 0; y < h; y++)
      for (int x = 0; x < w; x++)
        reduced[x, y] = px[(y * blockSize) * image.Width + x * blockSize];
    return reduced;
  }

  public static Rgba32[] ToArray(Image<Rgba32> image)
  {
    Rgba32[] px = new Rgba32[image.Width * image.Height];
    image.CopyPixelDataTo(px);
    return px;
  }
}
