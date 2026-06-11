## 目的
策划一款Roguelike卡牌独立游戏，此仓库作为游戏知识库，用于维护游戏相关的设计文档和资料，帮助开发者随时记录想法，并将碎片化资料有条理地整理到此知识库中。

## 目录结构
本知识库使用Obsidian知识库管理：
- inspiration/：用于开发者随手记录玩法灵感、或记录每次用户与Codex的问答交互
- insight/：同类游戏设计与评价洞察
- design/：按照业界标准的游戏设计维度划分子目录，将灵感与洞察整理到对应文档中
- game/：Godot游戏项目目录
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

#### 任务 2 完成记录

完成日期：2026-06-04

已完成：
- 已在 `game/data/schemas/` 下创建首版 JSON Schema，覆盖 `cards`、`relics`、`enemies`、`encounters`、`rewards`、`run_sequence`、`localization`。
- 已在 `game/data/` 下创建 MVP 占位数据：`cards/cards.json`、`relics/relics.json`、`enemies/enemies.json`、`encounters/encounters.json`、`rewards/reward_packs.json`、`run_sequence/mvp_run.json`、`localization/zh_hans.json`。
- 已创建 10 张初始牌组计数配置、3 类固定可重复奖励包、1 个普通遗物、6 场固定遭遇、MVP 线性 Run 序列和简体中文本地化文本键。
- 已确保前两次普通战斗后的终结牌包包含群体攻击终结牌 `card_arc_sweep_finish`。
- 已新增无第三方依赖的数据校验器 `game/tools/data_validator/validate_data.py`。
- 已新增 Windows / Codex 友好的包装入口 `game/tools/data_validator/validate_data.ps1`。
- 本任务未实现运行时加载、规则层模型或 Godot UI，仅建立首版数据格式、占位数据与可运行校验流程。

校验工具已覆盖：
- 内容 ID 唯一。
- 引用的卡牌、敌人、遗物、奖励包、遭遇、Run 序列和文本键存在。
- 奖励包候选数量必须为 3。
- 终结牌必须定义正数 `min_chain`。
- 行动牌默认 `default_chain_delta = 1`，技能牌默认 `default_chain_delta = 0`，终结牌默认 `default_chain_delta = "consume_all"`。
- 奖励包候选卡牌类型必须匹配包类型。
- 初始牌组计数必须为 10。
- MVP Run 序列必须包含 6 场遭遇，Boss 通关目标必须引用 Boss 遭遇。
- MVP 前两次普通战斗后的终结牌包必须包含群体攻击终结牌。

验证结果：
- `.\game\tools\data_validator\validate_data.ps1`：通过。
- 输出结果：`Data validation passed. Validated 7 data files and 7 schemas.`

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

#### 任务 3 完成记录

完成日期：2026-06-04

已完成：
- 已在 `game/src/Domain/Cards/` 实现 `CardDefinition`，包含卡牌类型、稀有度、目标规则、默认连锁变化和效果列表。
- 已在 `game/src/Domain/Relics/` 实现 `RelicDefinition`，包含稀有度、触发时机、条件、效果和堆叠规则。
- 已在 `game/src/Domain/Enemies/` 实现 `EnemyDefinition` 与 `EnemyIntentDefinition`，用于表达敌人生命、意图序列和 UI 文本键。
- 已在 `game/src/Domain/Combat/` 实现 `EncounterDefinition`、`CombatState`、`DeckZones`、`CombatLogEvent` 等基础战斗模型。
- 已在 `game/src/Domain/Rewards/` 实现 `RewardPackDefinition`。
- 已在 `game/src/Domain/Runs/` 实现 `RunState`。
- 已在 `game/src/Application/Runs/` 新增 `RunStateFactory`，用于创建最小 Run 状态。
- 已在 `game/src/Application/Battle/` 新增 `CombatStateFactory`，用于根据 Run、遭遇和敌人定义创建最小战斗状态。
- 已新增 `game/tests/Unit/RoguelikeCardGame.Tests.csproj` 与 `game/tests/Unit/Program.cs`，作为无测试框架依赖的规则层 smoke test。
- 已更新 `game/RoguelikeCardGame.csproj`，启用 implicit usings，并排除 `tests/**/*.cs`，避免测试入口被 Godot 主工程编译。
- 本任务未实现回合流程、出牌结算、敌人行动结算、奖励选择或 UI，仅建立可被后续服务层使用的基础模型。

验证结果：
- `dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `dotnet build game\RoguelikeCardGame.csproj`：通过，0 个警告、0 个错误。
- `.\game\tools\data_validator\validate_data.ps1`：通过，输出 `Data validation passed. Validated 7 data files and 7 schemas.`。
- 规则层 smoke test 只编译 `game/src/Domain/` 与 `game/src/Application/`，验证这些基础模型可在普通 .NET console 项目中创建、序列化并被服务层使用，不依赖 Godot 节点树。

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

#### 任务 4 完成记录

完成日期：2026-06-04

已完成：
- 已新增 `game/src/Application/Battle/CombatTurnService.cs`，实现战斗开始、回合开始、抽牌、弃牌、弃牌堆重洗、玩家回合结束、敌人行动占位和新回合准备。
- 已更新 `game/src/Domain/Combat/CombatState.cs`，增加 `base_action_points` 与 `cards_per_turn`，用于规则层恢复行动点和每回合抽牌数量。
- 已更新 `game/src/Domain/Combat/CombatLogEvent.cs`，新增 `CardsDrawn`、`CardsDiscarded`、`DeckReshuffled`、`PlayerTurnEnded`、`EnemyTurnPlaceholderResolved`、`NewTurnPrepared` 等日志事件类型。
- 已调整 `game/src/Application/Battle/CombatStateFactory.cs`，让工厂创建 `NotStarted` 战斗状态；正式进入第一回合由 `CombatTurnService.StartCombat` 负责。
- 已扩展 `game/tests/Unit/Program.cs` smoke tests，覆盖战斗开始、抽牌、弃牌、重洗、防御清空时点、行动点恢复和回合结束清空规则。
- 本任务未实现卡牌使用、伤害结算、敌人攻击、防御抵挡或胜负结算；敌人行动仍是占位，只推进意图索引并写入日志。

验证结果：
- `dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `dotnet build game\RoguelikeCardGame.csproj`：通过，0 个警告、0 个错误。
- `.\game\tools\data_validator\validate_data.ps1`：通过，输出 `Data validation passed. Validated 7 data files and 7 schemas.`。

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

#### 任务 5 完成记录

完成日期：2026-06-04

已完成：
- 已新增 `game/src/Application/Battle/CardPlayService.cs`，实现规则层卡牌使用入口 `CanPlayCard` / `PlayCard` 与 `PlayCardResult`。
- 已实现 UI 可读取的失败原因 `PlayCardFailureReason`，覆盖非玩家回合、手牌不存在、行动点不足、连锁不足和目标缺失，并返回所需行动点、当前行动点、所需连锁、当前连锁、目标规则和本地化消息键。
- 已支持行动牌消耗行动点并默认 `+1` 连锁，技能牌默认不消耗行动点且不增加连锁，终结牌默认不消耗行动点、要求 `min_chain` 且使用后清空连锁。
- 已支持单体敌人、全部敌人、自己目标规则。
- 已支持基础效果模板：伤害、防御 / 获得防御、抽牌、获得当前回合行动点、临时减费占位；同时保留 `chain_threshold_bonus` 结算能力以兼容现有终结牌数据。
- 已让成功出牌返回 `CardPlayed`、`EffectResolved`、抽牌相关事件等结构化日志；失败出牌返回 `CardPlayRejected` 结构化事件。
- 已扩展 `game/src/Domain/Combat/CombatLogEvent.cs`，新增 `CardPlayRejected` 事件类型。
- 已扩展 `game/tests/Unit/Program.cs`，覆盖可打出、费用不足、连锁不足、目标缺失、行动牌默认加连锁、技能牌默认不加连锁、终结牌清空连锁，并额外覆盖群体目标、抽牌、获得行动点和临时减费占位。
- 本任务未接入 Godot UI、未实现敌人死亡 / 胜负结算，也未把卡牌数值写入表现层；这些留给后续任务。

验证结果：
- `dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `dotnet build game\RoguelikeCardGame.csproj`：通过，0 个警告、0 个错误。
- `.\game\tools\data_validator\validate_data.ps1`：通过，输出 `Data validation passed. Validated 7 data files and 7 schemas.`。
- 当前 Codex 沙箱中直接运行 `dotnet` 可能因写入 `obj` / `.godot\mono\temp\obj` 缓存受限而失败；已按权限规则重跑并验证通过。

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

#### 任务 6 完成记录

完成日期：2026-06-04

已完成：
- 已在 `game/src/Application/Battle/CombatTurnService.cs` 中新增真实敌人回合结算 `ResolveEnemyTurn`，按固定 `intent_sequence` 和 `IntentIndex` 逐个解析活着敌人的当前意图。
- 已新增敌人意图显示数据 `game/src/Domain/Combat/EnemyIntentView.cs`，提供敌人实例、敌人 ID、当前意图 ID、意图类型、UI 文本键和效果预览，供后续 Godot 表现层读取。
- 已实现敌人攻击结算：玩家防御先抵挡伤害，剩余伤害扣玩家生命，并输出 `EnemyIntentResolved` 结构化日志。
- 已实现敌人防御意图的基础结算：`gain_block` / `block` 作用于敌人自身，便于 MVP 精英和 Boss 的固定意图使用。
- 已在 `game/src/Application/Battle/CardPlayService.cs` 中补充战斗结果检查：卡牌伤害可使敌人生命降至 0，记录 `EnemyDied`；全部敌人死亡时进入 `CombatStatus.Victory` 并记录 `CombatEnded`。
- 已新增 `game/src/Application/Runs/RunProgressService.cs`，将 `CombatStatus.Defeat` 映射为 `RunStatus.Failed`，并将玩家生命记录为 0。
- 已扩展 `game/src/Domain/Combat/CombatLogEvent.cs`，新增 `EnemyDied` 事件类型。
- 已扩展 `game/tests/Unit/Program.cs`，覆盖敌人攻击、防御抵挡、固定意图序列推进与显示、敌人防御意图、敌人死亡、战斗胜利和 Run 失败。
- 本任务未实现状态系统、Boss 阶段变化、奖励推进、通关 Run 结算或 Godot UI；这些留给后续任务。

验证结果：
- `dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `dotnet build game\RoguelikeCardGame.csproj`：通过，0 个警告、0 个错误。
- `.\game\tools\data_validator\validate_data.ps1`：通过，输出 `Data validation passed. Validated 7 data files and 7 schemas.`。
- 当前 Codex 沙箱中直接运行 `dotnet` 仍可能因写入 `obj` / `.godot\mono\temp\obj` 缓存受限而失败；已按权限规则重跑并验证通过。

