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
public class ShotParameters
{
    public float powerMph;           // 0–200
    public float launchAngleDegrees; // 0–60
    public float sideAngleDegrees;   // -45 to 45
    public float backspinRpm;        // 0–10000
    public float sidespinRpm;        // -5000 to 5000
}

public struct ShotResult
{
    public int shotNumber;  // 1–6
    public int shotScore;   // Points this shot
    public int totalScore;  // Cumulative
}
```

## Bounce SFX

`OnGroundHit` fires on first contact and final landing. Intermediate bounces only fire if scaled apex exceeds `ballVisualRadius * 1.5` — prevents audible SFX for imperceptible bounces.
