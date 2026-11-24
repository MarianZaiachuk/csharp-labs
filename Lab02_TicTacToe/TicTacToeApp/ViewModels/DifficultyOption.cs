using TicTacToeApp.Models;

namespace TicTacToeApp.ViewModels;

/// <summary>
/// Варіант вибору рівня складності комп'ютера для інтерфейсу.
/// </summary>
public sealed record DifficultyOption(DifficultyLevel Level, string DisplayName);
