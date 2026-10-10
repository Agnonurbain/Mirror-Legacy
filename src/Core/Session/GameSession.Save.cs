using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Combat;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Events;
using MirrorChronicles.Mirror;
using MirrorChronicles.World;

namespace MirrorChronicles.Session
{
    // Saves: a game rebuilt from a save (older saves brought up to date), and the detached snapshot written each year.
    public sealed partial class GameSession
    {
        /// <summary>
        /// A save's text loaded, or null with the reason when it cannot be (unreadable, or read but inconsistent): the
        /// boundary with the player's files, where nothing may crash.
        /// </summary>
        public static GameSession TryLoad(string json, GameSetup setup, out string error)
        {
            error = null;
            try
            {
                var data = SaveSerializer.Deserialize(json);
                if (data.HistoricalRecords?.Any(r => r == null) == true) throw new System.IO.InvalidDataException("a record of the clan is empty");
                return FromSaveData(data, setup);
            }
            catch (Exception e) when (e is System.IO.InvalidDataException || e is ArgumentException || e is InvalidOperationException
                || e is KeyNotFoundException || e is NullReferenceException || e is InvalidCastException || e is FormatException)
            {
                error = e.Message;
                return null;
            }
        }

        /// <summary>The save was written by a version older than this one (an unreadable version counts as older).</summary>
        private static bool SavedBefore(GameData data, string version) =>
            !System.Version.TryParse(data.SaveVersion, out var saved) || saved < System.Version.Parse(version);

        /// <summary>
        /// Rebuilds a game from a save, bringing older saves up to date (stages, orifices, lifespans).
        /// The random stream restarts from the seed, year and phase: reproducible, not a continuation.
        /// </summary>
        public static GameSession FromSaveData(GameData data, GameSetup setup)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var content = RequireContent(setup);

            var rng = new Random(unchecked(data.Seed * 31 + data.CurrentYear * 4 + (int)data.CurrentPhase));
            var session = new GameSession(data.Seed, rng, data.ClanName ?? content.Clan.ClanName, setup);

            // Work on copies: the caller keeps its GameData, and two loads never share objects
            var records = (data.HistoricalRecords ?? new List<CharacterData>()).Select(r => r.Clone()).ToList();
            foreach (var record in records)
            {
                PowerLadder.Normalize(record);            // saves made before realm stages
                SpiritualOrificeRules.Normalize(record);  // saves made before orifices
                PowerLadder.NormalizeLifespan(record);    // keeps Dao wounds and mortal rolls
            }

