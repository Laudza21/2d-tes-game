using UnityEngine;

namespace NPCCustomization
{
    /// <summary>
    /// Enum untuk classify accessories: Permanent (tidak bisa ganti) atau Switchable (bisa ganti based on time/day)
    /// </summary>
    public enum AccessoryType
    {
        /// <summary>
        /// Permanent accessories seperti Beard, Elf Ears, Frog - bagian dari identitas NPC yang tidak berubah
        /// </summary>
        Permanent,
        
        /// <summary>
        /// Switchable accessories seperti Topi, Glasses, Wings - bisa ganti based on time/day
        /// </summary>
        Switchable
    }

    /// <summary>
    /// Category untuk NPC parts
    /// </summary>
    public enum PartCategory
    {
        Skin,
        Hair,
        Eyes,
        Clothes,
        Accessory
    }
}
