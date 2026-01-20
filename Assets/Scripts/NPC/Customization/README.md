# NPC Customization System - Documentation

## Overview

Sistem modular untuk customization NPC dengan multiple sprite layers (skin, eyes, hair, clothes, accessories) yang di-synchronize dengan animasi body.

## File Utama

| File | Fungsi |
|------|--------|
| `NPCPartData.cs` | ScriptableObject untuk menyimpan sprites dan offset setiap part |
| `ModularNPCRenderer.cs` | Component utama untuk render dan manage semua sprite layers |
| `NPCAnimationSynchronizer.cs` | Sync animasi across semua layers berdasarkan Animator state |
| `NPCCustomizationPreset.cs` | Preset untuk menyimpan kombinasi parts |

---

## Fitur yang Sudah Diimplementasi

### 1. Custom Direction Frame Offsets
**Problem:** Eyes sprite sheet punya layout berbeda dari body (tidak ada frame Up).

**Solution:** Setiap NPCPartData bisa punya custom frame mapping:

```
Body Layout (16 frames):     Part Layout (12 frames, no Up):
- Down: 0-3                  - Down: 0-3  
- Up: 4-7                    - Right: 4-7 (mapped from body 8-11)
- Right: 8-11                - Left: 8-11 (mapped from body 12-15)
- Left: 12-15
```

**Settings di Inspector:**
- `Use Custom Direction Offsets` = true
- `Custom Frame Offset Down` = 0
- `Custom Frame Offset Up` = -1 (tidak ada)
- `Custom Frame Offset Right` = 4
- `Custom Frame Offset Left` = 8

### 2. Combined Offset System (Direction + Per-Frame + Bobbing)
**Formula:**
```
Final Position = Offset Direction + Offset Per-Frame + Bobbing
```

**Cara Pakai:**
1. Set `offsetDown`, `offsetRight`, `offsetLeft` untuk base position per arah
2. Centang `Use Per Frame Offsets` untuk fine-tune frame tertentu
3. Isi `Frame Offsets` array dengan adjustment tambahan per frame
4. Bobbing otomatis ditambahkan di code

### 3. Eyes Special Handling
- Otomatis **hidden saat facing Up** (tidak ada sprite Up untuk eyes)
- Punya **code-based bobbing** sama seperti hair
- Frame remapping untuk sprite layout yang berbeda

### 4. Blend Tree Support
- Support untuk 4-directional movement dengan Blend Tree
- Auto-detect direction dari Horizontal/Vertical parameters
- Support 4-in-1 sprite sheets

---

## Cara Setup NPCPartData Baru

### Untuk Part dengan Layout Standard (sama dengan body):
1. Buat NPCPartData baru via Create > NPC Customization > NPC Part Data
2. Assign sprites ke Animation States
3. Set directional offsets jika perlu

### Untuk Part dengan Layout Berbeda (misal: tanpa Up):
1. Centang **"Use Custom Direction Offsets"**
2. Set offset sesuai layout:
   - Eyes tanpa Up: Down=0, Up=-1, Right=4, Left=8
3. Set **"Frames Per Direction For Part"** jika berbeda dari 4

### Untuk Fine-tune Per-Frame:
1. Centang **"Use Per Frame Offsets"**
2. Isi **Frame Offsets** array dengan adjustment (ADDITIVE dengan direction offset)
3. Contoh: frame 5 perlu geser 0.1 ke kanan → `frameOffsets[5] = (0.1, 0, 0)`

---

## Bobbing System

### Bobbing Offsets (di ModularNPCRenderer):
```csharp
public float[] bobbingOffsets = new float[] { 0f, -0.01f, -0.02f, -0.01f };
```

### Parts yang Punya Bobbing:
- ✅ Hair (code-based)
- ✅ Eyes (code-based)
- ❌ Skin (mengikuti body animator)
- ❌ Clothes (mengikuti body animator)

---

## Known Limitations

1. **Frame count harus sama** - Semua parts harus punya jumlah frame per direction yang sama dengan body (default 4). Jika ada part dengan frame lebih banyak, frame ekstra tidak akan ditampilkan.

2. **Layout harus dikonfigurasi manual** - Setiap part dengan layout berbeda harus di-setup manual custom offsets di Inspector.

---

## Troubleshooting

### Eyes menampilkan frame yang salah:
- Pastikan `Use Custom Direction Offsets` = true
- Cek apakah offset Down/Right/Left sudah sesuai layout sprite sheet

### Eyes tidak bobbing:
- Pastikan `Enable Code Bobbing` di ModularNPCRenderer = true
- Cek apakah eyes layer sudah di-assign di ModularNPCRenderer

### Part tidak sinkron dengan body:
- Cek apakah NPCAnimationSynchronizer sudah attached
- Pastikan `Frames Per Animation` sesuai jumlah frame body

---

## Quick Reference: Common Configurations

| Part Type | Use Custom Offsets | Down | Up | Right | Left |
|-----------|-------------------|------|-----|-------|------|
| Body/Skin (16 frames) | ❌ No | 0 | 4 | 8 | 12 |
| Eyes (12 frames, no Up) | ✅ Yes | 0 | -1 | 4 | 8 |
| Hair (16 frames) | ❌ No | 0 | 4 | 8 | 12 |
