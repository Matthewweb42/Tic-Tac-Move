using Godot;

namespace TicTacMove;

/// <summary>
/// Main game controller that orchestrates the Tic Tac Toe game.
/// Manages game flow, connects UI with game state.
/// </summary>
public partial class GameManager : Node2D
{
    private BoardState _boardState;
    private WinChecker _winChecker;
    private GridCell[,] _gridCells;
    private UIManager _uiManager;
    private GridContainer _gridContainer;
    private bool _gameOver;
    private int _gridSize;
    private int _cellSize;
    private CanvasLayer _gridLayer;
    private ColorRect _gridBackground;

    private const int GridSpacing = 6;
    private const float BoardScreenRatio = 0.75f; // Board takes 75% of smaller dimension
    private const float TopMarginRatio = 0.15f; // Top margin for turn label

    public override void _Ready()
    {
        GD.Print("GameManager _Ready starting...");
        
        _gridSize = GameSettings.GridSize;
        
        _boardState = new BoardState(_gridSize);
        _winChecker = new WinChecker();
        _gridCells = new GridCell[_gridSize, _gridSize];
        _gameOver = false;

        CreateUI();
        CreateGrid();
        _boardState.Reset();
        
        // Connect to viewport size changed signal
        GetTree().Root.SizeChanged += OnViewportSizeChanged;
        
        GD.Print($"GameManager _Ready complete! Grid: {_gridSize}x{_gridSize}");
    }

    public override void _ExitTree()
    {
        GetTree().Root.SizeChanged -= OnViewportSizeChanged;
    }

    private void OnViewportSizeChanged()
    {
        UpdateGridLayout();
    }

    private void UpdateGridLayout()
    {
        var viewportSize = GetViewportRect().Size;
        
        // Calculate board size based on viewport
        float maxBoardSize = Mathf.Min(viewportSize.X, viewportSize.Y - (viewportSize.Y * TopMarginRatio)) * BoardScreenRatio;
        _cellSize = (int)((maxBoardSize - (GridSpacing * (_gridSize + 1))) / _gridSize);
        
        int totalGridSize = _cellSize * _gridSize + GridSpacing * (_gridSize + 1);
        
        // Center horizontally, position below turn label
        float gridX = (viewportSize.X - totalGridSize) / 2 + GridSpacing;
        float gridY = viewportSize.Y * TopMarginRatio + 20;
        var gridOffset = new Vector2(gridX, gridY);
        
        // Update background
        if (_gridBackground != null)
        {
            _gridBackground.Position = gridOffset - new Vector2(GridSpacing, GridSpacing);
            _gridBackground.Size = new Vector2(totalGridSize, totalGridSize);
        }
        
        // Update grid container
        if (_gridContainer != null)
        {
            _gridContainer.Position = gridOffset;
        }
        
        // Update cell sizes
        for (int row = 0; row < _gridSize; row++)
        {
            for (int col = 0; col < _gridSize; col++)
            {
                if (_gridCells[row, col] != null)
                {
                    _gridCells[row, col].UpdateSize(_cellSize);
                }
            }
        }
    }

    private void CreateUI()
    {
        // Create a CanvasLayer for UI (layer 10 to be above grid)
        var uiLayer = new CanvasLayer();
        uiLayer.Layer = 10;
        AddChild(uiLayer);

        _uiManager = new UIManager();
        _uiManager.ResetRequested += OnResetRequested;
        uiLayer.AddChild(_uiManager);
    }

    private void CreateGrid()
    {
        var viewportSize = GetViewportRect().Size;
        
        // Calculate initial sizes
        float maxBoardSize = Mathf.Min(viewportSize.X, viewportSize.Y - (viewportSize.Y * TopMarginRatio)) * BoardScreenRatio;
        _cellSize = (int)((maxBoardSize - (GridSpacing * (_gridSize + 1))) / _gridSize);
        
        int totalGridSize = _cellSize * _gridSize + GridSpacing * (_gridSize + 1);
        
        float gridX = (viewportSize.X - totalGridSize) / 2 + GridSpacing;
        float gridY = viewportSize.Y * TopMarginRatio + 20;
        var gridOffset = new Vector2(gridX, gridY);

        // Create a CanvasLayer for the grid (layer 1, below UI)
        _gridLayer = new CanvasLayer();
        _gridLayer.Layer = 1;
        AddChild(_gridLayer);

        // Create background panel for grid
        _gridBackground = new ColorRect
        {
            Position = gridOffset - new Vector2(GridSpacing, GridSpacing),
            Size = new Vector2(totalGridSize, totalGridSize),
            Color = new Color(0.3f, 0.3f, 0.35f)
        };
        _gridLayer.AddChild(_gridBackground);

        // Create grid container
        _gridContainer = new GridContainer
        {
            Columns = _gridSize,
            Position = gridOffset
        };
        _gridContainer.AddThemeConstantOverride("h_separation", GridSpacing);
        _gridContainer.AddThemeConstantOverride("v_separation", GridSpacing);

        // Create cells
        for (int row = 0; row < _gridSize; row++)
        {
            for (int col = 0; col < _gridSize; col++)
            {
                var cell = new GridCell
                {
                    Row = row,
                    Col = col,
                    CellPixelSize = _cellSize
                };
                cell.CellClicked += OnCellClicked;
                _gridCells[row, col] = cell;
                _gridContainer.AddChild(cell);
            }
        }

        _gridLayer.AddChild(_gridContainer);
    }

    private void OnCellClicked(int row, int col)
    {
        GD.Print($"GameManager.OnCellClicked: row={row}, col={col}");
        
        if (_gameOver)
        {
            GD.Print("  Game is over, ignoring click");
            return;
        }

        if (!_boardState.IsCellEmpty(row, col))
        {
            GD.Print("  Cell is not empty, ignoring click");
            return;
        }

        // Capture current player before placing
        var currentPlayer = _boardState.CurrentPlayer;
        GD.Print($"  Current player: {currentPlayer}");

        // Place the mark
        _boardState.PlaceMark(row, col);
        GD.Print($"  Calling SetMark on cell [{row},{col}]");
        _gridCells[row, col].SetMark(currentPlayer);

        // Check for win
        var winResult = _winChecker.CheckForWin(_boardState);
        if (winResult.IsWin)
        {
            HandleWin(winResult);
            return;
        }

        // Check for draw
        if (_boardState.IsBoardFull())
        {
            HandleDraw();
            return;
        }

        // Switch turns
        _boardState.SwitchPlayer();
        _uiManager.UpdateTurnDisplay(_boardState.CurrentPlayer);
    }

    private void HandleWin(WinChecker.WinResult winResult)
    {
        _gameOver = true;

        // Highlight winning cells
        foreach (var cell in winResult.WinningCells)
        {
            _gridCells[cell.X, cell.Y].Highlight(true);
        }

        // Show win popup
        _uiManager.ShowWinPopup(winResult.Winner);
    }

    private void HandleDraw()
    {
        _gameOver = true;
        _uiManager.ShowDrawPopup();
    }

    private void OnResetRequested()
    {
        ResetGame();
    }

    private void ResetGame()
    {
        _gameOver = false;
        _boardState.Reset();

        // Reset all cells
        for (int row = 0; row < _gridSize; row++)
        {
            for (int col = 0; col < _gridSize; col++)
            {
                _gridCells[row, col].Reset();
            }
        }

        _uiManager.HideWinUI();
        _uiManager.UpdateTurnDisplay(_boardState.CurrentPlayer);
    }
}
