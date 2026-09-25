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

        public IReadOnlyList<CharacterData> Records => records;

        public void Register(CharacterData member)
        {
            if (!records.Contains(member))
                records.Add(member);
        }

        public CharacterData FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return records.Find(c => c.ID == id);
        }

        public List<CharacterData> ChildrenOf(string parentId)
        {
            return records.FindAll(c => c.FatherID == parentId || c.MotherID == parentId);
        }

        public void Clear() => records.Clear();
    }
}
