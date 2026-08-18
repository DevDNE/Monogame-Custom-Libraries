using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.GameFramework.Lifecycle;
public class GameStateManager
{
  private Stack<GameState> stateStack = new Stack<GameState>();
  // Scratch buffer reused across frames so Update/Draw are allocation-free
  // once the stack stops growing. We snapshot into this because a state's
  // Update can legitimately call ChangeState/PushState/PopState on this
  // manager mid-iteration (e.g. a combat state transitions to post-combat
  // when the battle ends), and that would otherwise invalidate a foreach
  // over the stack.
  //
  // Stack<T> enumerates in pop order, so the buffer always reads
  // [top .. bottom]. Update walks it forwards and Draw walks it backwards;
  // see the comment on Draw for why those directions differ.
  private readonly List<GameState> _iterationBuffer = new();

  // Set when PopState runs *during* Update, naming the state that was revealed
  // by it. See Update for why that state has to sit out the rest of the pass.
  private GameState _revealedDuringUpdate;
  private bool _updating;

  public void PushState(GameState newState)
  {
    if (stateStack.Count > 0) stateStack.Peek().Obscuring();
    stateStack.Push(newState);
    newState.Entered();
  }

  public void PopState()
  {
    if (stateStack.Count == 0) return;
    GameState popped = stateStack.Pop();
    popped.Leaving();
    if (stateStack.Count == 0) return;

    GameState revealed = stateStack.Peek();
    revealed.Revealed();
    if (_updating) _revealedDuringUpdate = revealed;
  }

  public void ChangeState(GameState newState)
  {
    if (stateStack.Count > 0)
    {
      GameState old = stateStack.Pop();
      old.Leaving();
    }
    stateStack.Push(newState);
    newState.Entered();
  }

  public GameState PeekState()
  {
    return stateStack.Peek();
  }

  public int StackDepth => stateStack.Count;

  public void Update(GameTime gameTime)
  {
    // Top-down. The state on top of the stack is the one in focus, so it gets
    // to act — and to transition the stack — before anything beneath it.
    _updating = true;
    _revealedDuringUpdate = null;
    try
    {
      _iterationBuffer.Clear();
      _iterationBuffer.AddRange(stateStack);
      foreach (GameState state in _iterationBuffer)
      {
        // A state revealed by a PopState earlier in this same pass sits the
        // rest of it out. Update runs top-down, so an overlay that pops itself
        // hands control back to a state further down the buffer that has not
        // been visited yet — and that state then sees the very same input that
        // dismissed the overlay. One press of P closed the pause menu and
        // immediately reopened it.
        //
        // This mirrors what push already does: a state pushed mid-Update is not
        // in the snapshot and does not run until the next frame. Popping now
        // behaves the same way in the other direction.
        if (ReferenceEquals(state, _revealedDuringUpdate)) continue;
        if (state.IsActive)
        {
          state.Update(gameTime);
        }
      }
    }
    finally
    {
      _updating = false;
      _revealedDuringUpdate = null;
    }
  }

  public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
  {
    // Bottom-up — the reverse of Update, and the reason this loop is not a
    // foreach. The buffer reads [top .. bottom], so painting it forwards drew
    // the topmost state first and let every state beneath paint over it: a
    // pause overlay pushed onto a play state rendered *behind* the game.
    // Walking backwards puts the bottom of the stack down first and the top
    // of the stack last, which is what "on top" has to mean.
    _iterationBuffer.Clear();
    _iterationBuffer.AddRange(stateStack);
    for (int i = _iterationBuffer.Count - 1; i >= 0; i--)
    {
      GameState state = _iterationBuffer[i];
      // IsVisible, not IsActive — it follows IsActive unless a state opted out,
      // which is how a paused scene stays on screen without simulating.
      if (state.IsVisible)
      {
        state.Draw(spriteBatch, gameTime);
      }
    }
  }
}
