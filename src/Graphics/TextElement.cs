using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphics {
  public class TextElement
  {
      public string Text { get; set; }
      public Vector2 Position { get; set; }
      public Color Color { get; set; }
      public SpriteFont Font { get; set; }

      public TextElement(string text, Vector2 position, Color color, SpriteFont font)
      {
          Text = text;
          Position = position;
          Color = color;
          Font = font;
      }
  }
}