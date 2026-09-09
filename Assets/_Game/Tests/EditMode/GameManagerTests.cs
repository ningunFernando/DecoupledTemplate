using System;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DecoupledTemplate.Core;
using DecoupledTemplate.Core.State;

namespace DecoupledTemplate.Tests
{
    /// <summary>
    /// Covers C2 and A2 at the level where they actually bit: GameManager derives its enum from
    /// the state machine instead of storing a second copy, so the two cannot drift apart (R7).
    /// </summary>
    public class GameManagerTests
    {
        private static readonly Regex NoImplementation = new Regex(@"^\[GameManager\].*has no implementation");

        private GameObject  _hostObject;
        private GameManager _gameManager;

        // ────────────────────────────────
        // SETUP
        // ────────────────────────────────
        #region Setup

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAllSubscriptions();

            _hostObject  = new GameObject(nameof(GameManagerTests));
            _gameManager = _hostObject.AddComponent<GameManager>();

            InitializeStates(_gameManager);
        }

        [TearDown]
        public void TearDown()
        {
            if (_hostObject != null) UnityEngine.Object.DestroyImmediate(_hostObject);

            EventBus.ClearAllSubscriptions();
        }

        #endregion

        // ────────────────────────────────
        // TESTS
        // ────────────────────────────────
        #region Tests

        [Test]
        public void ChangeState_UnimplementedState_DoesNotMutateCurrentState()
        {
            // Asserted, not assumed: with no state machine CurrentState reads Bootstrap and the
            // check below would pass without proving anything.
            Assert.AreEqual(GameState.Menu, _gameManager.CurrentState,
                "The state machine was not initialized.");

            LogAssert.Expect(LogType.Error, NoImplementation);
            _gameManager.ChangeState(GameState.GameOver);

            // The reference project mutated the enum here and left the machine behind, so Exit
            // never ran and the reported state was a lie (C2).
            Assert.AreEqual(GameState.Menu, _gameManager.CurrentState);
        }

        [Test]
        public void ChangeState_ImplementedState_TransitionsAndPublishes()
        {
            int count = 0;
            OnGameStateChanged received = default;

            Action<OnGameStateChanged> handler = e =>
            {
                received = e;
                count++;
            };

            EventBus.Subscribe(handler);

            _gameManager.ChangeState(GameState.Play);

            Assert.AreEqual(GameState.Play, _gameManager.CurrentState);
            Assert.AreEqual(1, count, "The transition did not reach the bus.");
            Assert.AreEqual(GameState.Menu, received.previousState);
            Assert.AreEqual(GameState.Play, received.newState);

            EventBus.Unsubscribe(handler);
        }

        [Test]
        public void ChangeState_ToCurrentState_PublishesNothing()
        {
            int count = 0;
            Action<OnGameStateChanged> handler = _ => count++;

            EventBus.Subscribe(handler);

            _gameManager.ChangeState(GameState.Menu);

            Assert.AreEqual(0, count, "A no-op transition still published a state change.");

            EventBus.Unsubscribe(handler);
        }

        [Test]
        public void StartGame_EntersPlayState()
        {
            // StartGame is what the Bootstrapper calls once the game scene is loaded. Without
            // this test its only exercise is a manual Play Mode run.
            Action<OnGameStateChanged> sink = _ => { };
            EventBus.Subscribe(sink);

            _gameManager.StartGame();

            Assert.AreEqual(GameState.Play, _gameManager.CurrentState);

            EventBus.Unsubscribe(sink);
        }

        [Test]
        public void RegisterManagers_NullPool_Throws()
        {
            // Failing loudly beats a manager reference that is silently null until the first
            // Get call, halfway into gameplay (R9, A3).
            Assert.Throws<ArgumentNullException>(() => _gameManager.RegisterManagers(null));
        }

        #endregion

        // ────────────────────────────────
        // HELPERS
        // ────────────────────────────────
        #region Helpers

        /// <summary>
        /// AddComponent does not run Awake outside Play Mode, so without this the states stay
        /// null and every ChangeState reports "no implementation", including the ones that do
        /// have a state class. Only InitializeStates is invoked, never Awake: the singleton half
        /// of Awake calls DontDestroyOnLoad, which has no meaning here and would leak an Instance
        /// between tests.
        /// </summary>
        private static void InitializeStates(GameManager gameManager)
        {
            MethodInfo method = typeof(GameManager)
                .GetMethod("InitializeStates", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(method,
                "GameManager.InitializeStates was renamed or removed. Update this test seam.");

            method.Invoke(gameManager, null);
        }

        #endregion
    }
}
