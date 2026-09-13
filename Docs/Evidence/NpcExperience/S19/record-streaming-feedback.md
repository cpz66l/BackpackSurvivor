# S19 最高记录与营地逐字输出反馈修复

## 原因与最终方案

前次实现仅在事实 JSON 加入 bestBackpackValue；get_progress 工具并不返回它，引用解析器也不支持该字段，直接说数值会触发旧数字/机制词校验。最高带回价值问法又没有进入专门的最高纪录提示。修复早期的真实模型样本进一步暴露：当前合同非空时，最高纪录仍受 objectiveEcho 的无关任务字段校验影响。

最终链路：两种原话 -> IsBestRecordQuery -> 只读 get_player_records -> SaveData.bestBackpackValue -> [[record:0]] 本地替换 -> 专用 {text} 协议 -> 营地打字队列。该查询不注入任务目标或最近几趟来代替全局最高纪录。未知与零区分；模型漏引用或编造数值时，本地回退仍明确给出存档纪录。

自然聊天从收到 SSE 后按已通过检查的句子增量发布，JSON/引用不显示；营地将这些增量缓存在当前请求队列，按 45 个文本元素/秒输出。字符切分按 StringInfo 文本元素进行，不切断 emoji 代理对；使用非缩放时间。开场和手动问答均接入；退出、禁用、销毁取消队列。最终完整响应到达时等待队列播放完，不一次性补出整段。

## 可复现验证

1. UnityMCP 刷新编译，控制台 0 错误。
2. EditMode：58/58 Passed，job `45ff511b20794717a7ed82ba584660fd`。
3. execute_code：`BackpackSurvivor.EditorTools.NpcRecordStreamingAudit.Run().GetAwaiter().GetResult()`，通过两种原话+非空合同、工具来源、引用替换、零/缺失、错误数值回退、SSE 在 DONE 前发布、Unicode 解码、不完整流保留片段、UI 单字递增/emoji/取消。
4. 原 `NpcConversationMemoryAudit.Run()` 仍通过。
5. 只读加载玩家现有 save_data.json，给独立 NpcDialogueService 注入同一数值与当前合同，使用当前 NpcPersona 和 DeepSeek 设置进行真实请求。不改玩家存档、不启动一局。

最终真实样本在 `record-streaming-live.json`：两种原话均返回 ￥105,750、toolCount=1、failure=null、fallback=false；分别收到 20/19 个 SSE chunk，首片段约 1579/1616 ms，早于完整响应约 1680/1727 ms。追加普通闲聊收 75 个 chunk、5 个片段回调，首片段约 931 ms、完成约 1634 ms；三次最终文本均与增量拼接一致。

初轮未成功样本也保留在 `record-streaming-first-attempt.json`，两次 objective_echo_mismatch 使用正确本地回退；最终协议已解除此无关校验。

## 限制

模型服务测试与隔离组件测试不能替代用户对现有 Camp 场景中逐字速度的观感确认。首个可显示片段需等待网络和句子检查；不是原始 JSON/token 无校验直出。此次未修改其他历史检索、存档写入与波次/结算生命周期，也不宣称 S16–S20 全部体验要求完成。
