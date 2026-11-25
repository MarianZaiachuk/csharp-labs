namespace TicTacToeApp.Models;

public sealed class TicTacToeGame
{
    public const int DefaultBoardSize = 3;
    public const int MinBoardSize = 3;
    public const CellState HumanMark = CellState.X;
    public const CellState ComputerMark = CellState.O;

    private readonly CellState[,] _board;
    private readonly Random _random;
    private List<BoardPosition> _winningCells = [];

    public TicTacToeGame(int boardSize = DefaultBoardSize, Random? random = null)
    {
        if (boardSize < MinBoardSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(boardSize),
                $"Розмір поля має бути не менше {MinBoardSize}.");
        }

        BoardSize = boardSize;
        _board = new CellState[boardSize, boardSize];
        _random = random ?? Random.Shared;
        Reset();
    }

    public int BoardSize { get; }

    public CellState CurrentPlayer { get; private set; }

    public GameStatus Status { get; private set; }

    public bool IsGameOver => Status != GameStatus.InProgress;

    public IReadOnlyList<BoardPosition> WinningCells => _winningCells;

    public CellState GetCell(int row, int column)
    {
        if (!IsInBounds(row, column))
        {
            throw new ArgumentOutOfRangeException($"Некоректні координати ({row}, {column}).");
        }

        return _board[row, column];
    }

    public void Reset()
    {
        for (int row = 0; row < BoardSize; row++)
        {
            for (int col = 0; col < BoardSize; col++)
            {
                _board[row, col] = CellState.Empty;
            }
        }

        CurrentPlayer = HumanMark;
        Status = GameStatus.InProgress;
        _winningCells.Clear();
    }

    public bool MakeMove(int row, int column, CellState player)
    {
        if (IsGameOver ||
            player == CellState.Empty ||
            !IsInBounds(row, column) ||
            _board[row, column] != CellState.Empty)
        {
            return false;
        }

        _board[row, column] = player;

        CellState winner = FindWinner(out List<BoardPosition> winningLine);
        if (winner != CellState.Empty)
        {
            _winningCells = winningLine;
            Status = winner == CellState.X ? GameStatus.XWins : GameStatus.OWins;
        }
        else if (IsDraw())
        {
            Status = GameStatus.Draw;
        }
        else
        {
            CurrentPlayer = player == CellState.X ? CellState.O : CellState.X;
        }

        return true;
    }

    public IReadOnlyList<BoardPosition> GetAvailableMoves()
    {
        var moves = new List<BoardPosition>();
        for (int row = 0; row < BoardSize; row++)
        {
            for (int col = 0; col < BoardSize; col++)
            {
                if (_board[row, col] == CellState.Empty)
                {
                    moves.Add(new BoardPosition(row, col));
                }
            }
        }

        return moves;
    }

    public CellState CheckWinner() => FindWinner(out _);

    public bool IsDraw() => CheckWinner() == CellState.Empty && GetAvailableMoves().Count == 0;

    public BoardPosition? ChooseComputerMove(
        DifficultyLevel difficulty,
        CellState computerMark = ComputerMark,
        CellState humanMark = HumanMark)
    {
        if (IsGameOver)
        {
            return null;
        }

        IReadOnlyList<BoardPosition> availableMoves = GetAvailableMoves();
        if (availableMoves.Count == 0)
        {
            return null;
        }

        return difficulty switch
        {
            DifficultyLevel.Random => PickRandomMove(availableMoves),
            DifficultyLevel.Block => FindWinningMove(humanMark, availableMoves)
                                     ?? PickRandomMove(availableMoves),
            DifficultyLevel.WinAndBlock => FindWinningMove(computerMark, availableMoves)
                                           ?? FindWinningMove(humanMark, availableMoves)
                                           ?? PickRandomMove(availableMoves),
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty), "Невідомий рівень складності.")
        };
    }

    private CellState FindWinner(out List<BoardPosition> winningLine)
    {
        for (int row = 0; row < BoardSize; row++)
        {
            CellState rowWinner = CheckLine(row, 0, 0, 1, out winningLine);
            if (rowWinner != CellState.Empty)
            {
                return rowWinner;
            }
        }

        for (int col = 0; col < BoardSize; col++)
        {
            CellState colWinner = CheckLine(0, col, 1, 0, out winningLine);
            if (colWinner != CellState.Empty)
            {
                return colWinner;
            }
        }

        CellState mainDiagWinner = CheckLine(0, 0, 1, 1, out winningLine);
        if (mainDiagWinner != CellState.Empty)
        {
            return mainDiagWinner;
        }

        return CheckLine(0, BoardSize - 1, 1, -1, out winningLine);
    }

    private CellState CheckLine(
        int startRow,
        int startCol,
        int dRow,
        int dCol,
        out List<BoardPosition> line)
    {
        line = new List<BoardPosition>(BoardSize);
        CellState first = _board[startRow, startCol];
        if (first == CellState.Empty)
        {
            return CellState.Empty;
        }

        for (int step = 0; step < BoardSize; step++)
        {
            int r = startRow + step * dRow;
            int c = startCol + step * dCol;
            if (_board[r, c] != first)
            {
                line.Clear();
                return CellState.Empty;
            }

            line.Add(new BoardPosition(r, c));
        }

        return first;
    }

    private BoardPosition? FindWinningMove(CellState mark, IReadOnlyList<BoardPosition> availableMoves)
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            BoardPosition move = availableMoves[i];
            _board[move.Row, move.Column] = mark;
            try
            {
                if (CheckWinner() == mark)
                {
                    return move;
                }
            }
            finally
            {
                _board[move.Row, move.Column] = CellState.Empty;
            }
        }

        return null;
    }

    private BoardPosition PickRandomMove(IReadOnlyList<BoardPosition> availableMoves) =>
        availableMoves[_random.Next(availableMoves.Count)];

    private bool IsInBounds(int row, int column) =>
        row >= 0 && row < BoardSize && column >= 0 && column < BoardSize;
}
