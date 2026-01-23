# Animator Controller Guide - Modular NPC System

## Overview

Panduan struktur Animator Controller yang terintegrasi dengan **ModularNPCRenderer**. Pendekatan ini meminimalkan jumlah state di Animator karena switching tool/weapon dilakukan via script, bukan via Animator transitions.

---

## Konsep Utama

### Prinsip Dasar:
- **Animator Controller** → Handle **BODY animation** saja
- **ModularNPCRenderer** → Handle **tool/weapon/accessory sprites**
- **1 Animation State** bisa dipakai untuk **multiple tools** yang animasi body-nya sama

### Contoh:
```
Pickaxe, Hoe, Bug Net → Body animation SAMA
                      → Tool sprite BEDA (di-handle ModularNPCRenderer)
                      → Animator cuma perlu 1 state "DiggingCatching"
```

---

## Struktur Animator Controller

```
🎮 PLAYER ANIMATOR CONTROLLER

┌─────────────────────────────────────────────────────────────┐
│ Layer 0: BASE MOVEMENT (Weight: 1.0, Always Active)        │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────┐    ┌─────────┐    ┌─────────┐                 │
│  │  Idle   │◄──►│  Walk   │◄──►│   Run   │                 │
│  │(BlendT) │    │(BlendT) │    │(BlendT) │                 │
│  └─────────┘    └─────────┘    └─────────┘                 │
│                                                             │
│  Transitions:                                               │
│  - Idle → Walk: Speed > 0.1                                │
│  - Walk → Run: Speed > 5                                   │
│  - Run → Walk: Speed < 5                                   │
│  - Walk → Idle: Speed < 0.1                                │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ Layer 1: TOOLS & FARMING (Weight: 0 or 1, Override)        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────┐                   │
│  │ 📁 Sub-State: DiggingCatching       │                   │
│  │  ┌───────────────────────────┐      │                   │
│  │  │ DiggingCatching_Action    │      │                   │
│  │  │ (Blend Tree 4-dir)        │      │                   │
│  │  │                           │      │                   │
│  │  │ Untuk: Pickaxe, Hoe,      │      │                   │
│  │  │        Bug Net            │      │                   │
│  │  └───────────────────────────┘      │                   │
│  └─────────────────────────────────────┘                   │
│                                                             │
│  ┌─────────────────────────────────────┐                   │
│  │ 📁 Sub-State: Cutting               │                   │
│  │  ┌───────────────────────────┐      │                   │
│  │  │ Cutting_Action            │      │                   │
│  │  │ (Blend Tree 4-dir)        │      │                   │
│  │  │                           │      │                   │
│  │  │ Untuk: Axe, Sickle        │      │                   │
│  │  └───────────────────────────┘      │                   │
│  └─────────────────────────────────────┘                   │
│                                                             │
│  ┌────────────────┐  ┌────────────────┐                    │
│  │ Shovel_Action  │  │ Watering_Action│                    │
│  │ (Blend Tree)   │  │ (Blend Tree)   │                    │
│  └────────────────┘  └────────────────┘                    │
│                                                             │
│  Transitions:                                               │
│  - Any → DiggingCatching: ToolAction == 1                  │
│  - Any → Cutting: ToolAction == 2                          │
│  - Any → Shovel: ToolAction == 3                           │
│  - Any → Watering: ToolAction == 4                         │
│  - All → Exit: ToolAction == 0                             │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ Layer 2: COMBAT (Weight: 0 or 1, Override)                 │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌────────────────┐  ┌────────────────┐                    │
│  │ SwordAttack    │  │ Archer         │                    │
│  │ (Blend Tree)   │  │ (Blend Tree)   │                    │
│  └────────────────┘  └────────────────┘                    │
│                                                             │
│  ┌────────────────┐  ┌────────────────┐                    │
│  │ Damage         │  │ Death          │                    │
│  └────────────────┘  └────────────────┘                    │
│                                                             │
│  Transitions:                                               │
│  - Any → SwordAttack: IsAttacking && WeaponType == 1       │
│  - Any → Archer: IsAttacking && WeaponType == 2            │
│  - Any → Damage: TakeDamage trigger                        │
│  - Damage → Death: IsDead                                  │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ Layer 3: FISHING (Weight: 0 or 1, Override)                │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ 📁 Sub-State: Fishing Sequence                      │   │
│  │                                                      │   │
│  │  ┌──────┐   ┌──────┐   ┌──────┐   ┌──────┐   ┌─────┐│   │
│  │  │ Cast │──►│ Wait │──►│ Bite │──►│ Reel │──►│Catch││   │
│  │  └──────┘   └──────┘   └──────┘   └──────┘   └─────┘│   │
│  │                                                      │   │
│  │  Transitions:                                        │   │
│  │  - Cast → Wait: HasExitTime                         │   │
│  │  - Wait → Bite: FishBite trigger                    │   │
│  │  - Bite → Reel: Auto                                │   │
│  │  - Reel → Catch: ReelComplete                       │   │
│  │  - Catch → Exit: HasExitTime                        │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Entry Transition:                                          │
│  - Any → Fishing Sub-State: IsFishing == true              │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ Layer 4: CARRYING (Weight: 0 or 1, Override)               │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ 📁 Sub-State: Carrying Actions                      │   │
│  │                                                      │   │
│  │  ┌────────────┐  ┌────────────┐  ┌────────────┐     │   │
│  │  │Carry_Idle  │◄►│Carry_Walk  │◄►│Carry_Run   │     │   │
│  │  │(Blend Tree)│  │(Blend Tree)│  │(Blend Tree)│     │   │
│  │  └────────────┘  └────────────┘  └────────────┘     │   │
│  │         ▲                                            │   │
│  │         │                     ┌────────────┐        │   │
│  │  ┌──────┴─────┐              │ Throw      │        │   │
│  │  │ PickUp     │──────────────►            │        │   │
│  │  └────────────┘              └────────────┘        │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Transitions:                                               │
│  - Entry → PickUp: StartCarrying trigger                   │
│  - PickUp → Carry_Idle: HasExitTime                        │
│  - Carry_Idle/Walk/Run: Same as Base Movement              │
│  - Any → Throw: ThrowItem trigger                          │
│  - Throw → Exit: HasExitTime                               │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ Layer 5: VEHICLES (Weight: 0 or 1, Override)               │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────┐                   │
│  │ 📁 Sub-State: HORSE                 │                   │
│  │  ┌───────┐ ┌───────┐ ┌───────┐     │                   │
│  │  │ Idle  │◄►│ Walk  │◄►│ Run   │     │                   │
│  │  └───────┘ └───────┘ └───────┘     │                   │
│  │  ┌───────┐ ┌───────┐               │                   │
│  │  │ Lower │ │Eating │               │                   │
│  │  └───────┘ └───────┘               │                   │
│  └─────────────────────────────────────┘                   │
│                                                             │
│  ┌─────────────────────────────────────┐                   │
│  │ 📁 Sub-State: BICYCLE               │                   │
│  │  ┌───────┐ ┌───────┐               │                   │
│  │  │ Idle  │◄►│ Run   │               │                   │
│  │  └───────┘ └───────┘               │                   │
│  └─────────────────────────────────────┘                   │
│                                                             │
│  ┌─────────────────────────────────────┐                   │
│  │ 📁 Sub-State: BEAR                  │                   │
│  │  ┌───────┐ ┌───────┐ ┌───────┐     │                   │
│  │  │ Idle  │◄►│ Walk  │◄►│ Run   │     │                   │
│  │  └───────┘ └───────┘ └───────┘     │                   │
│  │  ┌───────┐ ┌───────┐ ┌───────┐     │                   │
│  │  │Attack │ │ Hit   │ │ Dead  │     │                   │
│  │  └───────┘ └───────┘ └───────┘     │                   │
│  └─────────────────────────────────────┘                   │
│                                                             │
│  Transitions:                                               │
│  - Any → Horse: VehicleType == 1                           │
│  - Any → Bicycle: VehicleType == 2                         │
│  - Any → Bear: VehicleType == 3                            │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ Layer 6: SPECIAL (Weight: 0 or 1, Override/Additive)       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────┐                   │
│  │ 📁 Sub-State: UMBRELLA              │                   │
│  │  ┌───────┐ ┌───────┐ ┌───────┐     │                   │
│  │  │ Idle  │◄►│ Walk  │◄►│ Run   │     │                   │
│  │  └───────┘ └───────┘ └───────┘     │                   │
│  └─────────────────────────────────────┘                   │
│                                                             │
│  ┌────────────────┐  ┌────────────────┐                    │
│  │ Petting        │  │ Climbing       │                    │
│  │ (1 animation)  │  │ (1 animation)  │                    │
│  └────────────────┘  └────────────────┘                    │
│                                                             │
│  Transitions:                                               │
│  - Any → Umbrella: IsUsingUmbrella == true                 │
│  - Any → Petting: IsPetting == true                        │
│  - Any → Climbing: IsClimbing == true                      │
└─────────────────────────────────────────────────────────────┘
```

