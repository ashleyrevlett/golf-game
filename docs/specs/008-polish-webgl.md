---
issue: 8
title: Polish + WebGL optimization
type: feature
tags: [webgl, polish, performance, touch, bootstrap]
---

# Polish + WebGL Optimization

## What
Final integration, WebGL build config, mobile touch handling, and service bootstrap.

## Bootstrap
- Bootstrap.cs registers MockAuthService and MockLeaderboardService
- Runs before AppManager via DefaultExecutionOrder(-100)
- Configures physics (autoSync off, 50Hz fixed timestep)

## WebGL Template
- Custom template with responsive canvas, touch-action: none
- Mobile viewport meta tags (no zoom/scale)
- Green loading bar matching game theme

## Touch Input
- TouchBlocker.jslib prevents scroll/zoom/context menu on canvas
- TouchInputBlocker.cs calls jslib and captures keyboard input

## Performance Config
- Static batching (CourseBuilder already handles)
- Shared materials (8 total, already in place)
- Physics autoSync disabled
- 50Hz fixed timestep

## Files
- `Assets/Scripts/Multiplayer/Bootstrap.cs`
- `Assets/Scripts/Core/TouchInputBlocker.cs`
- `Assets/Plugins/WebGL/TouchBlocker.jslib`
- `Assets/WebGLTemplates/GolfGame/index.html`
- `Assets/WebGLTemplates/GolfGame/TemplateData/style.css`
