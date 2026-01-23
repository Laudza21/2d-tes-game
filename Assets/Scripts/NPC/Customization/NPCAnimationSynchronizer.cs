using UnityEngine;

namespace NPCCustomization
{
    /// <summary>
    /// Component untuk sync animasi across all sprite layers.
    /// SUPPORTS 2 MODES:
    /// 1. Animation Events (RECOMMENDED) - Manual events di animation clips untuk perfect sync
    /// 2. Auto-detection (FALLBACK) - Auto detect state/frame jika tidak ada events
    /// </summary>
    [RequireComponent(typeof(ModularNPCRenderer))]
    [RequireComponent(typeof(Animator))]
    public class NPCAnimationSynchronizer : MonoBehaviour
    {
        [Header("References")]
        private ModularNPCRenderer npcRenderer;
        private Animator animator;

        [Header("Sync Mode")]
        [Tooltip("RECOMMENDED: Use Animation Events untuk perfect sync. Set TRUE jika Anda sudah add Animation Events di clips.")]
        public bool useAnimationEvents = true;

        [Tooltip("Fallback ke auto-detection jika Animation Events tidak tersedia")]
        public bool enableAutoDetectionFallback = true;

        [Header("Animation State Tracking")]
        [SerializeField] private string currentStateName = "";
        [SerializeField] private int currentFrame = -1;
        
        [Header("Auto-Detection Settings")]
        [Tooltip("Jumlah frames per animation (untuk auto-detection mode)")]
        public int framesPerAnimation = 4;

        [Tooltip("Update mode untuk auto-detection")]
        public bool useFixedUpdate = false;
        
        [Header("Blend Tree Support")]
        [Tooltip("Enable Blend Tree detection (for 4-directional movement)")]
        public bool useBlendTree = false;
        
        [Tooltip("Parameter name for horizontal movement (default: Horizontal)")]
        public string horizontalParameter = "Horizontal";
        
        [Tooltip("Parameter name for vertical movement (default: Vertical)")]
        public string verticalParameter = "Vertical";
        
        [Tooltip("Base state name prefix (e.g., 'Idle', 'Walk')")]
        public string baseStateName = "Idle";
        
        [Tooltip("Use 4-in-1 sprite sheets (all directions in 1 state)")]
        public bool use4in1SpriteSheets = true;
        
        [Tooltip("Frames per direction in sprite sheet (e.g., 4 frames for Down, 4 for Up...)")]
        public int framesPerDirection = 4;

        [Header("Debug")]
        [Tooltip("Log setiap frame update untuk debugging")]
        public bool debugMode = false;

        private AnimatorStateInfo previousStateInfo;
        private int previousFrame = -1;
        private float lastEventTime = -1f;
        
        [Header("Current Direction")]
        [Tooltip("Current direction for directional offsets (Down/Up/Left/Right)")]
        public string currentDirection = "Down";
        
        [Header("Multi-Layer Support")]
        [Tooltip("Index of currently active override layer (0 = Base Layer)")]
        [SerializeField] private int activeLayerIndex = 0;
        
        [Tooltip("Layer names untuk reference")]
        public string[] layerNames = new string[] 
        {
            "Base Layer",
            "Tools & Farming",
            "Combat",
            "Fishing",
            "Carrying",
            "Vehicles",
            "Special"
        };

        void Awake()
        {
            npcRenderer = GetComponent<ModularNPCRenderer>();
            animator = GetComponent<Animator>();
        }

        void Start()
        {
            if (useAnimationEvents)
            {
                if (debugMode)
                {
                    Debug.Log($"[{gameObject.name}] NPCAnimationSynchronizer: Animation Events mode enabled. Add Animation Events to your clips!");
                }
            }
            else
            {
                if (debugMode)
                {
                    Debug.Log($"[{gameObject.name}] NPCAnimationSynchronizer: Auto-detection mode enabled.");
                }
            }
        }

        void LateUpdate()
        {
            // Auto-detection mode (fallback)
            // Menggunakan LateUpdate agar berjalan SETELAH Animator mengupdate state
            // Ini mencegah delay 1 frame antara body dan parts lain
            if (!useAnimationEvents || (enableAutoDetectionFallback && ShouldUseFallback()))
            {
                if (!useFixedUpdate)
                {
                    SynchronizeAnimationAutoDetect();
                }
            }
        }

