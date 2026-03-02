---
issue: 4
title: Camera system (Cinemachine 3.x)
type: feature
tags: [camera, cinemachine, state-machine]
---

# Camera System

## What
Cinemachine 3.x camera rig with priority-based switching driven by shot state.

## Cameras
- **Tee**: Behind ball, 1.5m up, 3m back. FOV 60. Active during Ready.
- **Flight**: Right offset, tracks ball with damping. FOV 50. Active during Flying.
- **Landing**: Close-up, low angle. FOV 40. Active during Landed.
- **Reset**: Hard cut back to tee (reuses tee camera).

## Blends
- Tee->Flight: EaseInOut 0.5s
- Flight->Landing: EaseIn 0.3s
- Landing->Reset: Cut
- Reset->Tee: Cut

## Files
- `Assets/Scripts/Camera/CameraConfig.cs` — ScriptableObject config
- `Assets/Scripts/Camera/CameraController.cs` — State-driven switching

## Acceptance
- Camera transitions match shot phases
- No clipping through terrain
- Flight tracking feels dramatic
