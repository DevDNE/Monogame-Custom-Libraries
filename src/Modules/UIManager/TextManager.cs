using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace UIManager
{
  public class TextManager
  {
    private SpriteFont _font;
    private List<TextElement> textElements;

    public TextManager(SpriteFont font)
    {
      _font = font;
      textElements = new List<TextElement>();
    }
    public void AddText(string text, Vector2 position, Color color)
    {
      textElements.Add(new TextElement(text, position, color, _font));
    }

    public void RemoveText(string text)
    {
      textElements.RemoveAll(t => t.Text == text);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
      foreach (var textElement in textElements)
      {
        spriteBatch.DrawString(textElement.Font, textElement.Text, textElement.Position, textElement.Color);
      }
    }

    public void ScrollText()
    {
      foreach (TextElement item in textElements.ToList()) 
      {
        if (textElements.Count > 25)
        {
          textElements.RemoveAt(0);
        }
        item.Position = new Vector2(item.Position.X, item.Position.Y + 20);
      }
    }
  }
}