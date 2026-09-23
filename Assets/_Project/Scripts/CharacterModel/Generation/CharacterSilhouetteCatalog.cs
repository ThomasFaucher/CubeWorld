namespace CubeWorld.CharacterModel.Generation
{
    /// <summary>
    /// Catalogue FIGÉ (pas de RNG) de <see cref="CharacterSilhouette"/> par
    /// <see cref="CharacterArchetype"/> — le pattern recommandé par le README de CharacterModel
    /// pour de vrais types visuels distincts (voir §6, "Vrais archétypes/skins distincts").
    /// Ajouter un nouvel archétype visuel = ajouter une entrée ici, jamais une plage
    /// aléatoire dans <see cref="CharacterRng"/>.
    /// </summary>
    internal static class CharacterSilhouetteCatalog
    {
        /// <summary>
        /// Épéiste humain : bonnet pointu, buste plein en tonneau (baudrier diagonal), grosse
        /// épaulière de cuir — le combattant lourd de référence.
        /// </summary>
        private static readonly CharacterSilhouette Swordsman = new(
            hasPointedEars: false,
            hasHair: true,
            cap: CapStyle.Pointed,
            hasEyes: true,
            hasBlush: true,
            hasSkullFace: false,
            torso: TorsoStyle.Solid,
            hasPauldrons: true
        );

        /// <summary>
        /// Archer humain : tête nue (juste les cheveux, aucun couvre-tête) et pas
        /// d'épaulière — silhouette dégagée et agile, sans l'armure lourde de l'épéiste.
        /// </summary>
        private static readonly CharacterSilhouette Archer = new(
            hasPointedEars: false,
            hasHair: true,
            cap: CapStyle.None,
            hasEyes: true,
            hasBlush: true,
            hasSkullFace: false,
            torso: TorsoStyle.Solid,
            hasPauldrons: false
        );

        /// <summary>
        /// Mage humain : chapeau de sorcier à large bord, robe ample sans baudrier martial
        /// (voir <see cref="TorsoStyle.Robe"/>), pas d'épaulière — silhouette de lanceur de
        /// sorts sans équipement de mêlée.
        /// </summary>
        private static readonly CharacterSilhouette Mage = new(
            hasPointedEars: false,
            hasHair: true,
            cap: CapStyle.Wizard,
            hasEyes: true,
            hasBlush: true,
            hasSkullFace: false,
            torso: TorsoStyle.Robe,
            hasPauldrons: false
        );

        /// <summary>
        /// Elfe : oreilles pointues, bonnet pointu et grosse épaulière comme l'épéiste — seule
        /// silhouette à porter des oreilles pointues, différenciée surtout par sa palette
        /// (voir <see cref="CharacterPalette"/>).
        /// </summary>
        private static readonly CharacterSilhouette Elf = new(
            hasPointedEars: true,
            hasHair: true,
            cap: CapStyle.Pointed,
            hasEyes: true,
            hasBlush: true,
            hasSkullFace: false,
            torso: TorsoStyle.Solid,
            hasPauldrons: true
        );

        /// <summary>
        /// Squelette : crâne nu à orbites ET nez creusés + mâchoire à dents, cage thoracique à
        /// claire-voie, os nus.
        /// </summary>
        private static readonly CharacterSilhouette Skeleton = new(
            hasPointedEars: false,
            hasHair: false,
            cap: CapStyle.None,
            hasEyes: false,
            hasBlush: false,
            hasSkullFace: true,
            torso: TorsoStyle.Ribcage,
            hasPauldrons: false
        );

        public static CharacterSilhouette Get(CharacterArchetype archetype) =>
            archetype switch
            {
                CharacterArchetype.Archer => Archer,
                CharacterArchetype.Mage => Mage,
                CharacterArchetype.Elf => Elf,
                CharacterArchetype.Skeleton => Skeleton,
                _ => Swordsman,
            };
    }
}
