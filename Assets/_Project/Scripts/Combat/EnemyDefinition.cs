using UnityEngine;

namespace CubeWorld.Combat
{
    /// <summary>
    /// Définition d'un type d'ennemi : stats, comportement, récompenses. Pattern
    /// ScriptableObject identique à World.WorldConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "CubeWorld/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Header("Identité")]
        [SerializeField]
        private string _displayName;

        [Header("Stats")]
        [SerializeField]
        private int _maxHP = 30;

        [SerializeField]
        private int _attackDamage = 5;

        [SerializeField]
        private int _xpReward = 20;

        [Header("Comportement")]
        [SerializeField]
        private float _moveSpeed = 3.5f;

        [SerializeField]
        private float _attackRange = 1.5f;

        [SerializeField]
        private float _attackCooldown = 1.2f;

        [SerializeField]
        private float _detectionRadius = 10f;

        [Header("Loot")]
        [SerializeField]
        private LootTableDefinition _lootTable;

        [Header("Gabarit (placeholder visuel)")]
        [SerializeField]
        private float _height = 1.6f;

        [SerializeField]
        private float _radius = 0.45f;

        [SerializeField]
        private Color _bodyColor = new(0.6f, 0.15f, 0.15f);

        public string DisplayName => _displayName;
        public int MaxHP => _maxHP;
        public int AttackDamage => _attackDamage;
        public int XPReward => _xpReward;
        public float MoveSpeed => _moveSpeed;
        public float AttackRange => _attackRange;
        public float AttackCooldown => _attackCooldown;
        public float DetectionRadius => _detectionRadius;
        public LootTableDefinition LootTable => _lootTable;
        public float Height => _height;
        public float Radius => _radius;
        public Color BodyColor => _bodyColor;
    }
}
