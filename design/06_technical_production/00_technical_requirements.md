# 技术需求

状态：草案  
上级索引：[[design/README|Design Knowledge Base]]

## 目的

定义开发实现必须支持的系统能力、技术约束和工程取舍，保证 [[design/00_product/00_game_concept|游戏概述]] 中的“卡组联动与 combo 爽感”可以被稳定制作、测试和迭代。

## 技术定位

本项目首版技术栈采用 Godot 4.6.x .NET 版 + C#。当前项目推荐并已验证的 Godot 小版本为 4.6.3 stable；项目初始化后应在工程文档、导出模板和构建脚本中锁定具体 Godot 小版本，避免不同开发环境产生资源导入或导出差异。

Godot 负责场景、UI、动画、音频、输入、资源导入和平台导出；C# 负责核心游戏逻辑、数据加载、战斗结算、规则校验、存档、调试工具和测试辅助。除非后续明确需要，运行时玩法逻辑不使用 GDScript 混写，避免同一规则分散在多种脚本语言中。

Codex 作为 coding agent 参与实现与维护：它应优先读取 `design/` 中的设计规范，再修改代码、数据和文档；涉及玩法规则、制作范围或长期维护成本的变化，需要同步更新对应设计文档与 [[design/08_governance/00_decision_log|决策记录]]。

## 核心工程目标

- 数据驱动：卡牌、敌人、遗物、状态、奖励包、固定遭遇和后续地图节点都应以稳定内容 ID 配置，避免把数值和内容写死在场景或 UI 中。
- 可复现：随机数必须由 Run 种子和明确的随机流驱动，方便重放、调试和分享高光构筑。
- 可复盘：战斗结算需要输出结构化日志，记录出牌、目标、行动点变化、连锁层数变化、防御变化、抽弃牌变化、敌人意图和触发效果。
- 可验证：关键规则要能通过自动化测试或调试沙盒验证，尤其是行动点、连锁层数、终结牌、抽弃牌循环、防御结算和奖励包。
- 可扩展：第一版 MVP 内容很小，但代码结构不能阻碍后续扩展普通战斗随机池、路线图、事件、商店、更多遗物和完整 Run。
- 可读反馈：技术实现必须支持 [[design/03_experience/00_ui_ux|界面与交互]] 要求的阈值提示、无法出牌原因、终结牌额外效果预告和高连锁反馈。

## 第一版 MVP 必须支持

第一版 MVP 的实现范围应服务 [[design/00_product/03_scope_and_success_criteria|项目范围与成功标准]] 与 [[design/07_production/00_roadmap_milestones|开发路线图与里程碑]]：

- 1 个线性章节：普通战斗 1 -> 普通战斗 2 -> 普通战斗 3 -> 精英战斗 -> 普通战斗 4 -> Boss 战斗。
- 6 场固定遭遇，不接入普通战斗随机池。
- 初始牌组 10 张，奖励牌约 9 张，普通遗物 1 个。
- 行动牌、技能牌、终结牌三类卡牌及其默认连锁规则。
- 基础行动点上限 3、每回合抽 5 张、抽牌堆不足时重洗弃牌堆。
- 玩家回合结束时清空未用行动点、连锁层数和未使用手牌；防御在敌人行动后、下一回合开始时清空剩余值。
- 终结牌最低连锁使用条件、高连锁额外效果、使用后默认消耗当前连锁层数。
- 普通战斗后的三类卡牌包奖励：行动牌包、技能牌包、终结牌包；打开后从 3 张同类型候选中选择 0-3 张加入卡组。
- MVP 固定可重复奖励池；前两次普通战斗后的终结牌包固定包含 1 张群体攻击终结牌。
- 精英战斗后额外获得 1 个普通遗物；第一版 MVP 中等同固定获得。
- 每场战斗开始时玩家生命回满；生命归零立即结束本次 MVP Run。
- Boss 击败即显示 MVP 通关，并提供重开入口。
- 基础调试入口：直接进入任意固定战斗、指定卡组、指定奖励包、指定随机种子。

## 推荐架构

### 规则层

规则层使用 C# 普通类或服务实现，不直接依赖 Godot 节点树。它负责 Run 状态、战斗状态、卡牌效果、敌人行为、奖励生成、随机数、存档模型和结构化日志。规则层应尽量可被单元测试调用。

