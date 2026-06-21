# 拍位对撞特写与打击反馈设计

日期：2026-06-22  
状态：等待用户复审  
关联设计：[[docs/superpowers/specs/2026-06-20-combat-rework-design|战斗重构设计：三拍拆招、成功产彩与终结分镜切入]]、[[design/01_core_gameplay/02_combat_system|战斗系统]]、[[design/03_experience/01_visual_direction|视觉方向]]

## 目的

本设计用于增强《剑与黑塔》当前三拍拆招战斗的打击感和爽感。当前已实现行动牌部署与动作对撞规则，本切片不重写规则核心，而是把“拍位结算”表现成漫画化近景对撞：主角冲到目标魔物身边，镜头切入只保留对撞双方，按左侧拍位 I、II、III 顺序执行行动，并显示伤害数字、格挡 / 闪避反馈、VFX 和产彩反馈。

目标体验是让玩家感到自己不是在等待一串日志播放，而是在驱动一段由三拍行动组成的战斗分镜。

## 范围

包含：

- 为拍位结算新增 MVP 级“特写覆盖层”演出。
- 播放顺序以左侧己方拍位 I、II、III 为准。
- 每拍根据目标魔物决定镜头切入与主角位移。
- 连续拍位锁定同一个魔物的拍位或本体时，保持同一个特写覆盖层，主角不回到初始站位，动作连续衔接。
- 目标魔物变化、空拍或无目标拍会结束当前连续特写段；后续有目标时主角先回到初始站位，再冲向新目标。
- 显示伤害数字、受击抖动、攻击 / 格挡 / 闪避反馈、命中短暂停顿与产彩反馈。
- 演出完全由规则日志驱动，不在表现层重新计算伤害、格挡、闪避或彩能。

不包含：

- 不新增目标锁定顺序规则。
- 不改变三拍真实结算顺序，仍按己方 `BeatIndex` 升序结算。
- 不制作完整逐帧角色攻击动画系统。
- 不新增外部依赖。
- 不实现最终商业级美术资源。
- 不把终结牌大特写完整纳入本切片；只预留后续接入位置。

## 规则顺序

真实规则结算顺序仍按左侧己方拍位编号执行：

1. 第 I 拍。
2. 第 II 拍。
3. 第 III 拍。

玩家选择目标的时间先后不改变结算顺序。即使玩家先锁定第 III 拍，再锁定第 I 拍，本轮结算与动画仍按 I、II、III 播放。

当前 `BeatCombatService.ResolveBeatRound()` 已按 `BeatIndex` 排序处理己方拍位，因此不需要新增 `TargetOrder` 字段。若未来允许拖拽调整拍位顺序，也应继续以最终拍位编号作为结算时间轴，而不是以目标点击时间作为结算时间轴。

未被锁定的魔物拍位仍在己方已锁定拍位全部结算后处理，保持当前规则压力：玩家没接住的魔物动作仍会正常打向玩家。

## 演出分组

表现层按日志生成 `BeatClashAnimationStep`，每个 step 对应一个已结算的己方拍位。step 按 `beat_index` 升序播放。

每个 step 至少需要包含：

- `beat_index`：己方拍位编号。
- `source_card_id`：该拍位行动牌。
- `card_instance_id`：该拍位行动牌实例。
- `enemy_instance_id`：目标魔物实例。
- `target_kind`：目标是魔物拍位还是魔物本体。
- `enemy_beat_index`：目标魔物拍位编号，目标本体时为空。
- `enemy_damage`：本拍对魔物造成的生命伤害或总伤害表现数值。
- `player_damage`：本拍对玩家造成的生命伤害或总伤害表现数值。
- `successful_player_actions`：本拍成功动作数，用于接产彩反馈。

连续性规则：

- 如果当前 step 与上一 step 的 `enemy_instance_id` 相同，表现层保持当前特写覆盖层，主角不回位，不重新切镜。
- 如果当前 step 与上一 step 的 `enemy_instance_id` 不同，表现层结束上一段特写，主角回到初始站位，再冲向新目标并进入新的特写覆盖层。
- 如果中间出现空拍或无目标拍，当前特写段结束，主角回初始站位。后续有目标 step 时重新冲刺切入。
- 同一魔物的不同拍位与本体都视为同一连续目标。例如 I 打魔物 A 第 1 拍、II 打魔物 A 第 2 拍、III 打魔物 A 本体时，应在同一个特写中连续演出。

示例：

- `I -> 魔物A拍位`、`II -> 魔物B拍位`、`III -> 魔物A本体`：冲 A，对撞，回位；冲 B，对撞，回位；冲 A，对撞，回位。
- `I -> 魔物A拍位1`、`II -> 魔物A拍位2`、`III -> 魔物A本体`：冲 A，连续执行 I / II / III，回位。

## 特写覆盖层

新增战斗表现组件，建议命名为 `BeatClashCutInLayer`。它是 `BattleScreen` 动画期间临时创建的覆盖层，挂在现有 `FxLayer` 上方或同级更高 `ZIndex`。

