## 目的
策划一款Roguelike卡牌独立游戏，此仓库作为游戏知识库，用于维护游戏相关的设计文档和资料，帮助开发者随时记录想法，并将碎片化资料有条理地整理到此知识库中。

## 目录结构
本知识库使用Obsidian知识库管理：
- inspiration/：用于开发者随手记录玩法灵感、或记录每次用户与Codex的问答交互
- insight/：同类游戏设计与评价洞察
- design/：按照业界标准的游戏设计维度划分子目录，将灵感与洞察整理到对应文档中
- AGENTS.md（本文件）：知识库根目录下的工作规则，

## Obsidian兼容规则
本仓库不强制依赖开发者安装Obsidian；Codex可以直接在Markdown知识库内闭环整理、维护和查询文档。但所有由开发者或Codex落地的文档，必须保持Obsidian知识库友好：
- 使用Markdown文件作为知识载体；
- 需要关联其他文档时，优先使用Obsidian双链语法，如 `[[design/01_core_gameplay/03_card_system|卡牌系统]]`；
- 文件路径、文档标题、交叉链接应保持稳定，避免随意重命名导致双链失效；
- 新增文档应放入符合语义的目录；若现有目录无法覆盖，再创建新文档或新子目录；
- 文档内容应方便在Obsidian中通过反向链接、图谱、标签和搜索进行管理。

## 整理原则
当inspiration/有文件新建或修改时：
1. 提炼开发者灵感核心想法、观点；
2. 匹配design/中相关文档，如有冲突或模糊观点可以追问开发者至明确；
3. 增加对应的交叉链接，方便Obsidian管理；
4. 如果无对应文档，则创建新文档

当insight/有文件新建或修改时：
1. 核对资讯来源，分析可靠性，提炼与本项目有关的关键内容；
2. 匹配design/中相关文档，分析可参考点、可改进点；
3. 增加对应的交叉链接，方便Obsidian管理；
4. 如果无对应文档，则创建新文档

## 查询原则
游戏开发请优先遵循design/中的所有设计与规范，仅必要时查看inspiration/、insight/

## 第一版 MVP 开发提示词库

本节用于开发者将第一版 MVP 的实现任务逐步交给 Codex。每次只投喂一个任务 Prompt；当前任务未稳定通过验证前，不进入下一任务。

### 通用 Prompt 头

以下内容建议复制到每次 MVP 开发任务 Prompt 的开头：

```text
你在 D:\Codex Workspace\RoguelikeCardGame 工作。本仓库是 Obsidian 友好的 Roguelike 卡牌游戏知识库，并将基于 Godot 4.6.x .NET + C# 开发第一版 MVP。

请先读取并遵循：
- AGENTS.md
- README.md
- design/00_product/03_scope_and_success_criteria.md
- design/01_core_gameplay/00_core_loop.md
- design/01_core_gameplay/01_run_structure.md
- design/01_core_gameplay/02_combat_system.md
- design/01_core_gameplay/03_card_system.md
- design/02_content_systems/02_enemies_and_bosses.md
- design/02_content_systems/03_relics_items_rewards.md
- design/03_experience/00_ui_ux.md
- design/06_technical_production/00_technical_requirements.md
- design/06_technical_production/01_data_pipeline_and_tools.md
- design/08_governance/2026-06-03_godot_csharp_codex_technical_stack.md
- design/08_governance/2026-06-04_keep_godot_project_in_same_repo_game_subdir.md

硬约束：
- Godot 工程根目录是 game/，即 game/project.godot。
- 游戏实现文件放在 game/ 下；知识库文档仍放在 design/、inspiration/、insight/。
- 玩法规则层与 Godot 表现层分离。
- C# 规则逻辑不要依赖 Godot 节点树。
- 卡牌、敌人、遗物、遭遇、奖励包优先数据驱动，不要把内容数值写死在场景里。
- 首版只做第一版 MVP，不做路线图、商店、事件、休息节点、长期成长、联机、Web 导出或完整内容池。
- 新增或修改代码、数据时，同步补充可运行校验、测试或最小验证方式。
- 如果改动影响设计理解、制作范围或长期维护成本，同步更新对应 design/ 文档、[[design/08_governance/01_change_log|变更日志]] 或 [[design/08_governance/00_decision_log|决策记录]]。
- 完成后说明修改了哪些文件、运行了哪些验证、哪些验证未能运行。
```

