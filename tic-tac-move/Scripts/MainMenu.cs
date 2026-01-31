using Godot;

namespace TicTacMove;

/// <summary>
/// Main menu controller with Start Game button.
/// Can be extended with more options in the future.
/// </summary>
public partial class MainMenu : Control
{
    private const string GameScenePath = "res://Scenes/Game.tscn";
    private Label _gridSizeLabel;
    private int _selectedGridSize = 3;

    public override void _Ready()
    {
        GD.Print("MainMenu _Ready starting...");
        
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        
        // Create background
        var background = new ColorRect
        {
            Color = new Color(0.1f, 0.1f, 0.15f)
        };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        // Create centered container
        var centerContainer = new CenterContainer();
        centerContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(centerContainer);

        // Create vertical layout
        var vbox = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(400, 500)
        };
        vbox.AddThemeConstantOverride("separation", 20);
        centerContainer.AddChild(vbox);

        // Title
        var titleLabel = new Label
        {
            Text = "Tic Tac Move",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 64);
        vbox.AddChild(titleLabel);

        // Subtitle (for future upgrade teaser)
        var subtitleLabel = new Label
        {
            Text = "Classic Mode",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        subtitleLabel.AddThemeFontSizeOverride("font_size", 24);
        subtitleLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));
        vbox.AddChild(subtitleLabel);

        // Spacer
        vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 20) });

        // Grid Size selector
        var gridSizeContainer = new HBoxContainer();
        gridSizeContainer.AddThemeConstantOverride("separation", 10);
        
        var decreaseButton = new Button
        {
            Text = "◀",
            CustomMinimumSize = new Vector2(70, 70)
        };
        decreaseButton.AddThemeFontSizeOverride("font_size", 28);
        decreaseButton.Pressed += OnDecreaseGridSize;
        
        _gridSizeLabel = new Label
        {
            Text = "3 x 3",
            HorizontalAlignment = HorizontalAlignment.Center,
            CustomMinimumSize = new Vector2(140, 70)
        };
        _gridSizeLabel.AddThemeFontSizeOverride("font_size", 36);
        
        var increaseButton = new Button
        {
            Text = "▶",
            CustomMinimumSize = new Vector2(70, 70)
        };
        increaseButton.AddThemeFontSizeOverride("font_size", 28);
        increaseButton.Pressed += OnIncreaseGridSize;
        
        gridSizeContainer.AddChild(decreaseButton);
        gridSizeContainer.AddChild(_gridSizeLabel);
        gridSizeContainer.AddChild(increaseButton);
        
        // Center the grid size selector
        var gridSizeCenterContainer = new CenterContainer();
        gridSizeCenterContainer.AddChild(gridSizeContainer);
        vbox.AddChild(gridSizeCenterContainer);

        var gridSizeHint = new Label
        {
            Text = "Board Size",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        gridSizeHint.AddThemeFontSizeOverride("font_size", 18);
        gridSizeHint.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        vbox.AddChild(gridSizeHint);

        // Spacer
        vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 15) });

        // Start Game button
        var startButton = new Button
        {
            Text = "Start Game",
            CustomMinimumSize = new Vector2(280, 80)
        };
        startButton.AddThemeFontSizeOverride("font_size", 32);
        startButton.Pressed += OnStartGamePressed;
        vbox.AddChild(startButton);

        // Quit button
        var quitButton = new Button
        {
            Text = "Quit",
            CustomMinimumSize = new Vector2(280, 50)
        };
        quitButton.AddThemeFontSizeOverride("font_size", 20);
        quitButton.Pressed += OnQuitPressed;
        vbox.AddChild(quitButton);
        
        UpdateGridSizeLabel();
    }

    private void OnDecreaseGridSize()
    {
        if (_selectedGridSize > 3)
        {
            _selectedGridSize--;
            UpdateGridSizeLabel();
        }
    }

    private void OnIncreaseGridSize()
    {
        if (_selectedGridSize < 7)
        {
            _selectedGridSize++;
            UpdateGridSizeLabel();
        }
    }

    private void UpdateGridSizeLabel()
    {
        _gridSizeLabel.Text = $"{_selectedGridSize} x {_selectedGridSize}";
    }

    private void OnStartGamePressed()
    {
        GameSettings.GridSize = _selectedGridSize;
        GD.Print($"MainMenu: Start Game pressed with grid size {_selectedGridSize}, loading {GameScenePath}");
        GetTree().ChangeSceneToFile(GameScenePath);
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
