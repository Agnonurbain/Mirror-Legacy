using System;
using System.Collections.Generic;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// A war's yearly battle fought on the grid (G6; the user's choices of 2026-10-12): the clan picks up to WarMaxFighters of
    /// its free cultivators against the enemy's leader, at its strongest realm, and its soldiers a realm below — a soldier per
    /// ArmyPowerPerSoldier of its power, each ally who came keeping one off the field. Won, the enemy is weakened and looted;
    /// lost, the war's toll in stones, and each fallen fighter may die; left, a smaller toll. Several wars' battles wait in
    /// turn. The clan may also let chance settle a battle, as before. Each action answers with its refusal, or null when done.
    /// </summary>
    public sealed class WarBattles
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly WarSystem wars;
        private readonly TechniqueLibrary techniques;
        private readonly WoundSystem wounds;
        private readonly List<PendingWarBattle> queue = new List<PendingWarBattle>();

        public WarBattles(GameContext ctx, ClanManager clan, FactionManager factions, WarSystem wars, TechniqueLibrary techniques, WoundSystem wounds)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.wars = wars;
            this.techniques = techniques;
            this.wounds = wounds;
        }

        /// <summary>The battles waiting, the first answered first.</summary>
        public IReadOnlyList<PendingWarBattle> Queue => queue;

        public PendingWarBattle Pending => queue.Count > 0 ? queue[0] : null;

        /// <summary>The battle under way, until it is concluded; never saved (it is fought on its screen).</summary>
        public Battle Current { get; private set; }

        /// <summary>The clan's alchemy (set by the session): its fighters draw on its Autumn Convergence Pills.</summary>
        public AlchemySystem Alchemy { get; set; }

        public void Restore(IEnumerable<PendingWarBattle> saved)
        {
            queue.Clear();
            if (saved != null) queue.AddRange(saved);
        }

        /// <summary>The year's battle waits for the clan; false when it has nobody who can fight (chance settles it then).</summary>
        public bool Spring(ClanWar war, FactionData enemy, int alliesCame) => throw new NotImplementedException();

        /// <summary>Why this member cannot fight, or null when it can.</summary>
        public string FighterRefusal(CharacterData member) => FighterRules.Refusal(member);

        /// <summary>The enemy on the field: its leader, then the soldiers its allies did not keep away (drawn from the seed).</summary>
        public IReadOnlyList<CharacterData> Army(PendingWarBattle battle) => throw new NotImplementedException();

        /// <summary>The clan answers with these fighters: the battle opens on the grid.</summary>
        public string Fight(IReadOnlyList<string> fighterIds) => throw new NotImplementedException();

        /// <summary>The clan lets chance settle the battle, as before.</summary>
        public string LetBe() => throw new NotImplementedException();

        /// <summary>Once the battle is over: the enemy weakened and looted, or the war's toll and the fallen; the wounded.</summary>
        public string Conclude() => throw new NotImplementedException();
    }
}
