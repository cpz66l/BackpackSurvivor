# 《背包幸存者》第一个玩家角色 AI 八向序列帧实现方案

> 目标：制作首名可玩的军人角色，用 AI 生图完成角色定妆与待机、移动、射击序列帧；运行时采用 3D 逻辑根节点 + 2D 多方向精灵，不使用角色骨骼或 3D 模型。

## 1. 最终方案

角色采用“5 个独立方向 + 3 个镜像方向”的八向精灵方案：

| 独立制作 | 镜像得到 |
| --- | --- |
| S：朝屏幕下方，正面 | — |
| SW：朝屏幕左下，正面三分之二侧 | SE |
| W：朝屏幕左侧，纯侧面 | E |
| NW：朝屏幕左上，背面三分之二侧 | NE |
| N：朝屏幕上方，背面 | — |

角色身体、武器、枪口特效和地面阴影分层制作：

```text
PlayerRoot（移动、碰撞、血量、鼠标朝向）
├── VisualPivot（始终与摄像机平面平行）
│   ├── WeaponBack（朝背面时使用）
│   ├── BodySprite
│   ├── WeaponFront（朝正面/侧面时使用）
│   └── MuzzleFX
└── GroundShadow
```

资产边界必须严格遵守：

- `BodySprite` 图片只允许出现角色身体、服装、手臂和手，不允许出现任何武器；
- `WeaponBack/WeaponFront` 图片只允许出现武器，不允许出现人物、手或手臂；
- `MuzzleFX` 图片只允许出现枪口火焰/烟，不允许出现武器或人物；
- 三类资产分别生成、分别抠图、分别导入 Unity，不能先生成组合图再拆分。

第一版不拆分上下半身。玩家身体朝向鼠标，移动时播放通用战斗小跑。若实际测试中侧移、后退的滑步感明显，再升级为“腿跟随移动方向、上身跟随瞄准方向”。

## 2. 角色设定

### 2.1 基础身份

- 角色代号：**先锋（Vanguard）**
- 身份：封锁区前驻防部队下士，灾变后成为资源回收队员
- 年龄与体型：约 32 岁，中等身高，结实但不过分健壮
- 战斗定位：全能型初始角色，轮廓清晰、装备朴素，方便后续角色形成差异
- 美术比例：约 4.5 头身；适当放大头盔、手套和军靴，缩小时仍能辨认

### 2.2 固定外观

- 无徽章的橄榄绿战术头盔
- 深棕短发，面部简化，无胡须
- 铁锈橙围巾，作为玩家识别色
- 深青灰作战服
- 卡其色战术背心，正面四个对称方形弹匣袋
- 居中的小型方形背包
- 黑色手套，深棕色军靴
- 所有装备左右对称，不使用文字、国旗、臂章和单侧挂件，确保可以水平镜像
- 身体图层不绑定具体武器；双手保持通用的“空手持握”姿势，双手之间预留武器叠加空间

### 2.3 固定调色板

| 部位 | 建议颜色 |
| --- | --- |
| 作战服 | `#3F4C49`、阴影 `#283330` |
| 头盔 | `#46523C`、阴影 `#2F382A` |
| 战术背心 | `#8A7658`、阴影 `#5F503C` |
| 围巾识别色 | `#C9683E`、高光 `#E18453` |
| 手套/内衬 | `#252A2C` |
| 军靴 | `#392E28` |
| 皮肤 | `#B77E60`、高光 `#D39A75` |

AI 不一定能准确遵守十六进制色值，但必须把这些色块作为后期统一标准。

## 3. 第一版资产范围

| 动画 | 帧数 | 播放速度 | 独立方向 | 独立图片数 |
| --- | ---: | ---: | ---: | ---: |
| Idle 待机呼吸 | 4 | 4 FPS | 5 | 20 |
| Move 战斗小跑 | 6 | 10 FPS | 5 | 30 |
| Attack 射击后坐 | 3 | 15 FPS | 5 | 15 |
| 步枪静态方向图 | 1 | — | 5 | 5 |
| 枪口火焰 | 3 | 18–24 FPS | 可旋转复用 | 3 |

