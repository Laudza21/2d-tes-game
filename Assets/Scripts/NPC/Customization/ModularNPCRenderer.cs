using UnityEngine;
using System.Collections.Generic;

namespace NPCCustomization
{
    /// <summary>
    /// Component utama yang handle rendering dan layer management untuk modular NPC.
    /// Membuat child GameObjects untuk setiap layer dan sync sprites with animations.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class ModularNPCRenderer : MonoBehaviour
    {
        [Header("Customization")]
        [Tooltip("NPC Customization Preset yang menentukan appearance")]
        public NPCCustomizationPreset customizationPreset;

        [Header("Layer References")]
        public SpriteRenderer skinLayer;
        public SpriteRenderer eyesLayer;
        public SpriteRenderer clothesLayer;
        public SpriteRenderer hairLayer;
        public SpriteRenderer permanentAccessory1Layer;
        public SpriteRenderer permanentAccessory2Layer;
        public SpriteRenderer switchableAccessory1Layer;
        public SpriteRenderer switchableAccessory2Layer;
        public SpriteRenderer switchableAccessory3Layer;
        public SpriteRenderer switchableAccessory4Layer;

        [Header("Current Parts")]
        public NPCPartData currentSkin;
        public NPCPartData currentHair;
        public NPCPartData currentEyes;
        public NPCPartData currentClothes;
        public List<NPCPartData> currentPermanentAccessories = new List<NPCPartData>();
        public NPCPartData[] currentSwitchableAccessories = new NPCPartData[4];

        [Header("Settings")]
        [Tooltip("Sorting layer name untuk semua parts")]
        public string sortingLayerName = "Default";
        
        [Tooltip("Base sorting order (akan di-offset untuk setiap layer)")]
        public int baseSortingOrder = 0;
        
        // Direction tracking for offset updates
        private string previousDirection = "Down";
        
        [Header("Bobbing Settings")]
        [Tooltip("Enable code-based bobbing for parts without built-in sprite bobbing")]
        public bool enableCodeBobbing = true;
        
        [Tooltip("Bobbing Y offsets per frame (matches animation frames 0-3). Adjusts position to simulate body movement.")]
        public float[] bobbingOffsets = new float[] { 0f, -0.01f, -0.02f, -0.01f };
        
        // Current frame for bobbing calculation
        private int currentBobbingFrame = 0;

        private Animator animator;
        private Dictionary<PartCategory, SpriteRenderer> layerMap;

        void Awake()
        {
            animator = GetComponent<Animator>();
            InitializeLayerMap();
        }

        void Start()
        {
            // Apply customization dari preset jika ada
            if (customizationPreset != null)
            {
                ApplyCustomization(customizationPreset);
            }
        }

        /// <summary>
        /// Initialize layer references map
        /// </summary>
        private void InitializeLayerMap()
        {
            layerMap = new Dictionary<PartCategory, SpriteRenderer>
            {
                { PartCategory.Skin, skinLayer },
                { PartCategory.Eyes, eyesLayer },
                { PartCategory.Clothes, clothesLayer },
                { PartCategory.Hair, hairLayer }
            };
        }

        /// <summary>
        /// Apply full customization dari preset
        /// </summary>
        public void ApplyCustomization(NPCCustomizationPreset preset)
        {
            if (preset == null)
            {
                Debug.LogWarning("NPCCustomizationPreset is null!");
                return;
            }

            if (!preset.IsValid())
            {
                Debug.LogWarning($"NPCCustomizationPreset {preset.name} is not valid!");
                return;
            }

            customizationPreset = preset;

            // Setup layers jika belum ada
            if (skinLayer == null)
            {
                SetupLayers();
            }

            // Apply permanent parts
            SetPart(PartCategory.Skin, preset.skin);
            SetPart(PartCategory.Hair, preset.hair);
            SetPart(PartCategory.Eyes, preset.eyes);
            SetPart(PartCategory.Clothes, preset.clothes);

            // Apply permanent accessories
            ClearPermanentAccessories();
            foreach (var acc in preset.permanentAccessories)
            {
                if (acc != null)
                {
                    AddPermanentAccessory(acc);
                }
            }

            // Apply default switchable accessories
            for (int i = 0; i < 4; i++)
            {
                NPCPartData defaultAcc = preset.GetDefaultAccessory(i);
                SetSwitchableAccessory(i, defaultAcc);
            }

            Debug.Log($"Applied customization: {preset.name}");
        }

        /// <summary>
        /// Setup child GameObjects untuk setiap layer
        /// </summary>
        public void SetupLayers()
        {
            // Create atau get existing layers
            skinLayer = CreateOrGetLayer("SkinLayer", 0);
            eyesLayer = CreateOrGetLayer("EyesLayer", 1);
            clothesLayer = CreateOrGetLayer("ClothesLayer", 2);
            hairLayer = CreateOrGetLayer("HairLayer", 3);
            permanentAccessory1Layer = CreateOrGetLayer("PermanentAccessory1", 4);
            permanentAccessory2Layer = CreateOrGetLayer("PermanentAccessory2", 5);
            switchableAccessory1Layer = CreateOrGetLayer("SwitchableAccessory1", 6);
            switchableAccessory2Layer = CreateOrGetLayer("SwitchableAccessory2", 7);
            switchableAccessory3Layer = CreateOrGetLayer("SwitchableAccessory3", 8);
            switchableAccessory4Layer = CreateOrGetLayer("SwitchableAccessory4", 9);

            InitializeLayerMap();
        }

        /// <summary>
        /// Create atau get layer GameObject dengan SpriteRenderer
        /// </summary>
        private SpriteRenderer CreateOrGetLayer(string layerName, int sortingOrderOffset)
        {
            // Check jika child sudah ada
            Transform existingChild = transform.Find(layerName);
            
            if (existingChild != null)
            {
                SpriteRenderer sr = existingChild.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    sr = existingChild.gameObject.AddComponent<SpriteRenderer>();
                }
                SetupSpriteRenderer(sr, sortingOrderOffset);
                return sr;
            }

            // Buat baru
            GameObject layer = new GameObject(layerName);
            layer.transform.SetParent(transform);
            layer.transform.localPosition = Vector3.zero;
            layer.transform.localRotation = Quaternion.identity;
            layer.transform.localScale = Vector3.one;

            SpriteRenderer spriteRenderer = layer.AddComponent<SpriteRenderer>();
            SetupSpriteRenderer(spriteRenderer, sortingOrderOffset);

            return spriteRenderer;
        }

