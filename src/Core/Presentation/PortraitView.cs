using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    public enum AgeBracket { Child, Youth, Adult, Mature, Elder }

    /// <summary>What an ink portrait shows: the age, man or woman, the element tinting the robe, the aura of the realm
    /// (0 without cultivation), faded for the dead and the departed, and a variation of the member's own.</summary>
    public sealed record Portrait(AgeBracket Age, bool IsMale, Element Element, int Aura, bool Faded, int Variation);

    /// <summary>
    /// The ink portraits (Shuimo): the age is the share of life lived — a cultivator of a hundred years out of three
    /// hundred still looks a grown adult — and the variation comes from the member's id, the same every time.
    /// </summary>
    public static class PortraitView
    {
        private const int ChildUntil = 12;
        private const int YouthUntil = 20;
        private const double AdultUntil = 0.45;   // of the lifespan
        private const double MatureUntil = 0.75;

        public static Portrait Of(CharacterData member)
        {
            return new Portrait(AgeOf(member), member.IsMale, member.Affinity,
                SpiritualOrificeRules.CanCultivate(member) && member.OrificeKnown ? (int)member.Realm + 1 : 0,
                !member.IsAlive || member.Departed, Stable(member.ID));
        }

        public static Portrait For(GameSession session, string memberId) =>
            session?.Clan.FindById(memberId) is { } member ? Of(member) : null;

        private static AgeBracket AgeOf(CharacterData member)
        {
            if (member.Age < ChildUntil) return AgeBracket.Child;
            if (member.Age < YouthUntil) return AgeBracket.Youth;
            if (member.MaxLifespan <= 0) return AgeBracket.Adult; // a lifespan unknown: not ancient
            double lived = member.Age / (double)member.MaxLifespan;
            return lived < AdultUntil ? AgeBracket.Adult : lived < MatureUntil ? AgeBracket.Mature : AgeBracket.Elder;
        }

        /// <summary>FNV-1a of the id: stable across runs (string.GetHashCode is not).</summary>
        private static int Stable(string id)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in id ?? "") hash = (hash ^ c) * 16777619;
                return (int)(hash & 0x7fffffff);
            }
        }
    }
}
