# Cinemachine Camera + Obstacle Avoidance

Replace the regular camera with Cinemachine and add obstacle avoidance so the camera moves when something blocks the view (or collides with the camera).

---

## 1. Add Cinemachine Brain to Main Camera

- Select your **Main Camera** in the scene.
- **Add Component** → search **Cinemachine Brain**.
- Leave default settings (the Brain drives the Main Camera using the active Virtual Camera).

---

## 2. Create a Virtual Camera (Follow Player)

- **GameObject → Cinemachine → Cinemachine Camera** (or create an empty GameObject and add **Cinemachine Camera**).
- Name it **CM vcam Player** (so the setup script can find it by name).

**Where Follow / Look At are (Cinemachine 3):** On the **Cinemachine Camera** component, expand **Target**. **Tracking Target** = Follow; **Look At Target** = Look At. You can leave both empty and assign at runtime (section 6).

**Cinemachine Camera (Inspector):**

- **Follow**: Drag your **player’s body transform** (e.g. the same transform your current `CameraLook` uses — e.g. `Player → child 0`).
- **Look At**: Same as Follow (or a separate look-at target if you prefer).
- **Priority**: e.g. `10` (higher than other vcams if you add more later).

**Body (camera must have a Follow component to move):** A bare Cinemachine Camera does not follow. Choose one:

- **Cinemachine Follow** — fixed offset behind target. Add component, set **Follow Offset** (e.g. 0, 2, -6). Set **Tracked Object Offset** (or equivalent) and **Damping** as needed. No mouse/touch orbit.
- **Cinemachine Orbital Follow** (for look-around) — camera orbits around the target. Set **Radius** (e.g. 6.5). Set **Vertical Axis → Range** (e.g. -40 to 40) to match your pitch limits. Exposes **Horizontal Axis** and **Vertical Axis** (Look Orbit X / Look Orbit Y) for rotation input.

**Aim:**

- Use **Composer** or **Follow Offset** so the camera looks at the Look At target. Default is usually fine.

This replaces the “follow + distance” behaviour of your current `CameraLook`.

---

## 2b. Camera rotation (mouse / touch) with Orbital Follow

If you use **Cinemachine Orbital Follow** and want the player to rotate the camera with mouse or touch:

1. **Input Axis Controller** — On **CM vcam Player**, add **Cinemachine Input Axis Controller**. It discovers the Orbital Follow axes (Look Orbit X, Look Orbit Y). For each controller, set **Legacy Input** to **Mouse X** and **Mouse Y**. Enable **Cancel Delta Time** if the driver uses delta. Use **Legacy Gain** (e.g. 1 or 0.25) to tune sensitivity.
2. **Custom input for touch** — Add **CinemachineCameraInputProvider** to a persistent GameObject (e.g. Main Camera or same as **CinemachineCameraSetup**). Assign **Touch Look Field** to your look **FixedTouchField** (same as used by `PlayerInput`). Set **Touch Sensitivity** (e.g. 0.25) to match your previous feel. This sets `CinemachineCore.GetInputAxis` so "Mouse X" / "Mouse Y" come from touch when dragging the look area, otherwise from legacy `Input.GetAxis` (mouse).
3. **Disable CameraLook** — Once Cinemachine drives the camera, disable (or remove) **CameraLook**. Keep **PlayerInput** and the **FixedTouchField**; the provider reads from the touch field for Cinemachine.

---

## 3. Avoid Obstacles (Something Colliding / Blocking the Camera)

Use one (or both) of these on the **same** Cinemachine Camera GameObject:

### Option A – Keep line of sight (recommended for “blocking view”)

- Add component: **Cinemachine Deoccluder**.
- It keeps the **line of sight** between camera and **Look At** target clear:
  - When something (e.g. wall, pillar) is between camera and player, it **moves the camera forward** (or to an alternate position) so the player stays visible.
- In the Deoccluder:
  - **Strategy**: e.g. **Preserve Camera Height** or **Preserve Camera Distance** (try and pick what feels best).
  - **Obstacle Layers**: set to the layers your **obstacles** use (e.g. Default, Environment).
  - **Minimum Distance From Target**: so the camera doesn’t go inside the player (e.g. 1–2).
  - **Damping**: smoothness when moving away from obstacles.

### Option B – Keep camera out of geometry

- Add component: **Cinemachine Decollider**.
- It **stops the camera from going inside colliders** (e.g. terrain, walls).
- Use **Obstacle Layers** for walls/terrain.
- **Camera Radius**: small value (e.g. 0.2) so the camera is pushed out of obstacles.

Use **Deoccluder** when you care about “nothing blocking the view”; use **Decollider** when you care about “camera not inside walls/terrain”. You can use both.

---

## 4. Stop Using the Old Camera Script

- **Disable** (or remove) **CameraLook** on the Main Camera (or wherever it is).
- The Main Camera is now driven by **Cinemachine Brain** + your **Cinemachine Camera**; you don’t need to set `transform.position` / `transform.rotation` manually.

---

## 5. Keep Your Gameplay Code Using “Main Camera”

Your code uses `Camera.main` (e.g. in `Player.cs`, `Movement.cs`) for orientation. That’s fine:

- **Camera.main** still points to your Main Camera.
- Cinemachine Brain **moves** that camera; so **Camera.main.transform** is now the Cinemachine-driven position/rotation.
- No need to change `Camera.main` in scripts unless you switch to a different camera.

---

## 6. Runtime target (player spawned after game start)

If the player is spawned at runtime and not in the scene at edit time:

