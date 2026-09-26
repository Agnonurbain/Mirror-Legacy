using System.Collections.Generic;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Data
{
    /// <summary>Techniques carry their lore classification (LORE.md §2) and read the saves made before it.</summary>
    [TestFixture]
    public class TechniqueDataTests
    {
        private static TechniqueData LoadSavedTechnique(string type)
        {
            string json = "{ \"SaveVersion\": \"2.0\", \"Techniques\": [ { \"ID\": \"t1\", \"Name\": \"Flowing Fist\", \"Type\": \""
                + type + "\", \"PowerModifier\": 20 } ] }";
            return SaveSerializer.Deserialize(json).Techniques[0];
        }

        [TestCase("CultivationMethod", TechniqueKind.Cultivation, TechniqueEffect.None)]
        [TestCase("MartialArt", TechniqueKind.Weapon, TechniqueEffect.Strike)]
        [TestCase("SupportArt", TechniqueKind.Spell, TechniqueEffect.Heal)]
        public void OlderSaves_ReadTheirTypeAsAKindAndAnEffect(string type, TechniqueKind kind, TechniqueEffect effect)
        {
            var technique = LoadSavedTechnique(type);
            Assert.AreEqual(kind, technique.Kind);
            Assert.AreEqual(effect, technique.Effect);
        }

        [Test]
        public void Saves_NoLongerWriteTheOldType()
        {
            var data = new GameData { Techniques = new List<TechniqueData> { new TechniqueData { ID = "t1", Kind = TechniqueKind.Weapon } } };
            StringAssert.DoesNotContain("\"Type\"", SaveSerializer.Serialize(data));
        }

        [Test]
        public void Clone_CopiesTheInterpretedFields()
        {
            var original = new TechniqueData { InterpretedFields = new List<string> { "Grade" } };
            var copy = original.Clone();
            copy.InterpretedFields.Add("Category");
            CollectionAssert.AreEqual(new[] { "Grade" }, original.InterpretedFields);
        }

        [Test]
        public void Clone_CopiesTheFlaws()
        {
            var original = new TechniqueData
            {
                Flaws = new TechniqueFlaws { LifespanFactor = 0.95, SpeedByRealm = new Dictionary<CultivationRealm, double> { [CultivationRealm.Embryonic] = 1.5 } }
            };

            var copy = original.Clone();
            copy.Flaws.SpeedByRealm[CultivationRealm.Embryonic] = 3.0;

            Assert.AreEqual(1.5, original.Flaws.SpeedByRealm[CultivationRealm.Embryonic]);
        }
    }
}
