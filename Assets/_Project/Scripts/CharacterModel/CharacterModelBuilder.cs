using CubeWorld.CharacterModel.Generation;
using CubeWorld.CharacterModel.Parts;
using CubeWorld.CharacterModel.Rig;
using UnityEngine;

namespace CubeWorld.CharacterModel
{
    /// <summary>
    /// Point d'entrée du système de personnages voxel : construit un personnage chibi complet
    /// (grosse tête ~45% de la hauteur, membres épais, couleurs plates saturées) comme un
    /// ASSEMBLAGE de pièces indépendantes — un GameObject + mesh par partie du corps, chaque
    /// mesh pivotant autour de son articulation. La hiérarchie (noms stables, voir
    /// <see cref="CharacterRigDefinition"/>) est animable par rotation des Transforms — le
    /// joueur utilise un vrai Animator Controller (voir CharacterLocomotionAnimator,
    /// assemblée CubeWorld.Player) dont les clips ciblent ces chemins de Transform.
    ///
    /// Même seed -> même personnage (palette, proportions, coiffure, expression mise à part).
    /// Réutilisé au-delà du joueur (voir CubeWorld.Combat.EnemyAI) : n'importe quel appelant
    /// cross-assemblée peut construire un personnage via <see cref="Build"/> tant qu'il ne
    /// dépend que de l'API publique (pas des types internes des sous-dossiers).
    /// </summary>
    public static class CharacterModelBuilder
    {
        // Layout vertical en voxels (voir aussi les encastrements dans les générateurs :
        // jambes -2 rangées dans le pelvis, bottine -1 dans la jambe, cou -1 dans le torse).
        private const int LegHeight = 9;
        private const int HipsHeight = 3;
        private const int TorsoHeight = 9;
        private const int TorsoDepth = 8;

