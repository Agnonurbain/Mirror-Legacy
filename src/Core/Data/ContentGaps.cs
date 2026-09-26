using System.Collections.Generic;
using System.Linq;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// What the lore and the wiki leave open (user request, 2026-09-26): interpreted items and fields, abilities
    /// the world has not revealed, Qi of unknown family. When a source speaks, the matching data is replaced —
    /// no code change. <c>./Scripts/dev.sh gaps</c> prints this report.
    /// </summary>
    public static class ContentGaps
    {
        public static IReadOnlyList<string> Report(GameContent content)
        {
            var lines = new List<string>();

            foreach (var t in content.Techniques)
                Describe(lines, GameContentLoader.TechniquesFile, t.ID, t.Provenance, t.InterpretedFields);

            foreach (var q in content.Qi)
            {
                Describe(lines, GameContentLoader.QiFile, q.Id, q.Provenance, q.InterpretedFields);
                if (q.Family == QiFamily.Unknown) lines.Add($"{GameContentLoader.QiFile} · {q.Id} · family unknown");
            }

            foreach (var f in content.Fruitions)
            {
                Describe(lines, GameContentLoader.FruitionsFile, f.Id, f.Provenance, f.InterpretedFields);
                foreach (var a in f.Abilities)
                {
                    string id = $"{f.Id}:{a.Id}";
                    Describe(lines, GameContentLoader.FruitionsFile, id, a.Provenance, a.InterpretedFields);
                    if (a.Name == null) lines.Add($"{GameContentLoader.FruitionsFile} · {id} · not revealed by the lore");
                    else if (a.Types.Count == 0) lines.Add($"{GameContentLoader.FruitionsFile} · {id} · type unknown");
                }
            }
            return lines;
        }

        private static void Describe(List<string> lines, string file, string id, Provenance provenance, IEnumerable<string> fields)
        {
            if (provenance == Provenance.Interpretation) lines.Add($"{file} · {id} · interpretation");
            var interpreted = (fields ?? Enumerable.Empty<string>()).ToList();
            if (interpreted.Count > 0) lines.Add($"{file} · {id} · interpreted: {string.Join(", ", interpreted)}");
        }
    }
}
