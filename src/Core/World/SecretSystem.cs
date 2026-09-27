using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The mirror's secret and those who carry it (L2c.4b; LORE.md §11.5: the mirror must stay hidden, D7). Each year a
    /// member in the secret may talk — more when their mind is unsteady, far less when sworn to secrecy, whose oath then
    /// breaks. A leak reaches the power asking most (the most suspicious), or any: proof against the clan, and clues about
    /// a hidden treasure behind it. The mirror answers (L2c.4c): it blurs a power's memories or plants a false proof. When
    /// a power pieces the secret together, it matters only if one of its elders knows what the mirror is — a handful of
    /// old, very high-level beings (<see cref="MirrorLore"/>, user decision 2026-09-27). An ordinary power can only
    /// suspect a treasure: its suspicion of the clan grows, and it may sell the rumour to the strongest power where
    /// someone knows — the real danger. With a knower the consequences are immediate — an investigator comes — but the
    /// clan has a little time to sow doubt: a power made to doubt cannot act; one that still knows seizes the mirror if
    /// it dares, and otherwise tells only a peer who knows too, never an ordinary power.
    /// </summary>
    public sealed class SecretSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly SuspicionLedger suspicion;
        private readonly OathSystem oaths;
        private readonly MirrorSystem mirror;
        private readonly MirrorLore lore;

        public SecretSystem(GameContext ctx, ClanManager clan, FactionManager factions, SuspicionLedger suspicion, OathSystem oaths,
            MirrorSystem mirror, MirrorLore lore)
        {
            this.lore = lore;
            ctx.Events.OnPowerAbsorbed += (vassal, _) =>
            {
                if (Confrontation?.Faction == vassal) Confrontation = null; // the investigator's master is no more
            };
            this.mirror = mirror;
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.suspicion = suspicion;
            this.oaths = oaths;
        }

        private PlotSettings Settings => ctx.Content.Balance.Plots;

        /// <summary>A power that pierced the secret, awaiting its move; null when none.</summary>
        public Confrontation Confrontation { get; private set; }

        public void RestoreConfrontation(Confrontation saved) => Confrontation = saved;

        /// <summary>The mirror blurs a power's memories: its clues and proof dim (less against a Golden Core).</summary>
        public bool BlurMemories(string faction)
        {
            var power = factions.GetFactionByName(faction);
            if (power == null || !mirror.ConsumePower(Settings.BlurMirrorCost)) return false;
            // an elder's mind holds (today only Golden Core powers can know: mirrorLore lists no lower realm)
            double hold = power.HighestRealm >= CultivationRealm.GoldenCore || lore.Knows(faction) ? Settings.BlurStrongFactor : 1.0;
            suspicion.AddMirrorClues(faction, -(int)(Settings.BlurClues * hold));
            suspicion.AddEvidence(faction, -(int)(Settings.BlurEvidence * hold));
            ctx.Log.Info($"[Secrets] The mirror blurs what {faction} remembers.");
            return true;
        }

        /// <summary>Why the mirror cannot plant this false proof now; null when it can.</summary>
        public string FalseProofRefusal(string faction, string framed)
        {
            if (factions.GetFactionByName(faction) == null || factions.GetFactionByName(framed) == null) return "puissance inconnue";
            if (faction == framed) return "choisissez deux puissances différentes";
            if (mirror.MirrorPower < Settings.FalseProofMirrorCost) return $"il faut {Settings.FalseProofMirrorCost} de puissance du miroir";
            return null;
        }

        /// <summary>The mirror plants a false proof: part of a power's proof becomes its distrust of another.</summary>
        public bool PlantFalseProof(string faction, string framed)
        {
            if (FalseProofRefusal(faction, framed) != null || !mirror.ConsumePower(Settings.FalseProofMirrorCost)) return false;
            int moved = System.Math.Min(Settings.FalseProofAmount, suspicion.Evidence(faction));
            suspicion.AddEvidence(faction, -moved);
            suspicion.AddDistrust(faction, framed, moved);
            ctx.Log.Info($"[Secrets] A false proof turns {faction}'s eyes toward {framed}.");
            ctx.Events.TriggerDeed("planted-false-proof", framed); // a secret of the clan now (2026-09-27)
            return true;
        }

        public void ProcessYear()
        {
            if (factions.Factions.Count == 0) return;
            Leak();
            Confront();
        }

        private void Leak()
        {
            foreach (var keeper in clan.LivingMembers.Where(m => m.KnowsMirrorSecret && m.CaptorFaction == null).ToList()) // a captive talks to its captor (L6a)
            {
                var partner = oaths.SecrecyPartner(keeper);
                if (!ctx.Rng.Chance(PlotRules.LeakChance(keeper, partner != null, ctx.Content))) continue;

                int most = factions.Factions.Max(f => suspicion.OfClan(f.Name));
                var askers = factions.Factions.Where(f => suspicion.OfClan(f.Name) == most).ToList(); // all when nobody asks
                var listener = ctx.Rng.Pick(askers);
                suspicion.AddMirrorClues(listener.Name, Settings.LeakMirrorClue);
                suspicion.AddEvidence(listener.Name, Settings.LeakEvidence);
                ctx.Log.Warning($"[Secrets] {keeper.FullName} lets something slip before {listener.Name}.");
                if (partner != null) oaths.Transgress(keeper, partner, OathAct.RevealSecret); // the oath breaks
            }
        }

        /// <summary>The pierced secret: an investigator comes; a year later, doubt, seizure or sale.</summary>
        private void Confront()
        {
            if (Confrontation == null)
            {
                var complete = factions.Factions.Where(f => suspicion.MirrorClues(f.Name) >= SuspicionLedger.Max).ToList();
                var knowers = complete.Where(f => lore.Knows(f.Name)).ToList();
                var knowing = knowers.OrderByDescending(f => f.PowerLevel).FirstOrDefault(); // the others wait their turn, clues kept
                foreach (var ordinary in complete.Except(knowers)) SuspectATreasure(ordinary);
                if (knowing == null) return;
                Confrontation = new Confrontation(knowing.Name, Settings.ConfrontationYears);
                suspicion.AddToClan(knowing.Name, SuspicionLedger.Max); // the consequences are immediate
                ctx.Log.Warning($"[Secrets] {knowing.Name} has pieced the secret together: an investigator comes.");
                return;
            }

            int left = Confrontation.YearsLeft - 1;
            string faction = Confrontation.Faction;
            if (suspicion.MirrorClues(faction) < Settings.DoubtClues)
            {
                Confrontation = null; // made to doubt, it cannot act
                ctx.Log.Info($"[Secrets] {faction} doubts what it thought it knew.");
                return;
            }
            if (left > 0)
            {
                Confrontation = Confrontation with { YearsLeft = left };
                return;
            }

            Confrontation = null;
            var power = factions.GetFactionByName(faction);
            var strongest = clan.LivingMembers.Select(m => m.Realm).DefaultIfEmpty(CultivationRealm.Embryonic).Max();
            if (power != null && PlotRules.DaresWithoutProof(power, strongest, ctx.Content))
            {
                ctx.Events.TriggerMirrorSeized(faction);
                return;
            }
            suspicion.AddMirrorClues(faction, Settings.DoubtClues - 1 - suspicion.MirrorClues(faction)); // it moves on
            var peer = StrongestKnower(except: faction);
            if (peer == null) return;
            suspicion.AddMirrorClues(peer.Name, Settings.LeakMirrorClue); // too weak to seize: it tells only a peer who knows
            ctx.Log.Warning($"[Secrets] {faction}, too weak to act, tells {peer.Name}, whose elder knows too.");
        }

        /// <summary>
        /// An ordinary power at the end of its suspicion cannot name the mirror: it suspects a treasure, and may sell the
        /// rumour to the strongest power where someone knows (profit, D7) — never to another ordinary power.
        /// </summary>
        private void SuspectATreasure(FactionData power)
        {
            var s = ctx.Content.Balance.MirrorLore;
            suspicion.AddToClan(power.Name, s.TreasureSuspicion);
            suspicion.AddMirrorClues(power.Name, Settings.DoubtClues - 1 - suspicion.MirrorClues(power.Name)); // all it could tell itself
            ctx.Log.Info($"[Secrets] {power.Name} is sure the clan hides a treasure, without knowing what.");
            if (!ctx.Rng.Chance(s.RumourRiseChance)) return;
            var buyer = StrongestKnower(except: power.Name);
            if (buyer == null) return;
            suspicion.AddMirrorClues(buyer.Name, Settings.LeakMirrorClue);
            ctx.Log.Warning($"[Secrets] {power.Name} sells the rumour of a treasure to {buyer.Name}.");
        }

        private FactionData StrongestKnower(string except) =>
            factions.Factions.Where(f => f.Name != except && lore.Knows(f.Name)).OrderByDescending(f => f.PowerLevel).FirstOrDefault();
    }
}
