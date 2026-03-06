# Golf Game

> **Core value prop:** A mobile-browser golf game with async global leaderboards — pick up, play 6 shots, see how you rank. No install, no account required to play.

Closest-to-the-pin golf game built with Unity 6 for WebGL (mobile browser). 6 shots at 125 yards, lowest total distance to pin wins. Async leaderboard scoring via Unity Gaming Services.

## Stack

- **Engine:** Unity 6.3 LTS (WebGL target)
- **Language:** C# (.NET Standard 2.1)
- **UI:** UI Toolkit (screen-space)
- **Camera:** Cinemachine 3.x
- **Multiplayer:** Server-authoritative scoring (UGS Authentication, Leaderboards, Cloud Code)
- **Physics:** Unity built-in (Rigidbody, colliders)
- **Geometry:** Placeholder (cubes, cylinders, planes) -- no player/club models

## Running

1. Open project in Unity 6.3 LTS
2. Open `Scenes/Gameplay.unity`
3. Enter Play mode

## Tests

- **Cloud Code JS tests:** `node --test Assets/CloudCode/validate-and-post-score.test.js`
- **Unity edit-mode tests:** Window > General > Test Runner (requires Unity Editor)
- **CI:** GitHub Actions runs JS tests on every PR; GameCI edit-mode tests when `UNITY_LICENSE` secret is configured
