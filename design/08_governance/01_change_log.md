# 变更日志

状态：持续维护  
上级索引：[[design/README|Design Knowledge Base]]

## 目的

记录设计文档层面的变更摘要，帮助回溯项目方向如何演化。

## 记录规则

- 只记录会影响设计理解、制作范围或知识结构的变化。
- 普通错别字和格式微调不必记录。
- 每条记录应链接到受影响文档。

## 日志

### 2026-06-09

- 接入第一版 MVP 轻量动效：战斗界面根据 `CombatLogEvent` 播放出牌、伤害、防御、连锁提升、阈值达成、终结牌、敌人行动、抽弃牌和死亡反馈；奖励界面新增卡包打开、候选卡入场和选卡确认动效。该变更同步更新 [[design/03_experience/00_ui_ux|界面与交互]] 与 [[design/03_experience/01_visual_direction|视觉方向]]，明确当前阶段使用 Tween / VFX 方案，不扩展完整角色动作或动画时间轴。
- 调整战斗界面敌人呈现：敌人立绘不再包裹在大面积黑色面板中，改为透明舞台层叠加轻量状态条，并将敌人组整体右移以改善敌我构图；同步更新 [[design/03_experience/00_ui_ux|界面与交互]] 的敌人立绘承载规则。

### 2026-06-08

- 重构第一版 MVP 内容数据结构：将卡牌、敌人、遗物、遭遇、奖励包和 Run 序列迁移到 `game/data/gameplay/`，将卡牌 / 敌人 / 遗物 / 奖励包 view 与 asset manifest 迁移到 `game/data/presentation/`，并把内容 ID 统一为 `card.*`、`enemy.*`、`relic.*`、`reward_pack.*`、`encounter.*` 命名空间格式；同步更新 [[design/06_technical_production/01_data_pipeline_and_tools|数据管线与工具]]，明确规则数据不得包含表现字段。
- 将第一版 MVP 敌人与普通遗物的表现资源从 placeholder 迁移为 `asset.enemy.*.stand` / `asset.relic.*.icon` manifest 记录，并扩展 `game/tools/data_validator/validate_data.py`，要求表现 view 引用的 asset ID 必须存在且不能继续使用 placeholder；该变更补齐已归档美术资源与数据层表现索引的对应关系。
- 将第一版 MVP 现有背景、主角、卡牌模板、卡面插画、卡牌包、敌人、遗物、UI 图标和结算 VFX 接入 Godot 主流程；主菜单、战斗、奖励选择和结算界面通过 `game/data/presentation/` 查询资源，不再停留在无美术资源的纯占位 UI。同步更新 [[design/03_experience/00_ui_ux|界面与交互]] 与 [[design/03_experience/01_visual_direction|视觉方向]] 的运行时组合规则。

### 2026-06-06

