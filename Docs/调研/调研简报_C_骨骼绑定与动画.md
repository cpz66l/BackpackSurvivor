# 调研简报 C：AI 自动骨骼绑定与动画生成（截至 2026-07）

调研人：调研员C_骨骼绑定与动画
项目背景：Unity URP 俯视角幸存者类 Demo，人形感染体 + 人形主角 + 少量四足/异形 Boss，需走/跑/攻击/受击/死亡动画，导入 Unity Animator。

---

## 一、工具对比表（2026 年中实测状态）

| 工具 | 当前版本/状态 | 擅长点 | 非标准体型（四足/怪物） | 输出与 Unity 兼容 | 价格 | 授权（作品集安全性） | 已知缺陷 |
|---|---|---|---|---|---|---|---|
| **Meshy** | Meshy-6（2026-01） | 一站式：文/图生模型 → auto-rig → 500+ 动画预设 → FBX；绑定+动画 **0 积分**；可指定 A/T-pose 生成 | **支持 humanoid/biped/quadruped/custom**，对上传异形网格容忍度最高 | FBX（标称 Unity 支持）；骨骼命名非标准，Unity Humanoid 自动映射后需人工核对 | 免费 100 积分/月；Pro $20/月（1,000 积分+API+完整所有权） | 免费层 = **CC BY 4.0（须署名 Meshy 且模型公开）**；付费层可商用私有 | 髋部 pivot 偏移 5–15cm → 脚部 IK 滑步；三角面拓扑乱，变形前需重拓扑；肩部权重过平滑 |
| **Tripo AI** | Tripo 3.1 + Smart Mesh P1.0（2026-03 GDC） | 生成最快（~10s）、四边形拓扑干净、骨骼命名最贴近引擎标准骨架，重定向摩擦最小 | 绑定仅双足；实测对非人形（机器人）骨骼摆放是所有工具里最合理的 | 默认 GLB（Unity 需 GLTFast），quad 模式可出 FBX | 免费 200–300 积分/月；Pro ~$13.93/月（年付）/ $19.9 月付，3,000 积分 | 免费层 **CC BY 4.0**；付费层商用 | 肩/胸权重过平滑（举臂塌陷）；无手指/面部骨；**无动画库**，只有基础绑定 |
| **Adobe Mixamo** | 在线但**冻结维护**（2025-06 曾全站宕机数日，客服称"不再支持"；Adobe 未宣布关停） | 仍是最大免费人形动作库（数千条 mocap）+ 免费 auto-rig | **仅双足人形**，T/A-pose | 选 "FBX for Unity" 直接导入设 Humanoid，管线最成熟 | 完全免费（Adobe 账号） | **免版税、无限商用**，作品集安全；唯一限制：不得再分发原始文件 | 十年未实质更新，动捕数据"看着过时"；无 GLB；不可依赖其长期存活，需要的动作先全部下载 |
| **Reallusion AccuRIG 2.0** | 2.0（2025），免费 Windows 桌面 | 纯绑定质量所有免费工具里最好（含手指骨），权重干净；内嵌 ActorCore 动作商店 | **仅人形**，要求 A-pose 且朝向固定 | FBX/USD；注意导入缩放可能 0.01x 需修正 | 工具免费（需免费 ActorCore 账号导出）；绑定结果可商用、无附加费 | 绑定结果可自由商用，作品集安全 | 只有 Windows；ActorCore 动作库大多付费（有免费样例）；非人形不支持 |
| **Cascadeur** | 2026 版（AutoPosing/AutoPhysics/Unbaking/视频动捕） | 物理辅助手 K 动画，攻击/受击/死亡等"招牌动作"质感上限最高；AutoPosing 支持双足+四足 | 摆姿支持四足；但它**不是 auto-rigger**，需先有绑定 | FBX/DAE/USD/glTF（Indie 起）；骨骼命名规范 | 免费版=**非商用且只能导出自有格式 .casc**（300 帧/120 关节限制）；Indie $8/月（年付，年收入 <$10 万可商用，订满 2 年转永久）；Pro $33/月 | 免费版不可商用且导出被锁——**实际必须用 Indie $8/月** | 学习曲线存在；不适合"30 秒出绑定"场景 |
| **DeepMotion（Animate 3D / SayMotion）** | 2026 现役 | Animate 3D=视频动捕（手机拍摄即可，含 foot locking、身+脸+手）；SayMotion=文生 3D 动画 | 仅人形 | FBX/BVH/GLB 直接进 Unity | 免费 60 积分/月、20 秒/段；Starter $15/月起**才含商用授权**（180 积分）；Innovator 480 积分、30 秒/段 | 免费层**无商用授权**；纯个人作品集（不售卖）可用，上 Steam/itch 收费需 Starter | 单摄像头遮挡/转身时质量骤降；输出需清理滑步 |
| **Rokoko（Vision/Video/Create）** | 2026 现役 | Vision=浏览器视频动捕免费入门；Create=文生动作；Motion Library 数百条免费专业 mocap | 仅人形 | FBX/BVH，经免费 Rokoko Studio 导出 | Vision 免费（15 秒上限，5 次导出/月）；Video $10/月（年付）起；Create 免费 5 次 FBX 导出/月 | 免费片段与 Create 输出允许商用（以官网条款为准） | 导出必须走 Rokoko Studio 客户端；单目动捕精度有限 |
| **Cinevva（Auto Rigger / Prompt Animations）** | 2026 浏览器免费工具 | 浏览器内上传 GLB/FBX/OBJ → 绑定+烘焙 idle/walk/run/slash/hurt/fall 等预设 → GLB；**支持四足**；文生动作可自动识别 Mixamo/Tripo/CC4/UE5 骨骼命名并重定向 | **双足+四足均可**（Mixamo 做不到的） | GLB/BVH 输出（Unity 用 GLTFast 或转 FBX） | 免费 | 免费、模型归用户 | 预设数量有限；GLB 进 Unity 多一步转换；2026 年新工具，长期维护未知 |
| **Anything World → 已改名 Everything Universe** | anything.world（anythingworld.com 域名已挂牌出售） | Animate Anything：上传静态模型按类别（人/人形/动物/载具等）自动绑定+动画，类别覆盖最广；有 Unity/Unreal SDK | 动物等类别多，适合异形，但主打**低多边形**程序化资产 | FBX/glTF/GLB 下载 + 引擎 SDK | "免费开始"，具体定价不透明 | 条款需注册后确认 | 低模定位与 AI 生成的中高模角色不匹配；公司 2026 年处于改名/动荡期，不建议作为主管线 |
| **Krikey AI** | 2026 现役 | 浏览器文生/视频生动画，面向内容创作者 | 人形（Ready Player Me 系） | FBX 导出 | 免费层 + 付费 | 免费层条款需确认 | 偏营销/社媒视频，动作质量与游戏可控性一般 |

