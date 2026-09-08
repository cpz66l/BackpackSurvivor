# V0.4 核心代码保护审计

结论：**通过本次受保护源代码范围检查**。相对本轮开始前保存的 [gameplay-baseline-hashes.json](gameplay-baseline-hashes.json)，66 个 C# 文件中 **64 个 SHA-256 完全一致，只有 2 个获准文件变化**。GamePlay / Inventory / Core 目录没有新增或缺失 C# 文件。逐文件当前哈希与审计时间见 [core-code-protection-audit.json](core-code-protection-audit.json)。

| 目录（`Assets/BackpackSurvivor/Scripts/` 下） | 基线文件数 | 字节完全一致 | 获准变化 |
|---|---:|---:|---:|
| GamePlay | 50 | 48 | 2 |
| Inventory | 10 | 10 | 0 |
| Core | 6 | 6 | 0 |
| 合计 | 66 | 64 | 2 |

## 两项准确差异

`BackpackSurvivor/Assets/BackpackSurvivor/Scripts/GamePlay/Run/GameSession.cs`：

- `PauseRun()`：`private` 改为 `public`，用于 HUD 暂停按钮调用原有暂停逻辑。
- `ResumeRun()`：`private` 改为 `public`，用于暂停菜单继续按钮调用原有恢复逻辑。
- 两个方法的状态守卫、`Time.timeScale` 赋值与 `SetState` 调用内容均未变；没有修改升级、计时、死亡、胜利、掉落概率或结算算法。

完整最小差异：[GameSession.cs.baseline-diff.txt](GameSession.cs.baseline-diff.txt)。

`BackpackSurvivor/Assets/BackpackSurvivor/Scripts/GamePlay/Loot/Drops/DropItem.cs`：

- 新增 `using BS.Presentation` 和一个可空的序列化 `LootRarityBeacon` 引用。
- `Initialize` 末尾新增可空 `SetRarity(lootEntry.rarity)` 调用。
- `OnGetFromPool` 末尾新增可空 `ResetVisual()` 调用。
- 原本为空的 `OnReturnPool` 新增可空 `ResetVisual()` 调用。
- 原球形掉落实体、旋转、60 秒寿命、0.4 秒散落、碰撞器启停、拾取判断、收集事件、池接口与回收行为均未改动。原 `ApplyVisualColor` 的共享材质/MaterialPropertyBlock 分支已经存在于本轮基线中，不是此次新增的核心变化。

完整最小差异：[DropItem.cs.baseline-diff.txt](DropItem.cs.baseline-diff.txt)。

## 基线证明方式

先直接计算全部 66 个当前文件的 SHA-256，与本轮基线 JSON 比较；随后只在内存中逆去上述明确获准改动，还原文件已有的 UTF-8 BOM 和混合 CRLF/LF 换行。两个还原结果都**逐字节匹配基线哈希**，因此不是仅凭 Git HEAD 的差异或人工观察认定没有其他改动。审计过程没有回写受保护脚本。

| 文件 | 逆去获准改动后的 SHA-256，与基线相同 |
|---|---|
| GameSession.cs | `7079F294E4E8B4E6CFAD9106FE5FFA62AB3ADD4E467C4B9498723EFB0CB2DB9B` |
| DropItem.cs | `386F713835AFC35AE35A12FB07ADA31100953DFF1232ACAC73A0C47DA1A5ACB4` |

TXT 差异为了阅读统一显示换行；JSON 中保留真实当前文件哈希。当前文件仍有原来的混合换行，未为审计重排或格式化代码。

## 范围边界

该结果针对上述三个目录的 C# 源代码。Presentation 展示组件、Editor 构建器、场景引用、Prefab 变体、Build Settings 和美术资产属于本轮允许修改的范围，不包含在这 66 份哈希的结论中。地图半径、对象池接线和 UI 行为需要另外核对运行报告；哈希通过不等于完整玩法回归或 15 分钟 Windows 独立包验收。