首轮共约 **73 张正式透明图片**。受击先使用闪白、挤压和短暂停顿，死亡先使用统一碎裂/倒地特效，不制作八向专用序列帧。

## 4. AI 生成方法

### 4.1 必须具备的模型能力

用于正式逐帧生成的模型至少需要：

- 支持上传角色参考图；
- 支持较强的角色身份一致性；
- 最好支持姿态图、OpenPose、骨架图或结构参考；
- 支持固定 Seed 或可复现的编辑链；
- 支持局部重绘；
- 最好支持原生透明背景。

纯文本生图只用于第一次定妆。如果模型不能使用角色参考和姿态控制，不建议用它批量生成正式动画帧。

### 4.2 不直接生成整张 Sprite Sheet

不要用一句提示词要求 AI 一次生成 5 个方向、24 个动作帧。常见结果是：

- 头盔、背心和背包每格发生变化；
- 人物比例、镜头高度和脚底位置漂移；
- 模型把动画顺序理解成多名不同角色；
- 输出看似是精灵表，实际无法无缝循环。

正确流程是：

1. 先生成一张批准的角色定妆图；
2. 从定妆图生成 5 个方向的静止基准帧；
3. 每个方向都以自己的基准帧为身份参考；
4. 用固定姿态图逐帧生成动作；
5. 每张图单独抠图、对齐和清理；
6. 最后才在 Unity 或图集工具中组合。

### 4.3 提示词实际使用步骤

#### 阶段 A：锁定角色身份

1. 新建一个独立的角色生成任务，不上传武器参考图。
2. 使用“5.3 角色定妆图提示词”，画幅设为 1:1，生成 8–12 个候选。
3. 只选择角色比例、头盔、围巾、背心和背包最稳定的一版，不要因为姿势更酷而牺牲结构一致性。
4. 用局部重绘修正手、脸和服装；同一编辑链最多修改 3 次，仍有问题就回到原始候选重新分支。
5. 保存批准图为 `Vanguard_Master_Reference.png`，它是之后所有角色图片的第一参考图。
6. 从批准图提取固定色板和外观描述，后续不得临时添加新装备。

#### 阶段 B：生成 5 个方向基准帧

每个方向都从 `Vanguard_Master_Reference.png` 单独开始，不要按“正面→侧面→背面”连续编辑，否则误差会逐次累积。

每次输入的顺序固定为：

```text
[上传 Vanguard_Master_Reference.png]
[全局角色锁定词]
[当前方向词]
[空手基准动作词]
[角色专用无武器约束词]
[输出约束词]
[负面提示词]
```

具体操作：

1. 上传 `Vanguard_Master_Reference.png`，设置为高强度角色/身份参考。
2. 复制“5.1 全局角色锁定词”。
3. 从“5.4 五个方向基准帧”中只复制一个方向词，例如 S。
4. 复制空手基准动作词和角色专用无武器约束词。
5. 复制“5.2 输出约束词”。
6. 如果平台有独立 Negative Prompt 输入框，将“5.9 通用负面提示词”放进该输入框；否则在正文末尾追加 `Avoid:` 后粘贴。
7. 固定 Seed，生成 4 个候选。
8. 只保留镜头、体型和装备与 Master 最一致的一张，保存为 `Vanguard_Direction_S_Base.png`。
9. 用相同流程分别生成 SW、W、NW、N，始终从 Master 开新分支。
10. 将 SW、W、NW 临时水平翻转，确认其能够自然成为 SE、E、NE；若翻转后装备含义错误，应立即修改角色设计，而不是继续批量生产。

#### 阶段 C：准备动画姿态控制图

正式动画需要 4 张 Idle、6 张 Move、3 张 Attack 姿态图。姿态图只控制关节位置，不提供角色外观。

建议做法：

