# Ball Physics

## Model

Fixed timestep simulation with visual interpolation for smooth rendering.

### Ball States

| State | Description |
|-------|-------------|
| `Idle` | At rest on tee |
| `Flying` | Airborne |
| `Bouncing` | Hitting ground repeatedly |
| `Rolling` | Moving along ground |
| `Stopped` | Velocity below threshold (< 0.1 m/s) |

## Aerodynamics

- **Gravity** — standard
- **Drag** — coefficient ~0.25
- **Magnus force** — spin creates lift/curve

## Spin Effects

| Spin Type | Effect |
|-----------|--------|
| Backspin | Creates lift (Magnus), reduces roll ("check") |
| Sidespin | Curves ball in flight and during roll |

Spin decay: 0.98/frame in air, 0.6 on bounce.

## Ground Interaction

| Parameter | Value |
|-----------|-------|
| Bounce restitution | 0.5 |
| Friction | 0.4 |
| Rolling friction | 0.1 |

## Key Data Types

```csharp
// GolfGame.Golf.ShotParameters — created by ShotInput, consumed by BallController
public class ShotParameters
{
    public float PowerNormalized;    // 0–1 from power meter
    public float AimAngleDegrees;   // -45 to 45 (horizontal aim offset)
    public float BackspinRpm;       // higher = more lift, less roll
    public float SidespinRpm;       // negative = draw, positive = fade
    public float PowerMph(float maxMph); // computed from PowerNormalized
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

## Bounce SFX

`OnBallBounced(position, speed)` fires on each bounce. `OnBallLanded(position)` fires when the ball comes to rest. Bounce sound volume scales with impact speed; roll loop starts on first bounce and stops on landing.
