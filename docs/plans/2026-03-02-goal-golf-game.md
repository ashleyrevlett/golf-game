# Goal: Build Closest-to-the-Pin Golf Game

## Milestones

### M1: Unity Project Scaffold + Core Game Loop
Set up Unity 6 project structure, packages (Cinemachine 3.x, UI Toolkit), assembly definitions, and the core game state machine (MainMenu -> Gameplay -> GameOver). Deliver a running project with state transitions and placeholder scenes. Testable: scenes load, state machine transitions work, project compiles in Unity 6.

### M2: Ball Physics + Shot Mechanics
Implement the shot cycle: aiming (touch input for direction), power meter (hold-and-release), ball launch via Rigidbody.AddForce, flight physics (gravity, drag), and landing (bounce, roll, stop detection). Includes course terrain colliders and physics materials. Testable: player can aim, charge, and hit a ball that flies realistically and lands on the course.

### M3: Camera System (Cinemachine 3.x)
Set up four CinemachineCamera objects (tee, flight, landing, reset) with priority-based switching driven by the shot state machine. Flight camera follows ball with orbital tracking. Landing camera zooms in. Reset cuts back to tee. Testable: camera transitions match shot phases, flight tracking feels dramatic.

### M4: Course Environment + Scoring
Build the 125-yard hole with placeholder geometry: tee box, fairway, rough, green, pin, OB markers. Implement distance-to-pin calculation, best-distance tracking across 6 shots, and shot statistics (distance, carry, curve, ball speed). Testable: distance measurement is accurate, stats display correctly, game ends after 6 shots with correct best distance.

### M5: UI System (UI Toolkit)
Build all UI screens with UXML/USS: main menu, settings, gameplay HUD (shot count, best distance, shot stats, mini-leaderboard), and game over screen. Wire UI to game state and scoring systems. Testable: all screens render, data updates live during gameplay, touch-friendly on mobile viewport.

### M6: Multiplayer + Leaderboard Integration
Implement API interfaces for authentication (token-based) and leaderboard (POST score, GET rankings). Build mock implementations. Wire mini-leaderboard to display top 3 + player position during gameplay. Server-authoritative scoring flow. Testable: mock leaderboard updates after each shot, mini-leaderboard shows correct rankings.

### M7: Polish + WebGL Optimization
WebGL build configuration (IL2CPP, compression, memory settings). Touch input refinement for mobile browsers. Performance profiling and optimization (draw calls, texture compression, fixed timestep tuning). Final integration testing of all systems. Testable: game loads in mobile browser under 20MB, runs at stable framerate, full gameplay loop works end-to-end.

## Decision Log
- **Physics approach:** Hybrid (real Rigidbody physics + trajectory preview). Real physics for feel, server authority for scoring.
- **Camera:** Cinemachine 3.x with 4 virtual cameras, priority-based switching. Dramatic flight tracking.
- **UI:** UI Toolkit only per requirements. UXML for layout, USS for styling.
- **Multiplayer:** Interface-based API design with mock implementations. Decoupled from gameplay.
- **No Unity MCP available.** Proceeding with file-based project creation. All C# scripts and project configuration will be valid Unity 6 code. Requires Unity 6 installation to compile and test.
- **No player/club models.** Ball, course geometry, and pin only. All placeholder shapes.

## Status: In Progress
