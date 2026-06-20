# 数据管线与工具

状态：草案  
上级索引：[[design/README|Design Knowledge Base]]

## 目的

定义内容如何被设计、录入、校验、测试和发布。Roguelike 卡牌游戏的扩展效率高度依赖数据管线；本项目的工具链必须让开发者和 Codex 都能安全修改内容，而不把规则散落在不可追踪的场景节点里。

## 数据来源分工

- `design/`：记录设计意图、规则准则、范围边界和待验证问题，是最高设计依据。
- 游戏工程数据目录：记录可被 Godot 读取的实现参数，例如武器、卡牌数值、色彩、魔物行为、奖励池和文本键。
- Godot 场景与资源：记录表现层结构、动画、音频、贴图、字体和 UI 布局。
- 试玩日志与指标导出：记录实际运行数据，用于回填 [[design/04_balance_data/02_metrics_and_playtest_data|指标与测试数据]] 和 [[design/07_production/02_playtest_plan|试玩测试计划]]。

设计文档记录“为什么”和“应该怎样体验”；数据文件记录“当前实现参数”；代码记录“如何结算规则”。三者不应互相替代。

## 首版数据格式

首版内容数据采用 UTF-8 JSON + JSON Schema 校验。Godot `.tres`、`.res` 或场景资源主要用于表现资源、动画、UI 和素材引用；不作为首版卡牌 / 魔物 / 奖励数值的唯一来源。

## AI 占位美术生成与归档

后续通过 `gpt-image-2` 或其他 AI 图像工具生成的第一版 MVP 占位美术资源，必须优先遵循 [[design/03_experience/01_visual_direction|视觉方向]] 中的“MVP 占位美术最高准则”。`design/03_experience/assets/` 下的黑白线稿资源是最高风格参考；生成结果若与该风格冲突，不应进入正式 asset manifest。

生成前要求：

- 明确资产用途：角色、魔物、Boss、卡牌插画、VFX、UI 图标、背景层、奖励物或结算图。
- 明确输出形态：透明 PNG 零件、16:9 背景层、序列帧参考或 UI 图标。
- 提示词必须包含黑白手绘漫画线稿、高留白、粗细墨线、少量排线、纸面构图等约束；若资产不需要机制色，应明确要求无彩色。
- 若资产需要机制色，只允许使用红、黄、蓝、绿、紫作为小面积玩法编码。

归档前验收：

- 与 [[design/03_experience/assets/视觉参考.jpg|视觉参考]]、[[design/03_experience/assets/人物动作动画/人物.png|人物]]、[[design/03_experience/assets/魔物序列帧动画/骷髅1-5.png|骷髅1-5]]、[[design/03_experience/assets/爆炸效果.png|爆炸效果]] 并排查看，线条、留白、黑白灰比例和块面语言不突兀。
- 资源缩小到游戏内实际显示尺寸后仍能识别主体轮廓。
- 不包含烘焙的正式 UI 文本、规则文本、本地化文本或数值。
- 可被 Godot 表现层分层组合；不依赖不可裁切的整张图来表达规则状态。

归档要求：

- 临时生成图应先放入临时工作区或明确的候选目录；通过风格自检后，再复制或迁移到 `game/assets/art/` 的正式分类目录。
- 进入正式目录的资源需要获得稳定 asset ID，并写入 `game/data/presentation/` 的 asset manifest 或对应 view 文件。
- 资源命名应使用小写英文、数字和下划线，体现用途和内容，例如 `enemy.tower_scout.stand` 对应 `enemy_tower_scout_stand.png`。
- 重要风格参考、生成提示词或人工修图说明应保留在资源备注、制作记录或相邻说明文档中，方便后续复现和批量重做。

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
- `costs`：当前可为空或仅作为兼容字段，三拍切片主要由拍位限制。
- `requirements`
- `targeting`
- `actions`：行动牌内部动作串，包含动作类型、数值、段数和成功产彩条件。
- `effects`
- `energy`：当前记录无色彩能生成、消耗或保留规则；颜色继承留到后续五色阶段。
- `after_play`
- `tags`
- `balance`
- `vfx_asset`

行动牌必须显式声明动作串、目标规则和成功产彩条件。终结牌必须显式声明彩能需求、消耗规则和释放目标。五色阶段再要求声明附魔颜色继承和色彩结算方式。不再使用 `default_chain_delta`、`min_chain`、固定 3 / 5 / 8 阈值或 `skill` 卡牌类型。

表现数据位于 `game/data/presentation/card_views.json`，通过同一个 `id` 关联卡牌名称、规则文本、风味文本、卡牌模板 asset 和卡面 asset。

### 附魔与 Run 状态（后续五色阶段）

五色阶段运行时需要记录行动牌实例或卡牌 ID 的附魔状态：

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

### 魔物和 Boss

规则数据位于 `game/data/gameplay/enemies/enemies.json`。必备字段：

- `id`
- `rank`：普通、精英或 Boss。
- `world_theme`
- `stats`
- `ai`
- `immunities`
- `tags`

魔物意图需要同时定义 `preview`、拍位动作序列和可结算 `actions` / `effects`。表现数据位于 `game/data/presentation/enemy_views.json`。

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

普通战斗奖励当前应能表达：

- 武器卡牌三选一。

后续五色阶段再恢复随机色彩碎片。精英奖励额外表达普通遗物。Boss 奖励表达 Boss 遗物、Boss 特有卡牌或后续色彩核心。

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
- 引用的武器、卡牌、色彩、魔物、遗物、状态、文本键和 asset ID 存在。
- 每个 MVP 起始武器拥有固定 6 张初始卡牌，且必须是 4 张行动牌 + 2 张终结牌。
- 开局主副武器选择后，系统自动将主手 6 张全部加入、将副手 4 张行动牌加入，合计 10 张；当前 MVP 不再校验玩家起始牌自由选择。
- 卡牌类型只能是行动牌或终结牌。
- 行动牌必须声明动作串、拍位占用、目标规则和成功产彩条件。
- 终结牌必须声明彩能需求和消耗规则。
- 不得出现旧 `skill` 卡牌类型、`default_chain_delta`、`min_chain` 或 3 / 5 / 8 阈值字段。
- 后续五色阶段中，色彩碎片必须引用 5 种合法色彩之一。
- 普通战斗奖励当前必须包含 3 张武器卡牌候选；后续五色阶段再要求包含色彩碎片。
- 精英奖励必须包含普通遗物。
- Boss 奖励必须包含简化 Boss 奖励配置。
- 魔物意图必须同时具备 UI `preview` 和可结算 `effects`。
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
- 设置初始生命、玩家拍位长度、魔物拍位序列和彩能。
- 后续五色阶段支持指定色彩碎片和附魔状态。
- 跳过动画或加速动画。

### 奖励预览

提供奖励调试界面，支持查看武器卡牌三选一候选、精英遗物和 Boss 简化奖励；后续五色阶段再支持色彩碎片随机。

### 结构化战斗日志

战斗日志至少记录：

- 轮数。
- 拍位放置顺序。
- 卡牌 ID。
- 目标 ID。
- 玩家拍位 ID、魔物拍位 ID、空门目标和目标锁定状态。
- 动作对撞结果。
- 彩能生成、消耗和清空。
- 后续五色阶段记录颜色变化和色彩附加效果。
- 伤害、格挡、闪避、治疗、抽牌和状态变化。
- 魔物意图、魔物拍位动作和魔物行动。
- 终结牌消耗彩能数量。
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