### 任务 1：初始化 Godot + C# 工程骨架

```text
任务：初始化第一版 Godot 4.6.x .NET + C# 工程骨架。

请检查当前仓库是否已有 game/project.godot；如果没有，在 game/ 下创建 Godot C# 项目骨架，并建立技术文档中定义的目录：
game/assets/
game/data/
game/scenes/
game/src/
game/tests/
game/tools/
game/logs/

同时：
- 更新 .gitignore，忽略 Godot 本地导入缓存、构建输出、C# 临时输出、日志等不应提交内容。
- 创建基础 C# 命名空间 RoguelikeCardGame。
- 创建最小主场景或占位入口场景。
- 不实现玩法，只保证项目结构正确、Godot 可打开、C# 可编译。
- 完成后运行可用的 Godot / C# 编译或项目检查命令。
```

#### 任务 1 完成记录

完成日期：2026-06-04

已完成：
- 已在 `game/` 下初始化 Godot 4.6.3 .NET / C# 工程骨架，入口为 `game/project.godot`。
- 已创建 `game/RoguelikeCardGame.csproj`，基础命名空间为 `RoguelikeCardGame`。
- 已创建最小入口场景 `game/scenes/main/Main.tscn` 与占位脚本 `game/src/Presentation/Menus/MainMenu.cs`。
- 已按技术文档建立 `game/addons/`、`game/assets/`、`game/data/`、`game/scenes/`、`game/src/`、`game/tests/`、`game/tools/`、`game/logs/` 等目录，并用 `.gitkeep` 保持空目录可提交。
- 已更新 `.gitignore`，忽略 Godot 导入缓存、C# 构建输出、NuGet / dotnet 本地缓存、导出目录和本地日志；同时收窄 `data_*/` 忽略规则，避免误伤 `game/tools/data_validator/`。
- 已新增 `game/tools/check_project.ps1`，用于检查 Godot .NET 版、.NET 8 SDK，并触发项目构建验证。
- 本任务未实现玩法规则、内容数据或 UI 流程，仅保证工程结构、入口场景和 C# 骨架可用。

验证结果：
- `D:\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe --version`：确认 Godot 版本为 `4.6.3.stable.mono`。
- `dotnet --list-sdks`：确认可用 .NET SDK 包含 `8.0.421`。
- `dotnet build game\RoguelikeCardGame.csproj`：通过，0 个警告、0 个错误。
- Godot headless 启动 `res://scenes/main/Main.tscn`：通过，输出 `RoguelikeCardGame MVP shell ready.`，确认入口场景和 C# 脚本可实例化。
- `game/tools/check_project.ps1`：可运行并返回成功；当前 Codex 沙箱中 Godot 可能额外输出 root certificate / safe save 相关环境警告，但 `.NET project` 构建完成且程序集已生成。

### 任务 2：建立数据 Schema 与校验工具

```text
任务：建立 MVP 数据管线第一版。

请根据 design/06_technical_production/01_data_pipeline_and_tools.md，在 game/data/ 和 game/tools/ 下创建 JSON Schema、示例数据与校验工具，至少覆盖：
- cards
- relics
- enemies
- encounters
- rewards
- run sequence
- localization

校验工具至少检查：
- 内容 ID 唯一。
- 引用的卡牌、敌人、遗物、奖励包、文本键存在。
- 奖励包候选数量为 3。
- 终结牌必须有 min_chain。
- 行动牌、技能牌、终结牌默认连锁规则符合设计。
- MVP 前两次普通战斗后的终结牌包包含群体攻击终结牌。

先用占位数据即可，但字段要稳定。完成后运行数据校验，并修复所有校验错误。
```

### 任务 3：实现规则层基础模型

