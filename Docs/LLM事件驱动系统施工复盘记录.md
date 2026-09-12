# LLM 事件驱动系统施工复盘记录

本文件用于记录施工过程中的决策、实际改动、验证证据和未完成事项，供后续项目面试与技术复盘使用。

## 记录规则

- 只记录实际执行过的操作和结果，不把计划写成完成事实。
- 不记录 `DEEPSEEK_API_KEY` 明文，也不记录完整请求头、完整密钥或任何可复用凭证。
- 每个施工阶段记录：目标、改动文件、验证操作、结果、证据、未验证项和下一步。
- Unity 构建、运行和 Editor 操作必须注明实际发生时间；无法执行时记录具体阻塞原因。

## 2026-09-12 · 方案收敛

### 已确认的产品与技术决策

- 合同只有在“任务达成且存活”时完成并推进。
- 重试、重抽和次数限制首版放宽，流程打通后再根据数据收束。
- 开箱品质采用精确匹配。
- `KillElite` 通过补齐敌人身份事件支持。
- 合同局启用全量 `questOnly` 物品池，常规局不启用。
- 事件定义允许后续发布新版本；已接受合同保存自己的定义版本与完整条件快照。
- 首版启用白名单只读工具调用。
- 开发构建显示原始响应与工具审计；离线文案由本地维护。
- 营地未开始本局时显示空背包。
- tier 对波次压力保留接入点，但不作为首版重点。
- NPC、营地和美术先使用 UI 与纯文本占位。
- `EnemySpawner.ApplyWaveSettings` 后续只拒绝小于等于 0 的生成间隔，允许终局 `0.05` 生效。

### 文档变更

- `Docs/LLM事件驱动系统技术方案.md`
- `Docs/LLM事件驱动系统施工流程.md`

两份文档已同步更新数据模型、结算语义、掉落过滤、敌人身份事件、工具审计、存档恢复、S0–S12 依赖和验收口径。

## S0 · LLM 非流式最小请求

### 目标

从 Unity Editor 菜单发起一次 DeepSeek 非流式请求，验证 JSON 响应、thinking 关闭、只读工具调用、结果回填、最终回答和错误降级。

### 已实施改动

- `BackpackSurvivor/Assets/BackpackSurvivor/Editor/LLM/DeepSeekS0Probe.cs`
- `BackpackSurvivor/Assets/BackpackSurvivor/Editor/LLM.meta`
- `BackpackSurvivor/Assets/BackpackSurvivor/Editor/LLM/DeepSeekS0Probe.cs.meta`

Editor 菜单：`Tools/Backpack Survivor/LLM/S0 Probe DeepSeek (Non-Streaming)`。

探针行为：

- 读取进程级环境变量，Windows 下回退读取用户级环境变量。
- 请求使用 `deepseek-flash` 与 `thinking: disabled`；工具轮使用 `response_format: text`，最终轮使用 `response_format: json_object`。
- 注册白名单只读工具 `get_probe_facts`。
- 拒绝未知工具名和非空工具参数。
- 记录实际工具执行、参数和回填结果。
- 原始响应只以掩码形式写入 Console。
- 处理 HTTP 错误、空响应、JSON 解析失败、未执行白名单工具和工具轮次超限。

### 静态验证

- 已检查脚本括号数量：开括号 69，闭括号 69。
- 已修正 `System.Diagnostics.Debug` 与 `UnityEngine.Debug` 的命名冲突，使用 `Stopwatch` 类型别名。
- 已确认没有把密钥写入脚本、资产或文档。

### Unity / API 验证

状态：**已完成**。

已通过项目自带 `Tools/UnityMCP/Run-MCPRequests.py` 连接本机 UnityMCP HTTP 服务。脚本默认端口仍为 `8095`，本次实际监听端口为 `http://127.0.0.1:8080/mcp`，因此通过运行时覆盖端点完成连接；没有修改 MCP 工具脚本。

连接确认：

- Unity 实例：`BackpackSurvivor@86aa38285588d0a0`
- Unity 版本：`6000.3.20f1`
- 工程根目录校验：`E:/YouXiKaiFa/Backpack Survivor/BackpackSurvivor`
- 当前场景：`Assets/BackpackSurvivor/Scenes/Run/01-Run_ArtFull.unity`
- S0 执行前脚本编译状态：`is_compiling=false`、`ready_for_tools=true`
- 用户级及进程级 `DEEPSEEK_API_KEY` 均可见；本记录不保存其值

首次真实执行发现一个请求体兼容问题：工具轮将空的 `response_format` 序列化为 `type: ""`，DeepSeek 返回 HTTP 400。修正为工具轮显式 `response_format.type: "text"`、最终轮使用 `json_object` 后重跑成功。

成功执行证据（Unity `Editor.log`，本次运行约 1.7 秒）：

- 第 1 轮收到结构化 `tool_calls`，工具名为 `get_probe_facts`，参数为 `{}`。
- 白名单工具实际执行一次，审计回填：`{"status":"ok","facts":{"source":"s0_probe","value":42}}`。
- 第 2 轮收到 `finish_reason: stop`，内容为合法 JSON，包含 `answer` 与 `usedTools:["get_probe_facts"]`。
- 计数器：`errors=0`、`emptyContent=0`、`parseFailures=0`；最终记录 `thinking=disabled`、`tool audit complete`。
- 失败首轮与成功重跑的 UnityMCP 请求文件保存在 `Tools/UnityMCP/Logs/`（该目录为本地运行日志，不写入密钥）。

本次还确认 Unity Console MCP 读取在异步菜单执行结束后可能返回空队列，因此最终判定以 Unity `Editor.log` 中的完整原始响应、工具审计和计数器为准；没有把空队列误判为失败。

### 可复现步骤

1. 在启动 Unity Editor 的同一用户环境中配置 `DEEPSEEK_API_KEY`。
2. 打开 Unity 工程并等待脚本编译完成。
3. 选择 `Tools/Backpack Survivor/LLM/S0 Probe DeepSeek (Non-Streaming)`。
4. 在 Console 检查：每轮掩码原始响应、工具审计、最终 Probe JSON、耗时和错误计数。
5. 重复执行若干次，记录 HTTP 错误、空内容和结构化解析失败次数。
6. 若后续需要面试演示，可从 Unity `Editor.log` 截取上述两轮原始响应与工具审计；本次已将关键结果摘要固化在本节。

### 下一步

S0 已完成，可以进入 S1 流式最小验证。S1 之前不接入游戏逻辑、营地 UI 或正式对话服务。
