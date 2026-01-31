using Godot;

namespace TicTacMove;

/// <summary>
/// Main game controller that orchestrates the Tic Tac Toe game with movement.
/// Manages game flow, phases, and connects UI with game state.
/// </summary>
public partial class GameManager : Node2D
{
    private BoardState _boardState;
    private WinChecker _winChecker;
    private PhaseManager _phaseManager;
    private MovementResolver _movementResolver;
    private ResolutionAnimator _resolutionAnimator;
    private PremoveInputHandler _premoveInputHandler;

    private GridCell[,] _gridCells;
    private UIManager _uiManager;
    private GridContainer _gridContainer;
    private int _gridSize;
    private int _cellSize;
    private CanvasLayer _gridLayer;
    private CanvasLayer _uiLayer;
    private ColorRect _gridBackground;

    private const int GridSpacing = 6;
    private const float BoardScreenRatio = 0.70f; // Slightly smaller to make room for buttons
    private const float TopMarginRatio = 0.15f;

    public override void _Ready()
    {
        _gridSize = GameSettings.GridSize;

        // Initialize core systems
        _boardState = new BoardState(_gridSize);
        _winChecker = new WinChecker();
        _phaseManager = new PhaseManager();
        _movementResolver = new MovementResolver(_boardState);
        _resolutionAnimator = new ResolutionAnimator();
        _premoveInputHandler = new PremoveInputHandler();

        _gridCells = new GridCell[_gridSize, _gridSize];

        // Add PhaseManager and ResolutionAnimator as children (they're Nodes)
        AddChild(_phaseManager);
        AddChild(_resolutionAnimator);
        AddChild(_premoveInputHandler);

        CreateUI();
        CreateGrid();

        // Initialize handlers after grid is created
        _resolutionAnimator.Initialize(_gridCells, _boardState);
        _premoveInputHandler.Initialize(_boardState, _gridCells, _uiManager);

        // Connect signals
        _phaseManager.PhaseChanged += OnPhaseChanged;
        _premoveInputHandler.PremoveConfirmed += OnPremoveConfirmed;
        _resolutionAnimator.AnimationComplete += OnResolutionAnimationComplete;

        // Initialize game state
        _boardState.Reset();
        _phaseManager.Reset();

        // Connect to viewport size changed signal
        GetTree().Root.SizeChanged += OnViewportSizeChanged;

        // Initial layout update
        CallDeferred(nameof(UpdateAllLayouts));
    }

    public override void _ExitTree()
    {
        GetTree().Root.SizeChanged -= OnViewportSizeChanged;
    }

    private void OnViewportSizeChanged()
    {
        UpdateAllLayouts();
    }

    private void UpdateAllLayouts()
    {
        UpdateGridLayout();
        _premoveInputHandler.UpdateButtonPositions(GetViewportRect().Size);
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
        float gridY = viewportSize.Y * TopMarginRatio + 40; // Extra space for phase label
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
        _uiLayer = new CanvasLayer();
        _uiLayer.Layer = 10;
        AddChild(_uiLayer);

        _uiManager = new UIManager();
        _uiManager.ResetRequested += OnResetRequested;
        _uiLayer.AddChild(_uiManager);
    }

    private void CreateGrid()
    {
        var viewportSize = GetViewportRect().Size;

        // Calculate initial sizes
        float maxBoardSize = Mathf.Min(viewportSize.X, viewportSize.Y - (viewportSize.Y * TopMarginRatio)) * BoardScreenRatio;
        _cellSize = (int)((maxBoardSize - (GridSpacing * (_gridSize + 1))) / _gridSize);

        int totalGridSize = _cellSize * _gridSize + GridSpacing * (_gridSize + 1);

        float gridX = (viewportSize.X - totalGridSize) / 2 + GridSpacing;
        float gridY = viewportSize.Y * TopMarginRatio + 40;
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
        // Handle clicks based on current phase
        if (_phaseManager.IsGameOver())
        {
            return;
        }

        if (_phaseManager.CanPlacePiece())
        {
            HandlePlacement(row, col);
        }
        else if (_phaseManager.CanSetPremove())
        {
            // Let the premove handler deal with it
            _premoveInputHandler.HandleCellClick(row, col);
        }
    }

    private void HandlePlacement(int row, int col)
    {
        if (!_boardState.IsEmpty(row, col))
        {
            return;
        }

        // Determine team based on current phase
        var team = _phaseManager.GetActiveTeam();

        // Place the piece
        var piece = _boardState.PlacePiece(row, col, _phaseManager.TurnNumber, team);
        if (piece == null) return;

        // Update visual
        _gridCells[row, col].SetMark(piece.Team);

        // Notify phase manager and start premove input
        _phaseManager.OnPiecePlaced(piece);
        _premoveInputHandler.BeginPremoveInput(piece);
    }

    private void OnPhaseChanged(int newPhaseInt)
    {
        var newPhase = (GamePhase)newPhaseInt;

        // Update UI
        _uiManager.UpdatePhaseDisplay(newPhase);

        // Handle resolution phase
        if (newPhase == GamePhase.Resolution)
        {
            StartResolution();
        }
    }

    private void OnPremoveConfirmed()
    {
        // End premove input
        _premoveInputHandler.EndPremoveInput();

        // Advance the phase
        _phaseManager.OnPremoveSet();
    }

    private async void StartResolution()
    {
        // Resolve all movements
        var result = _movementResolver.Resolve();

        // Animate the resolution
        await _resolutionAnimator.AnimateResolution(result);
    }

    private void OnResolutionAnimationComplete()
    {
        // Refresh grid display to match board state
        _resolutionAnimator.RefreshGridDisplay();

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

        // Clear just-placed flags and continue to next turn
        _boardState.ClearJustPlacedFlags();
        _phaseManager.OnResolutionComplete();
    }

    private void HandleWin(WinChecker.WinResult winResult)
    {
        _phaseManager.SetGameOver();

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
        _phaseManager.SetGameOver();
        _uiManager.ShowDrawPopup();
    }

    private void OnResetRequested()
    {
        ResetGame();
    }

    private void ResetGame()
    {
        // Reset all systems
        _boardState.Reset();
        _phaseManager.Reset();
        _premoveInputHandler.Reset();

        // Reset all cells
        for (int row = 0; row < _gridSize; row++)
        {
            for (int col = 0; col < _gridSize; col++)
            {
                _gridCells[row, col].Reset();
            }
        }

        _uiManager.HideWinUI();
        _uiManager.UpdatePhaseDisplay(GamePhase.BluePlacement);
    }
}
