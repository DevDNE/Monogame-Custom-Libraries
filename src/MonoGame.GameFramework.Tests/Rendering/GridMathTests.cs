using FluentAssertions;
using Microsoft.Xna.Framework;
using MonoGame.GameFramework.Rendering;
using Xunit;

namespace MonoGame.GameFramework.Tests.Rendering;

public class GridMathTests
{
  [Fact]
  public void TryMouseToCell_InsideBounds_ReturnsTrue()
  {
    bool ok = GridMath.TryMouseToCell(new Vector2(90, 45), new Vector2(10, 5), cellSize: 40, columns: 5, rows: 3, out int col, out int row);
    ok.Should().BeTrue();
    col.Should().Be(2);
    row.Should().Be(1);
  }

  [Theory]
  [InlineData(9, 20)]   // left of origin
  [InlineData(20, 4)]   // above origin
  [InlineData(210, 20)] // col = (210-10)/40 = 5, equals columns
  [InlineData(20, 125)] // row = (125-5)/40 = 3, equals rows
  public void TryMouseToCell_OutOfBounds_ReturnsFalse(int mx, int my)
  {
    bool ok = GridMath.TryMouseToCell(new Vector2(mx, my), new Vector2(10, 5), cellSize: 40, columns: 5, rows: 3, out _, out _);
    ok.Should().BeFalse();
  }

  [Fact]
  public void TryMouseToCell_ZeroCellSize_ReturnsFalse()
  {
    bool ok = GridMath.TryMouseToCell(new Vector2(50, 50), Vector2.Zero, cellSize: 0, columns: 5, rows: 3, out _, out _);
    ok.Should().BeFalse();
  }

  [Fact]
  public void TryMouseToCell_ExactlyAtOrigin_ReturnsFirstCell()
  {
    bool ok = GridMath.TryMouseToCell(new Vector2(10, 5), new Vector2(10, 5), cellSize: 40, columns: 5, rows: 3, out int col, out int row);
    ok.Should().BeTrue();
    col.Should().Be(0);
    row.Should().Be(0);
  }

  [Theory]
  [InlineData(9, 20)]   // left of origin
  [InlineData(20, 4)]   // above origin
  [InlineData(210, 20)] // right of the last column
  [InlineData(20, 125)] // below the last row
  public void TryMouseToCell_OutOfBounds_SetsBothOutParamsToMinusOne(int mx, int my)
  {
    // It used to set -1 only when the point was left of or above the grid and
    // leave real out-of-range values in the out-params on the other two edges,
    // so a caller that skipped the bool got different garbage per edge.
    GridMath.TryMouseToCell(new Vector2(mx, my), new Vector2(10, 5), cellSize: 40, columns: 5, rows: 3, out int col, out int row)
      .Should().BeFalse();
    col.Should().Be(-1);
    row.Should().Be(-1);
  }

  [Fact]
  public void TryMouseToCell_ZeroCellSize_SetsBothOutParamsToMinusOne()
  {
    GridMath.TryMouseToCell(new Vector2(50, 50), Vector2.Zero, cellSize: 0, columns: 5, rows: 3, out int col, out int row)
      .Should().BeFalse();
    col.Should().Be(-1);
    row.Should().Be(-1);
  }

}