- Add **CinemachineCameraSetup** to a GameObject in the scene (e.g. a “GameManager” or the Main Camera).
- Leave **Virtual Camera** empty; the script finds **CM vcam Player** by name. Enable **Auto Find Player When Null** so when the player spawns with tag **Player**, the camera attaches. Or call **SetPlayerTarget(playerBodyTransform)** when you spawn the player.
- In code when the player spawns, call:
  - `GetComponent<CinemachineCameraSetup>().SetTarget(playerBodyTransform);`
- Or set **Follow** / **Look At** on the `CinemachineCamera` component directly in code.

---

## Apply all settings from the Editor (one click)

**Option A – Like your old camera (recommended)**

Camera **behind** the player (third-person), fixed offset. Touch/mouse **only changes look direction** (yaw/pitch). For **silhouette** (player visible through walls): camera **stays** when player hides behind an object; when character moves ahead the camera **smoothly** follows (no jerk). Camera position comes from Cinemachine when **Use Cinemachine For Position** is on.

1. Create a **Cinemachine Camera** and name it **CM vcam Player** (if you don’t have it yet).
2. Menu: **Game → Cinemachine → Apply All Player VCam Settings (Like Old Camera)**.

This will:

- Use **Cinemachine Follow** with **Follow Offset (0, 2, -6.5)** and **smooth position damping** – camera behind the player, smooth follow when character moves ahead.
- Add **Cinemachine Deoccluder** and **Cinemachine Decollider** on the vcam with **Avoid Obstacles** and **Decollision OFF** – camera does **not** move when player hides behind an object (use silhouette to see player); camera does **not** damp/push toward player when colliding with geometry.
- (Optional: to move camera only after occlusion lasts a while, enable **Avoid Obstacles** on Deoccluder and set **Minimum Occlusion Time**.)
- Add **CinemachineLookFromInput** with **Use Cinemachine For Position = true**: position from Cinemachine (Follow only; smooth damping), rotation only from touch/mouse.


Then **disable CameraLook**. **CinemachineCameraSetup** finds **CM vcam Player** by name and sets the target when the player spawns.

---

**Option A2 – Fall Guys style (orbit, player always in center)**

Player **always in the middle of the screen**. Camera **orbits 360° horizontally** around the player and **clamped vertically** (pitch limits). Drag/touch or mouse rotates the camera around the player; the camera always looks at the player.

1. Create a **Cinemachine Camera** and name it **CM vcam Player** (if you don’t have it yet).
2. Menu: **Game → Cinemachine → Apply Fall Guys Style (Orbit, Player Center)**.

This will:

- Use **Cinemachine Follow** on the vcam (for target reference). **CinemachineLookFromInput** controls position and rotation: camera **orbits** around the player (offset 0, 2, -6.5), **yaw -180 to 180** (full 360° horizontal), **pitch -40 to 40** (clamped vertical), and **looks at the player** so the player stays centered.
- **Use Cinemachine For Position** is **OFF** so the script sets orbit position and look-at.

Then **disable CameraLook**. **CinemachineCameraSetup** finds **CM vcam Player** by name and sets the target when the player spawns.

---

**Option B – Orbital (camera orbits when you drag)**

1. Menu: **Game → Cinemachine → Apply All Player VCam Settings (Orbital + Mouse Drag)**.
2. Camera **orbits** around the player when you drag (position changes on a sphere). Uses Orbital Follow + Input Axis Controller + CinemachineCameraInputProvider.

---

## Quick checklist

| Step | What to do |
|------|------------|
| 1 | Main Camera: add **Cinemachine Brain** |
| 2 | Create **Cinemachine Camera** named **CM vcam Player**; expand **Target** → leave **Tracking Target** / **Look At Target** empty for runtime; add **Cinemachine Orbital Follow** (Radius e.g. 6.5, Vertical Axis Range e.g. -40 to 40) or **Cinemachine Follow** (Follow Offset e.g. 0, 2, -6) |
| 3 | For orbit rotation: on **CM vcam Player** add **Cinemachine Input Axis Controller** (Legacy Input: Mouse X / Mouse Y, Cancel Delta Time on); add **CinemachineCameraInputProvider** to Main Camera (or same as setup), assign **Touch Look Field**, set **Touch Sensitivity** (e.g. 0.25) |
| 4 | On same vcam: add **Cinemachine Deoccluder** (obstacle avoidance) and/or **Cinemachine Decollider** |
| 5 | Add **CinemachineCameraSetup** to a GameObject; leave Virtual Camera empty; enable **Auto Find Player When Null** (or call **SetPlayerTarget** when player spawns) |
| 6 | Disable **CameraLook** (or remove it) |
| 7 | Keep using **Camera.main** in your scripts |

Obstacles must have **Colliders** and be on layers included in the Deoccluder/Decollider **Obstacle Layers** for avoidance to work.

---

## Your current setup (Inspector)

- **Tracking Target: None** — Leave it empty; **CinemachineCameraSetup** will assign the player at runtime (auto-find or **SetPlayerTarget**).
- **Position Control: None** — Add **Cinemachine Follow** (Add Component on the same GameObject) so the camera actually moves; set **Follow Offset** (e.g. 0, 2, -6).
- **Rotation Control: None** — Add an Aim component (e.g. **Cinemachine Rotation Composer**) if you want the camera to look at the target; or use **Cinemachine Follow** which can handle both position and rotation.
- **Add Extension** — Use this to add **Cinemachine Deoccluder** (obstacle / line-of-sight) or **Cinemachine Decollider** (camera out of geometry).

---

## References

- **Cinemachine (YouTube):** [https://www.youtube.com/watch?v=u0a1F6BlczE](https://www.youtube.com/watch?v=u0a1F6BlczE)
- **Cinemachine 3.1 Manual:** [Unity Docs – Cinemachine 3.1](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html)
- **Upgrade from Cinemachine 2 to 3:** [Unity Docs – Cinemachine Upgrade](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/manual/CinemachineUpgradeFrom2.html)
