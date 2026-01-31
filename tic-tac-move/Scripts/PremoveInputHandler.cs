using Godot;
using TicTacMove.Models;

namespace TicTacMove;

/// <summary>
/// Handles user input for setting premove directions.
/// Displays adjacent cell highlights and direction indicators.
/// </summary>
public partial class PremoveInputHandler : Node
{
    [Signal]
    public delegate void PremoveSelectedEventHandler(int pieceId, int direction);

    [Signal]
    public delegate void PremoveConfirmedEventHandler();

    private BoardState _boardState;
    private GridCell[,] _gridCells;
    private Piece _activePiece;
    private MoveDirection _selectedDirection = MoveDirection.None;
    private Button _confirmButton;
    private Button _skipButton;
    private Control _buttonContainer;

    public bool IsActive => _activePiece != null;

    public void Initialize(BoardState board, GridCell[,] cells, Control parentForButtons)
    {
        _boardState = board;
        _gridCells = cells;

        CreateButtons(parentForButtons);
    }

    private void CreateButtons(Control parent)
    {
        _buttonContainer = new Control
        {
            Visible = false
        };
        parent.AddChild(_buttonContainer);

        // Confirm button
        _confirmButton = new Button
        {
            Text = "Confirm Move",
            CustomMinimumSize = new Vector2(120, 40)
        };
        _confirmButton.Pressed += OnConfirmPressed;
        _buttonContainer.AddChild(_confirmButton);

        // Skip button (no movement)
        _skipButton = new Button
        {
            Text = "Stay",
            CustomMinimumSize = new Vector2(80, 40),
            Position = new Vector2(130, 0)
        };
        _skipButton.Pressed += OnSkipPressed;
        _buttonContainer.AddChild(_skipButton);
    }

    /// <summary>
    /// Position buttons relative to viewport.
    /// </summary>
    public void UpdateButtonPositions(Vector2 viewportSize)
    {
        if (_buttonContainer == null) return;

        // Position buttons at the bottom center
        float totalWidth = 220;
        _buttonContainer.Position = new Vector2(
            (viewportSize.X - totalWidth) / 2,
            viewportSize.Y - 60
        );
    }

    /// <summary>
    /// Begin premove input for a newly placed piece.
    /// </summary>
    public void BeginPremoveInput(Piece piece)
    {
        _activePiece = piece;
        _selectedDirection = MoveDirection.None;

        // Show adjacent cell highlights
        ShowAdjacentHighlights(piece.Position);

        // Mark the placed piece as active
        var cell = _gridCells[piece.Position.X, piece.Position.Y];
        cell.SetActiveForPremove(true);

        // Show buttons
        _buttonContainer.Visible = true;
        _confirmButton.Text = "Confirm (Stay)";
    }

    /// <summary>
    /// End premove input and clean up visuals.
    /// </summary>
    public void EndPremoveInput()
    {
        if (_activePiece != null)
        {
            // Clear active state
            var cell = _gridCells[_activePiece.Position.X, _activePiece.Position.Y];
            cell.SetActiveForPremove(false);
        }

        _activePiece = null;
        HideAllHighlights();
        _buttonContainer.Visible = false;
    }

    /// <summary>
    /// Handle a cell click during premove input.
    /// Returns true if the click was handled.
    /// </summary>
    public bool HandleCellClick(int row, int col)
    {
        if (_activePiece == null) return false;

        var clickedPos = new Vector2I(row, col);
        var piecePos = _activePiece.Position;

        // Check if clicking on the piece itself (stay)
        if (clickedPos == piecePos)
        {
            SelectDirection(MoveDirection.None);
            return true;
        }

        // Check if clicking an adjacent cell
        var direction = GetDirectionFromPositions(piecePos, clickedPos);
        if (direction != MoveDirection.None)
        {
            SelectDirection(direction);
            return true;
        }

        return false;
    }

    private void SelectDirection(MoveDirection direction)
    {
        _selectedDirection = direction;

        // Clear previous arrow
        if (_activePiece != null)
        {
            var pieceCell = _gridCells[_activePiece.Position.X, _activePiece.Position.Y];
            pieceCell.ShowDirectionArrow(direction, direction != MoveDirection.None);
        }

        // Update button text
        if (direction == MoveDirection.None)
        {
            _confirmButton.Text = "Confirm (Stay)";
        }
        else
        {
            _confirmButton.Text = $"Confirm ({direction})";
        }

        // Set the premove on the board state
        if (_activePiece != null)
        {
            _boardState.SetPremove(_activePiece.Id, direction);
            EmitSignal(SignalName.PremoveSelected, _activePiece.Id, (int)direction);
        }
    }

    private void OnConfirmPressed()
    {
        EmitSignal(SignalName.PremoveConfirmed);
    }

    private void OnSkipPressed()
    {
        SelectDirection(MoveDirection.None);
        EmitSignal(SignalName.PremoveConfirmed);
    }

    private MoveDirection GetDirectionFromPositions(Vector2I from, Vector2I to)
    {
        var diff = to - from;

        // Must be exactly one cell away (adjacent)
        if (Mathf.Abs(diff.X) + Mathf.Abs(diff.Y) != 1)
            return MoveDirection.None;

        if (diff.X == -1) return MoveDirection.Up;
        if (diff.X == 1) return MoveDirection.Down;
        if (diff.Y == -1) return MoveDirection.Left;
        if (diff.Y == 1) return MoveDirection.Right;

        return MoveDirection.None;
    }

    private void ShowAdjacentHighlights(Vector2I position)
    {
        var directions = new (MoveDirection dir, Vector2I offset)[]
        {
            (MoveDirection.Up, new Vector2I(-1, 0)),
            (MoveDirection.Down, new Vector2I(1, 0)),
            (MoveDirection.Left, new Vector2I(0, -1)),
            (MoveDirection.Right, new Vector2I(0, 1))
        };

        foreach (var (_, offset) in directions)
        {
            var targetPos = position + offset;

            if (_boardState.IsInBounds(targetPos.X, targetPos.Y))
            {
                var cell = _gridCells[targetPos.X, targetPos.Y];
                cell.ShowPremoveHighlight(true);
            }
        }
    }

    private void HideAllHighlights()
    {
        int size = _gridCells.GetLength(0);
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                _gridCells[row, col].ShowPremoveHighlight(false);
                _gridCells[row, col].ShowDirectionArrow(MoveDirection.None, false);
            }
        }
    }

    /// <summary>
    /// Clear all visual indicators (for game reset).
    /// </summary>
    public void Reset()
    {
        EndPremoveInput();
        _selectedDirection = MoveDirection.None;
    }
}
