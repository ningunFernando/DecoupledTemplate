using UnityEngine;
using UnityEngine.InputSystem;
using DecoupledTemplate.Core;
using DecoupledTemplate.Core.State;

namespace DecoupledTemplate.Player
{
    /// <summary>
    /// The one owner of player input (R11). Reads a serialized InputActionReference instead of
    /// PlayerInput in Send Messages mode, where renaming a handler breaks input with no compile
    /// error and no warning (M9). Input only counts while the game is in Play, and the reader
    /// learns the state from the bus instead of asking GameManager (R4).
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        // ────────────────────────────────
        // INSPECTOR
        // ────────────────────────────────
        #region Inspector

        [Header("Input")]
        [Tooltip("Vector2 action that moves the player, e.g. Player/Move in InputSystem_Actions.")]
        [SerializeField] private InputActionReference _moveAction;

        #endregion

        private Vector2 _rawMove;
        private bool    _isPlaying;
        private bool    _isSubscribed;

        // ────────────────────────────────
        // PUBLIC API
        // ────────────────────────────────
        #region Public API

        /// <summary>Move input with x to the right and y forward. Zero while the game is not in Play.</summary>
        public Vector2 Move => _isPlaying ? _rawMove : Vector2.zero;

        #endregion

        // ────────────────────────────────
        // LIFECYCLE
        // ────────────────────────────────
        #region Lifecycle

        private void Awake()
        {
            if (_moveAction == null || _moveAction.action == null)
            {
                // Disabled rather than left running without input (R9). Setting enabled here keeps
                // OnEnable from running, but Unity calls OnDisable on the spot, which is why OnDisable
                // only undoes what OnEnable actually did.
                Log.Error("[PlayerInputReader] Move action not assigned. Input is disabled.");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _moveAction.action.performed += OnMove;
            // Without canceled the last value sticks after the key is released.
            _moveAction.action.canceled  += OnMove;
            _moveAction.action.Enable();

            EventBus.Subscribe<OnGameStateChanged>(HandleGameStateChanged);

            _isSubscribed = true;
        }

        private void OnDisable()
        {
            if (!_isSubscribed) return;

            EventBus.Unsubscribe<OnGameStateChanged>(HandleGameStateChanged);

            _moveAction.action.performed -= OnMove;
            _moveAction.action.canceled  -= OnMove;
            _moveAction.action.Disable();

            _rawMove      = Vector2.zero;
            _isSubscribed = false;
        }

        #endregion

        // ────────────────────────────────
        // EVENT HANDLERS
        // ────────────────────────────────
        #region Event Handlers

        private void OnMove(InputAction.CallbackContext context)
        {
            _rawMove = context.ReadValue<Vector2>();
        }

        private void HandleGameStateChanged(OnGameStateChanged e)
        {
            _isPlaying = e.newState == GameState.Play;
        }

        #endregion
    }
}
