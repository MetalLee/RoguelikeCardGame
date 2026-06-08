# 视觉方向

状态：草案  
上级索引：[[design/README|Design Knowledge Base]]

## 目的

定义视觉风格、可读性要求和美术资产优先级。视觉必须同时服务漫画书感、识别和操作。

## 视觉目标

- 画面风格需要成为与同类 Roguelike 卡牌游戏区分的第一层差异化信号。
- 视觉基调采用“手绘漫画书风格”：整体画面应像一本可交互漫画，使用手绘线稿、网点阴影、纸张纹理、粗细变化明显的墨线、分格式构图和夸张动作张力。
- 战斗演出优先使用漫画分镜语言，例如冲击格、速度线、局部特写、拟声字、跨格打击和命中瞬间的短暂停顿，让玩家产生“正在看一场由自己出牌驱动的漫画战斗”的感觉。
- 在产品取舍中，画面演出仅次于连锁 combo 爽感，优先级高于内容数量、构筑深度、平衡严谨度和世界观氛围。
- 卡牌、敌人、状态和奖励能快速识别。
- 战斗信息层级清楚，不被背景或特效干扰。
- 高连锁爆发需要提供明显的动画升级感，让玩家感受到终结牌的强力兑现。
- 数值反馈和画面效果需要相互配合，让玩家同时看懂收益并感到爽快。
- 角色和世界观有记忆点。
- 独立游戏制作范围内可持续产出。

## 美术资产优先级

1. 卡牌框架、图标和关键词可读性。
2. 战斗界面和敌人表现。
3. 角色、Boss 和高频奖励。
4. 事件插图、背景和装饰元素。

## 第一版 MVP 关键元素视觉母版

后续界面原型应先对齐第一张关键元素视觉风格图，再扩展到战斗、奖励和结算界面。该母版至少包含：

当前视觉母版文件：

![[game/assets/art/style_guides/mvp_key_elements_visual_master.png|第一版 MVP 关键元素视觉母版]]

该母版用于统一主角、敌人、卡牌、卡牌包和遗物的手绘漫画书风格。母版图中的文字仅作为视觉排版参考；正式运行时卡牌名、效果文本、数值和 UI 文案仍应由 Godot 界面层绘制，避免图片内文字影响本地化和可读性。

