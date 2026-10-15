using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Heart Demons born of trauma (AUDIT_LORE.md §7.14; the user's choice of 2026-10-15), not only of a broken oath:
    /// mourning a spouse, a parent or a child; captivity; a grave defeat — struck down in a lost battle, a failed breakthrough
    /// that wounds the Dao; a betrayal — an ally's (the patriarch who sealed the treaty), a spouse unmasked as a spy. Each
    /// may breed a Heart Demon in a member who cultivates (🔎 chances and years in balance.json « oaths »).
    /// </summary>
    public sealed class TraumaSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;

        private OathSettings Settings => ctx.Content.Balance.Oaths;

        public TraumaSystem(GameContext ctx, ClanManager clan)
        {
            this.ctx = ctx;
            this.clan = clan;
            ctx.Events.OnCharacterDied += (dead, _) => { foreach (var kin in Kin(dead)) Suffer(kin, Trauma.Mourning); };
            ctx.Events.OnMemberCaptured += (captive, _) => Suffer(captive, Trauma.Captivity);
            ctx.Events.OnClanBattleLost += downed => { foreach (var m in downed) Suffer(m, Trauma.Defeat); };
            ctx.Events.OnTrialWoundedDao += m => Suffer(m, Trauma.WoundedTrial);
            ctx.Events.OnTreatyBetrayed += _ => Suffer(clan.GetPatriarch(), Trauma.Betrayal);
            ctx.Events.OnMemberUnmasked += spy => Suffer(SpouseOf(spy), Trauma.Betrayal);
        }

        /// <summary>The living of the clan who mourn the dead: its spouse, its parents, its children.</summary>
        private IEnumerable<CharacterData> Kin(CharacterData dead) =>
            clan.LivingMembers.Where(m => m != dead && (m.ID == dead.SpouseID || m.SpouseID == dead.ID
                || m.ID == dead.FatherID || m.ID == dead.MotherID || m.FatherID == dead.ID || m.MotherID == dead.ID)).ToList();

        private CharacterData SpouseOf(CharacterData member) =>
            clan.LivingMembers.FirstOrDefault(m => m != member && (m.ID == member.SpouseID || m.SpouseID == member.ID));

        /// <summary>The trauma may breed a Heart Demon in a living member who cultivates.</summary>
        private void Suffer(CharacterData member, Trauma trauma)
        {
            if (member == null || !member.IsAlive || !SpiritualOrificeRules.Cultivates(member)) return;
            double chance = Settings.TraumaChances != null && Settings.TraumaChances.TryGetValue(trauma, out var c) ? c : 0;
            if (!ctx.Rng.Chance(chance)) return;

            member.HeartDemonYearsLeft = Math.Max(member.HeartDemonYearsLeft, Settings.TraumaHeartDemonYears);
            ctx.Log.Info($"[Trauma] {member.FullName} bears a Heart Demon ({trauma}).");
            ctx.Events.TriggerTraumaHeartDemon(member, trauma);
        }
    }
}