        /// <summary>
        /// Setup SpriteRenderer properties
        /// </summary>
        private void SetupSpriteRenderer(SpriteRenderer sr, int sortingOrderOffset)
        {
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = baseSortingOrder + sortingOrderOffset;
        }

        /// <summary>
        /// Set part untuk category tertentu
        /// </summary>
        public void SetPart(PartCategory category, NPCPartData partData)
        {
            switch (category)
            {
                case PartCategory.Skin:
                    currentSkin = partData;
                    if (skinLayer != null && partData != null)
                    {
                        // Assign first frame sprite untuk preview di Edit Mode
                        Sprite firstSprite = partData.GetSpriteFrame("1. Idle", 0);
                        if (firstSprite != null) skinLayer.sprite = firstSprite;
                        // Apply offset (Edit Mode default to Down)
                        skinLayer.transform.localPosition = partData.offsetDown;
                    }
                    break;
                case PartCategory.Hair:
                    currentHair = partData;
                    if (hairLayer != null && partData != null)
                    {
                        Sprite firstSprite = partData.GetSpriteFrame("1. Idle", 0);
                        if (firstSprite != null) hairLayer.sprite = firstSprite;
                        hairLayer.transform.localPosition = partData.offsetDown;
                    }
                    break;
                case PartCategory.Eyes:
                    currentEyes = partData;
                    if (eyesLayer != null && partData != null)
                    {
                        Sprite firstSprite = partData.GetSpriteFrame("1. Idle", 0);
                        if (firstSprite != null) eyesLayer.sprite = firstSprite;
                        eyesLayer.transform.localPosition = partData.offsetDown;
                    }
                    break;
                case PartCategory.Clothes:
                    currentClothes = partData;
                    if (clothesLayer != null && partData != null)
                    {
                        Sprite firstSprite = partData.GetSpriteFrame("1. Idle", 0);
                        if (firstSprite != null) clothesLayer.sprite = firstSprite;
                        clothesLayer.transform.localPosition = partData.offsetDown;
                    }
                    break;
            }
        }

