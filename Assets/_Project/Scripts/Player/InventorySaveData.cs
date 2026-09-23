using System;

namespace CubeWorld.Player
{
    [Serializable]
    public sealed class InventorySaveData
    {
        // v2 : ajout de toolId (slot Outil). Rétro-compatible : absent d'une save v1,
        // JsonUtility le désérialise en null -> ItemCatalog.GetById(null) renvoie null.
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public InventorySlotSave[] slots;
        public string weaponId;
        public string toolId;
        public string headId;
        public string chestId;
        public string legsId;
        public int selectedHotbarIndex;
    }

    [Serializable]
    public sealed class InventorySlotSave
    {
        public string id;
        public int quantity;
    }
}
