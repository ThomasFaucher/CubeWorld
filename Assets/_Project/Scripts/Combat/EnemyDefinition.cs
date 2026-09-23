using CubeWorld.CharacterModel;
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

        [Header("Gabarit (collider + génération du personnage)")]
        [SerializeField]
        private float _height = 1.6f;

        [SerializeField]
        private float _radius = 0.45f;

        [Header("Visuel")]
        [Tooltip(
            "Silhouette/palette du personnage généré (voir Assets/_Project/Scripts/CharacterModel) "
                + "— même pipeline que le joueur, réutilisé pour les ennemis. Skeleton est le seul "
                + "preset pensé pour un monstre à ce jour."
        )]
        [SerializeField]
        private CharacterArchetype _archetype = CharacterArchetype.Skeleton;

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
        public CharacterArchetype Archetype => _archetype;
    }
}
