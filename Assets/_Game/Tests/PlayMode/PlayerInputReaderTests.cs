using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DecoupledTemplate.Player;

namespace DecoupledTemplate.Tests
{
    /// <summary>
    /// The reader's failure path. Reading real input is covered end to end in PlayerMoverTests.
    /// PlayMode because Awake and OnEnable only run there.
    /// </summary>
    public class PlayerInputReaderTests
    {
        private static readonly Regex NoMoveAction =
            new Regex(@"^\[PlayerInputReader\] Move action not assigned");

        // ────────────────────────────────
        // TESTS
        // ────────────────────────────────
        #region Tests

        [Test]
        public void Awake_WithoutMoveAction_LogsErrorAndDisables()
        {
            // Created inactive so Awake waits for SetActive, after the expectation is in place.
            var host = new GameObject("ReaderWithoutAction");
            host.SetActive(false);
            PlayerInputReader reader = host.AddComponent<PlayerInputReader>();

            LogAssert.Expect(LogType.Error, NoMoveAction);

            // Disabling in Awake makes Unity call OnDisable immediately, before any OnEnable. The first
            // version dereferenced the missing action there and threw a NullReferenceException right
            // after the error; any exception from OnEnable or OnDisable fails this test as unexpected.
            host.SetActive(true);

            Assert.IsFalse(reader.enabled, "The reader stayed enabled without a move action.");
            Assert.AreEqual(Vector2.zero, reader.Move);

            Object.Destroy(host);
        }

        #endregion
    }
}
