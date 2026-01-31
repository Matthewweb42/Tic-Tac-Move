using Godot;
using TicTacMove.Models;

namespace TicTacMove;

/// <summary>
/// Manages UI elements like turn indicator, phase display, win popup, and game messages.
/// </summary>
public partial class UIManager : Control
{
    [Signal]
    public delegate void ResetRequestedEventHandler();

    private Label _turnLabel;
    private Label _phaseLabel;
    private ColorRect _winOverlay;
    private Panel _winPopup;
    private Label _winLabel;
    private Label _winSubtitle;
    private Button _resetButton;
    private Button _mainMenuButton;

    // Colors
    private static readonly Color BlueColor = new(0.3f, 0.6f, 1.0f);
    private static readonly Color RedColor = new(1.0f, 0.4f, 0.4f);
    private static readonly Color NeutralColor = new(0.7f, 0.7f, 0.7f);

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        CreateTurnLabel();
        CreatePhaseLabel();
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
        _turnLabel.Size = new Vector2(viewportSize.X, 40);
        _turnLabel.Position = new Vector2(0, viewportSize.Y * 0.02f);

        // Update phase label below turn label
        _phaseLabel.Size = new Vector2(viewportSize.X, 30);
        _phaseLabel.Position = new Vector2(0, viewportSize.Y * 0.02f + 40);

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
            Text = "Blue Team's Turn",
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(0, 20),
            Size = new Vector2(600, 40)
        };
        _turnLabel.AddThemeFontSizeOverride("font_size", 28);
        _turnLabel.AddThemeColorOverride("font_color", BlueColor);
        AddChild(_turnLabel);
    }

    private void CreatePhaseLabel()
    {
        _phaseLabel = new Label
        {
            Text = "Place your piece",
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(0, 60),
            Size = new Vector2(600, 30)
        };
        _phaseLabel.AddThemeFontSizeOverride("font_size", 18);
        _phaseLabel.AddThemeColorOverride("font_color", NeutralColor);
        AddChild(_phaseLabel);
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
            Text = "Blue Team Wins!",
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

    /// <summary>
    /// Update display based on current game phase.
    /// </summary>
    public void UpdatePhaseDisplay(GamePhase phase)
    {
        switch (phase)
        {
            case GamePhase.BluePlacement:
                _turnLabel.Text = "Blue Team's Turn";
                _turnLabel.AddThemeColorOverride("font_color", BlueColor);
                _phaseLabel.Text = "Place your piece";
                break;

            case GamePhase.BluePremove:
                _turnLabel.Text = "Blue Team's Turn";
                _turnLabel.AddThemeColorOverride("font_color", BlueColor);
                _phaseLabel.Text = "Set move direction (or stay)";
                break;

            case GamePhase.RedPlacement:
                _turnLabel.Text = "Red Team's Turn";
                _turnLabel.AddThemeColorOverride("font_color", RedColor);
                _phaseLabel.Text = "Place your piece";
                break;

            case GamePhase.RedPremove:
                _turnLabel.Text = "Red Team's Turn";
                _turnLabel.AddThemeColorOverride("font_color", RedColor);
                _phaseLabel.Text = "Set move direction (or stay)";
                break;

            case GamePhase.Resolution:
                _turnLabel.Text = "Resolution Phase";
                _turnLabel.AddThemeColorOverride("font_color", NeutralColor);
                _phaseLabel.Text = "Pieces are moving...";
                break;

            case GamePhase.GameOver:
                _phaseLabel.Text = "";
                break;
        }
    }

    /// <summary>
    /// Legacy method for backward compatibility.
    /// </summary>
    public void UpdateTurnDisplay(BoardState.CellValue currentPlayer)
    {
        if (currentPlayer == BoardState.CellValue.X)
        {
            _turnLabel.Text = "Blue Team's Turn";
            _turnLabel.AddThemeColorOverride("font_color", BlueColor);
        }
        else
        {
            _turnLabel.Text = "Red Team's Turn";
            _turnLabel.AddThemeColorOverride("font_color", RedColor);
        }
    }

    public void ShowWinPopup(BoardState.CellValue winner)
    {
        if (winner == BoardState.CellValue.X)
        {
            _winLabel.Text = "Blue Team Wins!";
            _winLabel.AddThemeColorOverride("font_color", BlueColor);
        }
        else
        {
            _winLabel.Text = "Red Team Wins!";
            _winLabel.AddThemeColorOverride("font_color", RedColor);
        }
        _winSubtitle.Text = "GAME OVER";
        _winOverlay.Visible = true;
        _winPopup.Visible = true;
        _winPopup.MouseFilter = MouseFilterEnum.Stop;
    }

    /// <summary>
    /// Show win popup using Team enum.
    /// </summary>
    public void ShowWinPopup(Team winner)
    {
        var cellValue = winner == Team.Blue ? BoardState.CellValue.X : BoardState.CellValue.O;
        ShowWinPopup(cellValue);
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
