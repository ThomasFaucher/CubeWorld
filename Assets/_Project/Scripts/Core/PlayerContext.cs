using UnityEngine;

namespace CubeWorld.Core
{
    /// <summary>
    /// Référence au joueur accessible depuis n'importe quelle assembly sans
    /// dépendre de CubeWorld.Player (pas de tag magique dans ce projet).
    /// Assigné une fois par PlayerBootstrap.Awake.
    /// </summary>
    public static class PlayerContext
    {
        public static Transform Transform { get; set; }

        /// <summary>Rayon de la capsule de collision du joueur (les ennemis s'en servent pour ne pas lui rentrer dedans).</summary>
        public static float Radius { get; set; } = 0.4f;
    }
}
