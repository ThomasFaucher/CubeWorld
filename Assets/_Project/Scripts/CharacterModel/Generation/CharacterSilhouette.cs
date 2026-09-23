namespace CubeWorld.CharacterModel.Generation
{
    /// <summary>Silhouette du torse : buste plein (tunique), cage thoracique (squelette) ou robe ample (mage).</summary>
    internal enum TorsoStyle
    {
        Solid,
        Ribcage,
        Robe,
    }

    /// <summary>
    /// Style de couvre-tête stampé par-dessus les cheveux (voir
    /// CubeWorld.CharacterModel.Parts.HeadGenerator.StampHairAndCap). Indépendant de
    /// <see cref="CharacterSilhouette.HasHair"/> : un personnage peut avoir des cheveux
    /// peints sans aucun couvre-tête (<see cref="None"/>).
    /// </summary>
    internal enum CapStyle
    {
        /// <summary>Aucun couvre-tête : seule la bande de cheveux reste visible (tête nue/archer).</summary>
        None,

        /// <summary>Bonnet pointu snug incliné vers l'arrière — silhouette héros/elfe classique.</summary>
        Pointed,

        /// <summary>Chapeau de sorcier : large bord évasé à la base puis cône haut et pointu.</summary>
        Wizard,
    }

    /// <summary>
    /// Flags de FORME d'un archétype visuel — tout ce qui n'est pas une simple variation de
    /// couleur (voir <see cref="CharacterPalette"/> pour ça). Figés par
    /// <see cref="CharacterSilhouetteCatalog"/>, jamais tirés par <see cref="CharacterRng"/> :
    /// un archétype donné a toujours la même silhouette, seules les couleurs/proportions
    /// varient par seed à l'intérieur de ce gabarit.
    /// </summary>
    internal readonly struct CharacterSilhouette
    {
        /// <summary>Oreilles pointues d'elfe stampées sur le flanc du crâne.</summary>
        public readonly bool HasPointedEars;

        /// <summary>Mèches peintes sur le crâne + sourcils (sinon crâne nu).</summary>
        public readonly bool HasHair;

        /// <summary>Style de couvre-tête stampé par-dessus les cheveux (voir <see cref="CapStyle"/>).</summary>
        public readonly CapStyle Cap;

        /// <summary>Sclère+iris peints et éclat de regard (sinon orbite creuse).</summary>
        public readonly bool HasEyes;

        /// <summary>Blush peint sur les joues.</summary>
        public readonly bool HasBlush;

        /// <summary>
        /// Détails de crâne osseux : cavité nasale creusée + mâchoire à dents (voir
        /// CubeWorld.CharacterModel.Parts.HeadGenerator). Indépendant de
        /// <see cref="HasEyes"/> — un futur archétype pourrait avoir des orbites creuses sans
        /// être un crâne (ex. aveugle) ou l'inverse.
        /// </summary>
        public readonly bool HasSkullFace;

        /// <summary>Style du buste (voir <see cref="TorsoStyle"/>).</summary>
        public readonly TorsoStyle Torso;

        /// <summary>Épaulière de cuir surdimensionnée sur le bras haut.</summary>
        public readonly bool HasPauldrons;

        public CharacterSilhouette(
            bool hasPointedEars,
            bool hasHair,
            CapStyle cap,
            bool hasEyes,
            bool hasBlush,
            bool hasSkullFace,
            TorsoStyle torso,
            bool hasPauldrons
        )
        {
            HasPointedEars = hasPointedEars;
            HasHair = hasHair;
            Cap = cap;
            HasEyes = hasEyes;
            HasBlush = hasBlush;
            HasSkullFace = hasSkullFace;
            Torso = torso;
            HasPauldrons = hasPauldrons;
        }
    }
}
