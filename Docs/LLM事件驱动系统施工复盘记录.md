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

## S1 · 流式最小验证

### 目标

在不接入游戏逻辑的前提下，使用 `DownloadHandlerScript` 实际接收 DeepSeek SSE，验证分片组帧、跨分片 UTF-8、工具参数累加、`[DONE]` 收尾、首字延迟和 Editor 主动取消。

### 已实施改动

- `BackpackSurvivor/Assets/BackpackSurvivor/Editor/LLM/DeepSeekS1StreamingProbe.cs`
- `BackpackSurvivor/Assets/BackpackSurvivor/Editor/LLM/DeepSeekS1StreamingProbe.cs.meta`

Editor 菜单：

- `Tools/Backpack Survivor/LLM/S1 Probe DeepSeek (Streaming)`
- `Tools/Backpack Survivor/LLM/S1 Cancel Active Stream`

实现要点：

- 使用 `DownloadHandlerScript.ReceiveData` 接收原始字节，用严格 UTF-8 `Decoder` 跨网络分片解码。
- 按 SSE 空行组帧，支持 CRLF/LF、多个 `data:` 行和精确的 `data: [DONE]` 终止。
- 解析 `choices[].delta.tool_calls[].function.arguments`，按 tool-call index 使用 `StringBuilder` 累加，结束后再解析完整 JSON。
- 工具轮保持 `thinking: disabled` 和 `tool_choice: required`，不强制 JSON Output，避免工具轮返回 DSML。
- 取消菜单在 Unity 主线程调用 `Abort()`；取消异常单独记录为预期中断，不计入普通错误。
- 不访问游戏状态、存档、正式 NPC 或 UI。

### UnityMCP / API 验证

状态：**已完成**。

真实运行结果（Unity `Editor.log`）：

- 首次完整流：首个网络字节 `95 ms`，首个 SSE data 事件 `96 ms`，首个非空 token `1192 ms`，总耗时 `1413 ms`。
- 收到 `25` 个 SSE 事件，解析 `24` 个 JSON 分片，`parseFailures=0`、`decodeFailures=0`。
- 收到 `finish_reason=tool_calls`，随后收到 `data: [DONE]`，`doneSeen=true`。
- UTF-8 完整解码通过；工具参数共 `22` 个增量片段，组装为 `{"probe_text": "流式中文 UTF-8 分片验证。"}`，最终参数校验 `validated=true`。
- 取消验证：连续执行 Start 与 Cancel 菜单后记录 `stream cancelled; no gameplay state was changed`，没有记录普通错误，也没有后续完成摘要。
- UnityMCP `read_console` 读取错误数为 `0`。本次没有单独等待 30 秒超时，超时路径标记为未实测。

本次使用的流式工具名为 `echo_stream_probe`，仅用于只读兼容性验证；未执行任何游戏内工具。

### 下一步

S1 已完成，可以进入 S2 模型配置面板。S2 之前不接入正式对话历史、营地 UI 或游戏状态变更。

## S2 · 模型配置面板

### 目标

在主菜单提供独立的模型配置面板，持久化 BYOK Key 与四项软上限，显示实际生效的密钥来源，并复用 DeepSeek 非流式 JSON 请求形状完成连通性自检。

### 已实施改动

- `BackpackSurvivor/Assets/BackpackSurvivor/Scripts/Core/LLM/LlmModelConfig.cs`
- `BackpackSurvivor/Assets/BackpackSurvivor/Scripts/Core/LLM/LlmConfigService.cs`
- `BackpackSurvivor/Assets/BackpackSurvivor/Scripts/Presentation/MainMenu/NpcConfigView.cs`
- `BackpackSurvivor/Assets/BackpackSurvivor/Editor/LlmConfigPanelBuilder.cs`
- `BackpackSurvivor/Assets/BackpackSurvivor/Editor/V04MainMenuArtBuilder.cs`
- `BackpackSurvivor/Assets/BackpackSurvivor/Scenes/MainMenu/MainMenu.unity`