        /// <summary>
        /// Add permanent accessory
        /// </summary>
        public void AddPermanentAccessory(NPCPartData accessory)
        {
            if (accessory == null) return;
            
            if (currentPermanentAccessories.Count >= 2)
            {
                Debug.LogWarning("Maximum 2 permanent accessories allowed!");
                return;
            }

            currentPermanentAccessories.Add(accessory);
            
            // Assign sprite untuk preview di Edit Mode
            int index = currentPermanentAccessories.Count - 1;
            SpriteRenderer layer = index == 0 ? permanentAccessory1Layer : permanentAccessory2Layer;
            if (layer != null)
            {
                Sprite firstSprite = accessory.GetSpriteFrame("1. Idle", 0);
                if (firstSprite != null) layer.sprite = firstSprite;
                // Apply offset (Edit Mode default to Down)
                layer.transform.localPosition = accessory.offsetDown;
            }
        }

        /// <summary>
        /// Clear all permanent accessories
        /// </summary>
        public void ClearPermanentAccessories()
        {
            currentPermanentAccessories.Clear();
            
            // Clear sprites dari layers
            if (permanentAccessory1Layer != null) permanentAccessory1Layer.sprite = null;
            if (permanentAccessory2Layer != null) permanentAccessory2Layer.sprite = null;
        }

        /// <summary>
        /// Remove specific permanent accessory by index
        /// </summary>
        public void RemovePermanentAccessory(int index)
        {
            if (index < 0 || index >= currentPermanentAccessories.Count)
            {
                Debug.LogWarning($"Invalid accessory index: {index}");
                return;
            }

            currentPermanentAccessories.RemoveAt(index);
            
            // Re-assign sprites to layers based on new list
            if (permanentAccessory1Layer != null)
            {
                if (currentPermanentAccessories.Count > 0)
                {
                    Sprite sprite = currentPermanentAccessories[0].GetSpriteFrame("1. Idle", 0);
                    permanentAccessory1Layer.sprite = sprite;
                }
                else
                {
                    permanentAccessory1Layer.sprite = null;
                }
            }
            
            if (permanentAccessory2Layer != null)
            {
                if (currentPermanentAccessories.Count > 1)
                {
                    Sprite sprite = currentPermanentAccessories[1].GetSpriteFrame("1. Idle", 0);
                    permanentAccessory2Layer.sprite = sprite;
                }
                else
                {
                    permanentAccessory2Layer.sprite = null;
                }
            }
        }

        /// <summary>
        /// Set switchable accessory untuk slot tertentu
        /// </summary>
        public void SetSwitchableAccessory(int slotIndex, NPCPartData accessory)
        {
            if (slotIndex < 0 || slotIndex >= 4)
            {
                Debug.LogWarning($"Invalid slot index: {slotIndex}. Must be 0-3.");
                return;
            }

            currentSwitchableAccessories[slotIndex] = accessory;
            
            // Assign sprite untuk preview di Edit Mode
            SpriteRenderer layer = null;
            switch (slotIndex)
            {
                case 0: layer = switchableAccessory1Layer; break;
                case 1: layer = switchableAccessory2Layer; break;
                case 2: layer = switchableAccessory3Layer; break;
                case 3: layer = switchableAccessory4Layer; break;
            }
            
            if (layer != null)
            {
                if (accessory != null)
                {
                    Sprite firstSprite = accessory.GetSpriteFrame("1. Idle", 0);
                    if (firstSprite != null) layer.sprite = firstSprite;
                    
                    // Apply render offset (Edit Mode default to Down)
                    layer.transform.localPosition = accessory.offsetDown;
                }
                else
                {
                    layer.sprite = null; // Clear sprite jika accessory null
                    layer.transform.localPosition = Vector3.zero; // Reset position
                }
            }
        }

