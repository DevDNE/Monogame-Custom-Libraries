using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.GameFramework.BattleGrid.Components;
using MonoGame.GameFramework.Rendering;

namespace MonoGame.GameFramework.BattleGrid.Components.Entities;

public class Gameboard
{
  private readonly DrawManager _drawManager;
  public SpriteSheet[,] PlayerTiles { get; } = new SpriteSheet[3, 3];
  public SpriteSheet[,] EnemyTiles { get; } = new SpriteSheet[3, 3];

  public Gameboard(ServiceProvider serviceProvider)
  {
    _drawManager = serviceProvider.GetService<DrawManager>();
  }

  public void LoadContent(ContentManager content)
  {
    Texture2D playerPanel = content.Load<Texture2D>(BattleArt.PlayerPanel);
    Texture2D enemyPanel = content.Load<Texture2D>(BattleArt.EnemyPanel);

    for (int row = 0; row < 3; row++)
    {
      for (int col = 0; col < 3; col++)
      {
        PlayerTiles[row, col] = MakeTile(playerPanel,
          BattleConfig.PlayerBoardX + col * BattleConfig.TileSize,
          BattleConfig.BoardY + row * BattleConfig.TileSize,
          $"playerTile_{row}_{col}");
        _drawManager.AddSprite(PlayerTiles[row, col]);

        EnemyTiles[row, col] = MakeTile(enemyPanel,
          BattleConfig.EnemyBoardX + col * BattleConfig.TileSize,
          BattleConfig.BoardY + row * BattleConfig.TileSize,
          $"enemyTile_{row}_{col}");
        _drawManager.AddSprite(EnemyTiles[row, col]);
      }
    }
  }

  /// <summary>
  /// Tiles butt up against each other with no gap: the 1px outline the art
  /// carries on all four sides *is* the grid line, and two of them meeting
  /// gives the 2px seam a board wants. The old code inset each tile by 2px to
  /// fake that with background showing through, which cannot work once the
  /// tiles are lit — the inset showed backdrop where a shadow belongs.
  /// </summary>
  private static SpriteSheet MakeTile(Texture2D texture, int x, int y, string name)
    => SpriteSheet.Static(
      texture,
      new Rectangle(x, y, BattleConfig.TileSize, BattleConfig.TileSize),
      name: name);

  public void UnloadContent()
  {
    for (int row = 0; row < 3; row++)
    {
      for (int col = 0; col < 3; col++)
      {
        if (PlayerTiles[row, col] != null) _drawManager.RemoveSprite(PlayerTiles[row, col]);
        if (EnemyTiles[row, col] != null) _drawManager.RemoveSprite(EnemyTiles[row, col]);
      }
    }
  }

}