1. 使用 Blender 简单人偶、OpenPose 编辑器或手绘骨架制作姿态；
2. 固定同一画布、摄像机和脚底基线；
3. 每个动作只改变必要关节，不移动摄像机；
4. S 方向先做完整姿态组并验证，其他方向按同一动作相位旋转；
5. 命名为 `Pose_Idle_00.png`、`Pose_Move_00.png`、`Pose_Attack_00.png` 等。

如果所用模型不支持姿态图，仍然要从“当前方向基准帧”逐帧开新分支，用动作词做低幅度图生图；不要用上一张动画帧生成下一张。

#### 阶段 D：逐帧生成角色身体

每张正式身体帧使用三个输入来源：

```text
身份参考：Vanguard_Master_Reference.png
方向参考：Vanguard_Direction_[DIR]_Base.png
姿态参考：Pose_[STATE]_[FRAME].png
```

推荐权重：

- 身份参考：0.85–0.95；
- 方向参考：0.80–0.90；
- 姿态控制：0.85–1.00；
- 图生图重绘幅度：0.25–0.45。

提示词组合顺序：

```text
[全局角色锁定词]
[当前方向词]
[当前动画帧动作词]
[角色专用无武器约束词]
[输出约束词]
```

每一帧都从同一个方向基准帧开始生成。以 S/Move/00 为例：

```text
Use the attached approved Vanguard character reference as the only identity reference.
The exact same male military survivor, about 32 years old, compact stylized 4.5-head-tall proportions,
an oversized plain olive ballistic helmet with no logo, short dark-brown hair, no beard,
a symmetrical rust-orange neck scarf, a dark teal-grey combat uniform,
a tan tactical vest with exactly four symmetrical square magazine pouches,
a small centered square backpack, black gloves, dark-brown military boots.
Keep the exact same face, body proportions, costume construction, colors and equipment in every image.
The outfit must remain bilaterally symmetrical, with no text, badges, flags or one-sided accessories.
Stylized 2.5D top-down action game character sprite, chunky readable silhouette,
clean hard-edged color blocks, restrained detail, pixel-art-inspired but without a fake pixel grid.

Facing straight toward the bottom of the screen, front view seen from the locked elevated camera.
Combat jog loop frame 1 of 6: left foot forward contact, right foot extended back,
torso stable, hands held in the standardized empty two-handed grip pose.

Character body layer only. Absolutely no weapon, firearm, rifle, gun, blade or tool.
Both empty gloved hands are visible and held in a consistent two-handed grip pose,
with a clean empty gap reserved for a separately rendered weapon sprite.
Do not replace the empty gap with any object.

One character only, full body, centered, both boots completely visible,
camera locked at 45 degrees above the ground with orthographic-like perspective,
identical camera, identical scale and identical framing as the supplied direction reference,
feet on the exact same baseline, no ground plane, no cast shadow, no scenery,
transparent background, clean alpha edge, game production sprite source.
```

生成后立刻检查：

1. 图片中是否完全没有武器；
2. 双手之间是否保留稳定的空位；
3. 角色高度、脚底和摄像机是否与方向基准帧一致；
4. 头盔、四个弹匣袋、围巾和背包是否完全一致；
5. 不合格就从方向基准帧重新生成，不在错误帧上继续编辑。

#### 阶段 E：独立生成武器

武器任务必须新开项目/对话，只上传批准的武器概念图，不上传角色参考图，避免模型自动生成人手或人物。

1. 使用“5.8 模块化步枪提示词”先生成武器概念图，批准唯一外观并保存为 `Rifle_Master_Reference.png`。
2. 分别生成 S、SW、W、NW、N 五个方向。每个方向都从武器 Master 开新分支。
3. 每张武器图只允许出现一把枪；人物、手、手臂和服装都必须为零。
4. 抠图后放进 128×128 透明画布，但不强求 AI 直接对齐角色双手。
5. 在 Aseprite/Photoshop 或 Unity 中，把枪托/主握把位置对齐到对应角色方向的手部空位。
6. 为每个方向记录 `GripAnchor` 与 `MuzzleAnchor`；锚点属于 Unity 配置，不绘制进 PNG。
7. 开火后坐不重新生成武器动画，通过 Unity 将武器沿枪管反方向位移 1–2 个逻辑像素实现。
8. 枪口火焰作为第三套独立资产生成/制作，绑定到 `MuzzleAnchor`。

