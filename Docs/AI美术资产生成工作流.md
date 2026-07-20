# 《背包幸存者》AI 美术资产生成工作流

> 依据 GDD（末日封锁区 / 冷色废墟 / 低饱和工业风 / 3D 俯视角 / Unity URP）定制的全 AI 美术管线。
> 调研基准时间：2026 年 7 月。AI 工具 3–6 个月一迭代，正式开工前花半天复核各工具当月版本与价格。

---

## 0. 总览：资产清单与管线映射

| 资产类别 | 数量估算 | 主管线 | 补充/兜底 |
| --- | --- | --- | --- |
| 风格圣经（调色板+参考图） | 10–15 张 | 即梦/Midjourney 概念图 | Gemini App 免费额度 |
| 角色三视图（主角+5敌人+3Boss+精英变体） | ~15 套 | Nano Banana Pro（Gemini 3 Pro Image） | Qwen 多角度 LoRA（免费）、FLUX.1 Kontext |
| 角色 3D 模型 | ~10 个 | Meshy（图生3D，指定 A/T-pose） | 混元3D 网页版（每日免费）、Tripo、Trellis 2（MIT） |
| 武器/道具模型（地面掉落物+宝箱+终端） | ~20–30 个 | Meshy / Tripo 批量 | Rodin Gen-2（英雄武器高模） |
| 场景模块（废墟墙体、车辆、油桶、污染区部件） | ~20 件 | Meshy / 混元3D | 灰盒保留+AI 贴图 |
| 骨骼绑定+基础动画 | 全角色 | Meshy auto-rig + 内置 500+ 动作（0 积分） | AccuRIG 2.0（免费）、Mixamo（先下载存档） |
| 感染体特色动画 | 丧尸步态/近战 | Quaternius UAL 2（CC0）重定向 | Mixamo 动作库 |
| 主角招牌动作（攻击/受击/死亡） | 3–5 条 | Cascadeur Indie $8/月 手K | 自拍视频 → Rokoko/DeepMotion |
| UI 图标（武器/道具/技能/背包格子） | ~50 个 | Recraft Basic 一个月批量 | FLUX.1 schnell + BiRefNet（免费） |
| VFX 贴图（枪口火焰/烟/血/污染雾） | ~20 张 | AI 单帧 + JangaFX CC0 序列帧 | Material Maker / Shader Graph（程序化） |
| 音效（枪声/UI/受击/环境） | ~30 条 | ElevenLabs SFX Starter $5/月 | Stable Audio（氛围/BGM） |

**总预算估算：约 ¥400–600**（Meshy Pro $20×2 个月 + Recraft $12×1 + ElevenLabs $5×1 + Cascadeur $8×1–2），配合大量免费工具。

---

## 1. 第 0 阶段：风格圣经（一切的前提，1–2 天）

风格不一致是全 AI 资产的最大风险。先固化以下三件事，之后所有资产都以此为锚：

### 1.1 调色板（hex 固定）

- 主色：青灰 `#3A4550` / 深铁灰 `#2B3038`
- 辅色：湿冷蓝绿 `#5C7A7A` / 雾白 `#B8C4C4`
- 点缀（稀有度/警示）：铁锈橙 `#C46A3A`、污染荧光绿 `#7AFF8A`、警示红 `#D94F4F`
- 稀有度色沿用 GDD：白绿蓝紫金红，但饱和度统一压低 20%

### 1.2 参考图板

用即梦免费积分（中文友好、有三视图固化功能）或 Midjourney 生成 10–15 张"末日封锁区"氛围图，选出最贴合的 3 张作为所有后续工具的**风格参考图**。

### 1.3 固定 Prompt 模板

```
[主体描述], post-apocalyptic quarantine zone, ruined industrial city,
muted cold color palette, teal-grey and rust orange accents, desaturated,
wet ground reflections, top-down game asset, [orthographic/T-pose 等结构词],
concept art for a stylized realistic survival game
```

主体词每次替换，风格/光照/质量词**永远不动**。

---

## 2. 第 1 阶段：概念图与三视图（第 1 周，与灰盒开发并行）

> 目标：为每个需要建模的角色/道具产出"正/侧/背 T-pose 三视图"，作为图生 3D 的输入。

### 2.1 主力：Nano Banana Pro（Gemini 3 Pro Image）

- 2026 年实测**单提示直出三视图能力最强**（正/侧/背同一张 sheet，多角色一致性好）。
- 提示词要点：`character turnaround sheet, front view, side view, back view, T-pose, full body, orthographic, consistent proportions across views, height reference line`。
- 成本：API 约 $0.134/张（2K），15 套三视图 **< ¥30**；Gemini App 有免费额度可先试。
- **注意**：战斗/武器/感染体类提示在 GPT Image 2 上会被拒审（实测 19 组拒 4 组），本项目题材优先走 Nano Banana，不要以 GPT Image 为主力。

### 2.2 免费兜底与补强

