using Godot;

namespace TicTacMove;

/// <summary>
/// Represents a single cell in the Tic Tac Toe grid.
/// Handles click input and displays X/O marks.
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
    private BoardState.CellValue _currentMark = BoardState.CellValue.Empty;

    // Colors for X and O placeholders
    private static readonly Color XColor = new Color(0.2f, 0.6f, 1.0f);   // Blue for X
    private static readonly Color OColor = new Color(1.0f, 0.4f, 0.4f);   // Red for O
    private static readonly Color EmptyColor = new Color(0.15f, 0.15f, 0.2f); // Dark background
    private static readonly Color HoverColor = new Color(0.25f, 0.25f, 0.35f); // Lighter on hover

    public override void _Ready()
    {
        GD.Print($"GridCell _Ready: Row={Row}, Col={Col}, Size={CellPixelSize}");
        
        int markSize = (int)(CellPixelSize * 0.7f);
        int markOffset = (CellPixelSize - markSize) / 2;
        
        // Create background
        _background = new ColorRect
        {
            Color = EmptyColor,
            Size = new Vector2(CellPixelSize, CellPixelSize),
            MouseFilter = MouseFilterEnum.Ignore  // Let clicks pass through to parent
        };
        AddChild(_background);

        // Create mark display (smaller rect centered in cell)
        _markDisplay = new ColorRect
        {
            Color = Colors.Transparent,
            Size = new Vector2(markSize, markSize),
            Position = new Vector2(markOffset, markOffset),
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore  // Let clicks pass through to parent
        };
        AddChild(_markDisplay);

        // Enable input
        MouseFilter = MouseFilterEnum.Stop;
        CustomMinimumSize = new Vector2(CellPixelSize, CellPixelSize);
        Size = new Vector2(CellPixelSize, CellPixelSize);
    }

    public override void _GuiInput(InputEvent @event)
    {
        GD.Print($"GridCell _GuiInput: Row={Row}, Col={Col}, Event={@event.GetType().Name}");
        
        if (@event is InputEventMouseButton mouseButton)
        {
            GD.Print($"  MouseButton: Button={mouseButton.ButtonIndex}, Pressed={mouseButton.Pressed}");
            
            if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
            {
                GD.Print($"  Left click detected! CurrentMark={_currentMark}");
                
                if (_currentMark == BoardState.CellValue.Empty)
                {
                    GD.Print($"  Emitting CellClicked signal for ({Row}, {Col})");
                    EmitSignal(SignalName.CellClicked, Row, Col);
                }
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        // Handle hover effect
        if (@event is InputEventMouseMotion && _currentMark == BoardState.CellValue.Empty)
        {
            var localPos = GetLocalMousePosition();
            var rect = GetRect();
            bool isHovering = new Rect2(Vector2.Zero, rect.Size).HasPoint(localPos);
            _background.Color = isHovering ? HoverColor : EmptyColor;
        }
    }

    public void SetMark(BoardState.CellValue mark)
    {
        GD.Print($"GridCell.SetMark: Row={Row}, Col={Col}, Mark={mark}");
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
            GD.Print($"  Mark display visible={_markDisplay.Visible}, color={_markDisplay.Color}");
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
    }

    public void UpdateSize(int newSize)
    {
        CellPixelSize = newSize;
        int markSize = (int)(CellPixelSize * 0.7f);
        int markOffset = (CellPixelSize - markSize) / 2;
        
        _background.Size = new Vector2(CellPixelSize, CellPixelSize);
        _markDisplay.Size = new Vector2(markSize, markSize);
        _markDisplay.Position = new Vector2(markOffset, markOffset);
        
        CustomMinimumSize = new Vector2(CellPixelSize, CellPixelSize);
        Size = new Vector2(CellPixelSize, CellPixelSize);
    }

    public Vector2 GetCenterPosition()
    {
        return GlobalPosition + Size / 2;
    }
}
