using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GraphicsManager
{
  public class Sprite
{
    public string Name { get; set; }
    public Texture2D Texture { get; set; }
    public Vector2 Position { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public Sprite(string name, Texture2D texture, Vector2 position, int width, int height)
    {
      Name = name;
      Texture = texture;
      Position = position;
      Width = width;
      Height = height;
    }
    public Rectangle BoundingBox
    {
      get
      {
        return new Rectangle((int)Position.X, (int)Position.Y, Width, Height);
      }
    }
  }
}