职责：

- 暗化普通战斗画面。
- 临时隐藏或弱化其他魔物、手牌和大部分 HUD。
- 在覆盖层中重建主角与目标魔物的近景视觉。
- 显示当前拍序，如 `I`、`II`、`III`。
- 播放主角冲刺、命中停顿、目标受击抖动、VFX、伤害数字和产彩反馈。
- 连续同目标 step 时复用当前覆盖层，不重建镜头。
- 演出结束后恢复普通战斗界面可见性和交互状态。

MVP 资源策略：

- 主角使用现有 `asset.character.zu.revolver.battle`。
- 魔物使用当前 `BattleEnemyView` 已加载的站姿或序列帧首帧。
- 攻击、重击、防御和冲击使用现有 `asset.vfx.*` 资源。
- 伤害数字使用 Godot `Label` 绘制，不生成图片字。
- 不新增二进制资源。

## 数据流

规则层仍负责事实：

1. 玩家在三拍区域放置行动牌并选择目标。
2. `BeatCombatService.ResolveBeatRound()` 按己方拍位编号结算。
3. 每个已结算己方拍位产生 `BeatActionResolved` 日志。
4. 若该拍成功产彩，紧跟产生 `BeatEnergyGenerated` 日志。
5. 表现层读取本轮新增日志，构建 `BeatClashAnimationStep` 列表。
6. `BeatClashCutInLayer` 按 step 顺序播放特写。

日志字段要求：

- `BeatActionResolved.NumericChanges["beat_index"]` 必须存在。
- `BeatActionResolved.TargetIds` 必须包含目标魔物实例 ID；目标为玩家时仅用于未被锁定魔物拍位，不进入己方拍位特写 step。
- `BeatActionResolved.Metadata["target_kind"]` 必须区分 `EnemyBeat` 与 `EnemyBody`。
- `BeatActionResolved.Metadata["enemy_beat_index"]` 在目标为魔物拍位时必须可读。
- `BeatActionResolved.NumericChanges["enemy_damage"]`、`["player_damage"]`、`["successful_player_actions"]` 用于表现数字与产彩反馈。
- `BeatEnergyGenerated.Metadata["card_instance_id"]` 与对应拍位行动牌实例一致时，绑定到同一个 step 末尾播放。

如果实现时发现现有字段已满足上述要求，则不新增日志字段；如果某字段缺失，则优先补充结构化日志，而不是让表现层猜测。

## 错误处理

- 如果某个 step 缺少目标魔物节点，跳过该 step 的特写层，保留日志预览，不阻塞整轮结算。
- 如果 VFX 资源无法加载，使用默认 `asset.vfx.enemy_hit_comic_burst`。
- 如果目标在前一拍已死亡，后续同目标 step 仍可播放短暂追加打击或跳过，具体以规则层是否产生该 step 的日志为准；表现层不自行判断是否应该造成伤害。
- 动画期间沿用现有交互锁，避免玩家在演出未结束时继续操作。

## 测试

规则层测试：

- 玩家先锁定第 III 拍再锁定第 I 拍时，`ResolveBeatRound()` 仍按 I、II、III 产生 `BeatActionResolved` 日志。
- `BeatActionResolved` 包含动画计划需要的目标魔物、目标类型、拍位编号和伤害数字。
- `BeatEnergyGenerated` 能通过 `card_instance_id` 绑定到对应拍位 step。

表现计划测试：

- A / B / A 目标序列生成 3 个特写段，每段都需要回位再冲刺。
- A / A / A 目标序列生成 1 个连续特写段，后两拍不回位、不重新切镜。
- A / 空 / A 目标序列生成 2 个特写段，空拍中断连续性。
- 魔物拍位与同一魔物本体视为同一连续目标。

Godot 手工验证：

- 普通战斗中，结束回合后主角冲向第 I 拍目标并进入近景。
- 连续同目标时，近景不断开，伤害数字与 VFX 连续播放。
- 切换目标时，主角回到初始位再冲向下一个目标。
- 演出结束后 HUD、手牌、魔物显示恢复，下一轮可继续操作。

## 验收标准

- 战斗规则结果与现有规则一致，除日志字段补齐外不改变数值。
- 拍位结算与动画播放均按 I、II、III。
- 玩家能明确看懂当前正在执行哪一拍、打向哪个魔物、造成多少伤害。
- 连续攻击同一魔物时动作连贯，不反复切出特写。
- 跨魔物攻击时主角先回初始位再冲向新目标。
- 数据校验、单元测试、主工程构建和 Godot headless 检查通过。

## 关联文档更新

实现完成后，应同步更新：

- [[design/01_core_gameplay/02_combat_system|战斗系统]]：补充拍位对撞特写、同目标连续演出和演出不改规则原则。
- [[design/03_experience/01_visual_direction|视觉方向]]：补充 MVP 特写覆盖层的表现规范。
- [[design/08_governance/01_change_log|变更日志]]：记录本次打击感增强。
