using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Clan
{
    /// <summary>
    /// The clan's roster: who lives, who leads, who is born. Every death goes through <see cref="Kill"/>,
    /// which updates the roster and the succession before telling the rest of the game.
    /// </summary>
    public sealed class ClanManager
    {
        private const int NewbornStability = 70;

        private readonly GameContext ctx;
        private readonly List<CharacterData> living = new List<CharacterData>();

        public string ClanName { get; }
        public BloodRegistry Registry { get; } = new BloodRegistry();
        public IReadOnlyList<CharacterData> LivingMembers => living;
        public string PatriarchID { get; private set; }

        public ClanManager(GameContext ctx, string clanName)
        {
            this.ctx = ctx;
            ClanName = clanName;
        }

        public CharacterData GetPatriarch() => living.Find(m => m.ID == PatriarchID);

        public CharacterData FindById(string id) => Registry.FindById(id);

        /// <summary>A newborn, a founder or a spouse marrying in.</summary>
        public void AddMember(CharacterData member)
        {
            Registry.Register(member);
            if (member.IsAlive && !living.Contains(member))
                living.Add(member);

            ctx.Log.Info($"[ClanManager] {member.FullName} joins the clan (age {member.Age}).");
            ctx.Events.TriggerCharacterBorn(member);
        }

        public void AppointPatriarch(CharacterData member)
        {
            PatriarchID = member?.ID;
        }

        /// <summary>Returns false when the character was already dead.</summary>
        public bool Kill(CharacterData character, DeathCause cause)
        {
            if (!character.IsAlive) return false;

            character.IsAlive = false;
            character.CauseOfDeath = cause;
            LeaveTheLiving(character);

            ctx.Log.Info($"[ClanManager] {character.FullName} dies at {character.Age}: {cause}.");
            ctx.Events.TriggerCharacterDied(character, cause);
            return true;
        }

        /// <summary>An ancestor leaves the mortal plane: gone from the clan, but not dead.</summary>
        public void Ascend(CharacterData character)
        {
            if (!character.IsAlive) return;

            character.IsAlive = false;
            LeaveTheLiving(character);

            ctx.Log.Info($"[ClanManager] The heavens open: {character.FullName} ascends!");
            ctx.Events.TriggerAncestorAscended(character);
        }

        /// <summary>
        /// A newborn of the clan: genetics from both parents, the hereditary orifice rolled (LORE.md §4, D3),
        /// a mortal lifespan until the first chakra, and nobody has examined it yet.
        /// </summary>
        public CharacterData GenerateChild(CharacterData father, CharacterData mother)
        {
            var rng = ctx.Rng;
            bool isMale = rng.NextDouble() >= 0.5;
            var names = isMale ? ctx.Content.Names.Male : ctx.Content.Names.Female;

            var child = new CharacterData
            {
                FirstName = names[rng.Next(names.Count)],
                LastName = ClanName,
                IsMale = isMale,
                Age = 0,
                MaxLifespan = SpiritualOrificeRules.MortalLifespan(rng.NextDouble()),
                SpiritualRoot = GeneticSystem.GenerateSpiritualRoot(father, mother, rng),
                Affinity = GeneticSystem.GenerateAffinity(father, mother, rng),
                Realm = CultivationRealm.Embryonic,
                MentalStability = NewbornStability,
                FatherID = father?.ID,
                MotherID = mother?.ID
            };
            child.HasSpiritualOrifice = SpiritualOrificeRules.HasOrificeAtBirth(
                SpiritualOrificeRules.CountParentsWithOrifice(father, mother), rng.NextDouble(), ctx.Content.Balance.OrificeOdds);
            child.ID = rng.NextId(); // seeded: the same game always names the same child

            AddMember(child);
            return child;
        }

        /// <summary>
        /// Each living couple whose mother is within the motherhood window may have a child
        /// (balance.json: 16-45, 25% a year). Returns the births.
        /// </summary>
        public int ProcessAnnualBirths()
        {
            var balance = ctx.Content.Balance;
            int births = 0;
            foreach (var father in living.Where(m => m.IsMale && !string.IsNullOrEmpty(m.SpouseID)).ToList())
            {
                var mother = living.Find(m => m.ID == father.SpouseID);
                if (mother == null || mother.Age < balance.MinMotherAge || mother.Age > balance.MaxMotherAge) continue;

                if (ctx.Rng.NextDouble() < balance.AnnualBirthChance)
                {
                    GenerateChild(father, mother);
                    births++;
                }
            }
            return births;
        }

        /// <summary>A Summit Eye cultivator examines newborns and newcomers (LORE.md §4).</summary>
        public int ExamineOrifices()
        {
            int examined = SpiritualOrificeRules.RevealOrifices(living);
            if (examined > 0)
                ctx.Log.Info($"[ClanManager] {examined} member(s) examined for a spiritual orifice.");
            return examined;
        }

        /// <summary>Rebuilds the roster from a save.</summary>
        public void Restore(IEnumerable<CharacterData> records, string patriarchId)
        {
            Registry.Clear();
            living.Clear();
            foreach (var record in records)
            {
                Registry.Register(record);
                if (record.IsAlive) living.Add(record);
            }

            PatriarchID = patriarchId;
            if (GetPatriarch() == null) ElectPatriarch();
        }

        private void LeaveTheLiving(CharacterData character)
        {
            living.Remove(character);
            if (character.ID == PatriarchID) ElectPatriarch();
        }

        /// <summary>Highest realm, then eldest, then strongest root.</summary>
        private void ElectPatriarch()
        {
            var successor = living
                .OrderByDescending(m => (int)m.Realm)
                .ThenByDescending(m => m.RealmStage)
                .ThenByDescending(m => m.Age)
                .ThenByDescending(m => m.SpiritualRoot)
                .FirstOrDefault();

            PatriarchID = successor?.ID;
            if (successor != null)
                ctx.Log.Info($"[ClanManager] {successor.FullName} becomes the new patriarch.");
            else
                ctx.Log.Warning("[ClanManager] The clan has no living members. The lineage is broken.");
        }
    }
}
