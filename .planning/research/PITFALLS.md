# Domain Pitfalls

**Domain:** Unity 6 WebGL game -- browser playability + CI/CD deployment to Cloudflare Pages
**Researched:** 2026-03-22

---

## Critical Pitfalls

Mistakes that cause broken deployments, white screens, or build pipeline failures.

---

### Pitfall 1: Cloudflare Pages Cannot Serve Pre-Compressed Files with Content-Encoding Headers

**Confidence:** HIGH (multiple Cloudflare community sources + official docs confirm)

**What goes wrong:** The project's `WebGLBuildScript.cs` (line 15) sets `compressionFormat = WebGLCompressionFormat.Gzip`, producing `.data.gz`, `.framework.js.gz`, and `.wasm.gz` files. The plan documented in `PROJECT.md` is to add a `_headers` file with `Content-Encoding: gzip` rules. **This will not work.** Cloudflare Pages does not honor custom `Content-Encoding` headers set via the `_headers` file. The platform controls compression at the edge and ignores origin `Content-Encoding` directives for static assets.

**Why it happens:** Cloudflare Pages manages its own compression pipeline. When Unity's pre-compressed `.gz` files are uploaded, Cloudflare may additionally gzip them (double compression) or strip the `Content-Encoding` header entirely. The browser then receives a file with the wrong encoding, causing `CompileError: WebAssembly.instantiateStreaming failed` or a blank white screen.

**Consequences:** Game fails to load in any browser. The WASM module cannot be instantiated. No error is shown to the user beyond a blank screen or a cryptic console error.

**Prevention:** The project already has `decompressionFallback = true` set in `WebGLBuildScript.cs` (line 16). This is the correct direction for Cloudflare Pages. When decompression fallback is enabled, Unity embeds a JavaScript decompressor in the loader and renames build artifacts to `.unityweb` extension. The loader detects whether the browser received proper `Content-Encoding` and falls back to JS-based decompression if not. **No `_headers` file is needed for Content-Encoding.**

However, `decompressionFallback = true` combined with Gzip compression has a subtle interaction: Cloudflare's edge may still gzip the already-gzipped `.unityweb` files. The JS decompressor handles this, but at a performance cost (larger loader, slower initial load, no WebAssembly streaming compilation).

**The canonical fix has two viable options:**

**Option A (Simple, current approach):** Keep Gzip + decompressionFallback. Accept the performance penalty. This is what the codebase already does.

**Option B (Better performance, more setup):** Build with compression disabled (`WebGLCompressionFormat.Disabled`), let Cloudflare handle compression at the edge automatically. This avoids double-compression entirely, enables WebAssembly streaming compilation, and produces the smallest-over-the-wire result because Cloudflare applies Brotli (better than Gzip) to all compressible assets by default.

If a `_headers` file is created, use it only for `Content-Type` (MIME type) overrides and caching, not `Content-Encoding`:

```
# _headers file for Cloudflare Pages (place in build output root)
/Build/*.wasm
  Content-Type: application/wasm
/Build/*.data
  Content-Type: application/octet-stream
/Build/*.js
  Content-Type: application/javascript
/*
  Cache-Control: public, max-age=31536000, immutable
/index.html
  Cache-Control: public, max-age=0, must-revalidate
```

**Detection:** After deploying, open browser DevTools > Network tab. Check the `Content-Encoding` response header on `.wasm` / `.data` files. If you see `Content-Encoding: gzip` on a file that was already pre-gzipped by Unity, double-compression is occurring.

**Codebase references:**
- `Assets/Editor/WebGLBuildScript.cs:15-16` (compression settings)
- `.planning/PROJECT.md:44` (documents the problem)
- `docs/ci-cd-gotchas.md:13-19` (documents intended fix -- needs correction)
- `docs/deployment.md:14` (states Brotli but build script uses Gzip -- inconsistency)

