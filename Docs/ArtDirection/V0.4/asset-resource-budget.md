# V0.4 美术资产资源说明与光柱审查

本说明区分新增源文件体积、格式对应的纹理像素载荷以及 Unity 已记录的 native 内存估算。三者都不等于最终 Windows 包大小、总显存或帧率。精确尺寸、文件 SHA-256、导入元数据和资源逐项字节数见 [asset-resource-audit.json](asset-resource-audit.json)。

## 本轮新增资源

| 资源 | 源文件 | 运行像素/几何预算 |
|---|---|---|
| `Art/UI/V04/InventoryFrame.png` | 512×664 RGBA，154,622 bytes，151.0 KiB | Windows BC7、无 mip：339,968 bytes，332 KiB |
| `Art/UI/V04/MainMenuBackground.png` | 1024×576，837,801 bytes，818.2 KiB | Windows BC7、无 mip：589,824 bytes，576 KiB |
| `Art/UI/V04/SolidWhite.asset` | 共用 1×1 Texture + Sprite，序列化文件 5,727 bytes | RGBA32 像素数据 4 bytes；对象元数据另计 |
| 装备稀有度光柱 | Shader、共享 Mesh、5 个材质和 DropItem Prefab Variant 合计20,182 bytes，约19.7 KiB；不含 `.meta`/C# | 1 个共享 Mesh，8 顶点、4 三角形；每个可见装备新增1个 MeshRenderer |
| `Prefabs/UI/V04/ItemView_V04.prefab` | 变体文件12,926 bytes | 复用原物品图标与星级图；新增色边使用现有 UGUI 圆角几何 |
| HUD、暂停、结算和菜单按钮 | 原生 UGUI、思源黑体 SDF、现有 `UpgradeRoundedGraphic` | 不生成各按钮的大图，不新增字体或材质副本 |

两张新增 PNG 合计 **992,423 bytes，约969.2 KiB**；两张图的 BC7 无 mip 像素载荷合计 **929,792 bytes，908 KiB**，再加共享白色像素4 bytes。该小计不包含思源字体原有/动态字形页、Sprite/Texture native 元数据、旧资源或其他场景纹理。

收尾实际Unity报告 [texture-runtime-audit.json](texture-runtime-audit.json) 确认两张图均为BC7、单mip、不可读。Editor native estimate分别为：背包边框 **734,080 bytes**、主菜单背景 **590,720 bytes**，合计 **1,324,800 bytes，约1.26 MiB**。这组实际native估算与上述908 KiB格式像素载荷含义不同，也不能称为准确GPU显存。

共享思源字体当前有 **7张1024² Alpha8页**；逐页实际Editor native estimate为2,098,048 bytes，7页合计14,686,336 bytes（约14.01 MiB），对应纯Alpha8像素载荷为7 MiB。已只读核对Git HEAD：原来就有相同7张页、相同fileID及尺寸，本次没有增加字体图集页。当前字形表相对HEAD从634增至649、字符表从636增至651，各增加15项；字体资源并非字节完全未变。具体变化归属不能仅凭Git HEAD划分到本轮或此前美术工作，但7页原有载荷没有被漏算成新增UI贴图。完整对照记录在资源JSON的 `fontAtlasGitComparison` 中，审计没有修改字体。

两个 PNG 当前 `.meta` 均已写入 Standalone BC7 override（TextureFormat25）、关闭 mip/ReadWrite、NPOT Scale=None。边框具体九宫格参数见 [inventory-frame.md](inventory-frame.md)：L/B/R/T = 39/33/39/33，严格中心 alpha 非零像素为0。不得把 NPOT 的 512×664 改成512×512，否则会改变边距与框比例。

源生图和离线 QA 归档位于 `Tools/ArtPipeline/Source` 与本 Docs 目录，没有放入运行纹理目录。旧 UI 资源仍可能通过旧场景或保留引用成为构建依赖；本清单没有把它们删掉，也没有以新增 PNG 小计宣称整个构建包大小。

## 沿用上一轮的资源

| 已有内容 | 延续预算 |
|---|---|
| 5级宝箱 | 总 GLB 源498,720 bytes，约487.0 KiB；总6,392三角形；单箱2个Renderer、1份共享材质；预热5及原0.32秒开盖/3秒回收规则保留 |
| 5类13项升级插图 | 1张1024²共享图集，797,206 bytes；Windows BC7、无 mip 像素载荷1 MiB；每项绑定不同Sprite但共享Texture |
| 夜战风尘与细雨 | 1张64²径向纹理，源PNG2,390 bytes；共享1份材质、2个ParticleSystem；最多120风尘+56细雨，总硬上限176；默认10+6粒/秒 |
| 夜间粒子限制 | 无新Light、粒子碰撞、拖尾、子发射器或屏幕深度软粒子采样；跟随玩家的局部体积，世界空间模拟；暂停及升级时沿用scaled time冻结 |
| 原场地 | 原120m直径保持，当前光柱资源报告记录MapRadius=60；没有为光柱改变地图尺寸或新增地形采样 |