#### 阶段 F：逐方向止损验证

严格按以下顺序投入产量：

1. S 方向身体：Idle 4 + Move 6 + Attack 3；
2. S 方向武器：1 张；
3. Unity 中组合身体与武器，检查双手、枪托和枪口；
4. 只有 S 方向验证通过，才制作 SW；
5. SW 通过后再制作 W、NW、N；
6. 最后统一生成镜像方向并配置 flipX。

## 5. 提示词体系

提示词建议使用英文。每次正式生成都由“角色锁定词 + 视角词 + 动作帧词 + 输出约束词”组成。

### 5.1 全局角色锁定词

以下内容在每一张动画帧中保持完全不变：

```text
Use the attached approved Vanguard character reference as the only identity reference.
The exact same male military survivor, about 32 years old, compact stylized 4.5-head-tall proportions,
an oversized plain olive ballistic helmet with no logo, short dark-brown hair, no beard,
a symmetrical rust-orange neck scarf, a dark teal-grey combat uniform,
a tan tactical vest with exactly four symmetrical square magazine pouches,
a small centered square backpack, black gloves, dark-brown military boots.
Keep the exact same face, body proportions, costume construction, colors and equipment in every image.
The outfit must remain bilaterally symmetrical, with no text, badges, flags or one-sided accessories.
Stylized 2.5D top-down action game character sprite, chunky readable silhouette,
clean hard-edged color blocks, restrained detail, pixel-art-inspired but without a fake pixel grid.
```

### 5.1.1 角色专用无武器约束词

该段必须加入所有角色基准帧和角色动画帧：

```text
Character body layer only. Absolutely no weapon, firearm, rifle, gun, blade or tool.
Both empty gloved hands are visible and held in a consistent two-handed grip pose,
with a clean empty gap reserved for a separately rendered weapon sprite.
Do not replace the empty gap with any object.
```

### 5.2 输出约束词

```text
One character only, full body, centered, both boots completely visible,
camera locked at 45 degrees above the ground with orthographic-like perspective,
identical camera, identical scale and identical framing as the supplied direction reference,
feet on the exact same baseline, no ground plane, no cast shadow, no scenery,
transparent background, clean alpha edge, game production sprite source.
```

如果模型不支持可靠的透明 PNG，把最后一行替换为：

```text
uniform flat medium-grey background #808080, no gradient, no shadow, no reflected light from the background
```

橄榄绿服装不适合使用绿幕；中灰背景更容易去除，也不会产生绿色边缘污染。

### 5.3 角色定妆图提示词

```text
Create a professional five-view turnaround sheet for Vanguard, a stylized military survivor for a 2.5D top-down action game.
Show the exact same character in five full-body views: front, front three-quarter, side, rear three-quarter and back.
All five views have identical body proportions, clothing construction, colors and scale.
Neutral relaxed low-ready pose, empty hands, no weapon.
The character is a compact 4.5-head-tall stylized adult male with an oversized plain olive helmet,
rust-orange neck scarf, dark teal-grey combat uniform, tan symmetrical tactical vest with four square pouches,
centered square backpack, black gloves and dark-brown boots.
Muted post-apocalyptic military palette, chunky readable shapes, hard-edged flat shading,
clean animation model sheet, neutral medium-grey background, no text, no labels, no shadow, no scenery.
```

定妆图只作为身份参考，不直接切进游戏。批准前检查：四个弹匣袋、围巾、背包、头盔形状和头身比是否在所有视角一致。

### 5.4 五个方向基准帧

在“全局角色锁定词”和“输出约束词”之间加入下列视角词之一：