- 主角：剑客形象，漫画风格，笔划简洁，不堆叠过多服饰、武器或面部细节；轮廓应清楚，便于在小尺寸战斗界面中识别。剑客在战斗中的基础形象和后续动作素材统一放入 `game/assets/art/characters/swordsman/`。主角不应只使用单一静态立绘；当玩家使用卡牌时，应根据卡牌效果展示不同动作或姿态，例如普通斩击、快速刺击、重击、防御、观察 / 专注、回费 / 减费、终结爆发等。
- 怪物：恶魔形象，漫画风格，笔划简洁，不堆叠过多鳞片、角、纹理或身体细节；重点保留可读轮廓、威胁姿态和敌我方向。第一版 MVP 敌人透明 PNG 立绘统一放入 `game/assets/art/enemies/`，并默认朝左，便于在战斗 UI 右侧面对玩家；具体资源索引见 [[design/02_content_systems/02_enemies_and_bosses|敌人与 Boss]]。
- 卡牌整体：使用竖版卡牌比例，避免被压成横向或扁平形状；卡牌内容需要完整留在边框内，并保留安全边距。每张卡需要包含卡牌名、卡面插图区和效果描述区。行动牌、技能牌和终结牌必须共用同一套卡牌组件尺寸，包括外框宽高、标题栏高度、卡面插图区高度、效果描述区高度和底部装饰位置，不能因为牌类不同改变卡牌大小。
- 行动牌：血红主题；左上角清晰展示所需行动点；边框使用粗线条，卡面构图体现漫画分镜风格；行动点样式需要和终结牌的连锁层数样式区分。当前模板资源：![[game/assets/art/cards/templates/action_card_template.png|行动牌模板]]
- 技能牌：冷青 / 幽蓝主题；技能牌默认不消耗行动点，因此不需要在左上角展示行动点；边框使用粗线条，卡面构图体现漫画分镜风格。当前模板资源：![[game/assets/art/cards/templates/skill_card_template.png|技能牌模板]]
- 终结牌：紫黑色主题；左上角清晰展示所需连锁层数，并使用不同于行动点的链环、连锁标记或其他稳定符号；边框使用粗线条，卡面构图体现漫画分镜风格。所有终结牌都必须拥有独立美术资源，不能只复用通用终结牌模板或普通攻击动作；独立资源至少应表达该终结牌的目标范围、核心收益和高连锁兑现感。当前模板资源：![[game/assets/art/cards/templates/finisher_card_template.png|终结牌模板]]
- 卡牌包：按行动牌、技能牌、终结牌的主题颜色设计，使用竖向包装比例，不能显得过扁；外层只表达牌包类型，不提前暴露具体卡牌。当前资源：![[game/assets/art/cards/packs/reward_pack_mvp_action.png|行动牌包]] ![[game/assets/art/cards/packs/reward_pack_mvp_skill.png|技能牌包]] ![[game/assets/art/cards/packs/reward_pack_mvp_finisher.png|终结牌包]]
- 遗物：遵循漫画风格，简单偏 Q 版，轮廓清楚，不堆叠过多材质和纹样细节。第一版 MVP 遗物资源：![[game/assets/art/relics/relic_mvp_chain_spark.png|连锁火花遗物图标]]

## 第一版 MVP 卡面插画资源

卡面插画资源只负责卡牌中部的漫画分镜画面，不承载卡框、标题、费用、连锁需求或规则文本；正式卡牌实例由 Godot 将插画与对应卡牌模板和 UI 文本动态组合。

