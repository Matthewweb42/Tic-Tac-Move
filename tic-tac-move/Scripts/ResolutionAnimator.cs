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

    private const float RevealDuration = 0.4f;
    private const float MoveDuration = 0.3f;
    private const float DestroyDuration = 0.3f;

    private GridCell[,] _gridCells;
    private BoardState _boardState;
    private ScreenShake _screenShake;
    private Node _particleContainer;

    public void Initialize(GridCell[,] cells, BoardState boardState)
    {
        _gridCells = cells;
        _boardState = boardState;

        // Create container for particle effects
        _particleContainer = new Node();
        AddChild(_particleContainer);
    }

    public void SetScreenShake(ScreenShake shake)
    {
        _screenShake = shake;
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

        // Phase 4: Animate destructions with screen shake
        if (result.DestroyedPieceIds.Count > 0)
        {
            _screenShake?.ShakeCollision();

            // Spawn destruction particles for each destroyed piece location
            foreach (var pieceId in result.DestroyedPieceIds)
            {
                // Pieces are already removed, but we can still show effects
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

        // Create a trail effect - spawn fading copies along the path
        SpawnTrail(fromCell, toCell, mark);

        // Clear source
        fromCell.ClearMark();

        // Brief pause for movement effect
        await ToSignal(GetTree().CreateTimer(MoveDuration), SceneTreeTimer.SignalName.Timeout);

        // Set mark at destination
        toCell.SetMark(mark);
    }

    private void SpawnTrail(GridCell fromCell, GridCell toCell, BoardState.CellValue mark)
    {
        // Create a simple trail by spawning colored rects that fade out
        var fromPos = fromCell.GlobalPosition;
        var toPos = toCell.GlobalPosition;
        var size = new Vector2(fromCell.CellPixelSize, fromCell.CellPixelSize);

        Color trailColor = mark == BoardState.CellValue.X
            ? new Color(0.3f, 0.7f, 1.0f, 0.5f)
            : new Color(1.0f, 0.4f, 0.4f, 0.5f);

        // Spawn trail particles
        for (int i = 0; i < 5; i++)
        {
            float t = i / 5f;
            var pos = fromPos.Lerp(toPos, t);

            var trail = new ColorRect
            {
                Color = trailColor,
                Size = size * 0.6f,
                Position = pos + size * 0.2f,
                ZIndex = -1
            };

            GetTree().Root.AddChild(trail);

            // Fade out the trail
            var tween = CreateTween();
            tween.TweenProperty(trail, "modulate:a", 0f, MoveDuration * (1f - t * 0.5f));
            tween.TweenCallback(Callable.From(() => trail.QueueFree()));
        }
    }

    private async Task AnimateBlocked(MovementResolver.MoveAction blocked)
    {
        var cell = _gridCells[blocked.From.X, blocked.From.Y];

        // Shake animation - quick position oscillation
        var originalPos = cell.Position;

        for (int i = 0; i < 4; i++)
        {
            float offset = 6f * (1f - i / 4f);
            cell.Position = originalPos + new Vector2(offset, 0);
            await ToSignal(GetTree().CreateTimer(0.025f), SceneTreeTimer.SignalName.Timeout);
            cell.Position = originalPos - new Vector2(offset, 0);
            await ToSignal(GetTree().CreateTimer(0.025f), SceneTreeTimer.SignalName.Timeout);
        }

        cell.Position = originalPos;
    }

    /// <summary>
    /// Spawn explosion particles at a cell location.
    /// </summary>
    public void SpawnExplosion(Vector2 position, Color color)
    {
        int particleCount = 12;

        for (int i = 0; i < particleCount; i++)
        {
            float angle = (Mathf.Tau / particleCount) * i;
            float speed = (float)GD.RandRange(80, 150);
            var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

            var particle = new ColorRect
            {
                Color = color,
                Size = new Vector2(8, 8),
                Position = position
            };

            GetTree().Root.AddChild(particle);

            var tween = CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(particle, "position", position + velocity * 0.5f, 0.4f);
            tween.TweenProperty(particle, "modulate:a", 0f, 0.4f);
            tween.TweenProperty(particle, "scale", Vector2.Zero, 0.4f);
            tween.SetParallel(false);
            tween.TweenCallback(Callable.From(() => particle.QueueFree()));
        }
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