实现约定：

- 配置文件固定写入 `Application.persistentDataPath/llm_model_config.json`，不进入 `save_data.json`、`Resources/`、`StreamingAssets/` 或仓库。
- Key 解析顺序为进程环境变量 → Windows 用户级环境变量 → 独立配置文件；环境变量存在时，面板明确显示“环境变量优先”，不会使用文件 Key 发请求。
- 面板使用密码输入框，状态只显示“已配置/未配置”或掩码末四位；异常响应会再次掩码，日志不写 Key 明文。
- UI 通过 `LlmConfigPanelBuilder` 生成独立 `NpcConfigModalRoot`，并纳入 `MainMenuOverlayPresentation`；没有手工摆放场景对象，也没有复用普通音量设置。
- 四项默认值为 `20 / 40000 / 200 / 60`，本阶段只保存软上限，不统计实际额度。

### UnityMCP / API 验证

状态：**已完成**。

- UnityMCP 已加载 `MainMenu.unity`，执行 `Tools/Backpack Survivor/UI/Build LLM Config Panel` 成功；场景中出现 `LlmConfigButton`、`NpcConfigModalRoot` 和 `NpcConfigView`。
- 通过 UnityMCP `execute_code` 打开面板并调用自检，状态文本返回“自检成功：DeepSeek 已连通”，来源显示为环境变量优先，Key 仅显示掩码末四位。
- 自检完成后再次读取配置，确认文件存在于 `C:/Users/cp/AppData/LocalLow/DefaultCompany/BackpackSurvivor/llm_model_config.json`，默认上限仍为 `20 / 40000`，说明写入路径与读取路径一致。
- 写入临时 `FILE_SENTINEL` 后解析配置，仍返回环境变量来源和环境变量掩码；随后恢复原文件，证明优先级和恢复操作正确。
- UnityMCP `read_console` 错误数为 `0`。本次没有启动游戏构建，也没有改动 `runSceneName`；主菜单入口仍保持 `01-Run_ArtFull`，入口切换属于 S7。
- 保存后重新加载 `MainMenu.unity`，UnityMCP 查到 `NpcConfigView=True`、`LlmConfigButton=True`，面板默认保持非激活，证明序列化引用可恢复。
- 错误 Key 的 HTTP 失败文案已实现并掩码，但由于本机环境变量有效，本次未通过真实错误 Key 覆盖该分支；后续在无环境变量的独立发布配置环境补测。

### 下一步

S2 已完成，可以进入 S3 判定输入补齐。S3 开始接入局内事实采集，但仍不接入 NPC 对话内容。


## S3 · 判定输入补齐（本次实施报告）

**阶段**：S3，2026-09-12。

**核心链路**：真实敌人死亡/宝箱交互 → 分类计数 → 结算前取消拖拽 → 同一物品列表冻结 `QuestRunSnapshot` 与既有 `RunResult` → Console / JSON。

**目的**：为 S4 提供可验证的本地事实，避免精英身份、精确开箱品质和拖拽中的物品在结算时丢失。

**技术选择**：

- `BS.Quest.Core` 只引用纯 C# 的 `BS.Inventory`，设置 `noEngineReferences: true`；使用自有 `RunOutcome`，不引用默认程序集的 GameState。
- `EnemyKind` 从池使用的 prefab 保留到死亡事件：普通默认 Normal、精英 prefab 显式 Elite、远程触发 Ranged；保留总击杀与宝箱进度的原有统计语义。
- 五个宝箱 bundle 显式存品质索引 0–4，成功交互只计一次；未知品质另计，不猜测颜色/名称。不改权重和 TrySpawnDrop 调用签名。
- 拖拽是从网格暂时移除物品的事务。原占位保留可阻止并发拾取占用；结算先恢复原方向与锚点，再聚合逐件值。对外快照复制数组/列表，隔离后续修改与下一局重置。
- S3 为既定快照字段补上 questOnly 数据透传，含丢弃后再拾取；S6 再做资产标记和过滤。两份方案同步记录此阶段分工。

