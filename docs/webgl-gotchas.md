# WebGL Gotchas

## Custom WebGL Templates

### Template Not Used

Custom templates require the complete folder structure, not just `index.html`:

```
Assets/WebGLTemplates/MyTemplate/
├── index.html
├── TemplateData/
│   ├── style.css
│   ├── favicon.ico
│   └── progress-bar-*.png
└── thumbnail.png         (optional)
```

Copy from Unity's built-in templates as a base:
```bash
cp -r "/Applications/Unity/Hub/Editor/6000.x.x/PlaybackEngines/WebGLSupport/BuildTools/WebGLTemplates/Base/PWA" Assets/WebGLTemplates/MyTemplate
```

### Template Setting Format

In `ProjectSettings/ProjectSettings.asset`:
- Custom: `webGLTemplate: PROJECT:MyTemplate`
- Built-in: `webGLTemplate: APPLICATION:Minimal`

## Fullscreen

### iOS Safari — No Fullscreen API

iOS Safari does not support the Fullscreen API. Options:
1. Hide the fullscreen button when API is unavailable (`document.fullscreenEnabled`)
2. PWA "Add to Home Screen" for fullscreen-like experience
3. Parent page can maximize the iframe to fill viewport

### Fullscreen in iframes

The iframe **must** have fullscreen permission:
```html
<iframe src="game/index.html" allow="fullscreen" allowfullscreen></iframe>
```

### Fullscreen Request Ignored

Browser requires a user gesture. Use `pointerdown` not `click`:
```javascript
btn.addEventListener('pointerdown', () => {
  document.documentElement.requestFullscreen();
});
```

## Batch Builds

Unity Editor must be closed before running batch builds:
```bash
/Applications/Unity/Hub/Editor/6000.x.x/Unity.app/Contents/MacOS/Unity \
  -quit -batchmode \
  -projectPath "/path/to/project" \
  -executeMethod BuildScript.BuildWebGL \
  -logFile /tmp/unity-build.log
```

## NativeWebSocket Build Settings

**Critical**: For NativeWebSocket in WebGL, disable these in Player Settings → Publishing:
- **Target WebAssembly 2023** — UNCHECKED
- **Use WebAssembly.Table** — UNCHECKED

Without this: `Module.dynCall_vi is not a function` at runtime.

## Build Profile Overrides (Unity 6)

Unity 6 Build Profiles **override** global `ProjectSettings/ProjectSettings.asset`. Changing a setting in ProjectSettings alone has no effect if a Build Profile is active. Always update in the Build Profile `.asset` files.

## JS Interop — Callbacks from JS to C#

When a jslib function needs to report results back to C#, use `SendMessage`:

```javascript
mergeInto(LibraryManager.library, {
    DoAsyncWork: function(gameObjectNamePtr, paramPtr) {
        var goName = UTF8ToString(gameObjectNamePtr);
        var param = UTF8ToString(paramPtr);

        navigator.share({ text: param }).then(function() {
            SendMessage(goName, 'OnAsyncResult', 'success');
        }).catch(function() {
            SendMessage(goName, 'OnAsyncResult', 'failed');
        });
    }
});
```

In C#:
```csharp
#if UNITY_WEBGL && !UNITY_EDITOR
[DllImport("__Internal")]
private static extern void DoAsyncWork(string gameObjectName, string param);
#endif

public void OnAsyncResult(string result)
{
    // Handle result from JavaScript
}
```

**Critical — dual rejection handling**: Async browser APIs can reject both synchronously (API absent) and as a Promise (user dismisses). Always use both try/catch and `.catch()`:
```js
try {
    if (navigator.share) {
        navigator.share(...).catch(function() { /* promise rejection */ });
    }
} catch(e) {
    // Synchronous throw (API absent)
}
```

**Note**: `SendMessage` requires an exact GameObject name and method name. If the receiving GameObject is renamed or the method is removed, the callback fails silently. Guard with existence checks in C#.

## Missing .meta Files

Always commit `.meta` files with new scripts/assets. Unity uses them for GUID references. Missing `.meta` = broken scene/prefab references on other machines.
