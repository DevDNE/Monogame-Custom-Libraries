using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.AutoBattler.GameStates;

public class PostCombatState : GameState
{
  private readonly KeyboardManager _keyboard;
  private readonly SpriteFont _font;
  private readonly GameModel _model;
  private readonly int _viewportWidth;
  private readonly int _viewportHeight;
  private readonly Action _onNextRound;
  private readonly Action _onRestart;

  private Side? _winner;
  private bool _gameOver;

  public PostCombatState(ServiceProvider sp, SpriteFont font, GameModel model, int vw, int vh, Action onNextRound, Action onRestart)
  {
    _keyboard = sp.GetService<KeyboardManager>();
    _font = font;
    _model = model;
    _viewportWidth = vw;
    _viewportHeight = vh;
    _onNextRound = onNextRound;
    _onRestart = onRestart;
  }

  public void Configure(Side? winner)
  {
    _winner = winner;
    ApplyOutcome();
  }

  public override void Entered() { IsActive = true; }
  public override void Leaving() { }
  public override void Obscuring() => IsActive = false;
  public override void Revealed() => IsActive = true;

  private void ApplyOutcome()
  {
    int damage = 5 + _model.Round * 2; // hero damage scales with round
    if (_winner == Side.Player)
    {
      _model.EnemyHeroHp -= damage;
      if (_model.EnemyHeroHp <= 0) { _model.EnemyHeroHp = 0; _gameOver = true; }
      _model.Gold += 15 + _model.Round * 2; // winnings
    }
    else if (_winner == Side.Enemy)
    {
      _model.PlayerHeroHp -= damage;
      if (_model.PlayerHeroHp <= 0) { _model.PlayerHeroHp = 0; _gameOver = true; }
      _model.Gold += 5;
    }
    else
    {
      _model.Gold += 10;
    }
    _model.Round++;
  }

  public override void Update(GameTime gameTime)
  {
    if (_gameOver)
    {
      if (_keyboard.WasKeyPressed(Keys.R)) _onRestart();
      return;
    }
    if (_keyboard.WasKeyPressed(Keys.Space) || _keyboard.WasKeyPressed(Keys.Enter)) _onNextRound();
  }

  public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
  {
    // PointClamp even though this state currently draws only text over a
    // scrim: the game ships textures now, and the next sprite anyone adds to
    // this overlay would be silently blurred. check-sprites enforces it.
    spriteBatch.Begin(samplerState: SamplerState.PointClamp);
    Primitives.DrawRectangle(spriteBatch, new Rectangle(0, 0, _viewportWidth, _viewportHeight), new Color(0, 0, 0, 180));

    string heading = _winner switch
    {
      Side.Player => "Round won!",
      Side.Enemy => "Round lost",
      _ => "Draw",
    };
    Color headingColor = _winner switch
    {
      Side.Player => new Color(120, 220, 140),
      Side.Enemy => new Color(230, 90, 100),
      _ => new Color(220, 220, 220),
    };

    Vector2 hs = _font.MeasureString(heading);
    Vector2 center = new(_viewportWidth * 0.5f, _viewportHeight * 0.5f);
    spriteBatch.DrawString(_font, heading, new Vector2(center.X - hs.X * 0.5f, center.Y - 90), headingColor);

    string hpLine = $"Player {_model.PlayerHeroHp} HP   vs   Enemy {_model.EnemyHeroHp} HP";
    Vector2 hpSz = _font.MeasureString(hpLine);
    spriteBatch.DrawString(_font, hpLine, new Vector2(center.X - hpSz.X * 0.5f, center.Y - 40), Color.White);

    string goldLine = $"Gold: {_model.Gold}";
    Vector2 gSz = _font.MeasureString(goldLine);
    spriteBatch.DrawString(_font, goldLine, new Vector2(center.X - gSz.X * 0.5f, center.Y - 10), new Color(255, 220, 120));

    if (_gameOver)
    {
      string over = _model.PlayerHeroHp <= 0 ? "You were defeated" : "You conquered the enemy";
      Vector2 oSz = _font.MeasureString(over);
      spriteBatch.DrawString(_font, over, new Vector2(center.X - oSz.X * 0.5f, center.Y + 30), Color.White);
      const string hint = "Press R to restart the game";
      Vector2 iSz = _font.MeasureString(hint);
      spriteBatch.DrawString(_font, hint, new Vector2(center.X - iSz.X * 0.5f, center.Y + 60), new Color(210, 210, 220));
    }
    else
    {
      const string hint = "Space / Enter for the next shop phase";
      Vector2 iSz = _font.MeasureString(hint);
      spriteBatch.DrawString(_font, hint, new Vector2(center.X - iSz.X * 0.5f, center.Y + 40), new Color(210, 210, 220));
    }

    spriteBatch.End();
  }
}
