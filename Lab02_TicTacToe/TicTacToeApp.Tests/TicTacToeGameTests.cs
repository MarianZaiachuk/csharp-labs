using TicTacToeApp.Models;

namespace TicTacToeApp.Tests;

public class TicTacToeGameTests
{
    [Theory]
    [InlineData(0, CellState.X)]
    [InlineData(1, CellState.X)]
    [InlineData(2, CellState.X)]
    [InlineData(0, CellState.O)]
    [InlineData(1, CellState.O)]
    [InlineData(2, CellState.O)]
    public void CheckWinner_DetectsWinInEveryRow(int row, CellState player)
    {
        var game = new TicTacToeGame();
        CellState other = player == CellState.X ? CellState.O : CellState.X;
        int otherRow = (row + 1) % game.BoardSize;

        Assert.True(game.MakeMove(row, 0, player));
        Assert.True(game.MakeMove(otherRow, 0, other));
        Assert.True(game.MakeMove(row, 1, player));
        Assert.True(game.MakeMove(otherRow, 1, other));
        Assert.True(game.MakeMove(row, 2, player));

        Assert.Equal(player, game.CheckWinner());
        Assert.True(game.IsGameOver);
        Assert.Equal(3, game.WinningCells.Count);
    }

    [Theory]
    [InlineData(0, CellState.X)]
    [InlineData(1, CellState.X)]
    [InlineData(2, CellState.X)]
    [InlineData(0, CellState.O)]
    [InlineData(1, CellState.O)]
    [InlineData(2, CellState.O)]
    public void CheckWinner_DetectsWinInEveryColumn(int col, CellState player)
    {
        var game = new TicTacToeGame();
        CellState other = player == CellState.X ? CellState.O : CellState.X;
        int otherCol = (col + 1) % game.BoardSize;

        Assert.True(game.MakeMove(0, col, player));
        Assert.True(game.MakeMove(0, otherCol, other));
        Assert.True(game.MakeMove(1, col, player));
        Assert.True(game.MakeMove(1, otherCol, other));
        Assert.True(game.MakeMove(2, col, player));

        Assert.Equal(player, game.CheckWinner());
        Assert.True(game.IsGameOver);
        Assert.Equal(3, game.WinningCells.Count);
    }

    [Fact]
    public void CheckWinner_DetectsMainDiagonalWin()
    {
        var game = new TicTacToeGame();

        game.MakeMove(0, 0, CellState.X);
        game.MakeMove(0, 1, CellState.O);
        game.MakeMove(1, 1, CellState.X);
        game.MakeMove(0, 2, CellState.O);
        game.MakeMove(2, 2, CellState.X);

        Assert.Equal(CellState.X, game.CheckWinner());
        Assert.Equal(GameStatus.XWins, game.Status);
        Assert.Contains(new BoardPosition(0, 0), game.WinningCells);
        Assert.Contains(new BoardPosition(1, 1), game.WinningCells);
        Assert.Contains(new BoardPosition(2, 2), game.WinningCells);
    }

    [Fact]
    public void CheckWinner_DetectsAntiDiagonalWin()
    {
        var game = new TicTacToeGame();

        game.MakeMove(0, 2, CellState.O);
        game.MakeMove(0, 0, CellState.X);
        game.MakeMove(1, 1, CellState.O);
        game.MakeMove(1, 0, CellState.X);
        game.MakeMove(2, 0, CellState.O);

        Assert.Equal(CellState.O, game.CheckWinner());
        Assert.Equal(GameStatus.OWins, game.Status);
    }

    [Fact]
    public void IsDraw_ReturnsTrueOnFullBoardWithoutWinner()
    {
        var game = new TicTacToeGame();

        // X O X
        // X O O
        // O X X
        game.MakeMove(0, 0, CellState.X);
        game.MakeMove(0, 1, CellState.O);
        game.MakeMove(0, 2, CellState.X);
        game.MakeMove(1, 1, CellState.O);
        game.MakeMove(1, 0, CellState.X);
        game.MakeMove(1, 2, CellState.O);
        game.MakeMove(2, 1, CellState.X);
        game.MakeMove(2, 0, CellState.O);
        game.MakeMove(2, 2, CellState.X);

        Assert.Equal(CellState.Empty, game.CheckWinner());
        Assert.True(game.IsDraw());
        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.True(game.IsGameOver);
    }