### 表现层

表现层使用 Godot 场景和节点实现，包括战斗界面、卡牌手牌、敌人展示、连锁层数 UI、奖励选择、结算界面、动画和音频。表现层通过事件、命令或 ViewModel 读取规则层结果，不应自行决定伤害、抽牌、连锁变化等规则。

### 数据层

数据层读取内容配置、文本本地化、资源引用和版本信息。内容配置必须有校验流程；错误信息要能定位到具体内容 ID、字段和非法引用。

### 工具层

工具层包括数据校验、调试面板、战斗沙盒、种子复现、日志导出、试玩指标导出和 Codex 可调用的自动化检查脚本。

## Godot 项目目标文件层级

根据 [[design/08_governance/2026-06-04_keep_godot_project_in_same_repo_game_subdir|Godot 工程与知识库同仓库并放入 game 子目录]]，第一版 MVP 阶段 Godot 工程与知识库保留在同一个 Git 仓库中，但 Godot 工程根目录固定为 `game/`。知识库根目录继续维护 `design/`、`inspiration/`、`insight/` 等文档目录，游戏实现相关文件放在 `game/` 下。

Godot 工程初始化后，后续创建文件应优先遵循以下目标层级。该层级用于让 Codex、开发者和 Godot 编辑器对同一类文件有稳定落点，避免规则、数据、场景和素材互相混杂。

```text
game/
  project.godot
  addons/
  assets/
    art/
      cards/
      characters/
      enemies/
      backgrounds/
      ui/
      vfx/
    audio/
      music/
      sfx/
    fonts/
    shaders/
  data/
    schemas/
    cards/
    relics/
    enemies/
    encounters/
    rewards/
    status/
    localization/
  scenes/
    main/
    battle/
    rewards/
    menus/
    debug/
  src/
    Domain/
      Cards/
      Combat/
      Effects/
      Enemies/
      Rewards/
      Runs/
    Application/
      Battle/
      Rewards/
      Runs/
    Infrastructure/
      Content/
      Localization/
      Logging/
      Randomness/
      Save/
    Presentation/
      Battle/
      Cards/
      Rewards/
      Menus/
      Debug/
    Tools/
  tests/
    Unit/
    Fixtures/
  tools/
    data_validator/
    exporters/
  logs/
```

### 目录职责

- `addons/`：第三方或自研 Godot 插件；新增插件前需要说明用途和维护成本。
- `assets/`：美术、音频、字体、Shader 和特效资源。临时占位资源也应放在对应子目录，保持尺寸和命名稳定。
- `data/`：运行时内容数据和 JSON Schema。卡牌、遗物、敌人、固定遭遇、奖励包、状态、文本本地化都应放在这里，不写死在场景节点中。
- `scenes/`：Godot `.tscn` 场景。战斗、奖励、菜单和调试场景分目录维护；场景只承载表现结构和节点引用，不承担核心规则结算。
- `src/Domain/`：不依赖 Godot 节点树的核心规则模型，例如卡牌、战斗状态、效果、敌人行为、奖励和 Run 状态。
- `src/Application/`：用例和服务编排，例如战斗流程、奖励选择、Run 推进和调试启动。
- `src/Infrastructure/`：数据加载、JSON 校验、本地化、日志、随机数、存档和平台适配。
- `src/Presentation/`：可挂接到 Godot 场景的 C# 节点脚本、ViewModel 和 UI 控制逻辑。
- `src/Tools/`：编辑器内调试面板、战斗沙盒入口和开发期工具代码。
- `tests/`：规则层单元测试、数据校验测试和测试夹具。
- `tools/`：可由命令行运行的外部工具，例如数据校验、数据导出或试玩日志汇总。
- `logs/`：本地调试日志和试玩导出文件，仅作为开发期输出目录；正式 Git 规则应避免提交个人日志。

### 创建文件准则