以上属于已认可并沿用的内容，不重复计入本轮两张新增 PNG 的增量。此前升级图集的 Unity native estimate 约2 MiB，也不能替代1 MiB的BC7像素载荷或反过来称作准确VRAM。

## 光柱 Shader 与运行代码独立审查

审查文件：

- `Scripts/Presentation/Loot/LootRarityBeacon.cs`
- `Art/Effects/Loot/V04/LootRarityBeacon.shader`
- `Editor/V04LootBeaconBuilder.cs`
- 原 `GamePlay/Loot/Drops/DropItem.cs` 与 `Core/Pooling/ObjectPool.cs`

没有发现需要立即修复的材质/网格泄漏或明显高成本渲染配置。组件只在初始化时切换 `sharedMaterial`，每帧读已有 Collider 的启用状态来控制光柱可见性；不写碰撞器、库存、轨迹或收集状态。每帧路径不创建数组、材质、网格或对象，不做全场搜索、射线检测或排序。`SharedMesh` 属性中的 GetComponent 用于审计读取，不在 LateUpdate 路径中。

对象池仍只回调原 DropItem 的 IPoolable。取出和回收均重置视觉；OnEnable/OnDisable 也清空旧稀有度。飞行协程启用期间原 Collider 关闭，LateUpdate 在渲染前看到最终 Collider 状态并隐藏光柱。这样保留原 FIFO 池及弹性扩容行为，不另外建立光柱池。

Shader 为单 Pass、无贴图采样的解析光柱/底环，共4三角形；不读取场景深度，不加灯光、阴影、光照/反射探针、粒子或额外碰撞。`Blend SrcAlpha One`、`ZWrite Off`、`ZTest LEqual`，雾混向黑色以避免出现雾色矩形。顶点在GPU朝向相机并固定世界向上，原掉落物自转不会带着光柱转。少量 `exp2`、长度和 smoothstep 运算作用于窄光柱及小底环，未引入循环采样或全屏效果。

远视角实测后，构建器默认宽度调整为0.36/0.40/0.44/0.48/0.52m，高度2.0/2.3/2.6/2.9/3.2m，强度0.24/0.28/0.34/0.40/0.44（按Common→Legendary顺序）。参数表见 [loot-beacon-implementation.md](loot-beacon-implementation.md)。这一调整不改变网格、材质数量或纹理预算；屏幕覆盖范围随尺寸增加，不能把几何数量不变解释为像素开销完全不变。材质重建后的验证由总集成完成，本资源JSON保留其记录时间对应的源文件体积快照。

当前 [loot-beacon-resources.json](loot-beacon-resources.json) 已记录：8顶点/4三角形、5个材质、0贴图、0新Light/Particle、Shader错误/警告均0；共享Mesh native estimate为1,992 bytes，5个材质合计5,440 bytes。这些是现有Unity资源报告的值，不是本离线审阅估算出的FPS。

当前 [loot-beacon-pool-validation.json](loot-beacon-pool-validation.json) 的 `allPassed=true`，五色共享材质、飞行隐藏、回池清理以及同实例Legendary→Common复用检查通过。真实散落协程结束、密集画面和独立包性能仍按各自测试报告验收，不能由池测试代替。

## 已知预算边界

- **预热30不是活跃装备上限。** 原ObjectPool在空池时会扩容；原掉落60秒寿命未改。30件可见装备新增120三角形只是线性示例，不是最大值保证。
- 透明叠加的实际成本随屏幕覆盖、同屏掉落数量和GPU变化。5个共享材质开启GPU instancing并不保证固定5次draw call；SRP Batcher、透明排序与平台会影响批处理。
- Shader在45–65m内淡出，65m后alpha为0，但仅淡出并不在CPU上关闭Renderer；不应将距离淡出宣称为硬性的远距draw-call剔除。当前相机视锥仍使用Unity正常剔除。
- 当前LateUpdate每帧调用Renderer.enabled setter，即便状态不变也有一次轻量native调用；未发现与当前规模不相称的CPU工作。若后续实际密集掉落压测指向此处，再按测量结果增加状态缓存，无需为假设负载改动玩法池。
- 光环以当前平面地图worldY=0为基准，共享Mesh采用保守Bounds容纳顶点位移。未来若更换为高低起伏地形或缩放掉落Prefab，需要重新检查地面基准及剔除Bounds；本轮平面场地无需新增地形查询。

此次独立审查没有修改Shader、光柱运行代码或受保护算法。核心代码变化证明见 [core-code-protection.md](core-code-protection.md)。文档不宣称FPS、准确总显存或15分钟Windows压力测试已完成。
