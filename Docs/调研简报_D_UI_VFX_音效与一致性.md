# 调研简报 D：游戏 UI 美术 / 特效贴图 / AI 音效 / 风格一致性（截至 2026-07）

## 1. 游戏 UI 美术（图标/按钮/边框/HUD）

| 工具/模型 | 擅长点 | 透明背景/矢量 | 价格（2026-07） | 商用授权 | 已知缺陷 |
|---|---|---|---|---|---|
| **Recraft V3/V4**（V4 于 2026-02 发布） | 成套图标、矢量风格、hex 精确配色、文字渲染；Custom Style（传参考图即成风格，无需训练）；API 批量生成 | ✅ 原生 SVG/Lottie 矢量输出（2 credits/张），内置背景移除 | 免费档 30-50 credits/天（图片公开、**不可商用**）；Basic $12/月（年付 $10）1,000 credits；API 约 $0.04 位图/$0.08 矢量 | 付费档完全所有权且退订后仍有效 | 复杂多层构图不稳定；SVG 锚点过多需清理；写实人像弱 |
| **FLUX.2 Pro / FLUX.1 系列**（Black Forest Labs） | FLUX.2 支持最多 10 张参考图一致性 + hex 精确配色 + UI mockup 文字；FLUX.1 生态有 Civitai 大量游戏图标 LoRA | 不原生透明，需配 BiRefNet/rembg 抠图；矢量需再转 | FLUX.1 schnell Apache 2.0 免费可商用；dev 权重非商用（产出按 BFL 条款可商用，需自查）；Pro 走 API 约 $0.05/张；fal.ai 训 LoRA 约 $2-4/次 | schnell 最干净；dev 产出商用按官方输出条款 | 本地需 12GB+ 显存；成套图标仍需 LoRA 或参考图锁定 |
| **SD 3.5 / SDXL + LoRA** | 自训 LoRA 是成套图标一致性的上限方案；kohya_ss 训练 1-3 小时 | 配 LayerDiffuse/BiRefNet 出透明 | 模型免费；云训练 Civitai/Replicate 有免费档或几美元 | SD3.5 Community License（年收入 <$1M 免费商用） | 工程门槛最高；文字渲染差，UI 文字须后期 |
| **Ideogram V3** | 文字/Logo 最强，免费档 10 credits/天且可商用无水印 | 位图 | 免费可用；Plus $20/月 | 免费档即允许商用 | 无矢量、参考图仅 1 张，成套一致性弱于 Recraft |
| **Scenario / Layer AI**（游戏专用） | 上传 10-20 张参考图 15 分钟训出自有风格模型，批量出风格锁定资产；集成 Unity 工作流 | 位图+抠图 | Scenario 有免费档；Layer AI Indie $49/月 500 资产 | 付费档可商用 | 贵；对学生 Demo 性价比不如 Recraft+LoRA |
| Midjourney V7（--sref/--sw） | 风格参考最省事的泛用方案 | 位图 | $10/月起，无免费档 | 付费可商用 | 无 API 官方批量、无矢量、图标成套性一般 |

**结论**：本项目 UI 首选 **Recraft**——同一 Custom Style + 固定 hex 调色板批量生成全部图标/边框，SVG 可经 Unity Vector Graphics 包导入或直接导出带透明 PNG。预算零则免费档只能做风格探索（不可商用），正式资产建议付一个月 Basic $10 集中生成后取消。备选：FLUX.1 schnell + Civitai 游戏图标 LoRA（完全免费可商用）。

## 2. 特效贴图：AI vs 程序化

**适合 AI 生成**：单帧粒子 sprite（烟雾团、光斑、辉光、星芒）、魔法符文/法阵贴图、污渍/锈迹/血渍 decal、风格化枪口火焰单帧概念图。用与主视觉同一风格模型生成，BiRefNet 抠透明。

**必须用程序化工具（不要用 AI）**：
- **渐变图/LUT/噪声图/溶解遮罩/flowmap**——Unity Shader Graph、Material Maker（免费开源）、IlluGen（节点式，JangaFX，约 $15/月）分钟级生成且完全可控；AI 生成噪声图毫无意义且不可平铺。
- **序列帧 flipbook（爆炸/火焰/烟）**——AI 帧间一致性是通病（2026 年仍如此），正确做法是 **EmberGen**（GPU 实时流体，导 flipbook/VDB，Indie $25/月，14 天试用）或直接用 **JangaFX 官方 2026 年发布的 CC0 flipbook 包**（爆炸、火、烟、血溅、水花，公有领域，免费可商用）+ 免费 **GlueIT** 打包精灵表。IlluGen 1.1+ 有 Looper 节点可把 80 帧交叉淡化压成 64 帧无缝循环。
- **枪口火焰**：推荐 EmberGen 短模拟烘焙或 CC0 素材调色，而非 AI 序列帧。

**成本结论**：特效贴图可以做到 **0 元**（CC0 包 + Material Maker + Unity 自带 VFX Graph/Shader Graph）；EmberGen 仅在需要定制爆炸时付一个月。被裁员艺术家可申请 JangaFX 6 个月免费许可（学生不适用但值得关注）。

