using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>
    /// The clan's treaties with the powers (LORE.md D7: an alliance is a contract of profit, never a guarantee; user
    /// decision 2026-09-27). Non-aggression (no ambush, no strike without proof), trade (stones each year, cheaper
    /// knowledge), defence (an ally may foil an ambush — and draws closer to the secret), vassalage both ways (the vassal
    /// pays tribute; a suzerain protects the clan but tightens its grip until it takes stones and the clan's best art).
    /// A treaty may be secret, or sealed by the patriarch's Dao oath. Each year a power may betray — more when it suspects
    /// the clan or towers over it, far less when sworn; the others see a public betrayal and distrust the betrayer in
    /// silence; a secret treaty may come to light, and everyone then doubts both. The clan may break its word too.
    /// Each action answers with its refusal, or null when done.
    /// </summary>
    public sealed class TreatySystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly FactionManager factions;
        private readonly SuspicionLedger suspicion;
        private readonly TechniqueLibrary techniques;
        private readonly List<Treaty> treaties = new List<Treaty>();

        public TreatySystem(GameContext ctx, ClanManager clan, ResourceManager resources, FactionManager factions, SuspicionLedger suspicion,
            TechniqueLibrary techniques)
        {
            this.techniques = techniques;
            ctx.Events.OnPowerAbsorbed += (vassal, suzerain) => Dissolve(vassal, suzerain);
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.factions = factions;
            this.suspicion = suspicion;
        }

        private TreatySettings Settings => ctx.Content.Balance.Treaties;

        public IReadOnlyList<Treaty> All => treaties;

        public IReadOnlyList<Treaty> With(string faction) => treaties.Where(t => t.Faction == faction).ToList();

        public bool Has(string faction, TreatyKind kind) => treaties.Any(t => t.Faction == faction && t.Kind == kind);

        /// <summary>Whether a power protects the clan from others: a defensive ally, or the clan's suzerain.</summary>
        public IReadOnlyList<string> Guardians(string against) =>
            treaties.Where(t => t.Faction != against && (t.Kind == TreatyKind.Defence || (t.Kind == TreatyKind.Vassalage && !t.ClanIsSuzerain)))
                .Select(t => t.Faction).ToList();

        /// <summary>Whether a power has bound itself not to strike the clan: non-aggression, or its suzerainty over it.</summary>
        public bool Spares(string faction) =>
            Has(faction, TreatyKind.NonAggression) || treaties.Any(t => t.Faction == faction && t.Kind == TreatyKind.Vassalage && !t.ClanIsSuzerain);

        public void RestoreTreaties(IEnumerable<Treaty> saved)
        {
            treaties.Clear();
            if (saved != null) treaties.AddRange(saved);
        }

        public CultivationRealm ClanStrongest =>
            clan.LivingMembers.Where(m => m.CaptorFaction == null).Select(m => m.Realm).DefaultIfEmpty(CultivationRealm.Embryonic).Max();

        public string Propose(string faction, TreatyKind kind, bool secret = false, bool sealedByOath = false, bool clanAsSuzerain = false,
            int? years = null)
        {
            var power = factions.GetFactionByName(faction);
            string refusal = Refusal(power, kind, sealedByOath, clanAsSuzerain);
            if (refusal != null) return refusal;
            int year = ctx.Clock.Year;
            treaties.Add(new Treaty($"treaty-{power.ID}-{kind}-{year}", kind, faction, year, years.HasValue ? year + years : null, secret,
                sealedByOath, kind == TreatyKind.Vassalage && clanAsSuzerain));
            ctx.Log.Info($"[Treaties] The clan and {faction} conclude a {(secret ? "secret " : "")}{kind} treaty.");
            return null;
        }

        /// <summary>Why a power will not conclude this treaty now; null when it will.</summary>
        public string Refusal(FactionData power, TreatyKind kind, bool sealedByOath = false, bool clanAsSuzerain = false)
        {
            var s = Settings;
            if (power == null) return "puissance inconnue";
            if (Has(power.Name, kind)) return "un traité de ce genre les lie déjà";
            var strongest = ClanStrongest;
            if (kind == TreatyKind.Vassalage && !clanAsSuzerain)
            {
                if (treaties.Any(t => t.Kind == TreatyKind.Vassalage && !t.ClanIsSuzerain)) return "le clan sert déjà un suzerain";
                if ((int)power.HighestRealm < (int)strongest + s.SuzerainRealmMargin) return "elle est trop faible pour protéger le clan";
            }
            if (kind == TreatyKind.Vassalage && clanAsSuzerain && (int)strongest < (int)power.HighestRealm + s.SuzerainRealmMargin)
                return "le clan n'a pas l'ascendant sur elle";
            if (sealedByOath && clan.GetPatriarch() == null) return "il faut un patriarche pour jurer";
            if (sealedByOath && clan.GetPatriarch().CaptorFaction != null) return "le patriarche est captif : il ne peut jurer";
            int min = s.MinRelation.TryGetValue(kind, out var m) ? m : 0;
            if (power.RelationWithPlayer < min) return $"la relation est trop froide ({min} requise)";
            int willing = TreatyRules.Willingness(power, kind, clanAsSuzerain, strongest, suspicion.OfClan(power.Name), s);
            return willing < s.AcceptThreshold ? "elle n'y trouve pas son compte" : null;
        }

        /// <summary>
        /// The clan breaks its word: the relation, the clan's name when it was public, a Heart Demon when sworn — whatever the
        /// kind, a vassalage too (the suzerain's reprisal, if any, is its own affair: its schemes and strikes).
        /// </summary>
        public string Break(string treatyId)
        {
            var treaty = treaties.FirstOrDefault(t => t.Id == treatyId);
            if (treaty == null) return "traité inconnu";
            treaties.Remove(treaty);
            ChangeRelation(treaty.Faction, Settings.BreakRelation);
            if (!treaty.Secret)
                foreach (var other in factions.Factions.Where(f => f.Name != treaty.Faction))
                    suspicion.AddToClan(other.Name, Settings.BreakReputation); // the clan's word is worth less
            var patriarch = clan.GetPatriarch();
            if (treaty.Sealed && patriarch != null)
                patriarch.HeartDemonYearsLeft = Math.Max(patriarch.HeartDemonYearsLeft, Settings.SealedHeartDemonYears); // the oath breaks
            ctx.Log.Warning($"[Treaties] The clan breaks its {treaty.Kind} treaty with {treaty.Faction}.");
            return null;
        }

        /// <summary>A power absorbed by another: its treaties with the clan end at once, without blame.</summary>
        private void Dissolve(string vassal, string suzerain)
        {
            if (treaties.RemoveAll(t => t.Faction == vassal) > 0)
                ctx.Log.Warning($"[Treaties] {vassal} is absorbed by {suzerain}: its treaties with the clan end.");
        }

        public void ProcessYear()
        {
            foreach (var treaty in treaties.ToList())
            {
                var power = factions.GetFactionByName(treaty.Faction);
                if (power == null || (treaty.EndYear.HasValue && ctx.Clock.Year >= treaty.EndYear))
                {
                    treaties.Remove(treaty); // its power is gone, or its term is reached
                    continue;
                }
                var current = Apply(treaty, power);
                if (ctx.Rng.Chance(TreatyRules.BetrayalChance(power, current, suspicion.OfClan(power.Name), ClanStrongest, Settings)))
                {
                    Betray(current, power);
                    continue;
                }
                if (current.Secret && ctx.Rng.Chance(Settings.SecretDiscoveryChance)) ComeToLight(current, power);
            }
        }

        /// <summary>What a treaty does each year; returns it as it stands after.</summary>
        private Treaty Apply(Treaty treaty, FactionData power)
        {
            var s = Settings;
            switch (treaty.Kind)
            {
                case TreatyKind.Trade:
                    resources.AddSpiritStones(s.TradeIncome);
                    return treaty;
                case TreatyKind.Defence:
                    suspicion.AddMirrorClues(power.Name, s.AllyProximityClues); // an ally comes close
                    return treaty;
                case TreatyKind.Vassalage when treaty.ClanIsSuzerain:
                    int owed = (int)(Math.Max(0, power.Wealth) * s.VassalTributeShare);
                    power.Wealth = Math.Max(0, power.Wealth - owed);
                    resources.AddSpiritStones(owed);
                    return treaty;
                case TreatyKind.Vassalage:
                    return Serve(treaty, power);
                default:
                    return treaty;
            }
        }

        /// <summary>The clan as vassal: its tribute, the suzerain's closeness, and its grip — at the threshold, stones and the best art.</summary>
        private Treaty Serve(Treaty treaty, FactionData suzerain)
        {
            var s = Settings;
            int tribute = (int)(resources.SpiritStones * s.VassalTributeShare);
            resources.ConsumeSpiritStones(tribute);
            suzerain.Wealth += tribute;
            suspicion.AddMirrorClues(suzerain.Name, s.AllyProximityClues);
            int grip = treaty.Grip + s.GripPerYear;
            int absorptions = treaty.Absorptions;
            if (grip >= s.GripThreshold)
            {
                int taken = (int)(resources.SpiritStones * s.GripStonesShare);
                resources.ConsumeSpiritStones(taken);
                suzerain.Wealth += taken;
                var art = techniques.Known.Where(t => !suzerain.Techniques.Contains(t.ID))
                    .OrderByDescending(t => t.Grade).ThenBy(t => t.ID, StringComparer.Ordinal).FirstOrDefault();
                if (art != null) suzerain.Techniques.Add(art.ID);
                ctx.Log.Warning($"[Treaties] {suzerain.Name} tightens its grip: it takes {taken} stones{(art != null ? $" and « {art.Name} »" : "")}.");
                grip = 0;
                absorptions++;
                if (absorptions >= ctx.Content.Balance.Politics.ClanAbsorptionSteps) ctx.Events.TriggerClanAbsorbed(suzerain.Name); // no longer a clan of its own
            }
            var updated = treaty with { Grip = grip, Absorptions = absorptions };
            treaties[IndexOf(treaty)] = updated;
            return updated;
        }

        /// <summary>
        /// A power betrays: the treaty ends, the relation sours; what it seizes; the witnesses of a public treaty distrust it.
        /// A sworn power betrays far less often (<see cref="TreatyRules.BetrayalChance"/>) but pays no Heart Demon of its own:
        /// a power is no single cultivator whose path the oath could interrupt (the clan's patriarch is).
        /// </summary>
        private void Betray(Treaty treaty, FactionData power)
        {
            var s = Settings;
            treaties.RemoveAt(IndexOf(treaty));
            ChangeRelation(power.Name, s.BetrayalRelation);
            bool seizes = treaty.Kind != TreatyKind.Defence && !(treaty.Kind == TreatyKind.Vassalage && treaty.ClanIsSuzerain);
            if (seizes)
            {
                int seized = (int)(resources.SpiritStones * s.BetrayalStonesShare);
                resources.ConsumeSpiritStones(seized);
                power.Wealth += seized;
            }
            if (!treaty.Secret)
                foreach (var other in factions.Factions.Where(f => f.Name != power.Name))
                    suspicion.AddDistrust(other.Name, power.Name, s.BetrayalWitnessDistrust);
            ctx.Log.Warning($"[Treaties] {power.Name} betrays its {treaty.Kind} treaty with the clan.");
        }

        /// <summary>A secret treaty comes to light: everyone doubts both sides.</summary>
        private void ComeToLight(Treaty treaty, FactionData power)
        {
            treaties[IndexOf(treaty)] = treaty with { Secret = false };
            foreach (var other in factions.Factions.Where(f => f.Name != power.Name))
            {
                suspicion.AddDistrust(other.Name, power.Name, Settings.SecretDiscoveryDistrust);
                suspicion.AddToClan(other.Name, Settings.SecretDiscoveryDistrust);
            }
            ctx.Log.Warning($"[Treaties] The secret treaty between the clan and {power.Name} comes to light.");
        }

        /// <summary>A treaty's place by its id (unique), never by its value.</summary>
        private int IndexOf(Treaty treaty) => treaties.FindIndex(t => t.Id == treaty.Id);

        private void ChangeRelation(string faction, int amount)
        {
            var power = factions.GetFactionByName(faction);
            if (power != null) factions.ChangeRelation(power.ID, amount);
        }
    }
}
