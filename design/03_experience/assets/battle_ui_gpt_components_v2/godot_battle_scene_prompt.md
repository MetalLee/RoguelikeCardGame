# Prompt For Codex: Implement Battle Scene To Design Standard

```text
你在 D:\Codex Workspace\RoguelikeCardGame 工作。本仓库是 Obsidian 友好的 Roguelike 卡牌游戏知识库，Godot 工程根目录是 game/。

请先读取并遵循：
- AGENTS.md
- design/03_experience/00_ui_ux.md
- design/03_experience/01_visual_direction.md
- design/06_technical_production/00_technical_requirements.md
- design/03_experience/assets/battle_ui_effect_concept_2026-06-09_v2.png
- design/03_experience/assets/battle_ui_gpt_components_v2/README.md
- design/03_experience/assets/battle_ui_gpt_components_v2/component_manifest.json

任务：把当前 Godot 战斗场景改到 battle_ui_effect_concept_2026-06-09_v2.png 的布局与气质。

重要说明：
- battle_ui_gpt_components_v2/ 中的 PNG 是 gpt-image-2 风格生成组件，不是从设计图抠出来的素材。
- 当前可接入的 UI 组件已经按 component_manifest.json 处理为真实 alpha PNG；实现时优先通过 manifest 中 `alpha: true` 的拆分组件直接用 TextureRect / NinePatchRect / TextureButton 叠加，再用 Godot Label 动态绘制文本和数值。
- `ui_player_status_hud_generated.png`、`ui_enemy_status_hud_generated.png` 等整合旧图只作为历史风格参考；正式战斗 UI 应使用拆分后的玩家血量条、玩家护盾条、敌人名称条、敌人血量条、敌人护盾条、8 槽连锁条、连锁红点、AP 徽章、抽牌堆、弃牌堆和结束回合按钮组件。
- 所有中文文本、数字、卡牌名、费用、生命、防御、连锁、抽弃牌数量和意图说明都必须由 Godot UI 动态绘制，不要烘焙到图片里。

当前运行画面与目标图的差异：
1. 当前顶部常驻显示“第 1/6 战”和教学文案；目标图中上方应为 8 格连锁进度。
2. 当前敌人状态条在敌人脚下；目标图要求敌人名称、HP、防御和意图信息在右上角。
3. 当前连锁显示为 “1 / 3 5 8”；目标图要求 “连锁 X/8” 和 8 个槽位，3/5/8 阈值只做弱提示。
4. 当前手牌更像横向铺开；目标图要求底部中间紧凑扇形叠放。
5. 当前 AP、抽牌堆、弃牌堆、结束回合按钮位置和视觉重量不符合标准图；目标图要求 AP + 抽牌堆在左下，弃牌堆 + 结束回合在右下。
6. 当前角色 / 敌人舞台垂直空间不足；目标图要求主角和敌人同一水平地面线，并给未来更高 Boss 留空间。

布局要求，以 16:9 / 1920x1080 为基准实现响应式锚点：
- 背景：使用明亮温暖的手绘漫画战斗背景，保留纸张纹理和墨线，不走暗黑哥特。
- 主角：左侧中下，朝右，脚底基线约在画面高度 62%-68% 区间。
- 敌人：右侧中下，朝左，与主角同一地面基线；普通敌人略小于主角，Boss 可向上扩展。
- 玩家 HUD：左上，使用 `player_health_bar` 和 `player_block_bar` 两个拆分 alpha 组件，分别显示 HP 和 Defense；不显示头像，不生成第三行状态栏。
- 敌人 HUD：右上，使用 `enemy_name_bar`、`enemy_health_bar` 和 `enemy_block_bar` 三个拆分 alpha 组件，分别显示敌人名称、HP 和 Defense；不显示头像，不生成第四行状态 / 意图栏。敌人意图可在右上 HUD 下方或旁侧用 Godot 原生 Label / Icon 动态绘制。
- 连锁 HUD：中上，使用 `chain_meter_8_slots` 作为 8 个空槽底图，使用 `chain_point_red` 作为填充点叠入槽位；显示文本 “连锁 X/8”。填充 min(chain, 8) 个槽；chain > 8 时文本显示 “连锁 X/8+” 或等价形式。
- 左下：使用 `action_point_badge`、`draw_pile_panel` alpha 组件，动态绘制行动点、抽牌标签和抽牌堆数量。
- 右下：使用 `discard_pile_panel`、`end_turn_button` alpha 组件，动态绘制弃牌标签、弃牌堆数量和结束回合按钮文字。
- 手牌：底部中间，竖版卡牌保持统一尺寸，5 张手牌时紧凑扇形叠放；中心卡可轻微抬高用于 hover / selected；不要遮挡角色和敌人主体。

字体要求：
- 使用 game/assets/fonts/source_han_sans_sc/SourceHanSansSC-Heavy.otf 绘制 HUD 大数字、敌人名称、连锁标题和结束回合按钮。
- 使用 game/assets/fonts/source_han_sans_sc/SourceHanSansSC-Medium.otf 绘制辅助标签、状态说明和 tooltip。
- 文本使用浅色描边或深色投影保证在纸张和墨线背景上清晰。

规则边界：
- 不改规则层。
- 表现层只读取 CombatState、RunState、DeckZones 和 CombatLogEvent 等结果，不自行结算伤害、抽牌、连锁、费用、防御或胜负。
- 不把卡牌、敌人、遗物、奖励数据写死在场景中。

完成后运行验证：
- dotnet build game/RoguelikeCardGame.csproj
- dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
- game/tools/data_validator/validate_data.ps1 或等价 Python 校验
- Godot headless 项目启动检查

完成后说明修改文件、验证结果，以及哪些视觉细节仍需后续专门美术资源替换。
```
