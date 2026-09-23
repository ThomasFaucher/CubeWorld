# CharacterModel (intégration joueur)

Le pipeline de génération de personnage voxel (grilles → meshes → rig) vit dans sa propre
assemblée, **`CubeWorld.CharacterModel`** (`Assets/_Project/Scripts/CharacterModel/`), partagée
avec les ennemis (`CubeWorld.Combat.EnemyAI`) — voir son README pour le pipeline complet
(`CharacterModelBuilder`, `CharacterModelRoot`, silhouettes, palettes...).

Ce dossier-ci ne contient plus que la **glue spécifique au joueur**, qui n'a pas de sens pour un
ennemi et reste donc dans l'assemblée `CubeWorld.Player` :

- **`CharacterLocomotionAnimator.cs`** — pose un vrai `Animator` sur la racine du personnage,
  avec un `RuntimeAnimatorController` chargé via `Resources.Load` (le personnage étant assemblé
  en code, il n'y a pas de prefab où assigner ce champ dans l'inspecteur). Le controller n'a
  qu'un seul état ("Locomotion") portant un Blend Tree 1D qui mélange en continu trois clips
  (Idle/Walk/Run) selon le paramètre float `"Speed"` — Run est un cycle à part (foulée/swing de
  bras plus amples, inclinaison avant du buste plus marquée), pas `Walk` simplement rejoué plus
  vite. Ajouté par `PlayerController.CreateVoxelVisual()` juste après
  `CharacterModelBuilder.Build(...)`. Ne fait QUE nourrir, chaque frame (avec un léger damping
  via `Animator.SetFloat(..., dampTime, deltaTime)`, la vitesse du `CharacterController` sautant
  instantanément entre marche/sprint côté `PlayerMotor`), deux paramètres : `"Speed"` (poids du
  Blend Tree) et `"PlaybackSpeed"` (cadence de lecture, `Mathf.Max(1f, Speed /
  CadenceReferenceSpeed)` — jamais < 1x pour ne pas figer Idle à l'arrêt, accélère au-delà pour
  qu'un sprint plus rapide que le cycle baked ne glisse pas visuellement) ; toute la logique
  d'animation (courbes, inclinaison avant, seuils du Blend Tree) vit dans les assets sous
  `Assets/_Project/Resources/Animations/CharacterModel/`, générés par l'outil éditeur
  `CubeWorld.EditorTools.CharacterLocomotionAnimatorGenerator`
  (`Assets/_Project/Editor/CharacterLocomotionAnimatorGenerator.cs`, menu
  `CubeWorld/Character/Generate Locomotion Animator`) — ne jamais éditer ces assets à la main
  dans l'éditeur Unity, le prochain passage de l'outil les écraserait.
- **`PlayerGearVisual.cs`** — reflète visuellement l'arme ET l'outil réellement équipés
  (`PlayerEquipment.CurrentWeapon`/`CurrentTool`) : écoute `PlayerEquipment.EquipmentChanged` et
  monte/démonte les meshes correspondants (`CubeWorld.CharacterModel.Parts.GearGenerator`) sur
  `CharacterModelRoot.GearBackAnchor`/`ToolBackAnchor`, selon `WeaponDefinition.VisualKind`/
  `ToolDefinition.VisualKind`. S'abonne aussi à `CharacterCombatAnimationEvents`
  (`WeaponGrabbed`/`WeaponSheathed`/`ToolGrabbed`/`ToolSheathed`) pour déplacer RÉELLEMENT le mesh
  entre le dos et `CharacterModelRoot.WeaponGripAnchor` pendant une action (dégainer/ranger), au
  lieu de le garder fixe dans le dos. Bindé par `PlayerBootstrap` juste après la création du
  personnage et de l'équipement.
- **`CharacterCombatAnimationEvents.cs`** — pont entre le layer Animator "Combat" (voir le README
  de `CubeWorld.CharacterModel` pour le générateur) et le gameplay : posé sur la racine du modèle
  voxel (là où vit l'`Animator`, pas le joueur — un `AnimationEvent` ne peut appeler qu'un
  composant du GameObject qui porte l'`Animator`), reçoit les événements bakés dans les clips de
  combat/minage (`OnWeaponGrabbed`/`OnAttackHit`/`OnWeaponSheathed`/`OnToolGrabbed`/`OnMineHit`/
  `OnToolSheathed`) et les republie en événements C# pour `PlayerCombat`/`PlayerMining`/
  `PlayerGearVisual`. `TriggerAction(actionId)` (voir `CombatActionId.cs`) monte le poids du layer
  Combat à 1 et déclenche la transition Any State correspondante ; l'événement générique
  `OnActionEnd` (baké près de la fin de chaque clip) le redescend à 0.

Pourquoi ces fichiers ne sont-ils pas dans `CubeWorld.CharacterModel` ? Parce qu'ils dépendent de
types propres au joueur (`CharacterController`, `PlayerEquipment`, `WeaponDefinition`,
`ToolDefinition`) que le pipeline de génération ne connaît pas et ne doit pas connaître — un
ennemi n'a ni `CharacterController` ni inventaire d'équipement. Garder cette séparation permet à
`CubeWorld.Combat` de référencer `CubeWorld.CharacterModel` sans jamais dépendre de
`CubeWorld.Player` (qui, lui, dépend de `CubeWorld.Combat` pour l'équipement/les items).
