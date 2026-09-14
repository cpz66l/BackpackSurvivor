# 结算评语布局验证（2026-09-14）

## 问题与修复

旧 Builder 的 Debrief 中心 y=-236、高 32，按钮中心 y=-278、高 88，两者相交；评语禁用换行且使用省略号。QuestOutcome 高 36，却包含整份带出清单。

ResultDialog 调整为 1160×900。统计区、244 高的文本视口、底部按钮独立排列。视口以 VerticalLayoutGroup + ContentSizeFitter 排列评语标题、评语、合同结果及清单；TMP 自动换行，ScrollRect + RectMask2D 负责滚动与裁切。文本不延伸至按钮。

## 操作与实际结果

1. Unity 6000.3.20f1 / MCP，Edit Mode，编译后控制台 0 error。
2. 加载 01-Run_ArtFull，通过 ApplyResultToActiveScene 仅重建结算 UI 并保存。暂停面板未重建。
3. 在生成面板填入临时预览数据：6 行长评语、40 项物品清单。未调用结算、存档或 LLM。
4. 布局读数：内容高 560.03，视口高 244；评语实际高与 preferredHeight 均为 204.01，Normal 换行；滚动到底后内容底部与视口底部误差 0。
5. 调用 ScrollRect.OnScroll，scrollDelta=(0,-30)，归一化位置到 0，anchoredPosition.y=316.03，清单尾项可见，按钮保持位置。
6. 按现有 FitDialog 公式核算缩放后的边界（不是三次真实屏幕切换）：1920×1080 下比例 1、视口与按钮间隔 42；1366×768、1024×768 下比例 0.7822222、间隔 32.85333，面板 907.38×704，均在窗口内。
7. 已查看真实 Game View 预览截图：[长评语](long-debrief.png)、[滚动末尾](scroll-bottom.png)。重复文案及物品用于布局压力验证，不代表实际 NPC 回复。
8. 丢弃临时预览，恢复 MainMenu；仅保存正常默认文案、隐藏状态及新 UI 结构。

首次滚动检查取到未激活视口（内容高 0），不能作为验证证据；明确激活并填充文本后重新完成上述检查。

## 限制

本次没有再跑完整 15 分钟对局，没有新请求 DeepSeek；真实结算时的玩家观感待反馈。仅更新主流程使用的 01-Run_ArtFull 场景；旧 01-Run 场景可通过同一 Builder 更新，本次未改动。
