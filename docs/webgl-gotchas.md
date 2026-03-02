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

## Missing .meta Files

Always commit `.meta` files with new scripts/assets. Unity uses them for GUID references. Missing `.meta` = broken scene/prefab references on other machines.
