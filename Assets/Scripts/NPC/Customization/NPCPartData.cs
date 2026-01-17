using UnityEngine;
using System;

namespace NPCCustomization
{
    /// <summary>
    /// Serializable class untuk menyimpan sprites untuk satu animation state
    /// </summary>
    [Serializable]
    public class AnimationStateSprites
    {
        [Tooltip("Nama animation state (Idle, Walk, Run, Attack, dll)")]
        public string stateName;
        
        [Tooltip("Array of sprite frames untuk animation ini")]
        public Sprite[] frames;

        public AnimationStateSprites(string name)
        {
            stateName = name;
            frames = new Sprite[0];
        }
    }

    /// <summary>
    /// Base ScriptableObject untuk semua NPC parts.
    /// Menyimpan sprites untuk SEMUA animation states yang ada.
    /// </summary>
    [CreateAssetMenu(fileName = "NPCPart_", menuName = "NPC Customization/NPC Part Data")]
    public class NPCPartData : ScriptableObject
    {
        [Header("Part Information")]
        [Tooltip("Nama part ini (misal: 'Fawn Black Hair', 'Wizard Hat', dll)")]
        public string partName;
        
        [Tooltip("Category part ini")]
        public PartCategory category;
        
        [Header("Accessory Settings")]
        [Tooltip("Jika ini accessory, tentukan type-nya")]
        public AccessoryType accessoryType;
        
        [Header("Directional Render Offsets")]
        [Tooltip("Position offset saat character facing DOWN")]
        public Vector3 offsetDown = Vector3.zero;
        
        [Tooltip("Position offset saat character facing UP")]
        public Vector3 offsetUp = Vector3.zero;
        
        [Tooltip("Position offset saat character facing LEFT")]
        public Vector3 offsetLeft = Vector3.zero;
        
        [Tooltip("Position offset saat character facing RIGHT")]
        public Vector3 offsetRight = Vector3.zero;
        
        [Header("Per-Frame Offsets (Advanced)")]
        [Tooltip("Enable per-frame offset untuk fine-tuning setiap frame")]
        public bool usePerFrameOffsets = false;
        
        [Tooltip("Offset untuk setiap frame (index 0-15 untuk 4-in-1 sprites: Down 0-3, Up 4-7, Right 8-11, Left 12-15)")]
        public Vector3[] frameOffsets = new Vector3[16];
        
        [Header("Custom Direction Frame Offsets")]
        [Tooltip("Enable jika sprite sheet ini memiliki layout arah yang berbeda dari body (contoh: eyes tanpa frame Up)")]
        public bool useCustomDirectionOffsets = false;
        
        [Tooltip("Custom frame offset untuk arah Down (default: 0)")]
        public int customFrameOffsetDown = 0;
        
        [Tooltip("Custom frame offset untuk arah Up (default: 4). Set -1 jika tidak ada frame Up")]
        public int customFrameOffsetUp = 4;
        
        [Tooltip("Custom frame offset untuk arah Right (default: 8)")]
        public int customFrameOffsetRight = 8;
        
        [Tooltip("Custom frame offset untuk arah Left (default: 12)")]
        public int customFrameOffsetLeft = 12;
        
        [Tooltip("Jumlah frames per direction untuk part ini (default: 4)")]
        public int framesPerDirectionForPart = 4;
        
        /// <summary>
        /// Get render offset for specific direction
        /// </summary>
        public Vector3 GetOffsetForDirection(string direction)
        {
            switch (direction)
            {
                case "Down":
                    return offsetDown;
                case "Up":
                    return offsetUp;
                case "Left":
                    return offsetLeft;
                case "Right":
                    return offsetRight;
                default:
                    return Vector3.zero;
            }
        }
        
        /// <summary>
        /// Get offset for specific frame index (for per-frame alignment)
        /// Falls back to directional offset if per-frame is disabled
        /// </summary>
        public Vector3 GetOffsetForFrame(int frameIndex, string direction)
        {
            // If per-frame offsets enabled and array has valid data
            if (usePerFrameOffsets && frameOffsets != null && frameIndex < frameOffsets.Length)
            {
                return frameOffsets[frameIndex];
            }
            
            // Fallback to directional offset
            return GetOffsetForDirection(direction);
        }
        
