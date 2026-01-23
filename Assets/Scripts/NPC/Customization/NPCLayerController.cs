using UnityEngine;

namespace NPCCustomization
{
    /// <summary>
    /// Controller untuk manage Animator layer weights dan action states.
    /// Bekerja bersama ModularNPCRenderer untuk switch tools/weapons.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(ModularNPCRenderer))]
    public class NPCLayerController : MonoBehaviour
    {
        [Header("References")]
        private Animator animator;
        private ModularNPCRenderer npcRenderer;
        
        [Header("Layer Indices")]
        public const int BASE_LAYER = 0;
        public const int TOOLS_LAYER = 1;
        public const int COMBAT_LAYER = 2;
        public const int FISHING_LAYER = 3;
        public const int CARRYING_LAYER = 4;
        public const int VEHICLES_LAYER = 5;
        public const int SPECIAL_LAYER = 6;
        
        [Header("Tool Part Data")]
        [Tooltip("Tool sprite untuk Pickaxe")]
        public NPCPartData pickaxeToolPart;
        
        [Tooltip("Tool sprite untuk Hoe")]
        public NPCPartData hoeToolPart;
        
        [Tooltip("Tool sprite untuk Bug Net")]
        public NPCPartData bugNetToolPart;
        
        [Tooltip("Tool sprite untuk Axe")]
        public NPCPartData axeToolPart;
        
        [Tooltip("Tool sprite untuk Sickle")]
        public NPCPartData sickleToolPart;
        
        [Tooltip("Tool sprite untuk Shovel")]
        public NPCPartData shovelToolPart;
        
        [Tooltip("Tool sprite untuk Watering Can")]
        public NPCPartData wateringCanToolPart;
        
        [Header("Action Body Skins")]
        [Tooltip("Body skin untuk DiggingCatching action (Pickaxe/Hoe/BugNet)")]
        public NPCPartData diggingCatchingBodySkin;
        
        [Tooltip("Body skin untuk Cutting action (Axe/Sickle)")]
        public NPCPartData cuttingBodySkin;
        
        [Tooltip("Body skin untuk Shovel action")]
        public NPCPartData shovelBodySkin;
        
        [Tooltip("Body skin untuk Watering action")]
        public NPCPartData wateringBodySkin;
        
        [Header("Combat Body Skins")]
        [Tooltip("Body skin untuk Sword Attack")]
        public NPCPartData swordAttackBodySkin;
        
        [Tooltip("Body skin untuk Archer")]
        public NPCPartData archerBodySkin;
        
        [Header("Current State")]
        [SerializeField] private ToolType currentToolType = ToolType.None;
        [SerializeField] private ActionState currentActionState = ActionState.None;
        [SerializeField] private VehicleType currentVehicleType = VehicleType.None;
        
        [Header("Debug")]
        public bool debugMode = false;
        
        public enum ToolType
        {
            None = 0,
            Pickaxe = 1,
            Hoe = 2,
            BugNet = 3,
            Axe = 4,
            Sickle = 5,
            Shovel = 6,
            WateringCan = 7
        }
        
        public enum ActionState
        {
            None = 0,
            DiggingCatching = 1,
            Cutting = 2,
            Shoveling = 3,
            Watering = 4,
            SwordAttack = 5,
            Archer = 6,
            Fishing = 7,
            Carrying = 8
        }
        
        public enum VehicleType
        {
            None = 0,
            Horse = 1,
            Bicycle = 2,
            Bear = 3
        }
        
        void Awake()
        {
            animator = GetComponent<Animator>();
            npcRenderer = GetComponent<ModularNPCRenderer>();
        }
        
        void Start()
        {
            // Ensure all override layers start disabled
            DisableAllOverrideLayers();
        }
        
        #region Layer Management
        
        /// <summary>
        /// Disable semua override layers, aktifkan base layer only
        /// </summary>
        public void DisableAllOverrideLayers()
        {
            if (animator == null) return;
            
            for (int i = 1; i < animator.layerCount; i++)
            {
                animator.SetLayerWeight(i, 0f);
            }
            
            if (debugMode) Debug.Log("[NPCLayerController] All override layers disabled");
        }
        
        /// <summary>
        /// Enable specific layer dan disable yang lain
        /// </summary>
        public void EnableLayer(int layerIndex)
        {
            if (animator == null) return;
            
            DisableAllOverrideLayers();
            
            if (layerIndex > 0 && layerIndex < animator.layerCount)
            {
                animator.SetLayerWeight(layerIndex, 1f);
                if (debugMode) Debug.Log($"[NPCLayerController] Layer {layerIndex} enabled");
            }
        }
        
        #endregion
        
        #region Tool Actions
        
