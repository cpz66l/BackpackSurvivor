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


## S6 验收纠偏 · 真实资产与候选过滤

**阶段**：S6 返修与验证，2026-09-12。

**核心链路**：真实 LegendaryBonusDrops 标记 → 冻结合同名单 → 常规/保底候选过滤 → Epic/Legendary 宝箱束掷骰。

**目的**：修复“接口存在但真实资产未启用”和“空上下文放行”两处缺口，取得与原权重保持一致的实测证据。

**技术选择**：只标记设计明确的方舟计划核心、星火反应炉；其余三件传说仍常规。LootContext 保存 HashSet 副本并对外只读，缺失名单关闭权限；两条候选路径均把 null context 视作普通局。LootManager 初始化提前到 Awake，避免 GameSession.Start 与管理器 Start 顺序不确定。保留全部权重与既有保底行为。

**改动文件**：

- `Scripts/GamePlay/Loot/Rolling/LootContext.cs`：冻结名单，移除空名单通配。
- `Scripts/GamePlay/Loot/Rolling/LootRoller.cs`：null 上下文关闭权限，束中的空 channel 保护。
- `Scripts/GamePlay/Loot/Rolling/LootManager.cs`：Awake 初始化。
- `Data/EquipDrops/LegendaryBonusDrops.asset`：两个 questOnly 标记，权重仍 35/25/20/10/10。
- `Editor/LLM/QuestS6Audit.cs`：真实资产、参考序列、分布和保底审计，恢复 Random.state，销毁临时表。
- `Docs/Evidence/S6/verification.txt`：固定种子完整输出。
- 两份方案与本记录：澄清名单语义，纠正先前基于接口/编译的完成标记。

**验证结果**：

- UnityMCP 在非 Play 模式先执行 `Tools/Backpack Survivor/Quest/S6 Configure QuestOnly Assets`，再执行 `S6 Check QuestOnly Filter`；编译无错误，逐项断言通过。
- seed=20260912。每种宝箱/上下文各 10000 次：普通局 Epic/Legendary 的任务物品均为 0；合同局分别 748、3633。
- 与保留原权重和通道概率的参考表对照，40000 次宝箱输出序列逐次一致；常规传说池的剩余权重比为 35:20:10，样本分布在断言容差内。
- 保底用真实 Common 未命中累积触发：普通局任务命中 0/1000，合同局 990/1000；null、空名单、未知 id、全部过滤与名单防外部修改均验证普通/保底两条分支。
- 证据：`Docs/Evidence/S6/verification.txt`。无网络调用、无玩家存档写入、无场景修改。

**未验证或已知限制**：本次测试是真实掉落配置与掷骰器，未把脚本掷骰称为人工开箱游玩。营地目前还不能生成真实合同，S7 完成前不宣称玩家入口闭环已通。S4/S5/S7 的此前完成报告有验收缺项，状态表已纠正，后续继续补齐。

**超出范围未做**：S12 难度/可达性校准；没有修改任何现有掉落权重，没有定向保底，没有把合同模式的空名单视作全量池。

## S7 验收补齐 · 合同发放与营地往返（2026-09-13）

**阶段**：S7 · 调度营地与合同面板。

**核心链路**：本地事件池 → 抽签并冻结条件 → 成功写档 → 营地面板 → 出击注入合同及全量任务物品池 → 暂停/死亡返回营地 → 重试或重抽。

**目的**：补齐此前仅有空合同容器的缺口，证明玩家能沿实际按钮路径连续出击，并在重启后保留同一合同。

**技术选择**：沿用 Editor Builder 和项目中文字体；15 个本地占位事件（5 层、每层 3 个），不作为最终内容或难度结论。接受合同深拷贝条件及集合，避免修改 SO 或原候选。抽签记录最近五次事件并降低重复权重；重抽不限次数。接受合同采用临时文件 Flush/Replace 成功后再替换内存。验收通过 Editor SessionState 指定临时存档并跨 Play 重启，不读写玩家真实存档；临时启用后台帧后恢复。