            session.Clock.Restore(Math.Max(0, data.CurrentYear), data.CurrentPhase);
            session.Clan.Restore(records, data.PatriarchID);
            session.Resources.Restore(data.SpiritStones, data.MedicinalHerbs, data.SpiritualOres, data.Prestige, data.TechniqueFragments);
            session.Mirror.Restore(data.MirrorPower, data.RestoredFragments, data.MirrorAsleepUntil);
            session.Shards.Restore(data.RecoveredShards); // none before 2.21
            session.Shards.RestoreRuins(data.RevealedRuins);
            session.ShardSense.Restore(data.ShardDirections); // none before 2.23
            session.Marks.Restore(data.TechniqueMarks);      // none before 2.36
            session.Arts.Restore(data.ArtLegacies);          // none before 2.37
            session.Alchemy.Restore(data.EssencePills, data.PoisonedPills, data.Pills); // none before 2.38 / 2.39
            session.Karma.Restore(data.GenerationCount, data.TotalBirths, data.TotalDeaths, data.LastPatriarchId ?? session.Clan.PatriarchID);
            if (data.Buildings != null) session.Buildings.Restore(data.Buildings);
            if (data.Factions != null && data.Factions.Count > 0) session.Factions.Restore(data.Factions.Select(f => f.Clone()));
            else session.Factions.InitializeFactions();
            if (data.PoisonedPills == null) // before 2.39 the powers kept no pills: their alchemists' first store
                foreach (var power in session.Factions.Factions) power.EssencePills = session.Context.Content.Balance.Arts.EssencePill.PowerStartPills;
            if (SavedBefore(data, "2.40")) // before 2.40 the powers kept no formation: the one their realm knows, a level below
                foreach (var power in session.Factions.Factions) power.FormationLevel = World.PowerFormation.Start(power, session.Context.Content);
            session.Deduction.Restore((data.Fragments ?? new List<FragmentData>()).Select(f => f.Clone()));
            session.Knowledge.Restore(data.Knowledge);
            session.Techniques.Restore(
                data.Knowledge != null ? null : data.KnownTechniqueIds ?? content.Clan.StartingTechniques, // saves made before the knowledge module
                (data.Techniques ?? new List<TechniqueData>()).Select(t => t.Clone()));
            session.Resources.RestoreQi(data.SpiritualQi ?? content.Clan.StartingQi, data.QiHarvestProgress);
            foreach (var record in records)
            {
                session.Techniques.NormalizeMember(record);                  // a Qi cultivator practises a method
                if (data.Knowledge == null)                                 // saves made before the knowledge module
                {
                    session.Knowledge.Reveal(FactKind.Ability, record.FoundationId, KnowledgeSource.OlderSave);
                    foreach (var ability in record.DivineAbilities ?? new List<string>())
                        session.Knowledge.Reveal(FactKind.Ability, ability, KnowledgeSource.OlderSave);
                }
                if (record.Temperament == Temperament.None)                  // saves made before the Dao Heart
                    record.Temperament = FoundationRules.RandomTemperament(rng);
            }
            session.Oaths.Restore((data.Pacts ?? new List<PactData>()).Select(p => p.Clone()), data.VeiledOathBreakers);
            session.GoldenCore.Restore(data.GoldenCorePermissions); // saves made before L4b have none
            session.Resources.RestorePrayers(data.Prayers);           // saves made before 2.6: none gathered
            session.Resources.RestoreBeasts(data.CapturedBeasts);
            session.Suspicion.Restore(data.SuspicionOfClan, data.Distrust); // hidden; none in saves before 2.9
            session.Suspicion.RestoreEvidence(data.Evidence, data.PublicEvidence); // none public in saves before 2.22
            session.Suspicion.RestoreMirrorClues(data.MirrorClues);
            session.Secrets.RestoreConfrontation(data.Confrontation);
            session.Captives.RestorePrisoners(data.Prisoners); // none in saves before 2.10
            session.Treaties.RestoreTreaties(data.Treaties);   // none in saves before 2.11
            session.Politics.RestoreBonds(data.PowerBonds);    // none in saves before 2.12
            session.Politics.RestoreCoalition(data.Coalition);
            session.Politics.RestoreCalls(data.CallsToArms);
            session.Intrigues.RestoreDemands(data.Demands, data.QuietUntil); // none in saves before 2.13
            session.SecretBook.Restore(data.SecretsHeld, data.SecretProgress);
            if (data.SecretsHeld == null) session.SecretBook.DrawPowerSecrets(SecretBook.WorldRandom(data.Seed), session.Factions.Factions); // before 2.14
            session.PowerShards.Place(PowerShards.WorldRandom(data.Seed)); // saves before 2.21: the powers' shards placed now
            session.Elders.Populate(ElderSystem.WorldRandom(data.Seed));    // saves before 2.24: the powers' elders drawn now
            session.WorldFruitions.Link();
            session.WorldFruitions.RestoreRaces(data.FruitionRaces, data.MovedHolders); // none before 2.25
            session.Ancestors.Restore(data.PendingAncestors); // none before 2.26
            session.Probes.RestoreAlertness(data.Alertness);
            session.Suspicion.RestoreClanDistrust(data.ClanDistrust); // none in saves before 2.15
            session.Dealings.RestoreSpent(data.SpentSecrets);
            session.Patrons.RestorePacts(data.PatronPacts);
            session.Wars.Restore(data.Wars, data.ClanWars); // none in saves before 2.17
            session.Challenges.Restore(data.PendingChallenge); // none in saves before 2.18
            session.Upkeep.Restore(data.Impoverished);          // false in saves before 2.19
            session.DaoHunts.Restore(data.DaoPreys);            // none in saves before 2.20
            if (data.WorldBeasts != null) session.Bestiary.Restore(data.WorldBeasts);
            else session.Bestiary.Draw(BeastRegistry.WorldRandom(data.Seed)); // saved before 2.8: the world's beasts from its seed
            if (data.HuntingGround != null) session.Tasks.SetHuntingGround(data.HuntingGround); // a place gone from the map: home
            session.Talismans.Restore(data.TalismanOffer);
            int period = content.Balance.Talismans.RitualPeriodYears;
            session.Talismans.RestoreCalendar(data.NextRitualYear ?? (data.CurrentYear + period - 1) / period * period);
            session.Fruitions.Restore(data.FruitionStates, FruitionRegistry.WorldRandom(data.Seed)); // older saves: the world their seed draws
            session.Story.Restore(data.TriggeredStoryEvents ?? new List<StoryTriggerType>(), data.PendingStoryEvents ?? new List<StoryTriggerType>());
            session.Victory.Restore(data.GameLost); // a game « won » under the old rule goes on: there is no forced victory now
            session.Annals.Restore(data.Annals, session.Karma.GenerationCount, records); // none before 2.21
            session.Endings.Restore(data.PositionMoves, data.EndingStreaks);
            session.Imperial.Restore(data.KingdomYear, data.SovereignId, data.ImperialMerit); // none before 2.27
            session.Phenomena.Restore(data.Phenomena); // none before 2.28
            session.Dharma.Restore(data.RankDesignations); // none before 2.30
            session.Artifacts.Restore(data.ArtifactArmoury); // none before 2.31
            session.PowerSchemes.Restore(data.PowerCaptives); // none before 2.35
            session.Rebirths.Restore(data.WorldRebirths);   // none before 2.34
            session.Bridges.Restore(data.CorruptedVirtues);  // before 2.32: those of the start
            session.Demons.Restore(data.PendingDemons, data.RavagingDemons, data.Essences, data.UnderworldGrudgeUntil); // none before 2.29
            session.Sect.Restore(data.SectFoundedYear, session.Karma.GenerationCount); // none before 2.21
            session.Absorption.Restore(data.AbsorbedPowers);
            session.Accords.RestoreDebts(data.KnowledgeDebts);
            session.Sponsorships.Restore(data.PendingSponsorOffer, data.Sponsorships);
            session.Rivals.Restore(data.AscentRivals);
            session.Paths.Restore(data.AscentTomb);

