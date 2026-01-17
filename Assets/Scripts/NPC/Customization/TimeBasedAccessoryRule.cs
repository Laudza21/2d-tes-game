using UnityEngine;
using System;
using System.Collections.Generic;

namespace NPCCustomization
{
    /// <summary>
    /// Single rule untuk time-based accessory switching
    /// </summary>
    [Serializable]
    public class AccessoryTimeRule
    {
        [Tooltip("Hari dalam seminggu (0=Sunday, 1=Monday, dll)")]
        public DayOfWeek day;
        
        [Tooltip("Jam mulai (0-23)")]
        [Range(0, 23)]
        public int startHour;
        
        [Tooltip("Jam selesai (0-23)")]
        [Range(0, 23)]
        public int endHour = 23;
        
        [Tooltip("Accessory yang dipakai pada waktu ini")]
        public NPCPartData accessory;
        
        [Tooltip("Slot index yang di-apply (0-3)")]
        [Range(0, 3)]
        public int slotIndex;

        public AccessoryTimeRule()
        {
            day = DayOfWeek.Monday;
            startHour = 0;
            endHour = 23;
            slotIndex = 0;
        }

        public AccessoryTimeRule(DayOfWeek d, int start, int end, int slot, NPCPartData acc)
        {
            day = d;
            startHour = Mathf.Clamp(start, 0, 23);
            endHour = Mathf.Clamp(end, 0, 23);
            slotIndex = Mathf.Clamp(slot, 0, 3);
            accessory = acc;
        }

        /// <summary>
        /// Check apakah rule ini match dengan day/time yang diberikan
        /// </summary>
        public bool MatchesTime(DayOfWeek currentDay, int currentHour)
        {
            // Match day dan hour dalam range
            return day == currentDay && currentHour >= startHour && currentHour <= endHour;
        }
    }

    /// <summary>
    /// ScriptableObject untuk define time-based accessory switching rules
    /// </summary>
    [CreateAssetMenu(fileName = "AccessoryRules_", menuName = "NPC Customization/Accessory Time Rules")]
    public class TimeBasedAccessoryRule : ScriptableObject
    {
        [Header("Switching Rules")]
        [Tooltip("List of rules: kapan ganti accessory apa")]
        public List<AccessoryTimeRule> rules = new List<AccessoryTimeRule>();

        [Header("Settings")]
        [Tooltip("Enable/disable switching untuk testing")]
        public bool enableSwitching = true;

        /// <summary>
        /// Get accessories yang harus dipakai pada waktu tertentu
        /// Returns dictionary: slotIndex -> NPCPartData
        /// </summary>
        public Dictionary<int, NPCPartData> GetAccessoriesForTime(DayOfWeek currentDay, int currentHour)
        {
            Dictionary<int, NPCPartData> result = new Dictionary<int, NPCPartData>();
            
            if (!enableSwitching)
            {
                return result;
            }

            foreach (var rule in rules)
            {
                if (rule.MatchesTime(currentDay, currentHour))
                {
                    // Overwrite jika ada multiple rules untuk slot yang sama
                    result[rule.slotIndex] = rule.accessory;
                }
            }
            
            return result;
        }

        /// <summary>
        /// Get accessory untuk specific slot pada waktu tertentu
        /// </summary>
        public NPCPartData GetAccessoryForSlot(int slotIndex, DayOfWeek currentDay, int currentHour)
        {
            if (!enableSwitching)
            {
                return null;
            }

            // Cari rule yang match (yang terakhir akan digunakan jika ada multiple)
            NPCPartData result = null;
            
            foreach (var rule in rules)
            {
                if (rule.slotIndex == slotIndex && rule.MatchesTime(currentDay, currentHour))
                {
                    result = rule.accessory;
                }
            }
            
            return result;
        }

        /// <summary>
        /// Add rule baru
        /// </summary>
        public void AddRule(DayOfWeek day, int startHour, int endHour, int slotIndex, NPCPartData accessory)
        {
            rules.Add(new AccessoryTimeRule(day, startHour, endHour, slotIndex, accessory));
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Clear all rules
        /// </summary>
        public void ClearRules()
        {
            rules.Clear();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Get all unique days yang ada rules
        /// </summary>
        public List<DayOfWeek> GetUniqueDays()
        {
            HashSet<DayOfWeek> days = new HashSet<DayOfWeek>();
            foreach (var rule in rules)
            {
                days.Add(rule.day);
            }
            return new List<DayOfWeek>(days);
        }
    }
}
