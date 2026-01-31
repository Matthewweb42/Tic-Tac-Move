using Godot;

namespace TicTacMove.Models;

/// <summary>
/// Represents the owner/team of a piece.
/// </summary>
public enum Team
{
    None,
    Blue,  // Formerly X
    Red    // Formerly O
}

/// <summary>
/// Represents a direction for movement.
/// </summary>
public enum MoveDirection
{
    None,
    Up,
    Down,
    Left,
    Right
}

/// <summary>
/// Result of a premove after resolution.
/// </summary>
public enum PremoveResult
{
    Pending,
    Moved,
    Blocked,
    Destroyed,
    Swapped
}

/// <summary>
/// Stores premove information for a single piece.
/// </summary>
public class PremoveData
{
    public int PieceId { get; set; }
    public System.Collections.Generic.Queue<MoveDirection> MoveQueue { get; set; } = new();
    public Vector2I SourcePosition { get; set; }
    public PremoveResult Result { get; set; } = PremoveResult.Pending;

    /// <summary>
    /// Legacy property for backward compatibility - returns the first direction or None.
    /// </summary>
    public MoveDirection Direction => MoveQueue.Count > 0 ? MoveQueue.Peek() : MoveDirection.None;

    /// <summary>
    /// Legacy property for backward compatibility - calculates target from first move.
    /// </summary>
    public Vector2I TargetPosition
    {
        get
        {
            if (MoveQueue.Count == 0) return SourcePosition;
            var direction = MoveQueue.Peek();
            return SourcePosition + Piece.GetDirectionOffset(direction);
        }
    }
}

/// <summary>
/// Base class for all game pieces. Designed for extensibility.
/// </summary>
public class Piece
{
    private static int _nextId = 1;

    /// <summary>Unique identifier for this piece instance.</summary>
    public int Id { get; }

    /// <summary>Which team owns this piece.</summary>
    public Team Team { get; }

    /// <summary>Current position on the board (row, col).</summary>
    public Vector2I Position { get; set; }

    /// <summary>Queued movement direction for resolution phase.</summary>
    public MoveDirection QueuedMove { get; set; } = MoveDirection.None;

    /// <summary>Whether this piece was placed this turn (can set premove).</summary>
    public bool JustPlaced { get; set; }

    /// <summary>Turn number when this piece was placed.</summary>
    public int PlacedOnTurn { get; set; }

    // --- Extensibility fields for future piece types ---

    /// <summary>Health points (for future combat system).</summary>
    public int Health { get; set; } = 1;

    /// <summary>Maximum health (for future combat system).</summary>
    public int MaxHealth { get; set; } = 1;

    /// <summary>Attack power (for future combat system).</summary>
    public int AttackPower { get; set; } = 0;

    /// <summary>Piece type identifier for special abilities.</summary>
    public virtual string PieceType => "Standard";

    public Piece(Team team, Vector2I position)
    {
        Id = _nextId++;
        Team = team;
        Position = position;
        JustPlaced = true;
    }

    /// <summary>
    /// Calculate the target position based on queued move.
    /// </summary>
    public Vector2I GetTargetPosition()
    {
        return QueuedMove switch
        {
            MoveDirection.Up => Position + new Vector2I(-1, 0),
            MoveDirection.Down => Position + new Vector2I(1, 0),
            MoveDirection.Left => Position + new Vector2I(0, -1),
            MoveDirection.Right => Position + new Vector2I(0, 1),
            _ => Position
        };
    }

    /// <summary>
    /// Get the offset vector for a given direction.
    /// </summary>
    public static Vector2I GetDirectionOffset(MoveDirection direction)
    {
        return direction switch
        {
            MoveDirection.Up => new Vector2I(-1, 0),
            MoveDirection.Down => new Vector2I(1, 0),
            MoveDirection.Left => new Vector2I(0, -1),
            MoveDirection.Right => new Vector2I(0, 1),
            _ => Vector2I.Zero
        };
    }

    /// <summary>
    /// Virtual method for future piece-specific behaviors.
    /// </summary>
    public virtual void OnResolutionStart() { }

    /// <summary>
    /// Virtual method for future piece-specific post-move effects.
    /// </summary>
    public virtual void OnResolutionComplete() { }

    /// <summary>
    /// Reset the static ID counter (useful for testing/game reset).
    /// </summary>
    public static void ResetIdCounter()
    {
        _nextId = 1;
    }
}
