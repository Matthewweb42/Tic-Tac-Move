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

    private BoardState.CellValue _currentMark = BoardState.CellValue.Empty;
    private MoveDirection _currentDirection = MoveDirection.None;
    private bool _isActiveForPremove;
    private bool _showingPremoveHighlight;
    private Team _premoveHighlightTeam = Team.Blue;
    private bool _isHighlighted;
    private bool _isHovering;
    private float _pulseTime;
    private float _placementAnimProgress = 1f;

    // Colors
    private static readonly Color XColor = new(0.3f, 0.7f, 1.0f);           // Bright blue for X
    private static readonly Color XColorGlow = new(0.4f, 0.8f, 1.0f, 0.3f); // Blue glow
    private static readonly Color OColor = new(1.0f, 0.4f, 0.4f);           // Red for O
    private static readonly Color OColorGlow = new(1.0f, 0.5f, 0.5f, 0.3f); // Red glow
    private static readonly Color EmptyColor = new(0.12f, 0.12f, 0.16f);    // Dark background
    private static readonly Color HoverColor = new(0.20f, 0.20f, 0.26f);    // Lighter on hover
    private static readonly Color BluePremoveHighlight = new(0.2f, 0.4f, 0.6f, 0.3f);  // Blue highlight
    private static readonly Color RedPremoveHighlight = new(0.6f, 0.2f, 0.2f, 0.3f);   // Red highlight
    private static readonly Color ActiveColor = new(0.22f, 0.22f, 0.18f);
    private static readonly Color WinHighlightColor = new(0.2f, 0.35f, 0.15f);

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        CustomMinimumSize = new Vector2(CellPixelSize, CellPixelSize);
        Size = new Vector2(CellPixelSize, CellPixelSize);
    }

    public override void _Process(double delta)
    {
        // Update pulse animation for active pieces
        if (_currentMark != BoardState.CellValue.Empty)
        {
            _pulseTime += (float)delta * 2f;
            QueueRedraw();
        }

        // Placement animation
        if (_placementAnimProgress < 1f)
        {
            _placementAnimProgress += (float)delta * 4f;
            if (_placementAnimProgress > 1f) _placementAnimProgress = 1f;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        // Draw background
        Color bgColor = GetBackgroundColor();
        DrawRect(new Rect2(0, 0, CellPixelSize, CellPixelSize), bgColor);

        // Draw premove highlight overlay with team-specific color
        if (_showingPremoveHighlight)
        {
            Color highlightColor = _premoveHighlightTeam == Team.Blue ? BluePremoveHighlight : RedPremoveHighlight;
            DrawRect(new Rect2(0, 0, CellPixelSize, CellPixelSize), highlightColor);
        }

        // Draw X or O
        if (_currentMark != BoardState.CellValue.Empty)
        {
            float padding = CellPixelSize * 0.2f;
            float size = CellPixelSize - padding * 2;
            Vector2 center = new(CellPixelSize / 2f, CellPixelSize / 2f);

            // Apply placement animation (scale up from center)
            float scale = EaseOutBack(_placementAnimProgress);
            float animSize = size * scale;

            // Pulse effect
            float pulse = 1f + Mathf.Sin(_pulseTime) * 0.03f;

            if (_currentMark == BoardState.CellValue.X)
            {
                DrawX(center, animSize * pulse);
            }
            else
            {
                DrawO(center, animSize * pulse / 2f);
            }
        }

        // Draw direction arrow if set
        if (_currentDirection != MoveDirection.None)
        {
            DrawDirectionArrow(_currentDirection);
        }
    }

    private Color GetBackgroundColor()
    {
        if (_isHighlighted)
            return WinHighlightColor;
        if (_isActiveForPremove)
            return ActiveColor;
        if (_isHovering && _currentMark == BoardState.CellValue.Empty)
            return HoverColor;
        return EmptyColor;
    }

    private void DrawX(Vector2 center, float size)
    {
        float halfSize = size / 2f;
        float lineWidth = CellPixelSize * 0.12f;

        // Glow effect (draw larger, semi-transparent lines behind)
        float glowWidth = lineWidth * 2.5f;
        DrawLine(
            new Vector2(center.X - halfSize, center.Y - halfSize),
            new Vector2(center.X + halfSize, center.Y + halfSize),
            XColorGlow, glowWidth, true
        );
        DrawLine(
            new Vector2(center.X + halfSize, center.Y - halfSize),
            new Vector2(center.X - halfSize, center.Y + halfSize),
            XColorGlow, glowWidth, true
        );

        // Main X lines
        DrawLine(
            new Vector2(center.X - halfSize, center.Y - halfSize),
            new Vector2(center.X + halfSize, center.Y + halfSize),
            XColor, lineWidth, true
        );
        DrawLine(
            new Vector2(center.X + halfSize, center.Y - halfSize),
            new Vector2(center.X - halfSize, center.Y + halfSize),
            XColor, lineWidth, true
        );
    }

    private void DrawO(Vector2 center, float radius)
    {
        float lineWidth = CellPixelSize * 0.1f;

        // Glow effect
        DrawArc(center, radius, 0, Mathf.Tau, 64, OColorGlow, lineWidth * 2.5f, true);

        // Main O circle
        DrawArc(center, radius, 0, Mathf.Tau, 64, OColor, lineWidth, true);
    }

    private void DrawDirectionArrow(MoveDirection direction)
    {
        float arrowSize = CellPixelSize * 0.18f;
        float margin = CellPixelSize * 0.06f;
        Vector2 center = new(CellPixelSize / 2f, CellPixelSize / 2f);

        Vector2 arrowCenter = direction switch
        {
            MoveDirection.Up => new Vector2(center.X, margin + arrowSize / 2),
            MoveDirection.Down => new Vector2(center.X, CellPixelSize - margin - arrowSize / 2),
            MoveDirection.Left => new Vector2(margin + arrowSize / 2, center.Y),
            MoveDirection.Right => new Vector2(CellPixelSize - margin - arrowSize / 2, center.Y),
            _ => center
        };

        // Draw arrow triangle
        Vector2[] points = direction switch
        {
            MoveDirection.Up =>
            [
                arrowCenter + new Vector2(0, -arrowSize / 2),
                arrowCenter + new Vector2(-arrowSize / 2, arrowSize / 2),
                arrowCenter + new Vector2(arrowSize / 2, arrowSize / 2)
            ],
            MoveDirection.Down =>
            [
                arrowCenter + new Vector2(0, arrowSize / 2),
                arrowCenter + new Vector2(-arrowSize / 2, -arrowSize / 2),
                arrowCenter + new Vector2(arrowSize / 2, -arrowSize / 2)
            ],
            MoveDirection.Left =>
            [
                arrowCenter + new Vector2(-arrowSize / 2, 0),
                arrowCenter + new Vector2(arrowSize / 2, -arrowSize / 2),
                arrowCenter + new Vector2(arrowSize / 2, arrowSize / 2)
            ],
            MoveDirection.Right =>
            [
                arrowCenter + new Vector2(arrowSize / 2, 0),
                arrowCenter + new Vector2(-arrowSize / 2, -arrowSize / 2),
                arrowCenter + new Vector2(-arrowSize / 2, arrowSize / 2)
            ],
            _ => [arrowCenter, arrowCenter, arrowCenter]
        };

        // Color based on team
        Color arrowCol = _currentMark == BoardState.CellValue.X
            ? new Color(0.5f, 0.9f, 1f, 0.95f)
            : new Color(1f, 0.7f, 0.7f, 0.95f);

        DrawPolygon(points, [arrowCol]);
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
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
        if (@event is InputEventMouseMotion)
        {
            var localPos = GetLocalMousePosition();
            bool wasHovering = _isHovering;
            _isHovering = new Rect2(Vector2.Zero, Size).HasPoint(localPos);

            if (wasHovering != _isHovering)
            {
                QueueRedraw();
            }
        }
    }

    public void SetMark(BoardState.CellValue mark)
    {
        bool isNewMark = _currentMark == BoardState.CellValue.Empty && mark != BoardState.CellValue.Empty;
        _currentMark = mark;

        if (mark == BoardState.CellValue.Empty)
        {
            _placementAnimProgress = 1f;
        }
        else if (isNewMark)
        {
            _placementAnimProgress = 0f;
        }

        QueueRedraw();
    }

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

    public void ClearMark()
    {
        SetMark(BoardState.CellValue.Empty);
    }

    public void ShowPremoveHighlight(bool show, Team team = Team.Blue)
    {
        _showingPremoveHighlight = show;
        _premoveHighlightTeam = team;
        QueueRedraw();
    }

    public void ShowDirectionArrow(MoveDirection direction, bool visible)
    {
        _currentDirection = visible ? direction : MoveDirection.None;
        QueueRedraw();
    }

    public void SetActiveForPremove(bool active)
    {
        _isActiveForPremove = active;
        QueueRedraw();
    }

    public void Highlight(bool enabled)
    {
        _isHighlighted = enabled;
        QueueRedraw();
    }

    public void Reset()
    {
        _currentMark = BoardState.CellValue.Empty;
        _currentDirection = MoveDirection.None;
        _isActiveForPremove = false;
        _showingPremoveHighlight = false;
        _isHighlighted = false;
        _pulseTime = 0f;
        _placementAnimProgress = 1f;
        QueueRedraw();
    }

    public void UpdateSize(int newSize)
    {
        CellPixelSize = newSize;
        CustomMinimumSize = new Vector2(CellPixelSize, CellPixelSize);
        Size = new Vector2(CellPixelSize, CellPixelSize);
        QueueRedraw();
    }

    public Vector2 GetCenterPosition()
    {
        return GlobalPosition + Size / 2;
    }

    public bool HasMark()
    {
        return _currentMark != BoardState.CellValue.Empty;
    }

    public BoardState.CellValue GetCurrentMark()
    {
        return _currentMark;
    }
}
