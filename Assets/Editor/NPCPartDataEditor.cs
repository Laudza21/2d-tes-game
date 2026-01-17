using UnityEngine;
using UnityEditor;
using NPCCustomization;

namespace NPCCustomizationEditor
{
    [CustomEditor(typeof(NPCPartData))]
    public class NPCPartDataEditor : Editor
    {
        private Vector3 previousOffsetDown;
        private Vector3 previousOffsetUp;
        private Vector3 previousOffsetLeft;
        private Vector3 previousOffsetRight;
        private bool initialized = false;

        public override void OnInspectorGUI()
        {
            NPCPartData partData = (NPCPartData)target;

            // Store initial offset values
            if (!initialized)
            {
                previousOffsetDown = partData.offsetDown;
                previousOffsetUp = partData.offsetUp;
                previousOffsetLeft = partData.offsetLeft;
                previousOffsetRight = partData.offsetRight;
                initialized = true;
            }

            // Draw default inspector
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            
            // Check if any offset changed
            if (EditorGUI.EndChangeCheck())
            {
                if (partData.offsetDown != previousOffsetDown ||
                    partData.offsetUp != previousOffsetUp ||
                    partData.offsetLeft != previousOffsetLeft ||
                    partData.offsetRight != previousOffsetRight)
                {
                    // Offset changed! Apply to all NPCs in scene (use Down for Edit Mode)
                    ApplyOffsetToSceneNPCs(partData);
                    previousOffsetDown = partData.offsetDown;
                    previousOffsetUp = partData.offsetUp;
                    previousOffsetLeft = partData.offsetLeft;
                    previousOffsetRight = partData.offsetRight;
                }
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Directional Offset changes auto-apply to NPCs in Scene (Edit Mode uses Down offset)", MessageType.Info);
        }

        /// <summary>
        /// Apply offset to all NPCs in scene using this part
        /// </summary>
        private void ApplyOffsetToSceneNPCs(NPCPartData partData)
        {
            // Find all ModularNPCRenderer in scene
            ModularNPCRenderer[] renderers = FindObjectsByType<ModularNPCRenderer>(FindObjectsSortMode.None);
            
            int updateCount = 0;
            foreach (var renderer in renderers)
            {
                bool updated = false;
                
                // Check if this renderer uses this part
                if (renderer.currentSkin == partData && renderer.skinLayer != null)
                {
                    renderer.skinLayer.transform.localPosition = partData.offsetDown;
                    updated = true;
                }
                
                if (renderer.currentHair == partData && renderer.hairLayer != null)
                {
                    renderer.hairLayer.transform.localPosition = partData.offsetDown;
                    updated = true;
                }
                
                if (renderer.currentEyes == partData && renderer.eyesLayer != null)
                {
                    renderer.eyesLayer.transform.localPosition = partData.offsetDown;
                    updated = true;
                }
                
                if (renderer.currentClothes == partData && renderer.clothesLayer != null)
                {
                    renderer.clothesLayer.transform.localPosition = partData.offsetDown;
                    updated = true;
                }
                
                // Check permanent accessories
                if (renderer.currentPermanentAccessories != null)
                {
                    for (int i = 0; i < renderer.currentPermanentAccessories.Count; i++)
                    {
                        if (renderer.currentPermanentAccessories[i] == partData)
                        {
                            SpriteRenderer layer = i == 0 ? renderer.permanentAccessory1Layer : renderer.permanentAccessory2Layer;
                            if (layer != null)
                            {
                                layer.transform.localPosition = partData.offsetDown;
                                updated = true;
                            }
                        }
                    }
                }
                
                // Check switchable accessories
                for (int i = 0; i < renderer.currentSwitchableAccessories.Length; i++)
                {
                    if (renderer.currentSwitchableAccessories[i] == partData)
                    {
                        SpriteRenderer layer = null;
                        switch (i)
                        {
                            case 0: layer = renderer.switchableAccessory1Layer; break;
                            case 1: layer = renderer.switchableAccessory2Layer; break;
                            case 2: layer = renderer.switchableAccessory3Layer; break;
                            case 3: layer = renderer.switchableAccessory4Layer; break;
                        }
                        
                        if (layer != null)
                        {
                            layer.transform.localPosition = partData.offsetDown;
                            updated = true;
                        }
                    }
                }
                
                if (updated)
                {
                    EditorUtility.SetDirty(renderer.gameObject);
                    updateCount++;
                }
            }
            
            if (updateCount > 0)
            {
                Debug.Log($"[NPCPartData] Applied directional offsets (Down: {partData.offsetDown}) to {updateCount} NPC(s) in scene!");
                SceneView.RepaintAll();
            }
        }
    }
}