- **Qwen-Image-Edit 2511 + 多角度 LoRA**（Apache 2.0，本地 8–16GB 显存）：单图出 96 个相机角度，授权最干净。
- **FLUX.1 Kontext**：文字指令换视角（"show the back view"），身份漂移最慢；链式编辑 ≤3 步，否则累积漂移。
- **即梦**：中文提示+角色库，适合快速出风格草图；免费档有水印，仅用于参考不用于交付。

### 2.3 三视图质量验收

- 三视图头身比一致（提示词加身高参考线）；
- 侧/背面无"纸片人"细节冲突——出现即重生成，不要拿去喂 3D（垃圾进垃圾出）；
- 每套图留存 2–3 个备选。

---

## 3. 第 2 阶段：3D 模型生成（第 7–10 周集中产出）

> 原则：**永远喂图不喂纯文本**（text-to-3D 风格漂移大）；集中开 1–2 个月 Meshy Pro 批量产出。

### 3.1 主力：Meshy（Pro $20/月）

- 三视图直接喂 image-to-3D；综合质量 2026 盲测第一（网易/腾讯 1331 名 3D 美术盲测偏好率 63.8%）。
- 开 **Low Poly Mode** 控面数；PBR 贴图开箱即用；FBX 导出官方支持 Unity。
- 生成角色时**指定 A/T-pose**，为下一阶段绑定闭环铺路。
- 付费档资产**完全所有权**（免费档是 CC BY 4.0 需署名，作品集建议避开）。

### 3.2 免费补充

- **混元3D 3.0 网页版**：每日免费额度，原生支持多视图输入+智能减面，人物面部 SOTA，中文界面——用来跑角色备选和道具变体。
- **Tripo 免费 300 积分/月**：风格化批量一致性最好，quad 拓扑对游戏最友好；适合做同系列道具（武器家族）。
- **Trellis 2（微软，MIT 协议）**：有 16GB N 卡可本地零成本无限迭代，授权零风险；无卡可用 HF Spaces 免费演示。

### 3.3 英雄资产（特写镜头会拍到的）

- Boss、标志性武器：**Rodin Gen-2**（硬表面/机械精度之王）或混元3D 高精度档出高模 → 减面烘焙法线到低模。
- 注意：Rodin 按下载计费较贵（$30/月档），只在 2–3 个关键资产上用。

### 3.4 场景模块

- 用"模块化"思路：墙体段、破损车辆、集装箱、铁网、油桶、补给终端、污染地面 decal，各生成 2–3 变体，Unity 里拼装复用（俯视角+统一雾效，20 件左右即可撑满一张图）。
- 纯贴图类（地面裂纹、血渍、锈迹）不建模，AI 出图 + 程序化做无缝平铺。

### 3.5 面数纪律（URP 性能红线）

| 资产 | 三角面上限 |
| --- | --- |
| 主角/Boss | ≤ 15k |
| 普通敌人 | ≤ 8k |
| 道具/武器 | ≤ 5k |
| 场景模块 | ≤ 3k/件 |

AI 输出动辄几十万面，导入前必须用工具内置减面/remesh 或 Blender Decimate——**这是作品集里可以讲的技术点**。

---

## 4. 第 3 阶段：骨骼绑定与动画（第 8–10 周）

### 4.1 人形角色（主角 + 感染体 + 装甲怪等）——一站式闭环

1. Meshy 生成时已指定 A/T-pose → 直接用 **Meshy auto-rig + 500+ 内置动作库**（绑定/动画**不耗积分**）：走/跑/攻击/受击/死亡基本齐活。
2. 导出 FBX → Unity 设 **Humanoid** → **逐骨核对映射**（重点 Hips/Spine/Clavicle，Meshy 骨骼命名非标准，自动映射可能配错，不核对则全角色返工）。
3. **感染体"丧尸感"补强**：Quaternius Universal Animation Library 2（**CC0**）有现成的 zombie 移动和近战连击，Unity Humanoid 重定向直接混用，授权零风险。
4. **Mixamo**：仍免费且动作库最大，但已冻结维护（随时可能关停）——**本月就把需要的动作全部下载存档**，仅作补充。

### 4.2 四足/异形 Boss（畸变母体、黑雾主宰）

- 首选 Meshy 的 **quadruped auto-rig** + 对应预设（同一平台管线一致）；
- 免费备选 **Cinevva Auto Rigger**（浏览器、支持四足）；
- Unity 端用 **Generic rig**，动画烘焙在同一副骨架上——**不能**用 Humanoid 重定向，每个异形 Boss 的动画单独生成。

### 4.3 主角招牌动作（面试录屏的特写部分）

- **Cascadeur Indie $8/月**：物理辅助手K攻击连段/受击/死亡，质感上限远高于预设库（免费版禁商用+锁导出，直接上 Indie）；
- 或手机自拍视频 → **Rokoko Vision / DeepMotion** 转粗坯 → Cascadeur 清理；
- 文生动画（SayMotion 等）2026 年只对 **1–10 秒短 clip** 实用，长序列别指望。

### 4.4 避坑

