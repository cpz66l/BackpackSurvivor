# v0.4 世界装备稀有度光柱

实现遵循 `Docs/V0.4美术重构与迭代方案.md`。本模块仅给 `LootManager.dropPool` 生成的世界装备增加展示：敌人掉落、宝箱散落、背包丢弃共用同一个入口。XP、金币与场景装饰不添加稀有度光柱，五级宝箱的既有颜色保持不变。

## 五色与显示规格

| 装备稀有度 | 颜色 RGB | 最高高度 | 最宽处 | 强度 |
|---|---|---:|---:|---:|
| Common | 白 `(1,1,1)` | 2.00 m | 0.36 m | 0.24 |
| Uncommon | 绿 `(0,1,0)` | 2.30 m | 0.40 m | 0.28 |
| Rare | 蓝 `(0,0,1)` | 2.60 m | 0.44 m | 0.34 |
| Epic | 紫 `(0.6,0.2,0.9)` | 2.90 m | 0.48 m | 0.40 |
| Legendary | 红 `(1,0,0)` | 3.20 m | 0.52 m | 0.44 |

以上为实际游戏远视角检查后采用的默认值：提高光柱宽度、高度与强度，让装备在当前相机距离下更清楚。修改仅位于 `V04LootBeaconBuilder` 的三个默认数组；需重新执行构建入口才会写入五份材质。Shader、运行组件、底环半径、Mesh及对象池保持不变。新默认值的构建后画面和运行检查由总集成继续验证。

该映射来自原 `DropItem` 和背包 `ItemView` 的实际颜色代码。光柱使用窄核与向上衰减的软边，底部保留小范围软环，低稀有度更淡。底环半径 0.32–0.42 m。相机距离 45–65 m 之间逐渐淡出，并响应现有场景雾。Shader 不增加实时灯光，也不模拟对周围地面的照明。

## 资源预算

新增资源为 1 个共享 Mesh、1 个解析 URP Shader、5 个共享 Material、1 个 DropItem Prefab Variant：

- 单 Mesh **8 顶点 / 4 三角形 / 1 submesh**，竖向光柱与地面软环各一个 quad。
- 每个活跃装备最多新增 **1 个 MeshRenderer**；30 个可见装备对应 120 个新增三角形。材质槽/Renderer 数不直接等于实测 Draw Calls。
- **0 新贴图、0 Light、0 ParticleSystem、0 Collider**；关闭阴影、光照探针、反射探针和物体运动矢量。
- 所有装备共用五个材质资产；不调用 `Renderer.material`、不复制材质或网格，不创建独立光柱对象池。
- CPU 展示层每帧仅读取已有 Collider 的启用状态并同步 Renderer；没有射线、全场搜索、排序或材质动画写入。
- GPU 负责世界 Y 方向和水平面朝向相机的顶点位置，光柱不会随装备的 180°/秒自转旋转。

源资源与 Unity native 内存估算已在本轮验收时实测保存，不能用上述几何预算代替 FPS 或整机显存实测。相关一次性审计脚本已在仓库整理阶段移除，保留 JSON 结果作为历史证据。

## 接入边界

新变体：`Assets/BackpackSurvivor/Prefabs/Loot/V04/DropItem.prefab`，保留原 `Prefabs/Loot/DropItem.prefab` 为父 Prefab。构建器使用隔离的 PreviewScene / PrefabContents，**不会修改活动场景**。根集成步骤负责将目标场景 `LootManager.dropPool` 的 prefab 引用绑定到该变体，不修改池的预热数量或算法。

`DropItem.cs` 的改动只有：引用 Presentation 命名空间、一个可空 `rarityBeacon` 引用，以及 `Initialize` / `OnGetFromPool` / `OnReturnPool` 中的视觉初始化或重置调用。生成、概率、散落轨迹、计时、拾取、碰撞写入和事件保持原实现。

`LootRarityBeacon` 在 OnEnable 与 OnDisable 清空旧样式。只有 Initialize 给出有效稀有度后才允许显示；散落时读取 DropItem 已经禁用的碰撞器状态以隐藏光柱，落地后恢复。回池时立即隐藏，复用时不会闪出上一件装备的颜色。组件不实现 IPoolable，因为现有池只调用根上的一个 IPoolable 实现。

Shader 以当前平面地图的 world Y=0 为地面基准，光环略抬高 0.025 m 防止闪面。不新增地形采样、不改 120 m 地图尺寸或障碍物布局。

## 构建与历史验收记录

在目标 Unity 项目编译成功后调用：

```csharp
// Edit Mode：只生成或更新资产，返回 Prefab，调用方统一接线。
var prefab = BackpackSurvivor.EditorTools.V04LootBeaconBuilder.ApplyToActiveScene();
```

验收阶段曾在真实装备池中验证五色初始化、共享材质、仅增加一个 Renderer、每级飞行隐藏和回池清理，并沿原 FIFO 池重新取到同一件 Legendary 实例，再初始化为 Common，检查不会残留红色或飞行状态。测试不调用 Collect/Interact，不派发收集事件，不改变库存、概率或保底状态；所有临时租用对象都在 finally 中使用原 `Recycle()` 归还，并恢复 Random.state。

飞行检查覆盖原 0.4 秒协程结束后的 Collider、位置与光柱可见性，并在检查后归还对象。

报告位置均为本目录：`loot-beacon-resources.json`、`loot-beacon-pool-validation.json`、`loot-beacon-flight-validation.json`。历史 JSON 中的 `allPassed` 表示相应运行检查成功；文件存在本身不代表测试通过。

本实现未操作 Unity MCP 或自行保存主场景，实际画面和运行报告由根集成步骤串行完成。
