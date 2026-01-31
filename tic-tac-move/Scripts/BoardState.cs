using Godot;
using System.Collections.Generic;
using TicTacMove.Models;

namespace TicTacMove;

/// <summary>
/// Represents the state of the game board.
/// Handles board data, piece management, and state queries.
/// </summary>
public class BoardState
{
    // Keep CellValue for backward compatibility with WinChecker
    public enum CellValue
    {
        Empty,
        X,
        O
    }

    public int Size { get; private set; }
    public Team CurrentTeam { get; private set; } = Team.Blue;

    /// <summary>All pieces currently on the board, keyed by ID.</summary>
    private Dictionary<int, Piece> _pieces = new();

    /// <summary>Grid lookup: position -> piece at that position (or null).</summary>
    private Piece[,] _grid;

    /// <summary>Premoves queued for resolution.</summary>
    private Dictionary<int, PremoveData> _queuedPremoves = new();

    // Legacy support
    public CellValue CurrentPlayer => CurrentTeam == Team.Blue ? CellValue.X : CellValue.O;

    public BoardState(int size = 3)
    {
        Size = size;
        _grid = new Piece[size, size];
    }

    public void Reset()
    {
        _pieces.Clear();
        _queuedPremoves.Clear();
        _grid = new Piece[Size, Size];
        CurrentTeam = Team.Blue;
        Piece.ResetIdCounter();
    }

    /// <summary>
    /// Get the piece at the specified position.
    /// </summary>
    public Piece GetPieceAt(int row, int col)
    {
        if (!IsInBounds(row, col)) return null;
        return _grid[row, col];
    }

    /// <summary>
    /// Get the piece at the specified position.
    /// </summary>
    public Piece GetPieceAt(Vector2I pos) => GetPieceAt(pos.X, pos.Y);

    /// <summary>
    /// Check if a cell is empty.
    /// </summary>
    public bool IsEmpty(int row, int col)
    {
        return IsInBounds(row, col) && _grid[row, col] == null;
    }

    /// <summary>
    /// Check if coordinates are within board bounds.
    /// </summary>
    public bool IsInBounds(int row, int col)
    {
        return row >= 0 && row < Size && col >= 0 && col < Size;
    }

    /// <summary>
    /// Check if coordinates are within board bounds.
    /// </summary>
    public bool IsInBounds(Vector2I pos) => IsInBounds(pos.X, pos.Y);

    /// <summary>
    /// Place a new piece on the board.
    /// </summary>
    public Piece PlacePiece(int row, int col, int turnNumber, Team team)
    {
        if (!IsEmpty(row, col)) return null;

        var piece = new Piece(team, new Vector2I(row, col))
        {
            PlacedOnTurn = turnNumber,
            JustPlaced = true
        };

        _pieces[piece.Id] = piece;
        _grid[row, col] = piece;

        return piece;
    }

    /// <summary>
    /// Set a premove for a piece.
    /// </summary>
    public void SetPremove(int pieceId, MoveDirection direction)
    {
        if (!_pieces.TryGetValue(pieceId, out var piece)) return;

        piece.QueuedMove = direction;

        if (direction == MoveDirection.None)
        {
            _queuedPremoves.Remove(pieceId);
        }
        else
        {
            // If premove already exists, add to the queue
            if (_queuedPremoves.TryGetValue(pieceId, out var existingPremove))
            {
                existingPremove.MoveQueue.Enqueue(direction);
            }
            else
            {
                // Create new premove data with queue
                var premove = new PremoveData
                {
                    PieceId = pieceId,
                    SourcePosition = piece.Position
                };
                premove.MoveQueue.Enqueue(direction);
                _queuedPremoves[pieceId] = premove;
            }
        }
    }

    /// <summary>
    /// Get the number of premoves queued for a piece.
    /// </summary>
    public int GetPremoveCount(int pieceId)
    {
        if (_queuedPremoves.TryGetValue(pieceId, out var premove))
        {
            return premove.MoveQueue.Count;
        }
        return 0;
    }

    /// <summary>
    /// Clear a premove for a piece.
    /// </summary>
    public void ClearPremove(int pieceId)
    {
        if (_pieces.TryGetValue(pieceId, out var piece))
        {
            piece.QueuedMove = MoveDirection.None;
        }
        _queuedPremoves.Remove(pieceId);
    }

    /// <summary>
    /// Clear all queued premoves.
    /// </summary>
    public void ClearAllPremoves()
    {
        foreach (var piece in _pieces.Values)
        {
            piece.QueuedMove = MoveDirection.None;
        }
        _queuedPremoves.Clear();
    }

    /// <summary>
    /// Get all queued premoves.
    /// </summary>
    public IReadOnlyDictionary<int, PremoveData> GetQueuedPremoves() => _queuedPremoves;

    /// <summary>
    /// Get all pieces on the board.
    /// </summary>
    public IEnumerable<Piece> GetAllPieces() => _pieces.Values;

    /// <summary>
    /// Get a piece by ID.
    /// </summary>
    public Piece GetPieceById(int pieceId)
    {
        _pieces.TryGetValue(pieceId, out var piece);
        return piece;
    }

    /// <summary>
    /// Remove a piece from the board.
    /// </summary>
    public void RemovePiece(int pieceId)
    {
        if (_pieces.TryGetValue(pieceId, out var piece))
        {
            _grid[piece.Position.X, piece.Position.Y] = null;
            _pieces.Remove(pieceId);
            _queuedPremoves.Remove(pieceId);
        }
    }

    /// <summary>
    /// Move a piece to a new position.
    /// </summary>
    public void MovePiece(int pieceId, Vector2I newPosition)
    {
        if (!_pieces.TryGetValue(pieceId, out var piece)) return;

        _grid[piece.Position.X, piece.Position.Y] = null;
        piece.Position = newPosition;
        _grid[newPosition.X, newPosition.Y] = piece;
    }

    /// <summary>
    /// Switch to the other team.
    /// </summary>
    public void SwitchTeam()
    {
        CurrentTeam = CurrentTeam == Team.Blue ? Team.Red : Team.Blue;
    }

    /// <summary>
    /// Clear JustPlaced flags on all pieces.
    /// </summary>
    public void ClearJustPlacedFlags()
    {
        foreach (var piece in _pieces.Values)
        {
            piece.JustPlaced = false;
        }
    }

    /// <summary>
    /// Check if the board is full.
    /// </summary>
    public bool IsBoardFull()
    {
        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                if (_grid[row, col] == null)
                    return false;
            }
        }
        return true;
    }

    // --- Legacy compatibility methods for WinChecker ---

    /// <summary>
    /// Get cell value for WinChecker compatibility.
    /// </summary>
    public CellValue GetCell(int row, int col)
    {
        var piece = GetPieceAt(row, col);
        if (piece == null) return CellValue.Empty;
        return piece.Team == Team.Blue ? CellValue.X : CellValue.O;
    }

    /// <summary>
    /// Check if a cell is empty (legacy compatibility).
    /// </summary>
    public bool IsCellEmpty(int row, int col)
    {
        return IsEmpty(row, col);
    }

    /// <summary>
    /// Place a mark (legacy compatibility - wraps PlacePiece).
    /// Returns true if successful.
    /// </summary>
    public bool PlaceMark(int row, int col)
    {
        return PlacePiece(row, col, 0, CurrentTeam) != null;
    }

    /// <summary>
    /// Switch player (legacy compatibility - wraps SwitchTeam).
    /// </summary>
    public void SwitchPlayer()
    {
        SwitchTeam();
    }
}
