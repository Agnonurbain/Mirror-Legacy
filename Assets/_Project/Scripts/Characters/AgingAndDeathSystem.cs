using UnityEngine;
using MirrorChronicles.Core;
using MirrorChronicles.Events;
using MirrorChronicles.Data;
using MirrorChronicles.Clan;
using System.Collections.Generic;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Handles the aging of characters and their death by old age or other causes.
    /// Listens to the start of a new year.
    /// </summary>
    public class AgingAndDeathSystem : MonoBehaviour
    {
        public static AgingAndDeathSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnYearStarted += HandleAging;
        }

        private void OnDisable()
        {
            GameEvents.OnYearStarted -= HandleAging;
        }

        private void HandleAging(int year)
        {
            if (ClanManager.Instance == null) return;

            List<CharacterData> membersToDie = new List<CharacterData>();

            foreach (var member in ClanManager.Instance.LivingMembers)
            {
                member.Age++;
                int maxLifespan = CalculateMaxLifespan(member.Realm);

                if (member.Age >= maxLifespan)
                {
                    membersToDie.Add(member);
                }
            }

            foreach (var deadMember in membersToDie)
            {
                Die(deadMember, DeathCause.OldAge);
            }
        }

        public int CalculateMaxLifespan(CultivationRealm realm)
        {
            switch (realm)
            {
                case CultivationRealm.Embryonic: return 80;
                case CultivationRealm.QiRefinement: return 120;
                case CultivationRealm.Foundation: return 250;
                case CultivationRealm.PurpleMansion: return 500;
                case CultivationRealm.GoldenCore: return 1000;
                case CultivationRealm.DaoEmbryo: return 3000;
                default: return 80;
            }
        }

        public void Die(CharacterData character, DeathCause cause)
        {
            if (!character.IsAlive) return;

            Debug.Log($"<color=red>[Death]</color> {character.FirstName} has died at age {character.Age}. Cause: {cause}");
            character.IsAlive = false;
            
            if (ClanManager.Instance != null)
            {
                ClanManager.Instance.LivingMembers.Remove(character);
            }

            GameEvents.TriggerCharacterDied(character, cause);
        }
    }
}
