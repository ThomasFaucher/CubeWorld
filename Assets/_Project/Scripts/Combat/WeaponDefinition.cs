using UnityEngine;

namespace CubeWorld.Combat
{
    /// <summary>
    /// Définition d'une arme de mêlée. Data-driven pour permettre du ranged plus
    /// tard sans réécrire PlayerCombat/EnemyAI (voir MeleeAttackResolver).
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "CubeWorld/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField]
        private string _displayName;

        [SerializeField]
        private int _damage = 10;

        [SerializeField]
        private float _range = 2f;

        [SerializeField]
        private float _radius = 0.75f;

        [SerializeField]
        private float _attackCooldown = 0.6f;

        public string DisplayName => _displayName;
        public int Damage => _damage;
        public float Range => _range;
        public float Radius => _radius;
        public float AttackCooldown => _attackCooldown;
    }
}
