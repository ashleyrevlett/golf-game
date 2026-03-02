# Unity MCP Server

The [Coplay Unity MCP](https://github.com/CoplayDev/unity-mcp) server bridges Claude Code and the Unity Editor, enabling direct scene manipulation, asset management, and component editing from the CLI.

## Install

1. In Unity: `Window > Package Manager > + > Add package from git URL`
2. Enter: `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`
3. Launch: `Window > MCP for Unity > Start Server`

## Claude Code Config

Add to `.claude.json` (project-scoped) or `~/.claude.json` (global):

```json
{
  "mcpServers": {
    "unityMCP": {
      "url": "http://localhost:8080/mcp"
    }
  }
}
```

The server must be running in Unity Editor before Claude Code can connect.

## Available Tools

| Tool | Purpose |
|------|---------|
| `manage_scene` | Load, save, screenshot, query scene state |
| `manage_gameobject` | Create, delete, modify, find GameObjects |
| `manage_components` | Add, remove, set properties on components |
| `manage_asset` | Import, duplicate, move assets |
| `manage_material` | Create/modify materials and shaders |
| `manage_texture` | Import and configure textures |
| `manage_ui` | UI element creation and layout |
| `create_script` | Generate C# MonoBehaviour scripts |
| `manage_script` | Read, modify existing scripts |
| `validate_script` | Check for compile errors |
| `execute_menu_item` | Trigger Unity menu commands |
| `manage_editor` | Editor state, play/pause, undo/redo |
| `batch_execute` | Run multiple operations in one call |

## Usage Guidelines

- **Always screenshot after visual changes** — use `manage_scene(action="screenshot")` to verify transforms, UI layout, and material assignments look correct
- **Verify scale after creating/parenting** — check Transform is `(1,1,1)` unless intentionally scaled
- **Don't edit .prefab/.unity/.asset directly** — use MCP tools or Unity Editor instead
- **Batch operations** — use `batch_execute` when making multiple related changes to reduce round-trips
- **Scene files are read-only to Claude** — all scene modifications go through MCP, never through file edits

## Workflow

1. Open Unity Editor with the project
2. Start MCP server (`Window > MCP for Unity`)
3. Run Claude Code — it auto-connects to `localhost:8080`
4. Use MCP tools for scene/prefab/component work
5. Use file editing for C# scripts (faster, diffable)

## When to Use MCP vs File Editing

| Task | Use |
|------|-----|
| Creating/positioning GameObjects | MCP |
| Adding/configuring components | MCP |
| Material and texture setup | MCP |
| Writing C# scripts | File editing (Read/Write/Edit tools) |
| Scene hierarchy changes | MCP |
| Verifying visual results | MCP screenshot |
| UXML/USS files | File editing |