**改动文件**（以下路径相对 `BackpackSurvivor/Assets/BackpackSurvivor/`，新增资产均含 Unity meta）：

- `Scripts/Quest/Core/QuestConditions.cs`、`QuestDrawer.cs`：冻结 tag/isFinal、深拷贝、重复降权及非法权重过滤。
- `Scripts/Quest/Core/ObjectiveText.cs`：16 种条件的本地中文表达，开箱品质精确计数。
- `Scripts/Quest/QuestDatabase.cs`：读取全量 questOnly 资产池并提供候选。
- `Editor/QuestCatalogBuilder.cs`、`Data/Quest/`：建立数据库及 15 个可本地维护的占位定义；已有数据库不会被重建覆盖。
- `Scripts/GamePlay/Save/SaveData.cs`、`SaveService.cs`、`CampaignFile.cs`：抽签状态、接受合同写档接口和测试隔离路径。
- `Scripts/Quest/CampController.cs`、`Editor/CampSceneBuilder.cs`、`Scenes/Camp/Camp.unity`：可出击、重抽、返回的营地；未开局背包为空，UI 使用新输入系统。
- `Scripts/Presentation/Pause/PauseMenuView.cs`、`Result/ResultView.cs`：重试及结算返回营地。
- `Editor/V04MainMenuArtBuilder.cs`、`V04RunMenusArtBuilder.cs`，`Scenes/MainMenu/MainMenu.unity`、`Scenes/Run/01-Run_ArtFull.unity`：入口和按钮文案；场景由 Builder 重建保存，生成对象 ID 带来较多 YAML 差异。
- `Art/Font/SourceHanSansCN-Normal SDF.asset`：实际 UI 新增中文字符的动态字形。
- `Editor/LLM/QuestS7Audit.cs`、`Docs/Evidence/S7/`：自动化实景验收入口、日志、营地截图及恢复存档。
- 技术方案、施工流程、本记录：同步实际合同存储结构和验收状态。

**验证结果**：

- 非 Play 模式打开已保存的 Camp，执行 `Tools/Backpack Survivor/Quest/S7 Verify Camp Roundtrip`。
- 2026-09-13 00:00（北京时间）的最终复验全部通过：15 个唯一事件、各层 3 个、两个任务专属 ID；接受后修改条件/标签集合不影响候选。
- 初次进营地自动抽签并写档，原格式存档的 totalRuns=77、totalGold=123 保留；通过 LaunchButton 进入 ArtFull，逐字段对比接受实例一致且 LootManager 合同模式启用。
- 实际暂停菜单 Restart 按钮返回营地并保留原条件；点击 RedrawButton 后 drawCount 加一，再次出击；真实 Health 致死后本地判定未完成且 pending 保留，ResultDialog 的 Restart 按钮返回营地。
- 退出并再次进入 Play，合同完整条件与 seed 未变；测试覆盖写入目标被目录占用时失败并保留原目标。
- 证据：`Docs/Evidence/S7/verification.txt`、`camp.png`、`restored-save.json`。已目视检查营地截图，正文和按钮无重叠、中文可读。最终审核标记关闭、测试存档覆盖路径清除，Unity 已退出 Play；Console 无错误。

**未验证或已知限制**：Play 重启是存档加载验证，不等同于重启 Unity 进程或发布构建测试。S4 的全部失败边界、S5 连续胜利推进尚待补齐；现存 CompleteQuest 结算写档尚未做幂等与失败回滚，归 S11 修复。首次场景重建后的复验停留在初始化，退出 Play、完成编译并重新运行后通过；后台帧设置已纳入审计脚本。事件门槛未做 S12 可达性校准，不能作为平衡结论。