### 任务 7：录入并校验第一版 MVP 可玩内容

```text
任务：录入并校验第一版 MVP 可玩内容。

本任务的重点是“填写可玩的 MVP 内容”，不是重新设计数据格式、Schema 或校验器。请沿用任务 2 已建立的 JSON Schema、数据目录结构和数据校验工具；只有当现有格式无法表达必要 MVP 内容时，才做最小字段扩展，并同步更新 Schema、校验工具和相关测试。

请在 game/data/ 下创建或补全第一版 MVP 的真实可玩内容：
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

内容要求：
- 每张卡牌、每个敌人、每个遭遇、每个奖励包和遗物都必须有稳定内容 ID。
- 卡牌效果和敌人意图必须能被当前规则层或已明确的 MVP 占位效果结算。
- 前两次普通战斗后的终结牌包必须包含群体攻击终结牌。
- 奖励池为固定可重复池，允许重复加入同名牌。
- 数值可以采用保守原型数值，但必须服务 6 场战斗闭环，不再只是 Schema 示例数据。
- 不新增路线图、商店、事件、休息节点、长期成长或完整内容池。

完成后：
- 运行任务 2 建立的数据校验命令，并修复所有校验错误。
- 运行已有规则层 / 内容加载相关测试。
- 简要说明这些内容如何支撑 6 场 MVP 战斗，以及哪些数值仍需后续试玩调优。
```

#### 任务 7 完成记录

完成日期：2026-06-04

已完成：
- 已沿用任务 2 的数据目录、JSON Schema 和校验工具，未新增路线图、商店、事件、休息节点、长期成长或完整内容池。
- 已确认并保留 `game/data/cards/cards.json` 中第一版 MVP 可玩卡牌内容：初始牌组通过 Run 配置形成 10 张牌，构成为 6 张行动牌、3 张技能牌、1 张终结牌；奖励牌池为 3 张行动牌、3 张技能牌、3 张终结牌。
- 已确认并保留 `game/data/rewards/reward_packs.json` 中三类固定可重复奖励包，每包固定 3 张同类型候选牌；终结牌包包含群体攻击终结牌 `card_arc_sweep_finish`，可支撑前两次普通战斗后的多敌人教学准备。
- 已收紧 `game/data/enemies/enemies.json` 中敌人意图和保守原型数值：移除当前规则层未结算的 `gain_status` 占位意图，改为已实现的攻击 / 防御意图；略降低多敌人、精英和 Boss 数值，服务 6 场 MVP 闭环。
- 已同步更新 `game/data/localization/zh_hans.json` 中敌人意图数值与文本键，确保简体中文本地化与数据一致。
- 已保留 `game/data/relics/relics.json` 中普通遗物 `relic_mvp_chain_spark`，用于精英战后奖励与普通战斗 4 / Boss 战的后续收益验证。
- 已保留 `game/data/encounters/encounters.json` 中 6 场固定遭遇和 `game/data/run_sequence/mvp_run.json` 中 MVP 线性 Run 序列。
- 已扩展 `game/tools/data_validator/validate_data.py`，新增 MVP 卡牌效果和敌人意图效果白名单校验，确保卡牌效果、敌人攻击 / 防御意图都能被当前规则层或明确 MVP 占位结算。

6 场 MVP 内容支撑：
- 普通战斗 1：`enemy_training_dummy` 低伤害单体攻击，用于教学行动点、出牌和连锁积累。
- 普通战斗 2：`enemy_intent_scout` 在攻击和防御之间循环，用于教学敌人意图显示、防御时机和敌人防御。
- 普通战斗 3：双 `enemy_splitling`，用于教学多敌人目标选择，并让前两战可获得的群体终结牌发挥价值。
- 精英战斗：`enemy_elite_guardian` 高生命、攻击 / 防御循环，用于检验攻防节奏，并在胜利后提供固定普通遗物。
- 普通战斗 4：`enemy_relic_tester` 中等压力单体战，用于试用遗物和新增奖励牌。
- Boss 战斗：`enemy_chain_warden` 单体高生命 Boss，交替攻击与压迫意图，综合检验行动点规划、连锁、终结牌、防御和奖励构筑成果。

仍需后续试玩调优：
- 敌人生命和伤害是否让 6 场战斗稳定落在 10-15 分钟 MVP 时长内。
- 奖励牌是否足以明显改善普通战斗 3、精英和 Boss 的节奏。
- `card_setup_discount` 当前仍是规则层明确的临时减费占位效果，后续接入真实减费状态后需要回测强度。
- `relic_mvp_chain_spark` 的触发结算尚未在规则层落地，任务 8 / 后续流程接入遗物后需要验证收益是否足够可感知。

验证结果：
- `.\game\tools\data_validator\validate_data.ps1`：通过，输出 `Data validation passed. Validated 7 data files and 7 schemas.`。
- `dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `dotnet build game\RoguelikeCardGame.csproj`：通过，0 个警告、0 个错误。

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

#### 任务 8 完成记录

完成日期：2026-06-04

已完成：
- 已新增 `game/src/Application/Rewards/RewardService.cs`，实现战后奖励包流程：列出遭遇可选奖励包、打开指定奖励包、暴露固定 3 张候选牌、校验并领取 0-3 张卡牌。
- 已支持奖励牌加入 `RunState.MasterDeck`，同名卡牌可以重复加入，重复候选 ID 也按选择次数追加。
- 已支持精英遭遇通过 `EncounterRewardProfileDefinition.RelicId` 额外获得固定遗物，并避免同一固定遗物重复加入。
- 已扩展 `game/src/Application/Runs/RunProgressService.cs`：支持 `PrepareForCombat` 满血开战、`ApplyCombatResult` 处理失败 / Boss 通关、`AdvanceAfterRewards` 线性推进到下一场战斗。
- 已明确 `game/src/Application/Battle/CombatStateFactory.cs` 每场战斗以玩家最大生命开始，满足 MVP 每场战斗开始时生命回满。
- 已扩展 `game/tests/Unit/Program.cs`，覆盖奖励包选择、打开 3 候选、跳过拿牌、选择多张、重复加入同名牌、精英遗物获得、Boss 通关、失败结算、线性推进和满血开战。
- 本任务未接入 Godot UI 奖励界面、未实现遗物战斗触发效果，也未实现结算 / 重开流程；这些留给任务 9 / 10 及后续流程。

验证结果：
- `dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `dotnet build game\RoguelikeCardGame.csproj`：通过，0 个警告、0 个错误。
- `.\game\tools\data_validator\validate_data.ps1`：通过，输出 `Data validation passed. Validated 7 data files and 7 schemas.`。
- 当前 Codex 沙箱中直接运行 `dotnet` 仍可能因写入 `obj` / `.godot\mono\temp\obj` 缓存受限而失败；已按权限规则重跑并验证通过。

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

#### 任务 9 完成记录

完成日期：2026-06-05

已完成：
- 已新增 `game/src/Infrastructure/Content/GameContent.cs` 与 `game/src/Infrastructure/Content/RunSequenceDefinition.cs`，将 `game/data/` 中的卡牌、敌人、遭遇、奖励包、遗物、本地化和 MVP Run 序列加载为现有 `Domain` 模型。
- 已更新 `game/src/Presentation/Menus/MainMenu.cs`，把 `game/scenes/main/Main.tscn` 接入最小战斗界面。
- 战斗界面已显示玩家生命、防御、行动点、当前连锁层数与 3 / 5 / 8 阈值、抽牌堆 / 弃牌堆 / 卡组数量、敌人生命、敌人防御、敌人意图、手牌和最近结算日志。
- 战斗交互已支持选择敌人目标、点击手牌出牌、行动牌消耗行动点并增加连锁、技能牌结算防御 / 抽牌 / 回费、终结牌按连锁条件可用并清空连锁、结束回合、敌人行动和进入下一回合。
- 表现层只调用 `CombatStateFactory`、`CombatTurnService`、`CardPlayService`、`RunProgressService`、`RewardService` 等 Application 服务；伤害、抽牌、连锁、费用和防御仍由规则层结算。
- 已在战斗 UI 中通过按钮禁用和 tooltip 暴露无法出牌原因，包括行动点不足、连锁不足、目标缺失和非玩家回合等。
- 该界面仍是程序化占位 UI，尚未拆分正式 Battle / Card / Enemy 组件，也未接入手绘漫画书视觉资产和动画演出。

