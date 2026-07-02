using System;
using System.Collections.Generic;

namespace CubeWorld.Core
{
    /// <summary>
    /// Bus d'événements typé (publish/subscribe). Permet aux systèmes de
    /// communiquer sans se référencer directement : le monde publie
    /// « ChunkGenerated », l'UI s'y abonne, sans qu'aucun des deux ne
    /// connaisse l'autre.
    /// </summary>
    public static class EventBus
    {
        // Une liste de handlers par type d'événement.
        private static readonly Dictionary<Type, List<Delegate>> Handlers = new();

        /// <summary>S'abonne aux événements de type <typeparamref name="T"/>.</summary>
        public static void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (!Handlers.TryGetValue(typeof(T), out List<Delegate> list))
            {
                list = new List<Delegate>();
                Handlers[typeof(T)] = list;
            }

            list.Add(handler);
        }

        /// <summary>Se désabonne. À appeler systématiquement dans OnDisable/OnDestroy.</summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (Handlers.TryGetValue(typeof(T), out List<Delegate> list))
            {
                list.Remove(handler);
            }
        }

        /// <summary>Publie un événement à tous les abonnés du type <typeparamref name="T"/>.</summary>
        public static void Publish<T>(T gameEvent) where T : IGameEvent
        {
            if (!Handlers.TryGetValue(typeof(T), out List<Delegate> list))
            {
                return;
            }

            // Copie de la liste : un handler peut se désabonner pendant la publication.
            Delegate[] snapshot = list.ToArray();
            foreach (Delegate handler in snapshot)
            {
                ((Action<T>)handler).Invoke(gameEvent);
            }
        }

        /// <summary>Vide tous les abonnements (changement de scène, tests).</summary>
        public static void Clear()
        {
            Handlers.Clear();
        }
    }
}
