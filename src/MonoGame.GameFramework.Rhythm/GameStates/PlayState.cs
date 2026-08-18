using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.GameFramework.Audio;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.Rhythm.GameStates;

public class PlayState : GameState
{
  // Chart scrolls at NoteSpeed pixels/second. A note spawns when songTime
  // reaches (note.time - leadTime) and reaches the target line at note.time.
  private const float NoteSpeed = 500f;
  private const float LeadTimeSeconds = 1.6f; // time from spawn to target line
  private const int NoteWidth = 100;
  private const int NoteHeight = 24;
  private const int LaneWidth = 100;
  private const float PerfectWindow = 0.05f;
  private const float HitWindow = 0.15f;
  private const float MissPastWindow = 0.2f; // time past target before we count miss

  private static readonly Keys[] LaneKeys = { Keys.D, Keys.F, Keys.J, Keys.K };
  private static readonly Color[] LaneColors =
  {
    new Color(255, 120, 120),
    new Color(255, 210, 120),
    new Color(120, 200, 255),
    new Color(200, 120, 255),
  };

  private readonly KeyboardManager _keyboard;
  private readonly SoundManager _sound;
  private readonly SpriteFont _font;
  private readonly int _viewportWidth;
  private readonly int _viewportHeight;

  // Parallel arrays indexed by position in Chart.Notes:
  //   _consumed[i] = true once note i is resolved (hit / miss / offscreen).
  private bool[] _consumed;
  private float[] _flashRemaining; // per-lane hit-flash timer (seconds)
  private float _songTime;
  private int _score;
  private int _combo;
  private int _maxCombo;
  private int _hits;
  private int _perfects;
  private int _misses;
  private bool _clickLoaded;
  private bool _songEnded;

  private readonly RhythmArt _art;

  public PlayState(ServiceProvider sp, SpriteFont font, RhythmArt art, int vw, int vh)
  {
    _art = art;
    _keyboard = sp.GetService<KeyboardManager>();
    _sound = sp.GetService<SoundManager>();
    _font = font;
    _viewportWidth = vw;
    _viewportHeight = vh;
  }

  public override void Entered()
  {
    if (!_clickLoaded) { _sound.LoadSoundEffect("audio/click"); _clickLoaded = true; }
    StartFresh();
    IsActive = true;
  }

  public override void Leaving() { }
  public override void Obscuring() => IsActive = false;
  public override void Revealed() => IsActive = true;

  private void StartFresh()
  {
    _consumed = new bool[Chart.Notes.Length];
    _flashRemaining = new float[Chart.LaneCount];
    _songTime = 0f;
    _score = 0;
    _combo = 0;
    _maxCombo = 0;
    _hits = 0;
    _perfects = 0;
    _misses = 0;
    _songEnded = false;
  }

  public override void Update(GameTime gameTime)
  {
    if (_keyboard.WasKeyPressed(Keys.R)) { StartFresh(); return; }
    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

    if (!_songEnded)
    {
      _songTime += dt;
      if (_songTime > Chart.Duration + LeadTimeSeconds) _songEnded = true;
    }

    // Decay hit flashes
    for (int i = 0; i < _flashRemaining.Length; i++)
      if (_flashRemaining[i] > 0f) _flashRemaining[i] = MathF.Max(0f, _flashRemaining[i] - dt);

    HandleKeyPresses();
    MarkMissedNotes();
  }

  private void HandleKeyPresses()
  {
    for (int lane = 0; lane < Chart.LaneCount; lane++)
    {
      if (!_keyboard.WasKeyPressed(LaneKeys[lane])) continue;

      int bestIdx = -1;
      float bestAbsDelta = float.MaxValue;
      for (int i = 0; i < Chart.Notes.Length; i++)
      {
        if (_consumed[i]) continue;
        if (Chart.Notes[i].lane != lane) continue;
        float delta = Chart.Notes[i].time - _songTime; // positive = note hasn't reached line yet
        float abs = MathF.Abs(delta);
        if (abs > HitWindow) continue;
        if (abs < bestAbsDelta) { bestAbsDelta = abs; bestIdx = i; }
      }
      if (bestIdx >= 0)
      {
        _consumed[bestIdx] = true;
        _flashRemaining[lane] = 0.18f;
        _sound.PlaySoundEffect("audio/click");
        if (bestAbsDelta <= PerfectWindow) { _score += 100; _perfects++; }
        else { _score += 50; _hits++; }
        _combo++;
        if (_combo > _maxCombo) _maxCombo = _combo;
      }
    }
  }

  private void MarkMissedNotes()
  {
    for (int i = 0; i < Chart.Notes.Length; i++)
    {
      if (_consumed[i]) continue;
      if (_songTime - Chart.Notes[i].time > MissPastWindow)
      {
        _consumed[i] = true;
        _misses++;
        _combo = 0;
      }
    }
  }

