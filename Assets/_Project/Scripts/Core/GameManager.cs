using UnityEngine;

namespace CubeWorld.Core
{
    /// <summary>
    /// Point d'entrée du jeu : unique, survit aux changements de scène,
    /// séquence l'initialisation des systèmes. Aucune logique métier ici —
    /// elle vit dans les classes des assemblies World/Player/Combat/UI.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            // Garantit l'unicité : une seule instance dans toute la session.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                EventBus.Clear();
            }
        }
    }
}