        /// <summary>
        /// PUBLIC METHOD - Update all sprite layers untuk animation state tertentu
        /// Dipanggil dari NPCAnimationSynchronizer setiap kali animation frame berubah
        /// </summary>
        public void UpdateSpritesForAnimation(string stateName, int frameIndex)
        {
            // Get current direction
            string currentDirection = GetCurrentDirection();
            bool directionChanged = currentDirection != previousDirection;
            previousDirection = currentDirection;
            
            // Update sprite layers untuk basic parts
            UpdateLayerSprite(skinLayer, currentSkin, stateName, frameIndex, currentDirection, directionChanged, false);
            UpdateLayerSprite(hairLayer, currentHair, stateName, frameIndex, currentDirection, directionChanged, false);
            
            // Eyes: Special handling - skip entirely when facing Up (no sprites exist for Up)
            if (eyesLayer != null)
            {
                if (currentDirection == "Up")
                {
                    // Facing Up: hide eyes and don't update sprite at all
                    eyesLayer.enabled = false;
                    eyesLayer.sprite = null; // Clear sprite to avoid old frame showing
                }
                else
                {
                    // Other directions: show eyes and update sprite normally
                    // skipPositionUpdate = true, we apply bobbing manually below
                    eyesLayer.enabled = true;
                    UpdateLayerSprite(eyesLayer, currentEyes, stateName, frameIndex, currentDirection, directionChanged, true);
                }
            }
            
            UpdateLayerSprite(clothesLayer, currentClothes, stateName, frameIndex, currentDirection, directionChanged, false);
            
            // Apply code-based bobbing for eyes (same as hair)
            if (eyesLayer != null && currentEyes != null && currentDirection != "Up" && enableCodeBobbing)
            {
                Vector3 baseOffset = currentEyes.GetOffsetForDirection(currentDirection);
                int bobbingFrame = frameIndex % bobbingOffsets.Length;
                float bobbingY = bobbingOffsets[bobbingFrame];
                eyesLayer.transform.localPosition = new Vector3(baseOffset.x, baseOffset.y + bobbingY, baseOffset.z);
            }
            
            // Apply code-based bobbing for hair (since hair sprites don't have built-in bobbing)
            // Priority: 1) Per-frame offset from NPCPartData, 2) Code bobbing
            if (hairLayer != null && currentHair != null)
            {
                // If per-frame offsets are set in NPCPartData, use those instead of code bobbing
                if (currentHair.usePerFrameOffsets)
                {
                    hairLayer.transform.localPosition = currentHair.GetOffsetForFrame(frameIndex, currentDirection);
                }
                else if (enableCodeBobbing)
                {
                    // Fallback to code-based bobbing
                    Vector3 baseOffset = currentHair.GetOffsetForDirection(currentDirection);
                    int bobbingFrame = frameIndex % bobbingOffsets.Length;
                    float bobbingY = bobbingOffsets[bobbingFrame];
                    hairLayer.transform.localPosition = new Vector3(baseOffset.x, baseOffset.y + bobbingY, baseOffset.z);
                    currentBobbingFrame = bobbingFrame;
                }
            }

            // Update permanent accessories
            if (currentPermanentAccessories != null && currentPermanentAccessories.Count > 0)
            {
                UpdateLayerSprite(permanentAccessory1Layer, currentPermanentAccessories[0], stateName, frameIndex, currentDirection, directionChanged, false);
            }
            if (currentPermanentAccessories != null && currentPermanentAccessories.Count > 1)
            {
                UpdateLayerSprite(permanentAccessory2Layer, currentPermanentAccessories[1], stateName, frameIndex, currentDirection, directionChanged, false);
            }

            // Update switchable accessories
            for (int i = 0; i < currentSwitchableAccessories.Length; i++)
            {
                SpriteRenderer layer = null;
                switch (i)
                {
                    case 0: layer = switchableAccessory1Layer; break;
                    case 1: layer = switchableAccessory2Layer; break;
                    case 2: layer = switchableAccessory3Layer; break;
                    case 3: layer = switchableAccessory4Layer; break;
                }

                if (layer != null)
                {
                    UpdateLayerSprite(layer, currentSwitchableAccessories[i], stateName, frameIndex, currentDirection, directionChanged, false);
                }
            }
        }

