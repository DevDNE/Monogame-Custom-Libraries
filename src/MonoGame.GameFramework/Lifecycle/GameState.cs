using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.GameFramework.Lifecycle;
public abstract class GameState
{
  private bool? _isVisible;

  /// <summary>Whether <see cref="Update"/> runs. Set by the state, usually in its lifecycle hooks.</summary>
  public bool IsActive { get; set; }

  /// <summary>
  /// Whether <see cref="Draw"/> runs. Defaults to following <see cref="IsActive"/> — a state that
  /// is not simulating is not drawn — until a state sets it explicitly.
  ///
  /// The two gates are separate because an overlay needs them to disagree: pushing a pause menu
  /// should stop the scene beneath it from simulating while keeping it on screen. With one flag
  /// that is inexpressible, and the choice is between a paused game that vanishes and a paused
  /// game that keeps playing. Setting this is opt-in, so every state that never touches it behaves
  /// exactly as it did before the split.
  /// </summary>
  public bool IsVisible
  {
    get => _isVisible ?? IsActive;
    set => _isVisible = value;
  }

  /// <summary>Return <see cref="IsVisible"/> to following <see cref="IsActive"/>.</summary>
  public void ClearVisibilityOverride() => _isVisible = null;

  public abstract void Entered();
  public abstract void Leaving();
  public abstract void Obscuring();
  public abstract void Revealed();
  public abstract void Update(GameTime gameTime);
  public virtual void Draw(SpriteBatch spriteBatch, GameTime gameTime) { }
}
