using CubeWorld.Combat;
using UnityEngine;

namespace CubeWorld.Player
{
    /// <summary>Possède l'Inventory du joueur (capacité fixe, voir Combat.Inventory).</summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField]
        private int _capacity = 20;

        public Inventory Contents { get; private set; }

        private void Awake()
        {
            Contents = new Inventory(_capacity);
        }
    }
}
