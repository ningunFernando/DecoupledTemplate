using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DecoupledTemplate.Core.Pool;
using DecoupledTemplate.Data;

namespace DecoupledTemplate.Core
{
    /// <summary>
    /// Entry point of the game, alone in Scene_Bootstrap. Instantiates the managers, keeps the
    /// references and injects them, and only then loads the game scene (R6, A3).
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        // ────────────────────────────────
        // INSPECTOR
        // ────────────────────────────────
        #region Inspector

        [Header("Manager Prefabs")]
        [Tooltip("GameManager prefab. Required: the sequence throws if it is not assigned.")]
        [SerializeField] private GameManager       _gameManagerPrefab;
        [Tooltip("ObjectPoolManager prefab. Required: the sequence throws if it is not assigned.")]
        [SerializeField] private ObjectPoolManager _poolManagerPrefab;

        [Header("Configuration")]
        [Tooltip("Global game configuration. Required: the sequence throws if it is not assigned.")]
        [SerializeField] private GameConfigSO      _gameConfig;

        #endregion

        private GameManager       _gameManager;
        private ObjectPoolManager _poolManager;

        // ────────────────────────────────
        // LIFECYCLE
        // ────────────────────────────────
        #region Lifecycle

        private void Awake()
        {
            StartCoroutine(InitializeSequence());
        }

        private void OnDestroy()
        {
            // sceneLoaded is static: without this the handler would outlive the bootstrap scene.
            SceneManager.sceneLoaded -= OnGameSceneLoaded;
        }

        #endregion

        // ────────────────────────────────
        // INITIALIZATION
        // ────────────────────────────────
        #region Initialization

        /// <summary>
        /// Ordered startup. Each step is its own method, and the frame yielded in between lets
        /// Awake and Start of the freshly instantiated managers settle first.
        /// </summary>
        private IEnumerator InitializeSequence()
        {
            ValidateConfiguration();

            Log.Trace("[Bootstrapper] ----- SEQUENCE STARTED -----");

            InstantiateManagers();
            yield return null;
            Log.Trace("[Bootstrapper] Step 1 complete: Managers");

            // TODO(Fase-4): the save step belongs here, between Managers and Object Pools.
            // Core cannot reference Save (R3), so how SaveSystem reaches this sequence is an
            // open design decision, not a forgotten line.

            InitializeObjectPools();
            yield return null;
            Log.Trace("[Bootstrapper] Step 2 complete: Object Pools");

            Log.Trace("[Bootstrapper] ----- SEQUENCE COMPLETED -----");

            LoadGameScene();
        }

        #endregion

        // ────────────────────────────────
        // INITIALIZATION STEPS
        // ────────────────────────────────
        #region Initialization Steps

        /// <summary>
        /// Checked before anything runs. An empty scene name would otherwise only surface at the
        /// very end of the sequence, as a confusing LoadScene failure and with every manager
        /// already alive (R8, R9).
        /// </summary>
        private void ValidateConfiguration()
        {
            if (_gameConfig == null)
            {
                throw new InvalidOperationException("[Bootstrapper] Game config not assigned.");
            }

            if (string.IsNullOrWhiteSpace(_gameConfig.GameSceneName))
            {
                throw new InvalidOperationException("[Bootstrapper] GameConfigSO.gameSceneName is empty.");
            }
        }

        private void InstantiateManagers()
        {
            // Correct here and only here: this runs before anything is instantiated, so no
            // DontDestroyOnLoad subscriber can be left deaf by a later scene reload (A1).
            EventBus.ClearAllSubscriptions();

            _gameManager = InstantiateRequired(_gameManagerPrefab, "GameManager");
            _poolManager = InstantiateRequired(_poolManagerPrefab, "ObjectPoolManager");
        }

        /// <summary>
        /// Throwing on purpose: an incomplete bootstrap has to fail loudly and early. Degrading
        /// to a game that starts without its managers produces a NullReferenceException halfway
        /// through the coroutine and a black screen with no usable diagnostic (A3).
        /// </summary>
        private T InstantiateRequired<T>(T prefab, string label) where T : Component
        {
            if (prefab == null)
            {
                throw new InvalidOperationException($"[Bootstrapper] {label} prefab not assigned.");
            }

            return Instantiate(prefab);
        }

        private void InitializeObjectPools()
        {
            _gameManager.RegisterManagers(_poolManager);
            _poolManager.InitializePools();
        }

        private void LoadGameScene()
        {
            SceneManager.sceneLoaded += OnGameSceneLoaded;
            SceneManager.LoadScene(_gameConfig.GameSceneName);
        }

        #endregion

        // ────────────────────────────────
        // EVENT HANDLERS
        // ────────────────────────────────
        #region Event Handlers

        private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnGameSceneLoaded;

            Log.Trace($"[Bootstrapper] {scene.name} loaded. Launching game.");

            // Published here and not at the end of the sequence: scene objects subscribe in
            // OnEnable while the scene loads (R10), so by now they are listening.
            EventBus.Publish(new OnBootstrapComplete());

            _gameManager.StartGame();
        }

        #endregion
    }
}
