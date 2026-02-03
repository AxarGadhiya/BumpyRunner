# StumbleGuy Unity Project Index

## Project Overview
A multiplayer obstacle course racing game (similar to Fall Guys) built in Unity. The game features player movement, AI bots, ragdoll physics, and various environmental obstacles.

## Project Structure

### Core Directories

#### `/Game/`
Main game content directory containing all game-specific assets and scripts.

**Subdirectories:**
- **`/Scripts/`** - All C# game scripts
  - **`/Character/`** - core modular controller logic (Shared for Player & AI)
  - **`/Player/`** - Player-specific inputs and logic
  - **`/AI/`** - AI-specific logic
  - **`/Old/`** - Deprecated/Unused scripts (`Movement.cs`, `AIController.cs`)
  - **`/Camera/`** - Camera systems
  - **`/Environment/`** - Environmental interaction scripts
  - **`/GamePlay/`** - Gameplay and round management
- **`/GameAssets/`** - Game assets (meshes, materials, prefabs)
  - **`/Character/`** - Character models and materials
  - **`/Environment/`** - Environment meshes, materials, and prefabs
  - **`/PhysicMaterial/`** - Physics materials (Bounce, Friction, etc.)
- **`/Scenes/`** - Unity scene files
- **`/AnimationClip/`** - Animation clips
- **`/AnimatorController/`** - Animator controller assets

#### `/Joystick Pack/`
Third-party joystick input system for mobile controls.
**Subdirectories:**
- **`/Scripts/`** - Core joystick logic (`Base`, `Joysticks`, `Editor`)
- **`/Prefabs/`** - Ready-to-use joystick prefabs
- **`/Examples/`** - Example scenes and scripts

#### `/ithappy/`
Environment and level art asset packs.
**Subdirectories:**
- **`/Platformer_12_Underwater/`** - Underwater theme assets
- **`/Platformer_3_Chocolate/`** - Chocolate/Candy theme assets
- **`/Sweet_Land/`** - General sweet/candy land environment assets

#### `/Photon/`
Photon networking integration (currently commented out in code).

#### `/TextMesh Pro/`
TextMesh Pro package for UI text rendering.

#### `/Settings/`
Unity project settings and configurations.

---

## Key Scripts

### Core Game Management

#### `RunTimeGame.cs`
**Location:** `/Game/Scripts/RunTimeGame.cs`

**Purpose:** Main game state manager that persists across scenes.

**Key Features:**
- Manages player lists and names
- Tracks round progression (`round`)
- Tracks qualified players (`playersQualified`)
- Manages bot usage (`useBot`)
- Handles skin names
- Tracks non-qualified players
- Manages map names
- Persists using `DontDestroyOnLoad`

**Static Variables:**
- `playerObj` - List of player GameObjects
- `namePlayerObj` - List of player names
- `round` - Current round number
- `playersQualified` - Number of qualified players
- `useBot` - Whether bots are enabled
- `nameSkin` - Current skin name
- `nonQualifiedNames` - List of non-qualified player names
- `mapName` - List of map names
- `isLocalQualified` - Whether local player qualified

---

### Character System (Modular)

#### `CharacterController.cs` (Shared)
**Location:** `/Game/Scripts/Character/CharacterController.cs`

**Purpose:** Unified controller for both Player and AI agents. Replaces the old `Movement.cs`.

**Key Features:**
- **Unified Logic:** Handles physics, movement, jumps, dives, and slopes for ALL characters.
- **Input Agnostic:** Receives input commands (`SetInput`, `SetDive`) from external scripts (`Player.cs` or `AI.cs`).
- **Dependencies:**
  - `CharacterPhysics.cs` - Handles Raycasting, Ground checks, and Collision events.
  - `CharacterAnimator.cs` - Handles Animation state updates.
  - `Ragdoll.cs` - Handles ragdoll activation/deactivation.

#### `Player.cs`
**Location:** `/Game/Scripts/Player/Player.cs`
**Purpose:** Handles User Input (Joystick/Keyboard) and passes it to `CharacterController`.

#### `AI.cs`
**Location:** `/Game/Scripts/Character/AI.cs`
**Purpose:** Handles Goal/Target logic for bots and passes directional input to `CharacterController`.

#### `CharacterPhysics.cs`
**Location:** `/Game/Scripts/Character/CharacterPhysics.cs`
**Purpose:** Pure physics handling - Ground detection, Wall detection, and Collision callbacks.

---

### Deprecated / Old Scripts
**Location:** `/Game/Scripts/Old/`
- **`Movement.cs`**: Old monolithic player controller. Kept for reference.
- **`AIController.cs`**: Old monolithic AI controller. Kept for reference.

---

#### `Ragdoll.cs`
**Location:** `/Game/Scripts/Player/Ragdoll.cs`

**Purpose:** Manages ragdoll physics system for character.

**Key Features:**
- Enables/disables ragdoll physics
- Manages collider states
- Controls rigidbody constraints
- Integrates with Animator

**Key Methods:**
- `EnableRagdoll()` - Activates ragdoll physics
- `DisableRagdoll()` - Deactivates ragdoll, returns to animation

**Components:**
- `colBody[]` - Body colliders (enabled during ragdoll)
- `mainCols[]` - Main colliders (disabled during ragdoll)
- `rigidBody[]` - Rigidbody components for ragdoll parts
- `mainRb` - Main character rigidbody

---

