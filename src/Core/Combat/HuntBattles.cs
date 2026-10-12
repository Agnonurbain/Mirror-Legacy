using System;
using System.Collections.Generic;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// A hunt's capture fought on the grid (G6; the user's choices of 2026-10-12): once the team got close unseen, its strikers
    /// alone face the beast, weakened by the lures and the timing (by night it is livelier). Won, the beast is taken; lost, it
    /// breaks free, the strikers shaken, and one far weaker than it may die; fled, it breaks free. The clan may also let chance
    /// settle the capture, as before the battles. Each action answers with its refusal, or null when done.
    /// </summary>
    public sealed class HuntBattles
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly HuntOperations hunts;
        private readonly TechniqueLibrary techniques;
        private readonly WoundSystem wounds;

        public HuntBattles(GameContext ctx, ClanManager clan, HuntOperations hunts, TechniqueLibrary techniques, WoundSystem wounds)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.hunts = hunts;
            this.techniques = techniques;
            this.wounds = wounds;
        }

        public PendingHunt Pending { get; private set; }

        /// <summary>The battle under way, until it is concluded; never saved (it is fought on its screen).</summary>
        public Battle Current { get; private set; }

        /// <summary>The clan's alchemy (set by the session): its strikers draw on its Autumn Convergence Pills.</summary>
        public AlchemySystem Alchemy { get; set; }

        public void Restore(PendingHunt pending) => Pending = pending;

        /// <summary>The capture waits for the strikers; false when none of them can fight (chance settles it then).</summary>
        public bool Spring(HuntPlan plan, WorldBeast beast) => throw new NotImplementedException();

        /// <summary>The plan's strikers who can fight.</summary>
        public IReadOnlyList<CharacterData> Strikers(PendingHunt hunt) => throw new NotImplementedException();

        /// <summary>The beast as it stands on the grid: its species' name, realm and stage.</summary>
        public CharacterData Beast(PendingHunt hunt) => throw new NotImplementedException();

        /// <summary>The share of its vitality the beast has lost to the lures and the timing when the battle opens.</summary>
        public double Weakening(HuntPlan plan) => throw new NotImplementedException();

        /// <summary>The strikers fight: the battle opens on the grid, the beast weakened.</summary>
        public string Fight() => throw new NotImplementedException();

        /// <summary>The clan lets chance settle the capture, as before the battles.</summary>
        public string LetBe() => throw new NotImplementedException();

        /// <summary>Once the battle is over: the beast taken, or broken free; the fallen and the wounded.</summary>
        public string Conclude() => throw new NotImplementedException();
    }
}