```text
任务：实现不依赖 Godot 节点树的 C# 规则层基础模型。

请在 game/src/Domain 和 game/src/Application 中实现：
- CardDefinition
- RelicDefinition
- EnemyDefinition
- EncounterDefinition
- RewardPackDefinition
- RunState
- CombatState
- DeckZones：draw pile、hand、discard pile
- CombatLogEvent 或等价结构化日志事件

要求：
- 规则层普通 C# 类不依赖 Godot 节点树。
- 命名空间按目录组织，例如 RoguelikeCardGame.Domain.Combat。
- 暂不做 UI。
- 补充单元测试或最小测试，验证模型可创建、序列化或被服务层使用。
```

### 任务 4：实现回合流程与抽弃牌循环

```text
任务：实现 MVP 战斗回合流程。

请实现：
- 战斗开始。
- 回合开始：清空上一轮敌人行动后剩余防御、抽 5 张、恢复行动点。
- 抽牌堆不足时，将弃牌堆洗回抽牌堆继续抽。
- 玩家回合结束：清空未用行动点、清空连锁层数、弃掉未使用手牌。
- 敌人行动占位。
- 新回合准备。

请写测试覆盖：
- 抽牌。
- 弃牌。
- 重洗。
- 防御清空时点。
- 行动点恢复。
- 回合结束清空规则。

完成后运行测试。
```

### 任务 5：实现卡牌使用与基础效果系统

```text
任务：实现 MVP 卡牌使用规则和基础效果模板。

请支持：
- 行动牌消耗行动点，默认使用后 +1 连锁层数。
- 技能牌默认不消耗行动点，默认不增加连锁。
- 终结牌默认不消耗行动点，但需要满足 min_chain，使用后清空连锁。
- 目标规则：单体敌人、全部敌人、自己。
- 基础效果：伤害、防御、抽牌、获得当前回合行动点、临时减费或减费占位。

要求：
- 结算结果输出结构化日志事件。
- UI 需要的信息应能从规则层结果读取，例如费用不足、目标缺失、连锁不足。
- 不要把具体卡牌数值写死在表现层。

请写测试覆盖：
- 可打出。
- 费用不足。
- 连锁不足。
- 目标缺失。
- 行动牌 +1 连锁。
- 技能牌默认不加连锁。
- 终结牌清空连锁。
```

### 任务 6：实现敌人意图与胜负结算

```text
任务：实现固定敌人意图序列和基础胜负结算。

请根据 MVP 固定遭遇需求实现：
- 敌人生命。
- 敌人意图显示数据。
- 固定 intent_sequence。
- 敌人攻击结算，玩家防御抵挡伤害。
- 敌人死亡。
- 玩家生命归零导致 MVP Run 失败。
- 全部敌人死亡导致战斗胜利。

请写测试覆盖：
- 敌人攻击。
- 防御抵挡。
- 敌人死亡。
- 战斗胜利。
- Run 失败。
```

### 任务 7：落地 MVP 内容数据

```text
任务：落地第一版 MVP 占位内容数据。

请在 game/data/ 下创建或补全：
- 初始牌组 10 张：6 张行动牌、3 张技能牌、1 张终结牌。
- 奖励牌 9 张：行动牌 3 张、技能牌 3 张、终结牌 3 张。
- 普通遗物 1 个。
- 固定遭遇 6 场：普通战斗 1、普通战斗 2、普通战斗 3、精英战斗、普通战斗 4、Boss 战斗。
- 三类固定可重复奖励包：行动牌包、技能牌包、终结牌包。
- MVP 线性 Run 序列。
- 简体中文本地化文本键。

内容职责：
- 普通战斗 1 教行动点 / 出牌 / 连锁。
- 普通战斗 2 教敌人意图与防御。
- 普通战斗 3 教多敌人与目标选择。
- 精英战斗检验攻防节奏。
- 普通战斗 4 试用遗物和新卡。
- Boss 战斗综合检验。

数值可以保守占位，但必须能跑完闭环。完成后运行数据校验，并修复所有校验错误。
```