**超出范围未做**：本阶段没有接入 DeepSeek、波次字幕或结算汇报，没有新增美术或更改常规掉落权重。新增本地事件资产与接受接口是营地发放链路的缺失前置；不据此把 S5 全部标为完成。

## S4 验收补齐 · 判定器失败边界（2026-09-13）

**阶段**：S4 · 判定器。

**核心链路**：`QuestEvaluator.Evaluate(quest, snapshot) → QuestOutcome`，纯本地、无 IO、无副作用。

**改动文件**：`Scripts/Quest/Core/QuestEvaluator.cs` 增加条件字段校验；计数目标拒绝小于等于 0，物品 id、标签集合、任一物品集合、等级和宝箱品质缺失时均失败；进度计算与判定共用校验。`Tests/EditMode/QuestEvaluatorTests.cs` 增加无效字段失败边界。

**验证结果**：UnityMCP 启动 EditMode 测试作业 `d6c53de178ab471080f2d32ab19de78d`，结果 `Passed`，共 5/5，通过耗时 0.3639 秒。覆盖 16 种条件的正常达成、AND/可选/未知/空合同、死亡局保留条件满足状态、精确宝箱品质以及零计数、空 id、空集合、Unknown 品质、非法等级等失败边界。

**未验证或已知限制**：测试快照为纯 C# 构造，S3 真实快照回放仍需单独加入固定样本；`Progress01` 对物品类条件仍只提供 0/1 结果，属于表现层后续优化，不影响本地完成判定。

**超出范围未做**：未修改战斗、掉落、存档或 UI；未接入 LLM。

## S5 验收补齐 · 事件池连续抽签与存档往返（2026-09-13）

**阶段**：S5 · 事件池与抽签器。

**核心链路**：`QuestDatabase → QuestDrawer → QuestInstance → CampaignSave`。

**验证结果**：UnityMCP 直接调用 `QuestS5Audit.Run()`，读取真实 `Data/Quest/` 资产。审计按 tier 1 至 5 连续抽取，每层均得到该层候选；每次接受均冻结 seed、definitionVersion、条件和全量 questOnly 名单。已完成事件再次抽签被过滤，最后将 5 个已完成事件及 pending 合同经 `JsonUtility` 序列化并反序列化，字段完整恢复。证据见 `Docs/Evidence/S5/verification.txt`。

**未验证或已知限制**：这是 Editor 级连续流程审计，不模拟五次真实胜利结算；由 S11 负责将真实胜利结果以幂等方式写入 completed/tier。终局事件的可达性留待 S12 采样。

**超出范围未做**：不接 LLM，不做正式内容与美术，不改掉落权重。

## S8 实施记录 · 事实块、工具审计与安全回退（2026-09-13）

本次把 NPC 服务从无工具请求改为包含 `read_contract_facts` 的只读工具轮：客户端只接受无参数的白名单调用，将本地事实块作为 tool result 回填，再校验最终文本；原始响应和最终响应均掩码 API Key 后写入审计日志。事实块明确区分当前合同、当前局面和未开局空背包，输出校验拒绝承诺完成、直接发放、跳过任务及修改掉落/存档等表述。新增 2 个 NPC Core EditMode 用例，当前全套测试 7/7 通过。

本地 Unity 环境确认 `DEEPSEEK_API_KEY` 可见；一次真实请求审计按失败安全路径回落到离线简报，尚未把该次调用标记为成功。当前缺口是完整流式分片 UI、营地输入面板绑定，以及需留存一次真实工具调用后最终回答的网络证据。因此 S8 仍为进行中。

## S9 实施记录 · 事件驱动局内追踪器（2026-09-13）

`QuestTrackerView` 已移除每帧 `Update` 刷新，改为订阅 GameSession 的状态、时间、经验事件，以及 LootChest 开箱和 DropItem 拾取事件；启动时做一次初始化快照。显示逐条本地 objectiveText、完成勾选、总体进度，死亡结算明确显示本局失效。LootChest 新增只读 `OnOpened(ChestQuality)` 事件，保留原有计数语义。