- 归档终结牌“爆裂终结”“弧光横扫”“回流终结”“壁垒终结”的卡面插画到 `game/assets/art/cards/artwork/`，更新对应卡牌 `art_key`，并同步 [[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/03_experience/01_visual_direction|视觉方向]] 与 [[design/06_technical_production/01_data_pipeline_and_tools|数据管线与工具]] 的卡面资源索引；至此第一版 MVP 已录入的行动牌、技能牌和终结牌均具备独立卡面插画。
- 归档技能牌“基础防御”“战术观察”“回稳”“深度专注”“预备减费”的卡面插画到 `game/assets/art/cards/artwork/`，更新对应卡牌 `art_key`，并同步 [[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/03_experience/01_visual_direction|视觉方向]] 与 [[design/06_technical_production/01_data_pipeline_and_tools|数据管线与工具]] 的卡面资源索引。
- 归档行动牌“迅捷刺击”“重斩”“连段切击”“流步”“破防重击”的卡面插画到 `game/assets/art/cards/artwork/`，更新对应卡牌 `art_key`，并同步 [[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/03_experience/01_visual_direction|视觉方向]] 与 [[design/06_technical_production/01_data_pipeline_and_tools|数据管线与工具]] 的卡面资源索引。
- 归档行动牌“基础斩击”卡面插画到 `game/assets/art/cards/artwork/card_basic_strike.png`，并更新 `card_basic_strike` 的 `art_key` 为 `art.card.basic_strike`；同步更新 [[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/03_experience/01_visual_direction|视觉方向]] 与 [[design/06_technical_production/01_data_pipeline_and_tools|数据管线与工具]]，明确该资源会与行动牌模板在 Godot 中动态组合。
- 更新 [[design/06_technical_production/01_data_pipeline_and_tools|数据管线与工具]]，明确卡牌在 Godot 中优先由类型模板、卡面插画、数据文本和状态叠层动态组合展示，完整卡图预烘焙只作为后期可选缓存方案。
- 将第一版 MVP 关键元素视觉母版归档到 `game/assets/art/style_guides/mvp_key_elements_visual_master.png`，作为后续主角、敌人、卡牌、牌包、遗物和战斗 UI 素材生成的稳定视觉参考。
- 更新 [[design/03_experience/01_visual_direction|视觉方向]] 与 [[design/08_governance/2026-06-03_hand_drawn_comic_book_visual_direction|采用手绘漫画书风格作为主视觉方向]]，在关联部分嵌入该视觉母版，并明确母版中文字仅作为排版参考，正式运行时文本仍由 Godot UI 绘制。
- 更新 [[design/03_experience/01_visual_direction|视觉方向]]，明确剑客主角在使用卡牌时应根据卡牌效果展示不同动作，剑客动作资源统一放入 `game/assets/art/characters/swordsman/`；同时确认所有终结牌都需要独立美术资源，不能只复用通用模板或普通攻击动作。
- 生成并归档第一版 MVP 6 个敌人透明 PNG 立绘到 `game/assets/art/enemies/`，并更新 [[design/02_content_systems/02_enemies_and_bosses|敌人与 Boss]] 与 [[design/03_experience/01_visual_direction|视觉方向]]，将敌人 ID、战斗职责和美术资源建立稳定关联。
- 归档行动牌、技能牌和终结牌三类卡牌模板到 `game/assets/art/cards/templates/`，并更新 [[design/01_core_gameplay/03_card_system|卡牌系统]] 与 [[design/03_experience/01_visual_direction|视觉方向]]，明确模板只承载卡框、色彩和安全区，正式文本与数值仍由 Godot UI 绘制。
- 生成并归档第一版 MVP 战斗 VFX 到 `game/assets/art/vfx/`，覆盖普通斩击、重击、群体横扫、防御、连锁提升、3 / 5 / 8 层阈值达成、终结牌释放和敌人受击，并更新 [[design/03_experience/01_visual_direction|视觉方向]]、[[design/01_core_gameplay/02_combat_system|战斗系统]] 与 [[design/03_experience/02_audio_feedback|音频与反馈]] 建立反馈资源索引。
- 生成并归档第一版 MVP UI 图标、普通遗物图标和三类卡牌包素材，分别放入 `game/assets/art/ui/icons/`、`game/assets/art/relics/` 与 `game/assets/art/cards/packs/`；更新 [[design/03_experience/00_ui_ux|界面与交互]]、[[design/02_content_systems/03_relics_items_rewards|遗物、道具与奖励]]、[[design/01_core_gameplay/03_card_system|卡牌系统]] 与 [[design/03_experience/01_visual_direction|视觉方向]]，建立图标、遗物、卡包与设计职责的稳定关联。

### 2026-06-05

- 根据第一版 MVP 开发推进，新增 `game/src/Infrastructure/Content/` 内容加载层，将 `game/data/` 中卡牌、敌人、遭遇、奖励包、遗物、本地化和 MVP Run 序列加载为现有规则层模型，保持内容数据驱动，不把数值写死在 Godot 场景中。
- 更新 `game/src/Presentation/Menus/MainMenu.cs`，将 Godot 主场景接入最小可玩闭环：主菜单 / 开始 MVP -> 6 场固定战斗 -> 战后卡牌包 -> 精英固定普通遗物 -> Boss 通关或失败 -> 重开。
- 本轮实现覆盖 [[design/03_experience/00_ui_ux|界面与交互]] 中的 MVP 级信息表达：玩家生命、防御、行动点、连锁层数、抽弃牌数量、敌人生命、敌人意图、手牌、无法出牌原因提示和最近结算日志。
- 补充 `game/README.md` 的团队通用运行环境说明与验证命令，记录项目依赖 Godot 4.6.x .NET 版和 .NET SDK 8.0.x，并验证 `dotnet build`、规则层 smoke test、数据校验和 Godot headless 项目加载通过。

### 2026-06-04

- 根据开发者关于 Godot 工程与知识库是否同仓库的确认，决定第一版 MVP 阶段采用同一个 Git 仓库，但将 Godot 工程固定放入 `game/` 子目录，避免工程文件与 Obsidian 知识库根目录混杂。
- 新增 [[design/08_governance/2026-06-04_keep_godot_project_in_same_repo_game_subdir|Godot 工程与知识库同仓库并放入 game 子目录]] 决策记录，并更新 [[design/08_governance/00_decision_log|决策记录]]。
- 更新 [[design/06_technical_production/00_technical_requirements|技术需求]]，将 Godot 项目目标文件层级从仓库根目录调整为 `game/` 工程根目录。

### 2026-06-03

