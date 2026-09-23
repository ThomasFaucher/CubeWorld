namespace CubeWorld.CharacterModel
{
    /// <summary>
    /// Silhouette voxel d'un personnage généré (joueur ou ennemi). Chaque valeur résout un
    /// preset figé dans <see cref="Generation.CharacterSilhouetteCatalog"/> (forme) + une
    /// palette dédiée dans <see cref="Generation.CharacterPalette"/> (couleurs) — voir
    /// <see cref="CharacterModelBuilder.Build"/>.
    ///
    /// Anciennement "PlayerArchetype" dans l'assemblée CubeWorld.Player : renommé en
    /// migrant tout le pipeline CharacterModel dans sa propre assemblée
    /// (CubeWorld.CharacterModel), pour que CubeWorld.Combat (ennemis) puisse le réutiliser
    /// sans dépendre de CubeWorld.Player — voir CubeWorld.Combat.EnemyDefinition/EnemyAI.
    /// </summary>
    public enum CharacterArchetype
    {
        Swordsman,
        Archer,
        Mage,
        Elf,
        Skeleton,
    }
}
