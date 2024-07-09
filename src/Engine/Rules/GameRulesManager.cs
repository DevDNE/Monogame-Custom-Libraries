public class GameRulesManager
{
  private const int BoardWidth = 6;
  private const int BoardHeight = 3;
  private const int LeftSideLimit = 3;

  public bool CanMoveLeft(int currentX)
  {
    return currentX > 0 && currentX < LeftSideLimit;
  }

  public bool CanAttack(int currentX, int currentY)
  {
    return currentX > 0 && currentX < BoardWidth && currentY >= 0 && currentY < BoardHeight;
  }
}