        void FixedUpdate()
        {
            // Auto-detection mode (fallback) di FixedUpdate
            // Hanya jika user explicitly check "Use Fixed Update"
            if (!useAnimationEvents || (enableAutoDetectionFallback && ShouldUseFallback()))
            {
                if (useFixedUpdate)
                {
                    SynchronizeAnimationAutoDetect();
                }
            }
        }

        /// <summary>
        /// PUBLIC METHOD - Dipanggil dari Animation Events di Animation Clips
        /// Add Animation Event di setiap frame dengan function name: "OnAnimationFrame"
        /// Parameter: frameIndex (0, 1, 2, 3, ...)
        /// </summary>
        public void OnAnimationFrame(int frameIndex)
        {
            if (npcRenderer == null) return;

            // Get current animation state name from active layer
            AnimatorStateInfo stateInfo = GetActiveLayerStateInfo();
            string stateName = GetStateName(stateInfo);

            // Update frame tracking
            currentStateName = stateName;
            currentFrame = frameIndex;
            lastEventTime = Time.time;

            // Update all sprite layers dengan sprite untuk frame ini
            npcRenderer.UpdateSpritesForAnimation(stateName, frameIndex);

            if (debugMode)
            {
                Debug.Log($"[AnimEvent] State: {stateName}, Frame: {frameIndex}");
            }
        }

        /// <summary>
        /// Overload untuk Animation Events tanpa parameter
        /// Akan auto-detect frame dari normalized time
        /// </summary>
        public void OnAnimationFrame()
        {
            AnimatorStateInfo stateInfo = GetActiveLayerStateInfo();
            int frame = CalculateCurrentFrame(stateInfo, framesPerAnimation);
            OnAnimationFrame(frame);
        }

        /// <summary>
        /// Check apakah harus fallback ke auto-detection
        /// </summary>
        private bool ShouldUseFallback()
        {
            // Jika belum pernah receive event dalam 1 detik terakhir, assume tidak ada events
            return Time.time - lastEventTime > 1f;
        }

        /// <summary>
        /// Auto-detection synchronization (fallback mode)
        /// </summary>
        private void SynchronizeAnimationAutoDetect()
        {
            if (animator == null || npcRenderer == null) return;

            // Get current animator state from active layer
            AnimatorStateInfo stateInfo = GetActiveLayerStateInfo();
            
            // Detect state name
            string stateName = GetStateName(stateInfo);
            
            // Calculate current frame berdasarkan normalized time
            int frameCount = GetFrameCount(stateName);
            int baseFrame = CalculateCurrentFrame(stateInfo, frameCount);
            
            // Apply direction offset for 4-in-1 sprite sheets
            int finalFrame = baseFrame;
            if (useBlendTree && use4in1SpriteSheets)
            {
                int directionOffset = GetDirectionOffset(currentDirection);
                finalFrame = baseFrame + directionOffset;
                
                if (debugMode)
                {
                    Debug.Log($"[FrameCalc] Base:{baseFrame} + Offset:{directionOffset} (Dir:{currentDirection}) = Final:{finalFrame}");
                }
            }

            // Update jika state atau frame berubah
            if (stateName != currentStateName || finalFrame != currentFrame)
            {
                currentStateName = stateName;
                currentFrame = finalFrame;
                
                // Update all sprite layers
                npcRenderer.UpdateSpritesForAnimation(stateName, finalFrame);

                if (debugMode)
                {
                    Debug.Log($"[AutoDetect] State: {stateName}, Frame: {finalFrame}");
                }
            }

            previousStateInfo = stateInfo;
            previousFrame = finalFrame;
        }
        
