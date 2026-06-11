# RoguelikeCardGame

Roguelike 卡牌独立游戏原型项目，包含 Obsidian 友好的设计知识库和 Godot 4.6.x .NET / C# MVP 工程。

## Status

当前仓库已包含可运行的第一版 MVP：主菜单、战斗、奖励选择、线性 Run 推进、Boss 通关 / 失败结算和重开流程。MVP 仍处于原型验证阶段，重点是验证战斗局内循环、连锁层数、终结牌兑现和战后奖励闭环。

更多设计、范围和阶段状态见 [Design Knowledge Base](design/README.md) 与 [MVP 项目状态](design/07_production/03_mvp_project_status.md)。

## Requirements

- Godot `4.6.x` .NET / Mono 版，当前验证版本为 `4.6.3.stable.mono`。
- .NET SDK `8.0.x`。
- Python `3.x`，用于运行数据校验工具。

## Quick Start

Godot 工程根目录是 `game/`，入口场景为 `game/scenes/main/Main.tscn`。

```bash
cd game

# Godot 命令名称依安装方式不同，可能是 godot、godot4、godot-mono 或 Godot 可执行文件完整路径。
godot-mono --path .
```

也可以直接用 Godot .NET 编辑器打开 `game/project.godot`，点击运行项目。

## Rebuild Notes

- 修改 `game/src/**/*.cs` 后需要重新构建 C#。Godot 编辑器通常会自动编译；命令行启动前建议先运行 `dotnet build`。
- 修改 `game/data/**/*.json` 通常不需要重新构建，但需要重启当前游戏或重新进入流程，让数据重新加载。
- 修改 `.tscn`、图片、字体等 Godot 资源通常不需要手动 `dotnet build`；Godot 编辑器会处理资源导入。
- 修改 `game/project.godot`、入口场景或项目设置后，建议重启 Godot 项目。

## Validation

```bash
cd game

dotnet build RoguelikeCardGame.csproj -v:minimal
dotnet run --project tests/Unit/RoguelikeCardGame.Tests.csproj
python3 tools/data_validator/validate_data.py
godot-mono --headless --path . --quit
```

Windows PowerShell 下可使用项目检查脚本，并按本机 Godot 安装位置传入可执行文件路径：

```powershell
.\game\tools\check_project.ps1 -GodotPath "D:\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe"
```

## Repository Layout

- `game/`：Godot 4.6.x .NET / C# MVP 工程。
- `design/`：正式设计文档、技术方案、制作计划和治理记录。
- `inspiration/`：灵感、头脑风暴和阶段性 Q&A。
- `insight/`：竞品分析与外部资料洞察。
- `AGENTS.md`：知识库维护规则与 Codex 协作规则。

## Documentation

- [游戏概述](design/00_product/00_game_concept.md)
- [项目范围与成功标准](design/00_product/03_scope_and_success_criteria.md)
- [战斗系统](design/01_core_gameplay/02_combat_system.md)
- [卡牌系统](design/01_core_gameplay/03_card_system.md)
- [技术需求](design/06_technical_production/00_technical_requirements.md)
- [数据管线与工具](design/06_technical_production/01_data_pipeline_and_tools.md)
- [MVP 项目状态](design/07_production/03_mvp_project_status.md)
