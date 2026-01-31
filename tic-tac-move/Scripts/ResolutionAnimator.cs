using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicTacMove.Models;

namespace TicTacMove;

/// <summary>
/// Handles visual animations during the resolution phase.
/// </summary>
public partial class ResolutionAnimator : Node
{
    [Signal]
    public delegate void AnimationCompleteEventHandler();

    private const float RevealDuration = 0.3f;
    private const float MoveDuration = 0.25f;
    private const float DestroyDuration = 0.2f;

    private GridCell[,] _gridCells;
    private BoardState _boardState;

    public void Initialize(GridCell[,] cells, BoardState boardState)
    {
        _gridCells = cells;
        _boardState = boardState;
    }

    /// <summary>
    /// Animate the resolution phase.
    /// </summary>
    public async Task AnimateResolution(MovementResolver.ResolutionResult result)
    {
        // Phase 1: Brief pause to show resolution is happening
        await ToSignal(GetTree().CreateTimer(RevealDuration), SceneTreeTimer.SignalName.Timeout);

        // Phase 2: Animate all movements simultaneously
        var moveTasks = new List<Task>();

        foreach (var move in result.SuccessfulMoves)
        {
            moveTasks.Add(AnimateMove(move));
        }

        foreach (var blocked in result.BlockedMoves)
        {
            moveTasks.Add(AnimateBlocked(blocked));
        }

        // Wait for all movement animations
        if (moveTasks.Count > 0)
        {
            await Task.WhenAll(moveTasks);
        }

        // Phase 3: Update the grid visuals to reflect final state
        RefreshGridDisplay();

        // Phase 4: Animate destructions
        if (result.DestroyedPieceIds.Count > 0)
        {
            var destroyTasks = new List<Task>();
            foreach (var pieceId in result.DestroyedPieceIds)
            {
                // The piece was already removed from BoardState,
                // we just need to clear the visual if any remains
            }

            await ToSignal(GetTree().CreateTimer(DestroyDuration), SceneTreeTimer.SignalName.Timeout);
        }

        EmitSignal(SignalName.AnimationComplete);
    }

    private async Task AnimateMove(MovementResolver.MoveAction move)
    {
        var fromCell = _gridCells[move.From.X, move.From.Y];
        var toCell = _gridCells[move.To.X, move.To.Y];

        // Get the mark color from the source cell
        var mark = fromCell.GetCurrentMark();

        // Simple animation: fade out from source, fade in at destination
        // Clear source immediately for simplicity
        fromCell.ClearMark();

        // Brief pause for movement effect
        await ToSignal(GetTree().CreateTimer(MoveDuration), SceneTreeTimer.SignalName.Timeout);

        // Set mark at destination
        toCell.SetMark(mark);
    }

    private async Task AnimateBlocked(MovementResolver.MoveAction blocked)
    {
        var cell = _gridCells[blocked.From.X, blocked.From.Y];

        // Shake animation - quick position oscillation
        var originalPos = cell.Position;

        for (int i = 0; i < 3; i++)
        {
            cell.Position = originalPos + new Vector2(5, 0);
            await ToSignal(GetTree().CreateTimer(0.03f), SceneTreeTimer.SignalName.Timeout);
            cell.Position = originalPos - new Vector2(5, 0);
            await ToSignal(GetTree().CreateTimer(0.03f), SceneTreeTimer.SignalName.Timeout);
        }

        cell.Position = originalPos;
    }

    /// <summary>
    /// Refresh the entire grid display to match the current board state.
    /// </summary>
    public void RefreshGridDisplay()
    {
        int size = _gridCells.GetLength(0);

        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                var cell = _gridCells[row, col];
                var piece = _boardState.GetPieceAt(row, col);

                // Clear any premove visuals
                cell.ShowDirectionArrow(MoveDirection.None, false);
                cell.ShowPremoveHighlight(false);
                cell.SetActiveForPremove(false);

                if (piece != null)
                {
                    cell.SetMark(piece.Team);
                }
                else
                {
                    cell.ClearMark();
                }
            }
        }
    }

    /// <summary>
    /// Clear all direction arrows (at end of resolution).
    /// </summary>
    public void ClearAllDirectionArrows()
    {
        int size = _gridCells.GetLength(0);

        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                _gridCells[row, col].ShowDirectionArrow(MoveDirection.None, false);
            }
        }
    }
}
