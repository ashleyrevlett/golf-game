# Spec: Ball Physics + Shot Mechanics (#3)

## What
Complete shot cycle with physics-based ball flight, wind, spin effects, and stop detection.

## Requirements
- BallController: Rigidbody-based flight with custom drag, Magnus force (spin), wind
- BallPhysicsConfig: ScriptableObject for all tunable parameters
- ShotInput: touch/mouse aim (drag) + power meter (hold-release) -> ShotParameters
- WindSystem: random wind per shot, horizontal force during flight
- BallPhysics: static math utilities (launch velocity, drag, Magnus, spin decay)
- Integration with GameManager shot state machine
- Stop detection: velocity + angular velocity below threshold for N consecutive frames
- Flight timeout failsafe (30s)
- Ball reset to tee after landing

## Acceptance
- Aim, charge, launch produces realistic ball arc
- Wind varies per shot and visibly affects trajectory
- Spin creates lift (backspin) and curve (sidespin)
- Ball bounces and rolls to stop
- Stop triggers state transition via GameManager
- Physics config tweakable via ScriptableObject
