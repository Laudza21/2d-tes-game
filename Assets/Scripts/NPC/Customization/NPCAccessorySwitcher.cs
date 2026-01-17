using UnityEngine;
using System;
using GameTime;

namespace NPCCustomization
{
    /// <summary>
    /// Component yang handle automatic accessory switching based on time/day.
    /// Subscribe ke GameTimeManager dan apply rules dari TimeBasedAccessoryRule.
    /// </summary>
    [RequireComponent(typeof(ModularNPCRenderer))]
    public class NPCAccessorySwitcher : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Auto-detect ModularNPCRenderer component")]
        public bool autoDetectRenderer = true;
        
        [Tooltip("Auto-detect GameTimeManager in scene")]
        public bool autoDetectTimeManager = true;
        
        [Tooltip("Manual reference to Modular NPC Renderer (if auto-detect fails)")]
        public ModularNPCRenderer modularRenderer;
        
        [Tooltip("Manual reference to GameTimeManager (if auto-detect fails)")]
        [SerializeField] public GameTimeManager gameTimeManager;

        private ModularNPCRenderer npcRenderer;

        [Header("Switching Rules")]
        [Tooltip("Time-based accessory switching rules (can have multiple for different slots)")]
        public TimeBasedAccessoryRule[] accessoryRules;

        [Header("Settings")]
        [Tooltip("Enable/disable automatic switching")]
        public bool enableAutoSwitching = true;

        [Tooltip("Log switching events untuk debug")]
        public bool logSwitchingEvents = false;
        
        [Tooltip("Update interval in seconds (how often to check rules)")]
        public float updateInterval = 1.0f;

        void Awake()
        {
            // Auto-detect or use manual reference for ModularNPCRenderer
            if (autoDetectRenderer)
            {
                npcRenderer = GetComponent<ModularNPCRenderer>();
            }
            else if (modularRenderer != null)
            {
                npcRenderer = modularRenderer;
            }
            
            if (npcRenderer == null)
            {
                Debug.LogError($"[{gameObject.name}] ModularNPCRenderer not found! Auto-switching will not work.");
            }
            
            // Auto-detect or use manual reference for GameTimeManager
            if (autoDetectTimeManager)
            {
                gameTimeManager = FindFirstObjectByType<GameTimeManager>();
            }
            
            if (gameTimeManager == null)
            {
                Debug.LogWarning($"[{gameObject.name}] GameTimeManager not found! Auto-switching will not work.");
            }
        }

        void Start()
        {
            // Subscribe ke GameTimeManager events
            SubscribeToTimeEvents();

            // Apply initial accessories based on current time
            if (enableAutoSwitching && accessoryRules != null && accessoryRules.Length > 0)
            {
                ApplyAccessoriesForCurrentTime();
            }
        }

        void OnDestroy()
        {
            // Unsubscribe dari events
            UnsubscribeFromTimeEvents();
        }

        /// <summary>
        /// Subscribe ke GameTimeManager events
        /// </summary>
        private void SubscribeToTimeEvents()
        {
            if (gameTimeManager != null)
            {
                gameTimeManager.OnHourChanged += OnTimeChanged;
                gameTimeManager.OnDayChanged += OnDayChanged;
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] GameTimeManager not found! NPCAccessorySwitcher will not work.");
            }
        }

        /// <summary>
        /// Unsubscribe dari events
        /// </summary>
        private void UnsubscribeFromTimeEvents()
        {
            if (gameTimeManager != null)
            {
                gameTimeManager.OnHourChanged -= OnTimeChanged;
                gameTimeManager.OnDayChanged -= OnDayChanged;
            }
        }

        /// <summary>
        /// Called when hour changes
        /// </summary>
        private void OnTimeChanged(DayOfWeek currentDay, int currentHour)
        {
            if (!enableAutoSwitching || accessoryRules == null || accessoryRules.Length == 0) return;

            ApplyAccessoriesForTime(currentDay, currentHour);
        }

        /// <summary>
        /// Called when day changes
        /// </summary>
        private void OnDayChanged(DayOfWeek newDay)
        {
            if (!enableAutoSwitching || accessoryRules == null || accessoryRules.Length == 0) return;

            // Get current hour dari GameTimeManager
            int currentHour = gameTimeManager != null ? gameTimeManager.GetCurrentHour() : 0;
            
            ApplyAccessoriesForTime(newDay, currentHour);
        }

        /// <summary>
        /// Apply accessories untuk current time (dipanggil saat Start)
        /// </summary>
        private void ApplyAccessoriesForCurrentTime()
        {
            if (gameTimeManager == null) return;

            DayOfWeek currentDay = gameTimeManager.GetCurrentDay();
            int currentHour = gameTimeManager.GetCurrentHour();

            ApplyAccessoriesForTime(currentDay, currentHour);
        }

        /// <summary>
        /// Apply accessories based on specific time
        /// </summary>
        private void ApplyAccessoriesForTime(DayOfWeek day, int hour)
        {
            if (npcRenderer == null || accessoryRules == null || accessoryRules.Length == 0) return;

            // Loop through all rules
            foreach (var rule in accessoryRules)
            {
                if (rule == null) continue;
                
                // Get accessories dari this rule
                var accessories = rule.GetAccessoriesForTime(day, hour);

                // Apply setiap accessory ke slot-nya
                foreach (var kvp in accessories)
                {
                    int slotIndex = kvp.Key;
                    NPCPartData accessory = kvp.Value;

                    npcRenderer.SetSwitchableAccessory(slotIndex, accessory);

                    if (logSwitchingEvents)
                    {
                        string accName = accessory != null ? accessory.partName : "None";
                        Debug.Log($"[{gameObject.name}] Switched Slot {slotIndex} to: {accName} ({day} {hour:D2}:00)");
                    }
                }
            }
        }

        /// <summary>
        /// Manually apply accessories untuk specific time (untuk testing)
        /// </summary>
        public void ManuallyApplyAccessories(DayOfWeek day, int hour)
        {
            ApplyAccessoriesForTime(day, hour);
        }

        /// <summary>
        /// Clear specific accessory slot
        /// </summary>
        public void ClearAccessorySlot(int slotIndex)
        {
            if (npcRenderer != null)
            {
                npcRenderer.SetSwitchableAccessory(slotIndex, null);
            }
        }

        /// <summary>
        /// Clear all switchable accessories
        /// </summary>
        public void ClearAllAccessories()
        {
            if (npcRenderer != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    npcRenderer.SetSwitchableAccessory(i, null);
                }
            }
        }
    }
}