| 方向 | 视角提示词 |
| --- | --- |
| S | `Facing straight toward the bottom of the screen, front view seen from the locked elevated camera.` |
| SW | `Facing toward the bottom-left of the screen, front three-quarter view from the character's left side.` |
| W | `Facing directly toward the left side of the screen, clean left-side profile.` |
| NW | `Facing toward the top-left of the screen, rear three-quarter view from the character's left side.` |
| N | `Facing straight toward the top of the screen, centered back view.` |

基准动作词：

```text
Neutral combat low-ready stance, knees slightly relaxed, torso stable,
both empty gloved hands positioned in a standardized two-handed grip pose,
with a clean empty gap between the hands for a separate weapon sprite.
```

不允许用代理步枪辅助姿态。模型如果无法维持空手持握，应增加姿态图约束或局部重绘双手，而不是把枪生成进角色图。

### 5.5 Idle 待机动画提示词

每个方向先批准 Frame 0，再以该帧作为同方向后续三帧的直接图像参考。四帧动作描述如下：

| 帧 | 追加动作词 |
| ---: | --- |
| 0 | `Idle loop frame 1 of 4: neutral low-ready pose, shoulders relaxed, knees soft, feet planted.` |
| 1 | `Idle loop frame 2 of 4: a very subtle inhale, chest and shoulders slightly raised, feet completely locked.` |
| 2 | `Idle loop frame 3 of 4: return to the exact neutral low-ready pose, identical foot placement.` |
| 3 | `Idle loop frame 4 of 4: a very subtle exhale, torso and shoulders slightly lowered, feet completely locked, ready to loop back to frame 1.` |

附加约束：

```text
Extremely subtle breathing only. Do not move the feet, do not change the camera,
do not change the silhouette, costume, equipment, body size or canvas position.
```

### 5.6 Move 战斗小跑动画提示词

第一版制作“朝当前面对方向前进”的 6 帧战斗小跑。游戏中横移和后退也暂时复用它。

| 帧 | 追加动作词 |
| ---: | --- |
| 0 | `Combat jog loop frame 1 of 6: left foot forward contact, right foot extended back, torso stable, hands held in the standardized empty two-handed grip pose.` |
| 1 | `Combat jog loop frame 2 of 6: weight compressed over the left leg, body at the lowest point, right leg beginning to pass.` |
| 2 | `Combat jog loop frame 3 of 6: passing pose, right knee moving forward, body rising, left foot pushing off.` |
| 3 | `Combat jog loop frame 4 of 6: right foot forward contact, left foot extended back, mirror phase of frame 1.` |
| 4 | `Combat jog loop frame 5 of 6: weight compressed over the right leg, body at the lowest point, left leg beginning to pass.` |
| 5 | `Combat jog loop frame 6 of 6: passing pose, left knee moving forward, body rising, right foot pushing off, ready to loop.` |

每帧都追加：

```text
Compact tactical jog, not a sprint. Keep the head and empty grip-position hands controlled.
Preserve the exact camera, character scale and costume. No motion blur.
```

最可靠的做法是先准备 6 张简单的姿态骨架图，用 OpenPose/姿态控制锁定腿部位置，再让模型只负责角色外观。

### 5.7 Attack 射击后坐动画提示词

射击身体动画只表现后坐，武器和枪口火焰由独立层完成。

| 帧 | 追加动作词 |
| ---: | --- |
| 0 | `Ranged attack body frame 1 of 3: steady empty two-handed aiming pose, feet planted, torso braced, no weapon present.` |
| 1 | `Ranged attack body frame 2 of 3: short recoil reaction in shoulders and empty hands, elbows compress, torso leans back very slightly, feet remain locked, no weapon present.` |
| 2 | `Ranged attack body frame 3 of 3: halfway recovery from recoil, empty hands returning to the original grip points, feet remain locked, no weapon present.` |

每帧都追加：

```text
No weapon, no muzzle flash, no shell casing, no smoke and no dramatic body twist.
The recoil is short and readable, suitable for a three-frame game animation.
```

### 5.8 模块化步枪提示词

首把武器暂定为简化突击步枪。仍只生成 S、SW、W、NW、N 五个方向，其余方向镜像。

