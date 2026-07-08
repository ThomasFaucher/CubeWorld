# Architecture — CubeWorld

> Documentation technique système par système. Pour la présentation générale, la stack et la roadmap, voir [`README.md`](../README.md).

---

## 1. Contexte : un MMORPG voxel façon CubeWorld

Ce projet reproduit le gameplay et l'esthétique du jeu **CubeWorld** (Wollay) : un RPG d'action en monde ouvert voxel, où le joueur explore un terrain généré procéduralement, combat des monstres, récupère du butin, s'équipe et progresse en niveau. C'est le genre visé — un **MMORPG voxel** (exploration + combat + loot + équipement + artisanat, en ligne à terme) — qui guide les choix d'architecture, même si l'implémentation actuelle ne l'est pas encore complètement :

- **Aujourd'hui** : simulation 100% locale, un seul joueur, aucune couche réseau. Toute la logique (combat, inventaire, IA des ennemis) tourne dans le même processus, en confiance totale (pas de validation serveur, pas d'anti-triche).
- **Visé** : le découpage actuel en systèmes indépendants qui ne communiquent que par événements ou par interfaces (`IDamageable`, `EventBus`) est justement ce qui rendra une future séparation client/serveur possible sans tout réécrire — mais cette séparation n'existe pas encore. Il n'y a pas de notion de session, de compte, ni de synchronisation d'un autre joueur.

Le reste de ce document explique comment le jeu est architecturé aujourd'hui, système par système.

---

## 2. Principes transversaux

Ces règles s'appliquent à tout le projet et expliquent pourquoi le code est écrit comme il l'est :

| Principe | En pratique |
|---|---|
| **Une assembly (asmdef) par domaine** | `Core`, `World`, `Combat`, `Player`, `UI` compilent séparément, avec des dépendances explicites et **sans cycle** (voir schéma ci-dessous). Empêche `World` de dépendre de `Combat`, par exemple — un changement de gameplay ne peut jamais casser la génération du monde. |
| **EventBus (`CubeWorld.Core.EventBus`)** | Pub/sub typé pour les **notifications transverses** (XP gagnée, mort d'une entité, chunk chargé...). Jamais utilisé pour la mutation d'état primaire — voir §7. |
| **Logique pure + wrapper `MonoBehaviour`** | Les règles de jeu (déplacement, IA, dégâts, inventaire) vivent dans des classes C# pures, sans dépendance à Unity, testables isolément (`PlayerMotor`, `EnemyAIBrain`, `MeleeAttackResolver`, `Inventory`, `CraftingSystem`, `TerrainShape`). Un composant `MonoBehaviour` fin les possède et fait le pont avec la scène. |
| **ScriptableObject = données, pas de sous-classement** | Tout le contenu (ennemis, armes, armures, objets, recettes, config du monde) est un `ScriptableObject` avec des champs plats + enums, éditable sans toucher au code (`EnemyDefinition`, `WeaponDefinition`, `ArmorDefinition`, `ItemDefinition`, `LootTableDefinition`, `CraftingRecipeDefinition`, `WorldConfig`). |
| **Zéro prefab** | Tout est construit avec `new GameObject()` + `AddComponent<T>()` au runtime (chunks, joueur, ennemis, pickups, caméra), ou câblé à la main dans la scène pour les éléments uniques (Canvas UI). Pas de dossier `Prefabs/`. |
| **Style visuel** | Voxels colorés par couleur de vertex (pas de texture), flat shading, chunks cubiques 32³. Voir `VoxelPalette`, le shader `CubeWorld/VoxelTerrain`, et `PlayerVoxelModelBuilder` pour le personnage. |

### Graphe de dépendances (DAG)

```
CubeWorld.Core   (racine, zéro dépendance)
     ▲   ▲   ▲
     │   │   └── CubeWorld.World
     │   └────── CubeWorld.Combat
     └────────── CubeWorld.Player  (dépend de Core + World + Combat)
                       ▲
                       └── CubeWorld.UI  (dépend de Core + Combat + Player)
```

`World` et `Combat` ne se connaissent pas — ils ne communiquent que via des événements `Core` à payload primitif (voir §7).

---

## 3. Core (`CubeWorld.Core`)

Le socle sans lequel rien d'autre ne compile. Ne dépend d'aucune autre assembly du projet.

| Fichier | Rôle |
|---|---|
| `GameManager.cs` | Unique singleton du projet. Point d'entrée de la scène, survit aux changements de scène (`DontDestroyOnLoad`). Aucune logique métier. |
| `EventBus.cs` | Bus d'événements typé (`Subscribe<T>`, `Publish<T>`, `Unsubscribe<T>`). Voir §7. |
| `PlayerContext.cs` | `static Transform Transform` — référence globale au joueur, assignée une fois par `PlayerBootstrap.Awake`. Évite un tag Unity ou une dépendance de `World`/`Combat` vers `Player`. |
| `StatBlock.cs` | Stats RPG génériques (HP, MP, niveau, XP). Classe pure composée aussi bien par `PlayerHealth` que par `Combat.EnemyHealth` — pas d'héritage. Gère la montée de niveau (`GainXP`) et les bonus temporaires de PV max (`IncreaseMaxHP`/`DecreaseMaxHP`, utilisés par l'armure). |
| `IDamageable.cs` / `DamageInfo.cs` | Contrat commun pour « peut encaisser des dégâts » (joueur, ennemis), sans classe de base partagée — `Player` et `Combat` restent indépendants. |
| `Events/*.cs` | Payloads d'événements, volontairement primitifs (`string`, `int`, `Vector3`...), jamais de référence vers un type de `World`/`Combat`/`Player`. |

