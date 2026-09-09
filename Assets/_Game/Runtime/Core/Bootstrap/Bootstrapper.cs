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

        [Header("Save")]
        [Tooltip("SaveSystem prefab. Required: the sequence throws if it is not assigned.")]
        [SerializeField] private GameObject        _saveSystemPrefab;

        #endregion

        private GameManager       _gameManager;
        private ObjectPoolManager _poolManager;
        private ISaveLifecycle    _saveSystem;

        // ────────────────────────────────
        // LIFECYCLE
        // ────────────────────────────────
        #region Lifecycle

        private void Awake()
        {
            StartCoroutine(InitializeSequence());
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

            LoadSaveData();
            yield return null;
            Log.Trace("[Bootstrapper] Step 2 complete: Save Data");

            InitializeObjectPools();
            yield return null;
            Log.Trace("[Bootstrapper] Step 3 complete: Object Pools");

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

            if (_saveSystemPrefab == null)
            {
                throw new InvalidOperationException("[Bootstrapper] SaveSystem prefab not assigned.");
            }
        }

        private void InstantiateManagers()
        {
            // Correct here and only here: this runs before anything is instantiated, so no
            // DontDestroyOnLoad subscriber can be left deaf by a later scene reload (A1).
            EventBus.ClearAllSubscriptions();

            _gameManager = InstantiateRequired(_gameManagerPrefab, "GameManager");
            _poolManager = InstantiateRequired(_poolManagerPrefab, "ObjectPoolManager");

            // Untyped on purpose: Core cannot reference the Save assembly (R3), so the prefab is
            // a plain GameObject and the contract is the ISaveLifecycle interface that Core owns.
            GameObject saveInstance = InstantiateRequired(_saveSystemPrefab, "SaveSystem");
            _saveSystem = saveInstance.GetComponent<ISaveLifecycle>();

            if (_saveSystem == null)
            {
                throw new InvalidOperationException("[Bootstrapper] SaveSystem prefab has no ISaveLifecycle component.");
            }
        }

        /// <summary>
        /// Throwing on purpose: an incomplete bootstrap has to fail loudly and early. Degrading
        /// to a game that starts without its managers produces a NullReferenceException halfway
        /// through the coroutine and a black screen with no usable diagnostic (A3).
        /// </summary>
        private T InstantiateRequired<T>(T prefab, string label) where T : UnityEngine.Object
        {
            if (prefab == null)
            {
                throw new InvalidOperationException($"[Bootstrapper] {label} prefab not assigned.");
            }

            return Instantiate(prefab);
        }

        private void LoadSaveData()
        {
            _saveSystem.Load();
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
            // Self-unsubscribing here is what keeps the static event clean: loading the game
            // scene unloads Scene_Bootstrap and destroys this object before the callback fires,
            // so an OnDestroy doing the same job would remove the handler right before Unity
            // invokes it. The DontDestroyOnLoad managers outlive the scene; if Play Mode was
            // stopped mid-load the reference is gone and there is nothing left to launch.
            SceneManager.sceneLoaded -= OnGameSceneLoaded;

            if (_gameManager == null) return;

            Log.Trace($"[Bootstrapper] {scene.name} loaded. Launching game.");

            // Published here and not at the end of the sequence: scene objects subscribe in
            // OnEnable while the scene loads (R10), so by now they are listening.
            EventBus.Publish(new OnBootstrapComplete());

            _gameManager.StartGame();
        }

        #endregion
    }
}
