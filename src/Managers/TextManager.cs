using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Graphics;

namespace Managers;
public class TextManager
{
  private SpriteFont _font;
  private Dictionary<string, List<TextElement>> textGroups = new Dictionary<string, List<TextElement>>();

  public List<TextElement> GetTextGroups(string name)
  {
    return textGroups[name];
  }
  public void AddText(string group, string text, Vector2 position, Color color)
  {
    if (!textGroups.ContainsKey(group))
    {
      textGroups.Add(group, new List<TextElement>());
    }

    textGroups[group].Add(new TextElement(text, position, color, _font));
  }

  public void UpdateText(string groupName, string oldText, string newText, Vector2 position, Color color)
  {
    TextElement textElement = textGroups[groupName].Find(t => t.Text == oldText);
    if (textElement != null)
    {
      textElement.Text = newText;
      textElement.Position = position;
      textElement.Color = color;
    }
  }

  public void RemoveText(string groupName, string text)
  {
    textGroups[groupName].RemoveAll(t => t.Text == text);
  }

  public void LoadContent(SpriteFont font)
  {
    _font = font;
  }

  public void Draw(SpriteBatch spriteBatch)
  {
    foreach (var textGroup in textGroups)
    {
      foreach (var textElement in textGroups[textGroup.Key])
      {
        spriteBatch.DrawString(textElement.Font, textElement.Text, textElement.Position, textElement.Color);
      }
    }
  }

  public void ClearGroup(string groupName)
  {
    if (textGroups.ContainsKey(groupName))
    {
      textGroups[groupName].Clear();
    }
  }

  public void ScrollText(string groupName, int pixelsBetweenLines, int maxLines)
  {
    foreach (TextElement item in textGroups[groupName].ToList()) 
    {
      if (textGroups[groupName].Count > maxLines)
      {
        textGroups[groupName].RemoveAt(0);
      }
      item.Position = new Vector2(item.Position.X, item.Position.Y + pixelsBetweenLines);
    }
  }
}