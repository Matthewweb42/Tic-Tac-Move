namespace TicTacMove;

/// <summary>
/// Static class to hold game settings between scenes.
/// </summary>
public static class GameSettings
{
    public static int GridSize { get; set; } = 3;
    
    // Minimum tiles in a row needed to win (equals grid size)
    public static int WinLength => GridSize;
}
