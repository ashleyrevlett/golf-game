# Golf Game Design Doc

## What We're Building

A first-person 3D "closest-to-the-pin" golf game for mobile web browsers (WebGL). Players get 6 shots at 125 yards. Lowest distance to the pin wins. Real-time multiplayer leaderboard. Built with Unity 6.

## Core Fantasy

"I can stick it close." The thrill of a well-struck iron shot landing near the pin. Precision over power.

## Approach Evaluation

### Option A: Full Unity Project with Physics-Based Swing
- Unity Rigidbody for ball, configurable force/angle/spin
- Touch-based swing meter (drag to set power/direction)
- Cinemachine camera rig with multiple virtual cameras
- UI Toolkit for all menus and HUD
- **Pros:** Realistic feel, Unity physics handles trajectory naturally
- **Cons:** WebGL performance on mobile needs careful optimization

### Option B: Trajectory Calculation (No Real-Time Physics)
- Pre-calculate trajectory from input parameters
- Animate ball along spline, no Rigidbody during flight
- Physics only for bounce/roll on landing
- **Pros:** Deterministic, lighter on mobile, easier to sync multiplayer
- **Cons:** Less emergent behavior, harder to feel "real"

### Option C: Hybrid - Physics Launch + Trajectory Prediction
- Use Rigidbody for actual ball physics
- Show trajectory preview line before shot
- Cinemachine follows the real physics object
- Optimize with fixed timestep and simple colliders
- **Pros:** Best of both worlds -- real physics feel, predictable enough for scoring
- **Cons:** Slightly more complex

### Decision: Option C (Hybrid)

Real physics for the ball flight gives the best game feel. The trajectory preview helps the player plan shots (informed decisions per Sid Meier's Rule). WebGL performance is manageable with simple geometry and fixed timestep. Landing physics (bounce, roll, friction) happen naturally.

## Architecture

### Game State Machine
```
MainMenu -> Gameplay -> GameOver -> MainMenu
                |
         Shot Cycle:
         Aiming -> Charging -> InFlight -> Landed -> (repeat or GameOver)
```

### Shot Mechanics
- **Aiming:** Touch/drag to set direction. Camera behind ball looking at pin.
- **Power:** Hold-and-release meter. Power determines initial velocity magnitude.
- **Angle:** Automatic loft based on club (fixed at ~30 degrees for a 9-iron at 125 yards). Player controls left/right aim.
- **Spin:** Optional -- slight hook/slice based on timing of release. Adds skill ceiling.
- **Physics:** Ball launched with Rigidbody.AddForce. Gravity + drag + wind (stretch goal). Bounce on terrain collider with physics material (restitution, friction).

### Camera System (Cinemachine 3.x)
Four CinemachineCamera objects:
1. **Tee Camera:** Behind ball, looking at course. Used during aiming.
2. **Flight Camera:** Follows ball from distance with orbital offset. Dramatic tracking.
3. **Landing Camera:** Zooms to ball on landing. Close-up of result.
4. **Reset Camera:** Quick cut back to tee. Blend = Cut, not smooth.

Priority-based switching via CinemachineBrain. State machine drives priority changes.

### UI (UI Toolkit)
All UI via UXML + USS. No UGUI components.

**Screens:**
- Main Menu: Play, Settings, Credits
- Settings: Sound volume, quality toggle
- HUD: Shot count, best distance, shot stats, mini-leaderboard
- Game Over: Final score, leaderboard position, exit button

### Multiplayer
- External leaderboard API (interface-based, mockable)
- POST score after each shot lands
- GET leaderboard periodically during gameplay
- Auth via bearer token (interface-based, mockable)
- Server-authoritative: client sends shot params, server validates distance

### Course Layout
Single hole, 125 yards:
- Flat tee box (plane)
- Fairway (elongated plane, green material)
- Green (circular plane, lighter green)
- Pin (cylinder + sphere flag)
- Out-of-bounds markers (posts/ropes)
- Basic rough areas (darker planes, higher friction)
- All placeholder geometry -- cubes, planes, cylinders

## Technical Constraints

- **WebGL:** No threading, limited memory. Keep draw calls low. Use GPU instancing.
- **Mobile browser:** Touch input only. No right-click. Handle screen rotation.
- **Unity 6:** Use IL2CPP backend for WebGL. Addressables for asset loading.
- **Bundle size:** Target < 20MB initial download. Compress textures aggressively.

## Known Risks

1. **Unity not installed on dev machine.** Project files will be created but cannot be compiled/tested locally. Unity 6 installation required to verify.
2. **WebGL mobile performance.** Unity WebGL on mobile is improving (6.3 LTS adds multi-threading) but still requires testing on real devices.
3. **Physics determinism.** Rigidbody physics may vary slightly across platforms. For multiplayer fairness, server should be authority on scoring, not client physics.
