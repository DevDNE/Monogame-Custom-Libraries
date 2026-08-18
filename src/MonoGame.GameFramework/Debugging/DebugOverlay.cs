using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.GameFramework.Events;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Pooling;
using MonoGame.GameFramework.Rendering;
using MonoGame.GameFramework.Timing;
using MonoGame.GameFramework.UI;

namespace MonoGame.GameFramework.Debugging;

/// <summary>
/// Tilde-toggled debug overlay. Lives as a DI-registered singleton (not a
/// GameState) so it sits outside the state stack and survives transitions.
///
/// Keys (only while <see cref="Enabled"/>):
///   ~         toggle overlay visibility
///   Space     toggle pause of the simulation (via <see cref="ShouldSkipUpdate"/>)
///   .         advance one frame while paused
///
/// Integration in <c>Game1</c>:
/// <code>
///   _overlay.Update(gameTime);
///   if (!_overlay.ShouldSkipUpdate) _gameStateManager.Update(gameTime);
///   // ... draw game ...
///   _overlay.Draw(spriteBatch, gameTime);
/// </code>
/// </summary>
public sealed class DebugOverlay
{
  private const int FrameSampleSize = 60;
  private const int EventTailSize = 12;

  private readonly KeyboardManager _keyboard;
  private readonly EventManager _events;
  private readonly GameStateManager _states;
  private readonly UIManager _ui;
  private readonly TimerManager _timers;

  private readonly double[] _frameSamples = new double[FrameSampleSize];
  private int _frameSampleIndex;
  private int _frameSampleCount;

  private readonly Queue<string> _eventTail = new();
  private readonly Dictionary<string, Func<string>> _watches = new();
  private readonly List<string> _watchOrder = new();

  private long _gcEnabledBaseline;
  private int _gen0Baseline, _gen1Baseline, _gen2Baseline;
  private bool _stepRequested;
  private bool _shouldSkipUpdate;
  private bool _fontWarningLogged;

  public SpriteFont Font { get; private set; }
  public bool Enabled { get; private set; }
  public bool IsPaused { get; private set; }
  public bool ShouldSkipUpdate => _shouldSkipUpdate;
  public IReadOnlyCollection<string> RecentEvents => _eventTail;
  public IReadOnlyList<string> WatchKeys => _watchOrder;

  public DebugOverlay(KeyboardManager keyboard, EventManager events, GameStateManager states, UIManager ui, TimerManager timers)
  {
    _keyboard = keyboard ?? throw new ArgumentNullException(nameof(keyboard));
    _events = events ?? throw new ArgumentNullException(nameof(events));
    _states = states ?? throw new ArgumentNullException(nameof(states));
    _ui = ui ?? throw new ArgumentNullException(nameof(ui));
    _timers = timers ?? throw new ArgumentNullException(nameof(timers));
    _events.AnyEvent += OnAnyEvent;
  }

  public void SetFont(SpriteFont font) => Font = font;

  // ---- State machine (public so tests can drive without a real keyboard) ----

  public void ToggleEnabled()
  {
    Enabled = !Enabled;
    if (Enabled) SnapshotGcBaseline();
    else { IsPaused = false; _stepRequested = false; }
  }

  public void TogglePause()
  {
    if (!Enabled) return;
    IsPaused = !IsPaused;
    if (!IsPaused) _stepRequested = false;
  }

  public void RequestStep()
  {
    if (Enabled && IsPaused) _stepRequested = true;
  }

  // ---- Watches ----

  public void AddWatch(string name, Func<string> valueFn)
  {
    if (string.IsNullOrEmpty(name)) throw new ArgumentException("name required", nameof(name));
    if (valueFn == null) throw new ArgumentNullException(nameof(valueFn));
    if (!_watches.ContainsKey(name)) _watchOrder.Add(name);
    _watches[name] = valueFn;
  }

  public bool RemoveWatch(string name)
  {
    if (!_watches.Remove(name)) return false;
    _watchOrder.Remove(name);
    return true;
  }

  public void AddPooledSetWatch<T>(string name, PooledEntitySet<T> set) where T : class
    => AddWatch(name, () => $"{set.Count} live / {set.Pool.AvailableCount} pooled");

  // ---- Frame loop ----

  public void Update(GameTime gameTime)
  {
    HandleKeys();
    if (Enabled) RecordFrame(gameTime);

    bool wasStepping = _stepRequested;
    _stepRequested = false;
    _shouldSkipUpdate = Enabled && IsPaused && !wasStepping;
  }

  private void HandleKeys()
  {
    if (_keyboard.WasKeyPressed(Keys.OemTilde)) ToggleEnabled();
    if (Enabled)
    {
      if (_keyboard.WasKeyPressed(Keys.Space)) TogglePause();
      if (_keyboard.WasKeyPressed(Keys.OemPeriod)) RequestStep();
    }
  }

  private void RecordFrame(GameTime gameTime)
  {
    _frameSamples[_frameSampleIndex] = gameTime.ElapsedGameTime.TotalMilliseconds;
    _frameSampleIndex = (_frameSampleIndex + 1) % FrameSampleSize;
    if (_frameSampleCount < FrameSampleSize) _frameSampleCount++;
  }

