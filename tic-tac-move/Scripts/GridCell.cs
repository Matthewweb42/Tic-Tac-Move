using Godot;
using TicTacMove.Models;

namespace TicTacMove;

/// <summary>
/// Represents a single cell in the Tic Tac Toe grid.
/// Handles click input and displays X/O marks with premove indicators.
/// </summary>
public partial class GridCell : Control
{
    [Signal]
    public delegate void CellClickedEventHandler(int row, int col);

    [Export]
    public int Row { get; set; }

    [Export]
    public int Col { get; set; }

    public int CellPixelSize { get; set; } = 100;

    private ColorRect _background;
    private ColorRect _markDisplay;
    private ColorRect _premoveHighlight;
    private ColorRect _directionArrow;
    private ColorRect _activeBorder;

    private BoardState.CellValue _currentMark = BoardState.CellValue.Empty;
    private MoveDirection _currentDirection = MoveDirection.None;
    private bool _isActiveForPremove;
    private bool _showingPremoveHighlight;

    // Colors for X and O
    private static readonly Color XColor = new(0.2f, 0.6f, 1.0f);      // Blue for X
    private static readonly Color OColor = new(1.0f, 0.4f, 0.4f);      // Red for O
    private static readonly Color EmptyColor = new(0.15f, 0.15f, 0.2f); // Dark background
    private static readonly Color HoverColor = new(0.25f, 0.25f, 0.35f); // Lighter on hover
    private static readonly Color PremoveHighlightColor = new(0.4f, 0.4f, 0.2f, 0.4f); // Yellow highlight for premove selection

