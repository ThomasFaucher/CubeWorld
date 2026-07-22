# CharacterModel — personnage voxel chibi façon CubeWorld

Ce dossier contient **tout** le système qui fabrique l'apparence visuelle du
joueur : il n'y a **aucune texture, aucun sprite, aucun modèle 3D importé**.
Le personnage (tête, torse, bras, jambes, épée...) est entièrement **généré
en code** sous forme de petites grilles de voxels colorés, converties en
meshes, puis assemblées en hiérarchie de `Transform` animable.

> Règle d'or : **même seed → même personnage**, à l'exception de
> l'expression du visage qui est choisie indépendamment.

## 1. Vue d'ensemble du pipeline

```
seed (int)
  └─ CharacterRng                         (Generation/CharacterRng.cs)
       ├─ CharacterPalette.Generate()     (Generation/CharacterPalette.cs)   → toutes les couleurs
       ├─ CharacterBodyParams.Generate()  (Generation/CharacterBodyParams.cs) → proportions + coiffure
       └─ HeadShape.Generate()            (Generation/HeadShape.cs)          → galbe de la tête

CharacterModelBuilder.Build(...)          (CharacterModelBuilder.cs)         ← point d'entrée public
  ├─ HipsGenerator.Build            → pièce "Hips"   (bassin/pantalon/ceinture)
  ├─ TorsoGenerator.Build           → pièce "Torso"  (tunique/baudrier)
  ├─ HeadGenerator.Build            → pièce "Head"   (crâne/visage/cheveux/bonnet/oreilles)
  ├─ ArmGenerator.BuildUpperArm x2  → pièces "ArmL"/"ArmR"      (épaulière incluse)
  ├─ ArmGenerator.BuildForearm x2   → pièces "ForearmL"/"ForearmR"
  ├─ LegGenerator.BuildLeg x2       → pièces "LegL"/"LegR"
  ├─ LegGenerator.BuildFoot x2      → pièces "FootL"/"FootR"
  └─ GearGenerator.BuildSword       → pièce "SwordBack" (décorative, pas un os)

CharacterAssembler.Attach(...)            (Rig/CharacterAssembler.cs)
  → un GameObject (MeshFilter+MeshRenderer) par pièce, posé sur le socket du parent

CharacterModelRoot (MonoBehaviour)        (CharacterModelRoot.cs)
  → expose les Transforms des os pour l'animation / le gameplay

ProceduralCharacterAnimator (MonoBehaviour) (ProceduralCharacterAnimator.cs)
  → anime le rig par rotation (idle + marche), piloté par la vitesse du CharacterController
```

Point d'entrée appelé par le jeu : `PlayerController.CreateVoxelVisual()`
(`Assets/_Project/Scripts/Player/PlayerController.cs`), qui appelle
`CharacterModelBuilder.Build(_visualHeight, archetype, seed, _debugExpression)`.

## 2. Arborescence du dossier

