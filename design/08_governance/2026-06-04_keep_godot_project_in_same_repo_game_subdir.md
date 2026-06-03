# 决策：Godot 工程与知识库同仓库并放入 game 子目录

日期：2026-06-04  
状态：已接受  
影响范围：仓库结构 / 技术 / 制作 / Codex 协作

## 背景

本仓库当前是 Obsidian 友好的游戏设计知识库，并即将进入第一版 MVP 的 Godot + C# 工程开发阶段。开发者需要决定 Godot 工程应与知识库放在同一个 Git 仓库，还是另起独立 Git 仓库。

项目开发方式依赖 Codex 先读取 `design/` 中的设计规范，再修改游戏代码、数据和文档。如果游戏工程与知识库完全分离，Codex 在每次实现时需要跨仓库获取上下文，代码变更与设计文档变更也更难在同一次提交中对应起来。

同时，Godot 工程会产生 `project.godot`、导入缓存、场景、资源、C# 工程文件、运行日志和构建输出。如果直接放在知识库根目录，会让 Obsidian 知识结构与工程结构混杂，降低文档浏览和维护的清晰度。

## 选项

- 方案 A：Godot 工程直接放在知识库根目录。
- 方案 B：Godot 工程与知识库同一个 Git 仓库，但放入独立 `game/` 子目录。
- 方案 C：Godot 工程另起独立 Git 仓库，知识库保持单独仓库。

## 决策

采用方案 B：第一版 MVP 阶段，Godot 工程与本知识库保留在同一个 Git 仓库中，但 Godot 工程根目录固定为 `game/`，即 `game/project.godot`。

知识库根目录继续保留 `AGENTS.md`、`README.md`、`design/`、`inspiration/` 和 `insight/`。游戏实现相关的 Godot 工程文件、运行时数据、场景、C# 源码、测试、工具和本地日志应放在 `game/` 下。

推荐目标结构：

```text
RoguelikeCardGame/
  AGENTS.md
  README.md
  design/
  inspiration/
  insight/
  game/
    project.godot
    assets/
    data/
    scenes/
    src/
    tests/
    tools/
    logs/
```

## 理由

同仓库能让 Codex 在实现功能前直接读取 `design/`，并把代码、数据与设计文档更新放在同一次变更中，适合第一版 MVP 的快速验证节奏。

`game/` 子目录能把 Godot 工程与 Obsidian 知识库结构隔离，降低工程缓存、资源导入文件和构建输出对文档管理的干扰。后续 Codex 创建文件时也能根据目录边界判断哪些文件属于知识库，哪些文件属于游戏工程。

第一版 MVP 内容规模较小，暂时没有必要为了权限、Git LFS 或公开 / 私有边界拆成两个仓库。若后续出现大量美术音频二进制资源、团队分工明显分离、知识库需要公开而源码需要私有，或 Godot 工程明显影响知识库维护，再评估拆分仓库或引入子模块。

## 后续行动

- 将 [[design/06_technical_production/00_technical_requirements|技术需求]] 中的 Godot 项目目标文件层级调整为 `game/` 下的结构。
- 后续 Codex 初始化 Godot 工程时，应以 `game/` 为工程根目录。
- 后续 `.gitignore` 需要同时兼顾知识库和 `game/` 下的 Godot / C# 生成文件。
- 如未来拆分仓库，应保留本决策并新增替代决策记录，说明拆分原因、迁移方式和双仓库协作规则。

## 关联文档

- [[design/06_technical_production/00_technical_requirements|技术需求]]
- [[design/06_technical_production/01_data_pipeline_and_tools|数据管线与工具]]
- [[design/08_governance/2026-06-03_godot_csharp_codex_technical_stack|采用 Godot + C# 与 Codex 的技术生产方案]]