验证：UnityMCP 编译无错误，EditMode 作业 `50478d61f1d0454689238855b946e5f7` 结果 7/7 Passed。已验证核心纯函数和事件订阅编译链；尚未在 Builder 生成的正式 Run HUD 中截图人工拾取全过程，故 S9 状态仍为进行中。

## S10 实施记录 · 波次脉冲接入 DeepSeek（2026-09-13）

`WavePulseService` 继续订阅现有 `WaveDirector.OnWaveStageChanged(int,string,Color)`，延迟后构造当前阶段事实并调用 NPC 服务的 Pulse 路由；无历史、不写营地/结算记录，使用 token、阶段编号与 realtime TTL 丢弃过期回调。Pulse 路由上限为 60 字，失败回落本地阶段文案。编译验证无错误；尚未完成打满一局的 4 次阶段切换和人为慢响应截图，S10 仍为进行中。

## S11 实施记录 · 结算完成写档原子性（2026-09-13）

`GameSession.EndRun` 继续只使用冻结的 `QuestRunSnapshot` 做本地判定；只有 `QuestOutcome.Completed` 时调用新的 `SaveService.TryCompleteQuest`。该接口先深拷贝存档，在临时文件 Flush 后 Replace 成功时才替换内存并清除 pending、记录 completed、推进 tier；同一事件重复提交直接幂等返回，不会重复推进。写档失败保留 pending，且只输出错误摘要。Unity 编译验证无错误。

结算页已有本地任务结果入口和返回营地按钮，但逐件物品高亮、完整汇报请求及四种真实结算场景仍待补齐，S11 尚未完成。

## S12 实施记录 · 结算埋点字段补齐（2026-09-13）

`QuestTelemetry` JSONL 行新增 eventId、seed、eliteKills、各品质宝箱数组、任务专属物品数量等字段，保留 outcome/tier/kills/chests/backpackValue/completed；写入仍在结算冻结快照之后，失败只记录警告，不参与判定。`EnemySpawner.ApplyWaveSettings` 当前已只拒绝 `spawnInterval <= 0`，因此 0.05 秒配置可生效。

Unity 编译请求已发出，当前未采集足够真实局数，尚不能给出可达性统计报告或调整配置，S12 仍为进行中。

## S11 追加 · 结算逐件物品呈现（2026-09-13）

结算页任务区现在消费冻结快照，列出带出物品的 id 与等级，并以星标突出 `questOnly` 物品；空背包明确显示为空。该列表与本地判定使用同一份 `LastQuestSnapshot`，不受 LLM 文案影响。Unity 编译验证无错误。结算汇报异步请求和完整四类结算实景验收仍待补齐。

## S8 追加 · 营地输入面板接入（2026-09-13）

Camp Builder 新增中文文本输入框和回复区，回车调用 `NpcDialogueService.RequestCampReplyAsync`；离线回复仍保留，玩家输入只进入对话服务，不触发任何状态、背包或存档修改。CampController 对输入回调在销毁时解除。Builder 已重新生成并保存 Camp 场景，UnityMCP 编译无错误。

S8 仍待完成真实流式分片显示、工具调用后的最终回答网络证据和超时/诱导测试。

## S8 验收纠偏 · 真实工具回填与营地流式交互（2026-09-13）

**阶段**：S8 · 营地对话接入。

**核心链路**：营地输入 → 本地预分类 → 冻结事实 → 白名单工具请求/执行/回填 → DeepSeek 流式最终 JSON → 按句字段绑定 → UI；失败保留本地事实文案，出击可取消请求。

**目的**：把此前只会回退且缺少错误证据的占位服务，替换成在 Unity 内实测成功、可观测、可取消的真实对话链路。

**技术选择**：

