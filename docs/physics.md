# Ball Physics

## Model

Unity built-in physics (Rigidbody, colliders). Ball uses `ForceMode.Impulse` at launch and Unity's gravity/collision system for flight and bouncing. No custom aerodynamics simulation is active — drag, spin, and Magnus force are configured in `BallPhysicsConfig` but not yet applied by `BallController`.

## Ball State

`BallController` uses a single `isFlying` boolean:

| State | Condition |
|-------|-----------|
| Resting | `isFlying == false`, Rigidbody is kinematic |
| Flying | `isFlying == true`, Rigidbody is dynamic (gravity + collisions active) |

The ball transitions from Resting → Flying on `Launch()`, and from Flying → Resting when velocity and angular velocity drop below thresholds after a minimum flight time (0.5s).

## Launch Sequence

```
BallController.Launch(ShotParameters shot)
  1. Compute aim direction from shot.AimAngleDegrees (horizontal rotation)
  2. Apply loft angle (from BallPhysicsConfig.DefaultLoftAngle, default 30°)
  3. Calculate force = LaunchForce * shot.PowerNormalized
  4. Set isFlying = true, switch Rigidbody to dynamic
  5. Apply impulse force in launch direction
  6. Fire OnBallLaunched event
```

`LaunchForce = MaxPowerMph * MphToForceMultiplier` (from `BallPhysicsConfig`).

> **Note:** `ShotParameters.BackspinRpm` and `SidespinRpm` exist as data fields but `BallController` does not read them. Spin has no gameplay effect currently.

## Stop Detection

In `FixedUpdate`, after a 0.5s grace period:

- Linear velocity < 0.2 m/s **and** angular velocity < 0.1 rad/s → ball stops
- On stop: zero velocities, set kinematic, fire `OnBallLanded(position)`

## Events

| Event | Signature | When |
|-------|-----------|------|
| `OnBallLaunched` | `Action` | After impulse applied |
| `OnBallBounced` | `Action<Vector3, float>` | On collision while flying (position, speed) |
| `OnBallLanded` | `Action<Vector3>` | When ball comes to rest (position) |

## Configuration: BallPhysicsConfig

ScriptableObject at `Resources/BallPhysicsConfig`. All values are tunable in the Inspector.

### Active Parameters (used by BallController)

| Parameter | Default | Used In |
|-----------|---------|---------|
| `DefaultLoftAngle` | 30° | Launch direction |
| `MaxPowerMph` | 150 | Launch force calculation |
| `MphToForceMultiplier` | 0.6 | Launch force calculation |
| `BallMass` | 0.046 kg | Rigidbody mass |

### Configured but Inactive

These exist in `BallPhysicsConfig` but are **not consumed** by `BallController`:

| Parameter | Default | Intended Use |
|-----------|---------|--------------|
| `DragCoefficient` | 0.25 | Air resistance |
| `MagnusCoefficient` | 0.0001 | Spin-induced lift/curve |
| `SpinDecayAir` | 0.98/frame | Spin reduction in flight |
| `SpinDecayBounce` | 0.6 | Spin reduction on bounce |
| `BounceRestitution` | 0.5 | Bounce energy retention |
| `Friction` | 0.4 | Ground friction |
| `RollingFriction` | 0.1 | Rolling deceleration |
| `StopVelocityThreshold` | 0.1 m/s | Stop detection (BallController hardcodes 0.2) |
| `StopAngularThreshold` | 0.05 rad/s | Stop detection (BallController hardcodes 0.1) |
| `StopConsecutiveFrames` | 10 | Multi-frame stop check (not implemented) |
| `FlightTimeout` | 30s | Max flight duration (not implemented) |
| `WindMinSpeed` | 0 | Wind system range |
| `WindMaxSpeed` | 8 | Wind system range |
| `BallRadius` | 0.02135 m | Ball dimensions |

## Key Data Types

```csharp
// GolfGame.Golf.ShotParameters — created by ShotInput, consumed by BallController
public class ShotParameters
{
    public float PowerNormalized { get; set; }    // 0-1 from power meter
    public float AimAngleDegrees { get; set; }   // -45 to 45 (horizontal aim offset)
    public float BackspinRpm { get; set; }        // exists but unused by BallController
    public float SidespinRpm { get; set; }        // exists but unused by BallController
    public float PowerMph(float maxMph);          // computed from PowerNormalized
}

// GolfGame.Environment.ShotResult — per-shot statistics
public struct ShotResult
{
    public int ShotNumber;          // 1-based
    public float DistanceToPin;     // meters from landing to pin
    public float CarryDistance;     // tee to first bounce (meters)
    public float TotalDistance;     // tee to final rest (meters)
    public float LateralDeviation;  // positive = right (meters)
    public float BallSpeed;        // launch speed (m/s)
}
```
