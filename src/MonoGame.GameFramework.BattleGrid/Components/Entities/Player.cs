using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using MonoGame.GameFramework.Events;
using MonoGame.GameFramework.Input;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.BattleGrid.Components;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.BattleGrid.Components.Entities;

public class Player
{
  public const int MaxHp = 100;

  public int Hp { get; private set; } = MaxHp;
  public bool IsAlive => Hp > 0;
  public int GridCol { get; private set; } = 1;
  public int GridRow { get; private set; } = 1;

  private SpriteSheet character;
  private Texture2D _shotSheet;
  private readonly DrawManager _drawManager;
  private readonly KeyboardManager _keyboardManager;
  private readonly EventManager _eventManager;

  private List<Projectile> projectiles = new List<Projectile>();
  private Rectangle hitbox;

  public Player(ServiceProvider serviceProvider)
  {
    _drawManager = serviceProvider.GetService<DrawManager>();
    _keyboardManager = serviceProvider.GetService<KeyboardManager>();
    _eventManager = serviceProvider.GetService<EventManager>();
  }

  public void LoadContent(ContentManager content)
  {
    _shotSheet = content.Load<Texture2D>(BattleArt.Shots);
    Vector2 pos = BattleGrid.Grid.PlayerCellTopLeft(GridCol, GridRow);
    character = SpriteSheet.Static(
      content.Load<Texture2D>(BattleArt.Navi),
      new Rectangle((int)pos.X, (int)pos.Y, BattleConfig.DisplayWidth, BattleConfig.DisplayHeight),
      name: "Player");
    hitbox = new Rectangle((int)pos.X, (int)pos.Y, BattleConfig.DisplayWidth, BattleConfig.DisplayHeight);
    _drawManager.AddSprite(character);
  }

  public void UnloadContent()
  {
    if (character != null)
    {
      _drawManager.RemoveSprite(character);
      character = null;
    }
    foreach (Projectile p in projectiles) p.UnloadContent();
    projectiles.Clear();
  }

  public void Update(GameTime gameTime)
  {
    if (!IsAlive) return;
    HandleMove(Keys.W, 0, -1, "up");
    HandleMove(Keys.S, 0, 1, "down");
    HandleMove(Keys.A, -1, 0, "left");
    HandleMove(Keys.D, 1, 0, "right");
    HandleFire(gameTime);
  }

  private void HandleMove(Keys key, int dCol, int dRow, string label)
  {
    if (!_keyboardManager.WasKeyReleased(key)) return;
    int nc = GridCol + dCol;
    int nr = GridRow + dRow;
    if (nc < 0 || nc > 2 || nr < 0 || nr > 2) return;
    GridCol = nc;
    GridRow = nr;
    RefreshFromGrid();
    _eventManager.TriggerEvent("PlayerMoved", this, new GameEventArgs($"Player moved {label}"));
  }

  private void RefreshFromGrid()
  {
    Vector2 pos = BattleGrid.Grid.PlayerCellTopLeft(GridCol, GridRow);
    character.DestinationFrame = new Rectangle((int)pos.X, (int)pos.Y, BattleConfig.DisplayWidth, BattleConfig.DisplayHeight);
    hitbox = new Rectangle((int)pos.X, (int)pos.Y, BattleConfig.DisplayWidth, BattleConfig.DisplayHeight);
  }

  public void Damage(int amount)
  {
    Hp -= amount;
    if (Hp < 0) Hp = 0;
    _eventManager.TriggerEvent("PlayerHit", this, new GameEventArgs($"Player took {amount} damage"));
  }

  public void Heal(int amount)
  {
    Hp += amount;
    if (Hp > MaxHp) Hp = MaxHp;
  }

  public SpriteSheet GetCharacter() => character;

  private void HandleFire(GameTime gameTime)
  {
    if (_keyboardManager.WasKeyReleased(Keys.Space))
    {
      // Spawned at the navi's chest rather than its centre — that is where the
      // buster arm is drawn, and a shot leaving from the knees reads as a bug
      // even when the collision is identical.
      Vector2 spawn = new(
        character.Position.X + BattleConfig.DisplayWidth,
        character.Position.Y + BattleConfig.DisplayHeight * 0.45f - BattleConfig.ProjectileDisplaySize * 0.5f);
      FireProjectile(new Projectile(_drawManager, _shotSheet, BattleArt.BusterShot, spawn, new Vector2(10, 0)));
    }
    for (int i = projectiles.Count - 1; i >= 0; i--)
    {
      projectiles[i].Update(gameTime);
      int x = projectiles[i].GetHurtbox().X;
      if (x > BattleConfig.ProjectileOffscreenMaxX || x < 0)
      {
        projectiles[i].UnloadContent();
        projectiles.RemoveAt(i);
      }
    }
  }

  public void RemoveProjectileOnCollision(Projectile projectile)
  {
    projectile.UnloadContent();
    projectiles.Remove(projectile);
  }

  public void FireProjectile(Projectile projectile)
  {
    _drawManager.AddSprite(projectile.GetSprite());
    projectiles.Add(projectile);
    _eventManager.TriggerEvent("PlayerFiredProjectile", this, new GameEventArgs("Player fired a projectile"));
  }

  public List<Projectile> GetProjectiles() => projectiles;
  public Rectangle GetHitbox() => hitbox;
}
