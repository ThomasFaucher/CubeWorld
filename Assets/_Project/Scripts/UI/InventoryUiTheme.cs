using UnityEngine;

namespace CubeWorld.UI
{
    /// <summary>Palette partagée inventaire / hotbar / tooltips.</summary>
    public static class InventoryUiTheme
    {
        public static readonly Color Overlay = new(0.02f, 0.03f, 0.04f, 0.62f);
        public static readonly Color WindowBg = new(0.08f, 0.1f, 0.12f, 0.96f);
        public static readonly Color PanelInner = new(0.06f, 0.07f, 0.09f, 0.9f);
        public static readonly Color Accent = new(0.92f, 0.72f, 0.28f, 1f);
        public static readonly Color AccentMuted = new(0.92f, 0.72f, 0.28f, 0.35f);
        public static readonly Color TextPrimary = new(0.95f, 0.94f, 0.9f, 1f);
        public static readonly Color TextMuted = new(0.7f, 0.72f, 0.68f, 0.85f);
        public static readonly Color SlotEmpty = new(0.12f, 0.14f, 0.16f, 0.92f);
        public static readonly Color SlotFilled = new(0.16f, 0.18f, 0.2f, 0.96f);
        public static readonly Color SlotBorder = new(1f, 1f, 1f, 0.1f);
        public static readonly Color EquipFrame = new(0.14f, 0.16f, 0.18f, 0.98f);
        public static readonly Color EquipFrameEmpty = new(0.11f, 0.12f, 0.14f, 0.85f);
        public static readonly Color Button = new(0.22f, 0.42f, 0.34f, 1f);
        public static readonly Color ButtonDisabled = new(0.18f, 0.2f, 0.22f, 0.9f);
        public static readonly Color Danger = new(0.75f, 0.32f, 0.28f, 1f);

        public const float WindowWidth = 1020f;
        public const float WindowHeight = 560f;
        public const float SlotSize = 56f;
        public const float SlotGap = 6f;
        public const float EquipSlotSize = 78f;
    }
}
