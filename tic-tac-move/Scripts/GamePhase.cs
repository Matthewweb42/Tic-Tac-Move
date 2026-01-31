namespace TicTacMove;

/// <summary>
/// Represents the current phase of a game round.
/// Flow: Blue places+premove → Red moves resolve → Red places+premove → Blue moves resolve
/// </summary>
public enum GamePhase
{
    /// <summary>Blue player places a piece.</summary>
    BluePlacement,

    /// <summary>Blue player optionally sets premove direction.</summary>
    BluePremove,

    /// <summary>Red team's premoves from previous turn execute.</summary>
    RedResolution,

    /// <summary>Red player places a piece.</summary>
    RedPlacement,

    /// <summary>Red player optionally sets premove direction.</summary>
    RedPremove,

    /// <summary>Blue team's premoves from previous turn execute.</summary>
    BlueResolution,

    /// <summary>Game has ended (win or draw).</summary>
    GameOver
}