- 按消息类型组装 JObject，避免无关的空工具字段进入请求。复用已安装的 Newtonsoft JSON 3.2.2，无新增包。
- 工具轮非流式且 required；客户端验证工具白名单、参数、调用 id 和最多六个调用，按顺序回填。最终轮不携带工具定义，关闭 thinking，使用 JSON Output；营地最终轮 SSE 经严格 UTF-8 Decoder 处理。
- 模型只输出目标索引和字段引用，数量/单位/物品名最终由本地替换；不采用“只要数字出现过就放行”的弱校验。objectiveEcho 先于 text，完整句通过校验才回调 UI；未通过的尾部降级，已显示安全句不撤回。
- 一个营地会话复用一个服务及历史；重抽、离场销毁或取消。服务层执行轮次/总 token 上限，按实际 usage 计数，缺失 usage 时保守预留。初始 HTTP 失败最多重试一次，最终轮不重试。
- `NpcPersona.asset` 本地维护离线、受限话题、收尾及 Mock 文案；Editor 默认 Mock，关闭 `useMockInEditor` 后使用 DeepSeek，发布构建走正常密钥解析。入口自动发起开场，首句到达前保留本地简报。
- 玩家只看到文本；开发审计面板单独显示脱敏的请求、响应、实际工具执行和回填顺序。文本不启用富文本，审计使用滚动区域。

**改动文件**（前缀 `BackpackSurvivor/Assets/BackpackSurvivor/`，新增均含 meta）：

- `Scripts/Npc/NpcTransport.cs`：真实 UnityWebRequest/SSE 传输与可注入测试边界。
- `Scripts/Npc/NpcDialogueService.cs`：三面接口、协议组装、只读工具、历史/额度、句缓冲、降级、审计与取消。
- `Scripts/Npc/Core/NpcCore.cs`、`BS.Npc.Core.asmdef`：纯 C# 事实块、路由、字段绑定；读取现有 ItemTag/Rarity 因而新增 BS.Inventory 引用，仍无引擎引用。
- `Scripts/Npc/NpcPersona.cs`、`MockNpcDialogue.cs`、`Data/Quest/NpcPersona.asset`：本地维护的无网络响应。
- `Scripts/Quest/QuestDatabase.cs`、`Editor/QuestCatalogBuilder.cs`、`Data/Quest/QuestDatabase.asset`：只读物品定义目录，不向模型提供权重或掉落来源。
- `Scripts/Quest/Core/ObjectiveText.cs`：复用本地中文品质和类别名称。
- `Scripts/Quest/CampController.cs`、`Editor/CampSceneBuilder.cs`、`Scenes/Camp/Camp.unity`：会话生命周期、输入/滚动回复、开发审计；取消不阻塞出击。
- `Art/Font/SourceHanSansCN-Normal SDF.asset`：新增界面中文字形。
- `Tests/EditMode/NpcBindingTests.cs`：越权输入、数值改写、未知引用、富文本和本地字段隔离。
- `Editor/LLM/QuestS8Audit.cs`、`QuestS8SafetyAudit.cs`、`QuestS8UiAudit.cs`：真实请求、故障注入和实景验收入口。
- `Docs/Evidence/S8/`、两份方案、本复盘记录：证据与实现约定同步。

**验证结果**：