### 任务 8：实现奖励包与 Run 推进

```text
任务：实现 MVP 战后奖励与线性 Run 推进。

请支持：
- 普通战斗胜利后选择行动牌包、技能牌包、终结牌包之一。
- 打开后显示 3 张同类型候选牌。
- 玩家可选择 0-3 张加入卡组。
- 同名牌允许重复加入。
- 精英战斗后额外获得固定普通遗物。
- Boss 击败后进入 MVP 通关状态。
- 每场战斗开始时玩家生命回满。
- 生命归零立即结束本次 MVP Run，不提供当前战斗重试。

请写测试覆盖：
- 奖励包选择。
- 跳过拿牌。
- 选择多张。
- 重复加入同名牌。
- 精英遗物获得。
- Boss 通关。
- 失败结算。
```

### 任务 9：接入 Godot 战斗界面最小可玩版

```text
任务：把规则层接入 Godot 最小战斗界面。

请在 game/scenes/ 和 game/src/Presentation/ 下创建可运行的 MVP 战斗界面，先用占位 UI，不做复杂美术。界面至少显示：
- 玩家生命、防御、行动点。
- 当前连锁层数与 3 / 5 / 8 阈值。
- 手牌。
- 敌人生命、意图。
- 抽牌堆、弃牌堆数量。
- 结束回合按钮。
- 简单战斗日志。

交互至少支持：
- 点击卡牌。
- 选择目标。
- 出牌。
- 结束回合。
- 进入下一回合。

表现层只调用规则层，不自行结算伤害、抽牌、连锁、费用或防御。完成后运行 Godot 项目检查或可用的启动验证。
```

### 任务 10：接入奖励、结算与重开流程

```text
任务：实现 MVP 的界面流程闭环。

请接入：
主菜单 / 开始 MVP -> 战斗 -> 奖励选择 -> 下一场战斗 -> 精英遗物 -> Boss -> 通关结算 / 失败结算 -> 重开。

奖励界面要符合设计：
- 先选卡牌包类型。
- 再显示 3 张候选。
- 可选择 0-3 张加入卡组。
- 确认后进入下一场战斗。

结算界面要支持：
- Boss 击败显示 MVP 通关。
- 玩家生命归零显示失败。
- 通关和失败都提供重开入口。

先保证流程稳定，不做路线图、商店、事件或休息节点。
```

### 任务 11：实现调试入口、日志与试玩指标导出

```text
任务：实现 MVP 开发调试工具。

请添加调试入口，支持：
- 直接进入任意固定遭遇。
- 指定随机种子。
- 指定初始牌组或添加指定卡牌。
- 指定奖励包预览。
- 导出结构化战斗日志。
- 导出 MVP 试玩指标。

试玩指标至少包含：
- Run 种子。
- 节点顺序。
- 每场战斗回合数、受伤、胜负。
- 最高连锁层数。
- 达到 3 / 5 / 8 层次数。
- 终结牌使用次数和触发额外效果次数。
- 卡牌包类型选择、实际拿牌数量和卡牌 ID。
- 遗物获得节点。
- MVP 通关或失败节点。
- 总时长。

日志与指标输出应放在 game/logs/ 或明确的开发期输出目录，并避免提交个人日志。
```

### 任务 12：MVP 体验打磨与验收

```text
任务：进行第一版 MVP 验收与打磨。

请运行现有测试、数据校验和 Godot 项目检查。然后从设计文档出发检查：
- 是否能完成 6 场固定战斗。
- 是否能在 10-15 分钟内跑完。
- 行动点、连锁、终结牌、防御、抽弃牌循环是否符合设计。
- 奖励包是否形成构筑调整。
- 精英遗物是否能在普通战斗 4 和 Boss 战中体现收益。
- Boss 击败是否通关。
- 失败是否可重开。
- UI 是否清楚表达连锁层数、敌人意图、无法出牌原因。
- 结构化日志和试玩指标是否足够复盘。

请列出发现的问题，优先修复阻碍 MVP 闭环的问题；数值平衡只做必要调整，不扩展首版范围外系统。
```