| 卡牌   | 视觉职责                                                         | 资源                                                                   |
| ---- | ------------------------------------------------------------ | -------------------------------------------------------------------- |
| 基础斩击 | 血红速度线、剑客横向斩击和命中爆裂，用于支撑行动牌 `card.basic_strike` 的基础攻击与连锁启动反馈。  | ![[game/assets/art/cards/artwork/card_basic_strike.png\|基础斩击卡面]]     |
| 迅捷刺击 | 极窄直线突刺、远距离命中点和高速残影，用于支撑 0 费行动牌 `card.quick_jab` 的轻快启动感。      | ![[game/assets/art/cards/artwork/card_quick_jab.png\|迅捷刺击卡面]]        |
| 重斩   | 自上而下的重击构图和大面积命中爆裂，用于支撑 2 费行动牌 `card_heavy_strike` 的高成本强打反馈。  | ![[game/assets/art/cards/artwork/card_heavy_strike.png\|重斩卡面]]       |
| 连段切击 | 多个剑客动作残影和连续弧形斩击，用于支撑奖励行动牌 `card_chain_cut` 的连段推进感。           | ![[game/assets/art/cards/artwork/card_chain_cut.png\|连段切击卡面]]        |
| 流步   | 低身位滑步、绕行动线和远端追击命中，用于支撑奖励行动牌 `card_flow_step` 的位移与低费连锁启动感。    | ![[game/assets/art/cards/artwork/card_flow_step.png\|流步卡面]]          |
| 破防重击 | 剑光撞击盾牌并炸裂碎片，用于支撑奖励行动牌 `card_guard_break` 的破防和高压输出感。          | ![[game/assets/art/cards/artwork/card_guard_break.png\|破防重击卡面]]      |
| 基础防御 | 剑客架刀形成蓝色护盾，用于支撑技能牌 `card.basic_guard` 的防御和当前回合行动点恢复反馈。       | ![[game/assets/art/cards/artwork/card_basic_guard.png\|基础防御卡面]]      |
| 战术观察 | 分镜视线、目标锁定和路线规划，用于支撑技能牌 `card_tactical_read` 的观察、过牌和战术预判感。    | ![[game/assets/art/cards/artwork/card_tactical_read.png\|战术观察卡面]]    |
| 回稳   | 剑客低身位稳住姿态并形成环形能量，用于支撑技能牌 `card_second_wind` 的防御 / 回费和节奏恢复感。  | ![[game/assets/art/cards/artwork/card_second_wind.png\|回稳卡面]]        |
| 深度专注 | 静坐蓄势、环绕光刃和冷青能量场，用于支撑技能牌 `card_deep_focus` 的专注过牌与卡组运转感。       | ![[game/assets/art/cards/artwork/card_deep_focus.png\|深度专注卡面]]       |
| 预备减费 | 多分镜准备动作、握剑和起势姿态，用于支撑技能牌 `card_setup_discount` 的预备、减费和后续连段铺垫。 | ![[game/assets/art/cards/artwork/card_setup_discount.png\|预备减费卡面]]   |
| 爆裂终结 | 紫黑能量斩击贯穿单体目标并爆裂，用于支撑终结牌 `card_burst_finish` 的单体高伤害兑现。        | ![[game/assets/art/cards/artwork/card_burst_finish.png\|爆裂终结卡面]]     |
| 弧光横扫 | 宽幅紫色弧光横扫多名敌人，用于支撑终结牌 `card.arc_sweep_finish` 的群体攻击和多目标爆发。    | ![[game/assets/art/cards/artwork/card_arc_sweep_finish.png\|弧光横扫卡面]] |
| 回流终结 | 紫色能量斩击命中后形成回旋流线，用于支撑终结牌 `card_refund_finish` 的伤害与资源返还感。      | ![[game/assets/art/cards/artwork/card_refund_finish.png\|回流终结卡面]]    |
| 壁垒终结 | 巨大紫黑晶体壁垒拔地而起，用于支撑终结牌 `card_bulwark_finish` 的高额防御和压迫感。        | ![[game/assets/art/cards/artwork/card_bulwark_finish.png\|壁垒终结卡面]]   |

## 第一版 MVP 战斗 VFX 资源

第一版 MVP 战斗特效资源统一放入 `game/assets/art/vfx/`，用于 Godot 2D 动画图层。所有 VFX 应保持透明背景、粗墨线、网点阴影和高对比漫画风格，并由规则层输出的结构化日志事件驱动播放，避免表现层自行推断伤害、连锁、防御或资源收益。

| 反馈场景 | 用途 | 资源 |
| --- | --- | --- |
| 普通行动牌命中 | 普通斩击、轻攻击、基础攻击反馈。 | ![[game/assets/art/vfx/vfx_slash_speed_lines.png\|普通斩击速度线]] |
| 高费 / 重击命中 | 重斩、破防、强打的冲击格反馈。 | ![[game/assets/art/vfx/vfx_heavy_strike_impact_frame.png\|重击冲击格]] |
| 群体攻击 | 群体横扫和多目标命中的宽幅弧光。 | ![[game/assets/art/vfx/vfx_group_sweep_arc_light.png\|群体横扫弧光]] |
| 防御获得 / 格挡强调 | 技能防御、终结防御或遗物防御收益。 | ![[game/assets/art/vfx/vfx_defense_shield_flash.png\|防御护盾闪光]] |
| 连锁层数提升 | 每次行动牌或特殊效果提升连锁时的小反馈。 | ![[game/assets/art/vfx/vfx_chain_gain_spark.png\|连锁提升火花]] |
| 3 层阈值达成 | 低阈值教学反馈，提示终结牌基础可用。 | ![[game/assets/art/vfx/vfx_chain_threshold_3_burst.png\|3 层阈值小爆闪]] |
| 5 层阈值达成 | 中阈值反馈，提示续航 / 额外效果进入可用区。 | ![[game/assets/art/vfx/vfx_chain_threshold_5_burst.png\|5 层阈值中爆闪]] |
| 8 层阈值达成 | 高阈值反馈，提示成型爆发窗口。 | ![[game/assets/art/vfx/vfx_chain_threshold_8_burst.png\|8 层阈值大爆闪]] |
| 终结牌释放 | 终结牌出手、连锁兑现和高爆发前摇 / 命中叠层。 | ![[game/assets/art/vfx/vfx_finisher_release_shockwave.png\|终结牌紫黑冲击波]] |
| 敌人受击 | 敌人受到伤害时的命中爆点，可叠加伤害数字。 | ![[game/assets/art/vfx/vfx_enemy_hit_comic_burst.png\|敌人受击漫画爆点]] |

