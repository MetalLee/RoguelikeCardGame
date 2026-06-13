# 数据管线与工具

状态：草案  
上级索引：[[design/README|Design Knowledge Base]]

## 目的

定义内容如何被设计、录入、校验、测试和发布。Roguelike 卡牌游戏的扩展效率高度依赖数据管线；本项目的工具链必须让开发者和 Codex 都能安全修改内容，而不把规则散落在不可追踪的场景节点里。

## 数据来源分工

- `design/`：记录设计意图、规则准则、范围边界和待验证问题，是最高设计依据。
- 游戏工程数据目录：记录可被 Godot 读取的实现参数，例如武器、卡牌数值、色彩、敌人行为、奖励池和文本键。
- Godot 场景与资源：记录表现层结构、动画、音频、贴图、字体和 UI 布局。
- 试玩日志与指标导出：记录实际运行数据，用于回填 [[design/04_balance_data/02_metrics_and_playtest_data|指标与测试数据]] 和 [[design/07_production/02_playtest_plan|试玩测试计划]]。

设计文档记录“为什么”和“应该怎样体验”；数据文件记录“当前实现参数”；代码记录“如何结算规则”。三者不应互相替代。

## 首版数据格式

首版内容数据采用 UTF-8 JSON + JSON Schema 校验。Godot `.tres`、`.res` 或场景资源主要用于表现资源、动画、UI 和素材引用；不作为首版卡牌 / 敌人 / 奖励数值的唯一来源。

## 内容 ID 规则

- 每个内容对象必须有稳定 ID，不随显示名称、语言或美术资源变化。
- ID 使用小写英文、数字、下划线和命名空间点号，例如 `weapon.revolver_sword`、`card.revolver_slash`、`color.red`、`enemy.tower_scout`。
- ID 一旦被存档、日志或外部数据引用，不应随意重命名；确需替换时使用迁移表。
- 显示名称、描述文本和风味文本使用本地化键，不作为逻辑引用。

## 数据对象

### 武器

规则数据建议位于 `game/data/gameplay/weapons/weapons.json`。必备字段：

- `id`
- `starting_pool_ids`
- `reward_pool_ids`
- `main_hand_allowed`
- `off_hand_allowed`
- `tags`

第一版 MVP 需要 `weapon.revolver_sword` 和 `weapon.mechanical_arm`。

### 色彩

规则数据建议位于 `game/data/gameplay/colors/colors.json`。必备字段：

- `id`：`color.red`、`color.yellow`、`color.blue`、`color.green`、`color.purple`。
- `role`
- `base_effect_template`
- `stack_rule`
- `tags`

### 卡牌

规则数据位于 `game/data/gameplay/cards/cards.json`。必备字段：

- `id`
- `weapon_id`
- `card_type`：`action` 或 `finisher`
- `rarity`
- `costs`
- `requirements`
- `targeting`
- `effects`
- `energy`：彩能生成、消耗、颜色继承或保留规则。
- `after_play`
- `tags`
- `balance`
- `vfx_asset`

行动牌若生成彩能，必须显式声明生成数量和是否继承附魔颜色。终结牌必须显式声明彩能需求、消耗规则和色彩结算方式。不再使用 `default_chain_delta`、`min_chain`、固定 3 / 5 / 8 阈值或 `skill` 卡牌类型。

表现数据位于 `game/data/presentation/card_views.json`，通过同一个 `id` 关联卡牌名称、规则文本、风味文本、卡牌模板 asset 和卡面 asset。

### 附魔与 Run 状态

运行时需要记录行动牌实例或卡牌 ID 的附魔状态：

- 未附魔。
- 红色。
- 黄色。
- 蓝色。
- 绿色。
- 紫色。

若同名牌允许多张且附魔状态不同，运行时需要区分卡牌实例 ID，不能只用卡牌定义 ID 表示整叠卡。

### 遗物

规则数据位于 `game/data/gameplay/relics/relics.json`。必备字段：

- `id`
- `rarity`
- `trigger`
- `conditions`
- `effects`
- `stack_rule`
- `tags`

Boss 遗物应与普通遗物区分稀有度或来源。

### 敌人和 Boss

