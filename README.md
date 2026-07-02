# CubeWorld — Clone Unity

> Projet personnel de développement d'un clone fidèle du jeu **CubeWorld** (Wollay, 2013), réalisé avec Unity 6.3 LTS en C#.

---

## Présentation

CubeWorld est un RPG d'action en monde ouvert voxel, avec génération procédurale de terrain, exploration, combat et progression de personnage. Ce projet vise à reproduire fidèlement son gameplay et son esthétique, tout en constituant une base d'apprentissage sérieux du développement de jeux 3D.

---

## Direction artistique — style CubeWorld, pas Minecraft

L'esthétique visée est celle du CubeWorld original, qui se distingue nettement de Minecraft :

- **Voxels colorés, pas de textures** : chaque voxel porte une couleur unie (couleurs par vertex dans le mesh), aucun atlas de textures.
- **Chunks cubiques (32×32×32)** : le monde s'étend aussi verticalement — falaises abruptes, surplombs, grottes — contrairement aux colonnes 16×16×256 de Minecraft.
- **Flat shading** : rendu low-poly avec normales par face, palette vive et saturée.
- **Terrain expressif** : relief marqué (montagnes, canyons), variations de teinte par voxel pour casser l'uniformité.

---

## Stack technique

| Technologie | Version | Rôle |
|---|---|---|
| Unity | 6.3 LTS (6000.3.19f1) | Moteur de jeu |
| C# | .NET 9 | Langage de scripting |
| Universal Render Pipeline (URP) | — | Pipeline de rendu |
| Burst Compiler | — | Compilation native pour les performances |
| Unity Jobs System | — | Génération de chunks en multithread |
| Input System | — | Gestion clavier / souris |
| Cinemachine | — | Caméra 3ème personne |
| TextMeshPro | — | UI et texte in-game |

---

## Structure du projet

```
Assets/
├── _Project/
│   ├── Scripts/            # Une assembly definition par domaine
│   │   ├── Core/           # CubeWorld.Core — GameManager, EventBus
│   │   ├── World/          # CubeWorld.World — chunks, voxels, génération procédurale
│   │   ├── Player/         # CubeWorld.Player — mouvement, caméra, stats
│   │   ├── Combat/         # CubeWorld.Combat — attaques, dégâts, ennemis
│   │   └── UI/             # CubeWorld.UI — HUD, inventaire, menus
│   ├── Input/              # Actions de l'Input System
│   └── Scenes/             # Main.unity
└── Settings/               # Assets URP (render pipeline, volume profiles)
```

Les dossiers `Prefabs/`, `Materials/`, etc. seront créés au fil du besoin.

### Architecture

- **Assembly definitions** : chaque domaine compile dans sa propre assembly, avec des dépendances explicites et sans cycles (`World`, `Combat` et `UI` ne dépendent que de `Core` ; `Player` dépend de `Core` et `World`).
- **EventBus** (`CubeWorld.Core.EventBus`) : communication découplée entre systèmes par événements typés (`IGameEvent`).
- **GameManager** : point d'entrée unique, sans logique métier.
- **WorldConfig** (ScriptableObject) : tous les paramètres du monde (taille de chunk, graine, bruit) éditables dans l'inspecteur.

---

## Roadmap

### Phase 1 — Monde voxel
- [x] Types de base (Voxel, VoxelType, WorldConfig)
- [ ] Système de chunks cubiques (32×32×32)
- [ ] Génération procédurale du terrain (Perlin Noise)
- [ ] Génération de mesh avec couleurs par vertex + flat shading (style CubeWorld)
- [ ] Face culling (ne pas afficher les faces cachées)
- [ ] Chargement/déchargement dynamique des chunks autour du joueur (Jobs + Burst)

### Phase 2 — Joueur
- [ ] CharacterController + déplacement (marche, sprint, saut)
- [ ] Caméra 3ème personne (Cinemachine)
- [ ] Collision avec le terrain voxel

### Phase 3 — Combat & RPG
- [ ] Système de stats (HP, MP, niveau, XP)
- [ ] Armes et attaques de base
- [ ] Ennemis avec IA basique (NavMesh)
- [ ] Loot et drops

### Phase 4 — Crafting & inventaire
- [ ] Système d'inventaire
- [ ] Crafting de base
- [ ] Équipement (armes, armures)

### Phase 5 — Monde avancé
- [ ] Biomes (forêt, désert, neige...)
- [ ] Eau et fluides
- [ ] Végétation procédurale
- [ ] Donjons et structures générées

### Phase 6 — Polish
- [ ] Effets visuels (particules, lumières)
- [ ] Son et musique
- [ ] Interface utilisateur complète
- [ ] Sauvegarde / chargement du monde

---

## Installation & lancement

### Prérequis

- [Unity Hub](https://unity.com/download)
- Unity **6.3 LTS** (6000.3.19f1)
- Visual Studio 2022 avec le module *Game development with Unity*
- Git + Git LFS

### Cloner le projet

```bash
git clone <url-du-repo>
cd CubeWorld
git lfs pull
```

### Ouvrir dans Unity

1. Ouvrir Unity Hub
2. **Projects → Add project from disk**
3. Sélectionner le dossier `CubeWorld`
4. Ouvrir avec Unity 6.3 LTS

---

## Conventions de code

- **Langue** : code en anglais, commentaires en français
- **Nommage** : PascalCase pour les classes, camelCase pour les variables privées, `_camelCase` pour les champs sérialisés
- **Un fichier = une classe**
- **Pas de logique dans les `MonoBehaviour`** — déléguer aux classes métier

---

## Références

- [CubeWorld Wiki](https://cubeworld.fandom.com/wiki/Cube_World_Wiki)
- [Sebastian Lague — Procedural Terrain](https://www.youtube.com/c/SebastianLague)
- [Unity Documentation](https://docs.unity3d.com)
- [Burst Compiler docs](https://docs.unity3d.com/Packages/com.unity.burst@latest)

---

## Auteur

**Thomas Faucher** — Apprenti développeur full stack  
Projet personnel — 2026
