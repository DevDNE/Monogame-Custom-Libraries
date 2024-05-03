using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GraphicsManager
{
  public class SpriteSheet
  {
    public string Name { get; set; }
    public Texture2D Texture { get; set; }
    public Vector2 Position { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public Rectangle[] Frames { get; set; }
    public float FrameInterval { get; set; }
    public int CurrentFrame { get; set; }
    private float elapsedTime;

    public SpriteSheet(
      string name,
      Texture2D spriteSheet,
      Vector2 position,
      int width, int height,
      Rectangle[] frames,
      float frameInterval,
      int currentFrame)
    {
      Name = name;
      Texture = spriteSheet;
      Position = position;
      Width = width;
      Height = height;
      Frames = frames;
      FrameInterval = frameInterval;
      CurrentFrame = currentFrame;
    }

    public void Update(GameTime gameTime)
    {
      elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

      if (elapsedTime >= FrameInterval)
      {
        CurrentFrame = (CurrentFrame + 1) % Frames.Length;
        elapsedTime = 0;
      }
    }
  }
}



