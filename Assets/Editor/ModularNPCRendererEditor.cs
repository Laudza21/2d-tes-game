using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NPCCustomization.Editor
{
    /// <summary>
    /// Custom Inspector untuk ModularNPCRenderer.
    /// Provides user-friendly UI untuk select parts dan manage customization.
    /// </summary>
    [CustomEditor(typeof(ModularNPCRenderer))]
    public class ModularNPCRendererEditor : UnityEditor.Editor
    {
        private ModularNPCRenderer renderer;
        
        // Part listings
        private List<NPCPartData> allSkins;
        private List<NPCPartData> allHair;
        private List<NPCPartData> allEyes;
        private List<NPCPartData> allClothes;
        private List<NPCPartData> allAccessories;

        // Selected indices
        private int selectedSkinIndex = 0;
        private int selectedHairIndex = 0;
        private int selectedEyesIndex = 0;
        private int selectedClothesIndex = 0;
        private int[] selectedSwitchableIndices = new int[4]; // Per slot dropdown selection

        // Foldouts
        private bool showPermanentParts = true;
        private bool showPermanentAccessories = false;
        private bool showSwitchableAccessories = false;

        private  SerializedProperty customizationPresetProp;

        void OnEnable()
        {
            renderer = (ModularNPCRenderer)target;
            customizationPresetProp = serializedObject.FindProperty("customizationPreset");
            
            // Load all available parts
            RefreshPartLists();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Modular NPC Customization", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Customize NPC dengan mixing & matching sprite parts. Changes akan langsung terlihat di Scene view.", MessageType.Info);

            EditorGUILayout.Space();

            // Preset field
            EditorGUILayout.PropertyField(customizationPresetProp, new GUIContent("Customization Preset"));

            EditorGUILayout.Space();

            // Quick actions
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Setup Layers"))
            {
                // Create child layers with proper Undo support
                Undo.RecordObject(renderer.gameObject, "Setup NPC Layers");
                
                CreateLayerInEditor("SkinLayer", 0);
                CreateLayerInEditor("EyesLayer", 1);
                CreateLayerInEditor("ClothesLayer", 2);
                CreateLayerInEditor("HairLayer", 3);
                CreateLayerInEditor("PermanentAccessory1", 4);
                CreateLayerInEditor("PermanentAccessory2", 5);
                CreateLayerInEditor("SwitchableAccessory1", 6);
                CreateLayerInEditor("SwitchableAccessory2", 7);
                CreateLayerInEditor("SwitchableAccessory3", 8);
                CreateLayerInEditor("SwitchableAccessory4", 9);
                
                // Call SetupLayers to assign references
                renderer.SetupLayers();
                EditorUtility.SetDirty(renderer);
                EditorUtility.DisplayDialog("Success", "Layers created! Check Hierarchy.", "OK");
            }
            if (GUILayout.Button("Apply Preset"))
            {
                if (renderer.customizationPreset != null)
                {
                    renderer.ApplyCustomization(renderer.customizationPreset);
                    EditorUtility.SetDirty(renderer);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "No preset assigned!", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Render offset preview button
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Render Offsets (Preview)", GUILayout.Height(30)))
            {
                ApplyRenderOffsetsInEditMode();
                EditorUtility.DisplayDialog("Success", "Render offsets applied! Check Scene view.", "OK");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Permanent Parts Section
            showPermanentParts = EditorGUILayout.Foldout(showPermanentParts, "Permanent Parts", true);
            if (showPermanentParts)
            {
                EditorGUI.indentLevel++;
                DrawPermanentPartsUI();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Permanent Accessories Section
            showPermanentAccessories = EditorGUILayout.Foldout(showPermanentAccessories, "Permanent Accessories (Beard, Ears, etc.)", true);
            if (showPermanentAccessories)
            {
                EditorGUI.indentLevel++;
                DrawPermanentAccessoriesUI();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Switchable Accessories Section
            showSwitchableAccessories = EditorGUILayout.Foldout(showSwitchableAccessories, "Switchable Accessories (Time-based)", true);
            if (showSwitchableAccessories)
            {
                EditorGUI.indentLevel++;
                DrawSwitchableAccessoriesUI();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Part Lists"))
            {
                RefreshPartLists();
            }
            if (GUILayout.Button("Clear All"))
            {
                if (EditorUtility.DisplayDialog("Clear All", "Clear all parts?", "Yes", "No"))
                {
                    renderer.ClearAllLayers();
                    EditorUtility.SetDirty(renderer);
                }
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draw UI untuk permanent parts
        /// </summary>
        private void DrawPermanentPartsUI()
        {
            // Skin
            DrawPartSelector("Skin", allSkins, ref selectedSkinIndex, PartCategory.Skin);

            // Hair
            DrawPartSelector("Hair", allHair, ref selectedHairIndex, PartCategory.Hair);

            // Eyes
            DrawPartSelector("Eyes", allEyes, ref selectedEyesIndex, PartCategory.Eyes);

            // Clothes
            DrawPartSelector("Clothes", allClothes, ref selectedClothesIndex, PartCategory.Clothes);
        }

        /// <summary>
        /// Draw part selector dropdown
        /// </summary>
        private void DrawPartSelector(string label, List<NPCPartData> parts, ref int selectedIndex, PartCategory category)
        {
            if (parts == null || parts.Count == 0)
            {
                EditorGUILayout.HelpBox($"No {label} parts found. Use Part Importer to generate assets.", MessageType.Warning);
                return;
            }

            string[] partNames = parts.Select(p => p != null ? p.partName : "None").ToArray();
            
            EditorGUILayout.BeginHorizontal();
            int newIndex = EditorGUILayout.Popup(label, selectedIndex, partNames);
            
            if (newIndex != selectedIndex || GUILayout.Button("Apply", GUILayout.Width(60)))
            {
                selectedIndex = newIndex;
                if (selectedIndex >= 0 && selectedIndex < parts.Count)
                {
                    renderer.SetPart(category, parts[selectedIndex]);
                    EditorUtility.SetDirty(renderer);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw UI untuk permanent accessories
        /// </summary>
        private void DrawPermanentAccessoriesUI()
        {
            EditorGUILayout.HelpBox("Accessories yang menyatu dengan body (max 2). Misal: Beard, Elf Ears, dll.", MessageType.Info);

            var permanentAccessories = allAccessories.Where(a => a != null && a.accessoryType == AccessoryType.Permanent).ToList();

            if (permanentAccessories.Count == 0)
            {
                EditorGUILayout.HelpBox("No permanent accessories found.", MessageType.Info);
                return;
            }

            // Show currently added accessories with Remove buttons
            EditorGUILayout.LabelField("Currently Added:", EditorStyles.boldLabel);
            
            if (renderer.currentPermanentAccessories != null && renderer.currentPermanentAccessories.Count > 0)
            {
                for (int i = 0; i < renderer.currentPermanentAccessories.Count; i++)
                {
                    var currentAcc = renderer.currentPermanentAccessories[i];
                    if (currentAcc == null) continue;
                    
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"{i + 1}. {currentAcc.partName}");
                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    {
                        renderer.RemovePermanentAccessory(i);
                        EditorUtility.SetDirty(renderer);
                        break; // Exit loop to avoid index issues
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.Space();
                
                if (GUILayout.Button("Clear All"))
                {
                    renderer.ClearPermanentAccessories();
                    EditorUtility.SetDirty(renderer);
                }
            }
            else
            {
                EditorGUILayout.LabelField("(None)", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Available Accessories:", EditorStyles.boldLabel);
            
            foreach (var acc in permanentAccessories)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(acc.partName);
                if (GUILayout.Button("Add", GUILayout.Width(60)))
                {
                    renderer.AddPermanentAccessory(acc);
                    EditorUtility.SetDirty(renderer);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// Draw UI untuk switchable accessories
        /// </summary>
        private void DrawSwitchableAccessoriesUI()
        {
            EditorGUILayout.HelpBox("Accessories yang bisa switch based on time/day. Use NPCAccessorySwitcher component untuk auto-switching.", MessageType.Info);

            var switchableAccessories = allAccessories.Where(a => a != null && a.accessoryType == AccessoryType.Switchable).ToList();

            if (switchableAccessories.Count == 0)
            {
                EditorGUILayout.HelpBox("No switchable accessories found.", MessageType.Info);
                return;
            }

            // Show slots
            for (int slotIndex = 0; slotIndex < 4; slotIndex++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Slot {slotIndex + 1}", EditorStyles.boldLabel);

                string[] accNames = switchableAccessories.Select(a => a.partName).ToArray();
                
                // Use persisted index for this slot
                selectedSwitchableIndices[slotIndex] = EditorGUILayout.Popup("Select Accessory", selectedSwitchableIndices[slotIndex], accNames);

                if (GUILayout.Button($"Apply to Slot {slotIndex + 1}"))
                {
                    int selectedIdx = selectedSwitchableIndices[slotIndex];
                    if (selectedIdx >= 0 && selectedIdx < switchableAccessories.Count)
                    {
                        renderer.SetSwitchableAccessory(slotIndex, switchableAccessories[selectedIdx]);
                        EditorUtility.SetDirty(renderer);
                    }
                }

                if (GUILayout.Button($"Clear Slot {slotIndex + 1}"))
                {
                    renderer.SetSwitchableAccessory(slotIndex, null);
                    EditorUtility.SetDirty(renderer);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        /// <summary>
        /// Refresh part lists dari assets
        /// </summary>
        private void RefreshPartLists()
        {
            string[] guids = AssetDatabase.FindAssets("t:NPCPartData");
            
            allSkins = new List<NPCPartData>();
            allHair = new List<NPCPartData>();
            allEyes = new List<NPCPartData>();
            allClothes = new List<NPCPartData>();
            allAccessories = new List<NPCPartData>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                NPCPartData part = AssetDatabase.LoadAssetAtPath<NPCPartData>(path);

                if (part != null)
                {
                    switch (part.category)
                    {
                        case PartCategory.Skin:
                            allSkins.Add(part);
                            break;
                        case PartCategory.Hair:
                            allHair.Add(part);
                            break;
                        case PartCategory.Eyes:
                            allEyes.Add(part);
                            break;
                        case PartCategory.Clothes:
                            allClothes.Add(part);
                            break;
                        case PartCategory.Accessory:
                            allAccessories.Add(part);
                            break;
                    }
                }
            }

            // Sort alphabetically
            allSkins.Sort((a, b) => a.partName.CompareTo(b.partName));
            allHair.Sort((a, b) => a.partName.CompareTo(b.partName));
            allEyes.Sort((a, b) => a.partName.CompareTo(b.partName));
            allClothes.Sort((a, b) => a.partName.CompareTo(b.partName));
            allAccessories.Sort((a, b) => a.partName.CompareTo(b.partName));
        }

        /// <summary>
        /// Create child layer GameObject dengan SpriteRenderer di Edit Mode
        /// </summary>
        private void CreateLayerInEditor(string layerName, int sortingOrder)
        {
            // Check jika sudah ada
            Transform existing = renderer.transform.Find(layerName);
            if (existing != null)
            {
                // Sudah ada, pastikan punya SpriteRenderer
                SpriteRenderer sr = existing.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    sr = existing.gameObject.AddComponent<SpriteRenderer>();
                }
                sr.sortingOrder = sortingOrder;
                return;
            }

            // Buat baru dengan Undo support
            GameObject layerObj = new GameObject(layerName);
            Undo.RegisterCreatedObjectUndo(layerObj, "Create " + layerName);
            
            layerObj.transform.SetParent(renderer.transform);
            layerObj.transform.localPosition = Vector3.zero;
            layerObj.transform.localRotation = Quaternion.identity;
            layerObj.transform.localScale = Vector3.one;

            SpriteRenderer spriteRenderer = layerObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = sortingOrder;
        }
        
        /// <summary>
        /// Apply render offsets from NPCPartData to child layer positions (Edit Mode preview)
        /// </summary>
        private void ApplyRenderOffsetsInEditMode()
        {
            // Apply offset for each part
            ApplyOffsetToLayer(renderer.skinLayer, renderer.currentSkin);
            ApplyOffsetToLayer(renderer.eyesLayer, renderer.currentEyes);
            ApplyOffsetToLayer(renderer.hairLayer, renderer.currentHair);
            ApplyOffsetToLayer(renderer.clothesLayer, renderer.currentClothes);
            
            // Apply offsets for permanent accessories
            if (renderer.currentPermanentAccessories != null && renderer.currentPermanentAccessories.Count > 0)
            {
                ApplyOffsetToLayer(renderer.permanentAccessory1Layer, renderer.currentPermanentAccessories[0]);
            }
            if (renderer.currentPermanentAccessories != null && renderer.currentPermanentAccessories.Count > 1)
            {
                ApplyOffsetToLayer(renderer.permanentAccessory2Layer, renderer.currentPermanentAccessories[1]);
            }
            
            // Apply offsets for switchable accessories
            ApplyOffsetToLayer(renderer.switchableAccessory1Layer, renderer.currentSwitchableAccessories[0]);
            ApplyOffsetToLayer(renderer.switchableAccessory2Layer, renderer.currentSwitchableAccessories[1]);
            ApplyOffsetToLayer(renderer.switchableAccessory3Layer, renderer.currentSwitchableAccessories[2]);
            ApplyOffsetToLayer(renderer.switchableAccessory4Layer, renderer.currentSwitchableAccessories[3]);
            
            EditorUtility.SetDirty(renderer.gameObject);
        }
        
        /// <summary>
        /// Apply offset from part data to layer
        /// </summary>
        private void ApplyOffsetToLayer(SpriteRenderer layer, NPCPartData partData)
        {
            if (layer == null || partData == null) return;
            
            Undo.RecordObject(layer.transform, "Apply Render Offset");
            // Edit Mode uses Down offset
            layer.transform.localPosition = partData.offsetDown;
        }
    }
}
