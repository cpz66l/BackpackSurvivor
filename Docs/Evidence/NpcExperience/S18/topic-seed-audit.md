# S18 小话题种子回归（2026-09-13）

UnityMCP execute_code 执行 `NpcConversationMemoryAudit.Run()`，实际结果：
`PASS approved topic / rejection branch / greeting / real-input roles / >4 turns / contract transition / fallback continuity / repair context / session isolation`。

Unity 控制台 0 错误；EditMode 53/53 Passed，0 failed，0 skipped；job `58725dd5c1c047688d293e674032d64f`。

本阶段加入六个已批准种子：旧贴纸、门边小石头命名、旧家政习惯、营地声音、修补痕迹、水杯照料。服务为每次营地会话随机选择一个种子，只将该种子的情境、态度、可展开细节与世界边界注入模型。玩家说“换个话题/不想聊”等拒绝后，状态标记为 rejected，后续请求明确禁止再次推送；状态只在内存，不写行动存档。

尚未进行真实 UI 三分支人工采样：需要分别验证接纳、拒绝/逗弄、换话题三条路径，以及真实回复是否确实因玩家回应变化。
