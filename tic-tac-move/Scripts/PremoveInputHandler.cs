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
    private Vector2I _currentProjectedPosition;  // Where the piece would be after queued moves
    private int _maxPremoves = 3;  // n-2 where n=5

    public bool IsActive => _activePiece != null;

    public void Initialize(BoardState board, GridCell[,] cells, Control parentForButtons)
    {
        _boardState = board;
        _gridCells = cells;
        _maxPremoves = board.Size - 2;  // n-2 where n is board size

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
        _currentProjectedPosition = piece.Position;

        // Show adjacent cell highlights
        ShowAdjacentHighlights(_currentProjectedPosition);

        // Mark the placed piece as active
        var cell = _gridCells[piece.Position.X, piece.Position.Y];
        cell.SetActiveForPremove(true);

        // Show buttons
        _buttonContainer.Visible = true;
        UpdateButtonText();
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

        // Check if clicking on the piece itself (stay/skip remaining moves)
        if (clickedPos == _activePiece.Position)
        {
            // If no moves set yet, treat as "stay"
            if (_boardState.GetPremoveCount(_activePiece.Id) == 0)
            {
                SelectDirection(MoveDirection.None);
            }
            else
            {
                // Already have moves, so just confirm
                EmitSignal(SignalName.PremoveConfirmed);
            }
            return true;
        }

        // Check if clicking an adjacent cell from the current projected position
        var direction = GetDirectionFromPositions(_currentProjectedPosition, clickedPos);
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

        if (direction == MoveDirection.None)
        {
            // Clear all premoves and confirm
            _boardState.ClearPremove(_activePiece.Id);
            EmitSignal(SignalName.PremoveConfirmed);
            return;
        }

        // Add this direction to the queue
        _boardState.SetPremove(_activePiece.Id, direction);
        EmitSignal(SignalName.PremoveSelected, _activePiece.Id, (int)direction);

        // Update projected position
        _currentProjectedPosition += Piece.GetDirectionOffset(direction);

        // Update visuals - show path
        UpdatePathVisuals();

        // Check if we've reached the limit
        int currentCount = _boardState.GetPremoveCount(_activePiece.Id);
        if (currentCount >= _maxPremoves)
        {
            // Auto-confirm after reaching max
            EmitSignal(SignalName.PremoveConfirmed);
        }
        else
        {
            // Update highlights for next move
            HideAllHighlights();
            ShowAdjacentHighlights(_currentProjectedPosition);
            UpdateButtonText();
        }
    }

    private void UpdatePathVisuals()
    {
        // Show arrows along the path
        var premoveData = _boardState.GetQueuedPremoves();
        if (!premoveData.TryGetValue(_activePiece.Id, out var data)) return;

        var position = _activePiece.Position;
        foreach (var dir in data.MoveQueue)
        {
            if (_boardState.IsInBounds(position.X, position.Y))
            {
                var cell = _gridCells[position.X, position.Y];
                cell.ShowDirectionArrow(dir, true);
            }
            position += Piece.GetDirectionOffset(dir);
        }
    }

    private void UpdateButtonText()
    {
        int currentCount = _boardState.GetPremoveCount(_activePiece.Id);
        if (currentCount == 0)
        {
            _confirmButton.Text = "Confirm (Stay)";
        }
        else
        {
            _confirmButton.Text = $"Confirm ({currentCount}/{_maxPremoves} moves)";
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

        // Get the team of the active piece
        Team team = _activePiece != null ? _activePiece.Team : Team.Blue;

        foreach (var (_, offset) in directions)
        {
            var targetPos = position + offset;

            if (_boardState.IsInBounds(targetPos.X, targetPos.Y))
            {
                var cell = _gridCells[targetPos.X, targetPos.Y];
                cell.ShowPremoveHighlight(true, team);
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
