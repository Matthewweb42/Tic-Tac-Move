using Godot;
using System.Collections.Generic;
using System.Linq;
using TicTacMove.Models;

namespace TicTacMove;

/// <summary>
/// Handles simultaneous movement resolution with collision detection.
/// </summary>
public class MovementResolver
{
    /// <summary>
    /// Result of the resolution phase.
    /// </summary>
    public class ResolutionResult
    {
        public List<MoveAction> SuccessfulMoves { get; } = new();
        public List<MoveAction> BlockedMoves { get; } = new();
        public List<int> DestroyedPieceIds { get; } = new();
        public List<(int pieceA, int pieceB)> Swaps { get; } = new();
    }

    /// <summary>
    /// Represents a single move action.
    /// </summary>
    public class MoveAction
    {
        public int PieceId { get; set; }
        public Vector2I From { get; set; }
        public Vector2I To { get; set; }
        public MoveDirection Direction { get; set; }
    }

    private readonly BoardState _boardState;

    public MovementResolver(BoardState boardState)
    {
        _boardState = boardState;
    }

    /// <summary>
    /// Resolve queued premoves for a specific team.
    /// </summary>
    public ResolutionResult Resolve(Team team)
    {
        var result = new ResolutionResult();
        var allPremoves = _boardState.GetQueuedPremoves();

        // Filter premoves to only this team's moves
        var teamPremoves = allPremoves
            .Where(kv => _boardState.GetPieceById(kv.Key)?.Team == team)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        if (teamPremoves.Count == 0)
            return result;

        // Step 1: Build movement intentions
        var intentions = BuildMovementIntentions(teamPremoves);

        // Step 2: Detect and handle edge blocks
        HandleEdgeBlocks(intentions, result);

        // Step 3: Detect swaps (two pieces moving to each other's positions)
        HandleSwaps(intentions, result);

        // Step 4: Detect collisions (multiple pieces moving to same cell)
        HandleCollisions(intentions, result);

        // Step 5: Handle moving into stationary pieces
        HandleStationaryBlocks(intentions, result);

        // Step 6: Execute remaining valid moves
        ExecuteValidMoves(intentions, result);

        // Step 7: Update premove queues - remove executed moves
        foreach (var pieceId in teamPremoves.Keys)
        {
            var premove = teamPremoves[pieceId];
            if (premove.MoveQueue.Count > 0)
            {
                // Dequeue the move we just executed
                premove.MoveQueue.Dequeue();

                // If no moves left, clear the premove entirely
                if (premove.MoveQueue.Count == 0)
                {
                    _boardState.ClearPremove(pieceId);
                }
                else
                {
                    // Update source position for next resolution
                    var piece = _boardState.GetPieceById(pieceId);
                    if (piece != null)
                    {
                        premove.SourcePosition = piece.Position;
                    }
                }
            }
        }

        return result;
    }

    private Dictionary<int, MoveAction> BuildMovementIntentions(
        IReadOnlyDictionary<int, PremoveData> premoves)
    {
        var intentions = new Dictionary<int, MoveAction>();

        foreach (var (pieceId, premove) in premoves)
        {
            if (premove.Direction == MoveDirection.None)
                continue; // Piece is staying

            // Use current piece position instead of source position
            var piece = _boardState.GetPieceById(pieceId);
            if (piece == null) continue;

            var targetPos = piece.Position + Piece.GetDirectionOffset(premove.Direction);

            intentions[pieceId] = new MoveAction
            {
                PieceId = pieceId,
                From = piece.Position,
                To = targetPos,
                Direction = premove.Direction
            };
        }

        return intentions;
    }

    private void HandleEdgeBlocks(Dictionary<int, MoveAction> intentions,
                                   ResolutionResult result)
    {
        var blocked = new List<int>();

        foreach (var (pieceId, action) in intentions)
        {
            if (!_boardState.IsInBounds(action.To.X, action.To.Y))
            {
                result.BlockedMoves.Add(action);
                blocked.Add(pieceId);
            }
        }

        foreach (var id in blocked)
            intentions.Remove(id);
    }

    private void HandleSwaps(Dictionary<int, MoveAction> intentions,
                             ResolutionResult result)
    {
        var processedPairs = new HashSet<(int, int)>();

        foreach (var (pieceIdA, actionA) in intentions.ToList())
        {
            foreach (var (pieceIdB, actionB) in intentions.ToList())
            {
                if (pieceIdA >= pieceIdB) continue; // Avoid duplicate checks

                // Already processed this pair
                var pairKey = (pieceIdA, pieceIdB);
                if (processedPairs.Contains(pairKey)) continue;

                // Check if they're swapping: A->B's position and B->A's position
                if (actionA.To == actionB.From && actionB.To == actionA.From)
                {
                    // This is a valid swap!
                    result.Swaps.Add((pieceIdA, pieceIdB));
                    result.SuccessfulMoves.Add(actionA);
                    result.SuccessfulMoves.Add(actionB);
                    processedPairs.Add(pairKey);

                    // Remove from intentions (handled separately)
                    intentions.Remove(pieceIdA);
                    intentions.Remove(pieceIdB);
                }
            }
        }
    }

    private void HandleCollisions(Dictionary<int, MoveAction> intentions,
                                   ResolutionResult result)
    {
        // Group moves by target position
        var movesByTarget = intentions
            .GroupBy(kv => kv.Value.To)
            .Where(g => g.Count() > 1) // Only collisions
            .ToList();

        foreach (var collision in movesByTarget)
        {
            // All pieces moving to this cell are destroyed
            foreach (var (pieceId, action) in collision)
            {
                result.DestroyedPieceIds.Add(pieceId);
                intentions.Remove(pieceId);
            }
        }
    }

    private void HandleStationaryBlocks(Dictionary<int, MoveAction> intentions,
                                         ResolutionResult result)
    {
        var blocked = new List<int>();

        foreach (var (pieceId, action) in intentions)
        {
            var targetPiece = _boardState.GetPieceAt(action.To);

            if (targetPiece != null)
            {
                // Check if target piece is moving away
                bool targetIsMoving = intentions.ContainsKey(targetPiece.Id);

                if (!targetIsMoving)
                {
                    // Target is stationary - block this move
                    result.BlockedMoves.Add(action);
                    blocked.Add(pieceId);
                }
            }
        }

        foreach (var id in blocked)
            intentions.Remove(id);
    }

    private void ExecuteValidMoves(Dictionary<int, MoveAction> intentions,
                                    ResolutionResult result)
    {
        // Execute swaps first (they were already added to SuccessfulMoves)
        foreach (var (pieceIdA, pieceIdB) in result.Swaps)
        {
            var pieceA = _boardState.GetPieceById(pieceIdA);
            var pieceB = _boardState.GetPieceById(pieceIdB);

            if (pieceA != null && pieceB != null)
            {
                var posA = pieceA.Position;
                var posB = pieceB.Position;

                // Swap positions
                _boardState.MovePiece(pieceIdA, posB);
                _boardState.MovePiece(pieceIdB, posA);
            }
        }

        // Execute remaining valid moves
        foreach (var (pieceId, action) in intentions)
        {
            _boardState.MovePiece(pieceId, action.To);
            result.SuccessfulMoves.Add(action);
        }

        // Remove destroyed pieces
        foreach (var pieceId in result.DestroyedPieceIds)
        {
            _boardState.RemovePiece(pieceId);
        }
    }
}