```
CharacterModel/
├── CharacterModelBuilder.cs      Point d'entrée : assemble un personnage complet
├── CharacterModelRoot.cs         Composant posé sur la racine : accès aux Transforms/os
├── ProceduralCharacterAnimator.cs Animation par rotation (idle/marche), sans skinning
│
├── Core/                         Brique bas niveau, réutilisable pour N'IMPORTE QUELLE pièce
│   ├── VoxelGrid.cs              Grille dense Color32[,,] d'UNE pièce (alpha 0 = vide)
│   ├── VoxelShape.cs             Primitives : Box, Column, Sphere, Dome, ConeUp, ConeDown
│   ├── VoxelStamper.cs           Tamponne une primitive (couleur unie ou par-voxel) dans une grille
│   ├── VoxelPartMeshBuilder.cs   Grille + pivot voxel + unité monde → façade pratique pour un générateur
│   └── VoxelGridMesher.cs        Grille complète → Mesh (face culling, AO vertex, jitter de couleur)
│
├── Generation/                   Tirage aléatoire (seed → paramètres), AUCUN mesh ici
│   ├── CharacterRng.cs           Random déterministe par seed (NextFloat/NextInt/Pick/NextBool)
│   ├── CharacterPalette.cs       Toutes les couleurs du personnage (règle de contraste, voir §4)
│   ├── CharacterBodyParams.cs    Proportions (tête, torse, membres) + HairStyle
│   ├── HeadShape.cs              Largeur de chaque rangée de la tête + rangées repères du visage
│   ├── CharacterExpression.cs    Enum public (Neutral/Happy/Angry/Sad/Surprised/Blink)
│   └── ExpressionShape.cs        Traduit une expression en paramètres de dessin (yeux/bouche/sourcils)
│
├── Parts/                        Un générateur par partie du corps : grille → BodyPart
│   ├── HeadGenerator.cs          Crâne squircle, visage peint, oreilles pointues, cheveux, bonnet
│   ├── TorsoGenerator.cs         Tunique tonneau, col, baudrier diagonal
│   ├── HipsGenerator.cs          Bassin/jupe, ceinture+boucle, "plug" caché dans la taille du torse
│   ├── ArmGenerator.cs           Bras haut (capsule + épaulière) et avant-bras (manche + moufle)
│   ├── LegGenerator.cs           Jambe (colonne) et pied (bottine)
│   └── GearGenerator.cs          Épée portée dans le dos (purement décorative pour l'instant)
│
└── Rig/                          Squelette / assemblage, indépendant du contenu des pièces
    ├── BodyPart.cs               Résultat d'un générateur : Mesh + sockets nommés
    ├── PartSocket.cs             Point d'ancrage (position+rotation locales) offert à un enfant
    ├── CharacterRigDefinition.cs Noms STABLES des os et des sockets (voir §5)
    └── CharacterAssembler.cs     Instancie les GameObjects et pose chaque pièce sur le socket parent
```

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
  `ShoulderL`, `ShoulderR`, `Back`).
- `CharacterAssembler.Attach(part, parent, socket, material)` crée le `GameObject`, le pose
  exactement sur le socket du parent, et devient lui-même le nouveau parent pour l'étage suivant.
- Convention : le mesh d'une pièce est TOUJOURS généré avec son point d'attache à l'origine. Ça
  veut dire que **n'importe quelle tête générée peut s'attacher à n'importe quel torse** qui expose
  un socket `Neck`, tant que les deux respectent cette convention — c'est la porte d'entrée pour un
  futur système de pièces interchangeables (voir §6).

### 3.3 Palette : règle de contraste de valeur (important, ne pas casser)

`CharacterPalette.Generate()` ne tire pas des couleurs au hasard total : il respecte une règle
assumée en 3 familles de valeur + 1 accent (documentée en tête du fichier) :

| Famille | Rôle | Exemples de champs |
|---|---|---|
| **Sombre** | cuir quasi noir | `Belt`, `LeatherLight`, `Boot`, `BootSole` |
| **Clair** | blanc cassé | `OutfitTrim`, `Pants` |
| **Moyen (identitaire)** | teinte du personnage | `OutfitPrimary`, `OutfitSecondary` |
| **Accent isolé** | seul élément vif, nulle part ailleurs | `Emblem` |

C'est cette alternance qui donne le relief "CubeWorld" à un shading plat sans texture. **Toute
nouvelle palette doit respecter cette règle**, sinon le rendu "lit" comme un aplat quel que soit
l'éclairage (voir le commentaire du fichier pour le détail).

### 3.4 Proportions chibi

`CharacterBodyParams.Generate()` tire des proportions dans des **fourchettes resserrées** (pas de
plage large) : tête large de 14 à 18 voxels (~1/3 de la hauteur hors bonnet), membres épais. Le but
est que la silhouette chibi reste reconnaissable d'un seed à l'autre — seuls les gabarits fins
varient.

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

## 4. Animation

