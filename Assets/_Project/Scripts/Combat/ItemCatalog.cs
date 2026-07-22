using System.Collections.Generic;
using UnityEngine;

namespace CubeWorld.Combat
{
    /// <summary>
    /// Registre Id → ItemDefinition pour save/load et lookups runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "CubeWorld/Item Catalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        [SerializeField]
        private ItemDefinition[] _items;

        private Dictionary<string, ItemDefinition> byId;

        public ItemDefinition GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            EnsureLookup();
            return byId.TryGetValue(id, out ItemDefinition item) ? item : null;
        }

        private void EnsureLookup()
        {
            if (byId != null)
            {
                return;
            }

            byId = new Dictionary<string, ItemDefinition>();
            if (_items == null)
            {
                return;
            }

            foreach (ItemDefinition item in _items)
            {
                if (item == null || string.IsNullOrEmpty(item.Id))
                {
                    continue;
                }

                byId[item.Id] = item;
            }
        }

        private void OnValidate()
        {
            byId = null;
        }
    }
}
