# 完整地图补充地标

两件资产沿用回收站的灰绿漆、炭黑钢架、褪色琥珀标记与青色指示灯。所有模型为米制、底部中心轴心、Unity Y 向上 / +Z 正面；集装箱的长边沿 X，货门朝 +X。

| 资产 | Unity 尺寸 X × Y × Z | 三角形 | GLB 大小 |
| --- | --- | ---: | ---: |
| ShippingContainer | 6.06 × 2.59 × 2.44 m | 1,568 | 113,796 bytes |
| CheckpointCanopy | 8 × 4.4 × 3.00087 m | 784 | 60,008 bytes |
| 合计 | — | 2,352 | 173,804 bytes / 169.7 KiB |

检查棚的标记薄片使进深超出 3 m 约 0.87 mm。每件导出只有一个网格、一个 primitive、一个 `ENV_AtlasSurface` 材质；复用现有 16 × 2 色板，三个嵌入小图总计每件 288 bytes，没有新增外部纹理。原有四张色板文件的 SHA256 在构建前后完全一致。数字是资源文件和几何统计，不能等同于 Unity 整个场景的显存或帧率。

集装箱使用浅几何压纹、屋顶肋条、角件与四根门锁杆，在俯视视角保留可读结构。检查棚是四柱开放结构，屋顶厚 0.32 m；建议只布置在外围检查站，避免主战斗视野被顶板遮挡。

碰撞建议：

- 集装箱一个 BoxCollider，中心 `(0, 1.295, 0)`，尺寸 `(6.06, 2.59, 2.44)`。
- 检查棚四个柱子 BoxCollider，中心为 `(±3.5, 2.01, ±1.1)`，尺寸均为 `(0.52, 4.02, 0.52)`。这个保守宽度同时覆盖柱脚；不要给整棚设置实体大盒碰撞，保留双向通行。
- 碰撞层使用场景现有 Obstacle 层。青色灯条只使用色板发光，不自带实时灯组件。

`ShippingContainer.png` 和 `CheckpointCanopy.png` 为导出 GLB 重新导入 Blender 后的实际渲染，已查看轮廓、法线、压纹、门锁、材质颜色与开放通行空间。它们不是 Unity 实机截图。

可编辑源文件：`Tools/ArtPipeline/Source/Quarantine_Landmarks.blend`。构建脚本：`Tools/ArtPipeline/build_landmark_modules.py`；导出校验脚本：`Tools/ArtPipeline/verify_landmark_modules.py`。构建不调用 Meshy，不会覆盖原有六件模型或样板场景。

```powershell
& 'E:/Blender/blender.exe' --background --factory-startup --python 'Tools/ArtPipeline/build_landmark_modules.py'
python 'Tools/ArtPipeline/verify_landmark_modules.py'
```
