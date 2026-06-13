# 术语表

状态：持续维护  
上级索引：[[design/README|Design Knowledge Base]]

## 目的

统一项目内的术语、关键词和机制命名，避免文档、UI 和数据表使用不同表达。

## 基础术语

| 术语 | 定义 | 关联文档 |
| --- | --- | --- |
| 《剑与黑塔》 | 本项目正式游戏名。 | [[design/00_product/00_game_concept]] |
| Run | 从开局到胜利或失败的一整局游戏流程。 | [[design/01_core_gameplay/01_run_structure]] |
| 左 | 本作主角，由左轮剑剑灵化为人类。 | [[design/02_content_systems/01_characters_and_archetypes]] |
| 茵 | 曾经的知名勇者，为救左牺牲肉身，其灵仍在左轮剑中沉睡。 | [[design/05_narrative_world/00_world_and_tone]] |
| 黑塔 | 失色之夜后出现的神秘塔群，被认为是夺回世界色彩的关键。 | [[design/05_narrative_world/00_world_and_tone]] |
| 勇者联盟 | 为攻克黑塔而成立的无政府组织，后逐渐腐败。 | [[design/05_narrative_world/00_world_and_tone]] |
| 构筑 | 玩家在一局中通过武器、卡牌、色彩、遗物、资源和路线选择形成的策略组合。 | [[design/01_core_gameplay/03_card_system]] |
| 武器 | 角色机制和卡池入口。每种武器拥有独立战斗方式和卡池，不是传统装备掉落系统。 | [[design/01_core_gameplay/03_card_system]] |
| 左轮剑 | 初始主手武器，剑与枪结合，通过斩击生成彩能并以枪形态释放。 | [[design/02_content_systems/01_characters_and_archetypes]] |
| 机械臂 | 初始副手武器，高科技义肢，偏防御、护盾和彩能控制。 | [[design/02_content_systems/01_characters_and_archetypes]] |
| 行动点 | 默认由行动牌消耗的每回合战斗资源；基础上限为 3，每回合恢复当前行动点上限 + 额外行动点数，未使用行动点在玩家回合结束时清空。 | [[design/01_core_gameplay/04_resource_economy]] |
| 彩能 | 原“连锁层数”的世界观命名和新版规则表达；行动牌主要生成、终结牌主要消耗的回合内战斗资源，基础上限为 6，跨回合不保留。 | [[design/01_core_gameplay/02_combat_system]] |
| 色彩 | 附着在彩能上的属性，由色彩碎片附魔行动牌后产生。 | [[design/01_core_gameplay/02_combat_system]] |
| 色彩碎片 | 普通战斗后的基础奖励之一，可为未附魔行动牌附魔。 | [[design/02_content_systems/03_relics_items_rewards]] |
| 色彩核心 | Boss 或阶段性高价值奖励，用于提升某种色彩的附魔或终结结算效果。 | [[design/02_content_systems/03_relics_items_rewards]] |
| 行动牌 | 主要负责攻击、防御、控制、过牌、回费、减费、生成彩能和调整牌序的卡牌类型，可被色彩碎片附魔。 | [[design/01_core_gameplay/03_card_system]] |
| 终结牌 | 消耗彩能造成强大的伤害或效果，并根据被消耗彩能的色彩产生追加效果的卡牌类型。 | [[design/01_core_gameplay/03_card_system]] |
| 防御 | 用于抵挡敌人伤害的短期资源；玩家回合结束时不清空，敌人行动后若仍有剩余，则在下一回合开始时清空。 | [[design/01_core_gameplay/02_combat_system]] |
| 敌人意图 | 敌人在下一次行动中将执行的行为预告。 | [[design/01_core_gameplay/02_combat_system]] |
| 遗物 | 通常跨战斗生效、改变规则或提供被动收益的奖励，也承担碎片化叙事。 | [[design/02_content_systems/03_relics_items_rewards]] |
| 普通遗物 | 精英战斗在基础奖励之外额外给予的遗物类型，主要提供稳定构筑收益或小型规则变化。 | [[design/02_content_systems/03_relics_items_rewards]] |
| Boss 遗物 | Boss 战斗额外提供的高影响遗物类型，应能影响构筑、改变玩法或显著改变后续决策。 | [[design/02_content_systems/03_relics_items_rewards]] |
| 事件 | 非标准战斗节点中的选择型遭遇，是碎片化叙事的主要渠道之一。 | [[design/02_content_systems/04_events_and_encounters]] |
| 抽牌堆 | 玩家当前可抽取卡牌的牌区；抽牌堆不足时，默认将弃牌堆洗回抽牌堆继续抽。 | [[design/01_core_gameplay/02_combat_system]] |
| 弃牌堆 | 已使用、被弃掉或回合结束弃置的普通卡牌所在牌区；可在抽牌堆不足或特定效果下被洗回抽牌堆。 | [[design/01_core_gameplay/02_combat_system]] |

## 色彩术语

| 术语 | 定义 | 备注 |
| --- | --- | --- |
| 红色 | 力量，附加伤害效果。 | 输出路线。 |
| 黄色 | 速度，增加释放次数、追加段数或复制轻量效果。 | 高风险，需防失控。 |
| 蓝色 | 防卫，附加护盾或防御效果。 | 抗压路线。 |
| 绿色 | 生命，附加回复或续航效果。 | 续航路线。 |
| 紫色 | 神秘，附加翻倍、放大或异常效果。 | 高风险高收益。 |

## 关键词候选

| 关键词 | 候选定义 | 备注 |
| --- | --- | --- |
| 附魔 | 将色彩碎片绑定到未附魔行动牌，使其后续生成对应色彩彩能。 | 需要区分同名牌实例。 |
| 回费 | 在当前回合恢复或获得行动点的效果。 | 默认可使当前行动点超过基础上限。 |
| 减费 | 降低卡牌行动点消耗的效果。 | 影响范围需在文本明确。 |
| 过牌 | 抽取、检索或以其他方式获得更多可用手牌的效果。 | 用于寻找附魔行动牌和终结牌。 |
| 重洗 | 将弃牌堆洗回抽牌堆的牌区操作。 | 默认在抽牌堆不足时发生。 |
| 保留 | 使手牌、防御、行动点或彩能突破默认清空规则并延续到指定时点的效果。 | 保留彩能是高价值效果。 |

## 历史术语

| 术语 | 状态 | 说明 |
| --- | --- | --- |
| 连锁层数 | 已替代 | 被“彩能”替代。历史文档和旧实现中的连锁层数对应新版彩能的前身，但 3 / 5 / 8 阈值不再保留。 |
| 技能牌 | 已移除 | 旧卡牌类型。防御、控制、过牌、回费等职责现归入行动牌或终结牌。 |
| 卡牌包 | 已替代 | 旧普通战斗奖励。现由色彩碎片 + 武器卡牌三选一替代。 |

所有正式关键词必须补充定义、显示文本、图标需求和结算时机。
