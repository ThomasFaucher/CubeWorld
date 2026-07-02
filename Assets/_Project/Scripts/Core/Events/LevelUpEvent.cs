namespace CubeWorld.Core
{
    public readonly struct LevelUpEvent : IGameEvent
    {
        public readonly int NewLevel;

        public LevelUpEvent(int newLevel)
        {
            NewLevel = newLevel;
        }
    }
}