- 实际 Unity 请求：4 个工具调用，64 个 SSE 分片，首句 1460.5ms，1663 tokens。操作：`Tools/Backpack Survivor/LLM/S8 Verify Camp Tool Roundtrip`。证据：`verification.txt`、`sentences.txt`、`audit.txt`。这是一次样本延迟，不作为长期 SLA。
- 真实营地 UI：使用临时存档及临时 Live 覆盖，自动开场成功；调用实际输入框 onSubmit 后完成第二轮；合同内容未改；开发面板可显示和滚动审计；越权输入走本地化解；请求未完成时点击出击，ArtFull 已 Running 且合同一致。证据：`ui.txt`、`camp-live.png`、`camp-reply.png`、`camp-audit.png`，已目视确认布局与中文可读。测试触发 UI 事件，不冒称物理鼠标/键盘人工操作。
- 错误密钥/连接失败：真实 UnityWebRequest 分别访问 DeepSeek（测试假 Key，401）和本机无服务端口（status 0）；两者均退回本地文案，真实 Key 未发送到测试本机地址。证据：`network-failures.txt`、`unauthorized.txt`、`connection-failure.txt`。
- 服务故障注入：14 组通过，覆盖未知工具、错参数、循环、错误目标索引、裸数字、残缺流、verdict 不一致、受限输入、轮次/token 额度、历史顺序、执行中取消。证据：`safety.txt`。故障注入通过同一个 NpcDialogueService，不把 Mock 结果当作真实网络成功。
- EditMode 最终作业 `c4c9e03d71814529b9987c3db41a2492`：25/25 Passed；其中 18 个新增 NPC 字段/路由用例。证据：`editmode.json`。
- Editor 默认 Mock 下重新执行 S7 完整往返：出击、暂停重试、重抽、真实死亡、返回营地、Play 重启恢复全部通过（`Docs/Evidence/S7/verification.txt`）。该验收不用网络，也未使用玩家实际存档。

**未验证或已知限制**：

- 并未穷尽所有自然语言内容风险；实现为人设规则、输入预分类、只读工具、字段约束和禁词校验的组合，仍需后续内容回归。没有声称大规模统计或发布构建验证。
- 此次没有重新运行 900 秒胜利局；离线情况下的死亡结算及营地往返已测。S9 实景进度、S10 全波次及慢响应、S11 四种结算和可靠推进、S12 采样报告仍不算完成。
- S4 的既有“16 类型通过/失败都覆盖”表述经再次核对不成立：原聚合测试主要验证通过路径。状态表已恢复为待补齐，不让新增加的 NPC 测试掩盖该缺项。

**超出范围未做**：本次不调整权重、不新增美术、不改任务达成权、不引入中转服务；Pulse/Debrief 共用的服务接口已提供，但未据此宣称 S10/S11 玩家链路完成。

## S4 最终验收 · 全类型失败边界（2026-09-13）

新增 `EveryObjectiveTypeHasARealFailureBoundary`，对 16 种 ObjectiveType 分别构造实际未满足快照，并额外验证空快照；覆盖缺失物品/标签、品质与等级过高、价值上下界、排除条件、击杀、精确宝箱品质、等级和存活时间。UnityMCP EditMode 作业 `115db7abb85a44cc9255ac8bdd8366f6`：26/26 Passed，0 failed，0 skipped，0.7755 秒。此前报告中“全类型失败边界”措辞曾超出证据范围，本条以该作业为新的依据恢复 S4 完成状态。

## S9/S11 增量修正 · 追踪刷新与结算合同冻结（2026-09-13）

`QuestTrackerView` 继续采用事件驱动刷新：时间事件按整秒去重，并补订阅敌人死亡事件，避免每帧重复执行判定；场景退出时解除全部订阅。`GameSession.StartRun` 深拷贝待执行合同，`EndRun` 只使用该局冻结副本进行本地判定、推进与埋点，防止结算期间营地存档变化污染结果。结算页异步汇报请求增加取消令牌，切换场景会取消未完成请求；本地判定文本和物品清单先行显示。

UnityMCP 刷新后 Console 错误数为 0；S7 完整往返证据仍为 PASS。S9 实景 HUD 截图、S10 全波次脉冲、S11 四种结算矩阵、S12 统计采样尚未完成，不能据此提前关闭阶段。

## S10 增量修正 · 脉冲会话绑定（2026-09-13）

