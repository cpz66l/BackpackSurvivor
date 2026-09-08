# V0.4 背包边框贴图

最终资源：`BackpackSurvivor/Assets/BackpackSurvivor/Art/UI/V04/InventoryFrame.png`。使用获批生图的蓝灰布边和四角金属片，已去除原图实际烘焙在 RGB 中的外部、中心棋盘底，并清理内侧连接到边框的灰色棋盘残点。布纹、缝线和四角保留；没有新增纹样或重绘框体。

| 项目 | 最终值 |
|---|---|
| PNG 格式与尺寸 | RGBA，512 × 664 px，保持原框裁切后的比例 |
| PNG 文件 | 154,622 bytes，约 151.0 KiB |
| 最外非零 alpha 包围盒 | 左上 `(2, 2)`，右下排他 `(510, 662)`；四边均有至少 2 px 全透明 padding |
| 严格全透明中心矩形 | 左上原点：`x = 39…472`、`y = 33…630`，434 × 598 px |
| Unity 左下原点中心 Rect | `(x: 39, y: 33, width: 434, height: 598)` |
| Unity Sprite Border | **Left 39 / Bottom 33 / Right 39 / Top 33** |
| 中心非零 alpha 像素 | **0**，脚本逐像素断言 |
| Windows BC7、无 mip 像素载荷估计 | 339,968 bytes，332 KiB；未计 Unity 元数据、共享字体或总显存 |
| 未压缩 RGBA 像素载荷 | 1,359,872 bytes，约 1.30 MiB |

建议导入为 Sprite / Single，Mesh Type 为 Full Rect，PPU 为 100；Alpha 使用文件 alpha，开启 Alpha Is Transparency，Wrap 为 Clamp，Filter 为 Bilinear，关闭 Read/Write 与 mipmaps。Windows 使用 BC7；**NPOT Scale 设置为 None**，保持 512 × 664，不应缩放成 512 × 512。以上为导入建议，本处理脚本没有操作 Unity Importer。

UGUI Image 使用 Sliced，`fillCenter = false`，`raycastTarget = false`。当 Canvas `referencePixelsPerUnit = 100`、Sprite PPU = 100、Image `pixelsPerUnitMultiplier = 1` 时，边距像素对应 UI 单位，**498 × 626 的外框 Rect 对应 420 × 560 的严格透明九宫格中心**。中心左上位置等于外框左上向右 39、向下 33。布边轮廓和抗锯齿本身不完全是矩形，中心矩形外仍可能存在少量透明留白；该边距保证网格不会碰到可见框体。

如果现有 Canvas 的 `referencePixelsPerUnit` 不是 100，不需要为了这张图改变整个 Canvas：使 Image `pixelsPerUnitMultiplier = Canvas.referencePixelsPerUnit / 100`，即可保持同样的边框 UI 单位尺寸。不要同时按另一个比例再次乘边距。

可复现流程：

```powershell
python Tools/ArtPipeline/prepare_v04_inventory_frame.py
```

脚本默认读取已归档的 `Tools/ArtPipeline/Source/V04InventoryFrame-source.png`，使用 Pillow / NumPy / SciPy：提取连续蓝灰框体、保留黑色轮廓、补回细小高光孔洞、去除背景污染边缘，再以预乘 alpha 缩放。最终自动寻找最大的全透明矩形并输出九宫格边距和 JSON 清单。原生图保持归档，不进入运行资源目录。

已目视检查 [深色底 QA](inventory-frame-dark-qa.png)，布纹和四角完整，中心与外部无棋盘残留，未见亮色边缘污染。[中心矩形 QA](inventory-frame-opening-qa.png) 用细线标出严格透明开口。精确像素、SHA-256 与载荷计算见 [inventory-frame-manifest.json](inventory-frame-manifest.json)。这些是离线贴图检查；Unity 九宫格导入、场景网格对齐和运行占用由集成阶段验证。
