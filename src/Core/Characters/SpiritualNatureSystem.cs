using System;
using System.Collections.Generic;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The elect of the Spiritual Nature (R10; LORE.md §3.1; the user's choices of 2026-10-12): among the children just found
    /// to bear an orifice, one in a thousand is elected — its destiny bound to a Fruition of its element, a free one first —
    /// and that Fruition's virtue forbids it some tasks. See <see cref="SpiritualNatureSettings"/>.
    /// </summary>
    public sealed class SpiritualNatureSystem
    {
        private readonly GameContext ctx;
        private readonly FruitionRegistry fruitions;

        public SpiritualNatureSystem(GameContext ctx, FruitionRegistry fruitions)
        {
            this.ctx = ctx;
            this.fruitions = fruitions;
        }

        /// <summary>The members just found to bear an orifice: each may be elected, one in a thousand.</summary>
        public void Consider(IEnumerable<CharacterData> examined) { }

        /// <summary>Elects the member; false when no Fruition of its element exists, or it already walks another way.</summary>
        public bool Elect(CharacterData member) => throw new NotImplementedException();
    }
}