**改动文件**（以下 Unity 路径以 `BackpackSurvivor/Assets/BackpackSurvivor/` 为前缀）：

- 新增 `Scripts/Quest/Core/BS.Quest.Core.asmdef`：纯程序集。
- 新增 `Scripts/Quest/Core/QuestRunSnapshot.cs`：快照、ItemRecord、RunOutcome、ChestQuality 与副本方法。
- 新增 `Scripts/GamePlay/Enemies/AI/EnemyKind.cs`：敌人分类。
- 修改 `Scripts/GamePlay/Enemies/AI/EnemyAI.cs`：携带身份的死亡事件；`Scripts/GamePlay/Enemies/AI/RangedEnemyAI.cs`：远程身份触发。
- 修改 `Scripts/GamePlay/Loot/Chests/ChestSpawner.cs`：适配事件参数，总击杀计数语义不变。
- 修改 `Prefabs/Enemy/EliteEnemy.prefab`：精英分类配置。
- 修改 `Scripts/Data/Loot/LootTableData.cs`：bundle 品质和条目 questOnly 字段。
- 修改 `Data/ChestDropLoot/CommonChestLoot.asset`、`UncommonChestLoot.asset`、`RareChestLoot.asset`、`EpicChestLoot.asset`、`LegendaryChestLoot.asset`：各只新增对应品质索引。
- 修改 `Scripts/GamePlay/Loot/Chests/LootChest.cs`：总数、精确品质和未知品质计数，重复交互保护与跨局清零。
- 修改 `Scripts/Inventory/Items/Item.cs`、`Scripts/GamePlay/Inventory/InventorySystem.cs`：questOnly 构造/拾取/丢弃透传。
- 修改 `Scripts/Inventory/Core/InventoryGrid.cs`：拖拽原占位保留。
- 修改 `Scripts/Presentation/Inventory/InventoryUIController.cs`：记录原方向、收束拖拽；没有新增 UI。
- 修改 `Scripts/GamePlay/Run/GameSession.cs`：精英计数、结束重入保护、拖拽恢复、冻结快照、统一结算事实来源与 Console 输出。
- 新增 `Editor/LLM/QuestS3Audit.cs`：身份资产配置菜单与纯数据验证菜单。
- 新增 `Editor/LLM/QuestS3PlayAudit.cs`：真实 prefab/Health/交互/拾取/计时结束/死亡验证及证据导出。
- 新增以上五个代码/程序集资产的 `.meta`，以及 `Scripts/Quest.meta`、`Scripts/Quest/Core.meta`。
- 修改 `Docs/LLM事件驱动系统技术方案.md`、`Docs/LLM事件驱动系统施工流程.md`：事实模型、身份实现、阶段分工与状态；修改本复盘记录。
- 新增 `Docs/Evidence/S3/verification.txt`、`victory.json`、`death.json`、`victory.png`、`death.png`：实际运行产物（死亡截图限制见下）。

**验证结果**：

操作（UnityMCP 驱动实际 Editor，菜单也可人工复现）：

1. 在非 Play 模式执行 `Tools/Backpack Survivor/Quest/S3 Configure Fact Identities`，写入五档品质与精英身份；等待编译。
2. 执行 `Tools/Backpack Survivor/Quest/S3 Check Pure Data`。验证旋转回滚、原占位阻止并发拾取、非保留位置仍可拾取、总值 201、questOnly 与快照副本隔离。
3. 直接打开 `Assets/BackpackSurvivor/Scenes/Run/01-Run_ArtFull.unity` 并进入 Play，执行 `Tools/Backpack Survivor/Quest/S3 Begin Full Duration Play Audit`。审计要求无常驻 SaveService，避免修改玩家存档。
4. 真实普通/精英/远程 prefab 经 Health.TakeDamage 死亡，重复伤害不重复计数；同一宝箱实例按池生命周期复用，五档 bundle 各开一次并重复交互；克隆运行时条目执行真实拾取→丢弃→拾取。
5. 审计以 30 倍时间、测试用高生命值跑完场景配置的 900 秒，不跳过计时结束；结束前将物品从网格拖出并旋转，验证自动恢复。订阅独立死亡台账与 OnRunEnded，断言旧结果、快照与网格值一致。
6. 胜利后执行 `Tools/Backpack Survivor/Quest/S3 Check Death And Reset`，验证下一次 StartRun 清零；再次拖拽旋转并经真实玩家 Health 死亡，核对死亡快照；修改返回副本不影响存储的快照。
7. 等待截图写出，读取 Console 错误，退出 Play；不保存运行时场景修改。

