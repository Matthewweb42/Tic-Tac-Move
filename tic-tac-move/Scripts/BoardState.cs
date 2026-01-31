using Godot;

namespace TicTacMove;

/// <summary>
/// Represents the state of the game board.
/// Handles board data and state queries.
/// </summary>
public class BoardState
{
    public enum CellValue
    {
        Empty,
        X,
        O
    }

    private CellValue[,] _cells;
    public int Size { get; private set; }

    public CellValue CurrentPlayer { get; private set; } = CellValue.X;

    public BoardState(int size = 3)
    {
        Size = size;
        _cells = new CellValue[size, size];
    }

    public void Reset()
    {
        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                _cells[row, col] = CellValue.Empty;
            }
        }
        CurrentPlayer = CellValue.X;
    }

    public CellValue GetCell(int row, int col)
    {
        return _cells[row, col];
    }

    public bool IsCellEmpty(int row, int col)
    {
        return _cells[row, col] == CellValue.Empty;
    }

    public bool PlaceMark(int row, int col)
    {
        if (!IsCellEmpty(row, col))
            return false;

        _cells[row, col] = CurrentPlayer;
        return true;
    }

    public void SwitchPlayer()
    {
        CurrentPlayer = CurrentPlayer == CellValue.X ? CellValue.O : CellValue.X;
    }

    public bool IsBoardFull()
    {
        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                if (_cells[row, col] == CellValue.Empty)
                    return false;
            }
        }
        return true;
    }
}
