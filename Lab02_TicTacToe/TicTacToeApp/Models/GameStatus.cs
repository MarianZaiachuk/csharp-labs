namespace TicTacToeApp.Models;

/// <summary>
/// Поточний результат або стан партії.
/// </summary>
public enum GameStatus
{
    InProgress = 0,
    XWins = 1,
    OWins = 2,
    Draw = 3
}
