using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.GameFramework.Text;
public class TextManager
{
  private SpriteFont _font;
  private readonly List<TextElement> _elements = new();
  private int _nextId = 1;

  /// <summary>Registered elements, in insertion order. Inspection seam for tests and debugging.</summary>
  public IReadOnlyList<TextElement> Elements => _elements;

  /// <summary>The element behind a handle, or null if it has been removed.</summary>
  public TextElement Find(TextHandle handle) => FindById(handle.Id);

  /// <summary>
  /// The font <see cref="Draw"/> would use: the element's own if it captured one,
  /// otherwise whatever the manager has loaded now. Exposed because the fallback
  /// is the fix for text registered before LoadContent, and asserting it through
  /// Draw would need a GraphicsDevice.
  /// </summary>
  public SpriteFont ResolveFont(TextElement element) => element?.Font ?? _font;

  public void LoadContent(SpriteFont font)
  {
    _font = font;
  }

  public TextHandle AddText(string group, string text, Vector2 position, Color color)
  {
    int id = _nextId++;
    _elements.Add(new TextElement
    {
      Id = id,
      Group = group,
      Text = text,
      Position = position,
      Color = color,
      Font = _font,
    });
    return new TextHandle(id);
  }

  public void UpdateText(TextHandle handle, string newText, Vector2 position, Color color)
  {
    TextElement el = FindById(handle.Id);
    if (el == null) return;
    el.Text = newText;
    el.Position = position;
    el.Color = color;
  }

  public void SetText(TextHandle handle, string newText)
  {
    TextElement el = FindById(handle.Id);
    if (el != null) el.Text = newText;
  }

  public void RemoveText(TextHandle handle)
  {
    _elements.RemoveAll(e => e.Id == handle.Id);
  }

  public void ClearGroup(string groupName)
  {
    _elements.RemoveAll(e => e.Group == groupName);
  }

  public void Draw(SpriteBatch spriteBatch)
  {
    foreach (TextElement el in _elements)
    {
      // Fall back to the manager's current font. AddText captures whatever font
      // was loaded at the time, which is null for anything registered before
      // LoadContent — and LoadContent did not backfill, so those elements kept
      // a null font forever and threw here, one frame away from the call that
      // actually caused it.
      SpriteFont font = ResolveFont(el);
      if (font == null || el.Text == null) continue;
      spriteBatch.DrawString(font, el.Text, el.Position, el.Color);
    }
  }

  public void ScrollText(string groupName, int pixelsBetweenLines, int maxLines)
  {
    int groupCount = 0;
    foreach (TextElement el in _elements) if (el.Group == groupName) groupCount++;

    while (groupCount > maxLines)
    {
      int idx = _elements.FindIndex(e => e.Group == groupName);
      if (idx < 0) break;
      _elements.RemoveAt(idx);
      groupCount--;
    }

    foreach (TextElement el in _elements)
    {
      if (el.Group == groupName)
        el.Position = new Vector2(el.Position.X, el.Position.Y + pixelsBetweenLines);
    }
  }

  private TextElement FindById(int id)
  {
    foreach (TextElement el in _elements) if (el.Id == id) return el;
    return null;
  }
}