  public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
  {
    spriteBatch.Begin(samplerState: SamplerState.PointClamp);

    // Horizon first, then the lanes over it. The lane tile is deliberately
    // mostly transparent, so this order is what lets the sun read through the
    // board instead of being hidden behind it.
    PixelDraw.Sprite(spriteBatch, _art.Horizon, 0, 0, RhythmArt.Scale);

    int boardWidth = LaneWidth * Chart.LaneCount;
    int boardX = (_viewportWidth - boardWidth) / 2;
    int targetY = _viewportHeight - 120;

    for (int lane = 0; lane < Chart.LaneCount; lane++)
    {
      PixelDraw.Tile(spriteBatch, _art.Lane,
        new Rectangle(boardX + lane * LaneWidth, 0, LaneWidth, _viewportHeight), RhythmArt.Scale);
    }

    // Receptors: hollow frames, so a note crossing one stays fully visible at
    // the exact moment the player is timing.
    for (int lane = 0; lane < Chart.LaneCount; lane++)
    {
      PixelDraw.Sprite(spriteBatch, _art.Receptor,
        boardX + lane * LaneWidth, targetY - _art.Receptor.Height * RhythmArt.Scale / 2,
        RhythmArt.Scale);
    }

    // Lane flash on a hit. Still a tinted rectangle rather than a sprite: it is
    // a full-lane wash that fades over 180ms, and alpha is the whole effect —
    // which is the one thing a binary-alpha sprite cannot do.
    for (int lane = 0; lane < Chart.LaneCount; lane++)
    {
      if (_flashRemaining[lane] <= 0f) continue;
      float alpha = _flashRemaining[lane] / 0.18f;
      Color c = LaneColors[lane] * alpha * 0.5f;
      Primitives.DrawRectangle(spriteBatch, new Rectangle(boardX + lane * LaneWidth, targetY - 60, LaneWidth - 2, 120), c);
    }

    // Notes
    for (int i = 0; i < Chart.Notes.Length; i++)
    {
      if (_consumed[i]) continue;
      (float time, int lane) = Chart.Notes[i];
      float delta = time - _songTime; // seconds until hit
      if (delta > LeadTimeSeconds) continue;           // not spawned yet
      if (delta < -MissPastWindow) continue;           // already missed & cleared
      float y = targetY - delta * NoteSpeed;
      // Struck art inside the hit window, approach art outside it. The colour
      // flip is near-complementary and lands on the frame the timing does, so
      // the player reads it before they read anything else.
      Rectangle source = System.Math.Abs(delta) <= HitWindow
        ? RhythmArt.NoteStruck
        : RhythmArt.NoteApproach;
      PixelDraw.Frame(spriteBatch, _art.Notes, source,
        boardX + lane * LaneWidth + (LaneWidth - NoteWidth) / 2,
        (int)y - NoteHeight / 2,
        RhythmArt.Scale);
    }

    // Lane key labels below the line
    for (int lane = 0; lane < Chart.LaneCount; lane++)
    {
      string label = LaneKeys[lane].ToString();
      Vector2 sz = _font.MeasureString(label);
      spriteBatch.DrawString(_font, label,
        new Vector2(boardX + lane * LaneWidth + (LaneWidth - sz.X) * 0.5f, targetY + 14),
        new Color(200, 200, 215));
    }

    // HUD
    spriteBatch.DrawString(_font, $"Score {_score}", new Vector2(20, 14), Color.White);
    spriteBatch.DrawString(_font, $"Combo {_combo}", new Vector2(20, 44), _combo > 0 ? new Color(255, 220, 120) : new Color(150, 150, 170));
    string timeStr = _songTime < Chart.Duration ? $"{_songTime:0.0}s / {Chart.Duration:0}s" : "Song end";
    Vector2 tSz = _font.MeasureString(timeStr);
    spriteBatch.DrawString(_font, timeStr, new Vector2(_viewportWidth - tSz.X - 20, 14), new Color(200, 200, 215));

    if (_songEnded) DrawFinal(spriteBatch);

    const string hint = "D / F / J / K hit lanes   R restart   Esc quit";
    Vector2 hs = _font.MeasureString(hint);
    spriteBatch.DrawString(_font, hint, new Vector2(_viewportWidth * 0.5f - hs.X * 0.5f, _viewportHeight - 30), new Color(180, 180, 200));

    spriteBatch.End();
  }

  private void DrawFinal(SpriteBatch spriteBatch)
  {
    Primitives.DrawRectangle(spriteBatch, new Rectangle(0, 0, _viewportWidth, _viewportHeight), new Color(0, 0, 0, 160));
    string heading = "Song complete";
    string line1 = $"Score {_score}  /  Max combo {_maxCombo}";
    string line2 = $"Perfect {_perfects}   Hit {_hits}   Miss {_misses}";
    const string hint = "Press R to retry";
    Vector2 hSz = _font.MeasureString(heading);
    Vector2 l1 = _font.MeasureString(line1);
    Vector2 l2 = _font.MeasureString(line2);
    Vector2 iSz = _font.MeasureString(hint);
    Vector2 c = new(_viewportWidth * 0.5f, _viewportHeight * 0.5f);
    spriteBatch.DrawString(_font, heading, new Vector2(c.X - hSz.X * 0.5f, c.Y - 80), Color.White);
    spriteBatch.DrawString(_font, line1, new Vector2(c.X - l1.X * 0.5f, c.Y - 30), new Color(255, 220, 120));
    spriteBatch.DrawString(_font, line2, new Vector2(c.X - l2.X * 0.5f, c.Y + 4), new Color(200, 210, 230));
    spriteBatch.DrawString(_font, hint,  new Vector2(c.X - iSz.X * 0.5f, c.Y + 50), new Color(210, 210, 220));
  }
}