  private void SnapshotGcBaseline()
  {
    _gcEnabledBaseline = GC.GetTotalMemory(false);
    _gen0Baseline = GC.CollectionCount(0);
    _gen1Baseline = GC.CollectionCount(1);
    _gen2Baseline = GC.CollectionCount(2);
  }

  public double AverageFrameMs()
  {
    if (_frameSampleCount == 0) return 0;
    double sum = 0;
    for (int i = 0; i < _frameSampleCount; i++) sum += _frameSamples[i];
    return sum / _frameSampleCount;
  }

  public double AverageFps()
  {
    double ms = AverageFrameMs();
    return ms <= 0 ? 0 : 1000.0 / ms;
  }

  // ---- Event tail ----

  private void OnAnyEvent(string name, object sender, GameEventArgs args)
  {
    string message = string.IsNullOrEmpty(args?.Message) ? name : $"{name}: {args.Message}";
    _eventTail.Enqueue(message);
    while (_eventTail.Count > EventTailSize) _eventTail.Dequeue();
  }

  // ---- Draw ----

  public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
  {
    if (!Enabled) return;
    if (Font == null)
    {
      if (!_fontWarningLogged)
      {
        Debug.WriteLine("[DebugOverlay] Draw called before SetFont(...). Overlay text will not render.");
        _fontWarningLogged = true;
      }
      return;
    }

    // PointClamp, not the default LinearClamp. The overlay draws only text and
    // solid rectangles today, but it draws them inside nine games that ship
    // pixel art, and the first sprite added here would otherwise be silently
    // blurred. This is the rule check-sprites enforces everywhere it can see.
    spriteBatch.Begin(samplerState: SamplerState.PointClamp);

    // Main panel on the top-left
    Rectangle panel = new(8, 8, 360, 240);
    Primitives.DrawRectangle(spriteBatch, panel, new Color(0, 0, 0, 180));

    int x = panel.X + 8;
    int y = panel.Y + 8;
    const int lineHeight = 18;

    Color headerColor = new(255, 220, 120);
    Color labelColor = new(220, 220, 230);
    Color mutedColor = new(170, 170, 190);

    spriteBatch.DrawString(Font, $"DEBUG  ~ toggle  Space pause  . step", new Vector2(x, y), headerColor); y += lineHeight + 2;
    spriteBatch.DrawString(Font, IsPaused ? "PAUSED" : "RUNNING", new Vector2(x, y), IsPaused ? new Color(255, 140, 140) : new Color(140, 220, 160)); y += lineHeight;

    spriteBatch.DrawString(Font, $"FPS {AverageFps():0}  {AverageFrameMs():0.0}ms", new Vector2(x, y), labelColor); y += lineHeight;

    long memMb = GC.GetTotalMemory(false) / (1024 * 1024);
    int g0 = GC.CollectionCount(0) - _gen0Baseline;
    int g1 = GC.CollectionCount(1) - _gen1Baseline;
    int g2 = GC.CollectionCount(2) - _gen2Baseline;
    spriteBatch.DrawString(Font, $"Mem {memMb}MB  gc {g0}/{g1}/{g2}", new Vector2(x, y), labelColor); y += lineHeight;

    spriteBatch.DrawString(Font, $"states {_states.StackDepth}  ui {_ui.ElementCount}  timers {_timers.ActiveTimerCount}", new Vector2(x, y), labelColor); y += lineHeight + 4;

    foreach (string name in _watchOrder)
    {
      if (y > panel.Bottom - lineHeight - 4) break;
      string val = SafeWatch(name);
      spriteBatch.DrawString(Font, $"{name}: {val}", new Vector2(x, y), labelColor); y += lineHeight;
    }

    // Event tail panel on the right
    Rectangle tail = new(panel.Right + 8, 8, 380, EventTailSize * lineHeight + 20);
    Primitives.DrawRectangle(spriteBatch, tail, new Color(0, 0, 0, 180));
    spriteBatch.DrawString(Font, "EVENTS (newest last)", new Vector2(tail.X + 8, tail.Y + 6), headerColor);
    int ey = tail.Y + 6 + lineHeight;
    int eventIndex = 0;
    int totalEvents = _eventTail.Count;
    foreach (string msg in _eventTail)
    {
      float fade = 0.4f + 0.6f * (eventIndex + 1) / (float)totalEvents;
      Color c = new((byte)(mutedColor.R * fade), (byte)(mutedColor.G * fade), (byte)(mutedColor.B * fade));
      spriteBatch.DrawString(Font, msg, new Vector2(tail.X + 8, ey), c);
      ey += lineHeight;
      eventIndex++;
    }

    spriteBatch.End();
  }

  private string SafeWatch(string name)
  {
    try { return _watches[name]?.Invoke() ?? "<null>"; }
    catch (Exception ex) { return $"<error: {ex.GetType().Name}>"; }
  }
}
