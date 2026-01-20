# NPC Customization System - Complete Documentation

## Overview

Sistem modular untuk customization NPC dengan multiple sprite layers (skin, eyes, hair, clothes, accessories) yang di-synchronize dengan animasi body. Mendukung sprite sheets dengan layout berbeda-beda per part.

---

## Struktur File

```
Assets/Scripts/NPC/Customization/
├── NPCPartData.cs              # ScriptableObject untuk data setiap part
├── ModularNPCRenderer.cs       # Component utama untuk render layers
├── NPCAnimationSynchronizer.cs # Sync animasi dengan Animator
├── NPCCustomizationPreset.cs   # Preset kombinasi parts
└── README.md                   # Dokumentasi ini
```

---

## NPCPartData.cs

ScriptableObject yang menyimpan data untuk setiap part (skin, eyes, hair, clothes, accessories).

### Properties Utama:

```csharp
// Info Part
public string partName;
public PartCategory category;        // Skin, Eyes, Clothes, Hair, Accessory
public AccessoryType accessoryType;  // Jika accessory

// Directional Offsets (posisi dasar per arah)
public Vector3 offsetDown;
public Vector3 offsetUp;
public Vector3 offsetLeft;
public Vector3 offsetRight;

// Per-Frame Offsets (fine-tuning per frame)
public bool usePerFrameOffsets;
public Vector3[] frameOffsets;       // Array 16 untuk tiap frame

// Custom Direction Frame Mapping (untuk sprite layout berbeda)
public bool useCustomDirectionOffsets;
public int customFrameOffsetDown;    // Default: 0
public int customFrameOffsetUp;      // Default: 4, set -1 jika tidak ada
public int customFrameOffsetRight;   // Default: 8
public int customFrameOffsetLeft;    // Default: 12
public int framesPerDirectionForPart; // Default: 4

// Animation Sprites
public AnimationStateSprites[] animationStates;
```

### Method Penting:

```csharp
// Get offset gabungan (direction + per-frame)
Vector3 GetOffsetForFrame(int frameIndex, string direction)
// Formula: Direction Offset + Per-Frame Offset (ADDITIVE)

// Remap frame index dari body ke part
int RemapFrameForPart(int bodyFrameIndex, string direction, int bodyFramesPerDirection)
// Contoh: body frame 8 → eyes frame 4 (jika eyes layout berbeda)

// Get custom offset untuk direction
int GetCustomOffsetForDirection(string direction)
// Return -1 jika direction tidak ada (misal Up untuk eyes)
```

---

## ModularNPCRenderer.cs

Component utama yang mengelola semua sprite layers.

### Layer References:
```csharp
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
```

### Bobbing Settings:
```csharp
public bool enableCodeBobbing = true;
public float[] bobbingOffsets = new float[] { 0f, -0.01f, -0.02f, -0.01f };
```

### Method Utama:
```csharp
// Update semua sprites untuk animasi
void UpdateSpritesForAnimation(string stateName, int frameIndex)

// Update single layer dengan remapping dan offsets
void UpdateLayerSprite(SpriteRenderer layer, NPCPartData partData, 
                       string stateName, int frameIndex, 
                       string currentDirection, bool directionChanged, 
                       bool skipPositionUpdate)
```

### Eyes Special Handling:
```csharp
// Eyes otomatis hidden saat facing Up
if (currentDirection == "Up")
{
    eyesLayer.enabled = false;
    eyesLayer.sprite = null;
}
else
{
    eyesLayer.enabled = true;
    // Update dengan bobbing
}
```

---

## NPCAnimationSynchronizer.cs

Sync animasi antara Animator dan semua sprite layers.

### Mode:
1. **Animation Events (RECOMMENDED)** - Manual events di animation clips
2. **Auto-detection (FALLBACK)** - Auto detect state/frame dari Animator

### Blend Tree Support:
```csharp
public bool useBlendTree = false;
public string horizontalParameter = "Horizontal";
public string verticalParameter = "Vertical";
public bool use4in1SpriteSheets = true;
public int framesPerDirection = 4;
```

### Direction Detection:
```csharp
// Dari Blend Tree parameters
string GetDirectionFromParameters(float horizontal, float vertical)
// Return: "Down", "Up", "Left", "Right"
```

### Frame Offset per Direction:
```csharp
// Body sprite sheet layout: 16 frames
// Down: 0-3, Up: 4-7, Right: 8-11, Left: 12-15
int GetDirectionOffset(string direction)
{
    switch (direction)
    {
        case "Down":  return 0;
        case "Up":    return framesPerDirection;      // 4
        case "Right": return framesPerDirection * 2;  // 8
        case "Left":  return framesPerDirection * 3;  // 12
    }
}
```

---

## Masalah yang Sudah Diselesaikan

### 1. Eyes Frame Tidak Sinkron dengan Body

**Problem:** 
Eyes punya layout berbeda (12 frames, tanpa Up):
- Down: 0-3
- Right: 4-7 (bukan 8-11 seperti body)
- Left: 8-11 (bukan 12-15 seperti body)

