namespace CubeWorld.CharacterModel.Rig
{
    /// <summary>
    /// Noms STABLES des os et des sockets du rig. Un Animator Controller retrouve ses courbes
    /// par chemin de Transform (ex. "Hips/Torso/ArmL") : ces noms ne doivent jamais changer
    /// d'un seed ou d'un archétype à l'autre, sinon les animations créées dans l'éditeur
    /// casseraient. Hiérarchie produite par CharacterModelBuilder :
    ///
    /// CharacterRoot
    /// └── Hips
    ///     ├── Torso
    ///     │   ├── Head
    ///     │   ├── ArmL ── ForearmL
    ///     │   └── ArmR ── ForearmR
    ///     ├── LegL ── FootL
    ///     └── LegR ── FootR
    /// </summary>
    internal static class CharacterRigDefinition
    {
        // Os (noms de GameObjects).
        public const string Root = "CharacterRoot";
        public const string Hips = "Hips";
        public const string Torso = "Torso";
        public const string Head = "Head";
        public const string ArmL = "ArmL";
        public const string ArmR = "ArmR";
        public const string ForearmL = "ForearmL";
        public const string ForearmR = "ForearmR";
        public const string LegL = "LegL";
        public const string LegR = "LegR";
        public const string FootL = "FootL";
        public const string FootR = "FootR";

        /// <summary>Équipement décoratif (pas un os d'animation) : épée portée dans le dos.</summary>
        public const string SwordBack = "SwordBack";

        /// <summary>Équipement décoratif (pas un os d'animation) : pioche (outil).</summary>
        public const string PickaxeBack = "PickaxeBack";

        /// <summary>
        /// Ancre statique (Transform vide, pas de mesh) posée sur le socket Back du
        /// torse à la construction du personnage — voir CharacterModelBuilder.Build.
        /// L'équipement (PlayerGearVisual) y monte/démonte le mesh d'arme dynamiquement
        /// au fil des changements d'équipement, sans jamais retoucher au rig lui-même.
        /// </summary>
        public const string GearBackAnchor = "GearBackAnchor";

        /// <summary>
        /// Ancre statique posée sur le socket BackTool du torse (voir SocketBackTool) :
        /// port dans le dos de l'outil (pioche) équipé, distinct de GearBackAnchor pour
        /// que l'arme ET l'outil puissent être visibles en même temps sans se superposer.
        /// </summary>
        public const string ToolBackAnchor = "ToolBackAnchor";

        /// <summary>
        /// Ancre statique posée sur le socket Grip de l'avant-bras droit (voir SocketGrip) :
        /// point où PlayerGearVisual déplace le mesh d'arme/outil pendant qu'il est
        /// activement tenu en main (attaque/minage), avant de le rendre au dos.
        /// </summary>
        public const string WeaponGripAnchor = "WeaponGripAnchor";

        // Sockets (points d'ancrage exposés par les pièces).
        public const string SocketTorso = "Torso";
        public const string SocketNeck = "Neck";
        public const string SocketShoulderL = "ShoulderL";
        public const string SocketShoulderR = "ShoulderR";
        public const string SocketElbow = "Elbow";
        public const string SocketHipL = "HipL";
        public const string SocketHipR = "HipR";
        public const string SocketAnkle = "Ankle";
        public const string SocketBack = "Back";

        /// <summary>Deuxième point de port dans le dos (voir ToolBackAnchor), incliné à l'opposé de SocketBack.</summary>
        public const string SocketBackTool = "BackTool";

        /// <summary>Point de préhension dans la moufle de l'avant-bras (voir WeaponGripAnchor).</summary>
        public const string SocketGrip = "Grip";
    }
}
