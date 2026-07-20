# 调研简报 A：AI 概念图 / 角色三视图 / 道具图标生成（截至 2026 年 7 月）

> 项目背景：Unity URP 3D 俯视角幸存者 Demo（末日封锁区 / 废墟 / 感染体，低饱和冷色调），学生独立开发，美术资产全 AI 生成，求职作品集用途。

---

## 一、主流模型能力对比表（2026 年中状态）

| 模型/工具 | 版本与形态 | 概念图能力 | 三视图/多视角一致性 | 价格 | 商用授权（作品集） | 主要缺陷 |
|---|---|---|---|---|---|---|
| **Nano Banana Pro / 2**（Google Gemini 3 Pro Image / 3.1 Flash Image） | 闭源，Gemini App + API | 综合质量第一梯队，单条提示可直接出正/侧/背 T-pose model sheet；官方支持单图内 5 角色+14 物体一致 | ★★★★★ 目前"一次成图三视图"最稳的方案之一；第三方评测 19 组角色设计提示全部通过（含战斗/武器） | API $0.134/张(2K)、$0.24(4K)；Batch 半价；Gemini App 有免费额度 | 订阅/API 输出可商用（有 SynthID 隐形水印） | 4K 贵；免费额度每日有限 |
| **GPT Image 2**（OpenAI） | 闭源，ChatGPT + API | 质量最高档，文字渲染好，三视图教程生态成熟（Dreamina 等有专门 turnaround 工作流） | ★★★★ 一致性好，但连续编辑存在身份漂移（arXiv 实测漂移快于 FLUX Kontext） | API 约 $0.05/张（标准）；ChatGPT Plus $20/月 | 输出归用户所有 | **内容审核会拒战斗/武器/生物机械类提示**（实测 19 组拒 4 组）——感染体/枪械题材是重大风险 |
| **Midjourney V7** | 闭源订阅 | 艺术感最强，风格探索首选；Omni Reference（--oref，权重 --ow 0–1000）替代旧 --cref，可从任意参考图锁定角色/主体；--sref 锁风格 | ★★★☆ 角色一致性可用（同角色跨场景），但不擅长"一张图内正侧背并列"的精确正交三视图 | $10/$30/$60/$120 月（无免费档）；--oref 消耗 GPU 时间更多 | 付费档均含商用权 | 无免费试用；正交 model sheet 结构控制弱于 Nano Banana/GPT Image |
| **FLUX.2**（Black Forest Labs） | [dev] 32B 开源权重（HF，2026-04）；[klein] 4B Apache 2.0（2026-01）；[pro/flex] API | 2026 开源质量天花板，多参考图（multi-reference）能力全系最强；FP8 量化可在高显存消费卡跑 | ★★★★☆ 多参考锁角色 + Kontext 编辑双路线 | API 按 MP 计费约 $0.03–0.045/MP（1K 图约几美分）；klein 本地免费 | **注意：[dev]/klein-9B 是非商用许可（NCL）**；仅 klein 4B 为 Apache 2.0；API 输出可商用 | dev 许可陷阱（"开源≠可商用"）；Flex 档贵（约 $2.46/1024 图） |
| **FLUX.1 Kontext**（BFL，2025-05 起） | API + [dev] 开源权重 | 上下文编辑模型：给一张角色图，用文字指令换姿势/视角/场景，arXiv 实测身份漂移速度显著慢于 GPT-Image 与 Gen-4 | ★★★★☆ "单图→多视角"路线的代表；社区有现成 Character Turnaround Sheet LoRA（5 姿势）工作流（RunComfy/ComfyUI） | API 约 $0.04–0.08/张；dev 本地免费（非商用） | dev 权重非商用；API 可商用 | 仅 1MP 输出；多次连续编辑仍会缓慢漂移 |
| **Stable Diffusion 3.5**（Stability） | 开源权重，ComfyUI 生态 | 生态最成熟：官方 ControlNet（Canny/Depth/Blur，8B 参数）+ 海量社区 LoRA | ★★★☆ 需自己搭管线：OpenPose/Depth 锁 T-pose 骨架 + 角色 LoRA 锁形象 | 本地免费；训练 LoRA 可租 4090（约 $0.34/h，单次 $2–10） | Stability Community License：年收入 < $1M 免费商用，输出归用户 | 上手陡；单人学生维护成本高；原生多视角一致性需靠 LoRA 补齐 |
| **Qwen-Image-Edit 2509/2511 + 多角度 LoRA**（阿里开源 + fal.ai 训练） | 开源，ComfyUI | **2026 年"单图出全角度"的明星开源方案**：fal.ai 用 3000+ 3D 渲染图训练的多角度 LoRA，支持 96 个相机角度，滑杆切视角，人物/场景一致性保持好 | ★★★★☆ 正面立绘→侧/背/任意角度，且可把输出当训练集再炼角色 LoRA（"一致性衍生训练集"玩法） | 免费开源（Apache 2.0）；本地 8–16GB 显存可跑，4090 约 30 秒/张；RunningHub 可在线跑 | Apache 2.0，作品集安全 | 视角切换后细节质量有损失；需 ComfyUI 基础 |
| **Recraft**（V3 及后续） | 闭源 SaaS | **图标/矢量专用**：原生 SVG/PNG、品牌色板、风格锁定（style lock）批量出同风格图标 | ★★★★（图标风格一致性维度） | 免费 50 积分/天（**不可商用、版权归 Recraft**）；Basic $12/月起（含商用）；API $0.04 位图 / $0.08 矢量 | 付费档安全；免费档作品集慎用 | 免费档版权归平台是最大坑 |
| **即梦 AI**（字节，Seedance 2.0 / 图像模型） | 闭源 SaaS | 中文提示友好，角色一致性工具链成熟（角色库+三视图固化玩法在漫剧圈已标准化），国内免魔法 | ★★★★ 有现成"三视图公式/模板"生态 | 免费 60–260 积分/天（有水印）；会员 ¥69/199/649 月 | 付费会员商用，免费档带水印、条款限制多 | 2026 年涨价+积分贬值；免费档水印 |
| **可灵 AI**（快手） | 闭源 SaaS | 多图参考功能对人物一致性保持评价很高（多图喂参考→新图锁脸/锁服装） | ★★★☆ | 免费额度+会员制 | 会员商用 | 偏视频，静态图管线不如即梦全 |
| **Scenario** | 游戏资产专用 SaaS | 训练自定义模型锁整个游戏画风；2025-10 起官方有 Character Turnaround 生成器（三种工作流：提示编辑/参考/精修） | ★★★★ 为游戏 pipeline 设计 | Creator $15/月；Pro $45/月（自定义训练） | 付费档商用 | 无精灵表/动画管线；性价比对个人一般 |
| **Leonardo.ai** | SaaS | 免费 150 token/天可长期白嫖概念图；Live Canvas 迭代快；可训练自定义模型 | ★★★ | 免费档慷慨；Apprentice $12/月含商用 | 付费档商用 | 角色一致性弱于第一梯队 |