**Sources:**
- [Cloudflare community: Pre-Compressed Assets in Pages](https://community.cloudflare.com/t/pre-compressed-assets-in-pages/300028)
- [Cloudflare Pages: _headers file does not work](https://community.cloudflare.com/t/cloudflare-pages-headers-file-does-not-work/602007)
- [Cloudflare Pages Headers docs](https://developers.cloudflare.com/pages/configuration/headers/)
- [Unity WebGL compression done right](https://miltoncandelero.github.io/unity-webgl-compression)
- [Unity Manual: Deploy a Web application](https://docs.unity3d.com/Manual/webgl-deploying.html)

---

### Pitfall 2: Unity License Activation Fails Silently in Docker CI

**Confidence:** HIGH (GameCI docs + codebase `.gitlab-ci.yml` confirmed)

**What goes wrong:** The `webgl-build` job in `.gitlab-ci.yml` (lines 38-42) activates the license via `-manualLicenseFile /tmp/unity.ulf`. If the `.ulf` file content is malformed, expired, or for the wrong Unity version, the activation command exits with code 1 -- but the pipeline continues because of the `|| true` on line 41. The subsequent build then fails with an unhelpful "no valid Unity license" error buried in thousands of lines of Unity log output.

**Why it happens:**
1. The `.ulf` file has strict version affinity. A license generated for Unity `2022.3.x` will not activate `6000.3.10f1`. The `.ulf` must be generated from the exact editor version in the Docker image.
2. The `|| true` on the activation command swallows the failure exit code.
3. Personal (free) licenses generate `.ulf` files with a different internal structure than Pro licenses. Personal `.ulf` files have limited validity and may silently expire.
4. The `.ulf` content stored in `$UNITY_LICENSE` GitLab CI variable may have been corrupted by copy-paste (trailing newlines, encoding issues).

**Consequences:** Pipeline runs for 20-40 minutes before failing on the actual build step, wasting CI minutes. Error message is buried in log output.

**Prevention:**
1. **Generate the `.ulf` from the exact Docker image.** Run interactively:
   ```bash
   docker run -it unityci/editor:6000.3.10f1-webgl-3 bash
   unity-editor -batchmode -nographics -createManualActivationFile -logFile /dev/stdout
   # Copy the .alf file out, upload to license.unity3d.com, download .ulf
   ```
2. **For Pro/Plus licenses, use serial key activation instead of `.ulf`:**
   ```yaml
   before_script:
     - unity-editor -batchmode -nographics -quit
         -username "$UNITY_EMAIL"
         -password "$UNITY_PASSWORD"
         -serial "$UNITY_SERIAL"
         -logFile /dev/stdout
     - |
       if [ $? -ne 0 ]; then
         echo "LICENSE ACTIVATION FAILED"
         exit 1
       fi
   ```
3. **Remove `|| true` from activation and add explicit validation:**
   ```yaml
   before_script:
     - echo "$UNITY_LICENSE" > /tmp/unity.ulf
     - unity-editor -batchmode -nographics -quit
         -manualLicenseFile /tmp/unity.ulf
         -logFile /tmp/activation.log
     - |
       if grep -q "LICENSE SYSTEM .* Failed" /tmp/activation.log; then
         echo "License activation failed!"
         cat /tmp/activation.log
         exit 1
       fi
     - rm -f /tmp/unity.ulf
   ```
4. **Store the CI variable as a "File" type** in GitLab (not "Variable") to avoid encoding issues with multi-line `.ulf` content.

**Detection:** Check `unity-editor` activation log for `"LICENSE SYSTEM"` messages. A successful activation logs `"LICENSE SYSTEM [2026...] Next license update check is after..."`. A failure logs `"LICENSE SYSTEM [2026...] Failed to activate..."`.

**Codebase references:**
- `.gitlab-ci.yml:38-42` (current activation approach)
- `docs/ci-cd-gotchas.md:33-47` (documents the three secrets)
- `.planning/PROJECT.md:43` (notes secrets not yet configured)

**Sources:**
- [GameCI: Activation for GitLab](https://game.ci/docs/gitlab/activation/)
- [GameCI: Common Issues](https://game.ci/docs/troubleshooting/common-issues/)
- [Unity Issue Tracker: Cannot activate license within a Docker container](https://issuetracker.unity3d.com/issues/cannot-activate-license-within-a-docker-container)

---

### Pitfall 3: Unhandled Exceptions in async void Kill WebGL

**Confidence:** HIGH (verified in codebase + Unity docs)

**What goes wrong:** An `async void` method throws an exception that is not caught. On desktop Unity, this logs an error. On WebGL, the exception propagates to the JavaScript runtime and terminates the WASM instance. The game freezes with no error visible to the player.

**Why it happens:** Unity WebGL's default exception mode ("Explicitly Thrown") only catches `throw new Exception()`. It does NOT catch NullReferenceException or unhandled Task exceptions. Developers test in Editor where exceptions are caught; WebGL behavior diverges.

**Consequences:** Game freezes. Player must reload the page. No error message shown.

**Prevention:** Every `async void` method must have its entire body in try-catch. Every fire-and-forget `_ = SomeAsync()` must use a wrapper that catches exceptions.

**Detection:** Test in a WebGL build, not just Editor. Monitor browser console for unhandled promise rejections.

**Codebase references:**
- `Assets/Scripts/UI/GameOverController.cs:132` - `UpdateFinalScore()`
- `Assets/Scripts/Core/AppManager.cs:81` - `HandleStateTransition()`
- `Assets/Scripts/Multiplayer/Bootstrap.cs:20` - `Awake()`
- `Assets/Scripts/Multiplayer/LeaderboardManager.cs:50,142,156`
- `.planning/codebase/CONCERNS.md:5-17`

**Sources:**
- [Unity Manual: Debug and troubleshoot Web builds](https://docs.unity3d.com/Manual/webgl-debugging.html)
- [Unity Discussions: async and uncaught exceptions](https://discussions.unity.com/t/async-and-uncaught-exceptions/824272)

---

### Pitfall 4: `System.Threading` Does Not Work in WebGL -- Silent Failures

**Confidence:** HIGH (Unity official docs confirm)

**What goes wrong:** Any code using `System.Threading.Timer`, `CancellationTokenSource` timeouts, `Task.Run`, `Task.Delay` (which internally uses `System.Threading.Timer`), `Thread.Sleep`, or manual thread creation will silently fail or deadlock in WebGL builds. The timer callback never fires, timeouts never trigger, and `Task.Run` queues work that never executes.

**Why it happens:** WebAssembly runs on a single thread. Unity's WebGL runtime does not include the managed threading subsystem. The `System.Threading` namespace compiles without errors but the runtime implementations are stubs that silently do nothing.

**Consequences in this codebase:**
1. `LeaderboardManager.cs` uses polling with delays. If any polling mechanism uses `Task.Delay` internally, it will hang forever.
2. `Bootstrap.cs:47` calls `UnityServices.InitializeAsync()` which returns a `Task`. This works because Unity's UGS SDK is WebGL-aware and uses coroutine-backed async internally. But any custom async code that chains `Task.Delay` or `CancellationTokenSource` with timeout will break.

**Specific dangerous patterns:**
```csharp
// BROKEN in WebGL -- timer never fires
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await SomeAsync(cts.Token);  // Token never cancels, waits forever

// BROKEN in WebGL -- Task.Delay uses System.Threading.Timer
await Task.Delay(1000);  // Hangs forever

// BROKEN in WebGL -- no thread pool
await Task.Run(() => ExpensiveWork());  // Never executes
```

**Prevention:**
- Use `Awaitable.WaitForSecondsAsync()` (Unity 6) instead of `Task.Delay`
- Use `UnityWebRequest` for HTTP (already WebGL-safe)
- Use coroutines (`IEnumerator` + `yield return`) for any delayed work
- For timeouts, track elapsed time manually via `Time.time` comparisons
- The UGS SDK `UnityServices.InitializeAsync()` is safe -- Unity's SDK uses WebGL-compatible async

**Detection:** Works in Editor (full .NET threading) but fails only in WebGL builds. Must test in actual browser.

**Codebase references:**
- `Assets/Scripts/Multiplayer/Bootstrap.cs:47` (InitializeAsync -- safe)
- `Assets/Scripts/Multiplayer/LeaderboardManager.cs:108-114` (polling loop)
- `.planning/codebase/CONCERNS.md:76-79`

**Sources:**
- [Unity Manual: WebGL Technical Limitations](https://docs.unity3d.com/6000.2/Documentation/Manual/webgl-technical-overview.html)
- [Unity Issue Tracker: Async Tasks do not run on WebGL when Threading is enabled](https://issuetracker.unity3d.com/issues/webgl-async-tasks-do-not-run-on-webgl-when-threading-is-enabled)

---

### Pitfall 5: iOS Safari WebGL Context Lost / Black Screen

**Confidence:** HIGH (Unity Issue Tracker + multiple community reports for iOS 17/18)

**What goes wrong:** On iOS Safari (especially iOS 17+ and iOS 18.4+), the game either shows a black screen on initial load, or the WebGL context is lost after the app is backgrounded and restored. Some reports indicate older iPads (9th gen and below) fail entirely after iOS 18.4.

**Why it happens:**
- iOS Safari aggressively reclaims GPU resources when a tab is backgrounded
- iOS 18.4 introduced changes to WebGL behavior that broke some Unity builds on older hardware
- WebGL2 has known compatibility issues on Safari. Unity Issue Tracker has a confirmed bug: "WebGL2 build shows black screen in Safari"
- UI Toolkit + WebGL + iOS has a specific rendering failure in Unity 6

**Consequences:** Complete loss of game state. User sees a black screen or frozen frame. No recovery without full page reload.

**Prevention:**
1. **Use WebGL 1 instead of WebGL 2** if targeting iOS Safari. In Player Settings > Other Settings > Graphics APIs, put WebGL 1 first or remove WebGL 2. Verify in Build Profile `.asset` if one is active.
2. **Handle context loss gracefully** in `Assets/WebGLTemplates/GolfGame/index.html`:
   ```javascript
   canvas.addEventListener('webglcontextlost', function(e) {
       e.preventDefault();
       document.getElementById('unity-loading-bar').style.display = 'block';
       document.getElementById('unity-loading-bar').innerHTML =
           '<p style="color:white;text-align:center;padding-top:40vh">' +
           'Session expired. Tap to reload.</p>';
       document.addEventListener('touchstart', function() {
           window.location.reload();
       }, { once: true });
   }, false);
   ```
3. **Test on actual iOS devices.** Desktop Safari and Chrome DevTools mobile emulation do not reproduce the issue.

**Detection:** Test on an iPhone with iOS 17+ in Safari. Background the tab for 30 seconds, return. If the screen is black or frozen, context loss handling is needed.

**Codebase references:**
- `Assets/WebGLTemplates/GolfGame/index.html` (no context loss handling currently)
- `docs/webgl-gotchas.md:32-36` (documents iOS fullscreen limitation but not context loss)

**Sources:**
- [Unity Discussions: WebGL context lost - iOS 17 Safari](https://discussions.unity.com/t/webgl-context-lost-ios-17-safari/930432)
- [Unity Discussions: WebGL not working on Safari after iOS 18.4](https://discussions.unity.com/t/webgl-is-not-working-on-safari-after-ios-18-4-update/1628007)
- [Unity Discussions: UI Toolkit + WebGL + iOS issue on Unity 6](https://discussions.unity.com/t/ui-toolkit-webgl-ios-issue-on-unity-6/1571559)
- [Unity Issue Tracker: WebGL2 build shows black screen in Safari](https://issuetracker.unity3d.com/issues/safari-webgl2-build-shows-black-screen-in-safari)

---

### Pitfall 6: Git LFS Pointer Files Break Unity Import in CI

**Confidence:** HIGH (documented in codebase + confirmed by community)

**What goes wrong:** Two related failures:

**6a. LFS smudge filter not run.** GitLab CI clones the repo but LFS objects are not downloaded. Unity finds LFS pointer files (134-byte text stubs) instead of actual textures/audio. The build produces a broken game with missing assets or errors out during import.

**6b. `.lfs-assets-id` dirty build.** After `git lfs pull`, Unity's Library cache regeneration creates or modifies `.lfs-assets-id`, making the working tree "dirty." Some Unity CI workflows fail when the working directory is not clean.

**Why it happens:**
- `.gitattributes` (lines 1-56) tracks `.png`, `.jpg`, `.wav`, `.fbx` and many other binary types via LFS
- The CI Docker image may not have `git-lfs` in all image versions
- `GIT_LFS_SKIP_SMUDGE` may be set globally on the runner

**Consequences:** Build succeeds but produces a broken game (pink/missing textures, no audio) or fails during Unity import.

**Prevention:** The current `.gitlab-ci.yml` (lines 44-45) already handles this correctly:
```yaml
script:
  - git lfs install && git lfs pull
  - rm -f .lfs-assets-id
```

Additional safeguards:
1. **Verify LFS objects were actually downloaded:**
   ```yaml
   - git lfs install && git lfs pull
   - |
     POINTER_COUNT=$(git lfs ls-files | grep "^-" | wc -l)
     if [ "$POINTER_COUNT" -gt 0 ]; then
       echo "ERROR: $POINTER_COUNT LFS files are still pointers!"
       git lfs ls-files | grep "^-"
       exit 1
     fi
   - rm -f .lfs-assets-id
   ```
2. Set `GIT_LFS_SKIP_SMUDGE: "0"` explicitly in the job's variables.

**Detection:** In the build log, look for Unity import errors like `"Could not read file"` or textures showing as pink squares.

**Codebase references:**
- `.gitlab-ci.yml:44-45` (current LFS handling)
- `.gitattributes` (LFS tracking rules)
- `docs/ci-cd-gotchas.md:51-56`

**Sources:**
- [Git LFS and Unity](https://geraldclarkaudio.medium.com/git-lfs-and-unity-dc7c7544a7c5)
- [Unity WebGL project not working with git lfs](https://github.com/git-lfs/git-lfs/discussions/5047)

---

### Pitfall 7: iOS Safari Memory Ceiling Causes Silent Page Reload

**Confidence:** HIGH (community reports, standard WebGL/iOS limitation)

**What goes wrong:** WebAssembly memory grows beyond iOS Safari's limit (~256MB initial, growth triggers reload). The browser reloads the page silently. No crash callback, no error event, no way for the game to save state.

**Why it happens:** WebGL memory is pre-allocated as a contiguous ArrayBuffer. When it needs to grow (e.g., unbounded queues, texture loading, GC pressure), Safari may refuse and reload instead of throwing an error.

**Consequences:** Player loses all progress. Page reloads to initial state.

**Prevention:** Cap all dynamic data structures (queues, lists, caches). Monitor memory usage. Test on real iOS devices. The `LeaderboardManager.cs` retry queue (noted in CONCERNS.md as unbounded) is a specific risk here.

**Detection:** Only detectable on real iOS hardware. Desktop Safari emulation does not reproduce the memory limit.

**Codebase references:**
- `Assets/Scripts/Multiplayer/LeaderboardManager.cs:29-30` (unbounded retry queue)
- `.planning/codebase/CONCERNS.md:57-63`

**Sources:**
- [Unity Discussions: WebGL memory increment and crash on iOS](https://discussions.unity.com/t/webgl-memory-increment-issue-and-crash-on-ios/894771)

---

## Moderate Pitfalls

---

### Pitfall 8: UI Toolkit Touch Input Erratic on Mobile WebGL

**Confidence:** MEDIUM (multiple community reports January 2026, no official fix documented)

**What goes wrong:** UI Toolkit buttons in WebGL builds require multiple taps to register on mobile browsers. Single taps are sometimes ignored. Drags work but taps do not on iOS Safari.

**Why it happens:** Unity's UI Toolkit WebGL input handling has a known mismatch between mouse events (which WebGL simulates from touch) and touch events. The first tap may be interpreted as "focusing" the canvas rather than a click.

**Prevention:**
1. Ensure the canvas has `touch-action: none` (already present in `index.html:13`)
2. Make touch targets large (minimum 48x48dp)
3. Use `pointerdown` events instead of `click` for critical JavaScript interop
4. If UI Toolkit touch remains unreliable for menus, critical gameplay input (shot power/accuracy) should use Input System pointer events rather than UI Toolkit buttons (already the case for `ShotInput.cs`)

**Codebase references:**
- `Assets/WebGLTemplates/GolfGame/index.html:13` (`touch-action: none`)
- `Assets/Scripts/Golf/ShotInput.cs` (uses Input System, not UI Toolkit)
- UI controllers in `Assets/Scripts/UI/`

**Sources:**
- [Unity Discussions: UI Toolkit buttons with WebGL on mobile browsers](https://discussions.unity.com/t/problem-with-ui-toolkit-buttons-with-webgl-on-mobile-browsers/1706183)
- [Unity Discussions: Single touches don't work on WebGL/iOS](https://discussions.unity.com/t/single-touches-dont-seem-to-work-on-webgl-ios-but-drags-do/887021)

---

### Pitfall 9: Mobile Viewport Bounce / Overscroll Breaks Game Canvas

**Confidence:** MEDIUM (standard WebGL mobile deployment issue)

**What goes wrong:** On mobile Safari and Chrome, the browser's rubber-band scroll (overscroll bounce) and address bar show/hide cause the game canvas to resize unexpectedly. Touches near the top/bottom edges trigger browser navigation gestures instead of game input.

**Prevention:** The current `index.html` has basic viewport meta (line 6) but needs additional overscroll prevention. Add to the template:

```html
<!-- In <head> (update existing meta) -->
<meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover">
```

```css
/* In style.css */
html, body {
    margin: 0;
    padding: 0;
    overflow: hidden;
    position: fixed;
    width: 100%;
    height: 100%;
    touch-action: none;
    overscroll-behavior: none;
}
```

```javascript
// In index.html script
document.addEventListener('touchmove', function(e) { e.preventDefault(); }, { passive: false });
```

**Codebase references:**
- `Assets/WebGLTemplates/GolfGame/index.html:6` (viewport meta)
- `Assets/WebGLTemplates/GolfGame/TemplateData/style.css`

---

### Pitfall 10: Documentation Contains Contradictory Compression Guidance

**Confidence:** HIGH (verified by reading files)

**What goes wrong:** The codebase documentation is internally inconsistent about compression:
- `docs/ci-cd-gotchas.md:13` says "GameCI compresses to `.br` (Brotli)"
- `docs/deployment.md:14` says "Brotli-compressed Unity WebGL assets"
- `Assets/Editor/WebGLBuildScript.cs:15` actually sets `WebGLCompressionFormat.Gzip`
- `docs/ci-cd-gotchas.md:15-18` provides a `_headers` example for `.br` files

A developer following the docs would configure for Brotli when the build actually produces Gzip. The `_headers` file would reference `.wasm.br` files that do not exist.

**Prevention:** Update `docs/ci-cd-gotchas.md` and `docs/deployment.md` to match the actual build script. The build uses Gzip, not Brotli. And as documented in Pitfall 1, the `_headers` file should not set `Content-Encoding` on Cloudflare Pages anyway.

**Codebase references:**
- `docs/ci-cd-gotchas.md:13-19` (says Brotli, wrong)
- `docs/deployment.md:14` (says Brotli, wrong)
- `Assets/Editor/WebGLBuildScript.cs:15` (actual: Gzip)

---

### Pitfall 11: Docker Image Disk Space Exhaustion on Shared Runners

**Confidence:** MEDIUM (documented in codebase, standard CI issue)

**What goes wrong:** The `unityci/editor:6000.3.10f1-webgl-3` Docker image is 15+ GB. On GitLab.com shared runners with limited disk space, the image pull alone can exhaust available storage. Combined with Library cache generation, total disk usage can exceed 30 GB.

**Prevention:**
- `.gitlab-ci.yml:37` already cleans up unused directories in the container
- Consider using a self-hosted runner (`docs/deployment.md:38` is a TODO)
- Add `GIT_CLEAN_FLAGS: "-ffdx -e Library/"` to preserve Library cache on persistent runners
- Monitor for `"No space left on device"` errors

**Codebase references:**
- `.gitlab-ci.yml:37` (cleanup step)
- `docs/ci-cd-gotchas.md:23-28`
- `docs/deployment.md:38` (self-hosted runner TODO)

---

### Pitfall 12: `nameFilesAsHashes` Breaks Specific-Filename _headers Patterns

**Confidence:** MEDIUM (logical deduction from build script + Cloudflare docs)

**What goes wrong:** `WebGLBuildScript.cs:14` enables `nameFilesAsHashes = true`, producing filenames like `Build/abc123def456.data.gz` instead of `Build/golf-game.data.gz`. If a `_headers` file uses patterns like `/Build/golf-game.*`, those patterns will not match. Wildcard patterns like `/Build/*` still work.

**Prevention:** Always use wildcard patterns (`/Build/*`) in `_headers`, not specific filenames. Hashed filenames enable aggressive `Cache-Control: immutable` caching since the filename changes on every build.

**Codebase references:**
- `Assets/Editor/WebGLBuildScript.cs:14` (`nameFilesAsHashes = true`)

---

### Pitfall 13: FindFirstObjectByType Race Condition on Scene Load

**Confidence:** HIGH (verified in codebase)

**What goes wrong:** Component A calls `FindFirstObjectByType<ComponentB>()` in `Start()`. If ComponentB's GameObject has not yet been instantiated, the call returns null. Component A silently operates without ComponentB for the rest of the session.

**Why it happens:** Unity does not guarantee `Start()` order across GameObjects. On slower mobile devices, scene load may be slower, changing effective initialization order.

**Consequences:** Shot loop breaks silently. Camera stays stuck. Score never updates.

**Prevention:** Always null-check. Log errors at `LogError` level. Consider `[DefaultExecutionOrder]` for critical components. Bootstrap already uses `[DefaultExecutionOrder(-100)]`.

**Codebase references:**
- `Assets/Scripts/Camera/CameraController.cs:37-40`
- `Assets/Scripts/Golf/BallController.cs:52-53`
- `Assets/Scripts/Golf/ShotInput.cs:89-90`
- `.planning/codebase/CONCERNS.md:29-39`

---

## Minor Pitfalls

### Pitfall 14: Wrangler Deploy Requires Explicit Auth Variables in CI

**Confidence:** HIGH (standard Cloudflare Pages requirement)

**What goes wrong:** The deploy job in `.gitlab-ci.yml:65` runs `wrangler pages deploy` but does not explicitly declare authentication variables. Wrangler needs `CLOUDFLARE_API_TOKEN` and `CLOUDFLARE_ACCOUNT_ID`.

**Prevention:** Set as GitLab CI/CD variables (masked, protected). Ensure the deploy job can access them. The API token needs "Cloudflare Pages: Edit" permission.

**Codebase references:**
- `.gitlab-ci.yml:59-70` (deploy job)
- `docs/deployment.md:27-28` (local deploy reads from credentials file)

---

### Pitfall 15: Build Profile Overrides PlayerSettings API Calls

**Confidence:** HIGH (documented in codebase)

**What goes wrong:** Unity 6 Build Profiles override `ProjectSettings/ProjectSettings.asset`. If a Build Profile is active, the `PlayerSettings.*` calls in `WebGLBuildScript.cs` (compression, decompression fallback, nameFilesAsHashes) may be overridden by the Build Profile's saved values.

**Prevention:** Verify that no Build Profile `.asset` file overrides the settings. Check `Assets/Settings/` for Build Profile files.

**Codebase references:**
- `Assets/Editor/WebGLBuildScript.cs:14-16`
- `docs/webgl-gotchas.md:74-76`
- `CLAUDE.md` (warns about Build Profile overrides)

---

### Pitfall 16: Physics Timestep / Render Rate Mismatch

**Confidence:** MEDIUM (standard Unity issue)

**What goes wrong:** Physics runs at 50Hz but rendering at 60fps. Ball movement stutters on low-end mobile devices where multiple FixedUpdate calls execute per frame.

**Prevention:** Consider `Time.fixedDeltaTime = 1f/60f`. Verify `RigidbodyInterpolation.Interpolate` is enabled on the ball.

**Codebase references:**
- `Assets/Scripts/Multiplayer/Bootstrap.cs:87` (`Time.fixedDeltaTime = 0.02f`)
- `Assets/Scripts/Golf/BallController.cs`

---

### Pitfall 17: async void Bootstrap Silently Falls Back to Mock Services

**Confidence:** HIGH (verified in codebase)

**What goes wrong:** If UGS initialization fails in `Bootstrap.cs`, the game silently falls back to mock services. The player thinks they are on the leaderboard but scores are never actually submitted. No user-visible indication of offline mode.

**Prevention:** Add a visible "offline mode" indicator when mock services are active.

**Codebase references:**
- `Assets/Scripts/Multiplayer/Bootstrap.cs:66-69`
- `.planning/codebase/CONCERNS.md:5-17`

---

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| CI Pipeline Setup | License activation failure (Pitfall 2) | Generate .ulf from exact Docker image version; validate activation before build; remove `\|\| true` |
| CI Pipeline Setup | LFS pointer files (Pitfall 6) | Verify LFS smudge after pull; add pointer-count check |
| CI Pipeline Setup | Disk space on shared runners (Pitfall 11) | Monitor first run; plan for self-hosted runner if needed |
| Cloudflare Deployment | Double compression (Pitfall 1) | Use decompressionFallback or switch to disabled compression; do NOT rely on _headers for Content-Encoding |
| Cloudflare Deployment | Missing Wrangler auth (Pitfall 14) | Set CLOUDFLARE_API_TOKEN and CLOUDFLARE_ACCOUNT_ID as CI variables before first deploy |
| Cloudflare Deployment | Hashed filenames (Pitfall 12) | Use wildcard patterns in _headers file |
| Documentation Fix | Contradictory compression docs (Pitfall 10) | Fix docs/ci-cd-gotchas.md and docs/deployment.md to match actual build script (Gzip not Brotli) |
| Mobile Browser Testing | iOS Safari black screen (Pitfall 5) | Test on real iOS device; consider WebGL 1 over WebGL 2 |
| Mobile Browser Testing | Touch input erratic (Pitfall 8) | Large touch targets; test on real devices |
| Mobile Browser Testing | Viewport bounce (Pitfall 9) | CSS overscroll-behavior + prevent default touchmove |
| Mobile Browser Testing | Memory ceiling (Pitfall 7) | Cap retry queue; test on real iOS |
| Runtime Stability | Threading silent failures (Pitfall 4) | Audit all async code for System.Threading usage |
| Runtime Stability | async void crashes (Pitfall 3) | Wrap all async void in try-catch |
| Runtime Stability | Component lookup nulls (Pitfall 13) | Null-check + LogError on all FindFirstObjectByType |

---

## Sources

- [Cloudflare Pages: Pre-Compressed Assets limitation](https://community.cloudflare.com/t/pre-compressed-assets-in-pages/300028)
- [Cloudflare Pages: _headers not working for Content-Encoding](https://community.cloudflare.com/t/cloudflare-pages-headers-file-does-not-work/602007)
- [Cloudflare Pages: Headers configuration docs](https://developers.cloudflare.com/pages/configuration/headers/)
- [Unity WebGL compression done right](https://miltoncandelero.github.io/unity-webgl-compression)
- [Unity Manual: Deploy a Web application](https://docs.unity3d.com/Manual/webgl-deploying.html)
- [Unity Manual: WebGL Technical Limitations](https://docs.unity3d.com/6000.2/Documentation/Manual/webgl-technical-overview.html)
- [Unity Manual: Debug and troubleshoot Web builds](https://docs.unity3d.com/Manual/webgl-debugging.html)
- [GameCI: GitLab Activation](https://game.ci/docs/gitlab/activation/)
- [GameCI: Common Issues](https://game.ci/docs/troubleshooting/common-issues/)
- [Unity Issue Tracker: Cannot activate license in Docker](https://issuetracker.unity3d.com/issues/cannot-activate-license-within-a-docker-container)
- [Unity Issue Tracker: WebGL2 black screen in Safari](https://issuetracker.unity3d.com/issues/safari-webgl2-build-shows-black-screen-in-safari)
- [Unity Issue Tracker: Async Tasks do not run on WebGL](https://issuetracker.unity3d.com/issues/webgl-async-tasks-do-not-run-on-webgl-when-threading-is-enabled)
- [Unity Discussions: WebGL context lost - iOS 17 Safari](https://discussions.unity.com/t/webgl-context-lost-ios-17-safari/930432)
- [Unity Discussions: WebGL not working after iOS 18.4](https://discussions.unity.com/t/webgl-is-not-working-on-safari-after-ios-18-4-update/1628007)
- [Unity Discussions: UI Toolkit + WebGL + iOS on Unity 6](https://discussions.unity.com/t/ui-toolkit-webgl-ios-issue-on-unity-6/1571559)
- [Unity Discussions: UI Toolkit buttons erratic on mobile WebGL](https://discussions.unity.com/t/problem-with-ui-toolkit-buttons-with-webgl-on-mobile-browsers/1706183)
- [Unity Discussions: Single touches don't work on WebGL/iOS](https://discussions.unity.com/t/single-touches-dont-seem-to-work-on-webgl-ios-but-drags-do/887021)
- Direct codebase analysis of all referenced files (2026-03-22)

---

*Pitfalls audit: 2026-03-22*