结果与证据：

- 纯数据、真实三类死亡、宝箱五档/重复交互/池复用、questOnly 拾取往返、完整计时胜利、死亡拖拽与跨局快照隔离均通过。逐项时间戳见 `Docs/Evidence/S3/verification.txt`。
- 胜利快照：约 900 秒、等级 1、总击杀 3/精英 1、开箱 5，品质桶 `[1,1,1,1,1]`、未知品质 0、金币 0、背包价值 12000。物品“避难所主密钥”为 Legendary、等级 1、questOnly=true（仅测试实例）。完整机器可读字段见 `victory.json`。
- `victory.png` 显示原 ResultView 的 15:00、击杀 3、金币 0、背包价值 12000、传说物品 1，与快照一致；精英、开箱和逐件字段由独立断言核对，旧页面原本没有这些字段。
- 死亡快照：Died、时间 0、等级 1、击杀与开箱均清零，拖拽物品仍在快照内、价值 12000；见 `death.json`。
- Unity 编译和 Console 错误数为 0；Git 差异无空白错误。常规掉落资产只新增品质字段，未改变权重。

**未验证或已知限制**：

- 这是受控集成验证，不能代表原始难度的人工完整游玩或掉落可达性结论；未做 Player 构建。S12 再做概率与压力校准。
- 真实条目 questOnly 标记尚未启用；测试条目只在运行时克隆。未知品质计数路径已实现，本次未在 Play 注入损坏品质资产。
- 重置验证聚焦新计数与快照；直接调用 StartRun 不代表完整的清场/重试流程验收，正式合同重试在 S7/S11 接入。
- 死亡 JSON 与 OnRunEnded 断言通过，但 `death.png` 实际捕获到重置画面，没有显示死亡结果页，不能用作死亡页显示通过的证据；本次未定位该截图时机/同场景重置的表现问题。S11 正式结算界面需单独验收。

**超出范围未做**：判定器、合同抽签/存档推进、questOnly 候选过滤、NPC 对话和任务 UI、波次压力/间隔调整、权重校准。

面试复盘要点：关键问题并非“遍历背包求和”，而是 UI 拖拽暂时改变了数据源。通过原占位保留和结算前事务回滚，让旧结果与新判定输入在同一事实时点生成；随后以真实事件链和可保存快照证明一致性，而不是仅依据编译成功。

下一阶段：S4 · 判定器。消费本阶段真实快照，补齐纯本地条件模型、Evaluate 与 EditMode 边界测试。


## S4 · 判定器（本次实施报告）

**阶段**：S4。

**核心链路**：`QuestEvaluator.Evaluate(quest, snapshot) → QuestOutcome`，纯本地、无 IO、无副作用。

**目的**：让任务达成成为可独立测试的本地真相来源，供后续追踪器与结算表现消费。

**技术选择**：在 `BS.Quest.Core` 中增加条件模型、逐条结果和纯静态评估器；首版 AND，optional 不影响完成，死亡永不完成但记录死亡前条件，精确品质按单桶计数，空任务/未知条件不通过。

