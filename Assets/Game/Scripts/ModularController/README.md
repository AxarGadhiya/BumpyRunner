# Modular Controller (Player + AI)

Three scripts that split character control so **one shared controller** drives physics for both **player** and **enemy**.  
**Movement.cs** and **AIController.cs** are unchanged; use these when you want to test the modular setup.

---

## Scripts

| Script | Purpose |
|--------|--------|
| **CharacterPhysics.cs** | Rigidbody and collision only: grounding, floor normal, hit-from-player detection. Fires `OnHitByPlayer` and `OnLanded`. |
| **CharacterController.cs** → `SharedCharacterController` | Movement, jump, dive, slopes, ragdoll. Uses CharacterPhysics for Rb, Grounded, NormalVector and subscribes to its events. |
| **CharacterAnimator.cs** | Wraps Animator only; other scripts call its setters (SetJump, SetFall, SetHorizontal, etc.). |
| **Player.cs** | Player input, camera-relative movement, jump/dive from input, animator (vertical/horizontal, GetUp, etc.). |
| **AI.cs** | Target following, gap/wall detection, jump and jump-dive, wall avoidance. |

---

## Setup

### Player

1. Add **CharacterPhysics** to the player GameObject. Assign **Rigidbody** and **What Is Ground** layer mask.
2. Add **SharedCharacterController** (from `CharacterController.cs`) to the same GameObject. Assign **Character Physics**, **Rigidbody** (or leave empty to use physics’ Rb), **Animator**, **Ragdoll**. Leave **Movement Orientation** empty (Player sets it to the camera).
3. Add **Player** to the same GameObject.
4. Assign on **Player**: **Character Controller**, **Player Input**, **Animator**, and **Fixed Joystick** (optional; will use tag `JoyStick` if not set).
5. Remove or disable **Movement** on this GameObject when testing the modular setup.

### Enemy / Bot

1. Add **CharacterPhysics** to the bot GameObject. Assign **Rigidbody** and **What Is Ground**.
2. Add **SharedCharacterController** to the same GameObject. Assign **Character Physics**, **Rigidbody**, **Animator**, **Ragdoll**. Leave **Movement Orientation** empty (AI uses transform).
3. Add **AI** to the same GameObject.
4. Assign on **AI**: **Character Controller**, **Sensor Raycast** (optional; uses child index 3 if not set). Set **Target** or leave empty to use object with tag `TargetBot`.
5. Ensure the bot is on the **Bot** layer so waypoint triggers (TargetPointsAI) work.  
   **TargetPointsAI** has been updated to set `target` on both `AIController` and `AI`, so existing waypoints work for the modular AI.
6. Remove or disable **AIController** on this GameObject when testing the modular setup.

---

## Behaviour flags (SharedCharacterController)

- **Use Counter Movement**: `true` for player (smoother stop), `false` for AI. Set automatically by Player/AI in `Start()`.
- **Use Wall Avoidance**: `false` for player, `true` for AI (adds sideways force when hitting walls). Set automatically by Player/AI in `Start()`.

---

## Notes

- The shared controller does **not** read input; **Player** and **AI** call `SetInput(x, y)` and `SetDive(...)` each frame.
- For the player, orientation is the **camera**; for AI it is the **transform** (facing the target).
- Existing **TargetPointsAI** triggers now assign `target` to either `AIController` or `AI`, so you can keep using the same waypoints for modular bots.

**Single path (no double run):** Physics (collision, grounding) runs only in **CharacterPhysics**. Animation is driven only by **CharacterAnimator** when assigned, otherwise by the Animator directly — Controller, Player, and AI never drive the Animator from two places at once.