验证结果：
- `python3 game/tools/data_validator/validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- `dotnet build game/RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `godot-mono --headless --path game --quit`：通过，项目可加载。
- `dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `python3 game/tools/data_validator/validate_data.py`：通过，输出 `Data validation passed. Validated 7 data files and 7 schemas.`。
- `godot-mono --headless --path game --quit`：通过，项目可加载。
- `godot-mono --headless --path game`：运行超过 5 秒未立即崩溃，说明主场景启动链路可用。

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

#### 任务 10 完成记录

完成日期：2026-06-05

已完成：
- 已在 `game/src/Presentation/Menus/MainMenu.cs` 中接入 MVP 界面流程闭环：主菜单 / 开始 MVP -> 战斗 -> 奖励选择 -> 下一场战斗 -> 精英遗物 -> Boss -> 通关结算 / 失败结算 -> 重开。
- 普通 / 精英战斗胜利后会进入卡牌包选择界面；玩家先选择行动牌包、技能牌包或终结牌包，再看到 3 张同类型候选牌。
- 打开奖励包后，玩家可点击候选牌切换选择状态，并确认选择 0-3 张加入卡组；同名牌允许重复加入。
- 精英战斗胜利后，确认奖励时会通过 `RewardService.GrantEncounterRelic` 发放固定普通遗物 `relic_mvp_chain_spark`。
- 已在战斗流程中接入 `relic_mvp_chain_spark` 的 MVP 表现：每回合第一次使用行动牌时获得 2 点防御，并写入结构化日志。
- Boss 击败后进入 MVP 通关界面，显示最终卡组数量和遗物数量，并提供重新开始。
- 玩家生命归零后进入失败界面，并提供重新开始。
- 本轮仍未实现路线图、商店、事件、休息节点、长期成长或正式存档。

验证结果：
- `python3 game/tools/data_validator/validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- `dotnet build game/RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `godot-mono --headless --path game --quit`：通过，Godot 版本为 `4.6.3.stable.mono`，项目可启动。
- `dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `python3 game/tools/data_validator/validate_data.py`：通过，输出 `Data validation passed. Validated 7 data files and 7 schemas.`。
- `godot-mono --headless --path game --quit`：通过，项目可加载。
- 已通过 Godot 4.6.3 .NET 版打开 `game/project.godot`，确认 Godot GUI 可加载项目。

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

#### 任务 11 完成记录

完成日期：2026-06-05

已完成：
- 已新增 `game/src/Application/Debug/DebugRunService.cs`，支持直接进入指定固定遭遇、指定随机种子、覆盖初始牌组、追加指定卡牌、预览指定奖励包，并创建调试用 `RunState` 与 `CombatState`。
- 已新增 `game/src/Application/Debug/PlaytestMetricsService.cs`，可从结构化 `CombatState.Log` 汇总 MVP 试玩指标：Run 种子、节点顺序、每场战斗回合数、受伤、胜负、最高连锁、3 / 5 / 8 阈值达到次数、终结牌使用次数、终结牌额外效果触发次数、奖励选择、遗物获得、通关 / 失败节点和总时长。
- 已新增 `game/src/Application/Debug/DebugExportService.cs`，可导出结构化战斗日志 JSON 和试玩指标 JSON。
- 已新增开发期命令行入口 `game/tools/debug_mvp/debug_mvp.py` 与 Windows 包装脚本 `game/tools/debug_mvp/debug_mvp.ps1`，可读取 `game/data/` 并在 `game/logs/` 生成调试会话、战斗日志骨架和指标骨架。
- 已扩展 `game/tests/Unit/Program.cs`，覆盖调试遭遇入口、seed、牌组覆盖、追加卡牌、奖励包预览、指标汇总和 JSON 导出。
- 已确认 `.gitignore` 已忽略 `game/logs/*` 且保留 `game/logs/.gitkeep`，开发期个人日志不会被提交。
- 当前命令行入口用于 UI 未接入前的调试会话与导出骨架；真实交互战斗接入后，应由 Godot 表现层把实际 `CombatState.Log` 和 Run 过程记录传给 `DebugExportService` / `PlaytestMetricsService`。

验证结果：
- `dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `dotnet build game\RoguelikeCardGame.csproj`：通过，0 个警告、0 个错误。
- `.\game\tools\data_validator\validate_data.ps1`：通过，输出 `Data validation passed. Validated 7 data files and 7 schemas.`。
- `.\game\tools\debug_mvp\debug_mvp.ps1 -EncounterId encounter.mvp.normal_03 -Seed 424242 -AddCard card.arc_sweep_finish -RewardPackId reward_pack.mvp.finisher`：通过，并在 `game/logs/` 下生成调试会话、战斗日志骨架和试玩指标骨架。
- 当前 Codex 沙箱中直接运行 `dotnet` 仍可能因写入 `obj` / `.godot\mono\temp\obj` 缓存受限而失败；已按权限规则重跑并验证通过。

### 2026-06-08 数据结构重构完成记录

已完成：
- 已将第一版 MVP 内容数据迁移为分层结构：`game/data/gameplay/` 保存可结算规则数据，`game/data/presentation/` 保存 view 与 asset manifest，`game/data/localization/` 保存文本。
- 已将内容 ID 统一为命名空间格式，例如 `card.basic_strike`、`enemy.chain_warden`、`relic.mvp_chain_spark`、`reward_pack.mvp.finisher`、`encounter.mvp.normal_03`。
- 已将卡牌 `cost`、`min_chain`、`default_chain_delta`、`target_rule` 等旧扁平字段迁移为 `costs`、`requirements`、`targeting` 与显式 `effects` DSL；行动牌加连锁、终结牌清空连锁和阈值奖励都在规则 DSL 中表达。
- 已将敌人 `intent_sequence` 迁移为 `ai.fixed_sequence.intents`，并将 UI `preview` 与真实 `effects` 分离。
- 已从规则层 Domain 模型中移除卡牌 / 敌人 / 遗物 / 奖励包的展示字段，Godot 表现层通过 `GameContent` 查询 card / enemy / relic / reward pack view。
- 已删除旧路径下的内容 JSON 与旧 schema，避免后续开发误用旧结构。
- 已更新 `game/tools/data_validator/validate_data.py`，校验 12 个数据文件和 12 个 schema，并检查 gameplay / presentation / localization / asset manifest 之间的交叉引用。

验证结果：
- `python3 game/tools/data_validator/validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- `dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `dotnet build game/RoguelikeCardGame.csproj -v:minimal`：通过。

### 2026-06-08 UI 资源接入完成记录

已完成：
- 已盘点当前 `game/assets/art/` 下可用于第一版 MVP 的背景、主角、卡牌模板、卡面插画、卡牌包、敌人、遗物、UI 图标和 VFX 资源；当前主要缺口不是 PNG 文件缺失，而是这些资源尚未接入 Godot 运行界面。
- 已扩展 `game/data/presentation/assets.json`，补齐背景、主角、UI 图标和 VFX 的 asset manifest 记录，使表现层可以通过稳定 asset ID 读取资源。
- 已更新 `game/src/Presentation/Menus/MainMenu.cs`，让主菜单、战斗、奖励选择和结算流程读取 `GameContent` 中的 presentation view：背景图、剑客立绘、敌人立绘、卡牌模板、卡面插画、卡牌包图、遗物图标、状态图标和结算 VFX 均已接入当前真实游戏程序。
- 已将 PNG 加载改为直接读取图片字节并创建 `ImageTexture`，避免依赖本地 `.import` 缓存导致新环境打开项目时资源不显示。
- 已更新 [[design/03_experience/00_ui_ux|界面与交互]]、[[design/03_experience/01_visual_direction|视觉方向]] 和 [[design/08_governance/01_change_log|变更日志]]，明确新增美术资源必须进入 asset manifest 并被 view 引用后才视为已接入游戏。
- 本轮仍未拆分正式 `BattleScreen` / `RewardScreen` 场景组件，也未实现完整动画时间轴；当前目标是把已生成的场景与 UI 资源先接入可玩的 MVP 主流程。

验证结果：
- `python3 game/tools/data_validator/validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- `dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `dotnet build game/RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `godot-mono --headless --path game --quit`：通过，项目可加载，且不再输出 PNG resource loader 错误。

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

#### 任务 12 阶段完成记录：UI 场景解耦

完成日期：2026-06-09

已完成：
- 已将原本集中在 `game/src/Presentation/Menus/MainMenu.cs` 中的开始界面、战斗界面、奖励界面和结算界面拆分为独立表现层脚本与 Godot 场景。
- `MainMenu.cs` 已调整为 MVP 流程协调器，只负责加载内容、持有 Run / Combat / Reward 流程状态、调用 Application 服务，并在各个界面之间切换。
- 已新增开始界面 `game/src/Presentation/Menus/StartMenuScreen.cs` 与 `game/scenes/menus/StartMenuScreen.tscn`。
- 已新增战斗界面 `game/src/Presentation/Battle/BattleScreen.cs` 与 `game/scenes/battle/BattleScreen.tscn`，负责战斗 HUD、玩家 / 敌人 / 手牌布局、目标选择、出牌请求、结束回合请求等表现层交互。
- 已新增奖励界面 `game/src/Presentation/Rewards/RewardScreen.cs` 与 `game/scenes/rewards/RewardScreen.tscn`，负责卡牌包选择、打开卡牌包、候选牌选择和确认奖励的表现层交互。
- 已新增结算界面 `game/src/Presentation/Menus/RunResultScreen.cs` 与 `game/scenes/menus/RunResultScreen.tscn`，负责 MVP 通关 / 失败结算和重开入口。
- 已新增共享表现层基类 `game/src/Presentation/Shared/ComicScreen.cs`，集中管理漫画风背景、图片加载、图标按钮、面板、标签、卡牌/状态控件等共用 UI helper。
- 三类核心场景通过事件向 `MainMenu.cs` 汇报交互请求，表现层不自行结算伤害、抽牌、连锁、费用、防御、奖励领取或 Run 推进，仍由 Application / Domain 规则层负责。
- 本次只做表现层职责解耦与场景文件落地，未扩展路线图、商店、事件、休息节点、长期成长或首版范围外系统。