---

## Animator Parameters

```yaml
# Movement
Speed: Float           # 0 = idle, 0.1-5 = walk, >5 = run
Horizontal: Float      # -1 = left, 1 = right
Vertical: Float        # -1 = down, 1 = up

# Tools & Farming
ToolAction: Int        # 0=none, 1=DiggingCatching, 2=Cutting, 3=Shovel, 4=Watering

# Combat
IsAttacking: Bool
WeaponType: Int        # 1=Sword, 2=Bow
TakeDamage: Trigger
IsDead: Bool

# Fishing
IsFishing: Bool
FishBite: Trigger
ReelComplete: Bool

# Carrying
IsCarrying: Bool
StartCarrying: Trigger
ThrowItem: Trigger

# Vehicles
VehicleType: Int       # 0=none, 1=Horse, 2=Bicycle, 3=Bear
IsEating: Bool         # Horse eating
BearAttack: Trigger

# Special
IsUsingUmbrella: Bool
IsPetting: Bool
IsClimbing: Bool
```

---

## Layer Control via Script

```csharp
public class LayerController : MonoBehaviour
{
    private Animator animator;
    
    // Layer indices
    private const int BASE_LAYER = 0;
    private const int TOOLS_LAYER = 1;
    private const int COMBAT_LAYER = 2;
    private const int FISHING_LAYER = 3;
    private const int CARRYING_LAYER = 4;
    private const int VEHICLES_LAYER = 5;
    private const int SPECIAL_LAYER = 6;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        DisableAllOverrideLayers();
    }
    
    void DisableAllOverrideLayers()
    {
        for (int i = 1; i <= 6; i++)
        {
            animator.SetLayerWeight(i, 0f);
        }
    }
    
    // ==================== TOOLS ====================
    public void UseTool(ToolType tool)
    {
        DisableAllOverrideLayers();
        animator.SetLayerWeight(TOOLS_LAYER, 1f);
        
        switch (tool)
        {
            case ToolType.Pickaxe:
            case ToolType.Hoe:
            case ToolType.BugNet:
                animator.SetInteger("ToolAction", 1); // DiggingCatching
                break;
            case ToolType.Axe:
            case ToolType.Sickle:
                animator.SetInteger("ToolAction", 2); // Cutting
                break;
            case ToolType.Shovel:
                animator.SetInteger("ToolAction", 3);
                break;
            case ToolType.WateringCan:
                animator.SetInteger("ToolAction", 4);
                break;
        }
        
        // ModularNPCRenderer handles the actual tool sprite!
        modularRenderer.SetCurrentTool(tool);
    }
    
    public void StopUsingTool()
    {
        animator.SetInteger("ToolAction", 0);
        animator.SetLayerWeight(TOOLS_LAYER, 0f);
    }
    
    // ==================== VEHICLES ====================
    public void MountVehicle(VehicleType vehicle)
    {
        DisableAllOverrideLayers();
        animator.SetLayerWeight(VEHICLES_LAYER, 1f);
        animator.SetInteger("VehicleType", (int)vehicle);
    }
    
    public void Dismount()
    {
        animator.SetInteger("VehicleType", 0);
        animator.SetLayerWeight(VEHICLES_LAYER, 0f);
    }
    
    // ==================== COMBAT ====================
    public void Attack(WeaponType weapon)
    {
        DisableAllOverrideLayers();
        animator.SetLayerWeight(COMBAT_LAYER, 1f);
        animator.SetInteger("WeaponType", (int)weapon);
        animator.SetBool("IsAttacking", true);
    }
    
    // ... etc
}
```

