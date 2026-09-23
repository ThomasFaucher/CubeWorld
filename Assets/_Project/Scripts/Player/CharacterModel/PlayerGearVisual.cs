using CubeWorld.CharacterModel;
using CubeWorld.CharacterModel.Parts;
using CubeWorld.CharacterModel.Rig;
using CubeWorld.Combat;
using UnityEngine;

namespace CubeWorld.Player.CharacterModel
{
    /// <summary>
    /// Reflète visuellement l'équipement réellement porté (<see cref="PlayerEquipment"/>) sur
    /// le personnage : monte/démonte le mesh d'arme sur
    /// <see cref="CharacterModelRoot.GearBackAnchor"/> et le mesh d'outil sur
    /// <see cref="CharacterModelRoot.ToolBackAnchor"/> à chaque changement d'équipement — les
    /// deux peuvent être équipés/visibles en même temps, sans se superposer (sockets distincts,
    /// voir TorsoGenerator). Le mapping arme/outil -> silhouette passe par
    /// <see cref="WeaponDefinition.VisualKind"/>/<see cref="ToolDefinition.VisualKind"/>.
    ///
    /// Suit aussi les <see cref="CharacterCombatAnimationEvents"/> bakés dans les clips de
    /// combat/minage pour déplacer RÉELLEMENT le mesh entre le dos et
    /// <see cref="CharacterModelRoot.WeaponGripAnchor"/> pendant une action (dégainer/ranger),
    /// au lieu de le garder fixe dans le dos comme avant.
    /// </summary>
    public sealed class PlayerGearVisual : MonoBehaviour
    {
        private PlayerEquipment equipment;
        private CharacterModelRoot modelRoot;
        private CharacterCombatAnimationEvents combatEvents;

        private GameObject currentWeaponObject;
        private WeaponVisualKind currentWeaponKind = WeaponVisualKind.None;

        private GameObject currentToolObject;
        private ToolVisualKind currentToolKind = ToolVisualKind.None;

        /// <summary>Appelé par PlayerBootstrap juste après la création du personnage/de l'équipement.</summary>
        public void Bind(
            PlayerEquipment playerEquipment,
            CharacterModelRoot characterModelRoot,
            CharacterCombatAnimationEvents animationEvents
        )
        {
            equipment = playerEquipment;
            modelRoot = characterModelRoot;
            combatEvents = animationEvents;
            equipment.EquipmentChanged += Refresh;

            if (combatEvents != null)
            {
                combatEvents.WeaponGrabbed += DrawWeaponToHand;
                combatEvents.WeaponSheathed += SheathWeaponToBack;
                combatEvents.ToolGrabbed += DrawToolToHand;
                combatEvents.ToolSheathed += SheathToolToBack;
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (equipment != null)
            {
                equipment.EquipmentChanged -= Refresh;
            }

            if (combatEvents != null)
            {
                combatEvents.WeaponGrabbed -= DrawWeaponToHand;
                combatEvents.WeaponSheathed -= SheathWeaponToBack;
                combatEvents.ToolGrabbed -= DrawToolToHand;
                combatEvents.ToolSheathed -= SheathToolToBack;
            }
        }

        private void Refresh()
        {
            RefreshWeapon();
            RefreshTool();
        }

        private void RefreshWeapon()
        {
            WeaponVisualKind desiredKind = equipment.CurrentWeapon?.VisualKind ?? WeaponVisualKind.None;

            // L'équipement change aussi pour l'armure/l'outil, sans impact ici : ne reconstruire
            // le mesh que si la silhouette d'arme a réellement changé.
            if (desiredKind == currentWeaponKind)
            {
                return;
            }

            currentWeaponKind = desiredKind;

            if (currentWeaponObject != null)
            {
                Destroy(currentWeaponObject);
                currentWeaponObject = null;
            }

            if (desiredKind == WeaponVisualKind.None || modelRoot == null || modelRoot.GearBackAnchor == null)
            {
                return;
            }

            BodyPart gearPart = BuildWeaponPart(desiredKind);
            if (gearPart == null)
            {
                return;
            }

            currentWeaponObject = CharacterAssembler
                .AttachToAnchor(gearPart, modelRoot.GearBackAnchor, modelRoot.SharedMaterial)
                .gameObject;
        }

        private void RefreshTool()
        {
            ToolVisualKind desiredKind = equipment.CurrentTool?.VisualKind ?? ToolVisualKind.None;

            if (desiredKind == currentToolKind)
            {
                return;
            }

            currentToolKind = desiredKind;

            if (currentToolObject != null)
            {
                Destroy(currentToolObject);
                currentToolObject = null;
            }

            if (desiredKind == ToolVisualKind.None || modelRoot == null || modelRoot.ToolBackAnchor == null)
            {
                return;
            }

            BodyPart toolPart = BuildToolPart(desiredKind);
            if (toolPart == null)
            {
                return;
            }

            currentToolObject = CharacterAssembler
                .AttachToAnchor(toolPart, modelRoot.ToolBackAnchor, modelRoot.SharedMaterial)
                .gameObject;
        }

        // Palette/unité repris tels quels du personnage porteur (voir CharacterModelRoot) :
        // arme/outil doivent lire comme des pièces du même corps, pas des accessoires plaqués.
        private BodyPart BuildWeaponPart(WeaponVisualKind kind)
        {
            return kind switch
            {
                WeaponVisualKind.Sword => GearGenerator.BuildSword(modelRoot.Palette, modelRoot.Unit, colorSeed: 0),
                _ => null,
            };
        }

        private BodyPart BuildToolPart(ToolVisualKind kind)
        {
            return kind switch
            {
                ToolVisualKind.Pickaxe => GearGenerator.BuildPickaxe(modelRoot.Palette, modelRoot.Unit, colorSeed: 0),
                _ => null,
            };
        }

        // --- Dessin/rengainage (voir CharacterCombatAnimationEvents) -----------------------

        private void DrawWeaponToHand()
        {
            if (currentWeaponObject == null || modelRoot == null || modelRoot.WeaponGripAnchor == null)
            {
                return;
            }

            CharacterAssembler.MoveToAnchor(currentWeaponObject.transform, modelRoot.WeaponGripAnchor);
        }

        private void SheathWeaponToBack()
        {
            if (currentWeaponObject == null || modelRoot == null || modelRoot.GearBackAnchor == null)
            {
                return;
            }

            CharacterAssembler.MoveToAnchor(currentWeaponObject.transform, modelRoot.GearBackAnchor);
        }

        private void DrawToolToHand()
        {
            if (currentToolObject == null || modelRoot == null || modelRoot.WeaponGripAnchor == null)
            {
                return;
            }

            CharacterAssembler.MoveToAnchor(currentToolObject.transform, modelRoot.WeaponGripAnchor);
        }

        private void SheathToolToBack()
        {
            if (currentToolObject == null || modelRoot == null || modelRoot.ToolBackAnchor == null)
            {
                return;
            }

            CharacterAssembler.MoveToAnchor(currentToolObject.transform, modelRoot.ToolBackAnchor);
        }
    }
}