规则数据位于 `game/data/gameplay/enemies/enemies.json`。必备字段：

- `id`
- `rank`：普通、精英或 Boss。
- `world_theme`
- `stats`
- `ai`
- `immunities`
- `tags`

敌人意图需要同时定义 `preview` 和可结算 `effects`。表现数据位于 `game/data/presentation/enemy_views.json`。

### 遭遇

必备字段：

- `id`
- `node_type`
- `enemies`
- `reward_profile`
- `teaching_goal`
- `world_context`
- `difficulty_note`

MVP 遭遇 ID 应对应固定节点：普通战斗 1、普通战斗 2、普通战斗 3、精英战斗、普通战斗 4、Boss 战斗。

### 奖励

普通战斗奖励应能表达：

- 随机色彩碎片。
- 武器卡牌三选一。

精英奖励额外表达普通遗物。Boss 奖励表达 Boss 遗物、Boss 特有卡牌或色彩核心。

旧 `reward_pack` 结构被新奖励结构替代；如果代码迁移阶段暂时保留旧文件，必须标记为历史兼容，不作为新设计依据。

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
- 引用的武器、卡牌、色彩、敌人、遗物、状态、文本键和 asset ID 存在。
- 每个 MVP 起始武器拥有 8 张初始卡牌。
- 左轮剑起始选择数量为 6，机械臂起始选择数量为 4。
- 卡牌类型只能是行动牌或终结牌。
- 行动牌若生成彩能，必须声明生成数量和颜色继承规则。
- 终结牌必须声明彩能需求和消耗规则。
- 不得出现旧 `skill` 卡牌类型、`default_chain_delta`、`min_chain` 或 3 / 5 / 8 阈值字段。
- 色彩碎片必须引用 5 种合法色彩之一。
- 普通战斗奖励必须包含色彩碎片和 3 张卡牌候选。
- 精英奖励必须包含普通遗物。
- Boss 奖励必须包含简化 Boss 奖励配置。
- 敌人意图必须同时具备 UI `preview` 和可结算 `effects`。
- 文本键缺失、重复或指向空文本时报错。
- asset manifest 中引用的本地资源路径必须存在。

## 开发工具需求

### 数据校验命令

提供一个可由 Codex 和开发者运行的校验命令，用于检查 JSON Schema、引用关系和 MVP 特殊规则。校验失败应输出文件路径、内容 ID、字段名和失败原因。

### 战斗沙盒

提供开发者入口，支持：

- 选择固定遭遇。
- 选择起始武器卡牌或自定义牌组。
- 设置随机种子。
- 直接给予指定遗物。
- 设置初始生命、行动点和彩能。
- 指定色彩碎片和附魔状态。
- 跳过动画或加速动画。

### 奖励预览

提供奖励调试界面，支持查看色彩碎片随机、武器卡牌三选一候选、精英遗物和 Boss 简化奖励。

### 结构化战斗日志

战斗日志至少记录：

- 回合数。
- 出牌顺序。
- 卡牌 ID。
- 目标 ID。
- 行动点变化。
- 彩能生成、消耗、颜色变化和清空。
- 色彩附加效果。
- 伤害、防御、治疗、抽牌和状态变化。
- 敌人意图和敌人行动。
- 终结牌消耗彩能数量和颜色构成。
- 随机数调用点。

## Codex 协作流程

- Codex 修改玩法前必须先读取相关 `design/` 文档。
- Codex 新增或修改数据对象时，应同步更新数据校验、示例数据或测试用例。
- Codex 不应只改运行时代码而跳过内容数据；能数据化的内容优先进入数据文件。
- Codex 完成会影响设计理解的改动后，应更新 [[design/08_governance/01_change_log|变更日志]]；重大技术或玩法决策应更新 [[design/08_governance/00_decision_log|决策记录]]。

## 关联文档

- [[design/02_content_systems/00_content_taxonomy|内容分类法]]
- [[design/04_balance_data/02_metrics_and_playtest_data|指标与测试数据]]
- [[design/06_technical_production/00_technical_requirements|技术需求]]
- [[design/06_technical_production/02_save_config_platform|存档、配置与平台]]
