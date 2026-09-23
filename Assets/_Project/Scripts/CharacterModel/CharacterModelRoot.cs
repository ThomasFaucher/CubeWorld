using CubeWorld.CharacterModel.Generation;
using UnityEngine;

namespace CubeWorld.CharacterModel
{
    /// <summary>
    /// Posé sur la racine du personnage assemblé : accès typé aux Transforms des pièces
    /// (les "os" du rig sans skinning) — noms de Transform stables consommés par l'Animator
    /// Controller de locomotion du joueur (voir CharacterLocomotionAnimator), et utilisable
    /// par n'importe quel autre code de gameplay (regarder une cible avec Head, attacher une
    /// arme à ForearmR...). Expose aussi
    /// la palette/l'unité/le matériau de ce personnage précis (voir
    /// <see cref="Rig.CharacterAssembler"/>) : réutilisés par le PlayerGearVisual du joueur
    /// (assemblage CubeWorld.Player, hors de cette assemblée) pour que l'arme montée
    /// dynamiquement s'accorde exactement au corps généré ; tout autre code cross-assemblée
    /// (ex. génération d'ennemis) peut s'appuyer sur les mêmes propriétés publiques.
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

        /// <summary>Ancre du socket Back du torse (voir CharacterRigDefinition.GearBackAnchor).</summary>
        public Transform GearBackAnchor { get; private set; }

        /// <summary>Ancre du socket BackTool du torse (voir CharacterRigDefinition.ToolBackAnchor) — port de l'outil (pioche), distinct de l'arme.</summary>
        public Transform ToolBackAnchor { get; private set; }

        /// <summary>Ancre du socket Grip de l'avant-bras droit (voir CharacterRigDefinition.WeaponGripAnchor) — arme/outil tenu en main.</summary>
        public Transform WeaponGripAnchor { get; private set; }

        /// <summary>
        /// Palette de couleurs de ce personnage précis — publique pour que du code
        /// cross-assemblée (PlayerGearVisual, génération d'ennemis...) puisse générer des
        /// pièces additionnelles assorties (voir <see cref="Parts.GearGenerator"/>).
        /// </summary>
        public CharacterPalette Palette { get; private set; }

        /// <summary>Taille d'un voxel en unités monde pour ce personnage (voir CharacterModelBuilder.Build).</summary>
        public float Unit { get; private set; }

        /// <summary>Matériau partagé par toutes les pièces du corps — à réutiliser pour toute pièce ajoutée après coup (batching).</summary>
        public Material SharedMaterial { get; private set; }

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
            Transform footR,
            Transform gearBackAnchor,
            Transform toolBackAnchor,
            Transform weaponGripAnchor,
            CharacterPalette palette,
            float unit,
            Material sharedMaterial
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
            GearBackAnchor = gearBackAnchor;
            ToolBackAnchor = toolBackAnchor;
            WeaponGripAnchor = weaponGripAnchor;
            Palette = palette;
            Unit = unit;
            SharedMaterial = sharedMaterial;
        }
    }
}
