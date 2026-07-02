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
    }
}
