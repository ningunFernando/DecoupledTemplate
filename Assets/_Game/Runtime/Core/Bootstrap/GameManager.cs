using System;
using UnityEngine;
using DecoupledTemplate.Core.Pool;
using DecoupledTemplate.Core.State;

namespace DecoupledTemplate.Core
{
    /// <summary>
    /// Owns the state machine and holds the manager references the Bootstrapper injects.
    /// Survives scene loads, so anything subscribed to the bus outlives the bootstrap scene (A1).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ────────────────────────────────
        // SINGLETON
        // ────────────────────────────────
        #region Singleton

        public static GameManager Instance { get; private set; }

        /// <summary>
        /// Derived from the state machine on every read, never stored in its own field. Two
        /// fields written at different times are exactly how the enum and the machine came to
        /// report different states (C2, A2, R7).
        /// </summary>
        public GameState CurrentState => _stateMachine?.CurrentState?.Id ?? GameState.Bootstrap;

        public ObjectPoolManager PoolManager { get; private set; }

        #endregion

        private GameStateMachine _stateMachine;

        private MenuState   _menuState;
        private PlayState   _playState;
        private PausedState _pausedState;

        // ────────────────────────────────
        // LIFECYCLE
        // ────────────────────────────────
        #region Lifecycle

        private void Awake()
        {
            if (!InitializeSingleton()) return;

            InitializeStates();
        }

        private void Update()
        {
            _stateMachine?.Tick();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        // ────────────────────────────────
        // INITIALIZATION
        // ────────────────────────────────
        #region Initialization

        private bool InitializeSingleton()
        {
            if (Instance != null && Instance != this)
            {
                // Disable as well as destroy: the object stays alive until the end of the frame
                // and must not run Update in the meantime (R9).
                Log.Warn("[GameManager] Duplicate instance detected. Disabling and destroying this one.");
                enabled = false;
                Destroy(gameObject);
                return false;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Log.Trace("[GameManager] Singleton initialized.");
            return true;
        }

        private void InitializeStates()
        {
            _menuState   = new MenuState();
            _playState   = new PlayState();
            _pausedState = new PausedState();

            _stateMachine = new GameStateMachine();
            _stateMachine.Initialize(_menuState);

            Log.Trace("[GameManager] States created.");
        }

        /// <summary>
        /// Explicit injection by whoever instantiated the managers (R6). The Bootstrapper keeps
        /// what it instantiates and passes it here; nothing in this class uses Find.
        /// </summary>
        public void RegisterManagers(ObjectPoolManager poolManager)
        {
            if (poolManager == null) throw new ArgumentNullException(nameof(poolManager));

            PoolManager = poolManager;

            Log.Trace("[GameManager] Managers registered.");
        }

        #endregion

        // ────────────────────────────────
        // PUBLIC API
        // ────────────────────────────────
        #region Public API

        public void StartGame()
        {
            ChangeState(GameState.Play);

            Log.Trace("[GameManager] Game started.");
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            IGameState target = ResolveState(newState);

            if (target == null)
            {
                // Nothing was mutated before this point, so returning leaves the game in the
                // state it was already in rather than in a half-changed one (C2, R9).
                Log.Error($"[GameManager] State {newState} has no implementation.");
                return;
            }

            GameState previous = CurrentState;

            _stateMachine.TransitionTo(target);

            EventBus.Publish(new OnGameStateChanged
            {
                previousState = previous,
                newState = newState
            });
        }

        #endregion

        // ────────────────────────────────
        // PRIVATE
        // ────────────────────────────────
        #region Private

        private IGameState ResolveState(GameState state) => state switch
        {
            GameState.Menu   => _menuState,
            GameState.Play   => _playState,
            GameState.Paused => _pausedState,
            _ => null
        };

        #endregion
    }
}