```text
Use the attached approved rifle reference.
A single compact modular assault rifle sprite for a stylized 2.5D top-down action game,
dark gunmetal body, short magazine, simple readable silhouette, restrained mechanical detail,
viewed from the same locked 45-degree elevated orthographic-like game camera,
[WEAPON_DIRECTION],
weapon asset only, no character, no human silhouette, no hands, no arms, no clothing,
no muzzle flash, no shell casing,
centered on a transparent 128 by 128 production canvas, no ground, no shadow, clean alpha edge.
```

将 `[WEAPON_DIRECTION]` 替换为：

| 方向 | 武器方向词 |
| --- | --- |
| S | `rifle barrel pointing straight toward the bottom of the screen, stock pointing toward the top` |
| SW | `rifle barrel pointing toward the bottom-left of the screen, stock pointing toward the top-right` |
| W | `rifle barrel pointing directly toward the left side of the screen, stock pointing right` |
| NW | `rifle barrel pointing toward the top-left of the screen, stock pointing toward the bottom-right` |
| N | `rifle barrel pointing straight toward the top of the screen, stock pointing toward the bottom` |

武器图与身体图使用相同的 128×128 画布，但武器 PNG 自身不使用角色脚底作为物理 Pivot。最终位置由每个方向的 `GripAnchor` 在 Unity 中配置。

### 5.9 通用负面提示词

```text
multiple characters, duplicate body, extra limbs, missing limbs, malformed hands,
different helmet, different scarf, different vest, different backpack, different colors,
asymmetrical equipment, logo, text, letters, numbers, flag, badge, camouflage pattern,
eye-level camera, low-angle camera, fisheye, perspective distortion, cropped feet,
dynamic camera, motion blur, depth of field, cast shadow, floor, scenery, debris,
glow, rim light, dramatic cinematic lighting, soft watercolor edge,
fake pixel grid, inconsistent pixel size, checkerboard background, watermark
```

生成角色图片时，在负面词末尾额外追加：

```text
weapon, firearm, rifle, gun, pistol, shotgun, blade, tool, muzzle flash, shell casing
```

生成武器图片时，在负面词末尾额外追加：

```text
person, character, soldier, human, face, body, hand, arm, glove, clothing
```

## 6. 逐帧生成参数建议

不同平台参数名称可能不同，原则如下：

- 所有图片使用同一批准角色参考图；
- 同一方向的所有动画使用该方向基准帧作为第二参考；
- 固定 Seed；
- 角色参考强度保持较高，约 0.8–0.95；
- 姿态控制强度约 0.8–1.0；
- 图生图重绘幅度保持低到中等，约 0.25–0.45；
- 每次只改变姿态描述，不改变角色锁定词、视角词、分辨率和画幅；
- 一条连续编辑链不要超过 3 次，发生身份漂移时回到方向基准帧重新分支；
- 每帧生成 4 个候选，只保留最一致的一张。

推荐以 1024×1024 生成干净的平涂源图，不让 AI 直接绘制最终像素格。AI 的“伪像素”经常存在不一致的像素尺寸，动画播放时会闪烁。

## 7. 图片清理与统一规范

### 7.1 最终画布

- 文件格式：RGBA PNG；
- 画布：128×128；
- 角色可见高度：约 92–98 像素；
- 双脚中点：固定在从左上角计算的 `(64, 116)`；
- 底部预留约 12 像素；
- 所有帧使用相同缩放，不得按每帧包围盒分别缩放；
- Alpha 边缘尽量为硬边，不保留灰色背景光晕；
- 不在角色图片中保留地面阴影。

### 7.2 后期步骤

