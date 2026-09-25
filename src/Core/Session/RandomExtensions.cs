using System;
using System.Collections.Generic;
using MirrorChronicles.Data;

namespace MirrorChronicles.Session
{
    /// <summary>Rolls shared by the systems, all drawn from the session's seedable random source.</summary>
    public static class RandomExtensions
    {
        private static readonly int ElementCount = Enum.GetValues(typeof(Element)).Length;

        /// <summary>True with the given probability (0-1).</summary>
        public static bool Chance(this Random rng, double probability) => rng.NextDouble() < probability;

        /// <summary>Any element but <see cref="Element.None"/>.</summary>
        public static Element NextElement(this Random rng) => (Element)rng.Next(1, ElementCount);

        /// <summary>A random item, or the default value when the list is empty.</summary>
        public static T Pick<T>(this Random rng, IReadOnlyList<T> items) =>
            items.Count == 0 ? default : items[rng.Next(items.Count)];
    }
}
