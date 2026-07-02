namespace CubeWorld.Core
{
    public readonly struct ItemPickedUpEvent : IGameEvent
    {
        public readonly string ItemId;
        public readonly string DisplayName;
        public readonly int Quantity;

        public ItemPickedUpEvent(string itemId, string displayName, int quantity)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Quantity = quantity;
        }
    }
}