---

## Integrasi dengan ModularNPCRenderer

Tool/Weapon sprites di-manage oleh ModularNPCRenderer, bukan Animator:

```csharp
// Di ModularNPCRenderer atau script terpisah
public class ToolSpriteController : MonoBehaviour
{
    public ModularNPCRenderer renderer;
    
    [Header("Tool Part Data")]
    public NPCPartData pickaxePartData;
    public NPCPartData hoePartData;
    public NPCPartData bugNetPartData;
    public NPCPartData axePartData;
    public NPCPartData sicklePartData;
    
    public void SetCurrentTool(ToolType tool)
    {
        NPCPartData toolPart = null;
        
        switch (tool)
        {
            case ToolType.Pickaxe:
                toolPart = pickaxePartData;
                break;
            case ToolType.Hoe:
                toolPart = hoePartData;
                break;
            case ToolType.BugNet:
                toolPart = bugNetPartData;
                break;
            case ToolType.Axe:
                toolPart = axePartData;
                break;
            case ToolType.Sickle:
                toolPart = sicklePartData;
                break;
        }
        
        // Assign ke switchable accessory slot
        renderer.SetSwitchableAccessory(0, toolPart);
    }
}
```

---

## Setup NPCPartData untuk Tools

### Contoh: Pickaxe Part Data
```yaml
Part Name: Tool_Pickaxe
Category: Accessory
Accessory Type: Switchable

# Direction Offsets (posisi tool relative ke body)
Offset Down: (0, 0.1, 0)
Offset Up: (0, 0.15, 0)
Offset Left: (-0.1, 0.1, 0)
Offset Right: (0.1, 0.1, 0)

# Animation States
Animation States:
  - State Name: DiggingCatching
    Sprites: [Pickaxe frame 0-15]

# Custom Direction (jika layout beda dari body)
Use Custom Direction Offsets: false  # Sama dengan body
```

---

## Quick Setup Checklist

### Di Unity Animator Window:
- [ ] Create 7 layers (Base + 6 Override)
- [ ] Setup parameters (Speed, Horizontal, Vertical, ToolAction, etc.)
- [ ] Create Sub-State Machines untuk grouped actions
- [ ] Setup Blend Trees untuk 4-directional animations
- [ ] Configure transitions dengan conditions

### Di Project:
- [ ] Create NPCPartData untuk setiap tool/weapon
- [ ] Slice sprite sheets sesuai frame count
- [ ] Setup layer weights logic di script
- [ ] Test sinkronisasi body + tool sprites

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Layer tidak switch | Cek layer weight sudah di-set ke 1 |
| Animation tidak play | Cek parameter condition sudah benar |
| Tool sprite tidak sync | Cek NPCPartData frame mapping |
| Direction salah | Cek Custom Direction Offsets di NPCPartData |

---

## Related Files

- [ModularNPCRenderer.cs](./ModularNPCRenderer.cs)
- [NPCAnimationSynchronizer.cs](./NPCAnimationSynchronizer.cs)
- [NPCPartData.cs](./NPCPartData.cs)
- [README.md](./README.md) - Dokumentasi sistem customization
