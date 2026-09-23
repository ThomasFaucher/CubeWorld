namespace CubeWorld.Combat
{
    /// <summary>
    /// Forme voxel affichée sur le joueur pour cet outil (voir
    /// Player.CharacterModel.PlayerGearVisual). Découplé de <see cref="ToolDefinition"/> pour
    /// les mêmes raisons que <see cref="WeaponVisualKind"/> vis-à-vis de WeaponDefinition.
    /// </summary>
    public enum ToolVisualKind
    {
        /// <summary>Aucun visuel — l'ancre reste vide.</summary>
        None = 0,

        /// <summary>Pioche portée dans le dos — voir GearGenerator.BuildPickaxe.</summary>
        Pickaxe = 1,
    }
}