    public override void _Ready()
    {
        int markSize = (int)(CellPixelSize * 0.7f);
        int markOffset = (CellPixelSize - markSize) / 2;

        // Create background
        _background = new ColorRect
        {
            Color = EmptyColor,
            Size = new Vector2(CellPixelSize, CellPixelSize),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_background);

        // Create premove highlight overlay
        _premoveHighlight = new ColorRect
        {
            Color = PremoveHighlightColor,
            Size = new Vector2(CellPixelSize, CellPixelSize),
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_premoveHighlight);

        // Create mark display (smaller rect centered in cell)
        _markDisplay = new ColorRect
        {
            Color = Colors.Transparent,
            Size = new Vector2(markSize, markSize),
            Position = new Vector2(markOffset, markOffset),
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_markDisplay);

        // Create direction arrow indicator
        _directionArrow = new ColorRect
        {
            Color = new Color(1f, 1f, 1f, 0.8f),
            Size = new Vector2(CellPixelSize * 0.2f, CellPixelSize * 0.2f),
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_directionArrow);

        // Create active border for premove selection
        _activeBorder = new ColorRect
        {
            Color = Colors.Transparent,
            Size = new Vector2(CellPixelSize, CellPixelSize),
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_activeBorder);

        // Enable input
        MouseFilter = MouseFilterEnum.Stop;
        CustomMinimumSize = new Vector2(CellPixelSize, CellPixelSize);
        Size = new Vector2(CellPixelSize, CellPixelSize);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
            {
                EmitSignal(SignalName.CellClicked, Row, Col);
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        // Handle hover effect
        if (@event is InputEventMouseMotion && _currentMark == BoardState.CellValue.Empty && !_showingPremoveHighlight)
        {
            var localPos = GetLocalMousePosition();
            var rect = GetRect();
            bool isHovering = new Rect2(Vector2.Zero, rect.Size).HasPoint(localPos);
            _background.Color = isHovering ? HoverColor : EmptyColor;
        }
    }

    public void SetMark(BoardState.CellValue mark)
    {
        _currentMark = mark;

        if (mark == BoardState.CellValue.Empty)
        {
            _markDisplay.Visible = false;
            _background.Color = EmptyColor;
        }
        else
        {
            _markDisplay.Color = mark == BoardState.CellValue.X ? XColor : OColor;
            _markDisplay.Visible = true;
        }
    }

    /// <summary>
    /// Set mark using Team enum.
    /// </summary>
    public void SetMark(Team team)
    {
        var cellValue = team switch
        {
            Team.Blue => BoardState.CellValue.X,
            Team.Red => BoardState.CellValue.O,
            _ => BoardState.CellValue.Empty
        };
        SetMark(cellValue);
    }

    /// <summary>
    /// Show/hide the premove highlight on this cell.
    /// Used to indicate this is a valid premove target.
    /// </summary>
    public void ShowPremoveHighlight(bool show)
    {
        _showingPremoveHighlight = show;
        _premoveHighlight.Visible = show;

        if (show && _currentMark == BoardState.CellValue.Empty)
        {
            _background.Color = EmptyColor;
        }
    }

    /// <summary>
    /// Show a direction arrow on this cell indicating the queued premove.
    /// </summary>
    public void ShowDirectionArrow(MoveDirection direction, bool visible)
    {
        _currentDirection = direction;
        _directionArrow.Visible = visible && direction != MoveDirection.None;

        if (_directionArrow.Visible)
        {
            PositionDirectionArrow(direction);
        }
    }

    private void PositionDirectionArrow(MoveDirection direction)
    {
        float arrowSize = CellPixelSize * 0.15f;
        float margin = CellPixelSize * 0.1f;
        float center = CellPixelSize / 2f;

        _directionArrow.Size = new Vector2(arrowSize, arrowSize);

        // Position arrow on the edge of the cell pointing in the direction
        Vector2 position = direction switch
        {
            MoveDirection.Up => new Vector2(center - arrowSize / 2, margin),
            MoveDirection.Down => new Vector2(center - arrowSize / 2, CellPixelSize - margin - arrowSize),
            MoveDirection.Left => new Vector2(margin, center - arrowSize / 2),
            MoveDirection.Right => new Vector2(CellPixelSize - margin - arrowSize, center - arrowSize / 2),
            _ => new Vector2(center - arrowSize / 2, center - arrowSize / 2)
        };

        _directionArrow.Position = position;

        // Color the arrow based on the piece's team
        _directionArrow.Color = _currentMark switch
        {
            BoardState.CellValue.X => new Color(0.4f, 0.8f, 1f, 0.9f),  // Lighter blue for blue team
            BoardState.CellValue.O => new Color(1f, 0.6f, 0.6f, 0.9f),  // Lighter red for red team
            _ => new Color(1f, 1f, 1f, 0.8f)
        };
    }

    /// <summary>
    /// Set this cell as active for premove selection (pulsing border effect).
    /// </summary>
    public void SetActiveForPremove(bool active)
    {
        _isActiveForPremove = active;

        if (active)
        {
            // Draw a border effect by changing background
            _background.Color = new Color(0.3f, 0.3f, 0.2f);
        }
        else if (_currentMark == BoardState.CellValue.Empty)
        {
            _background.Color = EmptyColor;
        }
    }

    public void Highlight(bool enabled)
    {
        if (enabled && _currentMark != BoardState.CellValue.Empty)
        {
            // Make the winning cells brighter
            _background.Color = new Color(0.4f, 0.5f, 0.3f); // Greenish highlight
        }
    }

    public void Reset()
    {
        SetMark(BoardState.CellValue.Empty);
        ShowPremoveHighlight(false);
        ShowDirectionArrow(MoveDirection.None, false);
        SetActiveForPremove(false);
        _background.Color = EmptyColor;
    }

    /// <summary>
    /// Clear only the mark (piece destroyed during resolution).
    /// </summary>
    public void ClearMark()
    {
        SetMark(BoardState.CellValue.Empty);
    }

    public void UpdateSize(int newSize)
    {
        CellPixelSize = newSize;
        int markSize = (int)(CellPixelSize * 0.7f);
        int markOffset = (CellPixelSize - markSize) / 2;

        _background.Size = new Vector2(CellPixelSize, CellPixelSize);
        _premoveHighlight.Size = new Vector2(CellPixelSize, CellPixelSize);
        _markDisplay.Size = new Vector2(markSize, markSize);
        _markDisplay.Position = new Vector2(markOffset, markOffset);
        _activeBorder.Size = new Vector2(CellPixelSize, CellPixelSize);

        if (_currentDirection != MoveDirection.None)
        {
            PositionDirectionArrow(_currentDirection);
        }

        CustomMinimumSize = new Vector2(CellPixelSize, CellPixelSize);
        Size = new Vector2(CellPixelSize, CellPixelSize);
    }

    public Vector2 GetCenterPosition()
    {
        return GlobalPosition + Size / 2;
    }

    /// <summary>
    /// Check if this cell currently has a piece.
    /// </summary>
    public bool HasMark()
    {
        return _currentMark != BoardState.CellValue.Empty;
    }

    /// <summary>
    /// Get the current mark value.
    /// </summary>
    public BoardState.CellValue GetCurrentMark()
    {
        return _currentMark;
    }
}
