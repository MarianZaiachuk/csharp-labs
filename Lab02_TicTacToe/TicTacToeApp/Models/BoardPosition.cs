namespace TicTacToeApp.Models;

/// <summary>
/// Координата клітинки на двовимірному полі.
/// </summary>
public readonly record struct BoardPosition(int Row, int Column);
