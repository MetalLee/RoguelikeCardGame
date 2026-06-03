# 决策记录

状态：持续维护  
上级索引：[[design/README|Design Knowledge Base]]

## 目的

记录会影响游戏方向、系统规则、制作范围或长期维护成本的设计决策。决策记录用于解释“为什么这么做”，避免未来反复争论已经确认的问题。

## 记录规则

- 重要设计变更必须记录。
- 被废弃的方案也可以记录，因为它们解释了项目边界。
- 每条决策应链接到受影响的设计文档。
- 如决策被替代，应保留原记录并标注替代项。

## 决策索引

- [[design/08_governance/2026-06-02_combo_synergy_as_product_core|以卡组联动和 combo 爽感作为产品核心]]
- [[design/08_governance/2026-06-02_chain_count_as_combo_finisher_mechanic|以连锁层数作为 combo 爽感兑现机制]]
- [[design/08_governance/2026-06-03_godot_csharp_codex_technical_stack|采用 Godot + C# 与 Codex 的技术生产方案]]
- [[design/08_governance/2026-06-03_hand_drawn_comic_book_visual_direction|采用手绘漫画书风格作为主视觉方向]]
- [[design/08_governance/2026-06-04_keep_godot_project_in_same_repo_game_subdir|Godot 工程与知识库同仓库并放入 game 子目录]]

## 摘要表

| 日期 | 决策 | 状态 | 影响范围 | 关联文档 |
| --- | --- | --- | --- | --- |
| 2026-06-01 | 建立 design 目录作为最高设计准则 | 已接受 | 知识库治理 | [[design/README|Design Knowledge Base]] |
| 2026-06-02 | 以卡组联动和 combo 爽感作为产品核心 | 已接受 | 产品 / 玩法 / 内容 / 平衡 | [[design/00_product/00_game_concept|游戏概述]]、[[design/00_product/01_design_pillars|设计支柱]] |
| 2026-06-02 | 以连锁层数作为 combo 爽感兑现机制 | 已接受 | 战斗 / 卡牌 / 体验 / 平衡 | [[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/01_core_gameplay/03_card_system|卡牌系统]] |
| 2026-06-03 | 采用 Godot + C# 与 Codex 的技术生产方案 | 已接受 | 技术 / 制作 / 工具 / 平台 | [[design/06_technical_production/00_technical_requirements|技术需求]]、[[design/06_technical_production/01_data_pipeline_and_tools|数据管线与工具]]、[[design/06_technical_production/02_save_config_platform|存档、配置与平台]] |
| 2026-06-03 | 采用手绘漫画书风格作为主视觉方向 | 已接受 | 体验 / 视觉 / UI / 演出 | [[design/03_experience/01_visual_direction|视觉方向]]、[[design/03_experience/00_ui_ux|界面与交互]] |
| 2026-06-04 | Godot 工程与知识库同仓库并放入 game 子目录 | 已接受 | 仓库结构 / 技术 / 制作 / Codex 协作 | [[design/06_technical_production/00_technical_requirements|技术需求]]、[[design/06_technical_production/01_data_pipeline_and_tools|数据管线与工具]] |
