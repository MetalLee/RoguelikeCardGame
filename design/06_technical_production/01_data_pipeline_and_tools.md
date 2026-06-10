# 数据管线与工具

状态：草案  
上级索引：[[design/README|Design Knowledge Base]]

## 目的

定义内容如何被设计、录入、校验、测试和发布。Roguelike 卡牌游戏的扩展效率高度依赖数据管线；本项目的工具链必须让开发者和 Codex 都能安全修改内容，而不把规则散落在不可追踪的场景节点里。

## 数据来源分工

- `design/`：记录设计意图、规则准则、范围边界和待验证问题，是最高设计依据。
- 游戏工程数据目录：记录可被 Godot 读取的实现参数，例如卡牌数值、敌人行为、奖励池和文本键。
- Godot 场景与资源：记录表现层结构、动画、音频、贴图、字体和 UI 布局。
- 试玩日志与指标导出：记录实际运行数据，用于回填 [[design/04_balance_data/02_metrics_and_playtest_data|指标与测试数据]] 和 [[design/07_production/02_playtest_plan|试玩测试计划]]。

设计文档记录“为什么”和“应该怎样体验”；数据文件记录“当前实现参数”；代码记录“如何结算规则”。三者不应互相替代。

## 首版数据格式

首版内容数据采用 UTF-8 JSON + JSON Schema 校验。理由：

- 文本格式便于 Git diff、Code Review、Codex 修改和批量校验。
- JSON Schema 能检查必填字段、枚举、引用、数值范围和版本字段。
- Godot C# 可直接加载和反序列化 JSON，规则层可脱离 Godot 场景进行测试。
- 后续如需要策划表格，可从 CSV / XLSX 导出到同一套 JSON 中间格式，但运行时内容数据仍以统一 JSON 管线为准。

Godot `.tres`、`.res` 或场景资源主要用于表现资源、动画、UI 和素材引用；不作为首版卡牌 / 敌人 / 奖励数值的唯一来源。若后续团队更偏向 Godot Inspector 编辑内容，需要先保证可导出、可校验、可 diff，并保留稳定内容 ID。

## 内容 ID 规则

- 每个内容对象必须有稳定 ID，不随显示名称、语言或美术资源变化。
- ID 使用小写英文、数字、下划线和命名空间点号，例如 `card.basic_strike`、`enemy.chain_warden`、`encounter.mvp.normal_03`。
- ID 一旦被存档、日志或外部数据引用，不应随意重命名；确需替换时使用迁移表。
- 显示名称、描述文本和风味文本使用本地化键，不作为逻辑引用。

## 数据对象

### 目录分层

当前内容数据采用规则层、表现层、资源清单和本地化分离：

- `game/data/gameplay/`：卡牌、敌人、遗物、遭遇、奖励包和 Run 序列等可结算规则数据。
- `game/data/presentation/`：卡牌 view、敌人 view、遗物 view、奖励包 view 和 asset manifest。
- `game/data/localization/`：不同语言文本。
- `game/data/schemas/gameplay/` 与 `game/data/schemas/presentation/`：对应 JSON Schema。

`gameplay/` 不允许放入 `text_key`、`art_key`、`icon_key`、`ui_name_key` 等表现字段；这些字段归 `presentation/` 管理。

### 卡牌

规则数据位于 `game/data/gameplay/cards/cards.json`。必备字段：

- `id`：例如 `card.basic_strike`。
- `card_type`：行动牌 / 技能牌 / 终结牌。
- `rarity`
- `costs`：资源消耗数组，例如行动点消耗 `{ "resource": "action_point", "amount": 1 }`。
- `requirements`：打出条件数组，例如终结牌的连锁要求 `{ "op": "resource_at_least", "resource": "chain", "amount": 3 }`。
- `targeting`：是否需要选择目标、目标阵营和目标数量。
- `effects`：typed effect DSL。
- `after_play`：使用后移动、消耗、保留等牌区变化。
- `tags`
- `balance`：平衡定位，不参与规则结算。
- `vfx_asset`：可选卡牌特效资源 ID，用于声明这张牌在运行时触发的主视觉特效。该字段只保存 `asset.vfx.*` 稳定资源 ID，不保存文件路径、文本或 UI 布局参数。