        /// <summary>
        /// Remap frame index dari body ke part-specific index.
        /// Digunakan untuk part yang memiliki layout sprite sheet berbeda (contoh: eyes tanpa frame Up).
        /// Input: body frame index (0-15 untuk 4-in-1 dengan Down=0-3, Up=4-7, Right=8-11, Left=12-15)
        /// Output: part-specific frame index berdasarkan custom direction offsets
        /// Returns -1 jika direction tidak ada di part ini (contoh: Up untuk eyes)
        /// </summary>
        public int RemapFrameForPart(int bodyFrameIndex, string direction, int bodyFramesPerDirection = 4)
        {
            // Jika tidak menggunakan custom offsets, return frame index apa adanya
            if (!useCustomDirectionOffsets)
            {
                return bodyFrameIndex;
            }
            
            // Hitung frame dalam direction (0, 1, 2, 3)
            int frameWithinDirection = bodyFrameIndex % bodyFramesPerDirection;
            
            // Get custom offset untuk direction ini
            int customOffset = GetCustomOffsetForDirection(direction);
            
            // Return -1 jika direction tidak ada (contoh: Up untuk eyes)
            if (customOffset < 0)
            {
                return -1;
            }
            
            // Calculate final frame index untuk part ini
            return customOffset + frameWithinDirection;
        }
        
        /// <summary>
        /// Get custom frame offset untuk direction tertentu
        /// Returns -1 jika direction tidak ada di part ini
        /// </summary>
        public int GetCustomOffsetForDirection(string direction)
        {
            switch (direction)
            {
                case "Down":
                    return customFrameOffsetDown;
                case "Up":
                    return customFrameOffsetUp;
                case "Right":
                    return customFrameOffsetRight;
                case "Left":
                    return customFrameOffsetLeft;
                default:
                    return 0;
            }
        }
        
        [Header("Animation Sprites")]
        [Tooltip("Sprite arrays untuk setiap animation state")]
        public AnimationStateSprites[] animationStates;
        
        [Header("Metadata")]
        [TextArea(2, 4)]
        [Tooltip("Deskripsi part ini")]
        public string description;
        
        [Tooltip("Tags untuk filtering/searching")]
        public string[] tags;

        /// <summary>
        /// Get sprites untuk animation state tertentu
        /// </summary>
        public Sprite[] GetSpritesForState(string stateName)
        {
            foreach (var state in animationStates)
            {
                if (state.stateName == stateName)
                {
                    return state.frames;
                }
            }
            
            // Return empty array jika tidak ditemukan
            return new Sprite[0];
        }

        /// <summary>
        /// Get specific sprite frame untuk animation state tertentu
        /// </summary>
        public Sprite GetSpriteFrame(string stateName, int frameIndex)
        {
            Sprite[] frames = GetSpritesForState(stateName);
            
            if (frames.Length == 0 || frameIndex < 0 || frameIndex >= frames.Length)
            {
                return null;
            }
            
            return frames[frameIndex];
        }

        /// <summary>
        /// Check apakah part ini punya sprites untuk animation state tertentu
        /// </summary>
        public bool HasAnimationState(string stateName)
        {
            foreach (var state in animationStates)
            {
                if (state.stateName == stateName)
                {
                    return state.frames != null && state.frames.Length > 0;
                }
            }
            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Helper untuk add animation state baru (Editor only)
        /// </summary>
        public void AddAnimationState(string stateName, Sprite[] frames)
        {
            // Check jika state sudah ada
            for (int i = 0; i < animationStates.Length; i++)
            {
                if (animationStates[i].stateName == stateName)
                {
                    animationStates[i].frames = frames;
                    UnityEditor.EditorUtility.SetDirty(this);
                    return;
                }
            }
            
            // State belum ada, tambahkan baru
            var newState = new AnimationStateSprites(stateName);
            newState.frames = frames;
            
            var statesList = new System.Collections.Generic.List<AnimationStateSprites>(animationStates);
            statesList.Add(newState);
            animationStates = statesList.ToArray();
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
