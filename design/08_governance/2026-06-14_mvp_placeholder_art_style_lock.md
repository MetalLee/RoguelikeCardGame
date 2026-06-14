# 决策：锁定 MVP 占位美术风格参考

日期：2026-06-14  
状态：已接受  
影响范围：体验 / 视觉 / UI / 美术资产 / AI 生成流程

## 背景

`design/03_experience/assets/` 下已经归档了一批角色动作、魔物序列帧、Boss 动作、背景层、UI 零件和爆炸特效参考图。这批资源形成了比早期“手绘漫画书风格”更具体的美术语言：高留白黑白漫画线稿、粗细墨线、少量排线、纯黑剪影、横版荒芜舞台和克制机制色。

后续第一版 MVP 仍会通过 `gpt-image-2` 生成大量占位美术。如果没有更硬的风格锁定，生成结果容易漂移到彩色插画、厚涂概念图、暗黑哥特、霓虹特效或通用卡牌游戏 UI，削弱《剑与黑塔》的失色主题和视觉统一性。

## 选项

- 方案 A：继续沿用宽泛的“手绘漫画书风格”，每次生成时临时描述。
- 方案 B：将 `design/03_experience/assets/` 现有黑白线稿资源确认为 MVP 占位美术最高参考，并把生成、验收和归档规则写入设计文档。

## 决策

采用方案 B。

第一版 MVP 的占位美术默认采用黑白手绘漫画线稿风格：白纸高留白、清晰墨线、少量排线 / 网点、大面积纯黑剪影、横版分镜舞台和克制机制色。`design/03_experience/assets/` 下资源是后续 `gpt-image-2` 生成占位图的最高风格准则。

除非开发者明确提出新的替代决策，否则所有 MVP 角色、魔物、Boss、背景、VFX、UI 图标、卡牌插画、奖励物和结算图都必须先满足该风格锁定，再进入 `game/assets/art/` 和 `game/data/presentation/`。

## 理由

该方案把“失色世界”从叙事概念落实为可执行的视觉生产规则。黑白线稿和高留白能让卡牌文本、魔物意图、彩能槽、伤害数字和拖拽目标保持可读；少量机制色能让“夺回色彩”成为真正的玩法反馈，而不是普通装饰色。

这批参考图还覆盖了多个资产类型：主角、武器、背景、魔物、Boss 和爆炸特效，因此足以约束后续占位资源生成，不需要每次从零解释风格。

## 后续行动

- 已在 [[design/03_experience/01_visual_direction|视觉方向]] 中新增“MVP 占位美术最高准则”，记录参考资源、风格摘要、`gpt-image-2` 提示词约束、禁止项和验收标准。
- 已在 [[design/03_experience/00_ui_ux|界面与交互]] 中补充黑白漫画页式 UI 承载规则。
- 已在 [[design/06_technical_production/01_data_pipeline_and_tools|数据管线与工具]] 中补充 AI 占位美术生成、验收和归档要求。
- 后续生成新资产时，应先把失败样例和成功样例沉淀为提示词片段或制作记录，减少风格漂移。

## 关联文档

- [[design/03_experience/01_visual_direction|视觉方向]]
- [[design/03_experience/00_ui_ux|界面与交互]]
- [[design/06_technical_production/01_data_pipeline_and_tools|数据管线与工具]]
- [[design/08_governance/2026-06-03_hand_drawn_comic_book_visual_direction|采用手绘漫画书风格作为主视觉方向]]
- [[design/03_experience/assets/视觉参考.jpg|视觉参考]]
