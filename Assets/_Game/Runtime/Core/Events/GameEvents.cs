using DecoupledTemplate.Core.State;

namespace DecoupledTemplate.Core
{
    // ────────────────────────────────
    // BOOTSTRAP EVENTS
    // ────────────────────────────────
    #region Bootstrap Events

    /// <summary>
    /// Published once, after the bootstrap sequence completes and the game scene is loaded.
    /// </summary>
    public struct OnBootstrapComplete
    {
    }

    #endregion

    // ────────────────────────────────
    // GAME STATE EVENTS
    // ────────────────────────────────
    #region Game State Events

    /// <summary>
    /// Published by GameManager after a transition that actually changed the state.
    /// </summary>
    public struct OnGameStateChanged
    {
        public GameState previousState;
        public GameState newState;
    }

    #endregion

    // ────────────────────────────────
    // PROGRESS EVENTS
    // ────────────────────────────────
    #region Progress Events

    /// <summary>
    /// Published by ProgressService after any mutation of the saved progress.
    /// </summary>
    public struct OnProgressChanged
    {
        public int currency;
        public int totalEarned;
    }

    #endregion
}