- 根据 [[inspiration/2026-06-03_03_experience_mvp_visual_qa|03 Experience MVP Visual Q&A]]，确认主视觉方向调整为“手绘漫画书风格”：整体像可交互漫画，战斗演出使用漫画分镜、速度线、冲击格、局部特写和拟声字。
- 根据开发者对第一张关键图片的进一步要求，确认视觉母版需要包含：简洁漫画剑客主角、简洁漫画恶魔敌人、血红行动牌、冷青 / 幽蓝技能牌、紫黑终结牌、三类卡牌包，以及简单偏 Q 版漫画遗物。
- 更新 [[design/03_experience/01_visual_direction|视觉方向]]、[[design/03_experience/00_ui_ux|界面与交互]]、[[design/00_product/02_player_and_market|目标玩家与市场定位]] 与 [[design/05_narrative_world/00_world_and_tone|世界观与语气]]，并新增 [[design/08_governance/2026-06-03_hand_drawn_comic_book_visual_direction|采用手绘漫画书风格作为主视觉方向]] 决策记录。
- 根据开发者对 [[inspiration/2026-06-03_06_technical_production_qa|06 Technical Production Q&A]] 的回答，确认首版目标平台以 Windows PC 为主，后续再考虑 macOS、Linux 支持；内容数据一致使用 JSON + JSON Schema；第一版 MVP 只需要节点级自动存档；只做本地试玩日志和统计导出，暂不需要在线遥测。
- 更新 [[design/06_technical_production/00_technical_requirements|技术需求]]、[[design/06_technical_production/01_data_pipeline_and_tools|数据管线与工具]]、[[design/06_technical_production/02_save_config_platform|存档、配置与平台]] 与 [[inspiration/2026-06-03_06_technical_production_qa|06 Technical Production Q&A]]，将待确认项收束为已确认，并在技术需求中新增 Godot 项目目标文件层级，作为后续 Godot 开发创建文件的准则。
- 根据本轮技术生产整理，确认首版技术栈采用 Godot 4.6.x .NET 版 + C#，并使用 Codex 作为 coding agent 协助实现、校验、重构和文档维护。
- 更新 [[design/06_technical_production/00_technical_requirements|技术需求]]、[[design/06_technical_production/01_data_pipeline_and_tools|数据管线与工具]] 与 [[design/06_technical_production/02_save_config_platform|存档、配置与平台]]，补充技术定位、MVP 必备系统、规则层 / 表现层 / 数据层分工、JSON 数据管线、调试工具、存档策略、设置项和 PC 桌面优先平台目标。
- 新增 [[design/08_governance/2026-06-03_godot_csharp_codex_technical_stack|采用 Godot + C# 与 Codex 的技术生产方案]] 决策记录，并更新 [[design/08_governance/00_decision_log|决策记录]]。
- 将待开发者确认的问题整理到 [[inspiration/2026-06-03_06_technical_production_qa|06 Technical Production Q&A]]，包括首版平台、数据编辑格式、存档粒度和在线遥测 / 云存档需求。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认基础行动点上限为 3，遗物或技能可以提高上限；每回合恢复行动点数为当前行动点上限 + 额外行动点数，当前行动点可被效果继续增加。
- 更新 [[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]]、[[design/08_governance/02_glossary|术语表]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，补充行动点上限、当前回合行动点增加和下回合额外行动点的区别。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认默认牌库循环规则：抽牌堆不足时，将弃牌堆洗回抽牌堆继续抽；普通卡牌可在同一场战斗中多轮循环使用，部分卡牌可强制重洗弃牌堆。
- 更新 [[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]]、[[design/08_governance/02_glossary|术语表]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，补充抽牌堆、弃牌堆、重洗和强制重洗的基础定义，并将消耗牌、临时牌和特殊牌区留到后续卡牌模板阶段。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认普通战斗后的基础卡牌奖励为三类卡牌包：行动牌包、技能牌包、终结牌包；玩家选择 1 包打开后，从 3 张同类型候选卡牌中选择 0-3 张加入卡组。
- 更新 [[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/01_run_structure|单局结构]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]]、[[design/01_core_gameplay/05_progression|成长与解锁]] 与 [[design/08_governance/02_glossary|术语表]]，补充卡牌包奖励、卡组厚度控制和定向补强规则。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认普通战斗卡牌包选择前只显示类型：行动牌包、技能牌包或终结牌包，不预览具体卡牌。
- 更新 [[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/01_run_structure|单局结构]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]]、[[design/01_core_gameplay/05_progression|成长与解锁]] 与 [[design/08_governance/02_glossary|术语表]]，补充卡牌包选择前信息规则，并将待定义项收束为包内稀有度、节点质量和开包反馈。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认普通战斗卡牌包采用保底 + 阶段递进质量规则：每包至少 1 张高于普通稀有度的卡牌，早期以普通牌为主，中后期提高高稀有卡牌出现概率。
- 更新 [[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/01_run_structure|单局结构]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]]、[[design/01_core_gameplay/05_progression|成长与解锁]]、[[design/04_balance_data/00_balance_principles|平衡原则]] 与 [[design/08_governance/02_glossary|术语表]]，补充卡牌包质量保底、阶段递进和后续概率测试项。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认普通战斗卡牌包少拿或不拿默认没有额外补偿；跳过收益待商店、货币和删牌经济确定后再评估。
- 更新 [[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]]、[[design/01_core_gameplay/05_progression|成长与解锁]] 与 [[design/04_balance_data/00_balance_principles|平衡原则]]，补充少拿 / 不拿卡牌的无补偿基础规则和后续经济验证项。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认精英战斗除卡牌包外额外随机给 1 个普通遗物；Boss 战斗给稀有度更高的卡牌包，并额外给 1 个能够影响构筑、改变玩法的特殊遗物。
- 更新 [[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/01_run_structure|单局结构]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]]、[[design/01_core_gameplay/05_progression|成长与解锁]] 与 [[design/02_content_systems/03_relics_items_rewards|遗物、道具与奖励]]，补充普通战斗、精英战斗和 Boss 战斗的奖励层级差异。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认 Boss 特殊遗物采用三选一：Boss 提供 3 个特殊遗物候选，玩家选择 1 个获得。
- 更新 [[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/01_run_structure|单局结构]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]]、[[design/01_core_gameplay/05_progression|成长与解锁]]、[[design/02_content_systems/03_relics_items_rewards|遗物、道具与奖励]] 与 [[design/08_governance/02_glossary|术语表]]，补充 Boss 特殊遗物三选一和普通遗物 / 特殊遗物术语边界。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认第一版 MVP 采用 1 个线性章节：连续战斗 + 奖励选择 + 1 个精英 + 1 个 Boss，目标时长约 10-15 分钟，暂不做路线图、事件、商店、休息或完整经济。
- 更新 [[design/00_product/03_scope_and_success_criteria|项目范围与成功标准]]、[[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/01_run_structure|单局结构]]、[[design/02_content_systems/05_map_and_level_generation|地图与关卡生成]]、[[design/04_balance_data/02_metrics_and_playtest_data|指标与测试数据]]、[[design/07_production/00_roadmap_milestones|开发路线图与里程碑]] 与 [[design/07_production/02_playtest_plan|试玩测试计划]]，区分第一版 MVP 战斗验证范围和后续完整垂直切片范围。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认第一版 MVP 的固定战斗节点顺序为 3 场普通战斗 + 1 场精英战斗 + 1 场普通战斗 + 1 场 Boss 战斗。
- 更新 [[design/00_product/03_scope_and_success_criteria|项目范围与成功标准]]、[[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/01_run_structure|单局结构]]、[[design/02_content_systems/02_enemies_and_bosses|敌人与 Boss]]、[[design/02_content_systems/05_map_and_level_generation|地图与关卡生成]]、[[design/04_balance_data/01_difficulty_curve|难度曲线]]、[[design/04_balance_data/02_metrics_and_playtest_data|指标与测试数据]]、[[design/07_production/00_roadmap_milestones|开发路线图与里程碑]] 与 [[design/07_production/02_playtest_plan|试玩测试计划]]，补充 MVP 六场战斗的顺序和压力节奏。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认第一版 MVP 的 6 场战斗全部采用固定遭遇；普通战斗随机池等战斗循环成立后再加入。
- 更新 [[design/00_product/03_scope_and_success_criteria|项目范围与成功标准]]、[[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/01_run_structure|单局结构]]、[[design/02_content_systems/02_enemies_and_bosses|敌人与 Boss]]、[[design/02_content_systems/05_map_and_level_generation|地图与关卡生成]]、[[design/04_balance_data/01_difficulty_curve|难度曲线]]、[[design/04_balance_data/02_metrics_and_playtest_data|指标与测试数据]]、[[design/07_production/00_roadmap_milestones|开发路线图与里程碑]] 与 [[design/07_production/02_playtest_plan|试玩测试计划]]，补充固定遭遇、后置普通战斗随机池和逐节点测试记录要求。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认第一版 MVP 六场固定战斗采用教学递进型职责分配：普通 1 教行动点 / 出牌 / 连锁，普通 2 教敌人意图与防御，普通 3 教多敌人与目标选择，精英检验攻防节奏，普通 4 试用遗物和新卡，Boss 综合检验；前两次奖励应引导玩家有机会拿到群体攻击终结牌。
- 更新 [[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/01_run_structure|单局结构]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/02_content_systems/02_enemies_and_bosses|敌人与 Boss]]、[[design/04_balance_data/01_difficulty_curve|难度曲线]]、[[design/04_balance_data/02_metrics_and_playtest_data|指标与测试数据]] 与 [[design/07_production/02_playtest_plan|试玩测试计划]]，补充六场固定战斗的教学职责和群体攻击终结牌奖励引导需求。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认第一版 MVP 前两次普通战斗奖励采用固定保底、不明牌的方式引导群体攻击终结牌：普通战斗 1 后和普通战斗 2 后的终结牌包都固定包含 1 张群体攻击终结牌，玩家仍需选择终结牌包才能看到并获得；MVP 不做动态触发、首次选择追踪、额外去重或补偿逻辑。
- 更新 [[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/01_run_structure|单局结构]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/02_content_systems/02_enemies_and_bosses|敌人与 Boss]]、[[design/04_balance_data/01_difficulty_curve|难度曲线]]、[[design/04_balance_data/02_metrics_and_playtest_data|指标与测试数据]] 与 [[design/07_production/02_playtest_plan|试玩测试计划]]，补充前两次终结牌包均保底群体攻击终结牌、不明牌原则和不做复杂保底逻辑的 MVP 约束。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认第一版 MVP 每场战斗开始时生命回满，用于稳定测试单场战斗；正式版本方向为生命跨战斗保留且不自动恢复，但本轮不展开正式版本恢复节点和恢复经济。
- 更新 [[design/00_product/03_scope_and_success_criteria|项目范围与成功标准]]、[[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/01_run_structure|单局结构]]、[[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]]、[[design/04_balance_data/01_difficulty_curve|难度曲线]]、[[design/04_balance_data/02_metrics_and_playtest_data|指标与测试数据]] 与 [[design/07_production/02_playtest_plan|试玩测试计划]]，区分 MVP 每战回满的测试规则和正式版本跨战斗生命保留方向。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认第一版 MVP 中生命归零会立即结束本次 MVP Run，不提供当前战斗重试，以保证 Roguelike 体验一致。
- 更新 [[design/00_product/03_scope_and_success_criteria|项目范围与成功标准]]、[[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/01_run_structure|单局结构]]、[[design/04_balance_data/02_metrics_and_playtest_data|指标与测试数据]] 与 [[design/07_production/02_playtest_plan|试玩测试计划]]，补充 MVP 失败结算和测试记录要求。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认第一版 MVP 中 Boss 击败即胜利，显示 MVP 通关并提供重开入口；第一版 MVP 不做 Boss 战后奖励、局外奖励或解锁。
- 更新 [[design/00_product/03_scope_and_success_criteria|项目范围与成功标准]]、[[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/01_run_structure|单局结构]]、[[design/01_core_gameplay/05_progression|成长与解锁]]、[[design/02_content_systems/03_relics_items_rewards|遗物、道具与奖励]]、[[design/04_balance_data/02_metrics_and_playtest_data|指标与测试数据]] 与 [[design/07_production/02_playtest_plan|试玩测试计划]]，区分第一版 MVP 终点 Boss 的通关结算和后续完整 Run 阶段性 Boss 的奖励规则。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认第一版 MVP 内容策略采用小型教学池：围绕 6 场固定战斗制作小规模卡牌 / 遗物 / 敌人池，暂不追求流派完整度。
- 更新 [[design/00_product/03_scope_and_success_criteria|项目范围与成功标准]]、[[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/01_run_structure|单局结构]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/02_content_systems/00_content_taxonomy|内容分类法]]、[[design/02_content_systems/02_enemies_and_bosses|敌人与 Boss]]、[[design/02_content_systems/03_relics_items_rewards|遗物、道具与奖励]]、[[design/07_production/00_roadmap_milestones|开发路线图与里程碑]] 与 [[design/07_production/02_playtest_plan|试玩测试计划]]，补充小型教学池边界，并移除第一版 MVP 对 Boss 特殊遗物三选一的测试残留。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认第一版 MVP 小型教学池的数量边界：初始牌组 10 张，奖励牌约 9 张（行动牌、技能牌、终结牌各 3 张），普通遗物 1 个，6 场固定遭遇只做必要的 6 组敌人。
- 更新 [[design/00_product/03_scope_and_success_criteria|项目范围与成功标准]]、[[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/01_run_structure|单局结构]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/02_content_systems/00_content_taxonomy|内容分类法]]、[[design/02_content_systems/02_enemies_and_bosses|敌人与 Boss]]、[[design/02_content_systems/03_relics_items_rewards|遗物、道具与奖励]]、[[design/07_production/00_roadmap_milestones|开发路线图与里程碑]] 与 [[design/07_production/02_playtest_plan|试玩测试计划]]，补充极简内容数量，并将第一版 MVP 精英普通遗物奖励标记为实际固定获得。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认第一版 MVP 的 9 张奖励牌采用固定可重复池：每次打开某类型卡牌包，都显示该类型固定 3 张奖励牌；拿过的牌仍可再次出现，玩家可以重复加入同名牌。
- 更新 [[design/00_product/03_scope_and_success_criteria|项目范围与成功标准]]、[[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/01_run_structure|单局结构]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]]、[[design/01_core_gameplay/05_progression|成长与解锁]]、[[design/02_content_systems/00_content_taxonomy|内容分类法]]、[[design/07_production/00_roadmap_milestones|开发路线图与里程碑]]、[[design/07_production/02_playtest_plan|试玩测试计划]] 与 [[design/08_governance/02_glossary|术语表]]，补充固定可重复奖励池，并明确第一版 MVP 不做奖励牌移除、同名牌去重、候选不足补位或节点固定包差异。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认第一版 MVP 的 9 张奖励牌采用教学覆盖型功能分配：行动牌覆盖低费连锁、稳定输出、高费强打；技能牌覆盖防御 / 回费、过牌、减费或检索；终结牌覆盖群体攻击、资源返还、额外防御或控制。
- 更新 [[design/00_product/03_scope_and_success_criteria|项目范围与成功标准]]、[[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/01_run_structure|单局结构]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/02_content_systems/00_content_taxonomy|内容分类法]]、[[design/02_content_systems/02_enemies_and_bosses|敌人与 Boss]]、[[design/02_content_systems/03_relics_items_rewards|遗物、道具与奖励]]、[[design/07_production/00_roadmap_milestones|开发路线图与里程碑]] 与 [[design/07_production/02_playtest_plan|试玩测试计划]]，补充第一版 MVP 内容收束结论：本轮不继续确认具体 9 张牌、1 个普通遗物和 6 组敌人内容，它们进入 MVP 开发过程中按已定槽位和职责落地。

