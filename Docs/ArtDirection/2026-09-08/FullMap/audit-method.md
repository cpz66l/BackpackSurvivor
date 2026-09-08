# 全尺寸场景审核方法

`FullMapArtAudit` 是编辑器工具，只读取当前打开的 `01-Run_ArtFull.unity`，不会打开、保存场景，也不会改变生产刷怪逻辑。

## 场景及资源检查

Unity 菜单 **Tools → Backpack Survivor → Art → Audit Full Recovery Yard** 调用 `FullMapArtAudit.Record()`。编辑状态输出 `unity-fullmap-audit.json`，Play 状态输出 `runtime-fullmap-audit.json`。

检查范围：

- `MapBounds` 半径是否为 60 米，记录圆心、直径、地面碰撞盒及环境可见边界。
- 活动环境的 Renderer、材质槽、去重材质、去重 Mesh、实例三角形数和去重三角形数。材质槽数量不是实测 Draw Call 数。
- 去重环境 Mesh / Texture 的 `Profiler.GetRuntimeMemorySizeLong` 估算，所有引用纹理的资源路径、宽高、格式、可读状态、mipmap、过滤及各自估算。该值不等于整个游戏内存、准确显存或打包后资源大小。
- 源 GLB 总字节与数量。GLB 的磁盘压缩体积和 Unity 解码后的纹理内存分别记录，不能混用。
- 全场景缺失脚本、环境缺失网格、空材质槽、不支持的 Shader；所有环境 Shader 的 `ShaderUtil.GetShaderMessages` 编译错误和警告。
- 所有活动场景灯光，以及其中属于环境根节点的灯光：类型、强度、范围、阴影类型与总数。
- 障碍层碰撞体数量、命名为 Collision 的碰撞体是否错误落在其他层、非障碍实体碰撞体数量、最近障碍物距中心的保守距离。
- `SaveService` 是否存在且其对象未激活。Play 状态另记录游戏状态、已运行秒数、时间缩放和活动敌人数。

地面视觉方形允许延伸至可玩圆形边界之外。障碍边界使用世界轴对齐包围盒作保守检查，旋转物体临边时应结合实际形状复核。可见地面尺寸和可玩尺寸分别记录，避免将旋转后的包围盒误判为地图变大。

## 生成覆盖检查

Unity 菜单 **Tools → Backpack Survivor → Art → Check Full Map Spawn Coverage** 调用 `FullMapArtAudit.CheckSpawnCoverage()`，结果写入 `spawn-coverage.json`。

工具直接读取当前 `EnemySpawner` 的环形距离、占位半尺寸和最大尝试数，调用正式 `SpawnPositionSampler.TryFindInRing`。固定随机种子 20260908，在中心以及距离圆心 57 米的八个方向分别运行 64 次，共 576 次。每次成功结果会额外检查：

1. 出生点与其占位包围圆全部处于原地图边界内。
2. 玩家至出生点的水平距离落在场景配置的环形范围内（原配置 20–30 米）。
3. `Physics.OverlapBox` 未发现 Obstacle 层实体碰撞体重叠。

生成前同步物理 Transform，运行后通过 `finally` 恢复 `UnityEngine.Random.state`，不创建或移动任何对象，不逐条写控制台。若存在任何失败或非法成功，报告 `passed` 为 false，不将空样本算作成功。

此检查验证选定点的边界约束、障碍排除和成功率，不能替代玩家绕行、敌人移动路线、整局游戏或目标硬件 FPS 测试。运行状态的短时检查也不能证明完整十五分钟对局稳定。

## 灯光交互验收

使用固定相机位置分别检查干燥地面、湿区、裂缝、接缝；自定义 `BackpackSurvivor/Environment/Recovery Ground` 应接收主灯、局部灯和阴影，湿区的光照高光应随光线方向变化。对比需要使用同一曝光和相机，不靠切换整体亮度冒充表面响应。局部灯阴影数量与纹理规格须和 JSON 报告一起复核。

最终效果仍需从实际游玩镜头检查：地面纹理不能掩盖掉落物、敌人轮廓、攻击提示；远景细节应由 mipmap 与适当的纹理过滤稳定呈现。更改材质后重新运行场景审核，但不重复宣称旧报告已验证新材质。