**改动文件**：`Scripts/Quest/Core/QuestConditions.cs`（ObjectiveType、ObjectiveClause、QuestInstance、QuestOutcome、ClauseResult）；`Scripts/Quest/Core/QuestEvaluator.cs`（16 类条件评估与进度）；`Tests.meta`、`Tests/EditMode.meta`、`Tests/EditMode/BS.Quest.Tests.asmdef`（首个 EditMode 测试程序集）；`Tests/EditMode/QuestEvaluatorTests.cs`（边界测试）；同步更新施工流程阶段状态。

**验证结果**：
- 操作：UnityMCP `run_tests`，EditMode，程序集 `BS.Quest.Tests`。
- 结果：3 tests，3 passed，0 failed，0 skipped；Unity Console 编译错误修复后为 0。
- 证据：测试作业 `583fcaca17bc469a8e67d8765a7696b0`；测试覆盖 16 种 ObjectiveType、恰好门槛、AND、optional、死亡、空任务、未知类型。
- 边界用例：全部通过。

**未验证或已知限制**：未接入 ScriptableObject 事件定义、运行时合同存档、HUD 或真实 S3 快照回放；Progress01 对排除/背包类条件首版只返回 0/1。

**超出范围未做**：UI、LLM、掉落过滤、营地、存档推进。

下一阶段：S5 · 事件池与抽签器。


## S5 · 事件池与抽签器（本次实施报告）

**阶段**：S5。

**核心链路**：`QuestDatabase` → Core `QuestDrawer` → `QuestInstance`。

**目的**：建立按 tier 分桶、完成去重、前置解锁、近期 tag 降权和确定性随机抽签的本地合同来源。

**技术选择**：ScriptableObject 只负责定义与投影为纯 DTO；Core 不引用 Unity。抽签使用 seed 驱动 `System.Random`，过滤 tier、已完成事件和 `unlockAfter`，近期相同 tag 权重乘 0.25；候选为空返回 null，不伪造合同。接受合同复制条件列表，并首版接收全量 `questOnly` id。

**改动文件**：`Scripts/Quest/Core/QuestConditions.cs` 增加 QuestCandidate/合同文案字段；新增 `QuestDrawer.cs`；新增默认程序集 `Scripts/Quest/QuestEventDefinition.cs` 和 `QuestDatabase.cs`；`Tests/EditMode/QuestEvaluatorTests.cs` 增加抽签边界；同步更新施工流程。

**验证结果**：UnityMCP EditMode 测试程序集 `BS.Quest.Tests`，作业 `6bf38582a38042fcba5687ed6eca686e`：**4 passed / 0 failed / 0 skipped**。覆盖四层过滤、确定性 seed、空候选、合同复制和全量 questOnly 名单。

**未验证或已知限制**：CampaignSave 尚未接入现有 SaveService；没有创建正式事件资产，尚未做连续运行/重启存档验证；tag 当前使用整数占位，后续事件内容确定后可替换为 QuestTag 枚举。

**超出范围未做**：营地 UI、存档推进、questOnly 掉落过滤、LLM 与对话。

下一阶段：S6 · questOnly 过滤。


## S6 · questOnly 过滤（本次实施报告）

**阶段**：S6。

**核心链路**：`LootManager` 会话上下文 → `LootRoller.Roll/ RollBundle` 两条候选路径 → 过滤后的掉落。

**目的**：常规局排除任务局专属物品，合同局允许全量 `questOnly` 池，并保留未来子集配置接口。

**技术选择**：新增 `LootContext`；默认上下文关闭 questOnly；合同上下文允许全量或指定 id。过滤发生在抽签前，常规抽取和保底抽取均使用同一谓词；三个 `TrySpawnDrop` 调用点未修改。`LootManager.SetContractRun` 提供会话边界。

**改动文件**：`Scripts/GamePlay/Loot/Rolling/LootContext.cs`；`LootRoller.cs` 的重载与两条候选过滤；`LootManager.cs` 的上下文持有/设置；新增 `Editor/LLM/QuestS6Audit.cs` 审计菜单；同步施工流程状态。

**验证结果**：Unity 刷新编译，Console 错误 0；S6 审计菜单覆盖常规排除、合同允许和限制 id 三组断言。

