using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace MonoGame.GameFramework.Lifecycle;
public class SceneManager
{
  private Dictionary<string, GameScene> scenes = new Dictionary<string, GameScene>();
  private GameScene currentScene = null;
  private ContentManager _content;

  public void AddScene(string name, GameScene scene)
  {
    scenes[name] = scene;
  }

  public void RemoveScene(string name)
  {
    if (!scenes.TryGetValue(name, out GameScene scene))
    {
      throw new KeyNotFoundException($"Scene '{name}' does not exist.");
    }

    scene.UnloadContent();
    scenes.Remove(name);
    // Drop the current-scene reference when it is the one being removed.
    // Leaving it set meant Update and Draw kept calling into a scene whose
    // content had already been unloaded.
    if (ReferenceEquals(currentScene, scene)) currentScene = null;
  }

  public void LoadScene(string name)
  {
    if (!scenes.TryGetValue(name, out GameScene scene))
    {
      throw new KeyNotFoundException($"Scene '{name}' does not exist.");
    }
    if (_content == null)
    {
      throw new InvalidOperationException(
        "SceneManager.LoadContent(ContentManager) must be called before LoadScene — " +
        "otherwise the scene is handed a null ContentManager and fails inside its own LoadContent.");
    }

    currentScene?.UnloadContent();
    currentScene = scene;
    currentScene.LoadContent(_content);
  }

  public void LoadContent(ContentManager content)
  {
    _content = content;
  }

  public void Update(GameTime gameTime)
  {
    currentScene?.Update(gameTime);
  }

  public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
  {
    currentScene?.Draw(spriteBatch, gameTime);
  }
}
