namespace CubeWorld.Player.CharacterModel.Rig
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
    }
}