1. 使用原生透明输出，或用 BiRefNet/rembg/Photoshop 去除中灰背景；
2. 清理轮廓外 1–2 像素的灰边；
3. 以方向基准帧为标尺统一人物高度；
4. 把脚底对齐到 `(64, 116)`；
5. 使用 Area/Lanczos 从高分辨率缩小到 128×128；
6. 将颜色压缩到约 32–48 色，并按固定调色板校正；
7. 在 Aseprite/Photoshop 中逐帧清理头盔、围巾、双手和双脚；
8. 使用 Onion Skin 检查脚底漂移和轮廓闪动；
9. 在 4 FPS、10 FPS、15 FPS 下分别预览三个循环；
10. 需要更粗像素时，再测试“缩到 64×64、最近邻放大到 128×128”，不要重新让 AI 生成像素格。

### 7.3 命名规则

```text
Vanguard_Body_Idle_S_00.png
Vanguard_Body_Idle_SW_01.png
Vanguard_Body_Move_W_05.png
Vanguard_Body_Attack_NW_01.png
Vanguard_Rifle_S.png
Vanguard_Muzzle_00.png
```

方向统一使用：`N, NE, E, SE, S, SW, W, NW`。动画帧从 `00` 开始。

## 8. Unity 导入规范

### 8.1 目录

```text
Assets/BackpackSurvivor/Art/Characters/Player/Vanguard/
├── Sprites/
│   ├── Body/Idle/
│   ├── Body/Move/
│   ├── Body/Attack/
│   ├── Weapon/
│   └── VFX/
├── Materials/
├── AnimationData/
└── Prefabs/
```

高分辨率 AI 原图和中间文件不要放进 Unity 的 `Assets`，保存在项目外的 SourceArt 目录或版本化美术源文件目录中。

### 8.2 Texture Import Settings

对所有角色序列帧统一设置：

- Texture Type：`Sprite (2D and UI)`；
- Sprite Mode：单张 PNG 使用 `Single`；
- Pixels Per Unit：首版 `64`；
- Mesh Type：`Full Rect`；
- Pivot：Custom `(0.5, 0.094)`，对应 128 画布底部约 12 像素；
- Filter Mode：`Point (no filter)`；
- Generate Mip Maps：关闭；
- Compression：首版 `None`；
- Alpha Is Transparency：开启；
- Wrap Mode：`Clamp`；
- Max Size：`256`；
- Generate Physics Shape：关闭。

创建一个 `Vanguard.spriteatlas`：

- 关闭 Tight Packing；
- 关闭 Allow Rotation；
- Padding 设为 4–8；
- 将 Body、Weapon、VFX 文件夹加入 Atlas。

### 8.3 材质

原型阶段可以先用普通 Sprite Unlit 材质。正式版本使用 URP 自定义角色材质：

- Alpha Clip；
- ZTest LEqual；
- ZWrite On；
- 双面显示；
- 不由角色平面直接投射阴影；
- 受击颜色、全局明暗和溶解参数通过 MaterialPropertyBlock 控制。

Alpha Clip 和深度写入可以让角色正确被 3D 建筑、墙体和车辆遮挡。地面接触阴影由单独的椭圆 Mesh/Sprite 表现。

## 9. Unity 动画实现

### 9.1 不使用大型 Animator Controller

第一版建议使用轻量的 `DirectionalSpriteAnimator`，直接按帧切换 `SpriteRenderer.sprite`。这样比制作 24 个以上 Animator 状态更清楚，也更适合以后大量敌人复用。

创建 `DirectionalSpriteSet` ScriptableObject，保存：

```text
Idle[8]   -> 每个方向 4 张 Sprite、是否 flipX
Move[8]   -> 每个方向 6 张 Sprite、是否 flipX
Attack[8] -> 每个方向 3 张 Sprite、是否 flipX
IdleFPS   = 4
MoveFPS   = 10
AttackFPS = 15
```

镜像方向配置：

| 方向 | 使用图片 | flipX |
| --- | --- | --- |
| N | N | false |
| NE | NW | true |
| E | W | true |
| SE | SW | true |
| S | S | false |
| SW | SW | false |
| W | W | false |
| NW | NW | false |

### 9.2 方向计算

项目当前 `PlayerController` 已让 `PlayerRoot.forward` 朝向鼠标，因此表现层读取逻辑根节点的 forward，不读取 Billboard 子节点的 forward。

