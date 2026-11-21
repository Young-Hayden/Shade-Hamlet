# `Assets` 目录说明

本工程所有运行时资产位于 `Assets/`。自 2026-11-21 起按职责归类如下：

| 目录 | 内容 | 说明 |
|---|---|---|
| `Scenes/` | `界面.scene`、`RPG游戏.unity`、`RPG游戏/NavMesh*.asset` | 主菜单与第一章场景；NavMesh 数据随场景存放。 |
| `Script/` | 48 个自研 C# 脚本，按 9 个模块分目录 | 见下表。所有脚本已补中文 XML 文档注释。 |
| `Animations/` | `Hero/`、`Enemy/` | 角色动画剪辑、Animator Controller、武器模型。 |
| `Art/` | `UI/Menu/`、`Materials/` | 主菜单背景/暗角/纯白图；项目专用材质（灯笼、木材、夜景植被）。 |
| `Prefabs/` | 7 个预制体 + `Environment/` | 主角、士兵、敌人组、攻击盒、药水、箭矢；环境树碰撞变体。 |
| `Terrain/` | `RPG_Terrain.asset` | 本场景专用地形数据（复制自 Synty 示例，原资产未改）。 |
| `Story/` | 6 个 ScriptableObject | 章节定义与 5 段对话资产。 |
| `MainAsset/` | 第三方素材包 | Synty、BOXOPHOBIC、SimpleFX、Fantasy RPG UI 等，**不要移动或改名**（部分工具按路径查找）。 |

## `Script/` 模块

| 模块 | 数量 | 职责 |
|---|---:|---|
| `Core/` | 4 | 流程、场景过渡、音量服务、全局音效。 |
| `Player/` | 4 | 主角控制、直接移动、动画事件、第三人称相机。 |
| `Combat/` | 5 | 攻击盒、伤害/血条、命中反馈、伤害弹字、刺杀。 |
| `Enemy/` | 6 | 敌人 AI、感知、远程攻击、箭矢、动画事件、掉落。 |
| `Stealth/` | 5 | 潜行状态、噪音、隐藏区域、烟雾能力与烟雾区域。 |
| `Story/` | 12 | 剧情导演、会话/检查点、目标、章节/对话资产、UI、NPC/线索/偷听。 |
| `UI/` | 6 | 运行时 UI 工厂、HUD、背包、暂停、音量面板。 |
| `Items/` | 2 | 药水拾取与库存。 |
| `Legacy/` | 4 | 旧「取卷轴→撤离」与旧主菜单按钮，剧情模式下停用但保留。 |

## 约定

- 新增脚本按职责放入对应模块；跨模块引用直接使用类名，工程没有命名空间。
- 新增剧情内容放入 `Story/`，并以 `Story/Chapter` 菜单创建资产。
- 第三方 `MainAsset/` 保持原样；需要修改时复制到 `Art/` 或 `Prefabs/` 再改。