        /// <summary>
        /// Start menggunakan tool
        /// </summary>
        public void UseTool(ToolType tool)
        {
            if (tool == ToolType.None)
            {
                StopUsingTool();
                return;
            }
            
            currentToolType = tool;
            
            // Determine action state, body skin, dan tool part
            NPCPartData toolPart = null;
            NPCPartData bodySkin = null;
            ActionState actionState = ActionState.None;
            int toolActionParam = 0;
            
            switch (tool)
            {
                case ToolType.Pickaxe:
                    toolPart = pickaxeToolPart;
                    bodySkin = diggingCatchingBodySkin;
                    actionState = ActionState.DiggingCatching;
                    toolActionParam = 1;
                    break;
                    
                case ToolType.Hoe:
                    toolPart = hoeToolPart;
                    bodySkin = diggingCatchingBodySkin;
                    actionState = ActionState.DiggingCatching;
                    toolActionParam = 1;
                    break;
                    
                case ToolType.BugNet:
                    toolPart = bugNetToolPart;
                    bodySkin = diggingCatchingBodySkin;
                    actionState = ActionState.DiggingCatching;
                    toolActionParam = 1;
                    break;
                    
                case ToolType.Axe:
                    toolPart = axeToolPart;
                    bodySkin = cuttingBodySkin;
                    actionState = ActionState.Cutting;
                    toolActionParam = 2;
                    break;
                    
                case ToolType.Sickle:
                    toolPart = sickleToolPart;
                    bodySkin = cuttingBodySkin;
                    actionState = ActionState.Cutting;
                    toolActionParam = 2;
                    break;
                    
                case ToolType.Shovel:
                    toolPart = shovelToolPart;
                    bodySkin = shovelBodySkin;
                    actionState = ActionState.Shoveling;
                    toolActionParam = 3;
                    break;
                    
                case ToolType.WateringCan:
                    toolPart = wateringCanToolPart;
                    bodySkin = wateringBodySkin;
                    actionState = ActionState.Watering;
                    toolActionParam = 4;
                    break;
            }
            
            currentActionState = actionState;
            
            // Update animator
            EnableLayer(TOOLS_LAYER);
            animator.SetInteger("ToolAction", toolActionParam);
            
            // Update renderer
            npcRenderer.SetCurrentTool(toolPart);
            npcRenderer.SetActionBodySkin(bodySkin);
            
            if (debugMode) Debug.Log($"[NPCLayerController] Using tool: {tool} | Action: {actionState}");
        }
        
        /// <summary>
        /// Stop menggunakan tool, kembali ke base movement
        /// </summary>
        public void StopUsingTool()
        {
            currentToolType = ToolType.None;
            currentActionState = ActionState.None;
            
            // Reset animator
            animator.SetInteger("ToolAction", 0);
            DisableAllOverrideLayers();
            
            // Reset renderer
            npcRenderer.ClearAction();
            
            if (debugMode) Debug.Log("[NPCLayerController] Stopped using tool");
        }
        
        #endregion
        
        #region Combat Actions
        
        /// <summary>
        /// Start combat attack dengan weapon tertentu
        /// </summary>
        public void StartAttack(WeaponType weapon)
        {
            NPCPartData bodySkin = null;
            int weaponTypeParam = 0;
            
            switch (weapon)
            {
                case WeaponType.Sword:
                    bodySkin = swordAttackBodySkin;
                    weaponTypeParam = 1;
                    currentActionState = ActionState.SwordAttack;
                    break;
                case WeaponType.Bow:
                    bodySkin = archerBodySkin;
                    weaponTypeParam = 2;
                    currentActionState = ActionState.Archer;
                    break;
            }
            
            // Update animator
            EnableLayer(COMBAT_LAYER);
            animator.SetInteger("WeaponType", weaponTypeParam);
            animator.SetBool("IsAttacking", true);
            
            // Update renderer
            npcRenderer.SetActionBodySkin(bodySkin);
            
            if (debugMode) Debug.Log($"[NPCLayerController] Attack started: {weapon}");
        }
        
        /// <summary>
        /// Stop combat attack
        /// </summary>
        public void StopAttack()
        {
            animator.SetBool("IsAttacking", false);
            currentActionState = ActionState.None;
            
            DisableAllOverrideLayers();
            npcRenderer.ClearAction();
            
            if (debugMode) Debug.Log("[NPCLayerController] Attack stopped");
        }
        
        public enum WeaponType
        {
            Sword = 1,
            Bow = 2
        }
        
        #endregion
        
        #region Vehicle Actions
        
        /// <summary>
        /// Mount vehicle
        /// </summary>
        public void MountVehicle(VehicleType vehicle)
        {
            if (vehicle == VehicleType.None)
            {
                Dismount();
                return;
            }
            
            currentVehicleType = vehicle;
            
            // Update animator
            EnableLayer(VEHICLES_LAYER);
            animator.SetInteger("VehicleType", (int)vehicle);
            
            if (debugMode) Debug.Log($"[NPCLayerController] Mounted: {vehicle}");
        }
        
