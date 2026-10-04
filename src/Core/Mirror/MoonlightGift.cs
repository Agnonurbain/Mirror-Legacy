using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Mirror
{
    /// <summary>
    /// The Supreme Yin Moonlight given to the clan (📚 « sealed and stored by the Li Family for specialized cultivation
    /// needs »; LORE.md §11.5; AUDIT_LORE.md §3.7, the user's decision 2026-10-04): a portion of a Supreme Yin Qi sealed for
    /// the clan, or a member's cultivation nourished. So rare a Qi does not go unseen: each power with a spy in the clan
    /// gains clues toward the hidden treasure.
    /// </summary>
    public sealed class MoonlightGift
    {
        private readonly GameContext ctx;
        private readonly MirrorSystem mirror;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly SuspicionLedger suspicion;

        public MoonlightGift(GameContext ctx, MirrorSystem mirror, ClanManager clan, ResourceManager resources, SuspicionLedger suspicion)
        {
            this.ctx = ctx;
            this.mirror = mirror;
            this.clan = clan;
            this.resources = resources;
            this.suspicion = suspicion;
        }

        private MirrorTierSettings Settings => ctx.Content.Balance.MirrorTiers;

        /// <summary>Why the mirror cannot give its Moonlight now (French), or null.</summary>
        public string Refusal() => mirror.PayRefusal(Settings.GiftCost);

        /// <summary>A portion of the Supreme Yin Qi sealed for the clan. Null when done; else why not (French).</summary>
        public string SealQi()
        {
            if (Refusal() is { } refusal) return refusal;
            mirror.ConsumePower(Settings.GiftCost);
            resources.AddQi(Settings.GiftQiId, 1);
            Noticed();
            ctx.Log.Info("[Mirror] The mirror seals a portion of Supreme Yin Moonlight for the clan.");
            return null;
        }

        /// <summary>A member's cultivation nourished by the Moonlight. Null when done; else why not (French).</summary>
        public string Nourish(string memberId)
        {
            var member = clan.FindById(memberId);
            if (member == null || !member.IsAlive || member.CaptorFaction != null) return "ce membre n'est pas au domaine";
            if (!SpiritualOrificeRules.CanCultivate(member)) return "ce membre ne cultive pas";
            if (Refusal() is { } refusal) return refusal;
            mirror.ConsumePower(Settings.GiftCost);
            member.CultivationXP += (int)(PowerLadder.XpForNextStage(member.Realm) * Settings.GiftXpShare);
            Noticed();
            ctx.Log.Info($"[Mirror] The Supreme Yin Moonlight nourishes {member.FullName}'s cultivation.");
            return null;
        }

        /// <summary>The spies in the clan notice so rare a Qi; their power wonders (LORE.md §11.10 M4).</summary>
        private void Noticed()
        {
            foreach (var power in clan.LivingMembers.Where(m => m.SpyFor != null && !m.DoubleAgent).Select(m => m.SpyFor).Distinct().ToList())
                suspicion.AddMirrorClues(power, Settings.GiftClues);
        }
    }
}
