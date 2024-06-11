using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using InputManager;
using SoundManager;
using GraphicsManager;

namespace ManageScenes
{
  public class SceneManager
  {
    private Dictionary<string, GameScene> scenes;
    private GameScene currentScene;
    private FirstScene firstScene;
    public SceneManager(DrawManager drawManager, KeyboardInput keyboardInput, MouseInput mouseInput, GamePadInput gamePadInput, Media media, ContentManager content)
    {
      scenes = new Dictionary<string, GameScene>();
      currentScene = null;
      firstScene = new FirstScene(drawManager, keyboardInput, content);

      AddScene("FirstScene", firstScene);
    }

    public void AddScene(string name, GameScene scene)
    {
      scenes[name] = scene;
    }

    public void LoadScene(string name)
    {
      if (scenes.ContainsKey(name))
      {
        currentScene?.UnloadContent();
        currentScene = scenes[name];
        currentScene.LoadContent();
      }
      else
      {
        throw new KeyNotFoundException($"Scene '{name}' does not exist.");
      }
    }

    public void Update(GameTime gameTime)
    {
      currentScene?.Update(gameTime);
    }
  }
}