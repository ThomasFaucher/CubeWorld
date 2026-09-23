# CharacterModel — personnage voxel chibi façon CubeWorld

Ce dossier (assemblée **`CubeWorld.CharacterModel`**, référencée par `CubeWorld.Player` ET
`CubeWorld.Combat`) contient **tout** le pipeline qui fabrique l'apparence visuelle d'un
personnage généré : il n'y a **aucune texture, aucun sprite, aucun modèle 3D importé**. Le
personnage (tête, torse, bras, jambes...) est entièrement **généré en code** sous forme de
petites grilles de voxels colorés, converties en meshes, puis assemblées en hiérarchie de
`Transform` animable.

Ce pipeline est **partagé entre le joueur et les ennemis** : `CubeWorld.Player.PlayerController`
l'utilise pour le personnage jouable (voir aussi `PlayerGearVisual`/`CharacterLocomotionAnimator`
dans `Assets/_Project/Scripts/Player/CharacterModel/`, qui restent dans l'assemblée `Player` car
spécifiques au joueur), et `CubeWorld.Combat.EnemyAI` l'utilise pour générer les ennemis (voir
`EnemyDefinition.Archetype`) — au lieu de la capsule colorée placeholder d'origine. Aucun des deux
appelants n'a besoin de connaître les détails internes : seule l'API publique de ce dossier
compte (`CharacterModelBuilder.Build`, `CharacterModelRoot`, `CharacterArchetype`,
`Generation.CharacterExpression`, et — pour du code qui doit monter une pièce supplémentaire
après coup comme `PlayerGearVisual` — `CharacterModelRoot.Palette`/`Unit`/`SharedMaterial`,
`Rig.BodyPart`, `Rig.CharacterAssembler.AttachToAnchor`, `Parts.GearGenerator`).

L'arme affichée (épée...) sur le joueur n'en fait PAS partie : c'est un élément à part, monté/
démonté dynamiquement par `Player.CharacterModel.PlayerGearVisual` d'après l'équipement du joueur
(voir §5).

> Règle d'or : **même seed → même personnage**, à l'exception de
> l'expression du visage qui est choisie indépendamment.

## 1. Vue d'ensemble du pipeline

```
archetype (CharacterArchetype)
  └─ CharacterSilhouetteCatalog.Get()      (Generation/CharacterSilhouetteCatalog.cs) → forme FIGÉE (pas de rng)

seed (int)
  └─ CharacterRng                         (Generation/CharacterRng.cs)
       ├─ CharacterPalette.Generate(rng, archetype) (Generation/CharacterPalette.cs) → toutes les couleurs
       ├─ CharacterBodyParams.Generate()  (Generation/CharacterBodyParams.cs) → proportions + coiffure
       └─ HeadShape.Generate()            (Generation/HeadShape.cs)          → galbe de la tête

CharacterModelBuilder.Build(...)          (CharacterModelBuilder.cs)         ← point d'entrée public
  ├─ HipsGenerator.Build            → pièce "Hips"   (bassin/pantalon/ceinture)
  ├─ TorsoGenerator.Build           → pièce "Torso"  (tunique/robe/baudrier OU cage thoracique, selon la silhouette)
  ├─ HeadGenerator.Build            → pièce "Head"   (crâne/visage/cheveux/couvre-tête/oreilles, selon la silhouette)
  ├─ ArmGenerator.BuildUpperArm x2  → pièces "ArmL"/"ArmR"      (épaulière selon la silhouette)
  ├─ ArmGenerator.BuildForearm x2   → pièces "ForearmL"/"ForearmR"
  ├─ LegGenerator.BuildLeg x2       → pièces "LegL"/"LegR"
  └─ LegGenerator.BuildFoot x2      → pièces "FootL"/"FootR"
  (GearGenerator.BuildSword n'est PAS appelé ici : seule une ancre vide,
   "GearBackAnchor", est posée sur le socket "Back" du torse — voir §5)

CharacterAssembler.Attach(...)            (Rig/CharacterAssembler.cs)
  → un GameObject (MeshFilter+MeshRenderer) par pièce, posé sur le socket du parent

CharacterModelRoot (MonoBehaviour)        (CharacterModelRoot.cs)
  → expose les Transforms des os + palette/unité/matériau pour l'animation / le gameplay
```

