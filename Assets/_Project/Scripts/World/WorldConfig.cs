using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Configuration du monde voxel, éditable dans l'inspecteur sans toucher
    /// au code. Style CubeWorld : chunks cubiques (pas de colonnes façon
    /// Minecraft), le monde s'étend aussi verticalement.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldConfig", menuName = "CubeWorld/World Config")]
    public sealed class WorldConfig : ScriptableObject
    {
        [Header("Chunks")]
        [Tooltip("Taille d'un chunk cubique, en voxels (32 => 32x32x32).")]
        [SerializeField] private int _chunkSize = 32;

        [Tooltip("Taille d'un voxel, en unités Unity (1 = cube unitaire ; < 1 = monde plus détaillé/granuleux, le joueur ne change pas de taille).")]
        [SerializeField] private float _voxelSize = 0.5f;

        [Tooltip("Distance de vue horizontale, en chunks autour du joueur.")]
        [SerializeField] private int _viewDistance = 8;

        [Tooltip("Distance de vue verticale, en chunks au-dessus/en-dessous du joueur.")]
        [SerializeField] private int _verticalViewDistance = 4;

        [Header("Génération")]
        [Tooltip("Graine du monde : une même graine produit le même monde (ignorée si RandomizeSeedOnStart).")]
        [SerializeField] private int _seed = 1337;

        [Tooltip("Si coché, une nouvelle graine est tirée à chaque lancement (monde / biomes différents).")]
        [SerializeField] private bool _randomizeSeedOnStart = true;

        [Tooltip("Échelle du bruit de Perlin : plus petit = collines plus larges.")]
        [SerializeField] private float _noiseScale = 0.008f;

        [Tooltip("Hauteur maximale du terrain, en voxels.")]
        [SerializeField] private int _maxTerrainHeight = 96;

        [Tooltip("Niveau de la mer, en voxels.")]
        [SerializeField] private int _seaLevel = 40;

        [Header("Biomes")]
        [Tooltip("Échelle du bruit de biome : plus petit = régions de biome plus larges (nettement < NoiseScale).")]
        [SerializeField] private float _biomeNoiseScale = 0.0015f;

        public int ChunkSize => _chunkSize;
        public float VoxelSize => _voxelSize;
        public int ViewDistance => _viewDistance;
        public int VerticalViewDistance => _verticalViewDistance;
        public int Seed => _seed;
        public bool RandomizeSeedOnStart => _randomizeSeedOnStart;
        public float NoiseScale => _noiseScale;
        public int MaxTerrainHeight => _maxTerrainHeight;
        public int SeaLevel => _seaLevel;
        public float BiomeNoiseScale => _biomeNoiseScale;

        /// <summary>Override runtime de la graine (copie d'asset, pas l'asset disque).</summary>
        public void SetSeed(int seed) => _seed = seed;

        /// <summary>Nombre total de voxels dans un chunk.</summary>
        public int VoxelsPerChunk => _chunkSize * _chunkSize * _chunkSize;

        /// <summary>Nombre de chunks empilés verticalement pour couvrir la hauteur max du terrain.</summary>
        public int VerticalChunkCount => (_maxTerrainHeight + _chunkSize - 1) / _chunkSize;
    }
}
