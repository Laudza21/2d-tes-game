using UnityEngine;
using System.Collections.Generic;

namespace NPCCustomization
{
    /// <summary>
    /// ScriptableObject yang menyimpan kombinasi parts untuk satu NPC.
    /// Includes permanent parts dan switchable accessory pools.
    /// </summary>
    [CreateAssetMenu(fileName = "NPCPreset_", menuName = "NPC Customization/NPC Preset")]
    public class NPCCustomizationPreset : ScriptableObject
    {
        [Header("Permanent Parts")]
        [Tooltip("Skin part (warna kulit)")]
        public NPCPartData skin;
        
        [Tooltip("Hair part (style + warna)")]
        public NPCPartData hair;
        
        [Tooltip("Eyes part (Female/Male)")]
        public NPCPartData eyes;
        
        [Tooltip("Clothes part")]
        public NPCPartData clothes;
        
        [Tooltip("Permanent accessories (Beard, Elf Ears, dll - bagian dari identitas NPC)")]
        public List<NPCPartData> permanentAccessories = new List<NPCPartData>();

        [Header("Switchable Accessory Pools")]
        [Tooltip("Slot 1: Pool of accessories (misal: berbagai topi)")]
        public List<NPCPartData> switchableSlot1 = new List<NPCPartData>();
        
        [Tooltip("Slot 2: Pool of accessories (misal: berbagai glasses)")]
        public List<NPCPartData> switchableSlot2 = new List<NPCPartData>();
        
        [Tooltip("Slot 3: Pool of accessories")]
        public List<NPCPartData> switchableSlot3 = new List<NPCPartData>();
        
        [Tooltip("Slot 4: Pool of accessories")]
        public List<NPCPartData> switchableSlot4 = new List<NPCPartData>();

        [Header("Default Selections")]
        [Tooltip("Default accessory index untuk slot 1 (0 = first item)")]
        public int defaultSlot1Index = 0;
        
        [Tooltip("Default accessory index untuk slot 2")]
        public int defaultSlot2Index = 0;
        
        [Tooltip("Default accessory index untuk slot 3")]
        public int defaultSlot3Index = 0;
        
        [Tooltip("Default accessory index untuk slot 4")]
        public int defaultSlot4Index = 0;

        /// <summary>
        /// Get switchable accessory pool by slot index
        /// </summary>
        public List<NPCPartData> GetSwitchablePool(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0: return switchableSlot1;
                case 1: return switchableSlot2;
                case 2: return switchableSlot3;
                case 3: return switchableSlot4;
                default: return new List<NPCPartData>();
            }
        }

        /// <summary>
        /// Get default accessory untuk slot tertentu
        /// </summary>
        public NPCPartData GetDefaultAccessory(int slotIndex)
        {
            List<NPCPartData> pool = GetSwitchablePool(slotIndex);
            int defaultIndex = GetDefaultIndex(slotIndex);
            
            if (pool.Count == 0 || defaultIndex < 0 || defaultIndex >= pool.Count)
            {
                return null;
            }
            
            return pool[defaultIndex];
        }

        /// <summary>
        /// Get default index untuk slot tertentu
        /// </summary>
        public int GetDefaultIndex(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0: return defaultSlot1Index;
                case 1: return defaultSlot2Index;
                case 2: return defaultSlot3Index;
                case 3: return defaultSlot4Index;
                default: return 0;
            }
        }

        /// <summary>
        /// Validate preset - check semua parts valid
        /// </summary>
        public bool IsValid()
        {
            // Minimal harus ada skin
            if (skin == null)
            {
                Debug.LogWarning($"NPCPreset {name}: Missing skin!");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Get all permanent parts (skin, hair, eyes, clothes, permanent accessories)
        /// </summary>
        public List<NPCPartData> GetAllPermanentParts()
        {
            List<NPCPartData> parts = new List<NPCPartData>();
            
            if (skin != null) parts.Add(skin);
            if (hair != null) parts.Add(hair);
            if (eyes != null) parts.Add(eyes);
            if (clothes != null) parts.Add(clothes);
            
            foreach (var acc in permanentAccessories)
            {
                if (acc != null) parts.Add(acc);
            }
            
            return parts;
        }
    }
}
