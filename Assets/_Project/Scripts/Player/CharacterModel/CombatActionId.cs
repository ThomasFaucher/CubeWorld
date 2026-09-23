namespace CubeWorld.Player.CharacterModel
{
    /// <summary>
    /// Valeurs du paramètre Animator "ActionId" (voir CharacterCombatAnimationEvents et
    /// CubeWorld.EditorTools.CharacterLocomotionAnimatorGenerator, qui construit les états du
    /// layer "Combat" avec exactement ces mêmes valeurs) : chaque action de combat/minage
    /// correspond à un clip précis choisi par PlayerCombat/PlayerMining avant de déclencher
    /// "ActionTrigger". Public (contrairement aux chemins d'os, qui restent des chaînes en dur
    /// côté générateur) : rien n'empêche l'assemblée Editor par défaut de référencer
    /// CubeWorld.Player, donc pas de risque de désynchronisation à documenter manuellement.
    /// </summary>
    public static class CombatActionId
    {
        public const int SwordDrawSlash1 = 0;
        public const int SwordSlash1 = 1;
        public const int SwordSlash2 = 2;
        public const int SwordSlash3 = 3;
        public const int SwordSheath = 4;

        public const int PunchJabL = 5;
        public const int PunchJabR = 6;
        public const int PunchCross = 7;

        public const int PickaxeDrawSwing = 8;
        public const int PickaxeSwing = 9;
        public const int PickaxeSheath = 10;
    }
}