---

## 4. World (`CubeWorld.World`)

Le monde voxel : génération procédurale, streaming, meshing, collision, NavMesh.

| Fichier | Rôle |
|---|---|
| `WorldConfig.cs` | `ScriptableObject` : taille de chunk (32³), taille du voxel en unités monde, distance de vue, graine, échelle de bruit, hauteur max, niveau de la mer. Tout le reste du système lit ces valeurs. |
| `Voxel.cs` / `VoxelType.cs` | Un voxel = 1 octet (`VoxelType` : Air, Grass, Dirt, Stone, Sand, Snow, Water). Compact pour tenir en `NativeArray` (32³ = 32 768 octets par chunk). |
| `Chunk.cs` | Stocke les voxels d'un chunk dans un `NativeArray<Voxel>` plat (compatible Burst/Jobs), avec accès par coordonnées locales. |
| `TerrainShape.cs` / `TerrainGenerationJob.cs` / `TerrainGenerator.cs` | Génération procédurale : bruit de Perlin fractal (FBM, 4 octaves) → hauteur de surface, puis choix du voxel par couche (herbe/sable/neige en surface, terre, pierre, eau jusqu'au niveau de la mer). Planifiée en job Burst multithread. |
| `VoxelWorld.cs` | Le monde métier : possède les chunks (`Dictionary<int3, Chunk>`), planifie leur génération, expose l'accès aux voxels et aux voisins pour le meshing. Suit les `JobHandle` en cours pour ne jamais lire/libérer un chunk encore en cours d'écriture. |
| `ChunkMeshBuildJob.cs` / `ChunkMeshBuilder.cs` / `ChunkMeshData.cs` | Transforme les voxels d'un chunk en mesh : face culling (une face n'est générée que si elle touche de l'air), couleur par vertex (`VoxelPalette`), séparation terrain opaque / eau transparente. Job Burst, dépend de la génération du chunk **et** de ses 6 voisins directs déjà chargés. |
| `VoxelPalette.cs` | Couleur par `VoxelType`, avec une légère variation aléatoire déterministe par position (casse l'uniformité des grandes surfaces, notamment l'herbe). |
| `ChunkStreamer.cs` | Charge/décharge les colonnes de chunks en disque autour du joueur, budgété par frame (pas de gel), triées par proximité, avec hystérésis pour éviter les chargements/déchargements en boucle à la frontière. Classe métier pure. |
| `WorldBootstrap.cs` | `MonoBehaviour` pilote : à chaque frame, signale au streamer la position de la cible, planifie les jobs de meshing, matérialise en `GameObject` (mesh + collider) les chunks dont le job vient de finir. Seul point de contact entre la scène et le métier `World`. Publie `ChunkMeshMaterializedEvent`/`ChunkUnloadedEvent`. |
| `NavMeshRegionBaker.cs` | Rebake un `NavMeshSurface` borné autour du joueur à mesure que les chunks streament, débouncé (attend une accalmie) avec un intervalle minimum entre deux bakes. Publie `NavMeshBakedEvent`, consommé par `Combat.EnemySpawner`. |

---

## 5. Combat (`CubeWorld.Combat`)

Tout le contenu et les règles RPG : ennemis, armes, armures, objets, inventaire, craft. Ne dépend que de `Core` (ignore tout de `World` et `Player`).

### Contenu (ScriptableObjects)

| Fichier | Rôle |
|---|---|
| `EnemyDefinition.cs` | Stats, comportement (vitesse, portée d'attaque, détection), table de loot et gabarit visuel d'un type d'ennemi. |
| `WeaponDefinition.cs` | Dégâts, portée, rayon, cooldown d'une arme de mêlée. |
| `ArmorDefinition.cs` | Emplacement (`ArmorSlot` : Head/Chest/Legs), bonus de PV max, réduction de dégâts à plat. |
| `ItemDefinition.cs` | Objet ramassable/inventoriable : identité, catégorie (`ItemCategory` : Material/Weapon/Armor), taille de pile, référence vers `WeaponDefinition`/`ArmorDefinition` si équipable. |
| `LootTableDefinition.cs` | Table de drops : chaque entrée (objet, chance, quantité min/max) est tirée indépendamment — pas de tirage exclusif. |
| `CraftingRecipeDefinition.cs` | Liste d'ingrédients (objet + quantité) → objet produit + quantité. |

### Logique pure

| Fichier | Rôle |
|---|---|
| `EnemyAIBrain.cs` | Décide Idle/Chase/Attack selon la distance à la cible — ne connaît ni `NavMeshAgent` ni `MonoBehaviour`. |
| `MeleeAttackResolver.cs` | Résolution d'un coup de mêlée (capsule physique projetée devant l'attaquant, dédupliquée, attaquant exclu). **Partagée** par `Player.PlayerCombat` et `Combat.EnemyAI` — une seule implémentation des règles de mêlée. |
| `Inventory.cs` | Inventaire à capacité fixe, empilement par `ItemDefinition.MaxStackSize`. `TryAdd`/`TryRemove` sans ajout partiel (tout ou rien). |
| `CraftingSystem.cs` | Vérifie/consomme les ingrédients d'une recette contre un `Inventory`. |

### MonoBehaviours (pont scène)

| Fichier | Rôle |
|---|---|
| `EnemyAI.cs` | Pilote un `NavMeshAgent` selon les décisions d'`EnemyAIBrain`. Instancié en code par `EnemySpawner` (pas de config inspecteur). |
| `EnemyHealth.cs` | `StatBlock` d'un ennemi ; à la mort, publie les events et tire la table de loot (`ItemPickup.Spawn`). |
| `EnemySpawner.cs` | Fait apparaître des ennemis dans la zone NavMesh bakée (abonné à `NavMeshBakedEvent`), jusqu'à un plafond, et nettoie ceux hors de la nouvelle zone. |
| `ItemPickup.cs` | Objet ramassable dans le monde (cube coloré + trigger). `Spawn()` statique l'instancie, `Collect()` publie `ItemPickedUpEvent` et se détruit. |

---

## 6. Player (`CubeWorld.Player`)

Tout ce qui concerne le personnage jouable : mouvement, caméra, combat, inventaire, équipement, craft — composé sur un unique `GameObject "Player"` créé par `PlayerBootstrap`.

| Fichier | Rôle |
|---|---|
| `PlayerBootstrap.cs` | **Racine de composition** : instancie le joueur et tous ses composants dans le bon ordre, les branche entre eux, équipe l'arme de départ, connecte le rig caméra et le monde. Gère aussi le mode debug « caméra libre » (touche F). |
| `PlayerController.cs` / `PlayerMotor.cs` | Déplacement (`CharacterController` piloté par la logique pure de `PlayerMotor` : marche, sprint, saut, gravité), rotation vers la direction de mouvement (relative au yaw caméra). Possède aussi le modèle voxel du personnage (`PlayerVoxelModelBuilder`). |
| `PlayerVoxelModelBuilder.cs` | Construit à la volée le maillage du personnage : plusieurs volumes de petits voxels (pieds, jambes, bassin, torse, bras, mains, tête) fusionnés en un seul mesh, avec le même culling de faces et le même style (couleurs de vertex, flat shading) que le terrain. |
| `PlayerCameraRig.cs` | Caméra 3e personne orbitale (Cinemachine), construite en code, pilotée par l'action « Look ». |
| `PlayerHealth.cs` | `StatBlock` du joueur (`IDamageable`) ; les dégâts subis sont réduits par `PlayerEquipment.TotalDefense` ; écoute `XPGainedEvent` pour progresser. |
| `PlayerCombat.cs` | Attaque de mêlée sur l'action « Attack », via `MeleeAttackResolver`. Lit l'arme active sur `PlayerEquipment` à chaque coup (pas figée à l'équipement du moment du bind). |
| `PlayerInventory.cs` | Possède l'`Inventory` (capacité fixe) du joueur. |
| `PlayerEquipment.cs` | 4 emplacements (arme + tête/torse/jambes). Équiper retire l'objet de l'inventaire (et y remet l'ancien équipement) ; applique les bonus de PV via `PlayerHealth.ApplyArmorBonus`. |
| `PlayerLoot.cs` | Ramassage sur l'action « Interact » : cherche le `ItemPickup` le plus proche, l'ajoute à l'inventaire si la place le permet. |
| `PlayerCrafting.cs` | Wrapper fin autour de `Combat.CraftingSystem`, lié à l'inventaire du joueur. |
| `FlyCamera.cs` | Caméra libre de debug (ZQSD/WASD + souris), activée par la touche F via `PlayerBootstrap`. |

---

## 7. UI (`CubeWorld.UI`)

Interface d'inventaire/équipement/crafting. Le layout (Canvas, panneaux, templates) est construit à la main dans l'éditeur Unity ; le code clone les templates pour les listes dynamiques et met à jour les éléments fixes.

| Fichier | Rôle |
|---|---|
| `InventoryUI.cs` | Contrôleur principal. Résout les composants du joueur via `PlayerContext.Transform`, bascule le panneau sur l'action « ToggleInventory », rafraîchit les 3 sous-panneaux (slots, équipement, recettes) à l'ouverture et après chaque action. |
| `InventorySlotView.cs` / `CraftingRecipeView.cs` | Vues fines exposant les références (icône, textes, boutons) d'un template construit dans l'éditeur, cloné dynamiquement par slot/recette. |

---

## 8. Flux de données clés

### Ramassage d'un objet

```
ItemPickup (trigger au sol)
   → PlayerLoot.Update() détecte l'action Interact + le pickup le plus proche
   → inventory.Contents.TryAdd(item, quantity)   (mutation directe, pas d'event)
   → pickup.Collect() → EventBus.Publish(ItemPickedUpEvent)   (notification, ex. futur son/UI)
```

L'ajout à l'inventaire ne passe **jamais** par l'`EventBus` : `PlayerLoot` a déjà la référence `ItemDefinition` en main (via `ItemPickup`), donc pas besoin de retrouver un objet à partir d'un `string ItemId`. C'est le même principe que `PlayerCombat.Attack()`, qui appelle directement `IDamageable.ApplyDamage` plutôt que de publier un événement — **l'EventBus sert aux notifications transverses (XP, mort, niveau, chunks, navmesh), jamais à la mutation d'état primaire d'un système.**

### Combat et mort d'un ennemi

```
PlayerCombat.Attack() → MeleeAttackResolver.ResolveHits() → IDamageable.ApplyDamage(DamageInfo)
   → EnemyHealth.ApplyDamage() → stats.ApplyDamage() + EventBus.Publish(DamageDealtEvent)
   → si mort : EventBus.Publish(EntityDiedEvent) + Publish(XPGainedEvent) + SpawnLoot() (ItemPickup.Spawn par entrée de LootTableDefinition)
   → PlayerHealth.OnXPGained() (abonné à XPGainedEvent) → stats.GainXP() → LevelUpEvent si seuil franchi
   → EnemySpawner.OnEntityDied() (abonné à EntityDiedEvent) → retire l'ennemi de sa liste des vivants
```

### Streaming du monde et apparition des ennemis

```
WorldBootstrap.Update() → ChunkStreamer.Process() charge/décharge des colonnes
   → chunk matérialisé (mesh + collider) → EventBus.Publish(ChunkMeshMaterializedEvent)
   → NavMeshRegionBaker (abonné) redevient « dirty », rebake après une accalmie
   → EventBus.Publish(NavMeshBakedEvent)
   → EnemySpawner (abonné) nettoie les ennemis hors zone et en fait apparaître de nouveaux dedans
```

`World` ne connaît pas `Combat` : la seule communication passe par ces deux événements `Core`, à payload primitif (position, taille, coordonnées de chunk — jamais une référence `Chunk` ou `EnemyDefinition`).

### Équipement et craft

```
InventoryUI (bouton « Équiper ») → PlayerEquipment.EquipWeapon/EquipArmor(item)
   → inventory.Contents.TryRemove(item, 1) ; ancien équipement remis en inventaire
   → si armure : PlayerHealth.ApplyArmorBonus(±BonusMaxHP)

InventoryUI (bouton « Fabriquer ») → PlayerCrafting.TryCraft(recipe)
   → CraftingSystem.TryCraft() : retire les ingrédients, ajoute le résultat
```

---

## 9. Pour aller plus loin

- Roadmap détaillée par phase, stack technique, installation : [`README.md`](../README.md)
- Conventions de code (nommage, langue, style) : section « Conventions de code » du `README.md`