        /// <param name="targetHeight">Hauteur totale du personnage en unités monde.</param>
        /// <param name="archetype">
        /// Résout une silhouette FIGÉE (<see cref="CharacterSilhouetteCatalog"/>, forme) et
        /// une palette dédiée (<see cref="CharacterPalette"/>, couleurs). Le seed ne fait
        /// varier que les couleurs/proportions à l'intérieur de ce gabarit, jamais la forme.
        /// </param>
        public static GameObject Build(
            float targetHeight,
            CharacterArchetype archetype = CharacterArchetype.Swordsman,
            int seed = 0,
            CharacterExpression expression = CharacterExpression.Neutral
        )
        {
            var rng = new CharacterRng(seed);
            CharacterSilhouette silhouette = CharacterSilhouetteCatalog.Get(archetype);
            CharacterPalette palette = CharacterPalette.Generate(rng, archetype);
            CharacterBodyParams body = CharacterBodyParams.Generate(rng);
            HeadShape headShape = HeadShape.Generate(body.HeadWidth, body.HeadRows, rng);

            // Largeur de buste paire : tous les stamps intérieurs (taille, plug du pelvis...)
            // sont pairs eux aussi et restent donc parfaitement centrés.
            int torsoWidth = 2 * Mathf.Max(5, Mathf.RoundToInt(6f * body.TorsoWidthScale));
            int legThickness = Mathf.Clamp(Mathf.RoundToInt(5f * body.LimbThicknessScale), 4, 5);

            // Rangées du cône du bonnet au-dessus du crâne (le bonnet pointu du héros est
            // haut : ~30% de la largeur de tête).
            int capRows = Mathf.Max(4, Mathf.RoundToInt(body.HeadWidth * 0.30f));

            // La hauteur totale suit le nombre réel de rangées empilées (sol -> sommet des
            // cheveux) pour que la proportion "grosse tête chibi" reste stable par seed.
            int totalRows =
                LegHeight + HipsHeight + (TorsoHeight - 1) + 2 + body.HeadRows + capRows;
            float unit = targetHeight / totalRows;
            int colorSeed = 0;

            // Génération des pièces (bras et jambes générés deux fois : jitter de couleur
            // différent par côté, et meshes indépendants si on veut les faire diverger).
            BodyPart hips = HipsGenerator.Build(
                torsoWidth,
                TorsoDepth,
                HipsHeight,
                legThickness,
                palette,
                unit,
                colorSeed++
            );
            BodyPart torso = TorsoGenerator.Build(
                torsoWidth,
                TorsoHeight,
                TorsoDepth,
                ArmGenerator.Width,
                palette,
                silhouette,
                unit,
                colorSeed++
            );
            BodyPart head = HeadGenerator.Build(
                body,
                headShape,
                palette,
                expression,
                silhouette,
                capRows,
                unit,
                colorSeed++
            );
            BodyPart armL = ArmGenerator.BuildUpperArm(palette, silhouette, unit, colorSeed++);
            BodyPart armR = ArmGenerator.BuildUpperArm(palette, silhouette, unit, colorSeed++);
            BodyPart forearmL = ArmGenerator.BuildForearm(palette, unit, colorSeed++);
            BodyPart forearmR = ArmGenerator.BuildForearm(palette, unit, colorSeed++);
            BodyPart legL = LegGenerator.BuildLeg(
                legThickness,
                LegHeight,
                palette,
                unit,
                colorSeed++
            );
            BodyPart legR = LegGenerator.BuildLeg(
                legThickness,
                LegHeight,
                palette,
                unit,
                colorSeed++
            );
            BodyPart footL = LegGenerator.BuildFoot(legThickness, palette, unit, colorSeed++);
            BodyPart footR = LegGenerator.BuildFoot(legThickness, palette, unit, colorSeed++);

            // Assemblage : chaque pièce est posée sur le socket de son parent ; son Transform
            // est directement l'articulation à animer.
            var root = new GameObject(CharacterRigDefinition.Root);
            Material material = CharacterAssembler.CreateSharedMaterial();

            var hipsSocket = new PartSocket(
                CharacterRigDefinition.Hips,
                new Vector3(0f, LegHeight * unit, 0f)
            );
            Transform hipsT = CharacterAssembler.Attach(hips, root.transform, hipsSocket, material);
            Transform torsoT = CharacterAssembler.Attach(
                torso,
                hipsT,
                hips.GetSocket(CharacterRigDefinition.SocketTorso),
                material
            );
            Transform headT = CharacterAssembler.Attach(
                head,
                torsoT,
                torso.GetSocket(CharacterRigDefinition.SocketNeck),
                material
            );

            // Pas d'épée par défaut : l'arme affichée est désormais un élément à part, piloté
            // par l'équipement du joueur (inventaire) via PlayerGearVisual, qui monte/démonte
            // le mesh d'arme sur cette ancre au fil des changements d'équipement (voir
            // GearGenerator). L'ancre existe toujours, même sans arme équipée : le socket Back
            // du torse lui-même disparaît une fois l'assemblage terminé.
            Transform gearBackAnchor = CharacterAssembler.CreateAnchor(
                CharacterRigDefinition.GearBackAnchor,
                torsoT,
                torso.GetSocket(CharacterRigDefinition.SocketBack)
            );

            // Deuxième port dans le dos (outil, ex. pioche) : même principe, socket distinct
            // (voir TorsoGenerator) pour ne jamais superposer arme et outil.
            Transform toolBackAnchor = CharacterAssembler.CreateAnchor(
                CharacterRigDefinition.ToolBackAnchor,
                torsoT,
                torso.GetSocket(CharacterRigDefinition.SocketBackTool)
            );

            Transform armLT = CharacterAssembler.Attach(
                armL,
                torsoT,
                torso.GetSocket(CharacterRigDefinition.SocketShoulderL),
                material,
                CharacterRigDefinition.ArmL
            );
            Transform armRT = CharacterAssembler.Attach(
                armR,
                torsoT,
                torso.GetSocket(CharacterRigDefinition.SocketShoulderR),
                material,
                CharacterRigDefinition.ArmR
            );
            Transform forearmLT = CharacterAssembler.Attach(
                forearmL,
                armLT,
                armL.GetSocket(CharacterRigDefinition.SocketElbow),
                material,
                CharacterRigDefinition.ForearmL
            );
            Transform forearmRT = CharacterAssembler.Attach(
                forearmR,
                armRT,
                armR.GetSocket(CharacterRigDefinition.SocketElbow),
                material,
                CharacterRigDefinition.ForearmR
            );

            // Point de préhension dans la moufle droite : c'est ici (pas sur le dos) que
            // PlayerGearVisual pose l'arme/l'outil pendant qu'il est activement tenu en main.
            Transform weaponGripAnchor = CharacterAssembler.CreateAnchor(
                CharacterRigDefinition.WeaponGripAnchor,
                forearmRT,
                forearmR.GetSocket(CharacterRigDefinition.SocketGrip)
            );

            Transform legLT = CharacterAssembler.Attach(
                legL,
                hipsT,
                hips.GetSocket(CharacterRigDefinition.SocketHipL),
                material,
                CharacterRigDefinition.LegL
            );
            Transform legRT = CharacterAssembler.Attach(
                legR,
                hipsT,
                hips.GetSocket(CharacterRigDefinition.SocketHipR),
                material,
                CharacterRigDefinition.LegR
            );
            Transform footLT = CharacterAssembler.Attach(
                footL,
                legLT,
                legL.GetSocket(CharacterRigDefinition.SocketAnkle),
                material,
                CharacterRigDefinition.FootL
            );
            Transform footRT = CharacterAssembler.Attach(
                footR,
                legRT,
                legR.GetSocket(CharacterRigDefinition.SocketAnkle),
                material,
                CharacterRigDefinition.FootR
            );

            var model = root.AddComponent<CharacterModelRoot>();
            model.Bind(
                hipsT,
                torsoT,
                headT,
                armLT,
                forearmLT,
                armRT,
                forearmRT,
                legLT,
                footLT,
                legRT,
                footRT,
                gearBackAnchor,
                toolBackAnchor,
                weaponGripAnchor,
                palette,
                unit,
                material
            );

            return root;
        }
    }
}
