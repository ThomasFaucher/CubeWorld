namespace CubeWorld.Combat
{
    /// <summary>
    /// Forme voxel affichée dans le dos du joueur pour cette arme (voir
    /// Player.CharacterModel.PlayerGearVisual). Découplé de <see cref="WeaponDefinition"/>
    /// des stats de combat : plusieurs armes peuvent partager la même silhouette, et une
    /// nouvelle silhouette n'affecte aucun calcul de dégâts/portée.
    /// </summary>
    public enum WeaponVisualKind
    {
        /// <summary>Aucun visuel de dos (ex. arme à mains nues) — l'ancre reste vide.</summary>
        None = 0,

        /// <summary>Épée portée dans le dos — voir GearGenerator.BuildSword.</summary>
        Sword = 1,
    }
}
