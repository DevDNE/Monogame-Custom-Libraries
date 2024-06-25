using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using InputManager;
using SoundManager;
using GraphicsManager;
using Scenes;
using UIManager;

namespace SceneManager
{
  public class ManageScenes
  {
    private Dictionary<string, GameScene> scenes;
    private GameScene currentScene;
    private FirstScene firstScene;
    private ContentManager content;
    public ManageScenes(DrawManager drawManager, TextManager _textManager,  KeyboardInput keyboardInput, MouseInput mouseInput, GamePadInput gamePadInput, Media media, ContentManager content)
    {
      this.content = content;
      scenes = new Dictionary<string, GameScene>();
      currentScene = null;
      firstScene = new FirstScene(drawManager, _textManager, keyboardInput);
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
        currentScene.LoadContent(content);
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