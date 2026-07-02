using System;
using UnityEngine;

namespace CubeWorld.Combat
{
    /// <summary>Table de drops : chaque entrée est indépendamment tirée au sort (pas de tirage exclusif).</summary>
    [CreateAssetMenu(
        fileName = "LootTableDefinition",
        menuName = "CubeWorld/Loot Table Definition"
    )]
    public sealed class LootTableDefinition : ScriptableObject
    {
        [SerializeField]
        private LootEntry[] _entries = Array.Empty<LootEntry>();

        public LootEntry[] Entries => _entries;

        [Serializable]
        public struct LootEntry
        {
            public ItemDefinition Item;

            [Range(0f, 1f)]
            public float DropChance;
            public int MinQuantity;
            public int MaxQuantity;
        }
    }
}
