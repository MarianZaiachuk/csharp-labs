using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TicTacToeApp.Commands;
using TicTacToeApp.Models;

namespace TicTacToeApp.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private const string HumanTurnMessage = "Ваш хід (X). Оберіть вільну клітинку.";
    private const string HumanWonMessage = "Перемога гравця (X)!";
    private const string ComputerWonMessage = "Перемога комп'ютера (O)!";
    private const string DrawMessage = "Нічия! Вільних клітинок не залишилося.";

    private readonly TicTacToeGame _game = new();

    private DifficultyOption _selectedDifficulty;
    private string _statusMessage = HumanTurnMessage;
    private int _humanWins;
    private int _computerWins;
    private int _draws;

    public MainViewModel()
    {
        DifficultyOptions =
        [
            new DifficultyOption(DifficultyLevel.Random, "Рівень 1 — Випадковий хід"),
            new DifficultyOption(DifficultyLevel.Block, "Рівень 2 — Блокування гравця"),
            new DifficultyOption(DifficultyLevel.WinAndBlock, "Рівень 3 — Перемога і блокування")
        ];
        _selectedDifficulty = DifficultyOptions[2];

        Cells = [];
        for (int row = 0; row < _game.BoardSize; row++)
        {
            for (int col = 0; col < _game.BoardSize; col++)
            {
                Cells.Add(new CellViewModel(row, col));
            }
        }

        CellClickCommand = new RelayCommand(ExecuteCellClick, CanExecuteCellClick);
        NewGameCommand = new RelayCommand(ExecuteNewGame);

        SyncBoardState();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<CellViewModel> Cells { get; }

    public IReadOnlyList<DifficultyOption> DifficultyOptions { get; }

    public DifficultyOption SelectedDifficulty
    {
        get => _selectedDifficulty;
        set => SetField(ref _selectedDifficulty, value);
    }

    public int BoardSize => _game.BoardSize;

    public bool IsGameOver => _game.IsGameOver;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public int HumanWins
    {
        get => _humanWins;
        private set => SetField(ref _humanWins, value);
    }

    public int ComputerWins
    {
        get => _computerWins;
        private set => SetField(ref _computerWins, value);
    }

    public int Draws
    {
        get => _draws;
        private set => SetField(ref _draws, value);
    }

    public RelayCommand CellClickCommand { get; }

    public ICommand NewGameCommand { get; }

    private bool CanExecuteCellClick(object? parameter) =>
        parameter is CellViewModel cell &&
        !_game.IsGameOver &&
        _game.GetCell(cell.Row, cell.Column) == CellState.Empty;

    private void ExecuteCellClick(object? parameter)
    {
        if (parameter is not CellViewModel cell || _game.IsGameOver)
        {
            return;
        }

        if (!_game.MakeMove(cell.Row, cell.Column, TicTacToeGame.HumanMark))
        {
            return;
        }

        if (!_game.IsGameOver)
        {
            BoardPosition? computerMove = _game.ChooseComputerMove(SelectedDifficulty.Level);
            if (computerMove.HasValue)
            {
                _game.MakeMove(
                    computerMove.Value.Row,
                    computerMove.Value.Column,
                    TicTacToeGame.ComputerMark);
            }
        }

        UpdateScoreIfGameOver();
        SyncBoardState();
    }

    private void ExecuteNewGame()
    {
        _game.Reset();
        SyncBoardState();
    }

    private void UpdateScoreIfGameOver()
    {
        switch (_game.Status)
        {
            case GameStatus.XWins:
                HumanWins++;
                break;
            case GameStatus.OWins:
                ComputerWins++;
                break;
            case GameStatus.Draw:
                Draws++;
                break;
        }
    }

    private void SyncBoardState()
    {
        var winningSet = new HashSet<BoardPosition>(_game.WinningCells);

        for (int i = 0; i < Cells.Count; i++)
        {
            CellViewModel cell = Cells[i];
            CellState state = _game.GetCell(cell.Row, cell.Column);
            bool isEnabled = !_game.IsGameOver && state == CellState.Empty;
            bool isWinning = winningSet.Contains(new BoardPosition(cell.Row, cell.Column));
            cell.Update(state, isEnabled, isWinning);
        }

        StatusMessage = _game.Status switch
        {
            GameStatus.XWins => HumanWonMessage,
            GameStatus.OWins => ComputerWonMessage,
            GameStatus.Draw => DrawMessage,
            _ => HumanTurnMessage
        };

        OnPropertyChanged(nameof(IsGameOver));
        CellClickCommand.RaiseCanExecuteChanged();
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
