using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.GameFramework.Input;
using MonoGame.GameFramework.Lifecycle;
using MonoGame.GameFramework.Persistence;
using MonoGame.GameFramework.Rendering;
using MonoGame.GameFramework.Tweening;

namespace MonoGame.GameFramework.VisualNovel.GameStates;

public class PlayState : GameState
{
  private const float RevealDuration = 1.0f; // seconds to fully reveal a node's text

  private readonly KeyboardManager _keyboard;
  private readonly SaveSystem _saves;
  private readonly SpriteFont _font;
  private readonly int _viewportWidth;
  private readonly int _viewportHeight;
  private readonly string _savePath;

  private DialogueState _state = new();
  private DialogueNode _current;
  private Tween<float> _revealTween;
  private string _displayedText = "";
  private bool _atEnd;

  private readonly VisualNovelArt _art;

  public PlayState(ServiceProvider sp, SpriteFont font, VisualNovelArt art, int vw, int vh, string savePath)
  {
    _art = art;
    _keyboard = sp.GetService<KeyboardManager>();
    _saves = sp.GetService<SaveSystem>();
    _font = font;
    _viewportWidth = vw;
    _viewportHeight = vh;
    _savePath = savePath;
  }

  public void ResumeFrom(DialogueState state)
  {
    _state = state;
    _current = DialogueScript.Nodes[_state.CurrentNodeId];
    _atEnd = _current.Id == DialogueScript.EndNodeId;
    StartReveal();
  }

  public override void Entered()
  {
    if (_current == null) { _current = DialogueScript.Nodes[_state.CurrentNodeId]; StartReveal(); }
    IsActive = true;
  }

  public override void Leaving() { }
  public override void Obscuring() => IsActive = false;
  public override void Revealed() => IsActive = true;

  public override void Update(GameTime gameTime)
  {
    float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

    if (_keyboard.WasKeyPressed(Keys.R))
    {
      _state = new DialogueState();
      _current = DialogueScript.Nodes[_state.CurrentNodeId];
      _atEnd = false;
      StartReveal();
      return;
    }

    // Update text-reveal tween
    if (_revealTween != null && !_revealTween.IsComplete)
    {
      _revealTween.Update(dt);
      float progress = _revealTween.Current;
      int chars = (int)Math.Round(progress * _current.Text.Length);
      if (chars > _current.Text.Length) chars = _current.Text.Length;
      _displayedText = _current.Text.Substring(0, chars);
    }

    if (_atEnd) return;

    if (_current.Choices != null)
    {
      for (int i = 0; i < _current.Choices.Count && i < 3; i++)
      {
        if (_keyboard.WasKeyPressed(Keys.D1 + i) || _keyboard.WasKeyPressed(Keys.NumPad1 + i))
        {
          string next = _current.Choices[i].NextNodeId;
          _state.SeenChoices.Add($"{_current.Id}->{next}");
          AdvanceTo(next);
          return;
        }
      }
    }
    else if (_keyboard.WasKeyPressed(Keys.Space))
    {
      // If mid-reveal, skip to full text first.
      if (_revealTween != null && !_revealTween.IsComplete)
      {
        _revealTween.Update(999f); // force complete
        _displayedText = _current.Text;
        return;
      }
      if (!string.IsNullOrEmpty(_current.NextNodeId)) AdvanceTo(_current.NextNodeId);
    }
  }

  private void AdvanceTo(string nextNodeId)
  {
    _state.CurrentNodeId = nextNodeId;
    _current = DialogueScript.Nodes[nextNodeId];
    _atEnd = _current.Id == DialogueScript.EndNodeId;
    StartReveal();
    _saves.Save(_savePath, _state);
  }

  private void StartReveal()
  {
    _revealTween = Tween.Float(0f, 1f, RevealDuration, Easing.QuadOut);
    _displayedText = "";
  }

  public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
  {
    spriteBatch.Begin(samplerState: SamplerState.PointClamp);
    PixelDraw.Sprite(spriteBatch, _art.Room, 0, 0, VisualNovelArt.RoomScale);

    DrawPortrait(spriteBatch);
    DrawTextBox(spriteBatch);
    DrawHudHints(spriteBatch);

    spriteBatch.End();
  }