---

## 二、角色三视图（正/侧/背 T-pose）的成熟做法（2026 年共识）

到 2026 年中，三视图已从"玄学"变为三条可复现的成熟路线，按门槛排序：

**路线 1：多模态大模型一次成图（推荐主力）**
- Nano Banana Pro / GPT Image 2 直接提示："character model sheet, orthographic front/side/back views, neutral T-pose, flat lighting, white/grey background"。2026 年这两家对"同图多视角并列"的空间一致性已相当可靠，出 4–8 张选 1 张即可。
- 配套技巧：先用 Midjourney/即梦把角色、服装、环境、色调（本项目：低饱和冷色末日风）定调成一张"角色设定板"，再喂给 Nano Banana/GPT Image 做参考图生成三视图，一致性显著更好（LinkedIn 多位创作者验证的混合管线）。
- **本项目注意**：感染体/武器提示在 GPT Image 上被拒率高，优先走 Nano Banana Pro。

**路线 2：单图→多视角编辑（保底/补强）**
- 云端：FLUX.1 Kontext（文字指令"show the character from the side/back"）或其社区 Turnaround Sheet LoRA（5 姿势现成 ComfyUI 工作流）。
- 本地免费：Qwen-Image-Edit-2511 + fal.ai 多角度 LoRA（96 角度滑杆），正面立绘进、全角度出。
- 该路线的产出可二次利用：把合格的多角度图当数据集，炼一个角色专属 LoRA（Kohya / Civitai 在线训练器 / Scenario），后续所有姿势、表情、宣传图都从 LoRA 出，彻底锁死一致性。

**路线 3：传统可控管线（需要精控时）**
- SD 3.5 / FLUX + ControlNet（OpenPose 锁 T-pose 骨架、Depth 锁体积）+ 角色 LoRA + IPAdapter/FaceID 锁脸。控制力最强但搭建成本最高，学生项目只建议在路线 1/2 失效时采用。

**通用避坑点**：
- 侧/背面细节与正面冲突（"纸片人"、配件凭空消失）仍是所有模型的通病 → 三视图仅作建模参考，关键配件需单独出特写。
- 连续多次编辑会累积身份漂移（Kontext 漂移最慢，GPT Image 较快）→ 每条编辑链控制在 3 步内，必要时回到原图重开分支。
- 头身比在跨视角时易崩 → 提示词中显式写头身比与身高线（height line guides）有改善。