**未验证或已知限制**：审计菜单执行日志在 UnityMCP 异步读取窗口为空，尚未留存单独统计文件；未接入真实合同运行时（S7），未做大样本概率统计。

**超出范围未做**：营地、存档推进、对话、波次脉冲、结算汇报。

下一阶段：S7 · 调度营地与合同面板。


## S7 · 调度营地与合同面板（进行中）

本轮先完成 `CampaignSave` 数据结构及 `SaveService.SetPendingQuest/CompleteQuest`，兼容旧存档空值；Unity 编译通过。营地场景、Builder UI、出击入口和真实重启验证尚未完成，故不标记 S7 完成。


## S7 · 调度营地与合同面板（本次实施报告）

**阶段**：S7。

**核心链路**：主菜单 → Builder 生成 Camp 场景 → 本地合同/空背包面板 → 进入 `01-Run_ArtFull`。

**目的**：提供跨局合同循环的本地承载容器，并在未开局时显示空背包事实。

**技术选择**：新增 `CampaignSave` 与 SaveService 合同接口；新增 CampController 和 CampSceneBuilder，UI 使用占位文本，场景纳入 Build Settings；主菜单入口改为 Camp。

**改动文件**：`Scripts/GamePlay/Save/SaveData.cs`、`SaveService.cs`；新增 `Scripts/Quest/CampController.cs`、`Editor/CampSceneBuilder.cs`；新增 `Scenes/Camp/Camp.unity`；修改 MainMenu 场景入口；同步施工流程。

**验证结果**：UnityMCP 刷新编译无错误；执行 Builder 菜单后 Camp 场景文件存在并加入构建列表；入口序列化值为 Camp。

**未验证或已知限制**：尚未在完整持久化运行中人工点击营地按钮并跨场景重启；正式事件资产尚未创建，当前为空合同占位。

**超出范围未做**：S8 对话、S9 追踪器、S10 波次脉冲、S11 结算任务区。

下一阶段：S8 · 营地对话接入。


## S8 · 营地对话接入（进行中）

本轮建立 `BS.Npc.Core` 纯 C# 程序集及 `FactBlockBuilder`、`NpcResponseValidator`、`DialogueRouter`。事实块只读合同/快照，路由固定三个对话面，输出校验拒绝空响应、超长响应和承诺性措辞。DeepSeek 流式请求、工具审计、离线文案 UI 尚未接入，因此暂不标记完成。


S8 追加：新增 `NpcDialogueService`，使用现有配置解析和 UnityWebRequest 调用 DeepSeek，固定 thinking disabled，事实块注入、原始响应和工具审计日志、校验失败离线回退已具备。当前请求仍为无工具的最小文本通道，工具白名单与三种 UI 面板待补。


## S9/S10 · 表现接口（进行中）

新增 `QuestTrackerView`，统一消费 `QuestEvaluator` 结果；新增 `RadioSubtitleView`，订阅现有 `WaveDirector.OnWaveStageChanged(int,string,Color)` 并提供超时字幕。两者已编译通过，尚未绑定正式 Builder 场景，也未接入 S9 的全量实时事件和 S10 的异步 DeepSeek 脉冲请求。


## S11/S12 · 结算推进与埋点（进行中）

`GameSession.EndRun` 现在以冻结快照调用 `QuestEvaluator`，只有胜利且完成才清除 pending 并推进 tier；死亡/未达成保留合同。ResultView 提供可选任务结果文本。新增 `QuestTelemetry` 将结算最小字段写入本地 JSONL；`EnemySpawner` 已放宽间隔校验到只拒绝 <=0，使 0.05 配置生效。尚未完成结算汇报 UI、完整合同资产和 S12 统计采样报告。


S10 追加：新增 `WavePulseService`，订阅阶段事件、延迟 2 秒、使用递增 request token 和 realtime TTL 丢弃过期回调；`RadioPulseReplyView` 负责字幕显示与超时清空。当前回复为本地占位文本，尚未接 DeepSeek。
