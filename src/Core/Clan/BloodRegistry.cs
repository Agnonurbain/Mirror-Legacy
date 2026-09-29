using System.Collections.Generic;
using MirrorChronicles.Data;

namespace MirrorChronicles.Clan
{
    /// <summary>
    /// Every member who ever belonged to the clan, living or dead: the family tree and the Annals.
    /// </summary>
    public sealed class BloodRegistry
    {
        private readonly List<CharacterData> records = new List<CharacterData>();
        private readonly HashSet<CharacterData> registered = new HashSet<CharacterData>();
        private readonly Dictionary<string, CharacterData> byId = new Dictionary<string, CharacterData>(); // kinship walks it often

        public IReadOnlyList<CharacterData> Records => records;

        public void Register(CharacterData member)
        {
            if (!registered.Add(member)) return;
            records.Add(member);
            if (!string.IsNullOrEmpty(member.ID)) byId[member.ID] = member;
        }

        /// <summary>By the index, and else by a search that indexes what it finds (an ID given after joining).</summary>
        public CharacterData FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (byId.TryGetValue(id, out var known) && known.ID == id) return known;
            var found = records.Find(c => c.ID == id);
            if (found != null) byId[id] = found;
            return found;
        }

        public List<CharacterData> ChildrenOf(string parentId)
        {
            return records.FindAll(c => c.FatherID == parentId || c.MotherID == parentId);
        }

        public void Clear()
        {
            records.Clear();
            registered.Clear();
            byId.Clear();
        }
    }
}