## 3. AI 音效/音乐（2026 年中现状）

| 工具 | 定位 | 免费额度 | 付费 | 商用授权 | 风险 |
|---|---|---|---|---|---|
| **ElevenLabs SFX** | 短音效最强全能（枪声、UI、受击），支持最长约 30s 循环 | 免费档可生成但**不可商用** | Starter **$5/月** 起含商用授权；Creator $22 | 付费档 royalty-free 可入游戏 | 训练数据来源不透明，但无针对终端用户的判例 |
| **Stable Audio 2.5 / 3** | 环境音、氛围 bed、循环、SFX；2.5 云端最长约 3 分钟；3 为开放权重可本地（12GB+ 显存，ComfyUI） | 免费 10 轨/月（非商用） | Pro **$11.99/月** 250 轨商用 | 训练集为已授权的 AudioSparx，**来源最干净**；开放权重版 Community License 年收入 <$1M 免费商用 | 无 vocals；旋律性弱于 Suno |
| **Suno v5/v5.5** | 完整歌曲、Boss 战主题曲、带人声 | 免费 50 credits/天（非商用） | Pro $10/月 起商用 | RIAA 诉讼 2025-11 与华纳 $5 亿和解，残余风险低但条款声明"不保证版权归属"；Suno 保留用你的产出训练的权利 | 商业发行级音乐别裸用；作品集 BGM 可用 |
| **Udio v3** | MIDI stem 导出适合进 DAW 再加工 | 免费 10/天+100/月 | Standard $10 / Pro $30 | 2025-10 与 UMG 和解后法律姿态最干净 | 器乐强于声乐 |
| 小工具备选 | Foximusic（$7/25 次买断、永不 Expired）、SFX Engine（$8/月起） | 有 | 买断制 | 均 royalty-free | — |

**结论**：音效用 **ElevenLabs SFX（付 $5 Starter 一个月集中生成）**；BGM/氛围用 **Stable Audio Pro $11.99 一个月** 或本地 Stable Audio 3（0 元）。Suno 只用于需要"歌"的场合。Steam 自 2025-10 起要求 AI 生成内容披露，上架/发布页需勾选；作品集视频描述中注明 AI 生成是行业惯例。

## 4. 整套资产风格统一的工程方法（可执行清单）

1. **风格圣经先行**：先做 10-15 张参考图 + 固定 hex 调色板（本项目：低饱和冷色、青灰+铁锈橙点缀），所有工具共用这一份"视觉 DNA"。
2. **风格锚定手段按工具选**：Recraft=Custom Style + hex 锁定；Midjourney=--sref --sw 300-500；SD/FLUX=自训 LoRA（kohya_ss 1-3 小时）+ 固定种子区间；FLUX.2=多参考图。
3. **固定 prompt 模板**：风格/光照/质量词固定，只换主体；存为文本片段防漂移。
4. **后处理统一收口**（性价比最高的一步）：所有位图资产进 Unity 前过同一套批处理——统一调色 LUT、统一描边/锐化、统一分辨率规范；Unity 内 URP Volume（Color Adjustments + 统一 LUT + Tonemapping）做最终画面级统一。即使单张资产风格有 ±10% 漂移，统一后处理后肉眼不可辨。
5. **10% 筛选率**：生成量按 10:1 准备，只留通过质量阈值的资产。
6. **来源台账**：每个资产记录工具/日期/授权档位/付费状态截图，求职作品集被问授权时可自证。

## 5. 针对本项目的最终推荐（预算 ≈ $27 一次性）

| 资产类别 | 方案 | 成本 |
|---|---|---|
| UI 图标/边框/HUD | Recraft Basic 一个月，Custom Style 成套生成，SVG/PNG | $10-12 |
| UI 免费备选 | FLUX.1 schnell + Civitai 图标 LoRA + BiRefNet | $0 |
| 粒子 sprite/decal | 与主视觉同模型生成 + BiRefNet 抠图 | 计入图像工具 |
| flipbook/枪口火焰 | JangaFX CC0 flipbook 包 + GlueIT；定制再上 EmberGen 一个月 | $0（+$25 可选） |
| 渐变/噪声/遮罩 | Material Maker / Shader Graph（禁 AI） | $0 |
| 音效 | ElevenLabs SFX Starter 一个月 | $5 |
| BGM/氛围 | Stable Audio Pro 一个月 或本地 Stable Audio 3 | $11.99 或 $0 |
| 风格统一 | 风格圣经 + LoRA/Custom Style + URP 统一 LUT 后处理 + 授权台账 | $0 |

**注意事项**：① Recraft/ElevenLabs/Stable Audio 免费档均不可商用，付费档生成期间资产才带商用权，建议"付费一个月、集中生成、取消"；② Suno/Udio 产出条款声明平台保留训练使用权且不保证版权归属，勿上传未公开机密素材；③ 纯 AI 生成内容在美国版权局口径下不可登记版权，作品集声明"AI 生成 + 人工指导"即可；④ 音效单次生成上限约 30s，长氛围音需在 DAW 内交叉淡化循环；⑤ 所有 AI 位图务必过统一后处理，这是"看起来像同一款游戏"的决定性步骤。