        /// <summary>
        /// Get sprite index offset based on direction (for 4-in-1 sprite sheets)
        /// </summary>
        private int GetDirectionOffset(string direction)
        {
            // Sprite sheet layout: Down (0-3), Up (4-7), Right (8-11), Left (12-15)
            // Note: Some sprite sheets have Left/Right swapped
            switch (direction)
            {
                case "Down":
                    return 0;
                case "Up":
                    return framesPerDirection;
                case "Right":  // Was Left
                    return framesPerDirection * 2;
                case "Left":   // Was Right
                    return framesPerDirection * 3;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Get animation state name dari AnimatorStateInfo
        /// </summary>
        private string GetStateName(AnimatorStateInfo stateInfo)
        {
            // BLEND TREE MODE
            if (useBlendTree && animator != null)
            {
                // Get direction from Blend Tree parameters
                float horizontal = animator.GetFloat(horizontalParameter);
                float vertical = animator.GetFloat(verticalParameter);
                
                // Determine direction based on parameters
                string direction = GetDirectionFromParameters(horizontal, vertical);
                currentDirection = direction;
                
                // Auto-detect Blend Tree name from current state
                string blendTreeName = GetBlendTreeName(stateInfo);
                
                // 4-IN-1 SPRITE SHEETS MODE
                if (use4in1SpriteSheets)
                {
                    // Map Blend Tree name to NPCPartData state name
                    string stateName = MapBlendTreeToStateName(blendTreeName);
                    
                    if (debugMode)
                    {
                        Debug.Log($"[4in1] BlendTree:{blendTreeName} H:{horizontal:F2} V:{vertical:F2} → Dir:{direction} → State:{stateName}");
                    }
                    
                    return stateName;
                }
                else
                {
                    // SEPARATE STATES MODE: Generate directional state name
                    string stateName = $"{blendTreeName} {direction}";
                    
                    if (debugMode)
                    {
                        Debug.Log($"[BlendTree] {blendTreeName} H:{horizontal:F2} V:{vertical:F2} → Direction:{direction} → State:{stateName}");
                    }
                    
                    return stateName;
                }
            }
            
            // STANDARD MODE (non-Blend Tree)
            // Common animation states untuk Farm RPG
            if (stateInfo.IsName("Idle") || stateInfo.IsName("1. Idle"))
                return "1. Idle";
            
            if (stateInfo.IsName("Walk") || stateInfo.IsName("2. Walk"))
                return "2. Walk";
            
            if (stateInfo.IsName("Run") || stateInfo.IsName("3. Run"))
                return "3. Run";
            
            if (stateInfo.IsName("Attack") || stateInfo.IsName("8. SwordAttack"))
                return "8. SwordAttack";
            
            if (stateInfo.IsName("Damage") || stateInfo.IsName("10. Damage"))
                return "10. Damage";
            
            if (stateInfo.IsName("Death") || stateInfo.IsName("11. Death"))
                return "11. Death";

            // Fishing states
            if (stateInfo.IsName("Fishing - Cast") || stateInfo.IsName("12. Fishing - Cast"))
                return "12. Fishing - Cast";
            
            if (stateInfo.IsName("Fishing - Wait") || stateInfo.IsName("12.1. Fishing - Wait"))
                return "12.1. Fishing - Wait";

            // Default: return Idle
            return "1. Idle";
        }
        
        /// <summary>
        /// Get Blend Tree name from AnimatorStateInfo
        /// </summary>
        private string GetBlendTreeName(AnimatorStateInfo stateInfo)
        {
            // Get tag from state (if you set tags in Animator)
            // Or extract from fullPathHash
            // For now, use simple name mapping
            
            // Common Blend Tree names
            if (stateInfo.IsName("Idle")) return "Idle";
            if (stateInfo.IsName("Walk")) return "Walk";
            if (stateInfo.IsName("Run")) return "Run";
            if (stateInfo.IsName("Attack")) return "Attack";
            if (stateInfo.IsName("Damage")) return "Damage";
            if (stateInfo.IsName("Death")) return "Death";
            
            // If baseStateName is set, use that as fallback
            if (!string.IsNullOrEmpty(baseStateName))
                return baseStateName;
            
            // Default
            return "Idle";
        }
        
        /// <summary>
        /// Map Blend Tree name to NPCPartData state name (with number prefix)
        /// </summary>
        private string MapBlendTreeToStateName(string blendTreeName)
        {
            // Standard mapping: BlendTree name → NPCPartData state name
            switch (blendTreeName)
            {
                case "Idle":
                    return "1. Idle";
                case "Walk":
                    return "2. Walk";
                case "Run":
                    return "3. Run";
                case "Jump":
                    return "4. Jump";
                case "Climb":
                    return "5. Climb";
                case "AxeAttack":
                    return "6. AxeAttack";
                case "PickaxeAttack":
                    return "7. PickaxeAttack";
                case "SwordAttack":
                    return "8. SwordAttack";
                case "BowAttack":
                    return "9. BowAttack";
                case "Damage":
                    return "10. Damage";
                case "Death":
                    return "11. Death";
                case "Fishing - Cast":
                    return "12. Fishing - Cast";
                case "Fishing - Wait":
                    return "12.1. Fishing - Wait";
                default:
                    // If no match, try to format it
                    return $"1. {blendTreeName}";
            }
        }
        
        /// <summary>
        /// Determine direction dari Horizontal/Vertical parameters
        /// </summary>
        private string GetDirectionFromParameters(float horizontal, float vertical)
        {
            // Threshold untuk dead zone
            float threshold = 0.1f;
            
            // Prioritize strongest axis
            if (Mathf.Abs(vertical) > Mathf.Abs(horizontal))
            {
                // Vertical dominant
                if (vertical > threshold)
                    return "Up";
                else if (vertical < -threshold)
                    return "Down";
            }
            else
            {
                // Horizontal dominant
                if (horizontal > threshold)
                    return "Right";
                else if (horizontal < -threshold)
                    return "Left";
            }
            
            // Default ke last direction kalau dalam dead zone
            return currentDirection;
        }

        /// <summary>
        /// Calculate current frame dari normalized time
        /// </summary>
        private int CalculateCurrentFrame(AnimatorStateInfo stateInfo, int frameCount)
        {
            if (frameCount <= 0) frameCount = framesPerAnimation;
            
            // Normalized time: 0-1 untuk satu loop
            float normalizedTime = stateInfo.normalizedTime;
            
            // Get fractional part (untuk handle looping)
            normalizedTime = normalizedTime % 1f;
            
            // Calculate frame index
            int frame = Mathf.FloorToInt(normalizedTime * frameCount);
            
            // Clamp untuk safety
            frame = Mathf.Clamp(frame, 0, frameCount - 1);
            
            return frame;
        }

        /// <summary>
        /// Get frame count untuk animation state tertentu
        /// </summary>
        private int GetFrameCount(string stateName)
        {
            // Bisa customize per animation jika needed
            // Untuk sekarang, gunakan default
            return framesPerAnimation > 0 ? framesPerAnimation : 4;
        }
        
        /// <summary>
        /// Get AnimatorStateInfo dari layer yang sedang aktif.
        /// Scan dari layer tertinggi ke terendah, return state dari layer dengan weight > 0.5
        /// </summary>
        private AnimatorStateInfo GetActiveLayerStateInfo()
        {
            if (animator == null) return default;
            
            int layerCount = animator.layerCount;
            
            // Scan dari layer tertinggi ke terendah
            // Layer dengan weight > 0.5 dianggap aktif
            for (int i = layerCount - 1; i >= 0; i--)
            {
                float weight = animator.GetLayerWeight(i);
                if (i == 0 || weight > 0.5f)
                {
                    activeLayerIndex = i;
                    
                    if (debugMode && i > 0)
                    {
                        Debug.Log($"[MultiLayer] Active layer: {GetActiveLayerName()} (index {i}, weight {weight})");
                    }
                    
                    return animator.GetCurrentAnimatorStateInfo(i);
                }
            }
            
            activeLayerIndex = 0;
            return animator.GetCurrentAnimatorStateInfo(0);
        }
        
        /// <summary>
        /// Get index dari layer yang sedang aktif
        /// </summary>
        public int GetActiveLayerIndex()
        {
            return activeLayerIndex;
        }
        
        /// <summary>
        /// Get nama layer yang sedang aktif
        /// </summary>
        public string GetActiveLayerName()
        {
            if (activeLayerIndex < layerNames.Length)
                return layerNames[activeLayerIndex];
            return $"Layer {activeLayerIndex}";
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor helper: Force sync NOW
        /// </summary>
        [ContextMenu("Force Sync Now")]
        public void ForceSyncNow()
        {
            if (Application.isPlaying)
            {
                SynchronizeAnimationAutoDetect();
                Debug.Log($"Forced sync: {currentStateName} Frame {currentFrame}");
            }
            else
            {
                Debug.LogWarning("Can only force sync in Play mode!");
            }
        }

        /// <summary>
        /// Editor helper: Test Animation Event with frame 0
        /// </summary>
        [ContextMenu("Test Animation Event (Frame 0)")]
        public void TestAnimationEvent()
        {
            if (Application.isPlaying)
            {
                OnAnimationFrame(0);
                Debug.Log("Tested Animation Event with Frame 0");
            }
            else
            {
                Debug.LogWarning("Can only test in Play mode!");
            }
        }
#endif
    }
}
