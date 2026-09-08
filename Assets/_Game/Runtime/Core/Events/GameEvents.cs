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

    // TODO(Fase-4): OnProgressChanged is declared here once ProgressService exists to publish it.
    // Events nobody emits yet stay out (section 6.2 of the guide).
}
