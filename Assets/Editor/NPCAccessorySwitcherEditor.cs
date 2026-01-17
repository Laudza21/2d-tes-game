using UnityEditor;
using UnityEngine;
using System;

namespace NPCCustomization
{
    [CustomEditor(typeof(NPCAccessorySwitcher))]
    public class NPCAccessorySwitcherEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            NPCAccessorySwitcher switcher = (NPCAccessorySwitcher)target;

            // Show Debug Info section when in Play mode
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Debug Info", EditorStyles.boldLabel);

                // Get current time from GameTimeManager
                if (switcher.gameTimeManager != null)
                {
                    DayOfWeek currentDay = switcher.gameTimeManager.GetCurrentDay();
                    int currentHour = switcher.gameTimeManager.GetCurrentHour();

                    EditorGUILayout.LabelField("Current Day:", currentDay.ToString());
                    EditorGUILayout.LabelField("Current Hour:", currentHour.ToString());

                    // Show active accessories
                    EditorGUILayout.LabelField("Active Accessories:", EditorStyles.label);
                    
                    if (switcher.accessoryRules != null && switcher.accessoryRules.Length > 0)
                    {
                        foreach (var rule in switcher.accessoryRules)
                        {
                            if (rule == null) continue;

                            var accessories = rule.GetAccessoriesForTime(currentDay, currentHour);
                            foreach (var kvp in accessories)
                            {
                                string accName = kvp.Value != null ? kvp.Value.partName : "None";
                                EditorGUILayout.LabelField($"  Slot {kvp.Key}:", accName, EditorStyles.miniLabel);
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("  (No rules assigned)", EditorStyles.miniLabel);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("GameTimeManager not found!", MessageType.Warning);
                }

                // Force repaint to update time display
                Repaint();
            }
        }
    }
}
