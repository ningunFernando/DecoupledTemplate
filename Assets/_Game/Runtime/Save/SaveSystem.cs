using System;
using System.IO;
using UnityEngine;
using DecoupledTemplate.Core;

namespace DecoupledTemplate.Save
{
    /// <summary>
    /// The Unity adapter: owns the lifecycle, resolves persistentDataPath and reacts to pause and
    /// quit. Every rule lives in ProgressService and SaveMigrations; this class stays thin (R5, M6).
    /// </summary>
    public class SaveSystem : MonoBehaviour, ISaveLifecycle
    {
        // ────────────────────────────────
        // INSPECTOR
        // ────────────────────────────────
        #region Inspector

        [Header("Save")]
        [Tooltip("File name inside Application.persistentDataPath.")]
        [SerializeField] private string _fileName = "save.json";

        #endregion

        private ISaveStorage    _storage;
        private SaveMigrations  _migrations;
        private ProgressService _progress;
        private SaveData        _data;
        private bool            _dirty;

        // ────────────────────────────────
        // LIFECYCLE
        // ────────────────────────────────
        #region Lifecycle

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(_fileName))
            {
                throw new InvalidOperationException("[SaveSystem] Save file name is empty.");
            }

            _storage = new JsonSaveStorage(Path.Combine(Application.persistentDataPath, _fileName));
            _migrations = new SaveMigrations();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<OnProgressChanged>(MarkDirty);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnProgressChanged>(MarkDirty);
        }

        /// <summary>
        /// OnApplicationQuit is not reliable on Android and iOS; OnApplicationPause(true) is the
        /// primary trigger and quit is only a second chance (C6).
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveIfDirty();
        }

        private void OnApplicationQuit()
        {
            SaveIfDirty();
        }

        #endregion

        // ────────────────────────────────
        // PUBLIC API
        // ────────────────────────────────
        #region Public API

        /// <summary>
        /// The access point for gameplay and for the debug HUD. The SaveData itself is never
        /// exposed: a public mutable CurrentData compiled in the reference and left no trace (M6).
        /// </summary>
        public ProgressService Progress => _progress;

        public void Load()
        {
            SaveData stored = _storage.Load();

            if (stored == null)
            {
                _data = new SaveData();
            }
            else if (stored.saveVersion != SaveData.CURRENT_VERSION)
            {
                _data = LoadMigrated(stored);
            }
            else
            {
                _data = stored;
            }

            _progress = new ProgressService(_data);
            _dirty = false;

            Log.Trace($"[SaveSystem] Loaded save v{_data.saveVersion}.");
        }

        public void Save()
        {
            if (_data == null)
            {
                // Loud but not fatal: this also runs from OnApplicationPause, where throwing
                // would interrupt the platform's own pause handling.
                Log.Error("[SaveSystem] Save called before Load. Nothing written.");
                return;
            }

            _storage.Save(_data);
            _dirty = false;

            Log.Trace("[SaveSystem] Save written.");
        }

        #endregion

        // ────────────────────────────────
        // PRIVATE
        // ────────────────────────────────
        #region Private

        private void MarkDirty(OnProgressChanged e)
        {
            _dirty = true;
        }

        private SaveData LoadMigrated(SaveData stored)
        {
            _storage.Backup(stored.saveVersion);

            SaveData migrated = _migrations.Migrate(stored);

            if (migrated == null)
            {
                Log.Error("[SaveSystem] Migration failed. Starting a new save; the old file is kept as .bak.");
                return new SaveData();
            }

            return migrated;
        }

        private void SaveIfDirty()
        {
            if (_dirty) Save();
        }

        #endregion
    }
}