验证结果：
- `dotnet build game\RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `python game\tools\data_validator\validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- `dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- Godot 4.6.3 .NET headless 加载 `game/project.godot`：通过，项目可加载。
- 当前 Codex 沙箱中 `dotnet` 和 Godot headless 可能因写入 `.godot\mono\temp\obj`、`obj` 或 `user://` 日志缓存受限而失败；已按权限规则重跑并验证通过。

#### 任务 12 阶段完成记录：第一版轻量动效接入

完成日期：2026-06-09

已完成：
- 已在 `game/src/Presentation/Battle/BattleScreen.cs` 中接入第一版事件驱动 Tween / VFX 动效。
- 战斗动效由规则层 `CombatLogEvent` 触发，覆盖卡牌出手、伤害命中、敌人抖动、玩家受击、获得防御、连锁提升、3 / 5 / 8 阈值达成、终结牌冲击波、敌人行动、抽牌、弃牌、重洗和敌人死亡淡出。
- 已在 `game/src/Presentation/Menus/MainMenu.cs` 中将出牌和结束回合改为异步表现流程：规则层先完成结算并产生日志，当前战斗屏幕播放新增日志事件，随后刷新到结算后的新 UI 状态。
- 已在 `game/src/Presentation/Rewards/RewardScreen.cs` 中接入奖励界面动效，覆盖卡牌包打开、候选卡依次弹出和选卡确认反馈。
- 已增加交互防抖：动画播放期间忽略新的出牌、结束回合、开包和选卡请求，避免重复触发造成状态错乱。
- 已更新 [[design/03_experience/00_ui_ux|界面与交互]]、[[design/03_experience/01_visual_direction|视觉方向]] 和 [[design/08_governance/01_change_log|变更日志]]，明确当前阶段是轻量 Tween / VFX 方案，不扩展完整角色逐帧动作、骨骼动画或镜头时间轴。
- 本次未改动规则层战斗结算、卡牌效果、敌人意图、奖励逻辑或内容数据。

