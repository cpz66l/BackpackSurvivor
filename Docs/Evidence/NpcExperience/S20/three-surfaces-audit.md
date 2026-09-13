# S20 三面对话联调核对（2026-09-13）

静态链路核对：`WavePulseService` 订阅 `WaveDirector.OnWaveStageChanged`，调用 `NpcDialogueService.RequestPulseReplyAsync`；`ResultView` 订阅 `GameSession.OnRunEnded`，调用 `RequestDebriefAsync`；营地使用同一 `NpcPersona` 配置入口。`RadioPulseReplyView` 名牌已从“调度员”统一为“小芯”。

Unity 刷新/编译成功，控制台 0 错误；EditMode 53/53 Passed，0 failed，0 skipped；job `b26f184ef9f34fb6a8ae0291eebb75fc`。

待人工验收：局内触发一次波次阶段变化，确认短句名牌与营地一致；胜利和死亡各进入一次结算，确认结算只谈本局已确认事实，且不会把死亡说成带出成功。开发审计仍保留原始响应和工具调用。