- **滑步是 AI 绑定头号通病**（髋部 pivot 偏移）：Unity 开 Foot IK、核对 Root Motion；
- AI 三角面直接蒙皮，肩/肘/髋易穿模，约 60% 一次可用，其余重生成或 Blender 修权重；
- 避开授权红线：Bandai Namco 数据集（CC BY-NC）、MDM/MoMask 输出、DeepMotion 免费层（无商用授权）。

---

## 5. 第 4 阶段：UI 图标（第 9–10 周，1–2 天集中出完）

- **主力：Recraft Basic $12/月**，建一个自定义风格（上传风格圣经参考图 + 固定 hex 调色板），批量生成全套图标：武器、道具、芯片、技能、稀有度边框、背包格子底纹；输出透明底 PNG/SVG。
- **策略：付一个月，集中生成完，取消订阅。** 免费档版权归平台，千万别用。
- 免费替代：FLUX.1 schnell（Apache 2.0）+ Civitai 游戏图标 LoRA + BiRefNet 抠透明底。
- Unity 落地：1024² 带 alpha PNG → Sprite (2D and UI)；SVG 先转 PNG 或用 Unity Vector Graphics 包。

---

## 6. 第 5 阶段：VFX 贴图与音效（第 10–11 周）

### 6.1 VFX 贴图分工（可做到 $0）

| 类型 | 方案 |
| --- | --- |
| 单帧 sprite（烟团、辉光、星芒、血渍 decal、污染符文） | AI 生成（与主视觉同模型同风格词）+ BiRefNet 抠图 |
| 序列帧 flipbook（爆炸、火焰、水花） | **JangaFX 官方 CC0 包**（公有领域，直接商用），GlueIT 打包；**不要用 AI 生成序列帧**（帧间一致性是通病） |
| 渐变/LUT/噪声/溶解遮罩/flowmap | 程序化：Material Maker（免费）或 Shader Graph |

### 6.2 音效

- **ElevenLabs SFX Starter $5/月**：枪声、受击、UI、掉落、合成音（≤30s），付费档含商用授权；
- **Stable Audio**：环境氛围（风声、电流、远处尖叫）与 BGM 循环，训练集已授权、来源最干净；免费 10 轨/月（非商用），Pro $11.99/月 可商用；
- 长氛围用两条 30s 循环在 Unity 里交叉淡化；
- 注意：若以后上架 Steam，2025 年 10 月起需披露 AI 内容。

---

## 7. 导入 Unity 的工程规范

1. **格式**：统一 FBX（Unity 原生支持最好；GLB 需 glTFast/GLTFUtility 包）。
2. **材质**：URP 下统一转 **URP/Lit**；注意 Unity 用 metallic-smoothness 通道，AI 导出的 roughness 贴图需在 Blender 里反转打包（一步操作）。
3. **后处理统一收口（决定性步骤）**：所有 2D 资产过同一批处理（统一 LUT/描边/锐化/分辨率）；URP Volume 统一调色 + 雾效，可掩盖 ±10% 的单资产风格漂移。
4. **筛选率**：接受 10:1 的生成/采用比，把"重生成"排进时间预算。
5. **授权台账**：每个资产记录【工具/生成日期/订阅档位/授权条款截图】，作品集标注"AI 辅助生成 + 本人管线整合与性能优化"——这是面试加分点而非减分点。

---

## 8. 与 12 周排期对齐

| 周 | 美术动作 |
| --- | --- |
| 第 1 周 | 风格圣经（调色板/参考图/prompt 模板）；只出主角+1 敌人的三视图备用；Mixamo/Quaternius 动作库**下载存档** |
| 第 2–6 周 | 灰盒 MVP：全部用胶囊体+方块+免费占位（Kenney CC0），**不做任何正式美术** |
| 第 7 周 | 全角色三视图批量产出（Nano Banana Pro）；确定 Meshy Pro 订阅 |
| 第 8–9 周 | Meshy 批量建模+绑定+基础动画；Quaternius 重定向；场景模块拼装；Rodin 出 Boss 高模 |
| 第 9–10 周 | Recraft 一个月集中出全套 UI；Cascadeur 手K主角招牌动作 |
| 第 10–11 周 | VFX 贴图 + ElevenLabs/Stable Audio 音效；URP 后处理统一调色 |
| 第 12 周 | 整体风格检查、性能检查（面数/DrawCall）、录屏与作品集材料 |

---

## 附：各工具授权安全速查（作品集公开场景）

| 安全级别 | 工具 |
| --- | --- |
| ✅ 零风险 | Trellis 2 (MIT)、Qwen (Apache 2.0)、FLUX.1 schnell (Apache 2.0)、Quaternius (CC0)、JangaFX flipbook (CC0)、Mixamo（免版税）、AccuRIG 绑定结果 |
| ✅ 付费后干净 | Meshy Pro、Tripo 付费、Rodin、Recraft Basic、ElevenLabs Starter、Stable Audio Pro、Cascadeur Indie |
| ⚠️ 有附加条件 | Meshy/Tripo 免费档（CC BY 需署名）、混元3D 开源版（社区许可）、DeepMotion 免费档（非商用） |
| ❌ 避开 | Recraft 免费档（版权归平台）、Bandai Namco 数据集、MDM/MoMask 输出、即梦免费档（水印） |