    [Fact]
    public void MakeMove_RejectsOccupiedCellOutOfBoundsAndAfterGameOver()
    {
        var game = new TicTacToeGame();

        Assert.True(game.MakeMove(0, 0, CellState.X));
        Assert.False(game.MakeMove(0, 0, CellState.O));
        Assert.False(game.MakeMove(-1, 0, CellState.O));
        Assert.False(game.MakeMove(0, 3, CellState.O));
        Assert.False(game.MakeMove(1, 1, CellState.Empty));

        game.MakeMove(1, 0, CellState.O);
        game.MakeMove(0, 1, CellState.X);
        game.MakeMove(1, 1, CellState.O);
        game.MakeMove(0, 2, CellState.X);

        Assert.True(game.IsGameOver);
        Assert.False(game.MakeMove(2, 2, CellState.O));
    }

    [Fact]
    public void ChooseComputerMove_Level1_ReturnsValidAvailableCell()
    {
        var game = new TicTacToeGame(random: new Random(42));
        game.MakeMove(1, 1, CellState.X);

        BoardPosition? move = game.ChooseComputerMove(DifficultyLevel.Random);

        Assert.NotNull(move);
        Assert.Contains(move.Value, game.GetAvailableMoves());
        Assert.NotEqual(new BoardPosition(1, 1), move.Value);
    }

    [Fact]
    public void ChooseComputerMove_Level2_BlocksImmediateHumanWin()
    {
        var game = new TicTacToeGame(random: new Random(42));

        game.MakeMove(0, 0, CellState.X);
        game.MakeMove(2, 2, CellState.O);
        game.MakeMove(0, 1, CellState.X);

        BoardPosition? move = game.ChooseComputerMove(DifficultyLevel.Block);

        Assert.Equal(new BoardPosition(0, 2), move);
    }

    [Fact]
    public void ChooseComputerMove_Level3_PrefersWinningOverBlocking()
    {
        var game = new TicTacToeGame(random: new Random(42));

        // O has (1,0) and (1,1) -> winning move at (1,2)
        // X has (0,0) and (0,1) -> threat at (0,2)
        game.MakeMove(0, 0, CellState.X);
        game.MakeMove(1, 0, CellState.O);
        game.MakeMove(0, 1, CellState.X);
        game.MakeMove(1, 1, CellState.O);
        game.MakeMove(2, 2, CellState.X);

        BoardPosition? move = game.ChooseComputerMove(DifficultyLevel.WinAndBlock);

        Assert.Equal(new BoardPosition(1, 2), move);
    }

    [Fact]
    public void ChooseComputerMove_Level3_BlocksWhenNoImmediateWinAvailable()
    {
        var game = new TicTacToeGame(random: new Random(42));

        game.MakeMove(0, 0, CellState.X);
        game.MakeMove(1, 1, CellState.O);
        game.MakeMove(2, 0, CellState.X);

        BoardPosition? move = game.ChooseComputerMove(DifficultyLevel.WinAndBlock);

        Assert.Equal(new BoardPosition(1, 0), move);
    }

    [Fact]
    public void Reset_RestoresEmptyBoardAndInitialState()
    {
        var game = new TicTacToeGame();
        game.MakeMove(0, 0, CellState.X);
        game.MakeMove(1, 0, CellState.O);
        game.MakeMove(0, 1, CellState.X);
        game.MakeMove(1, 1, CellState.O);
        game.MakeMove(0, 2, CellState.X);

        Assert.True(game.IsGameOver);

        game.Reset();

        Assert.False(game.IsGameOver);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(CellState.X, game.CurrentPlayer);
        Assert.Empty(game.WinningCells);
        Assert.Equal(9, game.GetAvailableMoves().Count);
    }
}