#### `TargetPointsAI.cs`
**Location:** `/Game/Scripts/Player/AI/TargetPointsAI.cs`

**Purpose:** Waypoint system for AI navigation.

**Key Features:**
- Single or multiple target points
- Trigger-based target assignment
- Random target selection from multiple options
- Final target flag for end-of-level

**Usage:**
- Placed as trigger colliders in the level
- When AI bot enters trigger, assigns new target
- Supports branching paths with multiple targets

---

### Environment Systems

#### `Bounce.cs`
**Location:** `/Game/Scripts/Environment/Bounce.cs`

**Purpose:** Makes objects bounce players on collision.

**Key Features:**
- Configurable bounce force
- Activates ragdoll on collision
- Stun time configuration
- Debug logging

**Usage:**
- Attach to objects that should bounce players
- Configurable force and stun time

---

#### `Trampoline.cs`
**Location:** `/Game/Scripts/Environment/Trampoline.cs`

**Purpose:** Special bounce object with enhanced jump mechanics.

---

#### `Rotator.cs`
**Location:** `/Game/Scripts/Environment/Rotator.cs`

**Purpose:** Rotates objects continuously (obstacles).

---

#### `Pendulum2.cs`
**Location:** `/Game/Scripts/Environment/Pendulum2.cs`

**Purpose:** Pendulum obstacle that swings.

---

#### `MoveUpDown.cs`
**Location:** `/Game/Scripts/Environment/MoveUpDown.cs`

**Purpose:** Moves objects up and down (platforms).

---

#### `FanController.cs`
**Location:** `/Game/Scripts/Environment/FanController.cs`

**Purpose:** Fan that applies force to players.

---

### Camera System

#### `CameraLook.cs`
**Location:** `/Game/Scripts/CameraLook.cs`

**Purpose:** Third-person camera controller.

**Key Features:**
- **Input:**
  - Desktop: Mouse look
  - Mobile: Touch input via `LookAxis`
- **Camera Behavior:**
  - Follows player target
  - Smooth rotation
  - Configurable distance
  - Zoom functionality (scroll wheel)
  - Clamped vertical rotation
- **Target Management:**
  - Follows local player when qualified
  - Random player selection when not qualified
  - Auto-searches for player targets

**Key Methods:**
- `Look()` - Handles camera rotation and positioning
- `SearchTargetTransfrom()` - Finds and assigns camera target

**Dependencies:**
- `RunTimeGame` - For round/qualification state

---

## Game Systems

### Input System
- **Desktop:** Keyboard (WASD) + Mouse
- **Mobile:** Joystick Pack integration
- **Input Actions:** `InputSystem_Actions.inputactions`

### Physics
- Uses Unity's Rigidbody physics
- Custom physics materials in `/Game/GameAssets/PhysicMaterial/`
- Ragdoll physics for character interactions

### Networking
- Photon integration present but commented out
- Code structure suggests multiplayer support was planned/removed
- `PhotonView` references commented throughout codebase

### Animation
- Animator Controller: `/Game/AnimatorController/Anim.controller`
- Animation clips in `/Game/AnimationClip/`
- Parameters: Jump, Fall, Incline, GetUp, DiveStart, Emote, STOPALL

---

## Dependencies

### Unity Packages
- **TextMesh Pro** - UI text rendering
- **Input System** - New input system (`.inputactions` file present)
- **Photon** - Networking (partially integrated, commented out)

### Third-Party Assets
- **Joystick Pack** - Mobile joystick controls

---

## Key Game Mechanics

### Player Movement
1. **Ground Movement:** Physics-based with counter-movement for precision
2. **Jumping:** Force-based with cooldown system
3. **Diving:** Forward momentum boost, can be used in air
4. **Slope Handling:** Special movement on slopes up to 35° angle
5. **Ragdoll:** Activated when hit by other players or obstacles

### AI System
1. **Waypoint Following:** AI follows target points placed in level
2. **Obstacle Navigation:** Automatic jumping and diving
3. **Wall Avoidance:** Detects and navigates around walls

### Game Flow
1. **Round System:** Tracks rounds via `RunTimeGame.round`
2. **Qualification:** Players can qualify for next round
3. **Bot Support:** Can spawn AI bots (`RunTimeGame.useBot`)

---

## Scene Structure

### Main Scene
- **`GameScene.unity`** - Main game scene
  - Contains player spawns
  - Environment obstacles
  - Camera setup
  - UI elements

---

## Tags and Layers

### Tags
- `"Player"` - Player character
- `"Bot"` - AI bot
- `"TargetBot"` - AI target waypoint
- `"JoyStick"` - Joystick UI element

### Layers
- `"Player"` - Player layer
- `"Bot"` - Bot layer
- `"Trampoline"` - Trampoline objects

---

## Notes

### Commented Code
- Photon networking code is commented throughout
- Suggests multiplayer was planned or removed
- `PhotonView` and RPC calls are commented out

### Mobile Support
- Full mobile support with joystick controls
- Touch input for camera
- Frame rate capped at 60fps on mobile devices

### Physics
- Uses Unity's new physics API (`rb.linearVelocity` instead of `rb.velocity`)
- Suggests Unity 6 or newer physics system

---

## File Count Summary
- **C# Scripts:** 29 files
- **Unity Scenes:** 2 files
- **Meta Files:** 249+ files (Unity asset metadata)

---

*Last Updated: January 26, 2026*