        /// <summary>
        /// Update single layer sprite
        /// </summary>
        /// <param name="skipPositionUpdate">If true, skip position updates on sprite change (for eyes bobbing from sprite content). Still updates on direction change.</param>
        private void UpdateLayerSprite(SpriteRenderer layer, NPCPartData partData, string stateName, int frameIndex, string currentDirection, bool directionChanged, bool skipPositionUpdate = false)
        {
            if (layer == null) return;

            if (partData == null)
            {
                layer.sprite = null;
                return;
            }

            // Remap frame index untuk parts dengan custom direction offsets (contoh: eyes tanpa Up)
            int actualFrameIndex = partData.RemapFrameForPart(frameIndex, currentDirection, 4);
            
            // Jika remapped frame = -1, berarti direction tidak ada di part ini (contoh: Up untuk eyes)
            // Sembunyikan layer ini
            if (actualFrameIndex < 0)
            {
                layer.sprite = null;
                return;
            }

            Sprite newSprite = partData.GetSpriteFrame(stateName, actualFrameIndex);
            bool spriteChanged = layer.sprite != newSprite;
            
            // Update sprite if changed
            if (spriteChanged)
            {
                layer.sprite = newSprite;
            }
            
            // Update position:
            // - Normal layers: update on sprite OR direction change
            // - skipPositionUpdate layers (eyes): update ONLY on direction change (not on sprite change)
            bool shouldUpdatePosition = false;
            if (skipPositionUpdate)
            {
                // Eyes: only update when direction changes (to apply correct directional offset)
                shouldUpdatePosition = directionChanged;
            }
            else
            {
                // Normal: update when sprite or direction changes
                shouldUpdatePosition = spriteChanged || directionChanged;
            }
            
            if (shouldUpdatePosition)
            {
                // Use per-frame offset if available, otherwise fallback to directional offset
                layer.transform.localPosition = partData.GetOffsetForFrame(actualFrameIndex, currentDirection);
            }
        }
        
        /// <summary>
        /// Get current direction from NPCAnimationSynchronizer
        /// </summary>
        private string GetCurrentDirection()
        {
            NPCAnimationSynchronizer synchronizer = GetComponent<NPCAnimationSynchronizer>();
            if (synchronizer != null)
            {
                return synchronizer.currentDirection;
            }
            return "Down"; // Default
        }

        /// <summary>
        /// Get current animation state name dari Animator
        /// </summary>
        public string GetCurrentAnimationState()
        {
            if (animator == null) return "";

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName("Idle") ? "Idle" : stateInfo.ToString();
        }

        /// <summary>
        /// Get reference ke Animator
        /// </summary>
        public Animator GetAnimator()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            return animator;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor helper: Rebuild layers
        /// </summary>
        [ContextMenu("Rebuild Layers")]
        public void RebuildLayers()
        {
            SetupLayers();
            if (customizationPreset != null)
            {
                ApplyCustomization(customizationPreset);
            }
            Debug.Log("Layers rebuilt!");
        }

        /// <summary>
        /// Editor helper: Clear all layers
        /// </summary>
        [ContextMenu("Clear All Layers")]
        public void ClearAllLayers()
        {
            // Clear references
            currentSkin = null;
            currentHair = null;
            currentEyes = null;
            currentClothes = null;
            currentPermanentAccessories.Clear();
            currentSwitchableAccessories = new NPCPartData[4];

            // Clear sprites
            if (skinLayer != null) skinLayer.sprite = null;
            if (eyesLayer != null) eyesLayer.sprite = null;
            if (clothesLayer != null) clothesLayer.sprite = null;
            if (hairLayer != null) hairLayer.sprite = null;
            if (permanentAccessory1Layer != null) permanentAccessory1Layer.sprite = null;
            if (permanentAccessory2Layer != null) permanentAccessory2Layer.sprite = null;
            if (switchableAccessory1Layer != null) switchableAccessory1Layer.sprite = null;
            if (switchableAccessory2Layer != null) switchableAccessory2Layer.sprite = null;
            if (switchableAccessory3Layer != null) switchableAccessory3Layer.sprite = null;
            if (switchableAccessory4Layer != null) switchableAccessory4Layer.sprite = null;

            Debug.Log("All layers cleared!");
        }
#endif
    }
}
