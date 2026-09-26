using System;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>The outcome of one espionage mission.</summary>
    public readonly struct EspionageResult
    {
        public bool Success { get; init; }
        public string TargetFaction { get; init; }
        public int FragmentQuality { get; init; }
        public string StolenTechniqueId { get; init; } // a manual the power held and the clan lacked (L5b)
        public Element FragmentElement { get; init; }
        public int RelationPenalty { get; init; }
        public int StabilityPenalty { get; init; }
        public bool TriggeredCombat { get; init; }
    }

    /// <summary>
    /// Spies steal from rival factions. Success = root × 0.5% − power / 100, between 5% and 60%: sometimes a
    /// manual the power holds and the clan lacks (LORE.md §2.4), otherwise a technique fragment. A caught spy sours
    /// relations, is shaken, and may provoke a fight.
    /// </summary>
    public sealed class EspionageSystem
    {
        public const double MinSuccessChance = 0.05;
        public const double MaxSuccessChance = 0.60;
        public const int CaughtRelationPenalty = -20;
        public const int CaughtStabilityPenalty = -10;
        public const double RetaliationChance = 0.10;

        private readonly GameContext ctx;
        private readonly FactionManager factions;
        private readonly DeductionEngine deduction;
        private readonly MentalStabilitySystem stability;
        private readonly TechniqueLibrary techniques;

        public EspionageSystem(GameContext ctx, FactionManager factions, DeductionEngine deduction, MentalStabilitySystem stability,
            TechniqueLibrary techniques)
        {
            this.techniques = techniques;
            this.ctx = ctx;
            this.factions = factions;
            this.deduction = deduction;
            this.stability = stability;
        }

        public EspionageResult AttemptEspionage(CharacterData spy, FactionData target)
        {
            if (spy == null || target == null || !spy.IsAlive)
                return new EspionageResult();

            double chance = Math.Clamp(spy.SpiritualRoot * 0.005 - target.PowerLevel / 100.0, MinSuccessChance, MaxSuccessChance);
            if (ctx.Rng.Chance(chance))
            {
                var unknown = (target.Techniques ?? new System.Collections.Generic.List<string>()).Where(id => !techniques.Knows(id)).ToList();
                if (unknown.Count > 0 && ctx.Rng.Chance(ctx.Content.Balance.Diplomacy.StealManualChance))
                {
                    string stolen = ctx.Rng.Pick(unknown);
                    techniques.Learn(stolen);
                    ctx.Log.Info($"[Espionage] {spy.FullName} steals the manual « {stolen} » from {target.Name}.");
                    return new EspionageResult { Success = true, TargetFaction = target.Name, StolenTechniqueId = stolen };
                }

                int quality = Math.Clamp(target.PowerLevel / 250, 1, 5);
                var element = ctx.Rng.NextElement();
                deduction.AddFragment(element, quality, $"Stolen from {target.Name} by {spy.FullName}");
                ctx.Log.Info($"[Espionage] {spy.FullName} steals a Q{quality} {element} fragment from {target.Name}.");
                return new EspionageResult { Success = true, TargetFaction = target.Name, FragmentQuality = quality, FragmentElement = element };
            }

            factions.ChangeRelation(target.ID, CaughtRelationPenalty);
            stability.ApplyModifier(spy, CaughtStabilityPenalty);
            bool retaliation = ctx.Rng.Chance(RetaliationChance);
            ctx.Log.Warning($"[Espionage] {spy.FullName} is caught by {target.Name}{(retaliation ? ", who retaliates" : "")}!");

            return new EspionageResult
            {
                TargetFaction = target.Name,
                RelationPenalty = CaughtRelationPenalty,
                StabilityPenalty = CaughtStabilityPenalty,
                TriggeredCombat = retaliation
            };
        }
    }
}