方向量化逻辑：

```csharp
Vector3 cameraForward = Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up).normalized;
Vector3 cameraRight = Vector3.ProjectOnPlane(viewCamera.transform.right, Vector3.up).normalized;
Vector3 facing = logicRoot.forward;

float x = Vector3.Dot(facing, cameraRight);
float y = Vector3.Dot(facing, cameraForward);
float angle = Mathf.Atan2(x, y) * Mathf.Rad2Deg;
int directionIndex = ((Mathf.RoundToInt(angle / 45f) % 8) + 8) % 8;
```

方向数组顺序固定为：

```text
0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW
```

### 9.3 状态选择

```text
如果 WeaponSystem 正在触发射击 -> Attack
否则 CharacterController.velocity 水平速度 > 0.1 -> Move
否则 -> Idle
```

Attack 播放完 3 帧后，根据当前速度自动返回 Move 或 Idle。移动和待机循环播放；切换方向时保留当前动画相位，减少八向切换时的脚步跳变。

武器系统在实际发射时调用：

```csharp
playerSpriteAnimator.PlayAttack();
```

### 9.4 Billboard

`VisualPivot` 在 `LateUpdate` 中与摄像机平面保持平行：

```csharp
transform.rotation = viewCamera.transform.rotation;
```

逻辑根节点仍然正常绕 Y 轴旋转和碰撞；只有美术子节点覆盖为摄像机朝向。

### 9.5 武器层级

- 角色身体和武器始终引用不同 PNG/Sprite；禁止把组合后的画面回写成新的角色 Sprite；
- N、NE、NW：启用 `WeaponBack`，排序在身体后方；
- S、SE、SW：启用 `WeaponFront`，排序在身体前方；
- E、W：先使用前方层，若手臂穿插明显再单独修正；
- 开火时让武器沿瞄准反方向位移 1–2 个逻辑像素，再快速恢复；
- 枪口火焰绑定每个方向的 Muzzle Anchor，不烘焙进身体动画。

## 10. 验收标准

### 10.1 单帧一致性

- 每帧始终有同一形状的头盔、橙色围巾、四个弹匣袋和居中背包；
- 人物高度变化不超过 2 像素；
- 非运动需要时，脚底漂移不超过 1 像素；
- 不出现额外挂件、徽章、文字或迷彩图案；
- 五个方向的镜头俯角和角色比例一致。

### 10.2 动画

- Idle 第 3 帧回到第 0 帧时不跳动；
- Move 左右步态对称，6→0 帧衔接自然；
- Attack 后坐清楚但不改变脚底位置；
- 切换方向时人物大小和脚底位置不跳；
- 武器枪口与 VFX 出生点在所有方向对齐。

### 10.3 Unity 场景

- 角色脚底与地面阴影接触，不产生漂浮感；
- 角色能被 3D 墙体、车辆和建筑正确遮挡；
- 当前 45°/-45°固定镜头下轮廓自然；
- 1920×1080 和内部 960×540 两种渲染测试均无明显闪烁；
- 鼠标瞄准与移动方向不同时，滑步感仍在可接受范围。

## 11. 推荐执行顺序

1. 定妆图生成 8–12 版，批准 1 版；
2. 生成并批准 S、SW、W、NW、N 五张方向基准帧；
3. 只做 S 方向的 Idle、Move、Attack，导入 Unity 验证尺寸、风格和帧率；
4. 确认 S 方向成立后，再制作其余四个方向；
5. 制作模块化步枪和枪口锚点；
6. 统一抠图、脚底对齐、调色和像素清理；
7. 配置 SpriteAtlas、DirectionalSpriteSet 和角色 Prefab；
8. 在现有 Run 场景测试遮挡、阴影、瞄准和移动；
9. 通过验收后，将同一管线复制给第一个感染体。

最重要的止损点是第 3 步：先用一个方向验证 AI 帧间一致性和 Unity 最终观感，未通过前不要批量生成剩余 60 多张图片。
