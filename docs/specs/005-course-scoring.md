---
issue: 5
title: Course environment + scoring
type: feature
tags: [course, scoring, environment, pin, wind-indicator]
---

# Course Environment + Scoring

## What
125-yard hole with placeholder geometry and shot scoring system.

## Course Layout
- Tee box (4m flat plane), fairway (18m wide x 114m), rough (9m each side)
- Green (14m radius circle) with pin (2.5m pole + red flag)
- OB markers along both edges
- 8 shared materials, static batched for <50 draw calls

## Scoring
- Distance-to-pin: flat Vector3.Distance ignoring Y
- Best distance tracked across 6 shots
- Per-shot stats: distance, carry, lateral deviation, ball speed
- Events: OnShotScored, OnBestDistanceUpdated, OnGameComplete

## Wind Indicator
- 3D arrow near tee, rotates to wind direction, scales with speed
- Subscribes to WindSystem.OnWindChanged

## Files
- `Assets/Scripts/Environment/CourseConfig.cs`
- `Assets/Scripts/Environment/CourseBuilder.cs`
- `Assets/Scripts/Environment/PinController.cs`
- `Assets/Scripts/Environment/ScoringManager.cs`
- `Assets/Scripts/Environment/ShotResult.cs`
- `Assets/Scripts/Environment/WindIndicator.cs`
