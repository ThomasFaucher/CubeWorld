namespace CubeWorld.Core
{
    /// <summary>
    /// Interface marqueur : tout événement publié sur l'<see cref="EventBus"/>
    /// doit l'implémenter. On utilise des structs pour éviter les allocations.
    /// </summary>
    public interface IGameEvent
    {
    }
}
