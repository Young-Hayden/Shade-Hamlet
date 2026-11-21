# 《影村》文档索引

工程：`D:\Unity\projects\RPG_Project`（团结引擎 / Tuanjie 2022.3.62t5，工程内显示 1.8.3）
场景：`Assets/Scenes/界面.scene`（主菜单）、`Assets/Scenes/RPG游戏.unity`（第一章）
最近归整：2026-11-21（资产目录、脚本注释、文档索引）

## 推荐阅读顺序

1. **项目进度**：[`progress.md`](progress.md) —— 从 11-08 到 11-14 的逐阶段实施、修复、回归与推送记录。
2. **材料总清单**：[`项目材料清单-2026-11-21.md`](项目材料清单-2026-11-21.md) —— 资产/脚本/文档的结构、数量、用途与本次归整变更。
3. **设计**：[`superpowers/specs/`](superpowers/specs/) —— 14 份功能设计与场景设计。
4. **计划**：[`superpowers/plans/`](superpowers/plans/) —— 11 份实施计划与验收步骤。
5. **验收报告**：[`superpowers/reports/`](superpowers/reports/) —— 24 份逐阶段验收、审计与修复报告。
6. **证据材料**：[`verification/`](verification/) —— 22 个按日期归档的截图/日志目录，索引见 [`verification/README.md`](verification/README.md)。
7. **玩法与 UI 机会**：[`2026-11-08-gameplay-ui-opportunities.md`](2026-11-08-gameplay-ui-opportunities.md)。

## 目录约定

| 目录 | 内容 |
|---|---|
| `docs/progress.md` | 单一事实来源的进度流水，按日期与阶段追加，不覆盖历史。 |
| `docs/superpowers/specs/` | 设计文档，命名 `YYYY-MM-DD-<主题>-design.md`。 |
| `docs/superpowers/plans/` | 实施计划，命名 `YYYY-MM-DD-<主题>.md`。 |
| `docs/superpowers/reports/` | 验收/审计报告，命名 `YYYY-MM-DD-<主题>-verification.md` 或 `-audit.md`。 |
| `docs/verification/` | 截图、日志等证据，按 `YYYY-MM-DD-<主题>/` 分目录。 |
| `docs/项目材料清单-*.md` | 资产与脚本的结构清单，按归整日期快照。 |

## 维护规则

- 新增功能先写 spec，再写 plan，实施后补 report，并把截图/日志放入 `verification/` 对应日期目录。
- 每次小阶段完成后在 `progress.md` 追加记录；场景保存、提交、推送仍按原流程单独授权。
- 历史文档中的旧路径（如 `Assets/RPG/...`、`Assets/Perfabs/...`）保留原文，最新路径以材料总清单为准。
