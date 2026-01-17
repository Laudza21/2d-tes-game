using UnityEngine;
using UnityEditor;
using System;

namespace GameTime
{
    [CustomEditor(typeof(GameTimeManager))]
    public class GameTimeManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty currentDayProp;
        private SerializedProperty currentHourProp;
        private SerializedProperty currentMinuteProp;
        private SerializedProperty secondsPerHourProp;
        private SerializedProperty autoAdvanceTimeProp;

        private void OnEnable()
        {
            currentDayProp = serializedObject.FindProperty("currentDay");
            currentHourProp = serializedObject.FindProperty("currentHour");
            currentMinuteProp = serializedObject.FindProperty("currentMinute");
            secondsPerHourProp = serializedObject.FindProperty("secondsPerHour");
            autoAdvanceTimeProp = serializedObject.FindProperty("autoAdvanceTime");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GameTimeManager manager = (GameTimeManager)target;

            EditorGUILayout.LabelField("Current Time", EditorStyles.boldLabel);

            // Track old values
            DayOfWeek oldDay = (DayOfWeek)currentDayProp.enumValueIndex;
            int oldHour = currentHourProp.intValue;
            int oldMinute = currentMinuteProp.intValue;

            // Draw current time fields
            EditorGUI.BeginChangeCheck();
            
            DayOfWeek newDay = (DayOfWeek)EditorGUILayout.EnumPopup("Current Day", oldDay);
            int newHour = EditorGUILayout.IntSlider("Current Hour", oldHour, 0, 23);
            int newMinute = EditorGUILayout.IntSlider("Current Minute", oldMinute, 0, 59);

            if (EditorGUI.EndChangeCheck())
            {
                // Values changed - apply via SetTime to fire events
                if (Application.isPlaying)
                {
                    manager.SetTime(newDay, newHour, newMinute);
                }
                else
                {
                    // In Edit mode, just update fields
                    currentDayProp.enumValueIndex = (int)newDay;
                    currentHourProp.intValue = newHour;
                    currentMinuteProp.intValue = newMinute;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Time Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(secondsPerHourProp, new GUIContent("Seconds Per Hour"));
            EditorGUILayout.PropertyField(autoAdvanceTimeProp, new GUIContent("Auto Advance Time"));

            serializedObject.ApplyModifiedProperties();

            // Show formatted time in Play mode
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox($"Time: {manager.GetTimeString()}", MessageType.Info);
                Repaint(); // Keep updating
            }
        }
    }
}
