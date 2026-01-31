using Godot;
using System.Collections.Generic;

namespace TicTacMove;

/// <summary>
/// Handles win detection logic for Tic Tac Toe.
/// Returns winning line positions when a win is detected.
/// Supports variable grid sizes.
/// </summary>
public class WinChecker
{
    public class WinResult
    {
        public bool IsWin { get; set; }
        public BoardState.CellValue Winner { get; set; }
        public Vector2I[] WinningCells { get; set; } = System.Array.Empty<Vector2I>();
    }

    /// <summary>
    /// Generate all win patterns for a given grid size.
    /// Win requires filling an entire row, column, or diagonal.
    /// </summary>
    private static List<Vector2I[]> GenerateWinPatterns(int size)
    {
        var patterns = new List<Vector2I[]>();

        // Rows
        for (int row = 0; row < size; row++)
        {
            var pattern = new Vector2I[size];
            for (int col = 0; col < size; col++)
            {
                pattern[col] = new Vector2I(row, col);
            }
            patterns.Add(pattern);
        }

        // Columns
        for (int col = 0; col < size; col++)
        {
            var pattern = new Vector2I[size];
            for (int row = 0; row < size; row++)
            {
                pattern[row] = new Vector2I(row, col);
            }
            patterns.Add(pattern);
        }

        // Diagonal (top-left to bottom-right)
        var diag1 = new Vector2I[size];
        for (int i = 0; i < size; i++)
        {
            diag1[i] = new Vector2I(i, i);
        }
        patterns.Add(diag1);

        // Diagonal (top-right to bottom-left)
        var diag2 = new Vector2I[size];
        for (int i = 0; i < size; i++)
        {
            diag2[i] = new Vector2I(i, size - 1 - i);
        }
        patterns.Add(diag2);

        return patterns;
    }

    public WinResult CheckForWin(BoardState board)
    {
        var patterns = GenerateWinPatterns(board.Size);

        foreach (var pattern in patterns)
        {
            var firstCell = board.GetCell(pattern[0].X, pattern[0].Y);
            
            if (firstCell == BoardState.CellValue.Empty)
                continue;

            bool allMatch = true;
            for (int i = 1; i < pattern.Length; i++)
            {
                if (board.GetCell(pattern[i].X, pattern[i].Y) != firstCell)
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch)
            {
                return new WinResult
                {
                    IsWin = true,
                    Winner = firstCell,
                    WinningCells = pattern
                };
            }
        }

        return new WinResult { IsWin = false };
    }
}
