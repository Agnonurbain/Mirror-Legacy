using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Clan
{
    /// <summary>
    /// What the clan knows (LORE.md §2, §11.6): techniques of the world's catalog it has learned, and those
    /// the mirror deduced, which exist nowhere else. Also who practises which method.
    /// Catalog techniques are shared content: never modify one.
    /// </summary>
    public sealed class TechniqueLibrary
    {
        private readonly GameContext ctx;
        private readonly HashSet<string> knownIds = new HashSet<string>();
        private readonly List<TechniqueData> deduced = new List<TechniqueData>();

        public TechniqueLibrary(GameContext ctx)
        {
            this.ctx = ctx;
        }

        /// <summary>Catalog techniques the clan has learned.</summary>
        public IReadOnlyCollection<string> KnownIds => knownIds;

        /// <summary>Techniques the mirror deduced for the clan (saved whole: they exist nowhere else).</summary>
        public IReadOnlyList<TechniqueData> Deduced => deduced;

        /// <summary>Every technique the clan knows: learned from the catalog, then deduced.</summary>
        public IEnumerable<TechniqueData> Known =>
            ctx.Content.Techniques.Where(t => knownIds.Contains(t.ID)).Concat(deduced);

        public bool Knows(string id) => id != null && (knownIds.Contains(id) || deduced.Any(t => t.ID == id));

        /// <summary>Learns a technique of the world's catalog; false when the catalog lacks it.</summary>
        public bool Learn(string id)
        {
            if (ctx.Content.Techniques.All(t => t.ID != id))
            {
                ctx.Log.Warning($"[Techniques] No technique \"{id}\" exists to be learned.");
                return false;
            }
            knownIds.Add(id);
            return true;
        }

        public void AddDeduced(TechniqueData technique)
        {
            if (technique != null && !Knows(technique.ID)) deduced.Add(technique);
        }

        /// <summary>Any technique by id: the clan's deductions, then the world's catalog (known or not).</summary>
        public TechniqueData Find(string id)
        {
            if (id == null) return null;
            return deduced.FirstOrDefault(t => t.ID == id) ?? ctx.Content.Techniques.FirstOrDefault(t => t.ID == id);
        }

        public QiDefinition FindQi(string id) => id == null ? null : ctx.Content.Qi.FirstOrDefault(q => q.Id == id);

        /// <summary>The method a member practises, or null.</summary>
        public TechniqueData MethodOf(CharacterData member) => Find(member.CultivationMethodId);

        /// <summary>Known methods the member may take up, best grade first (then by name).</summary>
        public IReadOnlyList<TechniqueData> MethodsFor(CharacterData member) =>
            Known.Where(t => TechniqueRules.CanPractise(member, t))
                .OrderByDescending(t => t.Grade)
                .ThenBy(t => t.Name, StringComparer.Ordinal)
                .ToList();

        /// <summary>
        /// Sets the method a member practises, if the clan knows it and the member may practise it. The member
        /// learns it; a flawed method shortens their lifespan, once (it is computed from their realm's reach).
        /// </summary>
        public bool AssignMethod(CharacterData member, string methodId)
        {
            var method = Find(methodId);
            if (!Knows(methodId) || !TechniqueRules.CanPractise(member, method))
            {
                ctx.Log.Warning($"[Techniques] {member.FullName} cannot practise \"{methodId}\".");
                return false;
            }

            member.CultivationMethodId = method.ID;
            if (!member.KnownTechniqueIDs.Contains(method.ID)) member.KnownTechniqueIDs.Add(method.ID);

            int reach = PowerLadder.WoundedLifespan(PowerLadder.MaxLifespan(member.Realm, member.RealmStage), member.DaoWounds);
            member.MaxLifespan = Math.Min(PowerLadder.LifespanLimit(member), TechniqueRules.LifespanWithMethod(reach, method));
            return true;
        }

        /// <summary>Restores the clan's knowledge from a save; deductions saved before grades get one from their realm.</summary>
        public void Restore(IEnumerable<string> savedKnownIds, IEnumerable<TechniqueData> savedDeduced)
        {
            knownIds.Clear();
            foreach (var id in savedKnownIds ?? Enumerable.Empty<string>())
                if (ctx.Content.Techniques.Any(t => t.ID == id)) knownIds.Add(id); // a technique removed from the data is forgotten
            deduced.Clear();
            foreach (var technique in savedDeduced ?? Enumerable.Empty<TechniqueData>())
            {
                if (technique.Grade < TechniqueRules.MinGrade)
                    technique.Grade = Math.Clamp((int)technique.RequiredRealm + 1, TechniqueRules.MinGrade, TechniqueRules.MaxGrade - 1);
                deduced.Add(technique);
            }
        }

        /// <summary>
        /// Brings a record from an older save up to date: a Qi cultivator or beyond practises a method
        /// covering their realm (their own, else the best the clan knows on their Qi) and holds its Qi.
        /// </summary>
        public void NormalizeMember(CharacterData member)
        {
            if (member.Realm < CultivationRealm.QiRefinement) return; // breathing needs no manual

            var method = MethodOf(member);
            if (!TechniqueRules.Covers(method, member.Realm))
            {
                method = Known.Where(t => TechniqueRules.Covers(t, member.Realm) && (member.QiId == null || t.RequiredQiId == member.QiId))
                    .OrderByDescending(t => t.Grade)
                    .ThenBy(t => t.Name, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (method == null)
                {
                    ctx.Log.Warning($"[Techniques] {member.FullName} practises no method the clan knows for their realm.");
                    return;
                }
                member.CultivationMethodId = method.ID;
            }

            member.QiId ??= method.RequiredQiId;
            if (!member.KnownTechniqueIDs.Contains(method.ID)) member.KnownTechniqueIDs.Add(method.ID);
        }
    }
}
