using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace UIManager
{
  public class TextManager
  {
    private SpriteFont _font;
    private Dictionary<string, List<TextElement>> textLists;

    public TextManager(SpriteFont font)
    {
      _font = font;
      textLists = new Dictionary<string, List<TextElement>>();
    }

    public void AddTextList(string name)
    {
      textLists.Add(name, new List<TextElement>());
    }
    
    public void RemoveTextList(string name)
    {
      textLists.Remove(name);
    }

    public List<TextElement> GetTextList(string name)
    {
      return textLists[name];
    }
    public void AddText(string listName, string text, Vector2 position, Color color)
    {
      textLists[listName].Add(new TextElement(text, position, color, _font));
    }

    public void RemoveText(string listName, string text)
    {
      textLists[listName].RemoveAll(t => t.Text == text);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
      foreach (var textList in textLists)
      {
        foreach (var textElement in textLists[textList.Key])
        {
          spriteBatch.DrawString(textElement.Font, textElement.Text, textElement.Position, textElement.Color);
        }
      }
    }

    public void ScrollText(string listName)
    {
      foreach (TextElement item in textLists[listName].ToList()) 
      {
        if (textLists[listName].Count > 25)
        {
          textLists[listName].RemoveAt(0);
        }
        item.Position = new Vector2(item.Position.X, item.Position.Y + 20);
      }
    }
  }
}