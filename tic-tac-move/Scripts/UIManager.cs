using Godot;

namespace TicTacMove;

/// <summary>
/// Manages UI elements like turn indicator, win popup, and game messages.
/// </summary>
public partial class UIManager : Control
{
    [Signal]
    public delegate void ResetRequestedEventHandler();

    private Label _turnLabel;
    private ColorRect _winOverlay;
    private Panel _winPopup;
    private Label _winLabel;
    private Label _winSubtitle;
    private Button _resetButton;
    private Button _mainMenuButton;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        CreateTurnLabel();
        CreateWinScreen();
        
        // Connect to viewport size changes
        GetTree().Root.SizeChanged += OnViewportSizeChanged;
        UpdateLayout();
    }

    public override void _ExitTree()
    {
        GetTree().Root.SizeChanged -= OnViewportSizeChanged;
    }

    private void OnViewportSizeChanged()
    {
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        var viewportSize = GetViewportRect().Size;
        
        // Update turn label to span full width
        _turnLabel.Size = new Vector2(viewportSize.X, 50);
        _turnLabel.Position = new Vector2(0, viewportSize.Y * 0.03f);
        
        // Update win popup position to center
        float popupWidth = Mathf.Min(400, viewportSize.X * 0.8f);
        float popupHeight = 280;
        _winPopup.Size = new Vector2(popupWidth, popupHeight);
        _winPopup.Position = new Vector2(
            (viewportSize.X - popupWidth) / 2,
            (viewportSize.Y - popupHeight) / 2
        );
    }

    private void CreateTurnLabel()
    {
        _turnLabel = new Label
        {
            Text = "🔵 Blue Team's Turn",
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(0, 30),
            Size = new Vector2(600, 50)
        };
        _turnLabel.AddThemeFontSizeOverride("font_size", 32);
        AddChild(_turnLabel);
    }

    private void CreateWinScreen()
    {
        // Full screen semi-transparent overlay
        _winOverlay = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.7f),
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop
        };
        _winOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_winOverlay);

        // Centered popup panel
        _winPopup = new Panel
        {
            Visible = false,
            Size = new Vector2(400, 280),
            Position = new Vector2(100, 200)
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.12f, 0.18f, 1f),
            CornerRadiusTopLeft = 16,
            CornerRadiusTopRight = 16,
            CornerRadiusBottomLeft = 16,
            CornerRadiusBottomRight = 16,
            BorderWidthTop = 3,
            BorderWidthBottom = 3,
            BorderWidthLeft = 3,
            BorderWidthRight = 3,
            BorderColor = new Color(0.4f, 0.4f, 0.5f)
        };
        _winPopup.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer
        {
            Position = new Vector2(30, 25),
            Size = new Vector2(340, 230)
        };
        vbox.AddThemeConstantOverride("separation", 15);

        // "GAME OVER" subtitle
        _winSubtitle = new Label
        {
            Text = "GAME OVER",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _winSubtitle.AddThemeFontSizeOverride("font_size", 20);
        _winSubtitle.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));

        // Winner announcement
        _winLabel = new Label
        {
            Text = "🔵 Blue Team Wins!",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _winLabel.AddThemeFontSizeOverride("font_size", 36);

        // Play Again button
        _resetButton = new Button
        {
            Text = "Play Again",
            CustomMinimumSize = new Vector2(250, 60)
        };
        _resetButton.AddThemeFontSizeOverride("font_size", 24);
        _resetButton.Pressed += OnResetPressed;

        // Main Menu button
        _mainMenuButton = new Button
        {
            Text = "Main Menu",
            CustomMinimumSize = new Vector2(250, 50)
        };
        _mainMenuButton.AddThemeFontSizeOverride("font_size", 18);
        _mainMenuButton.Pressed += OnMainMenuPressed;

        vbox.AddChild(_winSubtitle);
        vbox.AddChild(_winLabel);
        vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 5) });
        vbox.AddChild(_resetButton);
        vbox.AddChild(_mainMenuButton);

        _winPopup.AddChild(vbox);
        AddChild(_winPopup);
    }

    public void UpdateTurnDisplay(BoardState.CellValue currentPlayer)
    {
        if (currentPlayer == BoardState.CellValue.X)
        {
            _turnLabel.Text = "🔵 Blue Team's Turn";
            _turnLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.6f, 1.0f));
        }
        else
        {
            _turnLabel.Text = "🔴 Red Team's Turn";
            _turnLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.4f, 0.4f));
        }
    }

    public void ShowWinPopup(BoardState.CellValue winner)
    {
        if (winner == BoardState.CellValue.X)
        {
            _winLabel.Text = "🔵 Blue Team Wins!";
            _winLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.6f, 1.0f));
        }
        else
        {
            _winLabel.Text = "🔴 Red Team Wins!";
            _winLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.4f, 0.4f));
        }
        _winSubtitle.Text = "GAME OVER";
        _winOverlay.Visible = true;
        _winPopup.Visible = true;
        _winPopup.MouseFilter = MouseFilterEnum.Stop;
    }

    public void ShowDrawPopup()
    {
        _winLabel.Text = "It's a Draw!";
        _winLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
        _winSubtitle.Text = "GAME OVER";
        _winOverlay.Visible = true;
        _winPopup.Visible = true;
        _winPopup.MouseFilter = MouseFilterEnum.Stop;
    }

    public void HideWinUI()
    {
        _winOverlay.Visible = false;
        _winPopup.Visible = false;
    }

    private void OnResetPressed()
    {
        EmitSignal(SignalName.ResetRequested);
    }

    private void OnMainMenuPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }
}
