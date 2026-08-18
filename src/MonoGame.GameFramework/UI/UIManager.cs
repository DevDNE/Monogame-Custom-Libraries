using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.UI;
public class UIManager
{
  private readonly MouseManager _mouseManager;
  private readonly Dictionary<string, List<SpriteSheet>> uiGroups = new();
  // Groups stack in the order they were first created — the most recently
  // created group sits on top. Hit-testing used to walk Dictionary.Values,
  // whose order is not defined, so two overlapping elements in different
  // groups resolved arbitrarily. Kept even when a group empties, so removing
  // every element from a group and re-adding does not silently restack it.
  private readonly List<string> _groupOrder = new();
  private readonly Dictionary<SpriteSheet, Action> clickHandlers = new();

  public SpriteSheet FocusedElement { get; private set; }
  public SpriteSheet HoveredElement { get; private set; }

  public int ElementCount
  {
    get
    {
      int total = 0;
      foreach (List<SpriteSheet> list in uiGroups.Values) total += list.Count;
      return total;
    }
  }

  public UIManager(MouseManager mouseManager)
  {
    _mouseManager = mouseManager;
  }

  /// <summary>Groups paint and hit-test in creation order; the newest group is on top.</summary>
  public IReadOnlyList<string> GroupOrder => _groupOrder;

  public void AddUIElement(string group, SpriteSheet uiElement)
  {
    if (!uiGroups.ContainsKey(group))
    {
      uiGroups[group] = new List<SpriteSheet>();
      _groupOrder.Add(group);
    }
    uiGroups[group].Add(uiElement);
  }

  public void RemoveUIElement(string group, SpriteSheet uiElement)
  {
    if (uiGroups.TryGetValue(group, out List<SpriteSheet> list))
    {
      list.Remove(uiElement);
    }
    clickHandlers.Remove(uiElement);
    if (FocusedElement == uiElement) FocusedElement = null;
    if (HoveredElement == uiElement) HoveredElement = null;
  }

  public void OnClick(SpriteSheet element, Action handler)
  {
    clickHandlers[element] = handler;
  }

  public void RemoveClickHandler(SpriteSheet element)
  {
    clickHandlers.Remove(element);
  }

  public void SetFocus(SpriteSheet element) => FocusedElement = element;
  public void ClearFocus() => FocusedElement = null;

  /// <summary>
  /// Topmost element under <paramref name="position"/>, or null. Walks groups
  /// newest-first and, within a group, most-recently-added first — so the
  /// element a player sees on top is the one that answers the click.
  /// </summary>
  public SpriteSheet GetElementAt(Vector2 position)
  {
    for (int g = _groupOrder.Count - 1; g >= 0; g--)
    {
      if (!uiGroups.TryGetValue(_groupOrder[g], out List<SpriteSheet> group)) continue;
      for (int i = group.Count - 1; i >= 0; i--)
      {
        if (group[i].DestinationFrame.Contains(position)) return group[i];
      }
    }
    return null;
  }

  public void Update(GameTime gameTime)
  {
    Vector2 mouse = _mouseManager.GetMousePosition();
    HoveredElement = GetElementAt(mouse);

    if (_mouseManager.WasLeftMouseButtonPressed())
    {
      if (HoveredElement != null)
      {
        FocusedElement = HoveredElement;
        if (clickHandlers.TryGetValue(HoveredElement, out Action handler)) handler();
      }
      else
      {
        FocusedElement = null;
      }
    }
  }
}
