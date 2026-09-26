using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Economy
{
    /// <summary>What the year's tasks produced, for the UI and the Annals.</summary>
    public readonly struct YearlyTaskReport
    {
        public int StonesMined { get; init; }
        public int Patrols { get; init; }
        public int QiPortionsGathered { get; init; }
    }

    /// <summary>
    /// Assigns each member's task for the year (<see cref="TaskRules"/> decides what is allowed) and
    /// resolves them when the Management phase ends. Teaching is resolved after everyone else.
    /// </summary>
    public sealed class TaskAssignmentSystem
    {
        public const int MineBaseYield = 50;
        public const int MineYieldPerRealm = 25;
        public const int RestStability = 5;
        public const double StudyBaseChance = 0.15;
        public const double StudyChancePerRoot = 0.003;
        public const int StudyXp = 10;
        public const int TeachingBaseXp = 20;
        public const int TeachingXpPerRealm = 10;
        public const int DiplomacyRelation = 5;

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly CultivationSystem cultivation;
        private readonly ResourceManager resources;
        private readonly MentalStabilitySystem stability;
        private readonly FactionManager factions;
        private readonly DeductionEngine deduction;
        private readonly EspionageSystem espionage;
        private readonly BuildingSystem buildings;
        private readonly TechniqueLibrary techniques;

        public TaskAssignmentSystem(GameContext ctx, ClanManager clan, CultivationSystem cultivation,
            ResourceManager resources, MentalStabilitySystem stability, FactionManager factions,
            DeductionEngine deduction, EspionageSystem espionage, BuildingSystem buildings, TechniqueLibrary techniques)
        {
            this.techniques = techniques;
            this.ctx = ctx;
            this.clan = clan;
            this.cultivation = cultivation;
            this.resources = resources;
            this.stability = stability;
            this.factions = factions;
            HuntingGround = ctx.Content.Clan.HomeRegion;
            this.deduction = deduction;
            this.espionage = espionage;
            this.buildings = buildings;
        }

        public bool AssignTask(CharacterData character, TaskType task)
        {
            if (!character.IsAlive) return false;
            if (!TaskRules.IsAllowed(character, task))
            {
                ctx.Log.Warning($"[Tasks] {character.FullName} cannot take {task} ({RankCatalog.DisplayName(character)}).");
                return false;
            }

            character.CurrentTask = task;
            return true;
        }

        public YearlyTaskReport ProcessYearlyTasks()
        {
            var members = clan.LivingMembers.ToList(); // tasks may kill or add members
            int stonesMined = 0;
            int patrols = 0;
            int qiGathered = 0;

            foreach (var member in members.Where(m => m.IsAlive))
            {
                if (!TaskRules.IsAllowed(member, member.CurrentTask))
                {
                    ctx.Log.Warning($"[Tasks] {member.FullName} cannot perform {member.CurrentTask}; task cleared.");
                    member.CurrentTask = TaskType.None; // e.g. a mortal still set to Cultivation from an old save
                    continue;
                }

                switch (member.CurrentTask)
                {
                    case TaskType.Cultivation: cultivation.ProcessYearlyCultivation(member); break;
                    case TaskType.Mine: stonesMined += MineYield(member); break;
                    case TaskType.Patrol: patrols++; break;
                    case TaskType.Rest: stability.ApplyModifier(member, RestStability); break;
                    case TaskType.Study: Study(member); break;
                    case TaskType.Diplomacy: Diplomacy(); break;
                    case TaskType.Espionage: espionage.AttemptEspionage(member, factions.RandomFaction()); break;
                    case TaskType.GatherQi: qiGathered += GatherQi(member); break;
                    case TaskType.HuntBeast: HuntBeast(member); break;
                        // Teaching needs this year's students: resolved below
                }
            }

            resources.AddSpiritStones(stonesMined);
            Teach(members);
            return new YearlyTaskReport { StonesMined = stonesMined, Patrols = patrols, QiPortionsGathered = qiGathered };
        }

        /// <summary>
        /// A year of harvesting spiritual Qi in wisps (LORE.md §2.5) with a method adapted to it: the Qi of
        /// the harvester's own method, otherwise the best Qi among the clan's known methods. A vanished Qi
        /// cannot be harvested, a ubiquitous one needs no harvest. Returns the portions condensed.
        /// </summary>
        private int GatherQi(CharacterData harvester)
        {
            var qi = Harvestable(techniques.MethodOf(harvester))
                ?? techniques.Known.Where(t => t.Kind == TechniqueKind.Cultivation)
                    .OrderByDescending(t => t.Grade)
                    .ThenBy(t => t.Name, StringComparer.Ordinal)
                    .Select(Harvestable)
                    .FirstOrDefault(q => q != null);
            if (qi == null)
            {
                ctx.Log.Warning($"[Tasks] {harvester.FullName} knows no Qi the clan can harvest.");
                return 0;
            }

            int portions = resources.AddHarvestWork(qi.Id, qi.YearsPerPortion);
            if (portions > 0) ctx.Log.Info($"[Tasks] {harvester.FullName} condenses {portions} portion(s) of {qi.Name}.");
            return portions;
        }

        private QiDefinition Harvestable(TechniqueData method)
        {
            var qi = techniques.FindQi(method?.RequiredQiId);
            return qi != null && !qi.Vanished && !qi.Ubiquitous ? qi : null;
        }

        /// <summary>50 + 25 per realm, +5% per Forge level.</summary>
        private int MineYield(CharacterData miner)
        {
            double forge = 1.0 + buildings.ForgeLevel * BuildingSystem.ForgeYieldBonusPerLevel;
            return (int)Math.Round((MineBaseYield + (int)miner.Realm * MineYieldPerRealm) * forge);
        }

        /// <summary>A chance to find a fragment (better with the root and the Library); otherwise some XP.</summary>
        private void Study(CharacterData scholar)
        {
            // A scholar with a foundation may first come to understand its Dao Partners (§5.3.3)
            if (scholar.FoundationId != null && !techniques.Knowledge.Knows(FactKind.DaoPartners, scholar.FoundationId)
                && ctx.Rng.Chance(ctx.Content.Balance.StudyRevealsPartnersChance))
            {
                techniques.Knowledge.Reveal(FactKind.DaoPartners, scholar.FoundationId, KnowledgeSource.Studied);
                ctx.Log.Info($"[Tasks] {scholar.FullName} comes to understand the Dao Partners of their foundation.");
                return;
            }

            double chance = StudyBaseChance + scholar.SpiritualRoot * StudyChancePerRoot
                + buildings.LibraryLevel * BuildingSystem.LibraryDiscoveryBonusPerLevel;
            if (ctx.Rng.Chance(chance))
            {
                int quality = Math.Clamp(scholar.SpiritualRoot / 25, 1, 4);
                var element = scholar.Affinity != Element.None ? scholar.Affinity : ctx.Rng.NextElement();
                deduction.AddFragment(element, quality, $"Found by {scholar.FullName}");
                ctx.Log.Info($"[Tasks] {scholar.FullName} finds a Q{quality} {element} fragment while studying.");
            }
            else
            {
                cultivation.GrantXp(scholar, StudyXp);
            }
        }

        /// <summary>+5 relation with a random faction, +2 per Council Room level.</summary>
        private void Diplomacy()
        {
            var target = factions.RandomFaction();
            if (target == null) return;
            factions.ChangeRelation(target.ID, DiplomacyRelation + buildings.CouncilLevel * BuildingSystem.CouncilRelationBonusPerLevel);
        }

        /// <summary>Each teacher takes one cultivating student, lowest realm first: 20 + 10 per teacher realm XP.</summary>
        private void Teach(IReadOnlyList<CharacterData> members)
        {
            var teachers = members.Where(m => m.IsAlive && m.CurrentTask == TaskType.Teaching).ToList();
            var students = members.Where(m => m.IsAlive && m.CurrentTask == TaskType.Cultivation).OrderBy(m => m.Realm).ToList();

            for (int i = 0; i < teachers.Count && i < students.Count; i++)
            {
                int bonus = TeachingBaseXp + (int)teachers[i].Realm * TeachingXpPerRealm;
                cultivation.GrantXp(students[i], bonus);
                ctx.Log.Info($"[Tasks] {teachers[i].FullName} teaches {students[i].FullName} (+{bonus} XP).");
            }
        }

        /// <summary>
        /// A year's hunt for a spirit beast (user decision, 2026-09-26: the mirror's ritual sacrifices beasts): with the
        /// balance's chance, a beast of the hunter's realm, never of a higher stage than theirs.
        /// </summary>
        private void HuntBeast(CharacterData hunter)
        {
            if (!ctx.Rng.Chance(ctx.Content.Balance.Talismans.HuntCaptureChance)) return;
            int stage = ctx.Rng.Next(1, System.Math.Max(1, hunter.RealmStage) + 1);
            var powersHere = factions.Factions.Where(f => f.RegionId == HuntingGround).ToList();
            string owner = powersHere.Count > 0 && ctx.Rng.Chance(ctx.Content.Balance.Talismans.OwnedBeastChance)
                ? ctx.Rng.Pick(powersHere).Name : null; // a power's beast, or a solitary one
            resources.AddBeast(new CapturedBeast(ctx.Rng.NextId(), hunter.Realm, stage, owner));
            ctx.Log.Info($"[Tasks] {hunter.FullName} captures a spirit beast ({hunter.Realm}, stage {stage}){(owner == null ? "" : $" belonging to {owner}")}.");
        }

        /// <summary>Where the clan's hunters go (a regions.json id): its home unless the player sends them elsewhere.</summary>
        public string HuntingGround { get; private set; }

        /// <summary>Sends the hunters to a place of the map; false for a place off it or a whole state or sea.</summary>
        public bool SetHuntingGround(string regionId)
        {
            if (ctx.Content.Regions.FirstOrDefault(r => r.Id == regionId)?.ParentId == null) return false;
            HuntingGround = regionId;
            return true;
        }
    }
}