表现数据位于 `game/data/presentation/card_views.json`，通过同一个 `id` 关联卡牌名称、规则文本、风味文本、卡牌模板 asset 和卡面 asset。

卡牌效果使用模板化 DSL，例如 `damage`、`gain_block`、`draw_cards`、`gain_resource`、`set_resource`、`conditional`、`temporary_discount`。行动牌的 +1 连锁、终结牌的连锁消耗和连锁阈值奖励都必须显式写入 `effects` / `requirements`，不再使用 `default_chain_delta` 或 `min_chain` 作为数据字段。

卡牌 VFX 属于“卡牌使用反馈的资源选择”，应随卡牌定义一起维护，避免表现层按卡牌类型、费用或卡牌名硬编码猜测特效。表现层只读取 `CardDefinition.VfxAsset` 并播放对应资源；若未指定 `vfx_asset`，运行时回退到默认命中特效 `asset.vfx.enemy_hit_comic_burst`。

### 遗物

规则数据位于 `game/data/gameplay/relics/relics.json`。必备字段：

- `id`
- `rarity`
- `trigger`
- `conditions`
- `effects`
- `stack_rule`
- `tags`

表现数据位于 `game/data/presentation/relic_views.json`，关联名称文本、规则文本和图标 asset。

### 敌人和 Boss

规则数据位于 `game/data/gameplay/enemies/enemies.json`。必备字段：

- `id`
- `rank`：普通、精英或 Boss。
- `stats`
- `ai`：第一版 MVP 使用 `fixed_sequence`，包含 `loop` 和 `intents`。
- `immunities`
- `tags`

敌人意图需要同时定义：

- `preview`：UI 展示给玩家看的承诺，例如攻击数值、防御数值。
- `effects`：真实结算效果。

表现数据位于 `game/data/presentation/enemy_views.json`，关联敌人名称、立绘 asset 和每个 intent id 的文本 key。后续普通战斗随机池成立后，再扩展 `ai.type` 为行为池、权重或阶段制 Boss AI。

### 遭遇

必备字段：

- `id`
- `node_type`
- `enemies`
- `reward_profile`
- `teaching_goal`
- `difficulty_note`

MVP 遭遇 ID 应对应固定节点：普通战斗 1、普通战斗 2、普通战斗 3、精英战斗、普通战斗 4、Boss 战斗。

### 奖励包

规则数据位于 `game/data/gameplay/rewards/reward_packs.json`。必备字段：

- `id`
- `pack_type`：行动牌包 / 技能牌包 / 终结牌包。
- `candidate_ids`
- `pick`
- `guarantee_rule`
- `repeat_rule`
- `tags`

第一版 MVP 使用固定可重复池：每类卡牌包固定显示该类型 3 张奖励牌，拿过的牌仍可再次出现并允许重复加入同名牌。

### 状态与关键词

状态和关键词必须与 [[design/08_governance/02_glossary|术语表]] 保持一致。正式进入游戏的数据对象需要补充显示文本、图标、触发时机、层数规则和持续时间规则。

### 固定 Run 序列

第一版 MVP 的 Run 序列应以数据配置：

1. 普通战斗 1。
2. 普通战斗 2。
3. 普通战斗 3。
4. 精英战斗。
5. 普通战斗 4。
6. Boss 战斗。

不要把这个顺序写死在 UI 或场景跳转脚本中；后续路线图系统应能替换这层配置。

## 校验规则

每次运行游戏、导出版本或提交内容前，应至少执行以下校验：

