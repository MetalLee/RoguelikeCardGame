# GPT-Generated Battle UI Components V2

来源标准图：![[design/03_experience/assets/battle_ui_effect_concept_2026-06-09_v2.png|第一版 MVP 战斗场景设计标准]]

本目录用于归档按标准图风格重新生成的战斗 UI 元素。此次没有从设计图裁切或抠图；每个组件都是独立图像生成结果。

## 与当前运行画面的主要差异

- 当前运行画面仍保留顶部战斗标题和教学文案，占用了中上方连锁区域；标准图要求中上方用于 `8` 格连锁点进度。
- 当前玩家 HUD 位于左上但布局仍是旧原型色块；标准图要求左上为明亮漫画风生命、防御、状态条，不显示头像。
- 当前敌人状态条放在敌人脚下附近；标准图要求右上角保留敌人 HUD 区域，不显示头像。运行时仅在敌人 hover 或单体目标箭头指向时显示该敌人的名称、生命、防御、状态和意图，默认不显示敌人信息。
- 当前连锁显示为 `1 / 3 5 8`；标准图要求 `连锁 X/8` 加 `8` 个固定槽位，`3 / 5 / 8` 只做弱提示。
- 当前手牌基本横向排列且占用较大中下区域；标准图要求底部中间紧凑扇形叠放，并给主角 / 敌人保留更大垂直舞台空间。
- 当前抽牌堆、弃牌堆、行动点和结束回合按钮仍偏原型化；标准图要求左下 AP + 抽牌堆、右下弃牌堆 + 结束回合按钮，保持稳定角落布局。

## 组件文件

| 组件 | 文件 | 说明 |
| --- | --- | --- |
| 背景 | `battle_background_generated.png` | GPT 生成的无 UI 战斗背景参考。 |
| 玩家血量条 | `ui_player_health_bar_generated.png` | 左上玩家血量条，包含心形图标；已处理为真实 alpha PNG，不含文字和数字。 |
| 玩家护盾条 | `ui_player_block_bar_generated.png` | 左上玩家护盾条，包含盾牌图标；已处理为真实 alpha PNG，不含文字和数字。 |
| 敌人名称条 | `ui_enemy_name_bar_generated.png` | 右上敌人名称条；已处理为真实 alpha PNG，不含文字和数字。 |
| 敌人血量条 | `ui_enemy_health_bar_generated.png` | 右上敌人血量条，包含心形图标；已处理为真实 alpha PNG，不含文字和数字。 |
| 敌人护盾条 | `ui_enemy_block_bar_generated.png` | 右上敌人护盾条，包含盾牌图标；已处理为真实 alpha PNG，不含文字和数字。 |
| 连锁点 | `ui_chain_meter_8_slots_generated.png` | 中上方 8 格空连锁槽；已处理为真实 alpha PNG，不含文字，第一格红点已移除。 |
| 连锁红点 | `ui_chain_point_red_generated.png` | 独立红色连锁点；已处理为真实 alpha PNG，用于运行时叠入连锁槽。 |
| 行动点 | `ui_action_point_badge_generated.png` | 左下行动点圆形徽章；已处理为真实 alpha PNG，中间留空，由 Godot 动态绘制行动点数字和 `AP`。 |
| 抽牌堆 | `ui_draw_pile_panel_generated.png` | 左下抽牌堆图标 / 空面板；已处理为真实 alpha PNG，不含文字和数字。 |
| 弃牌堆 | `ui_discard_pile_panel_generated.png` | 右下弃牌堆图标 / 空面板；已处理为真实 alpha PNG，不含文字和数字。 |
| 结束回合 | `ui_end_turn_button_generated.png` | 右下结束回合按钮框；已处理为真实 alpha PNG，不含文字。 |

## 透明度状态

`gpt-image-2` 在当前工具链下直接生成的大多数 PNG 为 `RGB`，没有真实 alpha 通道。部分图像虽然看起来像透明棋盘格，但棋盘格是被绘制进图片的像素，不是透明背景。

例外：`ui_action_point_badge_generated.png`、`ui_player_health_bar_generated.png`、`ui_player_block_bar_generated.png`、`ui_enemy_name_bar_generated.png`、`ui_enemy_health_bar_generated.png`、`ui_enemy_block_bar_generated.png`、`ui_chain_meter_8_slots_generated.png`、`ui_chain_point_red_generated.png`、`ui_draw_pile_panel_generated.png`、`ui_discard_pile_panel_generated.png` 和 `ui_end_turn_button_generated.png` 已重新生成绿幕源图，并将绿幕背景处理为真实 alpha；这些文件可作为透明 PNG 叠加使用。

`ui_player_health_bar_generated.png` 和 `ui_player_block_bar_generated.png` 是从玩家整合 HUD 拆出的独立组件；没有生成第三行攻击 / 连锁 / 增益状态栏。

`ui_enemy_name_bar_generated.png`、`ui_enemy_health_bar_generated.png` 和 `ui_enemy_block_bar_generated.png` 是从敌人整合 HUD 拆出的独立组件；没有生成第四行攻击意图 / 状态栏。

`ui_chain_meter_8_slots_generated.png` 是空槽底图，所有 8 个槽都保持未填充；`ui_chain_point_red_generated.png` 是独立填充点，由 Godot 根据当前连锁层数叠加到槽位中。

因此这些文件更适合作为：

- UI 风格参考；
- 重新生成原生透明资源的 prompt 参考；
- Godot 中用 `PanelContainer`、`NinePatchRect`、`TextureRect`、`StyleBoxTexture` 和动态 Label 还原布局的视觉目标。

如果后续一定要得到真正透明 PNG，有两条可选路线：

- 使用支持原生透明背景的图像模型路径重新生成。
- 使用后处理去背景，但这属于你本轮明确不希望采用的“抠图”路线。

## 字体标注

设计图是 AI 生成的扁平位图，没有可读取的真实字体元数据。Godot 实现建议使用仓库已接入字体：

- `game/assets/fonts/source_han_sans_sc/SourceHanSansSC-Heavy.otf`：HUD 大数字、生命值、行动点数字、`连锁 X/8`、敌人名称、`结束回合`。
- `game/assets/fonts/source_han_sans_sc/SourceHanSansSC-Medium.otf`：`抽牌`、`弃牌`、意图说明、状态说明、tooltip 和其他辅助文本。

建议字号以 1920x1080 为基准：

- 生命 / 防御数值：28-36 px。
- 行动点数字：56-72 px。
- 连锁标题：30-40 px。
- 按钮文字：30-40 px。
- 抽牌 / 弃牌标签和计数：24-34 px。

所有正式数值与文本都应由 Godot UI 动态绘制，组件图中的任何文字或数字都只能作为视觉参考。