`ProceduralCharacterAnimator` est une animation de **validation du rig**, pas la version finale :
elle capture la pose de repos (bind pose) à l'assemblage, puis applique des rotations en delta
(idle = respiration + dodelinement, marche = bras/jambes en opposition, pilotée par
`CharacterController.velocity`). Elle est conçue pour être remplacée par un Animator Controller le
jour venu — désactiver le composant suffit, car les noms d'os sont stables.

## 5. État actuel / limites connues

- **Un seul archétype est réellement implémenté.** `PlayerArchetype` (enum dans
  `Assets/_Project/Scripts/Player/PlayerArchetype.cs`) déclare déjà `Swordsman`, `Archer`, `Mage`,
  `Elf`, mais `CharacterModelBuilder.Build(...)` ignore ce paramètre : toutes les valeurs produisent
  aujourd'hui le même héros (façon Link) avec juste palette/proportions/coiffure variées par seed.
- **La silhouette "héros elfe" est câblée en dur**, pas paramétrée : `HeadGenerator.StampEars`
  stamppe toujours des oreilles pointues, `StampHairAndCap` peint toujours un bonnet vert
  (`palette.OutfitPrimary`), `GearGenerator` ne sait construire qu'une épée. Il n'existe **aucun
  flag** pour dire "pas d'oreilles", "pas de cheveux", "torse à claire-voie (squelette)", etc.
- Le seed ne fait donc varier que des **couleurs et proportions continues**, jamais la **forme** —
  ce qui suffit pour des variantes d'un même personnage, mais pas pour des types visuellement
  différents (squelette, robot, personnage "anime"...).
- `GearGenerator` note lui-même qu'il est temporaire : quand `PlayerEquipment` pilotera l'arme
  affichée, ce générateur deviendra le rendu voxel des armes d'après leur `ItemDefinition`.

## 6. Points d'extension (où toucher pour...)

| Besoin | Où toucher |
|---|---|
| Nouvelle plage de couleurs pour l'archétype existant | `CharacterPalette.Generate` |
| Nouvelle fourchette de proportions / nouvelle coiffure | `CharacterBodyParams.Generate` (+ `HairStyle` enum) |
| Nouveau motif peint sur une pièce existante (broderie, insigne...) | `colorAt` du `VoxelStamper.Stamp` dans le générateur concerné |
| Nouvelle arme/accessoire de dos | `GearGenerator` (+ nouveau nom d'os dans `CharacterRigDefinition` si ce n'est pas juste décoratif) |
| **Vrais archétypes/skins distincts** (squelette, anime, robot...) | Il faut d'abord introduire des **flags de silhouette** (ex. `HasPointedEars`, `HeadCovering`, `TorsoStyle`) consommés par les générateurs de `Parts/`, et un catalogue de presets FIGÉS (pas de RNG continu) pour choisir le "type" — voir discussion en cours, pas encore implémenté. Le pattern `ItemDefinition`/`ItemCatalog` (`Assets/_Project/Scripts/Combat/`) est le bon modèle à suivre (ScriptableObject + id stable + catalogue de lookup). |
| Nouvel os animable (ex. une queue, un accessoire mobile) | Ajouter le nom dans `CharacterRigDefinition`, exposer un socket depuis le générateur parent, attacher dans `CharacterModelBuilder.Build`, binder le Transform dans `CharacterModelRoot` |

## 7. Glossaire rapide

- **Seed** : entier qui initialise `CharacterRng`. Détermine palette + proportions + coiffure de façon reproductible.
- **Archétype** (`PlayerArchetype`) : paramètre actuellement accepté mais non consommé par le générateur.
- **Pièce / `BodyPart`** : un mesh généré + ses sockets (ex. la tête, un bras).
- **Socket** : point d'ancrage nommé, position/rotation locales à la pièce parente.
- **Os** : `Transform` d'une pièce assemblée, nommé de façon stable (voir §3.5), utilisé pour l'animation.