- 内容 ID 唯一。
- `gameplay/` 中不得出现表现字段，例如 `text_key`、`art_key`、`icon_key`、`ui_name_key`。
- 引用的卡牌、敌人、遗物、状态、文本键和 asset ID 存在。
- 行动牌必须显式获得 1 点连锁。
- 技能牌默认不得获得连锁，除非后续设计文档明确说明例外。
- 终结牌必须通过 `requirements` 显式声明最低连锁要求，并通过 `effects` 显式清空连锁。
- 奖励包候选数量满足 3 张候选和 0-3 张加入规则。
- MVP 前两次普通战斗后的终结牌包包含群体攻击终结牌。
- 敌人意图必须同时具备 UI `preview` 和可结算 `effects`。
- 所有数值在当前阶段允许范围内；异常高值必须显式标记为测试用。
- 文本键缺失、重复或指向空文本时报错。
- asset manifest 中引用的本地资源路径必须存在。
- 卡牌 `vfx_asset` 若存在，必须引用 `game/data/presentation/assets.json` 中已登记的 `asset.vfx.*` 资源 ID。
- 存档版本和内容版本字段存在。

## 开发工具需求

### 数据校验命令

提供一个可由 Codex 和开发者运行的校验命令，用于检查 JSON Schema、引用关系和 MVP 特殊规则。校验失败应输出文件路径、内容 ID、字段名和失败原因。

### 战斗沙盒

提供开发者入口，支持：

- 选择固定遭遇。
- 选择初始牌组或自定义牌组。
- 设置随机种子。
- 直接给予指定遗物。
- 设置初始生命、行动点和连锁层数。
- 跳过动画或加速动画。

战斗沙盒是验证卡牌效果、敌人行为、连锁阈值和动画反馈的核心工具。

### 奖励包预览

提供奖励包调试界面，支持查看行动牌包、技能牌包、终结牌包的候选、保底规则、可重复规则和文本展示。

### 结构化战斗日志

战斗日志至少记录：

- 回合数。
- 出牌顺序。
- 卡牌 ID。
- 目标 ID。
- 行动点变化。
- 连锁层数变化。
- 伤害、防御、治疗、抽牌和状态变化。
- 敌人意图和敌人行动。
- 终结牌触发阈值和额外效果。
- 随机数调用点。

日志需要支持开发者面板查看，也需要能导出为本地 JSONL 或 CSV，供试玩复盘。

### 试玩指标导出

第一版 MVP 至少导出：

- Run 种子。
- 节点顺序。
- 每场战斗回合数、受伤、死亡或胜利。
- 最高连锁层数。
- 达到 3 / 5 / 8 层次数。
- 终结牌使用次数和触发额外效果次数。
- 卡牌包类型选择、实际拿牌数量和卡牌 ID。
- 遗物获得节点。
- MVP 通关或失败节点。
- 总时长。

## Codex 协作流程

- Codex 修改玩法前必须先读取相关 `design/` 文档。
- Codex 新增或修改数据对象时，应同步更新数据校验、示例数据或测试用例。
- Codex 不应只改运行时代码而跳过内容数据；能数据化的内容优先进入数据文件。
- Codex 生成新卡牌、敌人或遗物时，应使用 [[design/_templates/content_spec_template|内容规格模板]] 或等价字段描述意图。
- Codex 完成会影响设计理解的改动后，应更新 [[design/08_governance/01_change_log|变更日志]]；重大技术或玩法决策应更新 [[design/08_governance/00_decision_log|决策记录]]。

## 资产与文本管线

- 美术、音频和特效资源通过 `game/data/presentation/assets.json` 中的稳定 asset ID 关联内容 ID。
- 临时占位资源也需要稳定尺寸和命名，避免 UI 反复抖动。
- 文本采用本地化键，首版默认简体中文文本，保留后续英文文本扩展能力。
- 卡牌规则文本必须优先清楚，不用世界观风味替代规则说明。