            session.Log.Info($"[Session] The {session.Clan.ClanName} clan resumes in year {session.Clock.Year}.");
            return session;
        }

        /// <summary>A detached snapshot: later play never changes it, so it can be kept or written later.</summary>
        public GameData ToSaveData()
        {
            return new GameData
            {
                Seed = Seed,
                ClanName = Clan.ClanName,
                CurrentYear = Clock.Year,
                CurrentPhase = Clock.Phase,
                PatriarchID = Clan.PatriarchID,
                HistoricalRecords = Clan.Registry.Records.Select(r => r.Clone()).ToList(),
                SpiritStones = Resources.SpiritStones,
                MedicinalHerbs = Resources.MedicinalHerbs,
                SpiritualOres = Resources.SpiritualOres,
                Prestige = Resources.Prestige,
                TechniqueFragments = Resources.TechniqueFragments,
                MirrorPower = Mirror.MirrorPower,
                RestoredFragments = Mirror.RestoredFragments,
                RecoveredShards = Shards.Recovered.ToList(),
                RevealedRuins = Shards.RevealedRuins.ToList(),
                ShardDirections = new Dictionary<string, string>(ShardSense.Directions),
                TechniqueMarks = new Dictionary<string, string>(Marks.Marks),
                ArtLegacies = Arts.Legacies.ToList(),
                EssencePills = Alchemy.EssencePills.ToDictionary(p => p.Key, p => p.Value),
                PoisonedPills = Alchemy.Poisoned.ToList(),
                Pills = Alchemy.Pills.ToDictionary(p => p.Key, p => p.Value),
                MirrorAsleepUntil = Mirror.AsleepUntil,
                Fragments = Deduction.Fragments.Select(f => f.Clone()).ToList(),
                Techniques = Techniques.Deduced.Select(t => t.Clone()).ToList(),
                Knowledge = Knowledge.Keys.ToList(), // the known techniques live there since 2.3
                Pacts = Oaths.Pacts.Select(p => p.Clone()).ToList(),
                VeiledOathBreakers = Oaths.Veiled.ToList(),
                SpiritualQi = new Dictionary<string, int>(Resources.SpiritualQi),
                QiHarvestProgress = new Dictionary<string, int>(Resources.QiHarvestProgress),
                FruitionStates = new Dictionary<string, FruitionState>(Fruitions.States),
                FruitionRaces = WorldFruitions.Races.ToList(),
                MovedHolders = WorldFruitions.Moved.ToList(),
                PendingAncestors = Ancestors.Pending.ToList(),
                GoldenCorePermissions = new Dictionary<string, string>(GoldenCore.Permissions),
                Prayers = Resources.Prayers,
                CapturedBeasts = Resources.Beasts.ToList(),
                WorldBeasts = Bestiary.Beasts.ToList(),
                SuspicionOfClan = new Dictionary<string, int>(Suspicion.ClanSuspicions),
                Distrust = new Dictionary<string, int>(Suspicion.Distrusts),
                Evidence = new Dictionary<string, int>(Suspicion.Evidences),
                PublicEvidence = Suspicion.PublicEvidence,
                MirrorClues = new Dictionary<string, int>(Suspicion.AllMirrorClues),
                Confrontation = Secrets.Confrontation,
                Prisoners = Captives.Prisoners.ToList(),
                Treaties = Treaties.All.ToList(),
                PowerBonds = Politics.Bonds.ToList(),
                Coalition = Politics.Coalition == null ? null : Politics.Coalition with { Members = Politics.Coalition.Members.ToList() },
                CallsToArms = Politics.PendingCalls.ToList(),
                Demands = Intrigues.Demands.ToList(),
                QuietUntil = new Dictionary<string, int>(Intrigues.QuietUntil),
                SecretsHeld = SecretBook.All.ToList(),
                SecretProgress = new Dictionary<string, int>(SecretBook.AllProgress),
                Alertness = new Dictionary<string, int>(Probes.AllAlertness),
                ClanDistrust = new Dictionary<string, int>(Suspicion.AllClanDistrust),
                SpentSecrets = Dealings.Spent.ToList(),
                PatronPacts = Patrons.Pacts.ToList(),
                Wars = Wars.Wars.Select(w => w with { SideA = w.SideA.ToList(), SideB = w.SideB.ToList() }).ToList(),
                ClanWars = Wars.ClanWars.ToList(),
                PendingChallenge = Challenges.Pending,
                Impoverished = Upkeep.Impoverished,
                DaoPreys = DaoHunts.Known.ToList(),
                Annals = Annals.Entries.ToList(),
                PositionMoves = new Dictionary<string, string>(Endings.Moves),
                EndingStreaks = new Dictionary<string, int>(Endings.Streaks),
                SectFoundedYear = Sect.FoundedYear,
                KingdomYear = Imperial.KingdomYear,
                SovereignId = Imperial.SovereignId,
                ImperialMerit = Imperial.Merit,
                Phenomena = Phenomena.Active.ToList(),
                PendingDemons = Demons.Pending.ToList(),
                RavagingDemons = Demons.Ravaging.ToList(),
                Essences = Demons.Essences,
                RankDesignations = Dharma.Designations.ToList(),
                ArtifactArmoury = Artifacts.Armoury.ToList(),
                CorruptedVirtues = Bridges.Corrupted.ToList(),
                WorldRebirths = Rebirths.Pending.ToList(),
                PowerCaptives = PowerSchemes.Captives.ToList(),
                UnderworldGrudgeUntil = Demons.GrudgeUntil,
                AbsorbedPowers = Absorption.Absorbed.ToList(),
                KnowledgeDebts = Accords.Debts.ToList(),
                PendingSponsorOffer = Sponsorships.Pending,
                Sponsorships = Sponsorships.Active.ToList(),
                AscentRivals = Rivals.Known.ToList(),
                AscentTomb = Paths.Tomb,
                HuntingGround = Tasks.HuntingGround,
                NextRitualYear = Talismans.NextRitualYear,
                TalismanOffer = Talismans.PendingOffer == null ? null
                    : new TalismanOffer(Talismans.PendingOffer.BeneficiaryId, new List<string>(Talismans.PendingOffer.Choices), Talismans.PendingOffer.Leap),
                GenerationCount = Karma.GenerationCount,
                TotalBirths = Karma.TotalBirths,
                TotalDeaths = Karma.TotalDeaths,
                LastPatriarchId = Karma.LastPatriarchId,
                Buildings = Buildings.Buildings.Select(b => new BuildingData(b.Type) { Level = b.Level }).ToList(),
                Factions = Factions.Factions.Select(f => f.Clone()).ToList(),
                TriggeredStoryEvents = Story.TriggeredEvents.ToList(),
                PendingStoryEvents = Story.PendingTriggers.ToList(),
                GameLost = Victory.GameLost
            };
        }
    }
}
