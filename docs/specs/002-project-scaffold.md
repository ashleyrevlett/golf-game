# Spec: Project Scaffold + Core Game Loop (#2)

## What
Unity 6 project structure with packages, assembly definitions, and two-tier state machine.

## Requirements
- AppManager: singleton, DontDestroyOnLoad, owns AppState transitions, fires OnAppStateChanged
- GameManager: gameplay scene, owns ShotState loop (Ready/Flying/Landed), 6-shot limit, fires OnGameOver
- SceneLoader: static async scene loading with progress events
- Assembly definitions: Core, Golf, Camera, UI, Multiplayer, Environment (Core has no game deps)
- Packages: Cinemachine 3.x, Input System, UI Toolkit
- Scenes: MainMenu (AppManager), Gameplay (GameManager + light)
- ProjectSettings: WebGL target, IL2CPP backend, New Input System

## Acceptance
- Project compiles in Unity 6
- State machine transitions work (verified by EditMode tests)
- Scenes load without errors
- All asmdef files resolve
- .meta files committed
