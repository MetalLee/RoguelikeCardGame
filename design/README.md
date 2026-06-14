# Design Knowledge Base

本目录是 Roguelike 卡牌独立游戏的最高设计准则。后续开发、迭代、灵感整理、竞品洞察沉淀，均应优先对齐这里的文档；`inspiration/` 和 `insight/` 只作为输入来源，不直接覆盖已经确认的设计规范。

## 使用规则

- 新灵感进入 `inspiration/` 后，先提炼核心观点，再链接到对应设计文档。
- 新竞品洞察进入 `insight/` 后，先评估来源可靠性，再沉淀为可借鉴点、可规避点或待验证假设。
- 对玩法、数值、内容、体验或制作范围有实质影响的修改，必须记录到 [[design/08_governance/00_decision_log|决策记录]]。
- 发生设计变更时，在相关文档中更新当前准则，并在 [[design/08_governance/01_change_log|变更日志]] 留下摘要。
- 尚未确认的信息使用“待定”“假设”“待验证”标记，避免把想法误当成规则。

## 文档地图

### 00 Product

定义产品方向、玩家承诺、设计支柱、目标用户、范围边界和成功标准。

- [[design/00_product/00_game_concept|游戏概述]]
- [[design/00_product/01_design_pillars|设计支柱]]
- [[design/00_product/02_player_and_market|目标玩家与市场定位]]
- [[design/00_product/03_scope_and_success_criteria|项目范围与成功标准]]

### 01 Core Gameplay

定义玩家每回合、每场战斗、每局 Run、长期成长中的核心体验。

- [[design/01_core_gameplay/00_core_loop|核心循环]]
- [[design/01_core_gameplay/01_run_structure|单局结构]]
- [[design/01_core_gameplay/02_combat_system|战斗系统]]
- [[design/01_core_gameplay/03_card_system|卡牌系统]]
- [[design/01_core_gameplay/04_resource_economy|资源与经济]]
- [[design/01_core_gameplay/05_progression|成长与解锁]]

### 02 Content Systems

定义可生产、可扩展、可平衡的内容框架。

- [[design/02_content_systems/00_content_taxonomy|内容分类法]]
- [[design/02_content_systems/01_characters_and_archetypes|角色与构筑原型]]
- [[design/02_content_systems/02_enemies_and_bosses|魔物与 Boss]]
- [[design/02_content_systems/03_relics_items_rewards|遗物、道具与奖励]]
- [[design/02_content_systems/04_events_and_encounters|事件与遭遇]]
- [[design/02_content_systems/05_map_and_level_generation|地图与关卡生成]]

### 03 Experience

定义玩家如何理解、操作、感受游戏。

- [[design/03_experience/00_ui_ux|界面与交互]]
- [[design/03_experience/01_visual_direction|视觉方向]]
- [[design/03_experience/02_audio_feedback|音频与反馈]]
- [[design/03_experience/03_onboarding_accessibility|引导与可访问性]]

### 04 Balance Data

定义平衡原则、难度曲线、测试指标和数据验证方式。

- [[design/04_balance_data/00_balance_principles|平衡原则]]
- [[design/04_balance_data/01_difficulty_curve|难度曲线]]
- [[design/04_balance_data/02_metrics_and_playtest_data|指标与测试数据]]

### 05 Narrative World

定义世界观、叙事语气和内容表达边界。

- [[design/05_narrative_world/00_world_and_tone|世界观与语气]]
- [[design/05_narrative_world/01_story_delivery|叙事投放]]

### 06 Technical Production

定义技术约束、数据管线、工具链、存档和平台要求。

- [[design/06_technical_production/00_technical_requirements|技术需求]]
- [[design/06_technical_production/01_data_pipeline_and_tools|数据管线与工具]]
- [[design/06_technical_production/02_save_config_platform|存档、配置与平台]]

### 07 Production

定义里程碑、范围控制、风险管理和测试节奏。

- [[design/07_production/00_roadmap_milestones|开发路线图与里程碑]]
- [[design/07_production/01_scope_risk_backlog|范围、风险与待办池]]
- [[design/07_production/02_playtest_plan|试玩测试计划]]
- [[design/07_production/03_mvp_project_status|MVP 项目状态]]

### 08 Governance

定义知识库和设计决策如何被维护。

- [[design/08_governance/00_decision_log|决策记录]]
- [[design/08_governance/01_change_log|变更日志]]
- [[design/08_governance/02_glossary|术语表]]

## 模板

- [[design/_templates/design_doc_template|设计文档模板]]
- [[design/_templates/content_spec_template|内容规格模板]]
- [[design/_templates/decision_record_template|决策记录模板]]
