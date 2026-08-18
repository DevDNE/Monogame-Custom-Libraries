using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGame.GameFramework.Events;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.BattleGrid.Components;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.BattleGrid.Components.Entities;

public class EnemyPlayer
{
  public const int MaxHp = 100;
  public const float ActionInterval = 1.5f;

  private enum Action { Move, Shoot }
  private enum Pattern { Single, Wide }

  public int Hp { get; private set; } = MaxHp;
  public bool IsAlive => Hp > 0;
  public int GridCol { get; private set; } = 1;
  public int GridRow { get; private set; } = 1;

  private SpriteSheet character;
  private Texture2D _shotSheet;
  private readonly DrawManager _drawManager;
  private readonly EventManager _eventManager;
  private readonly Random _random = new();

  private Rectangle hitbox;
  private readonly List<Projectile> _projectiles = new();

  private Action _nextAction = Action.Shoot;
  private Pattern _nextPattern = Pattern.Single;
  private float _actionTimer = ActionInterval;

  public EnemyPlayer(ServiceProvider serviceProvider)
  {
    _drawManager = serviceProvider.GetService<DrawManager>();
    _eventManager = serviceProvider.GetService<EventManager>();
  }

  public void LoadContent(ContentManager content)
  {
    _shotSheet = content.Load<Texture2D>(BattleArt.Shots);
    Vector2 pos = Grid.EnemyCellTopLeft(GridCol, GridRow);
    character = SpriteSheet.Static(
      content.Load<Texture2D>(BattleArt.Virus),
      new Rectangle((int)pos.X, (int)pos.Y, BattleConfig.DisplayWidth, BattleConfig.DisplayHeight),
      name: "Enemy");
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
    foreach (Projectile p in _projectiles) p.UnloadContent();
    _projectiles.Clear();
  }

  public void Update(GameTime gameTime)
  {
    if (!IsAlive) return;

    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
    _actionTimer -= dt;
    if (_actionTimer <= 0f)
    {
      PerformNextAction();
      _actionTimer = ActionInterval;
    }

    for (int i = _projectiles.Count - 1; i >= 0; i--)
    {
      _projectiles[i].Update(gameTime);
      int x = _projectiles[i].GetHurtbox().X;
      if (x < 0 || x > BattleConfig.ProjectileOffscreenMaxX)
      {
        _projectiles[i].UnloadContent();
        _projectiles.RemoveAt(i);
      }
    }
  }

  public void Damage(int amount)
  {
    Hp -= amount;
    if (Hp < 0) Hp = 0;
    _eventManager.TriggerEvent("EnemyHit", this, new GameEventArgs($"Enemy took {amount} damage"));
  }

  private void PerformNextAction()
  {
    if (_nextAction == Action.Move)
    {
      MoveToRandomCell();
      _nextAction = Action.Shoot;
    }
    else
    {
      FirePattern(_nextPattern);
      _nextPattern = _nextPattern == Pattern.Single ? Pattern.Wide : Pattern.Single;
      _nextAction = Action.Move;
    }
  }

  private void MoveToRandomCell()
  {
    int newCol, newRow;
    do
    {
      newCol = _random.Next(3);
      newRow = _random.Next(3);
    } while (newCol == GridCol && newRow == GridRow);

    GridCol = newCol;
    GridRow = newRow;
    Vector2 pos = Grid.EnemyCellTopLeft(GridCol, GridRow);
    character.DestinationFrame = new Rectangle((int)pos.X, (int)pos.Y, BattleConfig.DisplayWidth, BattleConfig.DisplayHeight);
    hitbox = new Rectangle((int)pos.X, (int)pos.Y, BattleConfig.DisplayWidth, BattleConfig.DisplayHeight);
  }

  private void FirePattern(Pattern p)
  {
    if (p == Pattern.Single) FireSingleShot();
    else FireWideShot();
  }

  private void FireSingleShot()
  {
    float y = character.Position.Y + BattleConfig.DisplayHeight * 0.45f - BattleConfig.ProjectileDisplaySize * 0.5f;
    SpawnProjectile(new Vector2(character.Position.X - BattleConfig.ProjectileDisplaySize, y), BattleArt.EnemyShot);
    _eventManager.TriggerEvent("EnemyFiredProjectile", this, new GameEventArgs("Enemy fired single shot"));
  }

  private void FireWideShot()
  {
    for (int row = 0; row < 3; row++)
    {
      float y = Grid.RowCenterY(row) - BattleConfig.ProjectileDisplaySize * 0.5f;
      float x = BattleConfig.EnemyBoardX - BattleConfig.ProjectileDisplaySize;
      SpawnProjectile(new Vector2(x, y), BattleArt.EnemyWideShot);
    }
    _eventManager.TriggerEvent("EnemyFiredProjectile", this, new GameEventArgs("Enemy fired wide shot"));
  }

  private void SpawnProjectile(Vector2 pos, Rectangle frame)
  {
    Projectile p = new(_drawManager, _shotSheet, frame, pos, new Vector2(-8, 0));
    _drawManager.AddSprite(p.GetSprite());
    _projectiles.Add(p);
  }

  public Rectangle GetHitbox() => hitbox;
  public List<Projectile> GetProjectiles() => _projectiles;

  public void RemoveProjectileOnCollision(Projectile projectile)
  {
    projectile.UnloadContent();
    _projectiles.Remove(projectile);
  }
}
