using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Graphics;
using Managers;

namespace Entities;
public class Gameboard : Entity
{
  private SpriteSheet topTile;
  private SpriteSheet midTile;
  private SpriteSheet botTile;
  private SpriteSheet enemyTopTile;
  private SpriteSheet enemyMidTile;
  private SpriteSheet enemyBotTile;
  private DrawManager drawManager;
  private Tile[,] tiles;
  public SpriteSheet[,] boardTiles = new SpriteSheet[3, 3];
  public SpriteSheet[,] enemyBoardTiles = new SpriteSheet[3, 3];
  int tileSize = 80;
  int xOffset = 115;
  int yOffset = 200;
  int enemyXOffset = 400;
  public Gameboard(ServiceProvider serviceProvider)
  {
    drawManager = serviceProvider.GetService<DrawManager>();
    tiles = new Tile[6, 3];
  }

  public override void LoadContent(ContentManager content)
  {
    //player side
    topTile = new SpriteSheet("TopTile", content.Load<Texture2D>("gfx/Battlefield_Tile"),
      new Vector2(0, 0), 40, 24,
      new Rectangle[] { new Rectangle(0, 0, 40, 24) },
      new Rectangle(200, 250, 80, 48), 0f, 0
    );
    midTile = new SpriteSheet("MidTile", content.Load<Texture2D>("gfx/Battlefield_Tile"),
      new Vector2(0, 0), 40, 24,
      new Rectangle[] { new Rectangle(48, 0, 40, 24) },
      new Rectangle(200, 298, 80, 48), 0f, 0
    );
    botTile = new SpriteSheet("BotTile", content.Load<Texture2D>("gfx/Battlefield_Tile"),
      new Vector2(0, 0), 40, 32,
      new Rectangle[] { new Rectangle(96, 0, 40, 32) },
      new Rectangle(200, 346, 80, 52), 0f, 0
    );

    //other side
    enemyTopTile = new SpriteSheet("TopTile", content.Load<Texture2D>("gfx/Battlefield_Tile"),
      new Vector2(0, 0), 40, 24,
      new Rectangle[] { new Rectangle(0, 0, 40, 24) },
      new Rectangle(200, 250, 80, 48), 0f, 0
    );
    enemyMidTile = new SpriteSheet("MidTile", content.Load<Texture2D>("gfx/Battlefield_Tile"),
      new Vector2(0, 0), 40, 24,
      new Rectangle[] { new Rectangle(48, 0, 40, 24) },
      new Rectangle(200, 298, 80, 48), 0f, 0
    );
    enemyBotTile = new SpriteSheet("BotTile", content.Load<Texture2D>("gfx/Battlefield_Tile"),
      new Vector2(0, 0), 40, 32,
      new Rectangle[] { new Rectangle(96, 0, 40, 32) },
      new Rectangle(200, 346, 80, 52), 0f, 0
    );

    for (int i = 0; i < 3; i++)
    {
      for (int j = 0; j < 3; j++)
      {
        if (i == 0) // Top row
        {
          boardTiles[i, j] = topTile.Clone() as SpriteSheet;
          enemyBoardTiles[i, j] = enemyTopTile.Clone() as SpriteSheet;
        }
        else if (i == 1) // Middle row
        {
          boardTiles[i, j] = midTile.Clone() as SpriteSheet;
          enemyBoardTiles[i, j] = enemyMidTile.Clone() as SpriteSheet;
        }
        else // Bottom row
        {
          boardTiles[i, j] = botTile.Clone() as SpriteSheet;
          enemyBoardTiles[i, j] = enemyBotTile.Clone() as SpriteSheet;
        }

        boardTiles[i, j].Position = new Vector2(j * tileSize + xOffset, i * tileSize + yOffset);
        boardTiles[i, j].DestinationFrame = new Rectangle((int)boardTiles[i, j].Position.X, (int)boardTiles[i, j].Position.Y, tileSize, tileSize);

        enemyBoardTiles[i, j].Position = new Vector2(j * tileSize + enemyXOffset, i * tileSize + yOffset);
        enemyBoardTiles[i, j].DestinationFrame = new Rectangle((int)enemyBoardTiles[i, j].Position.X, (int)enemyBoardTiles[i, j].Position.Y, tileSize, tileSize);

        // Add the tile to the draw manager
        drawManager.AddSprite(boardTiles[i, j]);
        drawManager.AddSprite(enemyBoardTiles[i, j]);
      }
    }
  }

  public override void UnloadContent()
  {
    throw new System.NotImplementedException();
  }

  public override void Update(GameTime gameTime)
  {

  }
}