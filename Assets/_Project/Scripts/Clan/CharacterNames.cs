using System.Collections.Generic;

namespace MirrorChronicles.Clan
{
    /// <summary>
    /// Name pools shared by births (ClanManager) and marriages (MarriageMatchmaker).
    /// </summary>
    public static class CharacterNames
    {
        public static readonly IReadOnlyList<string> Male = new[]
            { "Wei", "Jian", "Long", "Feng", "Hao", "Chen", "Ming", "Shan", "Zhi", "Bo", "Tao", "Jun", "Kai" };

        public static readonly IReadOnlyList<string> Female = new[]
            { "Xue", "Mei", "Lan", "Ying", "Lin", "Yue", "Hua", "Qing", "Zhen", "Rui", "Shu", "Dan" };

        /// <summary>
        /// Family names of wandering cultivators who marry into the clan (never the clan's own name).
        /// </summary>
        public static readonly IReadOnlyList<string> OutsiderFamilies = new[]
            { "Wang", "Zhao", "Chen", "Liu", "Sun", "Zhou", "Wu", "Zheng" };
    }
}
