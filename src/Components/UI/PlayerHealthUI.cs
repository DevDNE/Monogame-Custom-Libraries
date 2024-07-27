using Microsoft.Xna.Framework;
using Graphics;
using Microsoft.Extensions.DependencyInjection;
using Managers;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace UI;
public class PlayerHealthUI
{
  private SpriteSheet _healthBar;
  private UIManager _uiManager;

  public PlayerHealthUI(ServiceProvider serviceProvider)
  {
    _uiManager = serviceProvider.GetService<UIManager>();
  }

  public void LoadContent(ContentManager content)
  {
    // TODO: Change this to the actual health bar sprite
    _healthBar = new SpriteSheet("projectileSprite", content.Load<Texture2D>("gfx/Player_Idle"), 
      new Vector2(200, 270), 35, 40, 
      new Rectangle[] { new(0, 0, 35, 40), }, 
      new Rectangle(100, 100, 35, 40), 0.5f, 0);
    _uiManager.AddUIElement("player", _healthBar);
  }

  public void UnloadContent()
  {
    if (_healthBar != null)
    {
      _healthBar.Texture.Dispose();
      _healthBar.Texture = null;
    }
    _uiManager.RemoveUIElement("player", _healthBar);
  }
}