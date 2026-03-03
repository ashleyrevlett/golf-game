# Goal: Build Closest-to-the-Pin Golf Game

## Milestones

### M1: Unity Project Scaffold + Core Game Loop (Issue #2) -- DONE
Set up Unity 6 project structure, packages, assembly definitions, and core game state machine.
- **Status**: Done (PR #12 merged)
- **Dependencies**: None

### M2: Ball Physics + Shot Mechanics (Issue #3) -- DONE
Implement the shot cycle: aiming, power meter, ball launch, flight physics, and landing.
- **Status**: Done (PR #13 merged)
- **Dependencies**: M1

### M3: Camera System (Cinemachine 3.x) (Issue #4) -- DONE
Cinemachine 3.x camera rig with priority-based switching driven by shot state.
- **Status**: Done (PR #14 merged)
- **Dependencies**: M1

### M4: Course Environment + Scoring (Issue #5) -- DONE
125-yard hole with placeholder geometry and scoring system.
- **Status**: Done (PR #15 merged)
- **Dependencies**: M2

### M5: UI System (UI Toolkit) (Issue #6) -- DONE
All UI screens: main menu, settings, gameplay HUD, game over.
- **Status**: Done (PR #16 merged)
- **Dependencies**: M1, M4

### M6: Multiplayer + Leaderboard Integration (Issue #7) -- DONE
Interface-based auth and leaderboard services with mock implementations.
- **Status**: Done (PR #17 merged)
- **Dependencies**: M4, M5

### M7: Audio System (Issue #11) -- DONE
Centralized audio with pooled AudioSources, ball SFX, ambient audio, UI sounds.
- **Status**: Done (PR #18 merged)
- **Dependencies**: M2, M5

### M8: Polish + WebGL Optimization (Issue #8) -- DONE
Bootstrap, WebGL template, touch input blocking, performance config.
- **Status**: Done (PR #19 merged)
- **Dependencies**: All previous milestones

## Execution Order

1. M1 (#2) -- foundation
2. M2 (#3) + M3 (#4) -- parallel
3. M4 (#5) -- needs M2
4. M5 (#6) -- needs M4
5. M6 (#7) + M7 (#11) -- parallel
6. M8 (#8) -- final polish

## Decision Log
- **Physics approach:** Hybrid (real Rigidbody physics + trajectory preview). Real physics for feel, server authority for scoring.
- **Camera:** Cinemachine 3.x with 4 virtual cameras, priority-based switching.
- **UI:** UI Toolkit only. UXML for layout, USS for styling.
- **Multiplayer:** Interface-based API design with mock implementations. ServiceLocator for DI.
- **Audio:** Pooled AudioSources, nullable clips for silent operation without assets. WebGL autoplay handled.
- **No player/club models.** Ball, course geometry, and pin only. All placeholder shapes.

## Status: Complete

### Final State
All 8 milestones delivered. The game has:
- Core game loop with 6-shot state machine
- Ball physics with spin, drag, and wind effects
- Cinemachine 3.x camera system with shot-phase transitions
- 125-yard course with placeholder geometry, pin, and scoring
- Full UI Toolkit interface (menu, settings, HUD, game over)
- Mock multiplayer with in-memory leaderboard
- Audio system ready for clips (runs silently without assets)
- WebGL template with mobile touch handling

### To run
Open project in Unity 6.3 LTS, open Gameplay scene, enter Play mode.

### Known limitations
- No actual audio assets — game is silent until .wav/.ogg files added
- No CI workflow — needs UNITY_LICENSE secret
- Build Profile must be configured manually in Unity Editor
- No real API backend — mock services only
- Performance not profiled on real mobile devices

### Metrics
- Milestones: 8 planned, 8 completed, 0 added, 0 removed
- PRs: #12, #13, #14, #15, #16, #17, #18, #19
- Issues: #2, #3, #4, #5, #6, #7, #8, #11
