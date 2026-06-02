# 06 Technical Production Q&A

日期：2026-06-03  
状态：已确认  
来源：本轮 Codex 整理 [[design/06_technical_production/00_technical_requirements|技术需求]]、[[design/06_technical_production/01_data_pipeline_and_tools|数据管线与工具]]、[[design/06_technical_production/02_save_config_platform|存档、配置与平台]]

## 已确认

### Q1：本项目计划使用什么技术栈？

A：使用 Godot + C#。技术文档默认采用 Godot 4.6.x .NET 版 + C# 作为首版技术栈。

### Q2：本项目是否使用 Codex 参与开发？

A：使用 Codex 作为 coding agent。Codex 应优先读取 `design/` 中的设计规范，再修改代码、数据和文档；涉及玩法规则、制作范围或长期维护成本的变化，需要同步更新设计文档与治理记录。

### Q3：首版公开目标平台是否确认以 Windows PC 为主？

A：确认。目标平台以 Windows PC 为主，后续再考虑 macOS、Linux 支持。Web、移动和主机不进入第一版 MVP 范围。

### Q4：内容数据是否接受首版使用 JSON + JSON Schema？

A：确认。内容数据一致使用 JSON + JSON Schema；卡牌、敌人、遗物、奖励包和固定遭遇等内容数据都应进入统一 JSON 数据管线，并通过 JSON Schema 与引用校验保证内容安全。

### Q5：第一版 MVP 是否只需要节点级自动存档？

A：确认。第一版 MVP 只需要节点级自动存档，在 Run 开始、战斗开始、战斗胜利、奖励确认、Boss 通关和失败结算等节点保存；战斗中逐动作恢复留到后续阶段评估。

### Q6：公开试玩前是否需要在线遥测或云存档？

A：确认。第一版 MVP 只做本地试玩日志和统计导出，暂不需要在线遥测、云存档、账号或后台服务。

### Q7：Godot 项目文件应按什么目标层级创建？

A：已补充到 [[design/06_technical_production/00_technical_requirements|技术需求]]。后续创建 Godot 工程文件时，应优先遵循该文档中的“Godot 项目目标文件层级”，将核心规则 C# 代码、Godot 场景、JSON 内容数据、素材、工具、测试和本地日志分目录维护。

## 已同步设计文档

- [[design/06_technical_production/00_technical_requirements|技术需求]]
- [[design/06_technical_production/01_data_pipeline_and_tools|数据管线与工具]]
- [[design/06_technical_production/02_save_config_platform|存档、配置与平台]]
- [[design/08_governance/2026-06-03_godot_csharp_codex_technical_stack|采用 Godot + C# 与 Codex 的技术生产方案]]