        /// <summary>
        /// Dismount from vehicle
        /// </summary>
        public void Dismount()
        {
            currentVehicleType = VehicleType.None;
            
            animator.SetInteger("VehicleType", 0);
            DisableAllOverrideLayers();
            
            if (debugMode) Debug.Log("[NPCLayerController] Dismounted");
        }
        
        #endregion
        
        #region Fishing Actions
        
        /// <summary>
        /// Start fishing
        /// </summary>
        public void StartFishing()
        {
            currentActionState = ActionState.Fishing;
            
            EnableLayer(FISHING_LAYER);
            animator.SetBool("IsFishing", true);
            
            if (debugMode) Debug.Log("[NPCLayerController] Fishing started");
        }
        
        /// <summary>
        /// Stop fishing
        /// </summary>
        public void StopFishing()
        {
            currentActionState = ActionState.None;
            
            animator.SetBool("IsFishing", false);
            DisableAllOverrideLayers();
            
            if (debugMode) Debug.Log("[NPCLayerController] Fishing stopped");
        }
        
        /// <summary>
        /// Trigger fish bite
        /// </summary>
        public void TriggerFishBite()
        {
            animator.SetTrigger("FishBite");
        }
        
        #endregion
        
        #region Carrying Actions
        
        /// <summary>
        /// Start carrying item
        /// </summary>
        public void StartCarrying()
        {
            currentActionState = ActionState.Carrying;
            
            EnableLayer(CARRYING_LAYER);
            animator.SetBool("IsCarrying", true);
            animator.SetTrigger("StartCarrying");
            
            if (debugMode) Debug.Log("[NPCLayerController] Carrying started");
        }
        
        /// <summary>
        /// Throw item
        /// </summary>
        public void ThrowItem()
        {
            animator.SetTrigger("ThrowItem");
        }
        
        /// <summary>
        /// Stop carrying
        /// </summary>
        public void StopCarrying()
        {
            currentActionState = ActionState.None;
            
            animator.SetBool("IsCarrying", false);
            DisableAllOverrideLayers();
            
            if (debugMode) Debug.Log("[NPCLayerController] Carrying stopped");
        }
        
        #endregion
        
        #region Special Actions
        
        /// <summary>
        /// Toggle umbrella
        /// </summary>
        public void SetUmbrella(bool active)
        {
            if (active)
            {
                EnableLayer(SPECIAL_LAYER);
            }
            else
            {
                DisableAllOverrideLayers();
            }
            
            animator.SetBool("IsUsingUmbrella", active);
            
            if (debugMode) Debug.Log($"[NPCLayerController] Umbrella: {active}");
        }
        
        /// <summary>
        /// Start petting
        /// </summary>
        public void StartPetting()
        {
            EnableLayer(SPECIAL_LAYER);
            animator.SetBool("IsPetting", true);
            
            if (debugMode) Debug.Log("[NPCLayerController] Petting started");
        }
        
        /// <summary>
        /// Stop petting
        /// </summary>
        public void StopPetting()
        {
            animator.SetBool("IsPetting", false);
            DisableAllOverrideLayers();
            
            if (debugMode) Debug.Log("[NPCLayerController] Petting stopped");
        }
        
        /// <summary>
        /// Start climbing
        /// </summary>
        public void StartClimbing()
        {
            EnableLayer(SPECIAL_LAYER);
            animator.SetBool("IsClimbing", true);
            
            if (debugMode) Debug.Log("[NPCLayerController] Climbing started");
        }
        
        /// <summary>
        /// Stop climbing
        /// </summary>
        public void StopClimbing()
        {
            animator.SetBool("IsClimbing", false);
            DisableAllOverrideLayers();
            
            if (debugMode) Debug.Log("[NPCLayerController] Climbing stopped");
        }
        
        #endregion
        
        #region State Queries
        
        /// <summary>
        /// Get current tool type
        /// </summary>
        public ToolType GetCurrentToolType() => currentToolType;
        
        /// <summary>
        /// Get current action state
        /// </summary>
        public ActionState GetCurrentActionState() => currentActionState;
        
        /// <summary>
        /// Get current vehicle type
        /// </summary>
        public VehicleType GetCurrentVehicleType() => currentVehicleType;
        
        /// <summary>
        /// Check if player is in any action state
        /// </summary>
        public bool IsInAction() => currentActionState != ActionState.None;
        
        /// <summary>
        /// Check if player is mounted
        /// </summary>
        public bool IsMounted() => currentVehicleType != VehicleType.None;
        
        #endregion
    }
}
