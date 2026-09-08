# Unity MCP 接入记录

日期：2026-09-08。用户在场景研究期间允许接入 Unity MCP，本轮已完成接入和本项目读取验证。

| 项目 | 实测结果 |
| --- | --- |
| 本地服务 | `http://127.0.0.1:8095/mcp`，复用现有服务 |
| 服务版本 | `mcp-for-unity-server 3.4.7` |
| Unity 包 | `com.coplaydev.unity-mcp 10.2.0` |
| 目标实例 | `BackpackSurvivor@86aa38285588d0a0`（本次会话标识） |
| 根目录校验 | `E:/YouXiKaiFa/Backpack Survivor/BackpackSurvivor` |
| Unity | 6000.3.20f1，编译已完成，ready_for_tools=true |
| 场景读取 | MainMenu、01-Run 均成功；01-Run 12 个根对象 |
| 截图 | Main Camera 直接渲染成功，保存为 `current-unity-camera.png` |
| Console | 接入、场景加载与截图后的查询均返回 0 条 error |
| 场景修改状态 | 01-Run 加载和截图后 isDirty=false；未保存场景改动 |
| Blender | `E:/Blender/blender.exe`，5.2.1，已实际渲染原 GLB |
| Meshy | Unity MCP 存在 generate_model 入口；list_providers 返回 configured=false |

Meshy 接口已经找到，但当前尚未配置服务密钥。本轮没有提交生成任务，没有读取或输出密钥；首次使用生成服务时需在 Unity MCP 的提供商配置中完成设置。Blender 的直接建模、模型清理与导出流程不依赖 Meshy。

## 工程改动

- Packages/manifest.json 与 packages-lock.json：增加本地编辑器工具依赖。
- Assets/BackpackSurvivor/Editor/McpProjectBootstrap.cs 及 .meta：通过菜单或启动方法连接已配置的本地服务；校验地址，不改共享 EditorPrefs。
- Tools/UnityMCP/：通用包（716 文件、约 3.43 MiB）、MIT 许可证、版本来源、请求脚本与只读审计计划。
- .gitignore：精确开放上述必要文件，确保本地包依赖可随工程共享；日志和临时请求继续忽略。

没有更改其他 Unity 项目、服务器默认实例、正式场景、美术资源或 Gameplay 脚本；未提交 Git commit。

## 后续使用

本地工具说明见 `Tools/UnityMCP/README.md`。编辑器菜单为 `Tools / Backpack Survivor / Connect Local MCP`。

共享服务器的默认实例属于另一个项目，所以本项目辅助客户端每次先创建独立会话、精确选择 BackpackSurvivor 实例，再校验根目录；不得省略路径校验。

```powershell
python Tools/UnityMCP/Run-MCPRequests.py Tools/UnityMCP/Requests/audit.json Tools/UnityMCP/Logs/audit.json
```

上例只读。自定义计划可包含写操作，因此执行前仍需核对计划内容与任务范围。实例 ID 在重开编辑器后可能变化，脚本会重新发现实例，不应硬编码本次 ID。

这次验证覆盖编辑器编译与场景/相机读取，没有执行完整 15 分钟游戏回归或性能验收。