---

## 三、道具/武器图标（扁平化 + 透明底 UI 图标）方案

**首选：Recraft（付费档）**
- 原生矢量/扁平风格、支持透明背景 PNG 与 SVG、可用自定义"风格"批量锁同一套图标语言（幸存者游戏几十个道具图标一次统一）。
- $12/月 Basic 即含商用权与私有生成，**用一个月批量出完后可退订**。
- 切忌用免费档：版权归 Recraft 且禁商用。

**替代：GPT Image API（background: "transparent"）/ Nano Banana**
- GPT Image 原生支持透明背景参数，适合程序化批量出图；武器类提示有过审风险，措辞可软化（"survival tool icon"）。
- 无透明参数时用纯色背景 + rembg/PS 去底后补 alpha，幸存者类扁平图标边缘规则，自动去底成功率高。

**Unity 落地**：1024×1024 PNG（带 alpha）导入 Unity 设 Sprite (2D and UI)，URP 下直接用于 UGUI；SVG 需先转 PNG（Unity 对 SVG 支持有限）。

---

## 四、针对本项目的具体推荐（可直接执行）

**预算 ≈ 0–150 元的推荐管线：**

1. **风格定调（概念图）**：即梦免费每日积分 + Gemini App 免费额度跑概念稿；确立"低饱和冷色末日"风格板（1 张主视觉 + 色板 + 材质参考）。
2. **角色三视图**：Gemini App / Nano Banana Pro（API 每张约 ¥1，整个项目角色控制在 20 个内成本 < ¥50）按路线 1 出三视图；不满意的角色用 Qwen 多角度 LoRA（本地或 RunningHub 在线，免费）补侧/背面。
3. **风格统一兜底**：若多角色画风漂移，花 $45 开一个月 Scenario Pro 训一个项目画风模型，或在 Civitai 免费在线炼风格 LoRA。
4. **图标**：Recraft Basic $12 开一个月，建一个"flat game icon, muted cold palette"自定义风格，批量出完全部道具/武器图标后退订。
5. **怪物/战斗内容规避**：涉及感染体血腥、枪械的提示绕开 GPT Image；在 Nano Banana / 即梦 / 本地开源模型中生成。

**作品集安全性排序（从最安全到需规避）**：
Apache 2.0 开源（Qwen、FLUX.2 klein 4B、SD3.5 社区许可 <$1M）＞ 付费 API（OpenAI/Google/BFL API，输出归你）＞ 付费订阅（Midjourney/Recraft Basic/Scenario/即梦会员）＞ **慎用**：Recraft 免费档（版权归平台）、FLUX.2 [dev] 本地权重（非商用许可——作品集公开展示属灰色地带，建议只用其 API 或换 klein 4B）、即梦免费档（水印）。

**已知缺陷提醒（写进作品集 README 反而加分）**：
- 三视图仅解决"参考"，不能替代建模；AI 图在 Unity 中只作 Sprite/参考板/图标，3D 模型仍需 Tripo/Meshy 类图生 3D 或手建（超出本调研范围）。
- 所有云端模型 2026 年都有隐形水印（SynthID 等），不影响 Unity 使用。
- 国产平台积分/价格 2026 年 4 月起波动大（即梦涨价+积分贬值），锁预算时按"月"采购不要囤年卡。

---

## 五、主要信源（2025-10 至 2026-07）

- FLUX.1 Kontext 一致性漂移实测：arXiv 2506.15742
- VibeDex 角色设计模型横评（2026-05）：GPT Image 拒审率、Nano Banana Pro 全通过
- BFL 官方文档与 VentureBeat（2026-01）：FLUX.2 klein 4B Apache 2.0 / 9B NCL
- EvoLink / laozhang.ai（2026-02/04）：Nano Banana Pro API 定价 $0.134(2K)/$0.24(4K)
- puter.com / dynalord（2026-06/07）：FLUX API 按 MP 计费 $0.03–0.045
- prompt-architects / pamistanbul（2026-04/06）：Midjourney V7 Omni Reference 用法与定价
- AITrendTool / CheckThat.ai（2026-03/07）：Recraft 定价与免费档版权限制
- Scenario 官方博客（2025-10）与帮助中心（2026-04）：Turnaround 三种工作流
- 知乎/新浪/RunningHub（2026-01）：Qwen-Image-Edit-2511 多角度 LoRA（96 角度，fal.ai 训练，开源）
- 36氪/东方财富（2026-03/04）：即梦会员 ¥69/199/649 与涨价
- GitHub dalfc6/ji-meng-ai-invite-guide（2026-04）：即梦免费 60 积分/天