### 2026-06-02

- 根据 [[inspiration/2026-06-02_00_product_brainstorm_qa|00 Product 头脑风暴 Q&A]]，确认产品核心方向为“卡组联动与 combo 爽感”。
- 更新 [[design/00_product/00_game_concept|游戏概述]] 的一句话概念、玩家承诺和产品边界，使其强调构筑联动而非孤立单卡强度。
- 更新 [[design/00_product/01_design_pillars|设计支柱]]，将首要支柱调整为“每局都能形成可读的联动引擎”，并新增“Combo 爽感来自玩家构筑”。
- 更新 [[design/00_product/02_player_and_market|目标玩家与市场定位]] 与 [[design/00_product/03_scope_and_success_criteria|项目范围与成功标准]]，补充 combo 构筑玩家画像和垂直切片验证标准。
- 新增 [[design/08_governance/2026-06-02_combo_synergy_as_product_core|以卡组联动和 combo 爽感作为产品核心]] 决策记录。
- 根据 [[inspiration/2026-06-02_00_product_brainstorm_qa|00 Product 头脑风暴 Q&A]]，确认 combo 爽感的主要兑现机制为“打出卡牌积累连锁层数，并在高连锁下使用终结卡触发额外强力效果”。
- 更新 [[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/03_experience/00_ui_ux|界面与交互]]、[[design/03_experience/01_visual_direction|视觉方向]] 与 [[design/04_balance_data/00_balance_principles|平衡原则]]，补充连锁层数、终结卡、UI 反馈、动画表现和平衡约束。
- 更新 [[design/08_governance/02_glossary|术语表]]，新增“连锁层数”和“终结卡”。
- 新增 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|以连锁层数作为 combo 爽感兑现机制]] 决策记录。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认卡牌类型分为行动牌、技能牌和终结牌：行动牌默认 +1 连锁层数，技能牌默认不产生连锁层数，终结牌默认消耗所有当前连锁层数。
- 更新 [[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/08_governance/02_glossary|术语表]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，补充三类卡牌的默认连锁规则，并记录少数卡牌和遗物可以通过明确效果突破默认规则。
- 核心玩法术语统一为“终结牌”；产品阶段历史文档中的“终结卡”指同一概念。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认三类卡牌的战斗职责：行动牌负责攻击和叠连锁并默认消耗行动点；技能牌负责防御、抽牌、检索、减费、回费、状态、控制或调整牌序；终结牌负责消耗连锁并产生爆发效果，默认不消耗行动点。
- 更新 [[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]]、[[design/08_governance/02_glossary|术语表]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，补充行动点与三类卡牌职责的关系。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认行动点费用规则：行动牌消耗行动点，技能牌和终结牌默认不消耗行动点；行动点每回合固定恢复，并可通过技能牌、终结牌或遗物进行回费、减费、临时行动点增加。
- 更新 [[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]]、[[design/08_governance/02_glossary|术语表]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，补充行动点恢复和行动点修正效果。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认默认回合结束结算：未用行动点清空、连锁层数清空、未使用手牌弃掉；防御在玩家回合结束时不清空，而是在敌人行动后、下一回合开始时清空剩余值。
- 更新 [[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]]、[[design/08_governance/02_glossary|术语表]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，补充回合结算时点，以及技能牌、终结牌或遗物可保留手牌、防御、行动点或连锁层数的例外规则。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认基础回合参数为每回合恢复 3 点行动点、抽 5 张牌；牌组构筑完善后，技能牌和终结牌可通过回费、过牌提高卡组运转能力。
- 更新 [[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]]、[[design/08_governance/02_glossary|术语表]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，补充 3 行动点 / 5 抽牌基准以及过牌关键词候选。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认初始牌组为 10 张牌：6 张行动牌、2 张基础防御技能牌、1 张过牌技能牌、1 张终结牌。
- 更新 [[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，补充初始牌组结构和第一局教学链路。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认终结牌虽然默认不消耗行动点，但需要满足自身连锁层数使用条件后才能打出；初始牌组中的终结牌至少需要 3 层连锁才能打出，并在 5 层连锁及以上时获得更强效果。
- 更新 [[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，补充终结牌使用条件和初始终结牌教学模板。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认初始牌组行动牌费用以 1 费为主、少量 0 费；行动牌费用与连锁增长分离，费用影响伤害、回合节奏和行动点机会成本，但默认每打出 1 张行动牌只增加 1 层连锁。
- 更新 [[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，补充行动牌费用基准和 0 费行动牌强度约束。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认初始牌组 6 张行动牌的费用与功能分布为：4 张 1 费普通攻击、1 张 0 费弱攻击、1 张 2 费强攻击，用于教学费用影响伤害和节奏但不影响默认连锁增长。
- 更新 [[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，补充初始行动牌强弱对比结构。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认初始 2 张防御技能牌提供防御值并 +1 行动点，初始 1 张过牌技能牌抽 2 张牌。
- 更新 [[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，补充初始技能牌模板和技能牌辅助连锁延展的定位。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认初始终结牌定位为单体爆发：3 层造成高额单体伤害，5 层连锁及以上造成翻倍伤害；其他特殊效果终结牌通过闯关奖励逐步获得。
- 更新 [[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，补充初始终结牌单体爆发模板和特殊终结牌投放边界。
- 根据 [[inspiration/2026-06-02_01_core_gameplay_brainstorm_qa|01 Core Gameplay 头脑风暴 Q&A]]，确认初始卡组采用保守数值风格，具体伤害值、防御值和终结牌基础伤害留到最小原型阶段统一设定。
- 更新 [[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，将初始卡牌具体数值标记为原型阶段验证项。
- 根据 [[inspiration/2026-06-02_00_product_brainstorm_qa|00 Product 头脑风暴 Q&A]]，确认终结卡密度为自由构筑中的风险收益取舍：前期推荐 1-2 张，中后期可通过过牌、减费、回费、复制或连锁保留能力支撑更多终结卡，并允许终结卡之间形成联动流派。
- 更新 [[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]]、[[design/04_balance_data/00_balance_principles|平衡原则]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，补充终结卡密度、卡手风险和过牌能力的关系。
- 根据 [[inspiration/2026-06-02_00_product_brainstorm_qa|00 Product 头脑风暴 Q&A]]，确认终结卡效果空间可覆盖伤害、暴击、群体伤害、连锁保留、抽牌、敌人异常状态、自身增益和秒杀非 Boss 敌人；数值可使用固定值、连锁加算或连锁乘算。
- 更新 [[design/00_product/03_scope_and_success_criteria|项目范围与成功标准]]、[[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/04_balance_data/00_balance_principles|平衡原则]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，明确终结卡效果空间后续可逐步增加卡池深度和复杂效果。
- 根据 [[inspiration/2026-06-02_00_product_brainstorm_qa|00 Product 头脑风暴 Q&A]]，记录基础终结卡效果候选为单体高伤害、群体伤害、暴击率提升、抽牌 / 回费；该集合不作为首版内容承诺。
- 更新 [[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/01_core_gameplay/04_resource_economy|资源与经济]]、[[design/04_balance_data/00_balance_principles|平衡原则]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，将四类基础效果记录为候选验证方向。
- 根据 [[inspiration/2026-06-02_00_product_brainstorm_qa|00 Product 头脑风暴 Q&A]]，确认连锁阈值基准采用 3 / 5 / 8 三档：3 层用于理解机制，5 层用于续航连锁，8 层用于成型卡组的高爆发。
- 更新 [[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/03_experience/00_ui_ux|界面与交互]]、[[design/03_experience/03_onboarding_accessibility|引导与可访问性]]、[[design/04_balance_data/00_balance_principles|平衡原则]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，补充连锁阈值梯度和学习节奏。
- 根据 [[inspiration/2026-06-02_00_product_brainstorm_qa|00 Product 头脑风暴 Q&A]]，确认连锁层数不设置硬上限；3 / 5 / 8 是关键阈值而非上限。首版具体内容暂缓，待其他设计维度确认后再定。
- 更新 [[design/00_product/03_scope_and_success_criteria|项目范围与成功标准]]、[[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/03_card_system|卡牌系统]]、[[design/03_experience/00_ui_ux|界面与交互]]、[[design/04_balance_data/00_balance_principles|平衡原则]] 与 [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|连锁层数决策记录]]，移除首版具体效果承诺并补充超过 8 层后的待定义项。
- 根据 [[inspiration/2026-06-02_00_product_brainstorm_qa|00 Product 头脑风暴 Q&A]]，确认核心目标玩家为喜欢单局随机性、卡组构筑和爽快 combo 的卡牌玩家，并强调数值反馈与画面效果反馈。
- 更新 [[design/00_product/00_game_concept|游戏概述]]、[[design/00_product/02_player_and_market|目标玩家与市场定位]]、[[design/03_experience/00_ui_ux|界面与交互]] 与 [[design/03_experience/01_visual_direction|视觉方向]]，补充目标玩家画像和 combo 兑现反馈要求。
- 根据 [[inspiration/2026-06-02_00_product_brainstorm_qa|00 Product 头脑风暴 Q&A]]，确认非目标体验包括纯剧情体验、纯数值刷装、重度动作操作、低决策自动战斗和只看演出不研究构筑。
- 更新 [[design/00_product/00_game_concept|游戏概述]]、[[design/00_product/02_player_and_market|目标玩家与市场定位]]、[[design/00_product/03_scope_and_success_criteria|项目范围与成功标准]]、[[design/02_content_systems/03_relics_items_rewards|遗物、道具与奖励]] 与 [[design/05_narrative_world/00_world_and_tone|世界观与语气]]，补充剧情、装备系统、动作操作和纯观演体验边界。
- 根据 [[inspiration/2026-06-02_00_product_brainstorm_qa|00 Product 头脑风暴 Q&A]]，确认竞品参照为《杀戮尖塔》和《吸血鬼爬行者》，差异化重点为画面风格、连锁机制、打击感和动画演出爽感。
- 更新 [[design/00_product/02_player_and_market|目标玩家与市场定位]]、[[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/03_experience/01_visual_direction|视觉方向]] 与 [[design/03_experience/02_audio_feedback|音频与反馈]]，补充竞品参照和表现层差异化要求。
- 根据 [[inspiration/2026-06-02_00_product_brainstorm_qa|00 Product 头脑风暴 Q&A]]，确认过早期视觉基调；该表述已被 2026-06-03 的手绘漫画书风格决策替代。
- 更新 [[design/00_product/02_player_and_market|目标玩家与市场定位]]、[[design/03_experience/01_visual_direction|视觉方向]] 与 [[design/05_narrative_world/00_world_and_tone|世界观与语气]]，补充画面风格差异化和氛围边界。
- 根据 [[inspiration/2026-06-02_00_product_brainstorm_qa|00 Product 头脑风暴 Q&A]]，确认产品成功标准优先级：高层数连锁效果与演出爽感、玩家社区分享流派、反复游玩深度。
- 更新 [[design/00_product/03_scope_and_success_criteria|项目范围与成功标准]]、[[design/04_balance_data/02_metrics_and_playtest_data|指标与测试数据]] 与 [[design/07_production/02_playtest_plan|试玩测试计划]]，补充成功标准优先级、观察问题和测试记录项。
- 根据 [[inspiration/2026-06-02_00_product_brainstorm_qa|00 Product 头脑风暴 Q&A]]，确认产品取舍优先级为：连锁 combo 爽感 > 画面演出 > 内容数量 > 构筑深度 > 平衡严谨度 > 世界观氛围。
- 更新 [[design/00_product/00_game_concept|游戏概述]]、[[design/00_product/01_design_pillars|设计支柱]]、[[design/00_product/03_scope_and_success_criteria|项目范围与成功标准]]、[[design/03_experience/01_visual_direction|视觉方向]] 与 [[design/04_balance_data/00_balance_principles|平衡原则]]，补充资源冲突时的取舍顺序。
- 根据 [[inspiration/2026-06-02_00_product_brainstorm_qa|00 Product 头脑风暴 Q&A]]，确认单局目标时长为 30-40 分钟。
- 更新 [[design/01_core_gameplay/00_core_loop|核心循环]]、[[design/01_core_gameplay/01_run_structure|单局结构]] 与 [[design/04_balance_data/02_metrics_and_playtest_data|指标与测试数据]]，补充单局节奏目标和测试观察项。

### 2026-06-01

- 建立 `design/` 下的产品、核心玩法、内容系统、体验、平衡数据、叙事世界、技术生产、制作管理和治理文档结构。
- 新增通用设计文档、内容规格和决策记录模板。
- 将 [[design/README|Design Knowledge Base]] 设为设计文档索引。
