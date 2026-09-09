using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using DecoupledTemplate.Core;
using DecoupledTemplate.Core.Pool;
using DecoupledTemplate.Core.State;
using DecoupledTemplate.Save;

namespace DecoupledTemplate.Tests
{
    /// <summary>
    /// The one thing EditMode cannot prove: that the scene, the prefabs and the Inspector wiring
    /// actually produce a running game. Everything else about the bootstrap is checked in
    /// milliseconds elsewhere; this is the end-to-end smoke test the Definition of Done asks for.
    /// </summary>
    public class BootstrapSequenceTests
    {
        private const string BootstrapScene = "Scene_Bootstrap";

        /// <summary>
        /// Generous on purpose. The sequence yields three frames and then loads a scene, so the
        /// real cost is the scene load; this is a deadlock guard, not a performance budget.
        /// </summary>
        private const int MaxFrames = 600;

        // ────────────────────────────────
        // TEARDOWN
        // ────────────────────────────────
        #region Teardown

        /// <summary>
        /// The managers live in DontDestroyOnLoad, so without this they outlive the test and the
        /// next run starts with a GameManager already claiming the singleton.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            DestroyAll(Object.FindObjectsByType<GameManager>());
            DestroyAll(Object.FindObjectsByType<ObjectPoolManager>());
            DestroyAll(Object.FindObjectsByType<SaveSystem>());

            EventBus.ClearAllSubscriptions();
        }

        #endregion

        // ────────────────────────────────
        // TESTS
        // ────────────────────────────────
        #region Tests

        [UnityTest]
        public IEnumerator Bootstrap_FromBootstrapScene_ReachesGameSceneInPlayState()
        {
            SceneManager.LoadScene(BootstrapScene);
            yield return null;

            int frames = 0;

            while (frames < MaxFrames && !HasStarted())
            {
                frames++;
                yield return null;
            }

            Assert.IsNotNull(GameManager.Instance,
                $"No GameManager after {MaxFrames} frames. The sequence never instantiated the managers.");

            // Play is only reached through StartGame, which only runs from the sceneLoaded
            // handler. Asserting it covers the whole chain, including the handler that must not
            // be unsubscribed in OnDestroy.
            Assert.AreEqual(GameState.Play, GameManager.Instance.CurrentState,
                "The bootstrap finished but the game never started.");

            Assert.AreNotEqual(BootstrapScene, SceneManager.GetActiveScene().name,
                "The game scene was never loaded.");

            Assert.IsNotNull(GameManager.Instance.PoolManager,
                "The pool manager was never injected (R6).");
        }

        #endregion

        // ────────────────────────────────
        // HELPERS
        // ────────────────────────────────
        #region Helpers

        private static bool HasStarted() =>
            GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Play;

        private static void DestroyAll<T>(T[] components) where T : Component
        {
            foreach (T component in components)
            {
                if (component != null) Object.Destroy(component.gameObject);
            }
        }

        #endregion
    }
}
