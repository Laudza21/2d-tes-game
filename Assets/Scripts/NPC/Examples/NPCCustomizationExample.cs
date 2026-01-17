using UnityEngine;

namespace NPCCustomization
{
    /// <summary>
    /// Example script demonstrating NPC customization usage.
    /// Attach to GameObject dengan ModularNPCRenderer untuk demo.
    /// </summary>
    public class NPCCustomizationExample : MonoBehaviour
    {
        [Header("Example Settings")]
        [Tooltip("Test preset untuk demo")]
        public NPCCustomizationPreset testPreset;

        [Tooltip("Test dengan ganti accessories tiap X detik")]
        public bool testAccessorySwitching = false;

        [Tooltip("Interval untuk test switching (detik)")]
        public float switchInterval = 5f;

        private ModularNPCRenderer npcRenderer;
        private float switchTimer = 0f;
        private int currentAccessoryIndex = 0;

        void Start()
        {
            npcRenderer = GetComponent<ModularNPCRenderer>();

            if (npcRenderer == null)
            {
                Debug.LogError("NPCCustomizationExample requires ModularNPCRenderer component!");
                return;
            }

            // Apply test preset jika ada
            if (testPreset != null)
            {
                npcRenderer.ApplyCustomization(testPreset);
                Debug.Log($"Applied test preset: {testPreset.name}");
            }
        }

        void Update()
        {
            // Test accessory switching
            if (testAccessorySwitching && testPreset != null)
            {
                switchTimer += Time.deltaTime;

                if (switchTimer >= switchInterval)
                {
                    switchTimer = 0f;
                    TestSwitchAccessory();
                }
            }
        }

        /// <summary>
        /// Test: ganti accessory ke next option dalam pool
        /// </summary>
        private void TestSwitchAccessory()
        {
            // Test untuk slot 1
            var pool = testPreset.GetSwitchablePool(0);

            if (pool.Count > 0)
            {
                currentAccessoryIndex = (currentAccessoryIndex + 1) % pool.Count;
                
                NPCPartData nextAccessory = pool[currentAccessoryIndex];
                npcRenderer.SetSwitchableAccessory(0, nextAccessory);

                Debug.Log($"Switched to accessory: {nextAccessory.partName}");
            }
        }

        /// <summary>
        /// Example: manually change customization at runtime
        /// </summary>
        public void ApplyCustomPreset(NPCCustomizationPreset preset)
        {
            if (npcRenderer != null && preset != null)
            {
                npcRenderer.ApplyCustomization(preset);
            }
        }

        /// <summary>
        /// Example: change specific part at runtime
        /// </summary>
        public void ChangePart(PartCategory category, NPCPartData partData)
        {
            if (npcRenderer != null)
            {
                npcRenderer.SetPart(category, partData);
            }
        }

        /// <summary>
        /// Example: change accessory untuk specific slot
        /// </summary>
        public void ChangeAccessory(int slotIndex, NPCPartData accessory)
        {
            if (npcRenderer != null)
            {
                npcRenderer.SetSwitchableAccessory(slotIndex, accessory);
            }
        }
    }
}
