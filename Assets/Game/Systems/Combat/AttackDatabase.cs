using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.Combat
{
    [CreateAssetMenu(menuName = "EffectInstance System/Database/Attack Database")]
    public class AttackDatabase : ScriptableObject
    {
        [SerializeField]
        private List<AttackData> attacks = new();

        private Dictionary<int, AttackData> attackById;

        public void Initialize()
        {
            attackById = new Dictionary<int, AttackData>();

            foreach (var attack in attacks)
            {
                if (attack == null)
                    continue;

                if (attackById.ContainsKey(attack.AttackId))
                {
                    Debug.LogError(
                        $"Duplicate AttackId {attack.AttackId} in AttackDatabase",
                        attack
                    );
                    continue;
                }

                attackById.Add(attack.AttackId, attack);
            }
        }

        public AttackData GetAttack(int attackId)
        {
            if (attackById == null)
                Initialize();

            if (attackById.TryGetValue(attackId, out var attack))
                return attack;

            Debug.LogWarning($"AttackId {attackId} not found in AttackDatabase");
            return null;
        }

        public IReadOnlyList<AttackData> GetAllAttacks() => attacks;
    }
}