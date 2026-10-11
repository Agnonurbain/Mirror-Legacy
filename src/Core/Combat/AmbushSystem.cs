using System;
using System.Collections.Generic;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// An ambush fought on the grid (G6; the user's choices of 2026-10-11): when a power's ambush is not foiled, the members
    /// away from the domain with the one it struck — the free cultivators among them — face its agents, a realm or so below
    /// the power's strongest and never below the member struck. Beaten off, one agent is taken; beaten, the member struck is
    /// taken for ransom; fled, they escape with their wounds; nobody dies, the agents want a hostage. The clan may also let
    /// chance settle it, as before the battles. Each action answers with its refusal, or null when done.
    /// </summary>
    public sealed class AmbushSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly SchemeSystem schemes;
        private readonly CaptiveSystem captives;
        private readonly TechniqueLibrary techniques;
        private readonly WoundSystem wounds;

        public AmbushSystem(GameContext ctx, ClanManager clan, FactionManager factions, SchemeSystem schemes, CaptiveSystem captives,
            TechniqueLibrary techniques, WoundSystem wounds)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.schemes = schemes;
            this.captives = captives;
            this.techniques = techniques;
            this.wounds = wounds;
        }

        public Ambush Pending { get; private set; }

        /// <summary>The battle under way, until it is concluded; never saved (it is fought on its screen).</summary>
        public Battle Current { get; private set; }

        public void Restore(Ambush pending) => Pending = pending;

        /// <summary>The ambush waits for the clan; false when the member struck cannot fight (chance settles it then).</summary>
        public bool Spring(FactionData power, CharacterData target) => throw new NotImplementedException();

        /// <summary>The member struck first, then the others away who can fight, at most the settings' number.</summary>
        public IReadOnlyList<CharacterData> Fighters(Ambush ambush) => throw new NotImplementedException();

        /// <summary>The agents of an ambush, the same each time (drawn from its seed).</summary>
        public IReadOnlyList<CharacterData> Agents(Ambush ambush) => throw new NotImplementedException();

        /// <summary>The members away fight: the battle opens on the grid.</summary>
        public string Fight() => throw new NotImplementedException();

        /// <summary>The clan lets chance settle the ambush, as before the battles.</summary>
        public string LetBe() => throw new NotImplementedException();

        /// <summary>Once the battle is over: an agent taken, the member taken, or a flight; the wounds, and the chronicle.</summary>
        public string Conclude() => throw new NotImplementedException();
    }
}