- C# 规则代码优先进入 `src/Domain/` 或 `src/Application/`，只有需要 Godot 节点生命周期、信号、资源或 UI 的脚本才进入 `src/Presentation/`。
- 内容参数优先进入 `data/`，不要为了调试方便把卡牌数值、敌人生命或奖励候选直接写进场景。
- 同一内容对象使用稳定 ID 连接 JSON 数据、文本键、素材键、日志和存档。
- Godot 场景使用 `PascalCase.tscn` 或语义清楚的稳定名称；内容 JSON 使用小写英文和下划线。
- C# 命名空间按目录层级组织，例如 `RoguelikeCardGame.Domain.Combat`、`RoguelikeCardGame.Presentation.Battle`。
- 新增文件如果同时影响设计理解，应回写对应 `design/` 文档或 [[design/08_governance/01_change_log|变更日志]]。
- 本节中的工程目录路径默认相对于 `game/`；例如 `src/Domain/` 指 `game/src/Domain/`。

## 关键系统需求

### 战斗结算

- 使用明确的结算阶段：回合开始、抽牌、恢复行动点、玩家行动、玩家回合结束、敌人行动、新回合准备。
- 每张牌的使用条件、目标规则、费用、连锁变化和牌区行为都要在结算前可预览。
- 结算结果应生成事件列表，供 UI、动画、音频、日志和测试共同消费。
- 终结牌高连锁效果必须在结算前能被 UI 判断是否已满足。

### 随机数与复现

- 每个 Run 保存初始随机种子。
- 奖励、抽牌洗牌、敌人行为池和后续地图生成应使用可追踪的随机流。
- 第一版 MVP 因固定遭遇和固定奖励池，随机需求较少，但仍应建立种子接口，避免后续重构。

### UI 与反馈

- 支持连锁层数当前值、3 / 5 / 8 阈值、超过 8 层后的继续计数。
- 支持卡牌高亮：可打出、费用不足、目标缺失、连锁不足、额外效果已满足。
- 支持终结牌预览：预计消耗连锁层数、预计伤害或资源收益、结算后剩余层数。
- 支持战斗日志面板或调试面板，便于定位规则误解和数值问题。
- 动画、音效和屏幕反馈应由结算事件驱动，避免表现层与规则层各算一套结果。

### 调试与测试

- 提供开发者调试菜单：进入指定战斗、设置生命 / 行动点 / 连锁层数、抽指定牌、加入指定遗物、打开指定卡牌包。
- 提供最小自动化测试：基础回合流程、抽弃牌重洗、行动牌 +1 连锁、技能牌默认不加连锁、终结牌消耗连锁、防御清空时点、奖励包选择。
- 提供本地试玩数据导出，支持 [[design/04_balance_data/02_metrics_and_playtest_data|指标与测试数据]] 中的 MVP 指标。

## 性能与稳定性目标

- 高频出牌、抽牌、状态结算和数字反馈不能出现明显卡顿。
- 第一版 MVP 以 2D 卡牌战斗为主，性能风险主要来自 UI 刷新、动画堆叠和日志量，而不是复杂物理。
- 动画速度、减少动画和音量分类必须可配置，服务 [[design/03_experience/03_onboarding_accessibility|引导与可访问性]]。
- 存档、配置和数据加载失败时必须有可读错误日志；可恢复错误不应直接导致玩家长期进度丢失。
- 随机生成或数据筛选失败时必须有保底方案；第一版 MVP 固定池也要校验候选数量。

## 首版暂不做

- Web 版导出。Godot 4 的 C# 项目目前不适合作为 Web 首发目标。
- 联机、排行榜、云存档、在线账号和在线遥测。
- 主机平台适配、移动平台适配和手柄优先 UI。
- 完整地图生成、事件、商店、休息节点、普通战斗随机池和完整货币经济。
- 大型编辑器插件或过早复杂化的内部工具平台。

## 已确认技术生产问题

本轮技术生产问题已在 [[inspiration/2026-06-03_06_technical_production_qa|06 Technical Production Q&A]] 中确认：首版以 Windows PC 为主，后续再考虑 macOS、Linux 支持；内容数据一致使用 JSON + JSON Schema；第一版 MVP 只需要节点级自动存档；只做本地试玩日志和统计导出，暂不需要在线遥测。

## 关联文档

- [[design/06_technical_production/01_data_pipeline_and_tools|数据管线与工具]]
- [[design/06_technical_production/02_save_config_platform|存档、配置与平台]]
- [[design/08_governance/2026-06-03_godot_csharp_codex_technical_stack|采用 Godot + C# 与 Codex 的技术生产方案]]