### 配套免费动画库（强烈建议纳入管线）

| 库 | 授权 | 内容与价值 |
|---|---|---|
| **Quaternius Universal Animation Library 1+2** | **CC0（最自由）** | 250+ 条通用人形骨架动画，UAL 2 含 **zombie 移动、近战连击**——简直为感染体量身定做；Unity/Unreal/Godot 可重定向 |
| Kenney Animated Characters | CC0 | 低模角色+基础动作，原型期可用 |
| Rokoko Motion Library 免费片段 | 免费片段可商用 | 专业 mocap，质量高，需 Rokoko Studio |
| CMU mocap 数据库 | 可商用（不可转售数据） | 2,500+ 条，需大量清理（滑步/抖动） |
| ~~Bandai Namco Motion Dataset~~ | **CC BY-NC 4.0 禁商用** | 仅研究用途，**避开** |
| ~~MDM / MoMask 开源文生动作~~ | 代码 MIT 但训练数据为学术非商用 | 输出商用法律上不干净，**作品集避开** |

---

## 二、针对本项目的具体推荐

### 最省事主管线（人形主角 + 人形感染体）
1. **模型生成**：Meshy（文/图生 3D，指定 **A-pose 或 T-pose** 生成，绑定成功率显著提升）。
2. **绑定+基础动画**：直接用 **Meshy 内置 auto-rig + 500+ 预设**（走/跑/攻击/受击/死亡基本齐活，绑定和动画**不耗积分**），导出 FBX → Unity 设 **Humanoid** → 人工核对骨骼映射（重点检查 Hips/Spine/Clavicle）。
3. **动画补强（感染体的丧尸感）**：叠加 **Quaternius UAL 2（CC0）的 zombie locomotion 与近战连击**，在 Unity 里通过 Humanoid 重定向混用——风格统一且完全无授权顾虑。
4. **兜底方案**：Mixamo 仍可用作补充动作库（免费、授权安全），把需要的动作**现在就全部下载存档**（它随时可能关停）；若不想用 Meshy 的 CC BY 模型，可用免费 **AccuRIG 2.0** 替代绑定环节（绑定结果无署名要求）。

### 四足/异形 Boss
- 首选 **Meshy 的 quadruped auto-rig + 对应预设**（同一平台，管线一致）；
- 备选 **Cinevva Auto Rigger**（免费、支持四足、浏览器即出 GLB）；
- Anything World（现 Everything Universe）动物类别可作第三选择，但其低模定位和公司动荡期不建议依赖；
- Unity 端：非人形用 **Generic rig**，动画烘焙在同一副骨架上，**不能**用 Humanoid 重定向，每个 Boss 的动画需单独生成/烘焙。

### 主角"招牌动作"（决定作品集成败的部分）
- **Cascadeur Indie（$8/月）**：物理辅助手 K 攻击连段/受击/死亡，质量上限远高于任何预设库；免费版导出被锁且禁商用，直接用 Indie。
- 或**自拍视频 → DeepMotion Animate 3D / Rokoko Vision** 转 FBX，作为定制动作的粗坯，再用 Cascadeur 清理滑步。
- 文生动画（SayMotion / Rokoko Create / Cinevva Prompt Animations）2026 年对**单动作短片段（1–10 秒）**已实用，长序列和持械交互仍不稳——只用于生成短 clip 再在 Unity Animator 里混合。