验证结果：
- `python3 game/tools/data_validator/validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- `dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `dotnet build game/RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `godot-mono --headless --path game --quit`：通过，项目可加载。

#### 任务 12 阶段修复记录：重复手牌动画目标错误

完成日期：2026-06-09

已完成：
- 修复“点击任意同名手牌时总是第一张卡牌抖动”的问题。
- 原因是战斗界面出牌事件只传递 `cardId`，动画层在同名牌列表中只能选择第一张对应节点；初始牌组包含多张同名行动牌，因此视觉反馈会落到第一张同名牌上。
- 已将 `BattleScreen.CardRequested` 调整为传递 `cardId` 与 `handIndex`。
- 已将 `MainMenu.cs` 的出牌流程改为把 `handIndex` 传给规则层和动画层。
- 已扩展 `CardPlayService`，支持可选 hand slot 出牌；当传入 `handIndex` 时，会校验对应槽位确实是该卡牌，并从该槽位移除手牌。
- 已在 `CardPlayed` 日志的 `numeric_changes.hand_index` 中记录实际出牌槽位，供表现层动画和后续调试使用。
- 已补充规则层回归测试，覆盖重复同名牌按指定槽位出牌时，不会错误移除前面的同名牌。

验证结果：
- `python3 game/tools/data_validator/validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- `dotnet build game/RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `godot-mono --headless --path game --quit`：通过，Godot 版本为 `4.6.3.stable.mono`，项目可启动。

#### 任务 12 阶段完成记录：多敌人头顶状态与意图展示

完成日期：2026-06-11

已完成：
- 已更新 `game/src/Presentation/Battle/BattleEnemyView.cs`，每个存活敌人立绘头顶常驻显示轻量纸质状态条。
- 头顶状态条包含敌人名称、生命、当前防御和当前意图摘要；攻击 / 防御 / 压迫意图使用对应 UI 图标和颜色区分。
- 多敌人遭遇中，每个敌人各自显示自己的状态和意图，不再依赖右上角单个 HUD 承载全部敌人信息。
- 右上敌人 HUD 仍保留为当前焦点详情：无 hover / 拖拽目标时显示第一个存活敌人，hover 或单体目标箭头指向时切换到对应敌人，用于展示完整意图文本。
- 已更新 `game/src/Presentation/Battle/BattleScreen.cs`，向 `BattleEnemyView` 传入完整 `CombatState` 和字体加载器，使敌人视图可以基于当前战斗状态绘制每个敌人的头顶信息。
- 已同步更新 [[design/03_experience/00_ui_ux|界面与交互]] 和 [[design/08_governance/01_change_log|变更日志]]，明确多敌人战斗以敌人头顶状态条作为核心信息承载，右上 HUD 只作为焦点详情。
- 本次只调整战斗表现层信息布局，不改动敌人意图结算、卡牌目标判定、敌人数据、卡牌数值、奖励流程或 Run 推进。

验证结果：
- `python3 game/tools/data_validator/validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- `dotnet build game/RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `godot-mono --headless --path game --quit`：通过，Godot 版本为 `4.6.3.stable.mono`，项目可启动。
- `dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `python3 game/tools/data_validator/validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- `godot-mono --headless --path game --quit`：通过，项目可加载。

#### 任务 12 阶段修复记录：敌人黑底与站位调整

完成日期：2026-06-09

已完成：
- 已核查 `game/assets/art/enemies/*.png`，敌人 PNG 为 RGBA 且四角透明；截图中明显黑底主要来自战斗 UI 的敌人黑色面板容器。
- 已更新 `game/src/Presentation/Battle/BattleScreen.cs`，敌人控件不再使用大面积黑色 `PanelContainer`，改为透明舞台层直接显示敌人立绘。
- 已保留轻量纸质状态条显示敌人名称、生命和防御，避免状态信息混入立绘黑底。
- 已将敌人组整体向右移动，改善玩家左侧、敌人右侧的战斗构图。
- 已更新 [[design/03_experience/00_ui_ux|界面与交互]] 和 [[design/08_governance/01_change_log|变更日志]]，明确敌人立绘应融入战斗背景，不应包在黑色卡片容器中。

验证结果：
- `dotnet build game/RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `python3 game/tools/data_validator/validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- `godot-mono --headless --path game --quit`：通过，项目可加载。

#### 任务 12 阶段完成记录：统一卡牌 Panel 实例化

完成日期：2026-06-09

已完成：
- 已先核对 `game/assets/art/cards/templates/` 下行动牌、技能牌、终结牌模板资源，以及 [[design/03_experience/01_visual_direction|视觉方向]] 中关于卡牌组件尺寸、卡面插图、费用、卡牌名和效果文本运行时组合的要求。
- 已新增统一卡牌组件工厂 `game/src/Presentation/Cards/CardPanel.cs`，集中负责根据 `CardDefinition`、`card_views.json` 和 `assets.json` 组合卡牌显示。
- `CardPanel` 会根据卡牌类型选择对应模板：行动牌使用 `action_card_template.png`，技能牌使用 `skill_card_template.png`，终结牌使用 `finisher_card_template.png`。
- `CardPanel` 会根据卡牌 ID 从 `CardViewsById` 读取卡面插图资源，并将插图按比例缩放、居中放入模板透明画框内，避免拉伸变形。
- `CardPanel` 会将费用 / 连锁需求、卡牌名称和效果文本绘制到模板对应区域，模板作为上层覆盖，保留卡框、画框边缘、标题栏和效果文本区。
- `CardPanel` 暴露目标宽度参数，卡牌实例最终按等比例缩放到战斗手牌或奖励候选所需大小，三类卡牌共用同一组件比例和布局逻辑。
- 已更新 `game/src/Presentation/Battle/BattleScreen.cs`，战斗手牌不再手写模板 / 插图 / 文本拼装，改为调用 `CardPanel.Create(...)`，并保留无法出牌灰显、tooltip 和点击出牌热区。
- 已更新 `game/src/Presentation/Rewards/RewardScreen.cs`，奖励候选牌不再维护独立卡牌显示实现，改为调用同一个 `CardPanel.Create(...)`，并保留选择 / 取消选择热区与选中标记。
- 本次只统一表现层卡牌实例化方式，未改动规则层卡牌效果、费用、连锁需求、奖励逻辑或内容数据。

验证结果：
- `dotnet build game\RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `python game\tools\data_validator\validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- `dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- Godot 4.6.3 .NET headless 加载 `game/project.godot`：通过，项目可加载。
- 当前 Codex 沙箱中 `dotnet` 和 Godot headless 可能因写入 `.godot\mono\temp\obj`、`obj` 或 `user://` 日志缓存受限而失败；已按权限规则重跑并验证通过。

#### 任务 12 阶段完成记录：表现层流程与特效重构

完成日期：2026-06-09

已完成：
- 已分析 `game/src/Presentation/Battle/BattleScreen.cs` 中由轻量动效提交新增的渲染前引用清理逻辑：`enemyNodes`、`cardNodes`、`cardNodesByHandIndex`、`playerNode`、状态面板和 `fxLayer` 会在每次 `Render(...)` 前重置，避免后续动画拿到上一轮重建 UI 后的旧节点引用。
- 已将 `game/src/Presentation/Menus/MainMenu.cs` 从“大型入口脚本”收窄为 Godot 主入口：只负责加载 `GameContent`、创建场景宿主与 MVP 流程控制器，并提供启动失败兜底显示。
- 已新增 `game/src/Presentation/Flow/MvpRunFlowController.cs`，承接 MVP Run 表现层流程编排：开始 Run、进入当前遭遇、战斗出牌与结束回合、战斗胜负、奖励包选择、确认奖励、精英遗物发放、下一战推进、Boss 通关 / 失败结算和重开。
- 已新增 `game/src/Presentation/Shared/SceneScreenHost.cs`，集中处理 Godot 场景加载、当前 screen 清理、`ComicScreen` 初始化、纹理 / 字体缓存复用和 fatal error 界面显示。
- `StartMenuScreen`、`BattleScreen`、`RewardScreen`、`RunResultScreen` 继续通过事件向流程控制器汇报用户交互；伤害、费用、连锁、抽弃牌、防御、奖励领取和 Run 推进仍由 Application / Domain 层结算，表现层不自行改写规则结果。
- 已将 `game/src/Presentation/Battle/BattleScreen.cs` 中通用 Tween / VFX helper 抽取到 `game/src/Presentation/Shared/ComicScreen.cs`，包括 `CreateFxLayer`、`PulseNodeAsync`、`ShakeNodeAsync`、`LungeNodeAsync`、`SpawnVfxAsync`、`WaitAsync` 和 `CenterOf`。
- 已同步更新 `game/src/Presentation/Rewards/RewardScreen.cs`，删除其本地重复的 pulse / VFX / wait / center 实现，改为复用 `ComicScreen` 中的公共特效方法。
- `BattleScreen.cs` 现在主要保留战斗 HUD、敌人 / 手牌显示、目标选择、出牌请求、战斗日志事件到具体动效的映射；奖励界面也只保留奖励包与候选卡交互及其场景特有动效。
- 本次重构只调整表现层职责边界和公共特效复用方式，未改动规则层战斗结算、卡牌效果、敌人意图、奖励逻辑、内容数据或第一版 MVP 范围。

验证结果：
- `dotnet build game\RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `python game\tools\data_validator\validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- Godot 4.6.3 .NET headless 加载 `game/project.godot`：通过，项目可加载。
- 当前 Codex 沙箱中 Godot headless 可能因写入 `user://logs` 被拦截并崩溃；已按权限规则重跑并验证通过。

#### 任务 12 阶段完成记录：战斗 UI 标准图布局接入

完成日期：2026-06-10

已完成：
- 已根据 [[design/03_experience/assets/battle_ui_effect_concept_2026-06-09_v2.png|第一版 MVP 战斗场景设计标准]] 调整 `game/src/Presentation/Battle/BattleScreen.cs` 的战斗界面布局和气质。
- 已移除战斗界面顶部常驻的“第 X/6 战”和教学文案显示，把中上方稳定区域改为连锁 HUD。
- 已使用 `chain_meter_8_slots` 与 `chain_point_red` 运行时组合 8 槽连锁条，并由 Godot Label 动态绘制 `连锁 X/8`；当连锁超过 8 层时显示 `连锁 X/8+`。
- 已将玩家 HUD 固定到左上，只使用拆分 alpha 组件 `player_health_bar` 与 `player_block_bar`，分别动态绘制 HP 与 Defense，不显示头像或第三行状态栏。
- 已将敌人 HUD 固定到右上，只使用拆分 alpha 组件 `enemy_name_bar`、`enemy_health_bar` 与 `enemy_block_bar`，分别动态绘制敌人名称、HP 与 Defense；敌人意图改为右上 HUD 下方的动态 Label / 图标组合。
- 已将敌人舞台控件改为只承载透明立绘、目标点击热区和目标选中标记，敌人名称、生命、防御不再显示在敌人脚下。
- 已将 AP 徽章与抽牌堆固定到左下，将弃牌堆与结束回合按钮固定到右下，并使用对应拆分 alpha PNG 作为底图，所有数字和中文标签仍由 Godot 动态绘制。
- 已将战斗手牌从横向 HBox 改为底部中间紧凑扇形叠放，保留 `handIndex` 点击出牌、无法出牌 tooltip、灰显和动画节点引用。
- 已调整主角与敌人舞台站位，使双方脚底基线更接近同一水平线，并为更高 Boss 预留垂直空间。
- 已将连锁提升和 3 / 5 / 8 阈值 VFX 坐标迁移到中上方连锁条区域。
- 已将 `battle_ui_gpt_components_v2/` 中本轮使用的背景迁移并覆盖到 `game/assets/art/backgrounds/mvp_battle_desert_background_no_ui.png`，并将 alpha UI 组件复制到 `game/assets/art/ui/`。
- 已更新 `game/data/presentation/assets.json`，新增 `asset.ui.battle.*` 资源 ID，使表现层通过 asset manifest 读取组件，不在场景中散落素材路径。
- 本次未改动规则层战斗结算、卡牌效果、敌人意图、奖励逻辑或内容数据；表现层仍只读取 `CombatState`、`RunState`、`DeckZones`、`CombatLogEvent` 和 presentation view。

新增 / 接入资源：
- `game/assets/art/backgrounds/mvp_battle_desert_background_no_ui.png`
- `game/assets/art/ui/ui_player_health_bar.png`
- `game/assets/art/ui/ui_player_block_bar.png`
- `game/assets/art/ui/ui_enemy_name_bar.png`
- `game/assets/art/ui/ui_enemy_health_bar.png`
- `game/assets/art/ui/ui_enemy_block_bar.png`
- `game/assets/art/ui/ui_chain_meter_8_slots.png`
- `game/assets/art/ui/ui_chain_point_red.png`
- `game/assets/art/ui/ui_action_point_badge.png`
- `game/assets/art/ui/ui_draw_pile_panel.png`
- `game/assets/art/ui/ui_discard_pile_panel.png`
- `game/assets/art/ui/ui_end_turn_button.png`

验证结果：
- `python game\tools\data_validator\validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- `dotnet build game\RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- Godot 4.6.3 .NET headless 启动 `--path game --quit-after 3`：通过，项目与主场景可启动。
- 当前 Codex 沙箱中 `dotnet` 与 Godot headless 可能因写入 `.godot\mono\temp\obj`、`obj` 或 `user://logs` 被拦截；已按权限规则重跑并验证通过。

仍需后续视觉验收 / 美术替换：
- 当前 UI PNG 为 gpt-image-2 风格生成组件，虽已作为真实 alpha PNG 接入，但不是最终手工切图。
- 连锁红点槽位、HUD 文本落点、Boss 大体型站位和手牌 hover / selected 抬升仍需要基于实际运行截图继续微调。
- 后续若有正式手绘 UI 资源，应继续放入 `game/assets/art/ui/` 并通过 `game/data/presentation/assets.json` 以稳定 `asset.ui.battle.*` ID 接入。

#### 任务 12 阶段完成记录：卡牌 VFX 数据化

完成日期：2026-06-11

已完成：
- 已将“卡牌使用时播放哪个特效”的配置从 `game/src/Presentation/Battle/BattleScreen.cs` 的类型 / 费用硬编码分支，迁移到卡牌 Domain 数据。
- 已在 `game/src/Domain/Cards/CardDefinition.cs` 中新增 `VfxAsset` 字段，并使用 JSON 字段名 `vfx_asset`。
- 已在 `game/data/gameplay/cards/cards.json` 为 MVP 全部卡牌配置 `vfx_asset`，例如：
  - `card.basic_strike` 使用 `asset.vfx.slash_speed_lines`。
  - `card.heavy_strike` 与 `card.guard_break` 使用 `asset.vfx.heavy_strike_impact_frame`。
  - `card.arc_sweep_finish` 使用 `asset.vfx.group_sweep_arc_light`。
  - 防御类卡牌使用 `asset.vfx.defense_shield_flash`，抽牌 / 预备类卡牌使用 `asset.vfx.chain_gain_spark`。
- 已在 `game/src/Infrastructure/Content/GameContent.cs` 中解析 `vfx_asset` 到 `CardDefinition.VfxAsset`。
- 已更新 `game/data/schemas/gameplay/cards.schema.json`，允许 `vfx_asset` 并约束其格式为 `asset.vfx.*`。
- 已更新 `game/tools/data_validator/validate_data.py`，校验卡牌 `vfx_asset` 必须引用 `game/data/presentation/assets.json` 中已存在的资源 ID，避免数据写入不存在的特效。
- 已更新 `game/src/Presentation/Battle/BattleScreen.cs`，伤害、防御、抽牌、获得行动点和临时减费占位等卡牌效果播放时优先读取 `CardDefinition.VfxAsset`；如果卡牌没有指定特效，则默认回退到 `asset.vfx.enemy_hit_comic_burst`。
- 已移除战斗表现层中“行动牌默认斩击、2 费行动牌默认重击、终结牌默认冲击波”的硬编码推断，后续新增卡牌只需在数据中指定特效资源。
- 已在 `game/tests/Unit/Program.cs` 中补充 `CardDefinition.VfxAsset` 序列化 / 反序列化 smoke test。
- 本次只重构卡牌特效来源和表现层播放映射，未改动卡牌数值、伤害 / 防御 / 抽牌 / 连锁 / 费用等规则结算。

验证结果：
- `python game\tools\data_validator\validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- `dotnet build game\RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- Godot 4.6.3 .NET headless 启动 `--path game --quit-after 3`：通过，项目可启动。
- 当前 Codex 沙箱中 `dotnet` 与 Godot headless 可能因写入 `.godot\mono\temp\obj`、`obj` 或 `user://logs` 被拦截；已按权限规则重跑并验证通过。

后续资源规范：
- 新增卡牌时，应优先在 `game/assets/art/vfx/` 添加对应 PNG，并在 `game/data/presentation/assets.json` 中登记稳定资源 ID，再在卡牌数据中通过 `vfx_asset` 引用。
- 可采用 `vfx_` + 卡牌语义名的文件命名方式，例如 `vfx_basic_strike.png`、`vfx_heavy_strike.png`，并映射为 `asset.vfx.basic_strike`、`asset.vfx.heavy_strike`。
- 若暂时没有专属特效，可以省略 `vfx_asset`，表现层会使用 `asset.vfx.enemy_hit_comic_burst` 作为默认命中特效；但正式内容应尽量为关键卡牌配置专属 VFX。

#### 任务 12 阶段完成记录：手牌 hover 与拖拽出牌交互

完成日期：2026-06-11

已完成：
- 已优化 `game/src/Presentation/Battle/BattleScreen.cs` 的战斗手牌交互：手牌仍保持底部紧凑扇形布局，悬停任意卡牌时该牌会从原位上移、轻微突出并提升 `ZIndex`，离开后恢复原始位置、旋转角度和层级。
- 已将所有卡牌出牌方式从点击触发改为拖拽释放触发；单纯点击卡牌不会出牌。
- 拖拽期间卡牌跟随鼠标移动，并保持置顶 / 突出状态；拖拽过程中不修改规则层 `CombatState`。
- 对 `TargetRule.Self`、`TargetRule.AllEnemies` 和 `TargetRule.None` 卡牌，已新增中场释放区域：只有拖入主角、敌人和手牌之间的空场区域并释放，才会请求出牌；释放区仅在拖拽时显示高亮，不常驻遮挡画面。
- 对 `TargetRule.SingleEnemy` 卡牌，已新增 Godot 原生绘制的漫画风拖拽箭头：粗线、深色投影、清晰箭头头部；鼠标悬停有效敌人目标时，对应敌人显示高亮反馈。
- 单体敌人目标卡牌释放时必须指向有效敌人；释放到空白区域或仅中场释放区都会取消拖拽并回到原位。
- 不可打出状态下仍保留 hover 和拖拽视觉反馈，但释放时会再次调用 `CardPlayService.CanPlayCard(combat, card, targetEnemyInstanceId, handIndex)` 校验，不能绕过费用、连锁、回合状态、目标合法性或手牌槽位检查。
- 已将 `BattleScreen.CardRequested` 扩展为传递 `cardId`、`handIndex` 和可选 `targetEnemyInstanceId`。
- 已更新 `game/src/Presentation/Flow/MvpRunFlowController.cs`，出牌时使用表现层释放得到的明确目标敌人 ID；单体卡牌不再自动回退到当前选中敌人或第一个敌人。
- 已保持同名卡牌按 `handIndex` 出牌的修复不回退：拖拽释放请求仍携带并校验实际手牌槽位。
- 已在动画 / 出牌流程中保持交互防抖：成功释放出牌或请求结束回合后，当前战斗屏幕会锁定进一步手牌交互，避免动画期间重复拖拽或重复出牌。
- 已同步更新 [[design/03_experience/00_ui_ux|界面与交互]] 和 [[design/08_governance/01_change_log|变更日志]]，记录第一版 MVP 的拖拽释放出牌基线。
- 本次只修改表现层交互和流程事件签名，未改动规则层结算逻辑、卡牌数值、敌人数据、奖励逻辑或内容数据。

实现要点：
- hover 使用每张手牌的原始扇形位置、旋转和层级快照恢复，不触发整手牌重新排版。
- 非单体目标释放区使用运行时 `PanelContainer` 和几何命中判断实现，只在拖拽时显示克制高亮。
- 单体目标箭头使用 `Control._Draw()` 绘制双层线条和三角箭头头部，后续可替换为专门漫画风美术资源。
- 敌人目标高亮作为敌人舞台控件内的透明边框层显示，不改变敌人立绘或 HUD 布局。
- 规则合法性仍由 `CardPlayService` 负责，表现层只负责 hover、drag、arrow、release-zone highlight、target highlight 和出牌请求。

验证结果：
- `dotnet build game\RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `python game\tools\data_validator\validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- Godot 4.6.3 .NET headless 启动 `D:\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe --headless --path game --quit-after 3`：通过，项目可启动。
- 当前 Codex 沙箱中 `dotnet` 与 Godot headless 可能因写入 `.godot\mono\temp\obj`、`obj` 或 `user://logs` 被拦截；已按权限规则重跑并验证通过。

后续视觉替换点：
- 当前箭头、释放区和目标高亮为 Godot 原生绘制 / StyleBox 实现，已满足响应、清晰和不遮挡；后续若有正式手绘漫画风箭头、释放区提示或目标锁定资源，可继续放入 `game/assets/art/ui/` 或 `game/assets/art/vfx/` 并通过 `game/data/presentation/assets.json` 以稳定 asset ID 接入。

#### 任务 12 阶段完成记录：战斗表现层解耦

完成日期：2026-06-11

已完成：
- 已将 `game/src/Presentation/Battle/BattleScreen.cs` 从 1300+ 行的集中式战斗界面脚本，收敛为约 230 行的屏幕编排脚本。
- `BattleScreen.cs` 现在主要负责：接收 `CombatState` / `RunState` / `EncounterDefinition`，创建屏幕 root，转发战斗界面事件，显示玩家立绘、重开按钮、临时反馈和调试日志，并为动画层组装当前节点引用。
- 已新增 `game/src/Presentation/Battle/BattleHandView.cs`，承接手牌扇形渲染、hover 抬升置顶、拖拽释放、`handIndex` 节点映射、释放前 `CardPlayService.CanPlayCard` 校验和出牌请求事件。
- 已新增 `game/src/Presentation/Battle/BattleTargetingOverlay.cs`，承接中场释放区、单体目标拖拽箭头、敌人拖拽目标高亮、敌人与释放区几何命中检测。
- 已新增 `game/src/Presentation/Battle/BattleLogAnimator.cs`，承接 `CombatLogEvent` 到 Tween / VFX 的动画解释逻辑，包括出牌、伤害、防御、连锁、抽弃牌、敌人行动和死亡反馈。
- 已新增 `game/src/Presentation/Battle/BattleEnemyView.cs`，承接敌人舞台站位、敌人立绘节点创建、目标点击热区、选中图标、拖拽目标高亮挂载和 `enemyInstanceId -> Control` 节点映射。
- 已新增 `game/src/Presentation/Battle/BattleHudView.cs`，承接玩家 HUD、敌人 HUD、连锁 HUD、行动点徽章、抽牌堆、弃牌堆、遗物条和结束回合按钮创建，并向动画层暴露连锁、格挡、行动点和牌堆节点引用。
- 已将 `game/src/Presentation/Shared/ComicScreen.cs` 中的通用 Tween / VFX helper 调整为程序集内可复用，供 `BattleLogAnimator` 调用，避免动画 helper 在多个战斗表现类中重复。
- Godot 已为新增 C# 脚本生成对应 `.uid` 文件：`BattleHandView.cs.uid`、`BattleTargetingOverlay.cs.uid`、`BattleLogAnimator.cs.uid`、`BattleEnemyView.cs.uid`。
- 本次解耦只调整表现层职责边界和文件结构，未改动规则层战斗结算、卡牌效果、敌人意图、奖励逻辑、内容数据或第一版 MVP 范围。

拆分后主要文件规模：
- `game/src/Presentation/Battle/BattleScreen.cs`：约 230 行。
- `game/src/Presentation/Battle/BattleHudView.cs`：约 345 行。
- `game/src/Presentation/Battle/BattleEnemyView.cs`：约 154 行。
- `game/src/Presentation/Battle/BattleHandView.cs`：约 438 行。
- `game/src/Presentation/Battle/BattleTargetingOverlay.cs`：约 249 行。
- `game/src/Presentation/Battle/BattleLogAnimator.cs`：约 287 行。

验证结果：
- `dotnet build game\RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `python game\tools\data_validator\validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- Godot 4.6.3 .NET headless 启动 `D:\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe --headless --path game --quit-after 3`：通过，项目可启动。
- 当前 Codex 沙箱中 `dotnet` 与 Godot headless 可能因写入 `.godot\mono\temp\obj`、`obj` 或 `user://logs` 被拦截；已按权限规则重跑并验证通过。

后续维护建议：
- 后续新增手牌交互优先进入 `BattleHandView.cs`。
- 后续新增目标提示、AOE 范围提示或箭头美术替换优先进入 `BattleTargetingOverlay.cs`。
- 后续新增敌人站位、Boss 体型适配或敌人舞台 tooltip 优先进入 `BattleEnemyView.cs`。
- 后续新增 HUD 元素、状态图标或数值条布局优先进入 `BattleHudView.cs`。
- 后续新增战斗事件动画、音效触发或减少动画设置优先进入 `BattleLogAnimator.cs`。

#### 任务 12 阶段修复记录：单体目标箭头拖拽效果优化

完成日期：2026-06-11

已完成：
- 已排查并修复 `TargetRule.SingleEnemy` 卡牌拖拽时箭头不明显 / 不出现的问题：原先单体目标卡牌跟随鼠标移动，箭头起点与鼠标端点过近，且箭头层级容易被拖拽卡牌遮挡。
- 已更新 `game/src/Presentation/Battle/BattleHandView.cs`：单体敌人目标卡牌进入拖拽后只从原扇形位置向上抬升一小段距离，并保持置顶 / 突出，不再跟随鼠标移动。
- 已保持非单体目标卡牌原有拖拽手感不变：`TargetRule.Self`、`TargetRule.AllEnemies`、`TargetRule.None` 卡牌仍跟随鼠标移动，并通过中场释放区触发出牌。
- 已更新 `game/src/Presentation/Battle/BattleTargetingOverlay.cs`：新增从视口固定锚点绘制箭头的入口，单体目标箭头从卡牌上方锚点出发，末端跟随鼠标。
- 已提高箭头绘制层级并设为非相对 Z 排序，避免被手牌、敌人或 HUD 盖住。
- 已将单体目标箭头从直线改为带弧度的二次贝塞尔路径：箭身使用多段采样绘制，保留粗线、深色投影和浅色主线；箭头头部沿曲线末端切线方向旋转。
- 单体敌人目标卡牌释放时仍必须指向有效敌人；松开鼠标后箭头和目标高亮会清除，卡牌归位，再由流程控制器按 `cardId`、`handIndex`、`targetEnemyInstanceId` 请求规则层出牌。
- 本次只调整表现层拖拽视觉、箭头绘制和目标提示，不改动规则层结算、卡牌数值、敌人数据、目标规则或奖励流程。

实现要点：
- `BattleHandView` 负责区分单体目标卡牌与非单体目标卡牌的拖拽行为；单体目标卡牌使用固定卡牌锚点 + 鼠标端点绘制箭头，非单体目标卡牌继续使用卡牌跟随鼠标 + 中场释放区。
- `BattleTargetingOverlay` 负责箭头曲线路径、敌人目标命中检测、目标高亮和箭头隐藏 / 清理。
- 箭头曲线当前使用 Godot 原生 `_Draw()` 绘制，不依赖专门美术资源；后续如有正式漫画风箭头贴图或材质，可继续在 `BattleTargetingOverlay` 内替换绘制实现。

验证结果：
- `dotnet build game\RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `python game\tools\data_validator\validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- Godot 4.6.3 .NET headless 启动 `D:\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe --headless --path game --quit-after 3`：通过，项目可启动。
- 当前 Codex 沙箱中 Godot headless 可能因写入 `user://logs` 被拦截并崩溃；已按权限规则重跑并验证通过。

#### 任务 12 阶段完成记录：奖励包透明化与按钮风格调整

完成日期：2026-06-11

已完成：
- 已将三类奖励卡牌包资源替换为明亮手绘漫画纸质包装图，资源位于 `game/assets/art/cards/packs/reward_pack_mvp_action.png`、`game/assets/art/cards/packs/reward_pack_mvp_skill.png`、`game/assets/art/cards/packs/reward_pack_mvp_finisher.png`。
- 三张卡牌包图均已处理为透明背景 PNG，不再使用黑色或暗黑哥特底图承载。
- 已更新 `game/src/Presentation/Rewards/RewardScreen.cs`：奖励包选择控件移除黑色 `PanelContainer` 底板，改为透明点击区 + 纸质标签，直接叠加在漫画荒漠背景上。
- 已将“跳过并进入下一战”调整为“不拿牌，进入下一战”的纸质次级按钮样式，与奖励界面背景和卡牌包图统一。
- 已为奖励包选择增加轻量入场动画，三类卡牌包依次淡入 / 弹出；打开卡包时的脉冲和连锁火花范围同步放大。
- 已同步更新 [[design/03_experience/00_ui_ux|界面与交互]] 和 [[design/08_governance/01_change_log|变更日志]]，明确奖励包必须使用透明纸质包装图、不能包裹黑色底板，跳过按钮应为次级纸质操作。
- 本次只调整奖励界面的表现层、卡包美术资源和对应文档，不改动奖励规则、卡牌候选、卡牌数值、Run 推进或数据结构。

验证结果：
- `python3 game/tools/data_validator/validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- `dotnet build game/RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `godot-mono --headless --path game --quit`：通过，项目可加载。

#### 任务 12 阶段修复记录：奖励选择 hover 与卡包点击命中

完成日期：2026-06-11

已完成：
- 已新增 `game/src/Presentation/Rewards/RewardPackHitArea.cs`，通过覆盖 `Control._HasPoint(...)` 按卡牌包 PNG 的 alpha 非透明区域判断鼠标命中。
- 已更新 `game/src/Presentation/Rewards/RewardScreen.cs`：卡牌包不再使用覆盖整个控件矩形的透明 `Button`，点击和 hover 只在鼠标真正位于卡牌包图形上时触发。
- 已为卡牌包选择补充 hover 反馈：鼠标悬停时卡包轻微放大、置顶、提亮，并显示沿卡包 alpha 的柔和高光。
- 已为打开卡包后的 3 张候选奖励卡补充 hover 反馈：鼠标悬停时卡牌轻微放大、置顶、提亮；选中 / 取消选择仍沿用原有选中图标和确认流程。
- 已同步更新 [[design/03_experience/00_ui_ux|界面与交互]] 和 [[design/08_governance/01_change_log|变更日志]]，明确奖励包交互命中不能再由整块透明矩形代替。
- 本次只调整奖励界面交互表现和点击命中，不改动奖励规则、卡牌候选、卡牌数值、Run 推进或数据结构。

验证结果：
- `dotnet build game/RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。

#### 任务 12 阶段完成记录：拖拽有效目标卡牌蒙版反馈

完成日期：2026-06-11

已完成：
- 已优化战斗手牌拖拽反馈：隐藏中场释放区 UI，不再显示“释放到这里”的区域面板，但保留原有释放区几何判定。
- 对 `TargetRule.Self`、`TargetRule.AllEnemies` 和 `TargetRule.None` 卡牌，拖拽到有效中场释放区且 `CardPlayService.CanPlayCard(...)` 通过时，在被拖拽卡牌本身显示金色透明蒙版。
- 已隐藏单体敌人目标卡牌的敌人边框高亮，不再用敌人控件边框提示有效目标。
- 对 `TargetRule.SingleEnemy` 卡牌，箭头指向有效敌人且 `CardPlayService.CanPlayCard(...)` 通过时，在被拖拽卡牌本身显示金色透明蒙版。
- 取消拖拽、释放到无效区域、出牌请求发出或卡牌归位时，都会清除卡牌蒙版，避免残留 UI 状态。
- 本次只调整表现层反馈方式，不改动规则层结算、目标规则、出牌请求参数、卡牌数值、敌人数据或奖励流程。

实现要点：
- `game/src/Presentation/Battle/BattleTargetingOverlay.cs` 将释放区显示与释放区命中检测解耦：释放区面板保持隐藏，但 `IsPointerOverReleaseZone(...)` 仍可基于隐藏控件几何区域判断是否有效。
- `BattleTargetingOverlay.UpdateEnemyHighlights(...)` 现在统一隐藏敌人拖拽高亮边框，敌人命中检测仍保留给单体目标箭头释放判断使用。
- `game/src/Presentation/Battle/BattleHandView.cs` 为每张手牌创建默认隐藏的 `ValidTargetMask`，使用金色半透明 `StyleBoxFlat` 覆盖卡牌区域。
- `BattleHandView.UpdateCardDrag(...)` 根据目标规则分别判断“中场释放区有效”或“箭头指向有效敌人”，仅在规则可打出时显示卡牌蒙版。

验证结果：
- `dotnet build game\RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `python game\tools\data_validator\validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- Godot 4.6.3 .NET headless 启动 `D:\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe --headless --path game --quit-after 3`：通过，项目可启动。
- 当前 Codex 沙箱中 Godot headless 可能因写入 `user://logs` 被拦截并崩溃；已按权限规则重跑并验证通过。

#### 任务 12 阶段修复记录：敌人目标展示与右上 HUD 显示逻辑

完成日期：2026-06-11

已完成：
- 已删除战斗中的敌人点击选取功能：`game/src/Presentation/Battle/BattleEnemyView.cs` 不再创建透明点击 `Button`，不再发出 `EnemySelected` 事件，也不再绘制 `asset.ui.icon.target_selected` 选中目标图标。
- 已将敌人面板交互改为纯表现层 hover：`BattleEnemyView` 新增 `EnemyHoveredChanged(string? enemyInstanceId)`，仅用于通知鼠标进入 / 离开存活敌人热区，不修改规则层 `CombatState`。
- 已更新 `game/src/Presentation/Battle/BattleHudView.cs`：右上角敌人 HUD 默认不渲染，不再用“默认第一个敌人”填充；只有收到显式 focus 敌人 ID 时才显示敌人名称、HP、防御和当前意图。
- 已更新 `game/src/Presentation/Battle/BattleScreen.cs`：聚合敌人 hover 与单体目标卡牌拖拽箭头指向状态；单体拖拽进行中优先使用箭头指向敌人更新右上 HUD，否则使用当前 hover 敌人。
- 已更新 `game/src/Presentation/Battle/BattleHandView.cs`：拖拽 `TargetRule.SingleEnemy` 卡牌时，将箭头当前指向的敌人 ID 作为表现层 HUD focus 状态上报；指向空白区域或拖拽结束时上报空目标并隐藏 HUD。
- 已更新 `game/src/Presentation/Flow/MvpRunFlowController.cs`：删除“当前选中敌人”状态，不再在战斗开始、出牌后或新回合开始时维护默认敌人目标。
- 单体敌人目标卡牌出牌仍只使用释放瞬间箭头命中的明确 `targetEnemyInstanceId`；释放到空白区域不会回退到默认敌人或第一个敌人。
- 已保持同名卡牌按 `handIndex` 出牌的修复不回退，出牌请求仍携带 `cardId`、`handIndex` 和可选 `targetEnemyInstanceId`，并继续通过 `CardPlayService.CanPlayCard(...)` / `PlayCard(...)` 校验。
- 本次只修改表现层 hover / drag 指向状态、HUD 显示来源和必要流程状态清理，未改动规则层结算逻辑、卡牌数值、敌人数据、奖励流程或内容数据。

实现要点：
- 右上敌人 HUD 的显示目标来自 `BattleScreen` 内部的 `hoveredEnemyInstanceId` 与 `dragPointedEnemyInstanceId`，不再来自规则层目标选择状态。
- `BattleHudView.SetFocusedEnemy(null)` 会清除右上角敌人 HUD 节点，因此鼠标未悬停敌人且单体箭头未指向敌人时，不显示名称、HP、防御、意图、空白黑框或占位文本。
- `BattleHandView.CompleteCardDrag()` 仍在释放时重新调用 `BattleTargetingOverlay.EnemyUnderMouse(...)` 获取目标，并用该目标 ID 请求出牌，避免表现层 hover 状态成为规则目标来源。

验证结果：
- `dotnet build game/RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `python3 game/tools/data_validator/validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- `godot-mono --headless --path game --quit`：通过，Godot 版本为 `4.6.3.stable.mono`，项目可启动。
- 当前 Codex 沙箱中普通命令可能因 `bwrap: loopback: Failed RTM_NEWADDR` 被拦截；已按权限规则重跑并验证通过。

#### 任务 12 阶段修复记录：敌人 HUD 常驻显示

完成日期：2026-06-11

已完成：
- 已修复战斗右上敌人 HUD 只有 hover 时才显示的问题。
- 已更新 `game/src/Presentation/Battle/BattleHudView.cs`：战斗界面渲染时立即绘制敌人 HUD；当没有 hover 或单体目标箭头焦点时，默认显示第一个存活敌人的名称、生命、防御和当前意图。
- `BattleHudView.SetFocusedEnemy(null)` 现在不再清空并隐藏敌人 HUD，而是回退到第一个存活敌人；hover 存活敌人或单体目标箭头指向敌人时仍可临时切换右上 HUD 焦点。
- 已同步更新 [[design/03_experience/00_ui_ux|界面与交互]] 和 [[design/08_governance/01_change_log|变更日志]]，明确敌人生命、防御和意图属于战斗核心信息，必须常驻显示，hover / 拖拽只负责切换焦点。
- 本次只调整表现层 HUD 焦点和显示规则，不改动出牌目标判定、敌人数据、敌人意图结算、卡牌数值、奖励流程或 Run 推进。

验证结果：
- `dotnet build game/RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。

#### 任务 12 阶段修复记录：敌人 HUD 按敌人数切换

完成日期：2026-06-11

已完成：
- 已收敛敌人信息展示策略：单敌人遭遇只显示右上完整敌人 HUD，不显示敌人头顶状态条，避免重复展示。
- 已更新 `game/src/Presentation/Battle/BattleHudView.cs`：右上敌人 HUD 只在 `CombatState.Enemies.Count <= 1` 的单敌人遭遇中渲染；多敌人遭遇中 `SetFocusedEnemy(...)` 不再创建右上 HUD。
- 已更新 `game/src/Presentation/Battle/BattleEnemyView.cs`：多敌人遭遇才显示头顶缩略 HUD；单敌人遭遇不再显示头顶 HUD。
- 已将多敌人的头顶状态条改为缩小版敌人 HUD 视觉：使用现有敌人姓名条 / 生命条资源，加一个小意图 chip，减少上一版大纸框造成的突兀感。
- 多敌人头顶缩略 HUD 显示敌人名称、生命、防御和当前意图摘要；右上完整 HUD 不再与头顶 HUD 同时展示同一敌人信息。
- 已同步更新 [[design/03_experience/00_ui_ux|界面与交互]] 和 [[design/08_governance/01_change_log|变更日志]]，明确单敌人 / 多敌人两套敌人信息展示规则。
- 本次只调整战斗表现层信息布局，不改动敌人意图结算、卡牌目标判定、敌人数据、卡牌数值、奖励流程或 Run 推进。

验证结果：
- `dotnet build game/RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。

#### 任务 12 阶段修复记录：多敌人头顶 HUD 三行意图展示

完成日期：2026-06-11

已完成：
- 已更新 `game/src/Presentation/Battle/BattleEnemyView.cs`，将多敌人头顶缩略 HUD 从“姓名条 + 生命条 + 右侧小意图 chip”改为三行纵向布局。
- 三行布局为：第一行敌人名称，第二行生命 / 防御，第三行意图图标 + 意图文本摘要。
- 意图文本不再缩在右侧小角落；攻击、防守、压迫会分别显示为“攻击 N”“防守 N”“压迫 N+M”等可读文本。
- 已保留单敌人遭遇只使用右上完整敌人 HUD 的规则，多敌人遭遇仍关闭右上敌人 HUD。
- 已同步更新 [[design/03_experience/00_ui_ux|界面与交互]] 和 [[design/08_governance/01_change_log|变更日志]]，记录多敌人头顶 HUD 必须按三行展示，意图独占一行。
- 本次只调整战斗表现层信息布局，不改动敌人意图结算、卡牌目标判定、敌人数据、卡牌数值、奖励流程或 Run 推进。

验证结果：
- `dotnet build game/RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。


#### 任务 12 阶段完成记录：Run seed 随机系统与 Deck 洗牌复现

完成日期：2026-06-11

已完成：
- 已新增 `game/src/Infrastructure/Randomness/DeterministicRandom.cs`，实现项目内封装的确定性轻量 PRNG，并提供 Fisher-Yates 洗牌入口，避免依赖 `Random.Shared`、`DateTime` 或表现层临时随机。
- 已新增 `game/src/Infrastructure/Randomness/RunRandomStreams.cs` 与 `RandomStreamName.cs`，以 Run seed + stream name 派生相互独立的随机流，当前包含 `deck`、`map`、`reward`、`encounter`。
- 已新增 `game/src/Infrastructure/Randomness/RunSeedGenerator.cs`，主菜单每次点击开始 MVP 时生成新的非固定 seed。
- 已保持 `RunState.Seed` 作为 Run seed 保存位置，并在战斗创建日志中记录 `run_seed`，便于后续调试、复盘和试玩指标导出。
- 已更新 `game/src/Presentation/Flow/MvpRunFlowController.cs`：每次新 Run 创建 `RunRandomStreams.FromRunSeed(seed)`，并让 `CombatStateFactory`、`CombatTurnService`、`CardPlayService` 共用同一个 `deck` 随机流。
- 已更新 `game/src/Application/Battle/CombatStateFactory.cs`：创建战斗时先复制 `RunState.MasterDeck`，再用 `deck` 随机流洗牌后写入 `DeckZones.DrawPile`，第一回合抽 5 张来自洗牌后的抽牌堆。
- 已沿用并注入 `CombatTurnService` 的重洗入口：抽牌堆不足时，弃牌堆使用同一个 `deck` 随机流洗回抽牌堆；卡牌效果触发抽牌时也通过同一个服务推进 `deck` 流。
- 已更新 `game/src/Application/Debug/DebugRunService.cs`：调试入口使用指定 seed 初始化 `deck` 随机流，便于直接进入指定遭遇时复现手牌顺序。
- 已在 `game/tests/Unit/Program.cs` 补充随机系统 smoke tests，覆盖相同 seed 复现初始手牌、不同 seed 产生不同初始手牌、相同 seed 复现弃牌堆重洗、`deck` 与 `map` / `reward` / `encounter` 随机流互不影响，以及新 Run seed 不再固定为 `12345`。
- 已更新 `game/tests/Unit/RoguelikeCardGame.Tests.csproj`，让规则层 smoke test 纳入 `game/src/Infrastructure/Randomness/` 源码。
- 已更新 [[design/06_technical_production/00_technical_requirements|技术需求]]，以 `deck` 为例详细记录 Run seed 随机产生、随机流派生、初始洗牌、弃牌堆重洗、流隔离和复现条件。
- 已更新 [[design/08_governance/01_change_log|变更日志]]，记录第一版 MVP Run seed 随机系统落地。
- 本次未改动卡牌数值、敌人数据、奖励内容、拖拽出牌规则、敌人 HUD hover / 箭头指向显示、同名卡按 `handIndex` 出牌、奖励流程或结算流程。

机制要点：
- Run seed 是复现入口；主菜单新开 Run 自动生成不同 seed，调试入口可指定 seed。
- 随机流由 `RunRandomStreams` 按 Run seed + stream name 派生，`deck` 流只负责牌库相关随机，`map`、`reward`、`encounter` 已预留但当前 MVP 不实现随机地图、随机奖励池或随机遭遇内容。
- `deck` 流是有状态对象；同一 Run 内战斗初始洗牌、战斗内弃牌堆重洗和抽牌效果触发的重洗会按实际发生顺序推进。
- 一个随机流的调用次数变化不会影响另一个随机流，因此后续奖励随机多调用一次，不会改变 `deck` 的洗牌结果。
- 复现某个初始手牌顺序时，需要使用相同 `RunState.Seed`、相同 `MasterDeck` 内容与顺序、相同遭遇进入顺序，并在该战斗前保持相同的 `deck` 流调用历史。

验证结果：
- `dotnet build game/RoguelikeCardGame.csproj -v:minimal`：通过，0 个警告、0 个错误。
- `dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj`：通过，输出 `Domain model smoke tests passed.`。
- `python3 game/tools/data_validator/validate_data.py`：通过，输出 `Data validation passed. Validated 12 data files and 12 schemas.`。
- `godot-mono --headless --path game --quit`：通过，Godot 版本为 `4.6.3.stable.mono`，项目可启动。
- 当前 Codex 沙箱中普通命令可能因 `bwrap: loopback: Failed RTM_NEWADDR` 被拦截；已按权限规则重跑并验证通过。