Point d'entrée public appelé par les deux consommateurs cross-assemblée :

- **Joueur** : `PlayerController.CreateVoxelVisual()` (`Assets/_Project/Scripts/Player/PlayerController.cs`)
  appelle `CharacterModelBuilder.Build(_visualHeight, archetype, seed, _debugExpression)`, puis
  ajoute `Player.CharacterModel.CharacterLocomotionAnimator` par-dessus (Animator Controller réel,
  pas une animation par rotation calculée main). L'arme, elle, est
  branchée séparément par `PlayerBootstrap` via `PlayerGearVisual.Bind(equipment, player.CharacterModel)`.
- **Ennemis** : `EnemyAI.Initialize()` (`Assets/_Project/Scripts/Combat/EnemyAI.cs`) appelle
  `CharacterModelBuilder.Build(definition.Height, definition.Archetype, seed)` (pas d'animation
  procédurale pour l'instant — voir §5).

## 2. Arborescence du dossier

```
CharacterModel/                    (assemblée CubeWorld.CharacterModel)
├── CharacterArchetype.cs         Enum public : Swordsman/Archer/Mage/Elf/Skeleton
├── CharacterModelBuilder.cs      Point d'entrée : assemble un personnage complet
├── CharacterModelRoot.cs         Composant posé sur la racine : accès aux Transforms/os/palette
│
├── Core/                         Brique bas niveau, réutilisable pour N'IMPORTE QUELLE pièce
│   ├── VoxelGrid.cs              Grille dense Color32[,,] d'UNE pièce (alpha 0 = vide)
│   ├── VoxelShape.cs             Primitives : Box, Column, Sphere, Dome, ConeUp, ConeDown
│   ├── VoxelStamper.cs           Tamponne une primitive (couleur unie ou par-voxel) dans une grille
│   ├── VoxelPartMeshBuilder.cs   Grille + pivot voxel + unité monde → façade pratique pour un générateur
│   └── VoxelGridMesher.cs        Grille complète → Mesh (face culling, AO vertex, jitter de couleur)
│
├── Generation/                   Tirage aléatoire (seed → paramètres) + presets figés, AUCUN mesh ici
│   ├── CharacterRng.cs           Random déterministe par seed (NextFloat/NextInt/Pick/NextBool)
│   ├── CharacterSilhouette.cs    Flags de FORME (oreilles, cheveux/couvre-tête, yeux, blush, torse, épaulières)
│   ├── CharacterSilhouetteCatalog.cs Presets FIGÉS (pas de rng) de CharacterSilhouette par CharacterArchetype
│   ├── CharacterPalette.cs       Toutes les couleurs du personnage, par archétype (règle de contraste, voir §4)
│   ├── CharacterBodyParams.cs    Proportions (tête, torse, membres) + HairStyle
│   ├── HeadShape.cs              Largeur de chaque rangée de la tête + rangées repères du visage
│   ├── CharacterExpression.cs    Enum public (Neutral/Happy/Angry/Sad/Surprised/Blink)
│   └── ExpressionShape.cs        Traduit une expression en paramètres de dessin (yeux/bouche/sourcils)
│
├── Parts/                        Un générateur par partie du corps : grille → BodyPart
│   ├── HeadGenerator.cs          Crâne squircle, visage peint, oreilles pointues, cheveux, couvre-tête
│   ├── TorsoGenerator.cs         Tunique tonneau / robe ample / cage thoracique selon la silhouette
│   ├── HipsGenerator.cs          Bassin/jupe, ceinture+boucle, "plug" caché dans la taille du torse
│   ├── ArmGenerator.cs           Bras haut (capsule + épaulière) et avant-bras (manche + moufle)
│   ├── LegGenerator.cs           Jambe (colonne) et pied (bottine)
│   └── GearGenerator.cs          Épée portée dans le dos — PAS appelé par CharacterModelBuilder
│                                 (public : appelé cross-assemblée par PlayerGearVisual, voir §5)
│
└── Rig/                          Squelette / assemblage, indépendant du contenu des pièces
    ├── BodyPart.cs               Résultat d'un générateur : Mesh + sockets nommés (public, voir §5)
    ├── PartSocket.cs             Point d'ancrage (position+rotation locales) offert à un enfant
    ├── CharacterRigDefinition.cs Noms STABLES des os et des sockets, dont GearBackAnchor
    └── CharacterAssembler.cs     Instancie les GameObjects, pose chaque pièce sur son socket, et
                                  expose CreateAnchor/AttachToAnchor pour l'équipement dynamique (public, voir §5)
```

Le reste de l'intégration joueur (glue spécifique, PAS réutilisable pour les ennemis) vit dans
l'assemblée `CubeWorld.Player`, sous `Assets/_Project/Scripts/Player/CharacterModel/` :
`CharacterLocomotionAnimator.cs` (pilote un Animator Controller réel via le paramètre float
"Speed") et `PlayerGearVisual.cs` (arme équipée) — voir le README de ce sous-dossier.

## 3. Concepts clés

### 3.1 Une pièce = une grille voxel, meshée une seule fois

Chaque générateur de `Parts/` :
1. crée une `VoxelPartMeshBuilder` (taille de grille + position du **pivot** en coordonnées voxel) ;
2. tamponne dedans une ou plusieurs primitives via `VoxelStamper.Stamp(...)` (couleur unie, ou
   fonction `(x,y,z) → couleur` pour peindre un motif à la volée, ex. le visage) ;
3. appelle `part.ToMesh(...)` : **toute la grille est meshée en une passe**, donc les faces internes
   entre deux primitives qui se touchent (ex. les deux sphères de la capsule d'un bras) sont
   correctement supprimées — c'est la raison d'être de `VoxelGrid` (voir le commentaire en tête du
   fichier pour le contraste avec l'ancienne approche par primitive isolée).

Le pivot passé à `VoxelPartMeshBuilder` devient l'**origine du mesh** : c'est ce qui permet d'animer
une pièce par simple rotation de son `Transform`, sans skinning.

### 3.2 Rig : sockets + assemblage

- `BodyPart` = un mesh + un dictionnaire de `PartSocket` nommés (ex. le torse expose `Neck`,
  `ShoulderL`, `ShoulderR`, `Back`, `BackTool` ; l'avant-bras droit expose `Grip` — aucun d'eux
  n'est utilisé par une pièce du personnage lui-même : `CharacterModelBuilder.Build` y pose des
  ancres vides permanentes (`CharacterRigDefinition.GearBackAnchor`/`ToolBackAnchor`/
  `WeaponGripAnchor`) sur lesquelles `PlayerGearVisual` du joueur monte/démonte le mesh d'arme/
  d'outil au fil des changements d'équipement (dos), et le reparente temporairement vers
  `WeaponGripAnchor` (main) pendant une action de combat/minage — voir §5).
- `CharacterAssembler.Attach(part, parent, socket, material)` crée le `GameObject`, le pose
  exactement sur le socket du parent, et devient lui-même le nouveau parent pour l'étage suivant.
- Convention : le mesh d'une pièce est TOUJOURS généré avec son point d'attache à l'origine. Ça
  veut dire que **n'importe quelle tête générée peut s'attacher à n'importe quel torse** qui expose
  un socket `Neck`, tant que les deux respectent cette convention — c'est la porte d'entrée pour un
  futur système de pièces interchangeables (voir §6).

### 3.3 Palette : règle de contraste de valeur (important, ne pas casser)

`CharacterPalette.Generate(rng, archetype)` ne tire pas des couleurs au hasard total : il respecte
une règle assumée en 3 familles de valeur + 1 accent (documentée en tête du fichier), déclinée par
archétype. Les quatre archétypes humanoïdes (`Swordsman`/`Archer`/`Mage`/`Elf`) partagent un seul
générateur (`GenerateHumanoid`) — peau/cheveux/yeux/cuir/lame identiques, seules la teinte
identitaire et l'accent changent, via un `OutfitScheme` par archétype (`SwordsmanScheme`/
`ArcherScheme`/`MageScheme`/`ElfScheme`) ; `GenerateSkeleton` reste un générateur à part (peau →
os, pas de cheveux/blush) :

| Famille | Swordsman | Archer | Mage | Elf | Squelette | Exemples de champs |
|---|---|---|---|---|---|---|
| **Sombre** | cuir quasi noir | cuir quasi noir | cuir quasi noir | cuir quasi noir | cuir décomposé quasi noir | `Belt`, `LeatherLight`, `Boot`, `BootSole` |
| **Clair** | blanc cassé | blanc cassé | blanc cassé | blanc cassé | os pâle | `OutfitTrim`, `Pants`, `Skin` |
| **Moyen (identitaire)** | vert tunique | olive/brun rôdeur | bleu-violet arcaniste | émeraude/sarcelle | pagne/bandage grisâtre | `OutfitPrimary`, `OutfitSecondary` |
| **Accent isolé** | or saturé | bronze terni | violet-magenta arcanique | argent pâle | rouille/bronze terni | `Emblem` |

C'est cette alternance qui donne le relief "CubeWorld" à un shading plat sans texture. **Toute
nouvelle palette (nouvel archétype) doit respecter cette règle**, sinon le rendu "lit" comme un
aplat quel que soit l'éclairage (voir le commentaire du fichier pour le détail).

### 3.4 Proportions chibi

`CharacterBodyParams.Generate()` tire des proportions dans des **fourchettes resserrées** (pas de
plage large) : tête large de 14 à 18 voxels (~1/3 de la hauteur hors couvre-tête), membres épais.
Le but est que la silhouette chibi reste reconnaissable d'un seed à l'autre — seuls les gabarits
fins varient.

### 3.5 Noms stables du rig (`CharacterRigDefinition`)

```
CharacterRoot
└── Hips
    ├── Torso
    │   ├── Head
    │   ├── ArmL ── ForearmL
    │   └── ArmR ── ForearmR
    ├── LegL ── FootL
    └── LegR ── FootR
```

Ces noms (et les noms de sockets `Torso`, `Neck`, `ShoulderL/R`, `Elbow`, `HipL/R`, `Ankle`, `Back`)
**ne doivent jamais changer** : un futur Animator Controller retrouve ses courbes par chemin de
Transform (`"Hips/Torso/ArmL"`), donc renommer casserait toutes les animations créées dans l'éditeur.

### 3.6 Silhouette : flags de forme figés par archétype

`CharacterSilhouette` (`Generation/CharacterSilhouette.cs`) porte les flags qui changent la
**forme** (pas la couleur, voir §3.3) : `HasPointedEars`, `HasHair`, `Cap` (`CapStyle.None`/
`Pointed`/`Wizard`), `HasEyes`, `HasBlush`, `HasSkullFace`, `Torso` (`TorsoStyle.Solid`/`Ribcage`/
`Robe`), `HasPauldrons`. `CharacterSilhouetteCatalog.Get(archetype)` résout un preset FIGÉ (pas de
`CharacterRng` — un archétype a toujours la même silhouette, seules les couleurs/proportions
varient par seed) :

| Archétype | Oreilles pointues | Couvre-tête | Torse | Épaulières |
|---|---|---|---|---|
| **Swordsman** | non | `Pointed` (bonnet snug) | `Solid` (tunique + baudrier) | oui |
| **Archer** | non | `None` (tête nue, cheveux visibles) | `Solid` | non — silhouette dégagée/agile |
| **Mage** | non | `Wizard` (large bord + cône haut) | `Robe` (col en V + cordelette, pas de baudrier) | non |
| **Elf** | oui — seul archétype à en avoir | `Pointed` | `Solid` | oui |
| **Skeleton** | non | `None` | `Ribcage` (claire-voie) | non |

**Skeleton** a en plus `HasHair = HasEyes = HasBlush = false` et `HasSkullFace = true` : crâne nu à
orbites ET cavité nasale creusées (`HeadGenerator.CarveEyeSockets`/`CarveNasalCavity` VIDENT
réellement le voxel, pas un aplat peint) et mâchoire à dents (`HeadGenerator.CarveJaw` alterne
dents claires et interstices creusés sur la bande de bouche), cage thoracique à claire-voie
(`TorsoGenerator.StampRibcage` alterne bandes de côtes pleines et rangées non stampées = vrai
trou), bras nus sans épaulière. Les quatre autres ont `HasHair = HasEyes = HasBlush = true` et
`HasSkullFace = false`.

C'est aussi ce preset `Skeleton` qui est réutilisé pour l'apparence des ennemis (voir
`EnemyDefinition.Archetype` dans `CubeWorld.Combat`, §5) : un ennemi n'est rien de plus qu'un
appel à `CharacterModelBuilder.Build` avec cet archétype (ou un autre, une fois d'autres presets
monstre ajoutés).

Les générateurs de `Parts/` reçoivent ce `CharacterSilhouette` en paramètre et branchent leur
stamp en conséquence ; `HipsGenerator`/`LegGenerator` n'ont besoin d'aucun flag — la palette seule
suffit à les faire lire comme os/pagne. `GearGenerator` (épée) n'est toujours pas appelé par
`CharacterModelBuilder` (voir §5) : c'est le `PlayerGearVisual` du joueur qui l'appelle avec la
palette du porteur (`CharacterModelRoot.Palette`), mais pas encore sa silhouette — une lame
rouillée distincte pour le squelette resterait à brancher en passant la silhouette en plus de la
palette.

## 4. Animation

L'animation (`CharacterLocomotionAnimator`, un vrai `Animator` + `AnimatorController`, PAS de
rotation calculée main) est spécifique au joueur et vit dans l'assemblée `CubeWorld.Player`
(`Assets/_Project/Scripts/Player/CharacterModel/`) — voir son README. Les clips ciblent les
chemins de Transform stables de ce dossier (`Hips/Torso`, `Hips/Torso/ArmL`...) mais le composant
lui-même ne consomme que l'API publique de `CharacterModelRoot`, pas d'internals de ce dossier.
Les ennemis générés via ce pipeline n'ont pour l'instant aucune animation (voir §5).

## 5. État actuel / limites connues

- **Les cinq archétypes ont chacun une silhouette et une palette dédiées** (voir §3.6/§3.3) :
  `Swordsman` (bonnet + tunique + épaulières), `Archer` (tête nue, pas d'épaulière), `Mage`
  (chapeau de sorcier + robe), `Elf` (seul à avoir des oreilles pointues) et `Skeleton` (crâne nu +
  cage thoracique). Ce qui reste partagé entre les quatre humanoïdes (peau/cheveux/yeux/cuir/lame)
  est volontaire : ils doivent lire comme "de la même famille", pas comme des espèces différentes.
- La forme est pilotée par `CharacterSilhouetteCatalog` (figé, pas de RNG) et les couleurs par
  `CharacterPalette.Generate(rng, archetype)` (voir §3.3/§3.6) : le seed ne fait varier que les
  couleurs/proportions continues à l'intérieur du gabarit de l'archétype, jamais sa forme.
- **Aucune arme/outil n'est générée par `CharacterModelBuilder`.** L'épée
  (`GearGenerator.BuildSword`) et la pioche (`GearGenerator.BuildPickaxe`) sont appelées par le
  `PlayerGearVisual` du joueur (assemblée `CubeWorld.Player`), qui monte/démonte les meshes sur
  `GearBackAnchor`/`ToolBackAnchor` à chaque changement d'équipement, d'après
  `WeaponDefinition.VisualKind`/`ToolDefinition.VisualKind` de l'arme/l'outil réellement équipé(e)
  — et les reparente temporairement vers `WeaponGripAnchor` pendant une action de combat/minage
  (voir `CharacterCombatAnimationEvents` côté `Player/CharacterModel`). Cette pièce était
  `internal` avant la migration de ce pipeline dans sa propre assemblée ; `BodyPart`,
  `CharacterAssembler` et
  `GearGenerator` sont désormais `public` pour rester accessibles cross-assemblée — seul
  `PartSocket`/`CharacterRigDefinition` (usage interne au pipeline) restent `internal`.
- **Ennemis (`CubeWorld.Combat.EnemyAI`)** : génèrent désormais un vrai personnage via
  `CharacterModelBuilder.Build(definition.Height, definition.Archetype, seed)` au lieu de la
  capsule colorée placeholder d'origine (le seed vient de `GetInstanceID()` de l'ennemi, pour une
  variété de couleurs/proportions par instance sans configuration supplémentaire). Limite
  actuelle : un seul preset "monstre" existe (`Skeleton`), donc tous les types d'ennemis se
  ressemblent visuellement jusqu'à l'ajout d'autres presets dédiés (Goblin, Orc...) dans
  `CharacterSilhouetteCatalog`/`CharacterPalette` — voir §6. Aucune animation n'est encore
  branchée sur les ennemis (le rig reste en pose de repos) ; le
  `CharacterLocomotionAnimator`/AnimatorController du joueur est directement réutilisable (les
  chemins de courbes ne dépendent que des noms stables de `CharacterRigDefinition`, partagés par
  tout personnage généré via ce pipeline).

## 6. Points d'extension (où toucher pour...)

| Besoin | Où toucher |
|---|---|
| Nouvelle plage de couleurs pour l'archétype existant | Le `OutfitScheme` de l'archétype dans `CharacterPalette` (humanoïdes) / `CharacterPalette.GenerateSkeleton` |
| Nouvelle fourchette de proportions / nouvelle coiffure | `CharacterBodyParams.Generate` (+ `HairStyle` enum) |
| Nouveau motif peint sur une pièce existante (broderie, insigne...) | `colorAt` du `VoxelStamper.Stamp` dans le générateur concerné |
| Nouvelle arme/accessoire de dos (joueur) | Ajouter une valeur à `WeaponVisualKind` (`Assets/_Project/Scripts/Combat/WeaponVisualKind.cs`), un générateur `BuildXxx` dans `GearGenerator`, un cas dans `PlayerGearVisual.BuildGearPart`, puis assigner ce `VisualKind` sur le(s) `WeaponDefinition` concernés |
| **Nouvel archétype/skin distinct** (joueur ou monstre — robot, gobelin, orc...) | Ajouter une valeur à `CharacterArchetype`, un preset dans `CharacterSilhouetteCatalog` (flags de forme, voir §3.6), un `OutfitScheme` dans `CharacterPalette` (ou une branche `GenerateXxx` à part si la palette doit diverger plus que la teinte identitaire/l'accent, en respectant la règle 3 familles + 1 accent), puis consommer les nouveaux flags nécessaires dans les générateurs de `Parts/` concernés (voir §3.6 pour l'exemple Skeleton/Mage). Le pattern `ItemDefinition`/`ItemCatalog` (`Assets/_Project/Scripts/Combat/`) a inspiré `CharacterSilhouetteCatalog`, en version code (pas de ScriptableObject) puisqu'ajouter un archétype nécessite de toute façon du code dans les générateurs. Pour un ennemi : assigner le nouvel archétype à `EnemyDefinition.Archetype` dans l'inspecteur, rien d'autre à câbler. |
| Nouvel os animable (ex. une queue, un accessoire mobile) | Ajouter le nom dans `CharacterRigDefinition`, exposer un socket depuis le générateur parent, attacher dans `CharacterModelBuilder.Build`, binder le Transform dans `CharacterModelRoot` — puis regénérer les clips (`CubeWorld.EditorTools.CharacterLocomotionAnimatorGenerator`) si le nouvel os doit bouger en marche/idle |
| Animation pour les ennemis | Réutiliser `Player.CharacterModel.CharacterLocomotionAnimator` (même AnimatorController, ne dépend que de l'API publique de `CharacterModelRoot`) depuis `CubeWorld.Combat.EnemyAI`, ou migrer ce composant ici s'il doit devenir partagé |
| Nouveau clip/état d'Animator (attaque, saut...) | Éditer `CubeWorld.EditorTools.CharacterLocomotionAnimatorGenerator` (assemblée Editor, `Assets/_Project/Editor/`) pour ajouter le clip + l'état/la transition, puis relancer le menu `CubeWorld/Character/Generate Locomotion Animator` — ne JAMAIS éditer `CharacterLocomotion.controller`/les `.anim` à la main dans l'éditeur Unity, sinon le prochain passage de l'outil les écrase |

## 7. Glossaire rapide

- **Seed** : entier qui initialise `CharacterRng`. Détermine palette + proportions + coiffure de façon reproductible, à l'intérieur du gabarit figé par l'archétype.
- **Archétype** (`CharacterArchetype`) : résout une silhouette figée (`CharacterSilhouetteCatalog`) et une palette dédiée (`CharacterPalette`) — voir §3.6. Partagé par le joueur et les ennemis.
- **Silhouette** (`CharacterSilhouette`) : flags de FORME figés par archétype (oreilles, cheveux/couvre-tête, yeux, torse...), consommés par les générateurs de `Parts/`.
- **Pièce / `BodyPart`** : un mesh généré + ses sockets (ex. la tête, un bras).
- **Socket** : point d'ancrage nommé, position/rotation locales à la pièce parente.
- **Os** : `Transform` d'une pièce assemblée, nommé de façon stable (voir §3.5), utilisé pour l'animation.