波次脉冲请求现在记录 `GameSession` 实例身份、阶段索引、请求序号和取消令牌。场景退出或新阶段到来时，旧请求会被取消或丢弃；延迟、网络返回和 TTL 检查均在展示前执行，避免上一局或过期阶段的文案进入当前字幕。UnityMCP 刷新后 Console 错误数为 0。全波次实景采样仍待补做。

## S12 初始统计 · 掉落可达性基线（2026-09-13）

根据 S6 固定种子样本整理 `Docs/Evidence/S12/statistics.md`。样本证明普通局 questOnly 为 0、合同局可达，且没有改动权重或引入定向保底；由于尚无真实胜利/失败局 JSONL 汇总，本阶段仍为初始基线，不宣称完成可达性校准。

## S11 结算矩阵 · 判定边界（2026-09-13）

新增 `SettlementMatrixRequiresVictoryAndCompletedObjectives`，验证胜利且目标完成、胜利但目标未完成、死亡但条件已满足、死亡且条件未满足四种情况；只有胜利且目标完成返回 `Completed`。UnityMCP EditMode 作业 `23eaac20b1564caf8371c47161045f2d`：27/27 Passed。该作业验证判定矩阵，不替代真实场景结算往返。


## S9 用户反馈修复 · 合同追踪生命周期（2026-09-13）

**核心链路与目的**：营地出击 → StartRun 绑定合同 → HUD 目标刷新，修复实际有合同却一直显示“暂无进行中的合同”。

**技术选择**：每次刷新重新获取 CurrentQuest，不再在 Start 缓存可能尚未赋值的 null；订阅状态变化与 InventoryGrid.OnChanged，在暂停整理、丢弃物品时也刷新；敌人死亡延后一帧读取，消除订阅顺序影响。

**改动文件**：QuestTrackerView.cs；QuestTrackerFeedbackAudit.cs 与 meta；Docs/Evidence/Feedback/tracker.txt、tracker-target.png、tracker-death.png。

**验证结果**：UnityMCP 调用 S9 Verify Tracker Feedback。实际 Camp→Run 合同绑定通过；注入两种初始化顺序、暂停时添加/移除物品、死亡后失效文案和保留 pending 均通过，证据见 tracker.txt。测试使用临时独立存档。

**未验证或已知限制**：物品和死亡通过测试注入，不能替代人工完整胜利局或所有目标类型实景验收。

**超出范围未做**：此提交不改 NPC、配置面板和掉落权重。


## S2 用户需求增补 · 主菜单 AI NPC 设置（2026-09-13）

**核心链路与目的**：主菜单入口 → 开关/模型/额度草稿 → 显式保存或 DeepSeek 自检。开发默认开启，模型 deepseek-flash，thinking 关闭，密钥仍优先读环境变量。

**技术选择**：沿用 Editor Builder；新增 npcEnabled 与 model，旧 JSON 在默认对象上覆盖以保留新增字段默认值。自检使用草稿且验证 ok=true，不提前写盘；关闭面板中止请求；密码字段隐藏并不回填密钥。

**改动文件**：LlmModelConfig.cs、LlmConfigService.cs、NpcConfigView.cs、LlmConfigPanelBuilder.cs、MainMenu.unity、动态中文字库；NpcConfigFeedbackAudit.cs 与 meta；Feedback/config.txt、mainmenu-ai-config.png；两份方案同步。

**验证结果**：UnityMCP 调用 S2 Verify Config Feedback。首次入口打开、旧配置迁移、开关/自定义模型保存后重开、非法模型拒绝、恢复默认、密码掩码、环境变量优先全部通过；真实 deepseek-flash 自检返回成功，确认自检不改已保存配置。使用独立临时配置和存档路径，截图已目视检查，无字段遮挡。

**未验证或已知限制**：尚未测试发布构建；此提交仅完成配置基础设施，三个对话面对开关与模型的读取在紧接的 S8 修复中验证。

**超出范围未做**：不新增供应商或可变 API 地址，不将密钥写入仓库。
