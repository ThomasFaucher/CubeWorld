using UnityEngine;

namespace CubeWorld.Player.CharacterModel
{
    /// <summary>
    /// Posé sur la racine du personnage assemblé : accès typé aux Transforms des pièces
    /// (les "os" du rig sans skinning) pour l'animation par rotation — utilisé par
    /// <see cref="ProceduralCharacterAnimator"/>, et utilisable par n'importe quel autre
    /// code de gameplay (regarder une cible avec Head, attacher une arme à ForearmR...).
    /// </summary>
    public sealed class CharacterModelRoot : MonoBehaviour
    {
        public Transform Hips { get; private set; }
        public Transform Torso { get; private set; }
        public Transform Head { get; private set; }
        public Transform ArmL { get; private set; }
        public Transform ForearmL { get; private set; }
        public Transform ArmR { get; private set; }
        public Transform ForearmR { get; private set; }
        public Transform LegL { get; private set; }
        public Transform FootL { get; private set; }
        public Transform LegR { get; private set; }
        public Transform FootR { get; private set; }

        internal void Bind(
            Transform hips,
            Transform torso,
            Transform head,
            Transform armL,
            Transform forearmL,
            Transform armR,
            Transform forearmR,
            Transform legL,
            Transform footL,
            Transform legR,
            Transform footR
        )
        {
            Hips = hips;
            Torso = torso;
            Head = head;
            ArmL = armL;
            ForearmL = forearmL;
            ArmR = armR;
            ForearmR = forearmR;
            LegL = legL;
            FootL = footL;
            LegR = legR;
            FootR = footR;
        }
    }
}
