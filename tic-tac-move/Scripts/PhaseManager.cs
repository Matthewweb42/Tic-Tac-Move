using Godot;
using TicTacMove.Models;

namespace TicTacMove;

/// <summary>
/// Manages game phase transitions and enforces valid actions per phase.
/// </summary>
public partial class PhaseManager : Node
{
    [Signal]
    public delegate void PhaseChangedEventHandler(int newPhase);

    [Signal]
    public delegate void TurnNumberChangedEventHandler(int turnNumber);

    public GamePhase CurrentPhase { get; private set; } = GamePhase.BluePlacement;
    public int TurnNumber { get; private set; } = 1;

    /// <summary>The piece that was just placed (eligible for premove).</summary>
    public Piece CurrentlyPlacedPiece { get; private set; }

    public void Reset()
    {
        CurrentPhase = GamePhase.BluePlacement;
        TurnNumber = 1;
        CurrentlyPlacedPiece = null;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        EmitSignal(SignalName.TurnNumberChanged, TurnNumber);
    }

    /// <summary>
    /// Called when a piece has been placed on the board.
    /// Transitions from placement phase to premove phase.
    /// </summary>
    public void OnPiecePlaced(Piece piece)
    {
        CurrentlyPlacedPiece = piece;

        CurrentPhase = CurrentPhase switch
        {
            GamePhase.BluePlacement => GamePhase.BluePremove,
            GamePhase.RedPlacement => GamePhase.RedPremove,
            _ => CurrentPhase
        };

        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
    }

    /// <summary>
    /// Called when a premove direction has been set.
    /// </summary>
    public void OnPremoveSet()
    {
        AdvanceFromPremove();
    }

    /// <summary>
    /// Called when the player skips setting a premove (piece stays).
    /// </summary>
    public void OnPremoveSkipped()
    {
        AdvanceFromPremove();
    }

    private void AdvanceFromPremove()
    {
        CurrentlyPlacedPiece = null;

        CurrentPhase = CurrentPhase switch
        {
            GamePhase.BluePremove => GamePhase.RedPlacement,
            GamePhase.RedPremove => GamePhase.Resolution,
            _ => CurrentPhase
        };

        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
    }

    /// <summary>
    /// Called when resolution phase animations are complete.
    /// Advances to the next turn.
    /// </summary>
    public void OnResolutionComplete()
    {
        TurnNumber++;
        CurrentPhase = GamePhase.BluePlacement;

        EmitSignal(SignalName.TurnNumberChanged, TurnNumber);
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
    }

    /// <summary>
    /// Sets the game to game over state.
    /// </summary>
    public void SetGameOver()
    {
        CurrentPhase = GamePhase.GameOver;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
    }

    /// <summary>
    /// Gets the currently active team based on the phase.
    /// </summary>
    public Team GetActiveTeam()
    {
        return CurrentPhase switch
        {
            GamePhase.BluePlacement or GamePhase.BluePremove => Team.Blue,
            GamePhase.RedPlacement or GamePhase.RedPremove => Team.Red,
            _ => Team.None
        };
    }

    /// <summary>
    /// Returns true if a piece can be placed in the current phase.
    /// </summary>
    public bool CanPlacePiece() =>
        CurrentPhase == GamePhase.BluePlacement ||
        CurrentPhase == GamePhase.RedPlacement;

    /// <summary>
    /// Returns true if a premove can be set in the current phase.
    /// </summary>
    public bool CanSetPremove() =>
        CurrentPhase == GamePhase.BluePremove ||
        CurrentPhase == GamePhase.RedPremove;

    /// <summary>
    /// Returns true if currently in the resolution phase.
    /// </summary>
    public bool IsResolutionPhase() => CurrentPhase == GamePhase.Resolution;

    /// <summary>
    /// Returns true if the game is over.
    /// </summary>
    public bool IsGameOver() => CurrentPhase == GamePhase.GameOver;

    /// <summary>
    /// Returns true if it's currently blue's turn (placement or premove).
    /// </summary>
    public bool IsBlueTurn() =>
        CurrentPhase == GamePhase.BluePlacement ||
        CurrentPhase == GamePhase.BluePremove;

    /// <summary>
    /// Returns true if it's currently red's turn (placement or premove).
    /// </summary>
    public bool IsRedTurn() =>
        CurrentPhase == GamePhase.RedPlacement ||
        CurrentPhase == GamePhase.RedPremove;
}