**Solution:**
Implementasi `RemapFrameForPart()` di NPCPartData:
```csharp
// Body mengirim frame 8 (Right frame 0)
// Eyes remap ke frame 4 (karena Right di eyes mulai dari 4)
int eyesFrame = currentEyes.RemapFrameForPart(bodyFrame, direction, 4);
```

**Setup di Inspector:**
- Use Custom Direction Offsets: ✅
- Custom Frame Offset Down: 0
- Custom Frame Offset Up: -1 (tidak ada)
- Custom Frame Offset Right: 4
- Custom Frame Offset Left: 8

### 2. Eyes Tidak Ada Frame Up

**Problem:**
Eyes tidak punya sprites untuk arah Up.

**Solution:**
Special handling di ModularNPCRenderer:
```csharp
if (currentDirection == "Up")
{
    eyesLayer.enabled = false;
    eyesLayer.sprite = null;
}
```

### 3. Eyes Tidak Bobbing (Naik Turun)

**Problem:**
Setelah perubahan, eyes tidak lagi mengikuti gerakan naik-turun badan.

**Solution:**
Tambah code-based bobbing untuk eyes:
```csharp
if (eyesLayer != null && currentDirection != "Up" && enableCodeBobbing)
{
    int eyesFrameIndex = currentEyes.RemapFrameForPart(frameIndex, currentDirection, 4);
    Vector3 combinedOffset = currentEyes.GetOffsetForFrame(eyesFrameIndex, currentDirection);
    int bobbingFrame = frameIndex % bobbingOffsets.Length;
    float bobbingY = bobbingOffsets[bobbingFrame];
    eyesLayer.transform.localPosition = new Vector3(
        combinedOffset.x, 
        combinedOffset.y + bobbingY, 
        combinedOffset.z
    );
}
```

### 4. Offset Per-Direction dan Per-Frame Tidak Bisa Digabung

**Problem:**
Ingin fine-tune posisi frame tertentu sambil tetap punya base offset per direction.

**Solution:**
Ubah `GetOffsetForFrame()` menjadi ADDITIVE:
```csharp
public Vector3 GetOffsetForFrame(int frameIndex, string direction)
{
    // Base dari direction
    Vector3 baseOffset = GetOffsetForDirection(direction);
    
    // Tambah per-frame adjustment
    if (usePerFrameOffsets && frameIndex < frameOffsets.Length)
    {
        return baseOffset + frameOffsets[frameIndex];
    }
    
    return baseOffset;
}
```

---

## Setup Guide

### Setup NPC Baru:

1. **Buat GameObject** dengan Animator
2. **Add Components:**
   - ModularNPCRenderer
   - NPCAnimationSynchronizer
3. **Buat NPCPartData** untuk setiap part (skin, eyes, hair, clothes)
4. **Buat NPCCustomizationPreset** dan assign parts
5. **Assign preset** ke ModularNPCRenderer

### Setup Part dengan Layout Berbeda:

1. Buka NPCPartData di Inspector
2. Centang **"Use Custom Direction Offsets"**
3. Isi offset sesuai layout:

| Layout Type | Down | Up | Right | Left |
|-------------|------|-----|-------|------|
| Standard 16 frames | 0 | 4 | 8 | 12 |
| No Up (12 frames) | 0 | -1 | 4 | 8 |
| Down only (4 frames) | 0 | -1 | -1 | -1 |

### Fine-tune Posisi Per-Frame:

1. Centang **"Use Per Frame Offsets"**
2. Expand **Frame Offsets** array
3. Isi adjustment untuk frame yang perlu digeser
4. Nilai ADDITIVE (ditambah ke direction offset)

---

## Flow Animasi

```
1. Animator update state
      ↓
2. NPCAnimationSynchronizer detect frame & direction
      ↓
3. Call ModularNPCRenderer.UpdateSpritesForAnimation(state, frame)
      ↓
4. For each layer:
   a. RemapFrameForPart() → convert body frame to part frame
   b. GetSpriteFrame() → get sprite for that frame
   c. GetOffsetForFrame() → get combined offset
   d. Apply bobbing if enabled
   e. Set sprite & position
```

---

## Quick Reference

### Bobbing Values:
```csharp
// Frame 0: 0.00 (normal)
// Frame 1: -0.01 (turun sedikit)
// Frame 2: -0.02 (turun max)
// Frame 3: -0.01 (naik sedikit)
```

### Layer Order (sorting):
```
0: Skin
1: Eyes
2: Clothes
3: Hair
4-5: Permanent Accessories
6-9: Switchable Accessories
```

### Common Issues:
| Issue | Solution |
|-------|----------|
| Frame salah | Cek Custom Direction Offsets |
| Tidak bobbing | Enable Code Bobbing di ModularNPCRenderer |
| Eyes muncul saat Up | Pastikan Update terbaru sudah diapply |
| Posisi geser | Adjust directional offsets di NPCPartData |

---

## Change Log

### Session 2026-01-14 to 2026-01-20:
- ✅ Implementasi Custom Direction Frame Offsets
- ✅ Eyes auto-hide saat facing Up
- ✅ Combined offset system (Direction + Per-Frame + Bobbing)
- ✅ Frame remapping untuk sprite layouts berbeda
- ✅ Code-based bobbing untuk Eyes (sama seperti Hair)
