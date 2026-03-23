# Technology Stack

**Analysis Date:** 2026-03-22

## Languages

**Primary:**
- C# (.NET Standard 2.1) - Core game logic, UI controllers, services

**Secondary:**
- JavaScript (Node.js 20+) - Cloud Code server-side validation scripts

## Runtime

**Environment:**
- Unity 6 LTS (`6000.0.23f1` - WebGL target)
- WebGL Emscripten (32MB initial memory, up to 2GB max)
- IL2CPP scripting backend with managed stripping level 1

**Package Manager:**
- NPM / Node.js 20+ - For Cloud Code testing and deployment tooling
- Unity Package Manager - For engine packages and custom assemblies

**Lockfile:**
- Git LFS for binary assets
- `Packages/manifest.json` - Unity package dependencies

## Frameworks

**Core Engine:**
- Unity 6 LTS - Game engine and rendering

**UI & Input:**
- UI Toolkit - Screen-space UI (menu, game over, leaderboard panels)
- UGUI (uGUI) 2.0.0 - World-space 3D UI only (in-game score markers)
- Input System 1.11.2 - Keyboard and touch input

**Camera & Motion:**
- Cinemachine 3.1.3 - Camera management and transitions

**Testing:**
- Unity Test Framework 1.4.5 - EditMode and PlayMode tests
- Node.js `--test` - Cloud Code validation tests

**Build & Deployment:**
- GameCI `unity-builder` action (CI/CD)
- Wrangler (npm package) - Cloudflare Pages deployment
- Git LFS - Binary asset versioning

## Key Dependencies

**Critical:**
- `com.unity.services.authentication` (3.3.3) - Anonymous sign-in, player identity, access tokens
- `com.unity.services.cloudcode` (2.10.2) - Server-side validation via endpoint calls
- `com.unity.services.leaderboards` (2.1.0) - Reading/writing closest-to-pin scores
- `com.unity.nuget.newtonsoft-json` (3.2.2) - JSON serialization for UGS responses

**Infrastructure:**
- `com.unity.modules.physics` (1.0.0) - Rigidbody, colliders, physics simulation
- `com.unity.modules.ui` (1.0.0) - UGUI world-space elements
- `com.unity.modules.uielements` (1.0.0) - UI Toolkit runtime
- `com.unity.modules.unitywebrequest` (1.0.0) - HTTP requests (UGS backend)
- `com.unity.modules.jsonserialize` (1.0.0) - JSON parsing
- `com.unity.modules.animation` (1.0.0) - Animation playback
- `com.unity.modules.audio` (1.0.0) - Audio playback
- `com.unity.modules.imageconversion` (1.0.0) - Texture loading

**Development:**
- `com.unity.ide.rider` (3.0.34) - JetBrains Rider integration
- `com.unity.ide.visualstudio` (2.0.22) - Visual Studio integration

## Configuration

**Environment:**
- **Multiplayer:** Unity Gaming Services (UGS) project credentials via `UnityServices.InitializeAsync()`
  - Project ID: `9e06cda0-e65a-42bd-b45f-9f58add1bfda` (in ProjectSettings)
  - Organization ID: `ar-87c9d102-c74f-4720-86c1-d0ea2088888e`
- **Cloud Services:** Enabled in UGS dashboard (Authentication, Leaderboards, Cloud Code)
- **WebGL Memory:** 32MB initial, grows to 2GB max via geometric step (0.2x + 16MB linear)
- **Physics:** 50Hz fixed timestep (`Time.fixedDeltaTime = 0.02f`)

**Build Configuration:**
- Build Profile: `Assets/Settings/BuildProfiles/WebGLRelease.asset`
- Compression: Gzip with decompression fallback
- File naming: Content-hashed filenames (`nameFilesAsHashes = true`) to bust CDN caches
- WebGL Linking: asm.js/WebAssembly linker target 1
- Platform: WebGL with managed IL2CPP compilation

**Editor/Development:**
- Assembly definitions per folder: `GolfGame.Core`, `GolfGame.Golf`, `GolfGame.UI`, `GolfGame.Camera`, `GolfGame.Multiplayer`, `GolfGame.Environment`, `GolfGame.Audio` - compile-time isolation
- Test assemblies: `GolfGame.Tests.EditMode`, `GolfGame.Tests.PlayMode`
- MCP for Unity integration available via Window > MCP for Unity

## Platform Requirements

**Development:**
- Unity Hub with Unity 6 LTS (`6000.0.23f1`)
- C# IDE (Rider or Visual Studio recommended)
- Node.js 20+
- Git LFS for asset management
- macOS, Windows, or Linux for editor

**Production:**
- WebGL-capable browser (Chrome, Firefox, Safari, Edge)
- Mobile browser recommended for touch input
- Minimum 32MB WebAssembly memory allocation
- Unity Gaming Services account with project configured

**CI/CD:**
- Docker image: `unityci/editor:6000.3.10f1-webgl-3` (CI builds)
- Node.js 20 for Cloud Code testing and deployment
- GitLab CI with custom pipeline configuration

---

*Stack analysis: 2026-03-22*
