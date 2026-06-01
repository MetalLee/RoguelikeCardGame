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
- 统一术语为“终结卡”；确认连锁层数为回合内有条件保留资源，默认在回合结束或使用终结卡后归零，部分遗物或特殊终结卡可保留部分或全部层数。
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
- 根据 [[inspiration/2026-06-02_00_product_brainstorm_qa|00 Product 头脑风暴 Q&A]]，确认视觉基调为“暗黑哥特 + 手绘漫画”。
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
