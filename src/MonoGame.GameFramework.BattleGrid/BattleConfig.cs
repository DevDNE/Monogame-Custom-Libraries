namespace MonoGame.GameFramework.BattleGrid;

internal static class BattleConfig
{
  public const int TileSize = 80;
  public const int PlayerBoardX = 115;
  public const int EnemyBoardX = 400;
  public const int BoardY = 200;

  // Characters are authored 32x48 and drawn at BattleArt.Scale, so the display
  // size is exactly the source times a whole number. Anything else here would
  // put a half-pixel on screen no linter can see.
  public const int SourceWidth = 32;
  public const int SourceHeight = 48;
  public const int DisplayWidth = 64;
  public const int DisplayHeight = 96;

  public const int ProjectileSourceSize = 16;
  public const int ProjectileDisplaySize = 32;

  public const int ProjectileDamage = 10;
  public const int ProjectileOffscreenMaxX = 1000;
}
