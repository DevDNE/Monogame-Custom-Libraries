using System.Collections.Generic;
using Graphics;

namespace Managers;
public class UIManager
{
  private DrawManager _drawManager;
  private Dictionary<string, List<SpriteSheet>> uiGroups = new Dictionary<string, List<SpriteSheet>>();

  public void AddUIElement(string group, SpriteSheet uiElement)
  {
    if (!uiGroups.ContainsKey(group))
    {
      uiGroups[group] = new List<SpriteSheet>();
    }

    uiGroups[group].Add(uiElement);
    _drawManager.AddSprite(uiElement);
  }

  public void UpdateUIElement(string group, SpriteSheet uiElement)
  {
    if (uiGroups.ContainsKey(group))
    {
      uiGroups[group].Remove(uiElement);
      _drawManager.RemoveSprite(uiElement);
      uiGroups[group].Add(uiElement);
      _drawManager.AddSprite(uiElement);
    }
  }

  public void RemoveUIElement(string group, SpriteSheet uiElement)
  {
    if (uiGroups.ContainsKey(group))
    {
      uiGroups[group].Remove(uiElement);
      _drawManager.RemoveSprite(uiElement);
    }
  }

  public void LoadContent(DrawManager drawManager)
  {
    _drawManager = drawManager;
  }
}