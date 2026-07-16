using CubeWorld.Player.CharacterModel.Generation;
using CubeWorld.Player.CharacterModel.Parts;
using CubeWorld.Player.CharacterModel.Rig;
using UnityEngine;

namespace CubeWorld.Player.CharacterModel
{
    /// <summary>
    /// Point d'entrée du système de personnages voxel : construit un personnage chibi complet
    /// (grosse tête ~45% de la hauteur, membres épais, couleurs plates saturées) comme un
    /// ASSEMBLAGE de pièces indépendantes — un GameObject + mesh par partie du corps, chaque
    /// mesh pivotant autour de son articulation. La hiérarchie (noms stables, voir
    /// <see cref="CharacterRigDefinition"/>) est animable par rotation des Transforms, par
    /// code (<see cref="ProceduralCharacterAnimator"/>) ou par un Animator Controller.
    ///
    /// Même seed -> même personnage (palette, proportions, coiffure, expression mise à part).
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
        /// Un seul archétype de référence est implémenté pour l'instant : toutes les valeurs
        /// produisent la même silhouette. Brancher ici un switch (palettes/pièces par
        /// archétype) quand les autres seront redessinés sur ce système.
        /// </param>
        public static GameObject Build(
            float targetHeight,
            PlayerArchetype archetype = PlayerArchetype.Swordsman,
            int seed = 0,
            CharacterExpression expression = CharacterExpression.Neutral
        )
        {
            var rng = new CharacterRng(seed);
            CharacterPalette palette = CharacterPalette.Generate(rng);
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
                unit,
                colorSeed++
            );
            BodyPart head = HeadGenerator.Build(
                body,
                headShape,
                palette,
                expression,
                capRows,
                unit,
                colorSeed++
            );
            BodyPart armL = ArmGenerator.BuildUpperArm(palette, unit, colorSeed++);
            BodyPart armR = ArmGenerator.BuildUpperArm(palette, unit, colorSeed++);
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
            BodyPart sword = GearGenerator.BuildSword(palette, unit, colorSeed++);

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

            // Épée décorative dans le dos : suit le torse (pas un os d'animation, donc
            // absente de CharacterModelRoot). L'équipement réel la remplacera plus tard.
            CharacterAssembler.Attach(
                sword,
                torsoT,
                torso.GetSocket(CharacterRigDefinition.SocketBack),
                material
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
                footRT
            );

            return root;
        }
    }
}
