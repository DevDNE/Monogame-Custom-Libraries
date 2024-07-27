using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using Scenes;

namespace Managers;
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
    if (scenes.ContainsKey(name))
    {
      scenes[name].UnloadContent();
      scenes.Remove(name);
    }
    else
    {
      throw new KeyNotFoundException($"Scene '{name}' does not exist.");
    }
  }
  public void LoadScene(string name)
  {
    if (scenes.ContainsKey(name))
    {
      currentScene?.UnloadContent();
      currentScene = scenes[name];
      currentScene.LoadContent(_content);
    }
    else
    {
      throw new KeyNotFoundException($"Scene '{name}' does not exist.");
    }
  }

  public void LoadContent(ContentManager content)
  {
    _content = content;
  }

  public void Update(GameTime gameTime)
  {
    currentScene?.Update(gameTime);
  }
}