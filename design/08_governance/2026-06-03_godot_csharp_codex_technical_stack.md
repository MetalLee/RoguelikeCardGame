# 决策：采用 Godot + C# 与 Codex 的技术生产方案

日期：2026-06-03  
状态：已接受  
影响范围：技术 / 制作 / 工具 / 平台

> 2026-06-13 更新：本技术栈决策仍然有效，但文中关于“连锁层数”的玩法对象应按 [[design/08_governance/2026-06-13_sword_black_tower_color_energy_core|确立《剑与黑塔》的彩能、色彩与武器卡池核心]] 更新理解为彩能、色彩、武器卡池和附魔系统。Godot + C#、JSON 数据管线、规则层与表现层分离的技术方向不变。

## 背景

开发者确认本项目计划使用 Godot + C# 作为技术栈，并使用 Codex 作为 coding agent。项目当前处于设计知识库和第一版 MVP 规划阶段，核心目标是验证 [[design/00_product/00_game_concept|卡组联动与 combo 爽感]]、[[design/01_core_gameplay/02_combat_system|连锁层数战斗机制]] 和 10-15 分钟线性 MVP。

当前项目推荐并已验证的 Godot 小版本为 4.6.3 stable。Godot 官方文档显示，C# 项目需要使用 .NET 版编辑器；Godot 4 C# 项目适合优先面向桌面平台，而 Web 不适合作为首版导出目标。

## 选项

- 方案 A：Godot + GDScript，降低 Godot 内部脚本门槛。
- 方案 B：Godot + C#，用强类型和 .NET 工具链支撑规则层、数据校验、测试和 Codex 协作。
- 方案 C：Unity + C#，获得成熟商业生态，但增加引擎切换和项目复杂度。

## 决策

采用方案 B：Godot 4.6.x .NET 版 + C# 作为首版技术栈，Codex 作为主要 coding agent 协助实现、校验、重构和文档维护。

首版开发和试玩平台以 PC 桌面为目标，Windows 优先；macOS / Linux 在垂直切片后评估；Web、移动和主机不进入第一版 MVP 范围。

## 理由

Godot 适合 2D 卡牌战斗、UI、动画和独立游戏制作节奏。C# 适合把卡牌、敌人、奖励、连锁层数、行动点、抽弃牌和存档等规则拆成可测试、可校验、可由 Codex 安全修改的结构。

本项目第一版 MVP 内容池很小，但后续会扩展大量卡牌、遗物、敌人和奖励池。C# 的类型系统、JSON 反序列化、单元测试和工具脚本有利于建立数据驱动管线，减少“内容写死在场景里”的长期风险。

Codex 适合维护 Markdown 知识库、生成数据结构、补充校验脚本和根据设计文档实现局部功能。为了让 Codex 的工作可控，技术生产文档需要明确数据格式、稳定 ID、校验规则、调试入口、测试范围和变更记录规则。

## 后续行动

- 在项目初始化后锁定 Godot 具体小版本、.NET SDK 要求和导出模板。
- 建立 JSON + JSON Schema 的首版数据管线。
- 实现战斗规则层与 Godot 表现层分离。
- 实现战斗沙盒、数据校验、结构化战斗日志和本地试玩指标导出。
- 根据 [[inspiration/2026-06-03_06_technical_production_qa|06 Technical Production Q&A]] 确认首发平台、数据编辑偏好、存档粒度和在线遥测需求。

## 外部参考

- [Godot C#/.NET 官方文档](https://docs.godotengine.org/en/4.6/tutorials/scripting/c_sharp/index.html)
- [Godot 4.6.3 stable 发布说明](https://godotengine.org/article/maintenance-release-godot-4-6-3/)
- [Godot Web 导出官方文档](https://docs.godotengine.org/en/4.6/tutorials/export/exporting_for_web.html)

## 关联文档

- [[design/06_technical_production/00_technical_requirements|技术需求]]
- [[design/06_technical_production/01_data_pipeline_and_tools|数据管线与工具]]
- [[design/06_technical_production/02_save_config_platform|存档、配置与平台]]
