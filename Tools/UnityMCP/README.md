# Backpack Survivor local Unity MCP integration

This local development tool uses the existing server at `http://127.0.0.1:8095/mcp`.
The game project is `../../BackpackSurvivor`. It does not start, stop, or reconfigure
the shared server or any other Unity project.

`UnityPackage/` is a 3.43 MiB local copy of MCP for Unity 10.2.0. Its origin is
recorded in `source-version.json`; the MIT license is in `UPSTREAM-LICENSE.txt`.
No project assets, provider credentials, environment files, or logs were copied.

The Editor bootstrap `BackpackSurvivor.EditorTools.McpProjectBootstrap.Connect`
checks the existing local endpoint before connecting and does not write global
EditorPrefs. Launch Unity with that `-executeMethod`, or use the menu
`Tools / Backpack Survivor / Connect Local MCP` after the package compiles.

Run a project-pinned, read-only audit with any Python 3 installation:

```powershell
python Tools/UnityMCP/Run-MCPRequests.py Tools/UnityMCP/Requests/audit.json Tools/UnityMCP/Logs/audit.json
```

The standard-library client initializes a fresh MCP session, requires exactly one
`BackpackSurvivor` instance, pins its full instance ID, and verifies the project
root before it runs requests. This matters because the shared server defaults to
another project. It also reads the custom-tools resource before tool execution.

Plans contain a `requests` array. Each request is a resource (`kind: resource`,
`uri`), a tool (`kind: tool`, `tool`, `arguments`), or `kind: list_tools`.
Requests run sequentially and failures stop dependent work. Inline image results
are saved beside the output JSON. This runner is capable of mutations if a plan
explicitly asks for them; the included audit plan only reads editor state.

The repository's `.gitignore` allows only this vendored package, license, origin
metadata, client script, documentation, and audit plan under `Tools/UnityMCP/`.
Logs, temporary request plans, Python caches, and other local tools remain ignored.
This keeps the local package dependency reproducible when the changes are shared.
