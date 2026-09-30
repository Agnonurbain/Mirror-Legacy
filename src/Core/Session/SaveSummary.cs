using System.IO;
using System.Linq;

namespace MirrorChronicles.Session
{
    /// <summary>What a save holds, told on the title screen: the clan, the year, the generation, the living, the patriarch.</summary>
    public sealed record SaveSummary(string ClanName, int Year, int Generation, int Living, string Patriarch, string Version)
    {
        /// <summary>The summary of a save's text, or null when there is none or it cannot be read (never a crash).</summary>
        public static SaveSummary Read(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var data = SaveSerializer.Deserialize(json);
                var records = (data.HistoricalRecords ?? new System.Collections.Generic.List<Data.CharacterData>()).Where(r => r != null).ToList();
                string patriarch = records.FirstOrDefault(r => r.ID == data.PatriarchID)?.FullName;
                return new SaveSummary(data.ClanName, data.CurrentYear, data.GenerationCount,
                    records.Count(r => r.IsAlive && !r.Departed), patriarch, data.SaveVersion);
            }
            catch (InvalidDataException)
            {
                return null;
            }
        }
    }
}
