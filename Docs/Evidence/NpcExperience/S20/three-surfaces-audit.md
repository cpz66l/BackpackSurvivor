# S20 三面对话联调核对（2026-09-13）

静态链路核对：`WavePulseService` 订阅 `WaveDirector.OnWaveStageChanged`，调用 `NpcDialogueService.RequestPulseReplyAsync`；`ResultView` 订阅 `GameSession.OnRunEnded`，调用 `RequestDebriefAsync`；营地使用同一 `NpcPersona` 配置入口。`RadioPulseReplyView` 名牌已从“调度员”统一为“小芯”。

Unity 刷新/编译成功，控制台 0 错误；EditMode 53/53 Passed，0 failed，0 skipped；job `b26f184ef9f34fb6a8ae0291eebb75fc`。

待人工验收：局内触发一次波次阶段变化，确认短句名牌与营地一致；胜利和死亡各进入一次结算，确认结算只谈本局已确认事实，且不会把死亡说成带出成功。开发审计仍保留原始响应和工具调用。

## UnityMCP 运行时核对

在 Play Mode 中从 MainMenu 点击 `StartButton` 进入 `Camp`，点击 `LaunchButton` 进入 `01-Run_ArtFull`。运行时反射找到 `WavePulseService`、`RadioPulseReplyView`、`ResultView`，`GameSession.State=Running`。首个自动波次请求读取到 `WavePulseService.LastStatus=shown`；脉冲视图在 TTL 后自动清空。随后调用 `WaveDirector.EmitStageForAudit(2, "审计阶段")`，在状态已变化/请求过期时读取到 `expired_or_inactive`，没有晚到内容覆盖当前局面。

这证明入口、异步取消和过期保护实际工作；尚未证明完整胜利/死亡结算的文案质量。
