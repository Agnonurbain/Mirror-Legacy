using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The arts deduced by the mirror (spells, movements, weapons) are learned (AUDIT_LORE.md §7.11; the user's choice of
    /// 2026-10-15): an art enters the clan's library; a member is set to study it (the Study task) for its grade in years,
    /// then masters it and wields it on the battle grid. Only a cultivator of its realm studies it; leaving the study
    /// abandons it.
    /// </summary>
    [TestFixture]
    public class ArtStudyTests
    {
        private static int YearsPerGrade => Fixtures.Content.Balance.Techniques.ArtStudyYearsPerGrade;

        private static TechniqueData Art(TestWorld w, int grade = 2, TechniqueKind kind = TechniqueKind.Spell)
        {
            var art = new TechniqueData
            {
                ID = $"art-{kind}-{grade}", Name = "Paume de la Marée", Kind = kind, Effect = kind == TechniqueKind.Movement ? TechniqueEffect.None : TechniqueEffect.Strike,
                Grade = grade, Category = TechniqueCategory.Secret, DominantElement = Element.Water,
                RequiredRealm = MirrorChronicles.Characters.TechniqueRules.ArtRequiredRealm(grade, Fixtures.Content.Balance.Techniques),
                PowerModifier = 20, QiCost = 5, Range = 2
            };
            w.Techniques.AddDeduced(art);
            return art;
        }

        private static void Years(TestWorld w, int years)
        {
            for (int y = 0; y < years; y++) w.Tasks.ProcessYearlyTasks();
        }

        [Test]
        public void AMember_StudiesAnArt_ForItsGradeInYears_ThenMastersIt()
        {
            var w = new TestWorld();
            var art = Art(w, grade: 2);
            var scholar = w.Join(Fixtures.Cultivator());

            Assert.IsNull(w.Tasks.AssignArtStudy(scholar, art.ID));
            Assert.AreEqual(TaskType.Study, scholar.CurrentTask);
            Years(w, 2 * YearsPerGrade - 1);
            Assert.IsFalse(scholar.KnownTechniqueIDs.Contains(art.ID), "not yet");
            Years(w, 1);

            Assert.IsTrue(scholar.KnownTechniqueIDs.Contains(art.ID));
            Assert.IsNull(scholar.StudyArtId);
            Assert.AreEqual(TaskType.Cultivation, scholar.CurrentTask, "back to its cultivation");
        }

        [Test]
        public void TheMastery_IsTold()
        {
            var w = new TestWorld();
            var art = Art(w, grade: 1);
            var scholar = w.Join(Fixtures.Cultivator());
            (CharacterData Member, TechniqueData Art)? told = null;
            w.Ctx.Events.OnArtMastered += (m, a) => told = (m, a);
            w.Tasks.AssignArtStudy(scholar, art.ID);
            Years(w, YearsPerGrade);
            Assert.AreEqual((scholar, art), told);
        }

        [Test]
        public void AMethod_IsNoArtToStudy()
        {
            var w = new TestWorld();
            w.Techniques.Learn(Fixtures.ClanMethod);
            Assert.IsNotNull(w.Tasks.AssignArtStudy(w.Join(Fixtures.Cultivator()), Fixtures.ClanMethod));
        }

        [Test]
        public void AnArtTheClanLacks_CannotBeStudied()
        {
            var w = new TestWorld();
            Assert.IsNotNull(w.Tasks.AssignArtStudy(w.Join(Fixtures.Cultivator()), "an-art-nobody-knows"));
        }

        [Test]
        public void AnArtAboveTheMembersRealm_CannotBeStudied()
        {
            var w = new TestWorld();
            var art = Art(w, grade: 5); // from the Foundation
            Assert.IsNotNull(w.Tasks.AssignArtStudy(w.Join(Fixtures.Cultivator()), art.ID));
        }

        [Test]
        public void AMortal_StudiesNoArt()
        {
            var w = new TestWorld();
            var art = Art(w, grade: 1);
            Assert.IsNotNull(w.Tasks.AssignArtStudy(w.Join(Fixtures.Mortal()), art.ID));
        }

        [Test]
        public void AnArtAlreadyMastered_IsNotStudiedAgain()
        {
            var w = new TestWorld();
            var art = Art(w, grade: 1);
            var master = w.Join(Fixtures.Cultivator());
            master.KnownTechniqueIDs.Add(art.ID);
            Assert.IsNotNull(w.Tasks.AssignArtStudy(master, art.ID));
        }

        [Test]
        public void LeavingTheStudy_AbandonsIt()
        {
            var w = new TestWorld();
            var art = Art(w, grade: 2);
            var scholar = w.Join(Fixtures.Cultivator());
            w.Tasks.AssignArtStudy(scholar, art.ID);
            Years(w, 1);
            w.Tasks.AssignTask(scholar, TaskType.Cultivation);
            Assert.IsNull(scholar.StudyArtId);
            Assert.AreEqual(0, scholar.StudyArtYears);
        }

        [Test]
        public void AStudyBrokenOff_IsNotResumedByAPlainStudy()
        {
            var w = new TestWorld();
            var art = Art(w, grade: 2);
            var scholar = w.Join(Fixtures.Cultivator());
            w.Tasks.AssignArtStudy(scholar, art.ID);
            scholar.CurrentTask = TaskType.None; // taken captive, sent away… the task changed without AssignTask
            w.Tasks.AssignTask(scholar, TaskType.Study);
            Assert.IsNull(scholar.StudyArtId, "a plain study looks for fragments, as ever");
        }

        [Test]
        public void AStudyBrokenOff_IsForgottenAtTheYearsTasks()
        {
            var w = new TestWorld();
            var art = Art(w, grade: 2);
            var scholar = w.Join(Fixtures.Cultivator());
            w.Tasks.AssignArtStudy(scholar, art.ID);
            scholar.CurrentTask = TaskType.Mine;
            Years(w, 1);
            Assert.IsNull(scholar.StudyArtId);
            Assert.AreEqual(0, scholar.StudyArtYears);
        }

        [Test]
        public void ResumingTheSameArt_KeepsTheYearsGiven()
        {
            var w = new TestWorld();
            var art = Art(w, grade: 3);
            var scholar = w.Join(Fixtures.Cultivator());
            w.Tasks.AssignArtStudy(scholar, art.ID);
            Years(w, 1);
            Assert.IsNull(w.Tasks.AssignArtStudy(scholar, art.ID));
            Assert.AreEqual(1, scholar.StudyArtYears);
        }

        [Test]
        public void OneWhoMayNotStudy_StudiesNoArt()
        {
            var w = new TestWorld();
            var art = Art(w, grade: 1); // wielded from the Embryonic Breathing
            var breathing = w.Join(Fixtures.Cultivator(age: 20, realm: CultivationRealm.Embryonic, stage: 5));
            Assert.IsNotNull(w.Tasks.ArtStudyRefusal(breathing, art.ID), "the Breathing does not take the Study task");
            var retreating = w.Join(Fixtures.Cultivator());
            retreating.Retreat = Retreat.Manifestation;
            Assert.IsNotNull(w.Tasks.ArtStudyRefusal(retreating, art.ID));
        }

        [Test]
        public void AStudyUnderWay_IsKeptAcrossASave()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var art = new TechniqueData { ID = "art-saved", Name = "Pas du Héron", Kind = TechniqueKind.Movement, Grade = 1, Category = TechniqueCategory.Secret, MovementSteps = 1 };
            s.Techniques.AddDeduced(art);
            var scholar = Fixtures.Cultivator();
            s.Clan.AddMember(scholar);
            s.Tasks.AssignArtStudy(scholar, art.ID);
            scholar.StudyArtYears = 1;

            var loaded = GameSession.FromSaveData(s.ToSaveData(), new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var again = loaded.Clan.FindById(scholar.ID);
            Assert.AreEqual(art.ID, again.StudyArtId);
            Assert.AreEqual(1, again.StudyArtYears);
        }
    }
}
