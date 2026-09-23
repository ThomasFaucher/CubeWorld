using System;
using System.IO;
using CubeWorld.Combat;
using UnityEngine;

namespace CubeWorld.Player
{
    /// <summary>
    /// Save/load JSON de l'inventaire, équipement et index hotbar.
    /// Fichier : persistentDataPath/cubeworld_inventory.json
    /// </summary>
    public sealed class InventorySaveService : MonoBehaviour
    {
        private const string FileName = "cubeworld_inventory.json";
        private const float DebounceSeconds = 0.5f;

        private ItemCatalog catalog;
        private PlayerInventory inventory;
        private PlayerEquipment equipment;
        private PlayerHotbar hotbar;
        private float saveAtTime = -1f;
        private bool bound;
        private bool isLoading;

        public static string SavePath =>
            Path.Combine(Application.persistentDataPath, FileName);

        public void Bind(
            ItemCatalog itemCatalog,
            PlayerInventory playerInventory,
            PlayerEquipment playerEquipment,
            PlayerHotbar playerHotbar
        )
        {
            catalog = itemCatalog;
            inventory = playerInventory;
            equipment = playerEquipment;
            hotbar = playerHotbar;
            bound = catalog != null && inventory != null && equipment != null && hotbar != null;

            if (!bound)
            {
                Debug.LogWarning("[CubeWorld] InventorySaveService : bind incomplet.");
                return;
            }

            inventory.ContentsChanged += ScheduleSave;
            equipment.EquipmentChanged += ScheduleSave;
            hotbar.SelectionChanged += ScheduleSave;
        }

        private void OnDestroy()
        {
            if (inventory != null)
            {
                inventory.ContentsChanged -= ScheduleSave;
            }

            if (equipment != null)
            {
                equipment.EquipmentChanged -= ScheduleSave;
            }

            if (hotbar != null)
            {
                hotbar.SelectionChanged -= ScheduleSave;
            }
        }

        private void Update()
        {
            if (saveAtTime < 0f || Time.unscaledTime < saveAtTime)
            {
                return;
            }

            saveAtTime = -1f;
            Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Save();
            }
        }

        private void ScheduleSave()
        {
            if (!bound || isLoading)
            {
                return;
            }

            saveAtTime = Time.unscaledTime + DebounceSeconds;
        }

        public bool TryLoad()
        {
            if (!bound)
            {
                return false;
            }

            string path = SavePath;
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                InventorySaveData data = JsonUtility.FromJson<InventorySaveData>(json);
                if (data == null || data.version > InventorySaveData.CurrentVersion)
                {
                    return false;
                }

                Apply(data);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CubeWorld] Load inventaire échoué : {ex.Message}");
                return false;
            }
        }

        public void Save()
        {
            if (!bound)
            {
                return;
            }

            try
            {
                InventorySaveData data = Capture();
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CubeWorld] Save inventaire échoué : {ex.Message}");
            }
        }

        private InventorySaveData Capture()
        {
            var data = new InventorySaveData
            {
                version = InventorySaveData.CurrentVersion,
                slots = new InventorySlotSave[inventory.Capacity],
                weaponId = IdOf(equipment.WeaponItem),
                toolId = IdOf(equipment.ToolItem),
                headId = IdOf(equipment.GetArmor(ArmorSlot.Head)),
                chestId = IdOf(equipment.GetArmor(ArmorSlot.Chest)),
                legsId = IdOf(equipment.GetArmor(ArmorSlot.Legs)),
                selectedHotbarIndex = hotbar.SelectedIndex,
            };

            for (int i = 0; i < inventory.Capacity; i++)
            {
                Inventory.Slot slot = inventory.GetSlot(i);
                data.slots[i] = new InventorySlotSave
                {
                    id = slot.Item != null ? slot.Item.Id : string.Empty,
                    quantity = slot.Item != null ? slot.Quantity : 0,
                };
            }

            return data;
        }

        private void Apply(InventorySaveData data)
        {
            isLoading = true;
            try
            {
                inventory.Contents.Clear();

                if (data.slots != null)
                {
                    int count = Mathf.Min(data.slots.Length, inventory.Capacity);
                    for (int i = 0; i < count; i++)
                    {
                        InventorySlotSave slot = data.slots[i];
                        if (slot == null || string.IsNullOrEmpty(slot.id) || slot.quantity <= 0)
                        {
                            continue;
                        }

                        ItemDefinition item = catalog.GetById(slot.id);
                        if (item == null)
                        {
                            Debug.LogWarning(
                                $"[CubeWorld] Save : item inconnu « {slot.id} », slot {i} ignoré."
                            );
                            continue;
                        }

                        inventory.Contents.SetSlot(i, item, slot.quantity);
                    }
                }

                equipment.RestoreEquipped(
                    catalog.GetById(data.weaponId),
                    catalog.GetById(data.toolId),
                    catalog.GetById(data.headId),
                    catalog.GetById(data.chestId),
                    catalog.GetById(data.legsId)
                );

                hotbar.SelectIndex(data.selectedHotbarIndex);
                inventory.NotifyContentsChanged();
            }
            finally
            {
                isLoading = false;
            }
        }

        private static string IdOf(ItemDefinition item) =>
            item != null ? item.Id : string.Empty;
    }
}
