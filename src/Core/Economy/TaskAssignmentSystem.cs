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

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly CultivationSystem cultivation;
        private readonly ResourceManager resources;
        private readonly MentalStabilitySystem stability;
        private readonly FactionManager factions;
        private readonly Mirror.TalismanSystem talismans;
        private readonly World.BeastRegistry bestiary;
        private readonly DeductionEngine deduction;
        private readonly EspionageSystem espionage;
        private readonly BuildingSystem buildings;
        private readonly TechniqueLibrary techniques;
        private readonly Mirror.ShardSystem shards;
        private readonly SectSystem sect;

        public TaskAssignmentSystem(GameContext ctx, ClanManager clan, CultivationSystem cultivation,
            ResourceManager resources, MentalStabilitySystem stability, FactionManager factions,
            DeductionEngine deduction, EspionageSystem espionage, BuildingSystem buildings, TechniqueLibrary techniques,
            Mirror.TalismanSystem talismans, World.BeastRegistry bestiary, Mirror.ShardSystem shards, SectSystem sect)
        {
            this.sect = sect;
            this.shards = shards;
            this.talismans = talismans;
            this.bestiary = bestiary;
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
            if (character.CurrentTask == TaskType.HuntBeast || character.CurrentTask == TaskType.Diversion)
            {
                ctx.Log.Warning($"[Tasks] {character.FullName} is away on an operation until the year ends.");
                return false; // the hunt keeps them the whole year (HuntOperations): back at the next year's start
            }
            if (!TaskRules.IsAllowed(character, task, talismans.HuntWindowOpen, shards.LakeSearchOpen))
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
            var minersYield = new List<int>();
            int patrols = 0;
            int qiGathered = 0;
            int lakeSearchers = 0;

            foreach (var member in members.Where(m => m.IsAlive))
            {
                if (!TaskRules.IsAllowed(member, member.CurrentTask, talismans.HuntWindowOpen, shards.LakeSearchOpen))
                {
                    ctx.Log.Warning($"[Tasks] {member.FullName} cannot perform {member.CurrentTask}; task cleared.");
                    member.CurrentTask = TaskType.None; // e.g. a mortal still set to Cultivation from an old save
                    continue;
                }

                switch (member.CurrentTask)
                {
                    case TaskType.Cultivation: cultivation.ProcessYearlyCultivation(member); break;
                    case TaskType.Mine: minersYield.Add(MineYield(member)); break;
                    case TaskType.Patrol: patrols++; break;
                    case TaskType.Rest: stability.ApplyModifier(member, RestStability); break;
                    case TaskType.Study: Study(member); break;
                    case TaskType.Diplomacy: Diplomacy(member); break;
                    case TaskType.Espionage: espionage.AttemptEspionage(member, factions.RandomFaction()); break;
                    case TaskType.GatherQi: qiGathered += GatherQi(member); break;
                    case TaskType.HuntBeast:
                    case TaskType.Diversion: break; // engaged in a hunt operation this year (HuntOperations)
                    case TaskType.Seclusion: break; // hidden away (DaoHuntSystem): nothing else
                    case TaskType.ScoutBeasts: ScoutBeasts(member); break;
                    case TaskType.SearchLake: lakeSearchers++; break;
                    case TaskType.ArtPractice: break; // the arts' year (ArtSystem): mastery, and cultivation by one's gift
                        // Teaching needs this year's students: resolved below
                }
            }

            shards.SearchLake(lakeSearchers);
            int stonesMined = VeinYield(minersYield);
            resources.AddSpiritStones(stonesMined);
            resources.AddOres(Math.Min(minersYield.Count, VeinSlots) * ctx.Content.Balance.Upkeep.OresPerVeinMiner); // the veins' ores
            Teach(members);
            return new YearlyTaskReport { StonesMined = stonesMined, Patrols = patrols, QiPortionsGathered = qiGathered };
        }

        /// <summary>
        /// A year of harvesting spiritual Qi in wisps (LORE.md §2.5) with a method adapted to it: the Qi the
        /// harvester is sent for when the clan knows a method of it (2026-10-01), else the Qi of the harvester's
        /// own method, otherwise the best Qi among the clan's known methods. A vanished Qi cannot be harvested,
        /// a ubiquitous one needs no harvest. Returns the portions condensed.
        /// </summary>
        private int GatherQi(CharacterData harvester)
        {
            var qi = SentFor(harvester)
                ?? Harvestable(techniques.MethodOf(harvester))
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

        private QiDefinition SentFor(CharacterData harvester) =>
            harvester.HarvestQiId == null ? null
                : techniques.Known.Where(t => t.RequiredQiId == harvester.HarvestQiId).Select(Harvestable).FirstOrDefault(q => q != null);

        private QiDefinition Harvestable(TechniqueData method)
        {
            var qi = techniques.FindQi(method?.RequiredQiId);
            return qi != null && !qi.Vanished && !qi.Ubiquitous ? qi : null;
        }

        /// <summary>The miners who work the veins fully: a few, more with each level of the Mine building.</summary>
        public int VeinSlots
        {
            get
            {
                var upkeep = ctx.Content.Balance.Upkeep;
                return upkeep.VeinMiners + buildings.GetBuilding(BuildingType.Mine).Level * upkeep.VeinMinersPerMineLevel;
            }
        }

        /// <summary>What these miners would bring in a year (the Forge, the veins and the share beyond them).</summary>
        public int MiningYield(IEnumerable<CharacterData> miners) => VeinYield(miners.Select(MineYield).ToList());

        /// <summary>The best miners work the veins fully; those beyond them yield only a share (2026-09-29).</summary>
        private int VeinYield(List<int> yields)
        {
            double share = ctx.Content.Balance.Upkeep.ExtraMinerShare;
            var ordered = yields.OrderByDescending(y => y).ToList();
            return ordered.Take(VeinSlots).Sum() + ordered.Skip(VeinSlots).Sum(y => (int)Math.Round(y * share));
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
                deduction.AddFragment(element, quality, $"Trouvé par {scholar.FullName}");
                ctx.Log.Info($"[Tasks] {scholar.FullName} finds a Q{quality} {element} fragment while studying.");
            }
            else
            {
                cultivation.GrantXp(scholar, StudyXp);
            }
        }

        /// <summary>
        /// The power the diplomat is sent to (else a random one) warms by the envoy's realm (balance.json « diplomacy »), +2 per
        /// Council Room level; a gate or a sect receives no envoy below its rank (2026-10-01).
        /// </summary>
        private void Diplomacy(CharacterData diplomat)
        {
            var target = factions.GetFactionByName(diplomat.DiplomacyTarget) ?? factions.RandomFaction();
            if (target == null) return;
            var s = ctx.Content.Balance.Diplomacy;
            bool mortal = !SpiritualOrificeRules.CanCultivate(diplomat);
            bool great = target.Kind == FactionKind.Sect || target.Kind == FactionKind.Gate;
            if (great && (mortal || diplomat.Realm < s.GreatPowersReceive)) return; // not received
            int byRealm = s.EnvoyRelationByRealm[Math.Min((int)diplomat.Realm, s.EnvoyRelationByRealm.Length - 1)];
            factions.ChangeRelation(target.ID, (mortal ? s.MortalEnvoyRelation : byRealm) + buildings.CouncilLevel * BuildingSystem.CouncilRelationBonusPerLevel);
        }

        /// <summary>Each teacher takes one cultivating student, lowest realm first: 20 + 10 per teacher realm XP.</summary>
        private void Teach(IReadOnlyList<CharacterData> members)
        {
            var teachers = members.Where(m => m.IsAlive && m.CurrentTask == TaskType.Teaching).ToList();
            var students = members.Where(m => m.IsAlive && m.CurrentTask == TaskType.Cultivation).OrderBy(m => m.Realm).ToList();

            for (int i = 0; i < teachers.Count && i < students.Count; i++)
            {
                int bonus = (int)System.Math.Round((TeachingBaseXp + (int)teachers[i].Realm * TeachingXpPerRealm) * sect.TeachingFactor); // the peaks teach better
                cultivation.GrantXp(students[i], bonus);
                ctx.Log.Info($"[Tasks] {teachers[i].FullName} teaches {students[i].FullName} (+{bonus} XP).");
            }
        }

        /// <summary>A year's scouting (L2c.2): with the balance's chance, one beast of the hunting ground the clan did not know.</summary>
        private void ScoutBeasts(CharacterData scout)
        {
            var knowledge = techniques.Knowledge;
            var unknown = bestiary.In(HuntingGround).Where(b => !knowledge.Knows(World.FactKind.Beast, b.Id)).ToList();
            if (unknown.Count == 0 || !ctx.Rng.Chance(ctx.Content.Balance.Bestiary.ScoutRevealChance)) return;
            var found = ctx.Rng.Pick(unknown);
            knowledge.Reveal(World.FactKind.Beast, found.Id, World.KnowledgeSource.Studied);
            ctx.Log.Info($"[Tasks] {scout.FullName} finds a spirit beast ({found.Realm}, stage {found.Stage}).");
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
