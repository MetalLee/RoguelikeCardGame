# 界面与交互

状态：草案  
上级索引：[[design/README|Design Knowledge Base]]

## 目的

定义玩家如何读取状态、理解意图、执行操作和复盘结果。Roguelike 卡牌游戏的 UI 是核心玩法的一部分。

## 核心界面

- 主菜单。
- 角色选择。
- 地图路线。
- 战斗界面。
- 奖励选择。
- 商店。
- 事件。
- 牌库查看。
- 遗物和状态查看。
- 胜利与失败结算。

## 战斗界面信息优先级

1. 玩家生命、资源、当前连锁层数、手牌。
2. 敌人生命、意图、状态。
3. 当前可执行行动。
4. 抽牌堆、弃牌堆、消耗堆。
5. 遗物、持续效果和战斗日志。

## 第一版 MVP 界面方向

第一版 MVP 的界面应服务“手绘漫画书风格”。战斗界面可以像一页动态漫画：战斗舞台使用清晰的漫画格或页边框组织角色、敌人与反馈，关键攻击和终结牌释放可以短暂切入冲击格、速度线、局部特写或拟声字，但这些演出不能移动核心 UI 的稳定位置。

MVP 战斗界面默认按 PC 16:9 横屏设计：玩家状态、行动点、防御和生命保持在左下或下方稳定区；敌人生命、意图和状态显示在敌人附近；手牌位于底部；当前连锁层数和 3 / 5 / 8 阈值应处于玩家最容易扫视的位置；抽牌堆、弃牌堆、遗物、战斗日志和调试信息放在边缘辅助区。

奖励、结算和牌库查看界面可以延续漫画书的页框、墨线和纸张纹理，但不应把核心选择做成纯插画。普通战斗奖励仍需清楚表达“先选卡牌包类型，再从 3 张同类型候选中选择 0-3 张加入卡组”的两段式流程。

## 第一版 MVP 运行时资源接入

第一版 MVP 的 Godot 入口需要从 `game/data/presentation/` 读取表现 view 与 asset manifest，将背景、主角、敌人、卡牌模板、卡面插画、卡牌包、遗物图标和 UI 图标接入实际界面。资源文件只提供视觉层；卡牌名、费用、连锁需求、效果文本、生命、防御、意图和日志仍由 Godot UI 根据规则层和本地化数据绘制。

当前主菜单、战斗、奖励选择和结算流程可以先由程序化 UI 承载，但不得回退到无资源的纯占位界面。后续若拆分独立 `BattleScreen`、`RewardScreen` 或卡牌 / 敌人组件场景，应继续复用同一套 `presentation` 数据，不在场景节点里硬编码卡牌、敌人或奖励资源路径。

## 交互准则

- 卡牌目标、费用不足、无法行动原因必须即时反馈。
- 当前连锁层数、下一层变化和已满足的高连锁条件必须即时反馈。
- 连锁层数 UI 需要清楚标出 3 / 5 / 8 三个阈值，并在即将达到下一档时提供可读提示。
- 连锁层数不设上限，UI 需要能显示超过 8 层后的当前层数，并避免让玩家误解 8 层是硬上限。
- 终结牌触发时需要清楚展示关键数值变化，例如伤害、倍率、暴击率、抽牌数、回费数或状态层数。
- 当卡牌因连锁层数满足额外效果时，卡牌本体需要有明显但不过度干扰的高亮状态。
- 终结牌需要预告结算后连锁层数是否归零；若存在保留效果，必须显示保留后的预计层数。
- 回合结束前需要提示连锁层数将归零，除非当前遗物或卡牌效果会保留部分或全部层数。
- 敌人意图和状态说明应可悬停或点击查看。
- 关键决策需要确认，普通重复操作不应频繁打断。
- UI 文案优先清晰，少用只服务氛围的模糊表达。

## 第一版 MVP UI 图标资源

第一版 MVP 的战斗 UI 图标资源统一放入 `game/assets/art/ui/icons/`。图标用于辅助颜色信息，必须和文字 / 数值一起服务可读性；图标本身不包含文字或数字，正式数值仍由 Godot UI 绘制。

| UI 信息 / 状态 | 资源 |
| --- | --- |
| 生命 | ![[game/assets/art/ui/icons/ui_icon_life.png\|生命图标]] |
| 玩家防御 | ![[game/assets/art/ui/icons/ui_icon_player_block.png\|玩家防御图标]] |
| 行动点 | ![[game/assets/art/ui/icons/ui_icon_action_point.png\|行动点图标]] |
| 连锁层数 | ![[game/assets/art/ui/icons/ui_icon_chain_count.png\|连锁层数图标]] |
| 攻击意图 | ![[game/assets/art/ui/icons/ui_icon_attack_intent.png\|攻击意图图标]] |
| 防御意图 | ![[game/assets/art/ui/icons/ui_icon_defend_intent.png\|防御意图图标]] |
| 压迫 / 混合意图 | ![[game/assets/art/ui/icons/ui_icon_pressure_mixed_intent.png\|压迫混合意图图标]] |
| 抽牌堆 | ![[game/assets/art/ui/icons/ui_icon_draw_pile.png\|抽牌堆图标]] |
| 弃牌堆 | ![[game/assets/art/ui/icons/ui_icon_discard_pile.png\|弃牌堆图标]] |
| 牌库 | ![[game/assets/art/ui/icons/ui_icon_deck_library.png\|牌库图标]] |
| 结束回合 | ![[game/assets/art/ui/icons/ui_icon_end_turn.png\|结束回合图标]] |
| 目标选中 | ![[game/assets/art/ui/icons/ui_icon_target_selected.png\|目标选中图标]] |
| 可打出高亮 | ![[game/assets/art/ui/icons/ui_icon_playable_highlight.png\|可打出高亮图标]] |
| 费用不足 | ![[game/assets/art/ui/icons/ui_icon_insufficient_cost.png\|费用不足图标]] |
| 连锁不足 | ![[game/assets/art/ui/icons/ui_icon_insufficient_chain.png\|连锁不足图标]] |

完整图标母版保留为 ![[game/assets/art/ui/icons/ui_icon_sheet.png|第一版 MVP UI 图标母版]]，用于后续补充同风格图标时参考。

## 关联文档

- [[design/01_core_gameplay/02_combat_system|战斗系统]]
- [[design/03_experience/03_onboarding_accessibility|引导与可访问性]]
- [[design/03_experience/01_visual_direction|视觉方向]]
