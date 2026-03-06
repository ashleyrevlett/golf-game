# Golf Game

> **Vision:** Bring the thrill of competitive golf to anyone with a phone.
>
> **Core value prop:** A mobile-browser golf game with async global leaderboards — pick up, play 6 shots, see how you rank. No install, no account required to play.

Closest-to-the-pin golf game built with Unity 6 for WebGL (mobile browser). 6 shots at 125 yards, lowest total distance to pin wins. Async leaderboard scoring via Unity Gaming Services.

## Stack

- **Engine:** Unity 6 (`6000.0.23f1`) — WebGL target
- **Language:** C# (.NET Standard 2.1)
- **UI:** UI Toolkit (screen-space); UGUI for world-space 3D only
- **Camera:** Cinemachine 3.x
- **Multiplayer:** Server-authoritative scoring (UGS Authentication, Leaderboards, Cloud Code)
- **Physics:** Unity built-in (Rigidbody, colliders)
- **Geometry:** Placeholder (cubes, cylinders, planes) — no player/club models

## Getting Started

### Prerequisites

- [Unity Hub](https://unity.com/download) with Unity **6000.0.23f1** installed
- Node.js 20+ (for Cloud Code tests)
- Git LFS (`git lfs install`)

### Clone and Open

```bash
git clone https://github.com/roborev26/golf-game.git
cd golf-game
git lfs pull
```

Open the project in Unity Hub (it will detect the correct Unity version from `ProjectSettings/ProjectVersion.txt`).

### Run

1. Open `Assets/Scenes/Gameplay.unity`
2. Enter Play mode
3. Press **Space** (desktop) or **tap** (mobile) to start the power meter

### Build WebGL Locally

1. File > Build Settings > WebGL
2. Set Build Name to `golf-game`
3. Build and Run

## Tests

```bash
# Cloud Code JS tests
node --test Assets/CloudCode/validate-and-post-score.test.js

# Unity EditMode tests — via Test Runner window
# Window > General > Test Runner > EditMode > Run All

# Unity PlayMode tests — via Test Runner window
# Window > General > Test Runner > PlayMode > Run All
```

**CI:** GitHub Actions runs Cloud Code tests, EditMode tests, PlayMode tests, and a WebGL build on every push/PR. See `.github/workflows/ci.yml`.

## Project Structure

```
Assets/
  Scripts/
    Core/          # Game managers, state machine, config
    Golf/          # Ball physics, shot mechanics, scoring
    Camera/        # Cinemachine setup, camera transitions
    UI/            # UI Toolkit documents, controllers
    Multiplayer/   # Leaderboard API, player auth, UGS services
    Environment/   # Course generation, placeholder geometry
    Audio/         # Audio manager, SFX
  Scenes/          # MainMenu, Gameplay
  UI/              # UXML and USS files
  CloudCode/       # Cloud Code JS scripts (server-side validation)
  Tests/           # EditMode and PlayMode tests
```

## Conventions

See [`CLAUDE.md`](CLAUDE.md) for coding conventions, reference wiring patterns, and pre-commit checklist.

## Deployment

Tag-based deployment to Cloudflare Pages. See [`docs/deployment.md`](docs/deployment.md).

```bash
# Preview
git tag -a v1.0.0-rc.1 -m "Release candidate 1" && git push --tags

# Production
git tag -a v1.0.0 -m "Initial release" && git push --tags
```