### 卡牌特效资源规范

单张卡牌的主特效资源通过 `game/data/gameplay/cards/cards.json` 中的 `vfx_asset` 引用，并由 `game/data/presentation/assets.json` 统一登记。新增卡牌时，应优先为关键卡牌准备专属 VFX；临时没有专属资源时，可以省略 `vfx_asset`，表现层使用 `asset.vfx.enemy_hit_comic_burst` 作为默认命中特效。

- VFX PNG 资源放入 `game/assets/art/vfx/`，保持透明背景、稳定尺寸和漫画书风格。
- 资源文件名建议使用 `vfx_` + 卡牌语义名，例如 `vfx_basic_strike.png`、`vfx_heavy_strike.png`。
- asset ID 建议使用 `asset.vfx.<card_semantic_name>`，例如 `asset.vfx.basic_strike`、`asset.vfx.heavy_strike`。
- 多张卡牌可以在原型期复用同一个 VFX 资源，但表现层不应再根据行动牌 / 技能牌 / 终结牌或费用进行硬编码推断。
- 新增或替换 VFX 时，需要同步更新 `assets.json`、卡牌 `vfx_asset`、JSON Schema / 数据校验和必要的 smoke test。
- `vfx_asset` 只决定卡牌主视觉反馈；伤害、防御、连锁、抽牌、费用和胜负仍由规则层结算并通过结构化日志驱动表现层播放。

### 卡牌视觉素材组合

卡牌在运行时应优先由 Godot 表现层动态组合，而不是为每张卡牌维护一张已经合并文本、模板和卡面的完整图片。推荐结构为：卡牌类型模板、卡面插画、费用 / 名称 / 类型 / 规则文本 / 连锁条件标签，以及悬停、选中、费用不足、目标缺失、连锁不足等状态叠层。

- 三类卡牌模板放在 `game/assets/art/cards/templates/`，用于承载行动牌、技能牌和终结牌的卡框、色彩、装饰与安全区。
- 单张卡牌插画放在 `game/assets/art/cards/artwork/`，文件名优先使用对应卡牌 ID，例如 `card_basic_strike.png`。
- 单张卡牌插画使用稳定 asset ID 关联内容 ID；卡牌名称、规则文本、数值和本地化文本分别从 `gameplay/`、`presentation/` 与 `localization/` 读取。第一版 MVP 中已归档全部行动牌、技能牌和终结牌卡面，资源键采用 `asset.card.<card_id_without_prefix>.art` 形式，例如 `asset.card.basic_strike.art`。
- Godot UI 负责将模板、插画和文本实时组合展示；表现层只读取规则层 / 数据层结果，不把卡牌数值烘焙进图片。
- 卡牌状态变化应通过 Godot 节点、材质、Shader、颜色调制或叠层实现，避免为每个状态维护额外完整卡图。
- 只有在 UI 样式和卡牌内容稳定后，才考虑增加可选的离线预烘焙或缓存工具，用于图鉴缩略图、奖励预览批量缓存或低端设备性能优化；该缓存不能替代原始数据和分层素材。

该约定与 [[design/01_core_gameplay/03_card_system|卡牌系统]] 的数据驱动原则一致：卡牌规则、文本和数值保持可 diff、可校验、可本地化，卡面视觉资源则作为表现层素材引用。

## 首版暂不做

- 大型可视化内容编辑器。
- 在线内容热更新。
- 在线遥测后台。
- 多人协同数据库或云端策划工具。
- 复杂脚本语言嵌入式效果系统。

## 关联文档

- [[design/02_content_systems/00_content_taxonomy|内容分类法]]
- [[design/04_balance_data/02_metrics_and_playtest_data|指标与测试数据]]
- [[design/06_technical_production/00_technical_requirements|技术需求]]
- [[design/06_technical_production/02_save_config_platform|存档、配置与平台]]