  private void DrawPortrait(SpriteBatch spriteBatch)
  {
    // The bust stands on the floor line rather than floating in a panel, and
    // the dialogue box overlaps its lower edge — which is what makes it read as
    // a person in the room instead of a portrait pinned to it.
    Rectangle? source = VisualNovelArt.RectFor(_current.Speaker);
    if (source == null) return;

    int scale = VisualNovelArt.PortraitScale;
    int width = VisualNovelArt.PortraitWidth * scale;
    int height = VisualNovelArt.PortraitHeight * scale;
    int x = (_viewportWidth - width) / 2;
    int y = _viewportHeight - 260 - height + 60;

    PixelDraw.Frame(spriteBatch, _art.Portraits, source.Value, x, y, scale);

    string speaker = _current.Speaker switch
    {
      Portrait.Alex => "Alex",
      Portrait.Morgan => "Morgan",
      _ => "",
    };
    if (speaker.Length > 0)
    {
      Vector2 sz = _font.MeasureString(speaker);
      spriteBatch.DrawString(_font, speaker,
        new Vector2(x + width / 2f - sz.X * 0.5f, y - 34), new Color(255, 228, 204));
    }
  }

  private void DrawTextBox(SpriteBatch spriteBatch)
  {
    int boxH = 220;
    Rectangle box = new(40, _viewportHeight - boxH - 40, _viewportWidth - 80, boxH);
    _art.Frame.Draw(spriteBatch, box, VisualNovelArt.PortraitScale);

    // Inset from the frame's own content bounds rather than from the box, so
    // the text does not collide with the border however thick it gets.
    Rectangle content = _art.Frame.ContentBounds(box, VisualNovelArt.PortraitScale);
    string wrapped = WrapText(_displayedText, _font, content.Width - 24);
    spriteBatch.DrawString(_font, wrapped, new Vector2(content.X + 12, content.Y + 8), new Color(242, 240, 236));

    if (_current.Choices != null && (_revealTween == null || _revealTween.IsComplete))
    {
      int y = box.Y + 20 + (int)_font.MeasureString(wrapped).Y + 12;
      for (int i = 0; i < _current.Choices.Count; i++)
      {
        string label = $"{i + 1}. {_current.Choices[i].Text}";
        spriteBatch.DrawString(_font, label, new Vector2(box.X + 30, y), new Color(255, 220, 140));
        y += 28;
      }
    }
  }

  private void DrawHudHints(SpriteBatch spriteBatch)
  {
    string hint;
    if (_atEnd) hint = "R restart   Esc to quit";
    else if (_current.Choices != null) hint = "Press 1 / 2 / 3 to choose";
    else if (_revealTween != null && !_revealTween.IsComplete) hint = "Space skip reveal";
    else hint = "Space to continue";
    Vector2 sz = _font.MeasureString(hint);
    spriteBatch.DrawString(_font, hint, new Vector2(_viewportWidth - sz.X - 20, 14), new Color(200, 200, 215));
    spriteBatch.DrawString(_font, "Auto-saves on every advance", new Vector2(20, 14), new Color(150, 155, 180));
  }

  // Naive word wrap for a fixed width.
  private static string WrapText(string text, SpriteFont font, float maxWidth)
  {
    if (string.IsNullOrEmpty(text)) return text;
    string[] words = text.Split(' ');
    System.Text.StringBuilder sb = new();
    System.Text.StringBuilder line = new();
    foreach (string w in words)
    {
      string candidate = line.Length == 0 ? w : line + " " + w;
      if (font.MeasureString(candidate).X > maxWidth && line.Length > 0)
      {
        sb.AppendLine(line.ToString());
        line.Clear();
        line.Append(w);
      }
      else
      {
        if (line.Length > 0) line.Append(' ');
        line.Append(w);
      }
    }
    if (line.Length > 0) sb.Append(line);
    return sb.ToString();
  }
}
