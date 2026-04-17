using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MirrorChronicles.Clan;
using MirrorChronicles.Core;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;

namespace MirrorChronicles.Tests.PlayMode
{
    [TestFixture]
    public class SimulationPlayModeTest
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("ClanDomain", LoadSceneMode.Single);
            yield return null; // wait one frame for Awake
            yield return null; // wait one more for Start (GameManager.StartGameLoop)
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Load empty scene to reset state
            var newScene = SceneManager.CreateScene("CleanUp");
            SceneManager.SetActiveScene(newScene);
            yield return null;
        }

        /// <summary>
        /// Advance 4 phases (Management → Events → Breakthrough → Inheritance → next year).
        /// Yield after each phase so MonoBehaviour callbacks process.
        /// </summary>
        private IEnumerator AdvanceOneYear()
        {
            for (int phase = 0; phase < 4; phase++)
            {
                TimeManager.Instance.AdvancePhase();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Simulation10Years_ClanSurvives()
        {
            Assert.IsNotNull(TimeManager.Instance, "TimeManager missing");
            Assert.IsNotNull(ClanManager.Instance, "ClanManager missing");

            int initialYear = TimeManager.Instance.CurrentYear;
            int initialMembers = ClanManager.Instance.LivingMembers.Count;
            Assert.That(initialMembers, Is.GreaterThan(0), "Clan should start with members");

            for (int y = 0; y < 10; y++)
                yield return AdvanceOneYear();

            Assert.AreEqual(initialYear + 10, TimeManager.Instance.CurrentYear,
                "Should have advanced 10 years");
            Assert.That(ClanManager.Instance.LivingMembers.Count, Is.GreaterThan(0),
                "Clan should still have at least 1 living member after 10 years");
        }

        [UnityTest]
        public IEnumerator Simulation10Years_SpiritStonesGenerated()
        {
            Assert.IsNotNull(ResourceManager.Instance, "ResourceManager missing");

            int initialStones = ResourceManager.Instance.SpiritStones;

            for (int y = 0; y < 10; y++)
                yield return AdvanceOneYear();

            Assert.That(ResourceManager.Instance.SpiritStones, Is.GreaterThanOrEqualTo(initialStones),
                "Spirit stones should not decrease over 10 years of pure simulation");
        }

        [UnityTest]
        public IEnumerator AllSystemsPresent_AfterSceneLoad()
        {
            Assert.IsNotNull(GameManager.Instance, "GameManager");
            Assert.IsNotNull(TimeManager.Instance, "TimeManager");
            Assert.IsNotNull(SaveSystem.Instance, "SaveSystem");
            Assert.IsNotNull(ClanManager.Instance, "ClanManager");
            Assert.IsNotNull(BloodRegistry.Instance, "BloodRegistry");
            Assert.IsNotNull(ResourceManager.Instance, "ResourceManager");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PhaseAdvancement_CyclesThroughAllPhases()
        {
            Assert.AreEqual(GamePhase.Management, TimeManager.Instance.CurrentPhase);

            TimeManager.Instance.AdvancePhase();
            yield return null;
            Assert.AreEqual(GamePhase.Events, TimeManager.Instance.CurrentPhase);

            TimeManager.Instance.AdvancePhase();
            yield return null;
            Assert.AreEqual(GamePhase.Breakthrough, TimeManager.Instance.CurrentPhase);

            TimeManager.Instance.AdvancePhase();
            yield return null;
            Assert.AreEqual(GamePhase.Inheritance, TimeManager.Instance.CurrentPhase);

            int yearBefore = TimeManager.Instance.CurrentYear;
            TimeManager.Instance.AdvancePhase();
            yield return null;
            Assert.AreEqual(GamePhase.Management, TimeManager.Instance.CurrentPhase);
            Assert.AreEqual(yearBefore + 1, TimeManager.Instance.CurrentYear);
        }
    }
}
