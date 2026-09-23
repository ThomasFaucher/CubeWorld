using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CubeWorld.EditorTools
{
    /// <summary>
    /// Outil one-shot : (re)génère les <see cref="AnimationClip"/> de locomotion (Idle/Walk/
    /// Run) ET de combat/minage (attaque épée en combo, coups de poing, minage à la pioche),
    /// ainsi que l'<see cref="AnimatorController"/> à deux layers qui les orchestre, consommés
    /// par CubeWorld.Player.CharacterModel.CharacterLocomotionAnimator/
    /// CharacterCombatAnimationEvents (assemblée Player, pas référençable ici — même raison que
    /// les chemins d'os en dur ci-dessous : ce script, assemblée Editor par défaut, ne doit pas
    /// dépendre d'une assemblée runtime spécifique).
    ///
    /// Layer 0 "Locomotion" : Idle/Walk/Run mélangés en continu par un Blend Tree 1D (voir
    /// BuildController) — inchangé par l'ajout du combat.
    ///
    /// Layer 1 "Combat" : un <see cref="AvatarMask"/> restreint son effet au buste/tête/bras
    /// (le bas du corps reste piloté à 100% par le layer Locomotion, même pendant une attaque —
    /// voir BuildUpperBodyMask), poids piloté par script
    /// (CharacterCombatAnimationEvents.TriggerAction, PAS par l'Animator lui-même) : 0 hors
    /// combat, monte à 1 au déclenchement d'une action, retombe à 0 via l'AnimationEvent
    /// générique "OnActionEnd" baké près de la fin de chaque clip (voir BuildActionClip). Un
    /// seul paramètre "ActionTrigger" (Trigger) + "ActionId" (Int) suffit à sélectionner
    /// N'IMPORTE LEQUEL des clips de combat depuis un unique jeu de transitions Any State ->
    /// État, chacune ne revenant à l'état vide CombatIdle qu'après son Exit Time. Les valeurs
    /// numériques d'ActionId ci-dessous (voir BuildCombatLayer) DOIVENT rester synchronisées
    /// avec CubeWorld.Player.CharacterModel.CombatActionId (assemblée Player, lue par
    /// PlayerCombat/PlayerMining pour choisir l'action à déclencher).
    ///
    /// Les courbes de rotation ci-dessous sont la version "baked" des formules qui vivaient
    /// dans ProceduralCharacterAnimator — voir son historique git pour la justification de
    /// chaque terme (flexion de genou simulée, écart latéral, respiration...). Cet outil est
    /// idempotent : le relancer régénère les assets en place (mêmes GUID) sans casser les
    /// références existantes (utile si les constantes ci-dessous sont retouchées).
    ///
    /// Idle/Walk/Run sont mélangés en continu par un seul état "Locomotion" portant un
    /// Blend Tree 1D sur le paramètre "Speed" (voir BuildController), plutôt que des états
    /// discrets reliés par des transitions à seuil : Run n'est PAS Walk rejoué plus vite (ça
    /// donnait un sprint mécanique, la foulée "linéaire") mais un cycle à part, à foulée/swing
    /// de bras plus amples et surtout à inclinaison avant marquée — Walk porte déjà une
    /// inclinaison avant modérée en permanence (buste ET bassin), pas seulement le bob
    /// symétrique existant. Comme le Blend Tree interpole réellement les courbes entre clips,
    /// l'inclinaison croît naturellement avec la vitesse au lieu d'être un cran tout-ou-rien.
    ///
    /// Chemins d'os en dur ("Hips/Torso"...) plutôt qu'une référence à
    /// CubeWorld.CharacterModel.Rig.CharacterRigDefinition : cette classe est `internal` à
    /// l'assemblée CubeWorld.CharacterModel et ce script (assemblée Editor par défaut, dossier
    /// Assets/_Project/Editor/) ne peut pas — et ne doit pas — en dépendre. Ces noms sont
    /// documentés comme stables (voir CharacterRigDefinition.cs) : ne les modifier ici QUE si
    /// la hiérarchie du rig généré change réellement.
    /// </summary>
    public static class CharacterLocomotionAnimatorGenerator
    {
        private const string OutputFolder = "Assets/_Project/Resources/Animations/CharacterModel";
        private const string IdleClipPath = OutputFolder + "/CharacterIdle.anim";
        private const string WalkClipPath = OutputFolder + "/CharacterWalk.anim";
        private const string RunClipPath = OutputFolder + "/CharacterRun.anim";
        private const string ControllerPath = OutputFolder + "/CharacterLocomotion.controller";

        private const string SwordDrawSlash1Path = OutputFolder + "/SwordDrawSlash1.anim";
        private const string SwordSlash1Path = OutputFolder + "/SwordSlash1.anim";
        private const string SwordSlash2Path = OutputFolder + "/SwordSlash2.anim";
        private const string SwordSlash3Path = OutputFolder + "/SwordSlash3.anim";
        private const string SwordSheathPath = OutputFolder + "/SwordSheath.anim";
        private const string PunchJabLPath = OutputFolder + "/PunchJabL.anim";
        private const string PunchJabRPath = OutputFolder + "/PunchJabR.anim";
        private const string PunchCrossPath = OutputFolder + "/PunchCross.anim";
        private const string PickaxeDrawSwingPath = OutputFolder + "/PickaxeDrawSwing.anim";
        private const string PickaxeSwingPath = OutputFolder + "/PickaxeSwing.anim";
        private const string PickaxeSheathPath = OutputFolder + "/PickaxeSheath.anim";

        // Seuils (vitesse horizontale en m/s du CharacterController, voir Speed dans
        // CharacterLocomotionAnimator) du Blend Tree 1D de BuildController : à ces valeurs, le
        // clip correspondant est joué à poids plein. Calés sur les valeurs par défaut de
        // PlayerController (_walkSpeed=6, sprint = 6*1.8=10.8) avec une petite marge, pour que
        // marcher "normalement" (sans sprint) atteigne déjà la pose Walk pleine, et sprinter la
        // pose Run pleine — pas des vitesses garanties si ces champs sont retouchés à l'instance,
        // mais une référence raisonnable, comme le reste des constantes de ce fichier.
        private const float WalkBlendSpeed = 5f;
        private const float RunBlendSpeed = 9.5f;

        private const float LegSwingDegrees = 32f;
        private const float LegFlexDegrees = 14f;
        private const float LegSideSwingDegrees = 6f;
        private const float ArmSwingDegrees = 38f;
        private const float BreathDegrees = 2f;

        // Course : foulée/swing de bras nettement plus amples que la marche (pas juste la même
        // foulée rejouée plus vite) — c'est ce qui fait qu'un sprint a l'air de courir plutôt que
        // de "marcher vite".
        private const float RunLegSwingDegrees = 46f;
        private const float RunLegFlexDegrees = 30f;
        private const float RunLegSideSwingDegrees = 5f;
        private const float RunArmSwingDegrees = 55f;

        // Inclinaison avant constante (degrés, rotation X = pitch) du buste et du bassin —
        // absente de l'ancienne version (le buste ne faisait qu'un bob symétrique autour de 0°).
        // Le bassin penche moins que le buste (plus proche du centre de masse), comme une vraie
        // posture de course/marche.
        private const float WalkTorsoLeanDegrees = 6f;
        private const float WalkHipsLeanDegrees = 3f;
        private const float RunTorsoLeanDegrees = 16f;
        private const float RunHipsLeanDegrees = 8f;

        // Contre-inclinaison partielle de la tête par rapport au buste : dans une vraie posture
        // de course, le buste plonge vers l'avant mais le regard reste plus horizontal. Sans ça,
        // une tête qui suit le buste à l'identique accentue l'effet "mannequin penché en bloc".
        private const float HeadCounterLeanRatio = 0.3f;

        // Poids de la seconde harmonique (2x la fréquence de la foulée) ajoutée au swing
        // jambes/bras : un sinus pur produit un mouvement de pendule trop régulier ("trop
        // linéaire" perçu) ; cette petite distorsion asymétrise légèrement l'aller/retour, plus
        // proche d'une vraie marche/course où la phase d'appui et la phase aérienne n'ont pas la
        // même durée.
        private const float SecondaryHarmonicWeight = 0.15f;

        private const int SamplesPerLoop = 32;

        // Durées (secondes) des actions à un coup — pas de rapport avec SamplesPerLoop
        // (locomotion, en boucle) : ces clips ne bouclent pas, voir BuildActionClip.
        private const float SwordDrawSlash1Duration = 0.55f;
        private const float SwordSlashDuration = 0.42f;
        private const float SwordSheathDuration = 0.5f;
        private const float PunchDuration = 0.35f;
        private const float PickaxeDrawSwingDuration = 0.6f;
        private const float PickaxeSwingDuration = 0.45f;
        private const float PickaxeSheathDuration = 0.5f;

        [MenuItem("CubeWorld/Character/Generate Locomotion Animator")]
        public static void Generate()
        {
            EnsureFolder();

            AnimationClip idle = SaveOrReplaceClip(BuildIdleClip(), IdleClipPath);
            AnimationClip walk = SaveOrReplaceClip(BuildWalkClip(), WalkClipPath);
            AnimationClip run = SaveOrReplaceClip(BuildRunClip(), RunClipPath);

            AnimationClip swordDraw = SaveOrReplaceClip(BuildSwordDrawSlash1Clip(), SwordDrawSlash1Path);
            AnimationClip swordSlash1 = SaveOrReplaceClip(BuildSwordSlash1Clip(), SwordSlash1Path);
            AnimationClip swordSlash2 = SaveOrReplaceClip(BuildSwordSlash2Clip(), SwordSlash2Path);
            AnimationClip swordSlash3 = SaveOrReplaceClip(BuildSwordSlash3Clip(), SwordSlash3Path);
            AnimationClip swordSheath = SaveOrReplaceClip(BuildSwordSheathClip(), SwordSheathPath);
            AnimationClip punchJabL = SaveOrReplaceClip(BuildPunchJabLClip(), PunchJabLPath);
            AnimationClip punchJabR = SaveOrReplaceClip(BuildPunchJabRClip(), PunchJabRPath);
            AnimationClip punchCross = SaveOrReplaceClip(BuildPunchCrossClip(), PunchCrossPath);
            AnimationClip pickaxeDraw = SaveOrReplaceClip(BuildPickaxeDrawSwingClip(), PickaxeDrawSwingPath);
            AnimationClip pickaxeSwing = SaveOrReplaceClip(BuildPickaxeSwingClip(), PickaxeSwingPath);
            AnimationClip pickaxeSheath = SaveOrReplaceClip(BuildPickaxeSheathClip(), PickaxeSheathPath);

            BuildController(
                idle,
                walk,
                run,
                swordDraw,
                swordSlash1,
                swordSlash2,
                swordSlash3,
                swordSheath,
                punchJabL,
                punchJabR,
                punchCross,
                pickaxeDraw,
                pickaxeSwing,
                pickaxeSheath
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CubeWorld] Animator de locomotion/combat (re)généré dans {OutputFolder}.");
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Resources");
            }

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/Animations"))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "Animations");
            }

            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Resources/Animations", "CharacterModel");
            }
        }

        // --- Clips ------------------------------------------------------------------------

        // Sinus principal de la foulée + seconde harmonique (voir SecondaryHarmonicWeight) :
        // casse la symétrie parfaite du pendule sinusoïdal pur sans changer le caractère
        // globalement périodique de la courbe.
        private static float GaitSwing(float phase) =>
            Mathf.Sin(phase) + (SecondaryHarmonicWeight * Mathf.Sin(2f * phase));

        private static AnimationClip BuildWalkClip() =>
            BuildLocomotionClip(
                "CharacterWalk",
                LegSwingDegrees,
                LegFlexDegrees,
                LegSideSwingDegrees,
                ArmSwingDegrees,
                WalkTorsoLeanDegrees,
                WalkHipsLeanDegrees,
                torsoBobDegrees: 2f,
                headSwayDegrees: 2f,
                hipsSwayDegrees: 3f
            );

        private static AnimationClip BuildRunClip() =>
            BuildLocomotionClip(
                "CharacterRun",
                RunLegSwingDegrees,
                RunLegFlexDegrees,
                RunLegSideSwingDegrees,
                RunArmSwingDegrees,
                RunTorsoLeanDegrees,
                RunHipsLeanDegrees,
                torsoBobDegrees: 4f,
                headSwayDegrees: 3f,
                hipsSwayDegrees: 3f
            );

        // Marche et course partagent la même structure de courbes (foulée jambes/pieds, contre-
        // balancier bras, inclinaison avant buste/bassin, bob tête/buste) — seules les
        // amplitudes diffèrent (voir BuildWalkClip/BuildRunClip), pour garantir que Run soit un
        // vrai cycle distinct et pas un clone accidentellement désynchronisé de Walk.
        private static AnimationClip BuildLocomotionClip(
            string clipName,
            float legSwingDegrees,
            float legFlexDegrees,
            float legSideSwingDegrees,
            float armSwingDegrees,
            float torsoLeanDegrees,
            float hipsLeanDegrees,
            float torsoBobDegrees,
            float headSwayDegrees,
            float hipsSwayDegrees
        )
        {
            var clip = new AnimationClip { name = clipName, frameRate = 30f };

            int n = SamplesPerLoop;
            var times = new float[n + 1];
            var hips = new Vector3[n + 1];
            var legL = new Vector3[n + 1];
            var legR = new Vector3[n + 1];
            var footL = new Vector3[n + 1];
            var footR = new Vector3[n + 1];
            var armL = new Vector3[n + 1];
            var armR = new Vector3[n + 1];
            var forearmL = new Vector3[n + 1];
            var forearmR = new Vector3[n + 1];
            var torso = new Vector3[n + 1];
            var head = new Vector3[n + 1];

            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n;
                float phase = t * Mathf.PI * 2f;
                times[i] = t;

                float swing = GaitSwing(phase);
                float forwardL = Mathf.Max(0f, swing);
                float forwardR = Mathf.Max(0f, -swing);
                float flexL = forwardL * legFlexDegrees;
                float flexR = forwardR * legFlexDegrees;
                float sideL = forwardL * legSideSwingDegrees;
                float sideR = forwardR * legSideSwingDegrees;

                hips[i] = new Vector3(hipsLeanDegrees, 0f, swing * hipsSwayDegrees);

                legL[i] = new Vector3((swing * legSwingDegrees) + flexL, 0f, sideL);
                legR[i] = new Vector3((-swing * legSwingDegrees) + flexR, 0f, -sideR);
                footL[i] = new Vector3((-swing * legSwingDegrees * 0.5f) - flexL, 0f, 0f);
                footR[i] = new Vector3((swing * legSwingDegrees * 0.5f) - flexR, 0f, 0f);

                armL[i] = new Vector3(-swing * armSwingDegrees, 0f, 5f);
                armR[i] = new Vector3(swing * armSwingDegrees, 0f, -5f);

                float bendL = 10f + (Mathf.Max(0f, -swing) * 25f);
                float bendR = 10f + (Mathf.Max(0f, swing) * 25f);
                forearmL[i] = new Vector3(-bendL, 0f, 0f);
                forearmR[i] = new Vector3(-bendR, 0f, 0f);

                torso[i] = new Vector3(
                    torsoLeanDegrees + (Mathf.Sin(phase * 2f) * torsoBobDegrees),
                    0f,
                    0f
                );
                head[i] = new Vector3(
                    -torsoLeanDegrees * HeadCounterLeanRatio,
                    Mathf.Sin(phase) * headSwayDegrees,
                    0f
                );
            }

            AddEulerCurve(clip, "Hips", times, hips);
            AddEulerCurve(clip, "Hips/LegL", times, legL);
            AddEulerCurve(clip, "Hips/LegR", times, legR);
            AddEulerCurve(clip, "Hips/LegL/FootL", times, footL);
            AddEulerCurve(clip, "Hips/LegR/FootR", times, footR);
            AddEulerCurve(clip, "Hips/Torso/ArmL", times, armL);
            AddEulerCurve(clip, "Hips/Torso/ArmR", times, armR);
            AddEulerCurve(clip, "Hips/Torso/ArmL/ForearmL", times, forearmL);
            AddEulerCurve(clip, "Hips/Torso/ArmR/ForearmR", times, forearmR);
            AddEulerCurve(clip, "Hips/Torso", times, torso);
            AddEulerCurve(clip, "Hips/Torso/Head", times, head);

            // Élimine les changements de signe de composante entre keyframes consécutives
            // (double-cover des quaternions) qui produiraient sinon un artefact d'interpolation
            // — sans effet ici tant que les amplitudes restent petites, mais coûte rien.
            clip.EnsureQuaternionContinuity();
            SetLoop(clip);
            return clip;
        }

        private static AnimationClip BuildIdleClip()
        {
            var clip = new AnimationClip { name = "CharacterIdle", frameRate = 30f };

            int n = SamplesPerLoop;
            var times = new float[n + 1];
            var armL = new Vector3[n + 1];
            var armR = new Vector3[n + 1];
            var forearmL = new Vector3[n + 1];
            var forearmR = new Vector3[n + 1];
            var torso = new Vector3[n + 1];
            var head = new Vector3[n + 1];

            float loopDuration = Mathf.PI * 2f;

            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n;
                float phase = t * loopDuration;
                times[i] = t * loopDuration;

                float idleArmSway = Mathf.Sin(phase) * 2f;
                armL[i] = new Vector3(idleArmSway, 0f, 5f);
                armR[i] = new Vector3(idleArmSway, 0f, -5f);
                forearmL[i] = new Vector3(-10f, 0f, 0f);
                forearmR[i] = new Vector3(-10f, 0f, 0f);
                torso[i] = new Vector3(Mathf.Sin(phase) * BreathDegrees, 0f, 0f);
                head[i] = new Vector3(0f, 0f, Mathf.Sin(phase) * 1.5f);
            }

            AddEulerCurve(clip, "Hips/Torso/ArmL", times, armL);
            AddEulerCurve(clip, "Hips/Torso/ArmR", times, armR);
            AddEulerCurve(clip, "Hips/Torso/ArmL/ForearmL", times, forearmL);
            AddEulerCurve(clip, "Hips/Torso/ArmR/ForearmR", times, forearmR);
            AddEulerCurve(clip, "Hips/Torso", times, torso);
            AddEulerCurve(clip, "Hips/Torso/Head", times, head);

            clip.EnsureQuaternionContinuity();
            SetLoop(clip);
            return clip;
        }

        // --- Clips de combat/minage ---------------------------------------------------------

        /// <summary>Pose du buste/tête/bras à un instant donné d'une action (voir BuildActionClip). Hips/jambes hors-scope : masqués par BuildUpperBodyMask, inutile de les animer ici.</summary>
        private readonly struct UpperBodyPose
        {
            public readonly Vector3 Torso;
            public readonly Vector3 Head;
            public readonly Vector3 ArmL;
            public readonly Vector3 ArmR;
            public readonly Vector3 ForearmL;
            public readonly Vector3 ForearmR;

            public UpperBodyPose(
                Vector3 torso = default,
                Vector3 head = default,
                Vector3 armL = default,
                Vector3 armR = default,
                Vector3 forearmL = default,
                Vector3 forearmR = default
            )
            {
                Torso = torso;
                Head = head;
                ArmL = armL;
                ArmR = armR;
                ForearmL = forearmL;
                ForearmR = forearmR;
            }
        }

        private readonly struct PoseKey
        {
            public readonly float NormalizedTime;
            public readonly UpperBodyPose Pose;

            public PoseKey(float normalizedTime, UpperBodyPose pose)
            {
                NormalizedTime = normalizedTime;
                Pose = pose;
            }
        }

        // Convention d'axes pour les OS QUI PENDENT (Arm/Leg, mesh le long de -Y) — la même
        // que Walk/Run : +X = ARRIÈRE (−Z), −X = AVANT (+Z). (Le Torso, lui, a son mesh le
        // long de +Y : +X = pencher en AVANT — d'où l'asymétrie apparente.) Se tromper de
        // signe ici envoie les coups dans le dos du personnage.
        //
        // Bras/avant-bras au repos (proche de la pose neutre d'Idle/Walk) : point de départ/
        // retour commun à toutes les actions, pour qu'elles s'enchaînent proprement avec la
        // pose de base sans à-coup visible à l'entrée/la sortie du layer Combat.
        private static readonly UpperBodyPose RestPose = new(
            armL: new Vector3(-10f, 0f, 5f),
            armR: new Vector3(-10f, 0f, -5f),
            forearmL: new Vector3(-15f, 0f, 0f),
            forearmR: new Vector3(-15f, 0f, 0f)
        );

        /// <summary>
        /// Construit un clip d'action À UN COUP (pas de bouclage, contrairement à Idle/Walk/
        /// Run) : courbes buste/tête/bras interpolées entre les <paramref name="keys"/>
        /// (temps normalisés 0..1 de <paramref name="durationSeconds"/>), plus les
        /// <paramref name="events"/> demandés (eux aussi en temps normalisé) ET un événement
        /// générique "OnActionEnd" baké juste avant la dernière frame — voir
        /// CharacterCombatAnimationEvents.OnActionEnd (retombée du poids du layer Combat).
        /// </summary>
        private static AnimationClip BuildActionClip(
            string clipName,
            float durationSeconds,
            PoseKey[] keys,
            params (float normalizedTime, string functionName)[] events
        )
        {
            var clip = new AnimationClip { name = clipName, frameRate = 30f };

            int n = keys.Length;
            var times = new float[n];
            var torso = new Vector3[n];
            var head = new Vector3[n];
            var armL = new Vector3[n];
            var armR = new Vector3[n];
            var forearmL = new Vector3[n];
            var forearmR = new Vector3[n];

            for (int i = 0; i < n; i++)
            {
                times[i] = keys[i].NormalizedTime * durationSeconds;
                torso[i] = keys[i].Pose.Torso;
                head[i] = keys[i].Pose.Head;
                armL[i] = keys[i].Pose.ArmL;
                armR[i] = keys[i].Pose.ArmR;
                forearmL[i] = keys[i].Pose.ForearmL;
                forearmR[i] = keys[i].Pose.ForearmR;
            }

            AddEulerCurve(clip, "Hips/Torso", times, torso);
            AddEulerCurve(clip, "Hips/Torso/Head", times, head);
            AddEulerCurve(clip, "Hips/Torso/ArmL", times, armL);
            AddEulerCurve(clip, "Hips/Torso/ArmR", times, armR);
            AddEulerCurve(clip, "Hips/Torso/ArmL/ForearmL", times, forearmL);
            AddEulerCurve(clip, "Hips/Torso/ArmR/ForearmR", times, forearmR);
            clip.EnsureQuaternionContinuity();

            clip.wrapMode = WrapMode.Once;
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var animEvents = new AnimationEvent[events.Length + 1];
            for (int i = 0; i < events.Length; i++)
            {
                animEvents[i] = new AnimationEvent
                {
                    time = events[i].normalizedTime * durationSeconds,
                    functionName = events[i].functionName,
                };
            }

            // Baké sur TOUTE action combat/minage, juste avant la dernière frame : signale au
            // pont (CharacterCombatAnimationEvents) la fin de la lecture pour redescendre le
            // poids du layer Combat à 0. Interrompu naturellement si un nouveau TriggerAction
            // enchaîne le combo avant la fin (l'ancien clip est abandonné, son événement ne se
            // déclenche jamais) : le poids reste à 1 en continu tout le temps du combo.
            animEvents[events.Length] = new AnimationEvent
            {
                time = Mathf.Max(0f, durationSeconds - (1f / clip.frameRate)),
                functionName = "OnActionEnd",
            };

            AnimationUtility.SetAnimationEvents(clip, animEvents);
            return clip;
        }

        private static AnimationClip BuildSwordDrawSlash1Clip() =>
            BuildActionClip(
                "SwordDrawSlash1",
                SwordDrawSlash1Duration,
                new[]
                {
                    new PoseKey(0f, RestPose),
                    // La main droite remonte chercher la garde dans le dos (socket Back,
                    // incliné à +35° — voir TorsoGenerator) : bras levé haut et en ARRIÈRE
                    // (+X), avant-bras plié pour atteindre la nuque.
                    new PoseKey(
                        0.32f,
                        new UpperBodyPose(
                            torso: new Vector3(6f, 12f, 0f),
                            armL: RestPose.ArmL,
                            armR: new Vector3(125f, 0f, -35f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-110f, 0f, 0f)
                        )
                    ),
                    // Coup : le bras redescend et fauche vers l'AVANT (−X), buste qui se
                    // déroule dans le sens du coup.
                    new PoseKey(
                        0.7f,
                        new UpperBodyPose(
                            torso: new Vector3(14f, -22f, 0f),
                            head: new Vector3(0f, -8f, 0f),
                            armL: RestPose.ArmL,
                            armR: new Vector3(-85f, -15f, -20f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-15f, 0f, 0f)
                        )
                    ),
                    // Retour vers une garde relâchée (épée en main, prête à enchaîner).
                    new PoseKey(
                        1f,
                        new UpperBodyPose(
                            torso: new Vector3(5f, -6f, 0f),
                            armL: RestPose.ArmL,
                            armR: new Vector3(-28f, -5f, -15f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-25f, 0f, 0f)
                        )
                    ),
                },
                (0.32f, "OnWeaponGrabbed"),
                (0.7f, "OnAttackHit")
            );

        private static AnimationClip BuildSwordSlash1Clip() =>
            BuildActionClip(
                "SwordSlash1",
                SwordSlashDuration,
                new[]
                {
                    // Épée déjà en main (pas de dégainer) : coup horizontal gauche -> droite.
                    new PoseKey(
                        0f,
                        new UpperBodyPose(
                            torso: new Vector3(4f, 18f, 0f),
                            armL: RestPose.ArmL,
                            armR: new Vector3(-20f, -45f, -10f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-30f, 0f, 0f)
                        )
                    ),
                    new PoseKey(
                        0.55f,
                        new UpperBodyPose(
                            torso: new Vector3(10f, -18f, 0f),
                            head: new Vector3(0f, -6f, 0f),
                            armL: RestPose.ArmL,
                            armR: new Vector3(-75f, 55f, -20f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-15f, 0f, 0f)
                        )
                    ),
                    new PoseKey(
                        1f,
                        new UpperBodyPose(
                            torso: new Vector3(4f, 10f, 0f),
                            armL: RestPose.ArmL,
                            armR: new Vector3(-30f, 20f, -12f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-22f, 0f, 0f)
                        )
                    ),
                },
                (0.55f, "OnAttackHit")
            );

        private static AnimationClip BuildSwordSlash2Clip() =>
            BuildActionClip(
                "SwordSlash2",
                SwordSlashDuration,
                new[]
                {
                    // Coup diagonal descendant, bras levé haut/arrière (+X) avant de faucher
                    // vers l'avant (−X).
                    new PoseKey(
                        0f,
                        new UpperBodyPose(
                            torso: new Vector3(-4f, -10f, 0f),
                            armL: RestPose.ArmL,
                            armR: new Vector3(65f, -12f, -30f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-40f, 0f, 0f)
                        )
                    ),
                    new PoseKey(
                        0.5f,
                        new UpperBodyPose(
                            torso: new Vector3(16f, 10f, 0f),
                            head: new Vector3(0f, 4f, 0f),
                            armL: RestPose.ArmL,
                            armR: new Vector3(-75f, -8f, -10f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-10f, 0f, 0f)
                        )
                    ),
                    new PoseKey(
                        1f,
                        new UpperBodyPose(
                            torso: new Vector3(5f, 4f, 0f),
                            armL: RestPose.ArmL,
                            armR: new Vector3(-25f, 0f, -12f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-20f, 0f, 0f)
                        )
                    ),
                },
                (0.5f, "OnAttackHit")
            );

        private static AnimationClip BuildSwordSlash3Clip() =>
            BuildActionClip(
                "SwordSlash3",
                SwordSlashDuration,
                new[]
                {
                    // Coup final : revers puissant, plus grande amplitude de torse (dernier
                    // coup du combo, le plus marqué).
                    new PoseKey(
                        0f,
                        new UpperBodyPose(
                            torso: new Vector3(6f, -28f, 0f),
                            armL: new Vector3(-20f, 0f, 15f),
                            armR: new Vector3(-15f, 50f, -15f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-35f, 0f, 0f)
                        )
                    ),
                    new PoseKey(
                        0.5f,
                        new UpperBodyPose(
                            torso: new Vector3(18f, 26f, 0f),
                            head: new Vector3(0f, -6f, 0f),
                            armL: new Vector3(-20f, 0f, 15f),
                            armR: new Vector3(-80f, -60f, -20f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-10f, 0f, 0f)
                        )
                    ),
                    new PoseKey(
                        1f,
                        new UpperBodyPose(
                            torso: new Vector3(6f, 8f, 0f),
                            armL: RestPose.ArmL,
                            armR: new Vector3(-28f, -20f, -14f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-22f, 0f, 0f)
                        )
                    ),
                },
                (0.5f, "OnAttackHit")
            );

        private static AnimationClip BuildSwordSheathClip() =>
            BuildActionClip(
                "SwordSheath",
                SwordSheathDuration,
                new[]
                {
                    new PoseKey(
                        0f,
                        new UpperBodyPose(
                            armL: RestPose.ArmL,
                            armR: new Vector3(-25f, 0f, -12f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-20f, 0f, 0f)
                        )
                    ),
                    // La main ramène l'épée dans le dos (même trajectoire que le dégainer,
                    // inversée) puis la relâche.
                    new PoseKey(
                        0.75f,
                        new UpperBodyPose(
                            torso: new Vector3(5f, 10f, 0f),
                            armL: RestPose.ArmL,
                            armR: new Vector3(120f, 0f, -32f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-105f, 0f, 0f)
                        )
                    ),
                    new PoseKey(1f, RestPose),
                },
                (0.85f, "OnWeaponSheathed")
            );

        private static AnimationClip BuildPunchJabLClip() =>
            BuildActionClip(
                "PunchJabL",
                PunchDuration,
                new[]
                {
                    new PoseKey(0f, RestPose),
                    // Jab gauche : extension rapide vers l'avant (−X), garde droite remonte.
                    new PoseKey(
                        0.5f,
                        new UpperBodyPose(
                            torso: new Vector3(6f, 10f, 0f),
                            armL: new Vector3(-70f, 0f, 8f),
                            armR: new Vector3(-25f, 0f, -15f),
                            forearmL: new Vector3(-5f, 0f, 0f),
                            forearmR: new Vector3(-35f, 0f, 0f)
                        )
                    ),
                    new PoseKey(1f, RestPose),
                },
                (0.5f, "OnAttackHit")
            );

        private static AnimationClip BuildPunchJabRClip() =>
            BuildActionClip(
                "PunchJabR",
                PunchDuration,
                new[]
                {
                    new PoseKey(0f, RestPose),
                    // Miroir du jab gauche.
                    new PoseKey(
                        0.5f,
                        new UpperBodyPose(
                            torso: new Vector3(6f, -10f, 0f),
                            armL: new Vector3(-25f, 0f, 15f),
                            armR: new Vector3(-70f, 0f, -8f),
                            forearmL: new Vector3(-35f, 0f, 0f),
                            forearmR: new Vector3(-5f, 0f, 0f)
                        )
                    ),
                    new PoseKey(1f, RestPose),
                },
                (0.5f, "OnAttackHit")
            );

        private static AnimationClip BuildPunchCrossClip() =>
            BuildActionClip(
                "PunchCross",
                PunchDuration,
                new[]
                {
                    // Cross : plus ample que les jabs, gros pivot de buste pour la puissance,
                    // garde gauche haute.
                    new PoseKey(
                        0f,
                        new UpperBodyPose(
                            torso: new Vector3(0f, -18f, 0f),
                            armL: new Vector3(-35f, 0f, 18f),
                            armR: new Vector3(-10f, 30f, -10f),
                            forearmL: new Vector3(-25f, 0f, 0f),
                            forearmR: new Vector3(-40f, 0f, 0f)
                        )
                    ),
                    new PoseKey(
                        0.5f,
                        new UpperBodyPose(
                            torso: new Vector3(10f, 20f, 0f),
                            armL: new Vector3(-35f, 0f, 18f),
                            armR: new Vector3(-85f, -15f, -12f),
                            forearmL: new Vector3(-25f, 0f, 0f),
                            forearmR: new Vector3(0f, 0f, 0f)
                        )
                    ),
                    new PoseKey(1f, RestPose),
                },
                (0.5f, "OnAttackHit")
            );

        private static AnimationClip BuildPickaxeDrawSwingClip() =>
            BuildActionClip(
                "PickaxeDrawSwing",
                PickaxeDrawSwingDuration,
                new[]
                {
                    new PoseKey(0f, RestPose),
                    // Main droite remonte chercher le manche dans le dos (socket BackTool,
                    // incliné à -35°, à l'opposé de l'épée) : +X = arrière.
                    new PoseKey(
                        0.28f,
                        new UpperBodyPose(
                            torso: new Vector3(6f, 10f, 0f),
                            armL: RestPose.ArmL,
                            armR: new Vector3(118f, 0f, -32f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-100f, 0f, 0f)
                        )
                    ),
                    // Levée complète au-dessus de l'épaule (préparation du coup, +X).
                    new PoseKey(
                        0.55f,
                        new UpperBodyPose(
                            torso: new Vector3(-10f, 6f, 0f),
                            armL: new Vector3(-20f, 0f, 10f),
                            armR: new Vector3(165f, 0f, -15f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-25f, 0f, 0f)
                        )
                    ),
                    // Frappe : grand coup vertical vers le bas/l'avant (−X).
                    new PoseKey(
                        0.8f,
                        new UpperBodyPose(
                            torso: new Vector3(22f, 0f, 0f),
                            head: new Vector3(0f, 0f, 0f),
                            armL: new Vector3(-15f, 0f, 10f),
                            armR: new Vector3(-60f, 0f, -15f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-10f, 0f, 0f)
                        )
                    ),
                    new PoseKey(
                        1f,
                        new UpperBodyPose(
                            torso: new Vector3(8f, 0f, 0f),
                            armL: RestPose.ArmL,
                            armR: new Vector3(-28f, 0f, -12f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-15f, 0f, 0f)
                        )
                    ),
                },
                (0.28f, "OnToolGrabbed"),
                (0.8f, "OnMineHit")
            );

        private static AnimationClip BuildPickaxeSwingClip() =>
            BuildActionClip(
                "PickaxeSwing",
                PickaxeSwingDuration,
                new[]
                {
                    // Pioche déjà en main : cycle levée (+X) -> frappe (−X) sans redégainer.
                    new PoseKey(
                        0f,
                        new UpperBodyPose(
                            torso: new Vector3(-8f, 4f, 0f),
                            armL: new Vector3(-18f, 0f, 10f),
                            armR: new Vector3(140f, 0f, -12f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-20f, 0f, 0f)
                        )
                    ),
                    new PoseKey(
                        0.6f,
                        new UpperBodyPose(
                            torso: new Vector3(20f, 0f, 0f),
                            armL: new Vector3(-15f, 0f, 10f),
                            armR: new Vector3(-55f, 0f, -12f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-10f, 0f, 0f)
                        )
                    ),
                    new PoseKey(
                        1f,
                        new UpperBodyPose(
                            torso: new Vector3(6f, 0f, 0f),
                            armL: RestPose.ArmL,
                            armR: new Vector3(-25f, 0f, -12f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-15f, 0f, 0f)
                        )
                    ),
                },
                (0.6f, "OnMineHit")
            );

        private static AnimationClip BuildPickaxeSheathClip() =>
            BuildActionClip(
                "PickaxeSheath",
                PickaxeSheathDuration,
                new[]
                {
                    new PoseKey(
                        0f,
                        new UpperBodyPose(
                            armL: RestPose.ArmL,
                            armR: new Vector3(-25f, 0f, -12f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-15f, 0f, 0f)
                        )
                    ),
                    // Ramène la pioche dans le dos (socket BackTool) puis relâche la prise.
                    new PoseKey(
                        0.75f,
                        new UpperBodyPose(
                            torso: new Vector3(5f, 8f, 0f),
                            armL: RestPose.ArmL,
                            armR: new Vector3(115f, 0f, -30f),
                            forearmL: RestPose.ForearmL,
                            forearmR: new Vector3(-98f, 0f, 0f)
                        )
                    ),
                    new PoseKey(1f, RestPose),
                },
                (0.85f, "OnToolSheathed")
            );

        private static void SetLoop(AnimationClip clip)
        {
            clip.wrapMode = WrapMode.Loop;
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        // Quaternion component curves (localRotation.x/y/z/w) plutôt que des courbes d'Euler :
        // c'est ce que Unity applique réellement à un Transform.localRotation générique, sans
        // ambiguïté d'ordre d'axes ni de repli sur un Avatar humanoïde. Unity renormalise le
        // quaternion résultant à la lecture, donc l'interpolation composante par composante
        // reste stable pour les faibles amplitudes utilisées ici.
        private static void AddEulerCurve(
            AnimationClip clip,
            string path,
            float[] times,
            Vector3[] eulers
        )
        {
            var x = new AnimationCurve();
            var y = new AnimationCurve();
            var z = new AnimationCurve();
            var w = new AnimationCurve();

            for (int i = 0; i < times.Length; i++)
            {
                Quaternion q = Quaternion.Euler(eulers[i]);
                x.AddKey(times[i], q.x);
                y.AddKey(times[i], q.y);
                z.AddKey(times[i], q.z);
                w.AddKey(times[i], q.w);
            }

            for (int i = 0; i < times.Length; i++)
            {
                x.SmoothTangents(i, 0f);
                y.SmoothTangents(i, 0f);
                z.SmoothTangents(i, 0f);
                w.SmoothTangents(i, 0f);
            }

            clip.SetCurve(path, typeof(Transform), "localRotation.x", x);
            clip.SetCurve(path, typeof(Transform), "localRotation.y", y);
            clip.SetCurve(path, typeof(Transform), "localRotation.z", z);
            clip.SetCurve(path, typeof(Transform), "localRotation.w", w);
        }

        private static AnimationClip SaveOrReplaceClip(AnimationClip clip, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(clip, existing);
                return existing;
            }

            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        // --- Controller --------------------------------------------------------------------

        private static void BuildController(
            AnimationClip idle,
            AnimationClip walk,
            AnimationClip run,
            AnimationClip swordDraw,
            AnimationClip swordSlash1,
            AnimationClip swordSlash2,
            AnimationClip swordSlash3,
            AnimationClip swordSheath,
            AnimationClip punchJabL,
            AnimationClip punchJabR,
            AnimationClip punchCross,
            AnimationClip pickaxeDraw,
            AnimationClip pickaxeSwing,
            AnimationClip pickaxeSheath
        )
        {
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                ControllerPath
            );
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(
                ControllerPath
            );
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            // Second paramètre, distinct de "Speed" (qui pilote le poids du Blend Tree) : la
            // cadence de lecture réelle. Un perso qui se déplace à 10-11 m/s (sprint par défaut,
            // voir PlayerController) mais dont le cycle Walk/Run boucle en 1s pile (peu importe
            // la vitesse) donne l'impression de flotter/glisser — les jambes n'avancent pas assez
            // vite pour la distance parcourue. CharacterLocomotionAnimator calcule cette valeur
            // côté script (jamais < 1x, pour ne jamais figer la respiration d'Idle à vitesse
            // nulle) et l'assigne au state ci-dessous, PAS "Speed" directement.
            controller.AddParameter("PlaybackSpeed", AnimatorControllerParameterType.Float);

            // Layer 1 "Combat" (voir CharacterCombatAnimationEvents.TriggerAction) : un unique
            // Trigger partagé + un Int sélecteur, pas un paramètre par action — sinon N clips
            // = N paramètres Trigger à gérer côté script pour un seul bénéfice (éviter un Int),
            // largement moins pratique.
            controller.AddParameter("ActionId", AnimatorControllerParameterType.Int);
            controller.AddParameter("ActionTrigger", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

            // Un seul état portant un Blend Tree 1D plutôt que des états Idle/Walk discrets +
            // transitions à seuil : le mélange est continu sur toute la plage de "Speed", donc
            // l'inclinaison avant et l'amplitude de foulée grandissent progressivement avec la
            // vitesse au lieu de sauter d'un cran à l'autre (c'est ça qui rendait le sprint
            // "mécanique" — Walk rejoué plus vite via AnimatorState.speed, sans blend réel).
            // CreateBlendTreeInController crée l'état ET le Blend Tree ensemble et l'enregistre
            // comme sous-asset du controller (voir doc Unity) — pas besoin d'AddState séparé.
            AnimatorState locomotionState = controller.CreateBlendTreeInController(
                "Locomotion",
                out BlendTree blendTree
            );
            blendTree.blendType = BlendTreeType.Simple1D;
            blendTree.blendParameter = "Speed";
            blendTree.useAutomaticThresholds = false;
            blendTree.AddChild(idle, 0f);
            blendTree.AddChild(walk, WalkBlendSpeed);
            blendTree.AddChild(run, RunBlendSpeed);

            // La cadence de lecture du Blend Tree entier (Idle compris, mais son amplitude est
            // nulle à vitesse de marche/course donc invisible) suit "PlaybackSpeed" — voir
            // CharacterLocomotionAnimator.CadenceReferenceSpeed pour le calcul côté script (doit
            // rester cohérent avec WalkBlendSpeed ci-dessus, les deux fichiers vivant dans des
            // assemblées séparées qui ne peuvent pas partager une constante).
            locomotionState.speedParameterActive = true;
            locomotionState.speedParameter = "PlaybackSpeed";

            stateMachine.defaultState = locomotionState;

            BuildCombatLayer(
                controller,
                swordDraw,
                swordSlash1,
                swordSlash2,
                swordSlash3,
                swordSheath,
                punchJabL,
                punchJabR,
                punchCross,
                pickaxeDraw,
                pickaxeSwing,
                pickaxeSheath
            );
        }

        /// <summary>
        /// Restreint l'effet du layer Combat au buste/tête/bras : le bas du corps (Hips, jambes,
        /// pieds) reste piloté à 100% par le layer Locomotion, MÊME PENDANT une attaque/le
        /// minage (voir le plan — le joueur continue de marcher/courir en attaquant). Rig
        /// générique (pas d'Avatar humanoïde) : le masque se construit par chemin de transform
        /// (SetTransformPath/SetTransformActive), pas par AvatarMaskBodyPart.
        /// </summary>
        private static AvatarMask BuildUpperBodyMask()
        {
            var mask = new AvatarMask { name = "CombatUpperBodyMask" };

            (string path, bool active)[] entries =
            {
                ("", false),
                ("Hips", false),
                ("Hips/Torso", true),
                ("Hips/Torso/Head", true),
                ("Hips/Torso/ArmL", true),
                ("Hips/Torso/ArmL/ForearmL", true),
                ("Hips/Torso/ArmR", true),
                ("Hips/Torso/ArmR/ForearmR", true),
                ("Hips/LegL", false),
                ("Hips/LegL/FootL", false),
                ("Hips/LegR", false),
                ("Hips/LegR/FootR", false),
            };

            mask.transformCount = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                mask.SetTransformPath(i, entries[i].path);
                mask.SetTransformActive(i, entries[i].active);
            }

            // Aucune "body part" humanoïde (rig générique) : tout passe par le masque de
            // transforms ci-dessus, désactiver explicitement évite tout comportement implicite.
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            }

            return mask;
        }

        private static void BuildCombatLayer(
            AnimatorController controller,
            AnimationClip swordDraw,
            AnimationClip swordSlash1,
            AnimationClip swordSlash2,
            AnimationClip swordSlash3,
            AnimationClip swordSheath,
            AnimationClip punchJabL,
            AnimationClip punchJabR,
            AnimationClip punchCross,
            AnimationClip pickaxeDraw,
            AnimationClip pickaxeSwing,
            AnimationClip pickaxeSheath
        )
        {
            controller.AddLayer("Combat");

            AnimatorControllerLayer[] layers = controller.layers;
            AnimatorControllerLayer combatLayer = layers[1];

            AvatarMask mask = BuildUpperBodyMask();
            AssetDatabase.AddObjectToAsset(mask, controller);
            combatLayer.avatarMask = mask;
            // Piloté par script (CharacterCombatAnimationEvents.TriggerAction/OnActionEnd), pas
            // par l'Animator lui-même : 0 hors combat, jamais de poids par défaut ici.
            combatLayer.defaultWeight = 0f;

            AnimatorStateMachine combatSm = combatLayer.stateMachine;
            AnimatorState combatIdle = combatSm.AddState("CombatIdle");
            combatSm.defaultState = combatIdle;

            // Valeurs en dur, SYNCHRONISÉES avec CubeWorld.Player.CharacterModel.CombatActionId
            // (voir le commentaire de tête de fichier) : 0-4 épée, 5-7 poings, 8-10 pioche.
            AddCombatState(combatSm, combatIdle, "SwordDrawSlash1", swordDraw, 0);
            AddCombatState(combatSm, combatIdle, "SwordSlash1", swordSlash1, 1);
            AddCombatState(combatSm, combatIdle, "SwordSlash2", swordSlash2, 2);
            AddCombatState(combatSm, combatIdle, "SwordSlash3", swordSlash3, 3);
            AddCombatState(combatSm, combatIdle, "SwordSheath", swordSheath, 4);
            AddCombatState(combatSm, combatIdle, "PunchJabL", punchJabL, 5);
            AddCombatState(combatSm, combatIdle, "PunchJabR", punchJabR, 6);
            AddCombatState(combatSm, combatIdle, "PunchCross", punchCross, 7);
            AddCombatState(combatSm, combatIdle, "PickaxeDrawSwing", pickaxeDraw, 8);
            AddCombatState(combatSm, combatIdle, "PickaxeSwing", pickaxeSwing, 9);
            AddCombatState(combatSm, combatIdle, "PickaxeSheath", pickaxeSheath, 10);

            layers[1] = combatLayer;
            controller.layers = layers;
        }

        /// <summary>
        /// Une transition Any State -> <paramref name="clip"/> conditionnée par
        /// ActionId == <paramref name="actionId"/> ET ActionTrigger, plus une transition de
        /// sortie vers <paramref name="combatIdle"/> après l'Exit Time (fin du clip, pas de
        /// bouclage — voir BuildActionClip). canTransitionToSelf=true : un même actionId
        /// re-déclenché pendant que son propre clip joue encore (ex. presser Miner en rafale)
        /// relance l'animation depuis le début au lieu d'être ignoré.
        /// </summary>
        private static void AddCombatState(
            AnimatorStateMachine stateMachine,
            AnimatorState combatIdle,
            string name,
            AnimationClip clip,
            int actionId
        )
        {
            AnimatorState state = stateMachine.AddState(name);
            state.motion = clip;
            state.writeDefaultValues = true;

            AnimatorStateTransition enter = stateMachine.AddAnyStateTransition(state);
            enter.hasExitTime = false;
            enter.hasFixedDuration = true;
            enter.duration = 0.08f;
            enter.canTransitionToSelf = true;
            enter.AddCondition(AnimatorConditionMode.Equals, actionId, "ActionId");
            enter.AddCondition(AnimatorConditionMode.If, 0f, "ActionTrigger");

            AnimatorStateTransition exit = state.AddTransition(combatIdle);
            exit.hasExitTime = true;
            exit.exitTime = 1f;
            exit.hasFixedDuration = true;
            exit.duration = 0.1f;
        }
    }
}
