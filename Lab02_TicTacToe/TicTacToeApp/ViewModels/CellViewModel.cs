using System.ComponentModel;
using System.Runtime.CompilerServices;
using TicTacToeApp.Models;

namespace TicTacToeApp.ViewModels;

/// <summary>
/// Представлення однієї клітинки поля для прив'язування в XAML.
/// </summary>
public sealed class CellViewModel : INotifyPropertyChanged
{
    public const string EmptySymbol = "";
    public const string XSymbol = "X";
    public const string OSymbol = "O";

    private CellState _state = CellState.Empty;
    private string _symbol = EmptySymbol;
    private bool _isEnabled = true;
    private bool _isWinningCell;

    public CellViewModel(int row, int column)
    {
        Row = row;
        Column = column;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Row { get; }

    public int Column { get; }

    public CellState State
    {
        get => _state;
        private set => SetField(ref _state, value);
    }

    public string Symbol
    {
        get => _symbol;
        private set => SetField(ref _symbol, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        private set => SetField(ref _isEnabled, value);
    }

    public bool IsWinningCell
    {
        get => _isWinningCell;
        private set => SetField(ref _isWinningCell, value);
    }

    public void Update(CellState state, bool isEnabled, bool isWinningCell)
    {
        State = state;
        Symbol = state switch
        {
            CellState.X => XSymbol,
            CellState.O => OSymbol,
            _ => EmptySymbol
        };
        IsEnabled = isEnabled;
        IsWinningCell = isWinningCell;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
