---
name: unity-asset-organization
description: "Unity asset organization guidance. Use when organizing Unity Assets folders, renaming assets, reviewing prefab/UI/Resources layout, or proposing asset naming conventions and folder structure."
---

# Unity Asset Organization

## Overview
Standards for organizing assets, naming conventions, and folder structure to maintain clean, scalable Unity projects.

## Folder Structure
Use `Assets/Project/` for project-owned assets. If a project already uses `_Project/` to force the folder to the top of Unity's Project window, keep that existing convention instead of renaming it.

Use type-based folders for shared raw assets such as materials, textures, shaders, models, audio, and animations. Use feature/system grouping inside prefabs, UI, scripts, and scene-specific content when that keeps related work together.

```
Assets/
├── Project/              # Project-specific assets
│   ├── Data/
│   │   ├── Materials/
│   │   ├── Textures/
│   │   ├── Shaders/
│   │   ├── Models/
│   │   ├── VFX/
│   │   ├── Audio/
│   │   │   ├── Music/
│   │   │   └── SFX/
│   │   └── Animations/
│   ├── Prefabs/
│   │   ├── Gameplay/
│   │   ├── UI/
│   │   └── VFX/
│   ├── Scenes/
│   ├── Scripts/
│   │   ├── Core/
│   │   ├── Gameplay/
│   │   ├── UI/
│   │   └── Utils/
│   └── ScriptableObjects/
├── Plugins/               # Third-party plugins, jslib
├── Resources/             # Runtime-loaded assets (use sparingly)
├── StreamingAssets/       # Files copied as-is to build
└── ThirdParty/            # External packages
```

## Naming Conventions

### General Rules
- Use **PascalCase** for the descriptive base name
- Use underscores only for approved type prefixes, category prefixes, and texture suffixes
- Be descriptive: `PlayerHealthBar` not `HealthBar1`
- Include type suffix where helpful: `PlayerController`, `PlayerStats`
- Do not add redundant suffixes when the file extension already communicates the type, such as `Enemy.prefab` instead of `EnemyPrefab.prefab`

### Prefixes by Type
| Type | Prefix | Example |
|------|--------|---------|
| Prefab | Optional domain prefix | `Player.prefab`, `Enemy_Goblin.prefab`, `UI_HealthBar.prefab`, `VFX_Explosion.prefab` |
| Material | M_ | `M_Metal.mat` |
| Texture | T_ | `T_Wood_Diffuse.png` |
| UI Sprite | UI_ | `UI_Button_Normal.png` |
| Animation | Anim_ | `Anim_Run.anim` |
| Animator | AC_ | `AC_Player.controller` |
| ScriptableObject | SO_ | `SO_PlayerStats.asset` |
| Audio Clip | SFX_ / Music_ / VO_ | `SFX_Jump.wav`, `Music_BattleLoop.wav`, `VO_NarratorIntro.wav` |

### Texture Suffixes
| Suffix | Purpose |
|--------|---------|
| _Diffuse / _D | Albedo/Base color |
| _Normal / _N | Normal map |
| _Metallic / _M | Metallic map |
| _Roughness / _R | Roughness map |
| _AO | Ambient occlusion |
| _Emission / _E | Emission map |

## Prefab Best Practices

### Organization
- Group prefabs by feature/system when possible, not only by Unity type
- One prefab per file
- Keep prefab hierarchies shallow (max 3-4 levels)

### Naming
```
Player.prefab           # Main character
Enemy_Goblin.prefab     # Enemy variant
UI_HealthBar.prefab     # UI element
VFX_Explosion.prefab    # Visual effect
```

### Prefab Variants
```
Enemy_Base.prefab       # Base prefab
├── Enemy_Goblin.prefab    # Variant
├── Enemy_Skeleton.prefab  # Variant
└── Enemy_Boss.prefab      # Variant
```

## UI Organization
```
Prefabs/UI/
├── Common/
│   ├── UI_Button.prefab
│   ├── UI_Panel.prefab
│   └── UI_Text.prefab
├── Screens/
│   ├── UI_MainMenu.prefab
│   ├── UI_Settings.prefab
│   └── UI_GameOver.prefab
└── HUD/
    ├── UI_HealthBar.prefab
    └── UI_Minimap.prefab
```

## Resources Folder
Use sparingly - everything in Resources is included in build.

```csharp
// Only for truly dynamic loading
var prefab = Resources.Load<GameObject>("Prefabs/DynamicItem");
```

## Class Structure
- Keep Track C:\Users\yls11\Uni_Virtual_Projects\Dynamic 3D Movement\.agents\references\architecture\class-diagram.md updated with new classes and their relationships to assets.

**Prefer**: Addressables or direct references for most assets.

## Best Practices
- ✅ Use consistent naming across entire project
- ✅ Group shared raw assets by type and gameplay-facing assets by feature/system
- ✅ Keep Resources folder minimal
- ✅ Use Addressables for large projects
- ✅ Document project-specific exceptions in the project README or team docs
- ❌ **NEVER** use spaces in asset names
- ❌ **NEVER** use special characters outside the project's approved separators
- ❌ **NEVER** scatter related assets across folders
