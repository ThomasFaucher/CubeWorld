using System;

namespace CubeWorld.Player
{
    [Serializable]
    public sealed class InventorySaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public InventorySlotSave[] slots;
        public string weaponId;
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
