# S17 营地上下文反馈验证（2026-09-13）

## 注入式服务回归

通过 UnityMCP execute_code 执行 `BackpackSurvivor.EditorTools.NpcConversationMemoryAudit.Run().GetAwaiter().GetResult()`。
实际结果：`PASS greeting / real-input roles / >4 turns / contract transition / fallback continuity / repair context / session isolation`。
检查的是实际 NpcDialogueService 发给 INpcTransport 的 messages，不是只检查 UI 文本。合同变更测试覆盖服务通知；实际点击重抽按钮仍待人工复测。

Unity 控制台编译错误 0。EditMode 53/53 Passed，0 failed，0 skipped；job `1b44d8e445924c008fee1550f59f571f`。

## 真实模型样本

使用当前 NpcPersona 资产与默认 DeepSeek 配置、独立服务实例，在 Editor 内顺序发送三次请求。无存档或场景修改；未记录密钥。不是 UI 试玩结果。

1. 系统开场回复（failure=null）：主人回来啦。我把你今天坐的那块垫子拍了拍灰，水也温着——先歇会儿？
2. 玩家：我们把门口的小石头临时叫作咕噜，只在这次聊天里这样叫。
   回复（failure=null）：好，这次就叫它咕噜。名字是临时的，我记在这儿，不往档案里写。它今天有没有挪窝？还是依旧守着门口那块地。
3. 玩家：刚才我给它取了什么名字？顺便接着你开场时说的那件事聊聊。
   回复（failure=null）：刚才叫咕噜，只算这次聊天里的临时叫法。门口那块垫子我拍过灰了，水也还温着；你要坐就坐，咕噜继续守它的门。用户今天是不是有点累？

观察：找回临时名字并承接开场的垫子和温水；不是逐字记忆（将垫子位置补写为门口）。这些即兴生活细节不写入行动记录。仅此一个模型样本，不能代表所有话题的回忆质量。

## 玩家复测

重新进入营地，接着开场中的具体小事追问；临时起名后换两个话题，再问刚才的名字；重抽合同后再次追问。退出营地再进入不应承诺仍记得临时名字。
