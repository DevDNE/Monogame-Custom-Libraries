
using System;
using Microsoft.Xna.Framework;

namespace Utilities
{
  public static class MathHelperExtensions
  {
    // Returns the distance between two points
    public static float Distance(Vector2 point1, Vector2 point2)
    {
      return Vector2.Distance(point1, point2);
    }

    // Returns the angle in radians between two points
    public static float Angle(Vector2 point1, Vector2 point2)
    {
      return (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
    }

    // Clamps a value between a minimum and maximum range
    public static float Clamp(float value, float min, float max)
    {
      return MathHelper.Clamp(value, min, max);
    }

    // Converts degrees to radians
    public static float ToRadians(float degrees)
    {
      return MathHelper.ToRadians(degrees);
    }

    // Converts radians to degrees
    public static float ToDegrees(float radians)
    {
      return MathHelper.ToDegrees(radians);
    }

    // Returns a random float between a minimum and maximum range
    public static float RandomFloat(float min, float max)
    {
      return (float)random.NextDouble() * (max - min) + min;
    }

    // Returns a random integer between a minimum and maximum range
    public static int RandomInt(int min, int max)
    {
      return random.Next(min, max);
    }

    // Returns a random Vector2 within a specified rectangle
    public static Vector2 RandomVector2(Rectangle rectangle)
    {
      return new Vector2(RandomFloat(rectangle.Left, rectangle.Right), RandomFloat(rectangle.Top, rectangle.Bottom));
    }

    private static readonly Random random = new Random();
  }
}