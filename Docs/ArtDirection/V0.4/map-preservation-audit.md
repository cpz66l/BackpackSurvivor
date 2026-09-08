# V0.4 地图几何保留审计

结论：**离线结构核对通过**。对比本轮改造前的 `01-Run_ArtFull.before-v04.unity` 与当前 `Assets/BackpackSurvivor/Scenes/Run/01-Run_ArtFull.unity`，`Map` 子树的物体层级、Transform、MeshFilter、Collider 和 Prefab 实例结构均未改变。只发现56个MeshRenderer的 `m_Materials` 引用变化，其余对象/组件字段一致。具体文件哈希、组件计数与材质变化列表见 [map-preservation-audit.json](map-preservation-audit.json)。

| 核对范围 | 数量 | 结果 |
|---|---:|---|
| Map对象及组件序列化记录 | 1,055 | fileID集合完全相同，无增删 |
| Transform记录 | 374 | 全部一致；277个完整Transform，97个Prefab stripped引用 |
| MeshFilter | 149 | 全部一致，Mesh引用未改变 |
| BoxCollider | 97 | 全部字段一致，包含位置、尺寸、触发与启用状态 |
| PrefabInstance | 97 | 原Prefab来源、覆盖字段、增删组件/物体记录全部一致 |
| MeshRenderer | 149 | 56个仅材质引用变化，其余字段与另外93个Renderer一致 |
| MapBounds | 1 | 半径前后均60m，直径120m |

解析沿 `Map` 的 Transform 子树逐层收集记录，同时覆盖97个stripped Transform引用的PrefabInstance。将所有记录按fileID排序，仅移除MeshRenderer的材质字段后，改造前后得到相同的语义SHA-256。这一检查也确认Map内其余Light、MonoBehaviour等直接序列化记录没有变化。

原正式场景 `BackpackSurvivor/Assets/BackpackSurvivor/Scenes/Run/01-Run.unity` 的实际SHA-256与指定基准完全相同：

```text
06A4F25DCCFAA16A88A71809CF941BBE0794872AA3AD303507FC675D20043BA1
```

本次是离线场景序列化结构审计，没有操作Unity、修改Assets或写入场景。它确认场景中的几何组件、Mesh/Collider引用及Prefab实例覆盖保持不变；由于没有单独的外部模型/网格资产字节基线，不把这一结果扩展为所有外部模型文件的字节审计。ArtFull的整体文件哈希会因本轮允许的UI接线和材质变化而改变，不应要求它与改造前快照相同。

核心代码已在本次收尾再次复核：66份当前哈希均与 [core-code-protection-audit.json](core-code-protection-audit.json) 的已审计值一致，仍为相对基线64份完全一致、2份仅含获准改动。资源源体积与纹理实际native估算已同步至 [asset-resource-audit.json](asset-resource-audit.json)。此报告不替代交互回归、帧率测量或完整15分钟Windows独立包验收。