### 2026 年 Mixamo 是否仍是免费最优解？
**不再是唯一最优解，但也还没死。** 2026 年的"免费最优"已经易位：
- **绑定环节**：AccuRIG 2.0（免费桌面、质量最高）与 Meshy/Tripo 内置绑定已追平或超越 Mixamo；Mixamo 绑定器本身冻结在 2010 年代水平。
- **动画库环节**：Mixamo 仍是最大免费人形动作库且授权极友好，但数据观感过时、维护停滞、2025 年有全站宕机记录——**可用、不可依赖**。
- 新替代组合：**Meshy 一站式（绑定+500 预设）** 或 **AccuRIG + Quaternius CC0 库**，均比"Mixamo 一条线"更现代、更可控。

---

## 三、注意事项（避坑清单）

1. **署名陷阱**：Meshy / Tripo 免费层输出均为 **CC BY 4.0**——作品集可商用但必须署名、且免费层模型对公众可见。求职作品集若强调"全部 AI 生成"，建议在作品说明里标注工具来源；若想干净所有权，Meshy Pro $20/月或改用 AccuRIG 绑定 + CC0 动画。
2. **滑步是 2026 年 AI 绑定的头号通病**：Meshy 髋部 pivot 偏移、视频动捕的脚部漂移都会在 Unity 里表现为脚滑。对策：动画导入后开 **Foot IK**、Root Motion 设置核对、必要时 Cascadeur 里修。
3. **骨骼映射必查**：Meshy 骨骼命名非标准，Unity Humanoid 自动映射可能配错 Spine/Clavicle/UpperArm——导入后逐骨核对一遍再做动画状态机，否则所有角色返工。
4. **拓扑问题**：AI 生成的三角面网格直接蒙皮变形，肩/肘/髋容易穿模。AI Gaming Dev 实测"约 60% 一次可用"，其余要重生成或进 Blender 修权重；Tripo Smart Mesh（四边面）在需要变形时更稳。
5. **授权红线**：Bandai Namco 数据集（CC BY-NC）、MDM/MoMask 输出、DeepMotion 免费层（无商用授权）、Cascadeur 免费版（非商用+锁导出）——纯练习可以，**打算发布或售卖就必须绕开或升级付费**。
6. **文生动画的正确姿势**：只生成 1–10 秒短 clip，在 Unity Animator 里做混合与过渡；不要试图一次生成完整动作序列。
7. **先下载后依赖**：Mixamo、Rokoko 免费片段、Quaternius 库——凡是免费的在线资源，本月就全部下载存档，2026 年这类服务的关停/转向是常态（Anything World 改名、Adobe Animate 2026-03 停服都是前例）。

---

## 主要信息来源
- [Cinevva: Free Character Animations and Auto-Rigging (2026-07)](https://app.cinevva.com/guides/free-character-animations-rigging) — Mixamo 维护状态、AccuRIG/Cascadeur/Quaternius 授权细节
- [StraySpark: AI Auto-Rigging Showdown 2026（UE5.7/Blender 实测）](https://www.strayspark.studio/blog/ai-auto-rigging-showdown-2026-tripo-meshy-cascadeur-mixamo) — Tripo/Meshy/AccuRIG/Mixamo 实测缺陷
- [OnyxRanked: Tripo AI vs Meshy AI 2026](https://onyxranked.com/tripo-ai-vs-meshy-ai-2026/) / [Meshy AI Review 2026](https://onyxranked.com/meshy-ai-review-2026/)
- [Neural4D 2026 价格基准对比](https://www.neural4d.com/features/neural4d-vs-tripo-vs-meshy-vs-rodin) — 免费层积分与 CC BY 授权
- [Meshy 官方：商用授权说明](https://help.meshy.ai/en/articles/9992001-can-i-use-my-generated-assets-for-commercial-projects) / [免费计划内容](https://help.meshy.ai/en/articles/15696428-what-is-included-on-the-free-plan)
- [AI Gaming Dev: Meshy Review 2026](https://aigamingdev.com/blog/meshy-review/) — 拓扑与成功率实测
- [SoftwareSuggest: DeepMotion 定价](https://www.softwaresuggest.com/deepmotion) — 免费 60 积分、Starter $15 含商用授权
- [NoCapMocap: Mixamo alternatives 2026](https://www.nocapmocap.com/blog/mixamo-alternatives-2026) — Rokoko Vision 免费层限制
- [GrabOn: Meshy 优惠与授权解读（2026-07）](https://www.grabon.in/meshy-ai-coupons/) — 绑定/动画 0 积分、四足支持、CC BY 细节
- [Animation World Network: Animate Anything](https://www.awn.com/news/anything-world-launches-animate-anything-ai-rigging-and-animation-tool) + anything.world 官网（2026-07 访问，确认改名 Everything Universe）
