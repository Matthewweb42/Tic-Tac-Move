namespace TicTacMove;

/// <summary>
/// Represents the current phase of a game round.
/// </summary>
public enum GamePhase
{
    /// <summary>Blue player places a piece.</summary>
    BluePlacement,

    /// <summary>Blue player optionally sets premove direction.</summary>
    BluePremove,

    /// <summary>Red player places a piece.</summary>
    RedPlacement,

    /// <summary>Red player optionally sets premove direction.</summary>
    RedPremove,

    /// <summary>All premoves execute simultaneously.</summary>
    Resolution,

    /// <summary>Game has ended (win or draw).</summary>
    GameOver
}