## 运行时组合规则

Godot 表现层应通过 `game/data/presentation/assets.json`、`card_views.json`、`enemy_views.json`、`relic_views.json` 和 `reward_pack_views.json` 组合视觉资源。卡牌在运行时由类型模板、卡面插画、规则文本和状态高亮叠加生成；敌人由遭遇实例和 `enemy_views` 的立绘资源生成；奖励包和遗物由对应 view 显示。新增美术资源只有进入 asset manifest 并被 view 引用后，才视为已接入游戏。

## 设计准则

- 漫画书感不能压低可读性；卡牌文本、敌人意图、状态图标、伤害数字和连锁层数必须保持清楚。
- 手绘漫画风格应服务动作张力和终结牌演出，例如夸张命中姿态、速度线、分格式冲击、局部特写、拟声字或高对比轮廓。
- UI 可以使用漫画格、页边、墨线框和纸张纹理作为结构语言，但核心状态信息必须保持稳定位置和稳定尺寸，避免因为分镜演出导致玩家找不到生命、行动点、连锁层数、敌人意图或手牌。
- 特效应强调结果和节奏，不遮挡关键数值。
- 连锁层数提升、高连锁阈值达成和终结牌释放应拥有逐级增强的视觉反馈。
- 主角出牌动作应由卡牌效果驱动，而不是只由卡牌类型驱动；同为行动牌也应尽量区分轻击、重击、破防和位移动作，同为技能牌也应区分防御、过牌、观察和资源调整动作。若制作资源不足，允许先以动作类别复用，但终结牌不在复用范围内。
- 每张终结牌需要独立的卡牌美术、释放动作或特效层资源，用于支撑高连锁爆发的截图记忆点；MVP 阶段至少覆盖所有已录入终结牌。
- 巨额伤害和夸张特效可以作为核心爽感，但必须保持敌我生命、伤害数字、意图和后续操作可读。
- 伤害数字、暴击、倍率变化和资源返还等数值反馈应与特效节奏同步，避免玩家只看到动画却看不懂收益来源。
- 打击感应通过命中帧、短暂停顿、目标反应、数字弹出、屏幕震动或镜头强调等方式表达，但不能影响规则可读性。
- 同类信息使用统一色彩和形状语言。
- 稀有度、状态类型和目标类型需要稳定视觉编码。
- 临时占位图也应保持尺寸和信息层级稳定。

## 待验证问题

- 手绘漫画书风格如何绑定具体世界观、角色身份和敌人来源。
- 卡牌插画是否首版必须完整。
- 是否采用 2D、伪 3D 或混合表现。

## 关联文档

- [[design/05_narrative_world/00_world_and_tone|世界观与语气]]
- [[design/03_experience/00_ui_ux|界面与交互]]
- [[design/02_content_systems/02_enemies_and_bosses|敌人与 Boss]]
- [[design/02_content_systems/03_relics_items_rewards|遗物、道具与奖励]]
- [[design/01_core_gameplay/03_card_system|卡牌系统]]
- [[design/01_core_gameplay/02_combat_system|战斗系统]]
