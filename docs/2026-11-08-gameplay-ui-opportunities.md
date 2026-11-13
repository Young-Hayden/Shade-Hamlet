# RPG_Project 现有素材驱动的玩法与 UI 优化建议

日期：2026-11-08

## 先明确现状

当前可玩的核心是：左键点地移动、右键选敌并追击攻击、单个近战敌人巡逻与受击反击、双方血条、死亡销毁、开始/游戏场景切换。

本文以下内容是**后续建议，不是已实现功能**。`MainAsset/MainAsset/Game` 中虽然已有 HUD、背包、伤害数字、投射物和拾取物预制体，但抽查发现其部分自定义脚本引用未解析；工程中实际业务脚本仍主要是 `Assets/Script` 下的 9 个脚本。因此应复用这些预制体的美术、UI 层级和配置思路，并重新连接逻辑，不能当作开箱即用的完整系统。

## 一、最值得先做的玩法优化

### P1：补齐“命中—击杀—死亡—重开”的完整反馈闭环

**现有素材**
- `Game/UI/In Game/Damage Text.prefab`、`Damage Text Spawner.prefab`：伤害数字外观与层级参考。
- `Game/Effects/Level Up.prefab`：击杀或升级时的庆祝效果。
- `SimpleFX/Prefabs/FX_BloodSplatter.prefab`、`FX_Dust_Prefab_01.prefab`：命中和脚步/落地反馈。
- `Fantasy RPG UI Sample/UI/Parts/Background_01.png`、`Button_04.png`：死亡结算面板与按钮。

**建议玩法**
1. 攻击盒确认命中后再生成伤害数字，未命中不显示；数字从目标头顶上浮并淡出。
2. 主角/敌人受击时播放一次短促粒子，不要每帧生成；攻击盒已有每目标去重，可直接复用伤害事件。
3. 敌人死亡显示小型击杀反馈，并为目标计数/掉落提供事件。
4. 主角死亡后由独立游戏流程对象显示“重新挑战 / 返回主界面”，不要把该逻辑挂在即将销毁的 Hero 上。

**价值与风险**：投入小、即时手感提升最大。风险主要是粒子/伤害数字频繁 Instantiate；应限制生命周期，后续再做对象池。

### P1：加入药水拾取，形成第一次资源决策

**现有素材**
- `Game/Pickups/Health Pickup.prefab`、`Default Pickup.prefab`、`Pickup Spawner.prefab`。
- `Universal Gold Icons Sample/Potion_01(Red)_No BG.png`。
- `Game/UI/Inventory/InventorySlot.prefab`、`ActionSlot.prefab`。

**建议玩法**
- 敌人死亡掉落 1 瓶药水，靠近自动拾取；按 Q 使用，恢复固定 25～30 点生命，不超过上限。
- 背包先只做“药水数量”，不要一开始就上完整背包和拖拽。
- 让玩家决定“现在喝药继续打，还是保留到下一波”，比单纯提高攻击力更有意义。

**价值与风险**：能直接改善目前只能硬拼、受伤无法恢复的问题。需要新增拾取触发器、数量状态和使用冷却，复杂度中低。

### P2：用弓箭手形成第二种战斗节奏

**现有素材**
- `Game/Characters/Enemies/Archer.prefab`。
- `Game/Projectiles/Arrow Projectile Blue.prefab`、`Arrow Projectile Red.prefab`、`Root Arrow Projectile.prefab`。
- `Game/Effects/Arrow Impact Effect.prefab`。
- `Asset Packs/Animations/2Hand-Bow`、`Game/Weapons/Bow/Equipped Bow.prefab`。

**建议玩法**
- 近战兵保持现有追击；弓箭手在 6～8 米停步，每 2 秒射击一次。
- 箭矢使用真实飞行物和碰撞，给玩家走位空间；不要直接改成远程必中扣血。
- 先只做“一个近战 + 一个远程”的组合，验证玩家能通过移动躲箭后再增加数量。

**价值与风险**：比堆血量更能增加深度。风险是投射物生命周期、命中归属和寻路停留距离，复杂度中。

### P2：以现有村庄布置三段短流程，而不是继续扩大空地

**现有素材**
- `Synty - Polygon Student Sample/Prefabs/SM_Bld_Hut_01.prefab`、`SM_Bld_Village_02.prefab`、房屋/围栏/木箱/岩石预制体。
- `Game/Characters/Paths/Patrol Path.prefab`、`Game/Core/Portal.prefab`。
- 两种 Soldier 角色预制体与现有近战动画。

**建议流程**
1. **村口教学**：单个近战兵，学习移动、选敌和喝药。
2. **林间遭遇**：近战兵与弓箭手组合，学习优先击杀远程。
3. **村口守卫**：用 Soldier_02 做精英，适当提高生命，复用现有攻击动画；用围栏和房屋形成明确战场边界。
4. 完成后通过 Portal 或结算面板结束本轮，而不是无限巡逻。

**价值与风险**：只改摆放、触发器和少量参数即可形成目标感。不要一开始加入开放世界、随机地图或大量 NPC。

### P3：再做武器切换与轻量成长

**现有素材**
- `Game/Weapons/Sword`、`Bow`、`Unarmed` 三套装备预制体。
- `GameDev.tv Assets/Club`、`Hoe`、`Pitchfork` 模型。
- `Asset Packs/Animations` 下 Sword、2Hand-Sword、2Hand-Bow、2Hand-Club、2Hand-Spear、Unarmed 等动画目录。
- `Universal Gold Icons Sample` 中武器、护甲、头盔、手套、裤子、靴子图标。

**建议玩法**
- 先只做剑/弓两套：剑近战高伤，弓远程低伤；按 1/2 切换并替换 Animator OverrideController 与武器模型。
- 精英首次死亡解锁弓，比直接把所有武器放在开局更有目标。
- 装备成长先只保留武器和护甲两项，避免 six 个装备槽带来大量数值与 UI 工作。

## 二、UI 优化建议

### 1. 重构 HUD 信息层级

**推荐布局**
- **左上**：主角生命条 + 药水数量，保留当前简洁风格。
- **顶部中央**：选中敌人的名字和生命条；未选中时不显示。
- **底部中央**：3 个动作槽——剑、弓、药水，显示 1/2/Q 快捷键。
- **右上**：当前目标，例如“击败村口守卫 0/1”。
- **Esc**：统一收纳继续、重新开始、返回主界面、退出，不再让“退出游戏”长期占据左上角。

**可复用素材**
- `Game/UI/HUD.prefab` 的布局参考。
- `Fantasy RPG UI Sample/UI/Update 1.1/ScrollingBar_Long_size.png`。
- `Game/UI/Inventory/ActionSlot.prefab`、`EquipmentSlotUI.prefab`。
- `Universal Gold Icons Sample/T_scroll_NOBG.png` 可配合任务文本使用。

### 2. 统一视觉规范，优先解决可读性

- 当前左上角按钮是红字压深色底，对比度和观感都较差；换成 Fantasy RPG UI 的按钮底图，文字用米白或浅金，红色只用于危险/确认退出。
- 普通面板用深色半透明底，描边和标题用金色；伤害数字用白/黄，玩家受伤用红，治疗用绿。
- 同屏长期常驻的 UI 不超过“玩家状态、目标、动作槽”三组，其余放 Esc 或背包面板。
- 当前 Canvas Scaler 仍是固定像素模式；建议改为 **Scale With Screen Size**，以 1920×1080 为参考分辨率并检查 16:9、16:10 和窗口化布局。

### 3. 优化世界空间血条的显示策略

本轮已降低并缩小血条。下一步建议：
- 敌人满血且未被选中时隐藏；受击或被右键选中后显示。
- 主角可保留常驻血条，但最终应迁移到 HUD，头顶只保留敌人/精英信息。
- 远距离敌人不显示血条，避免拉远相机后满屏绿条。
- 可复用 `Game/UI/In Game/Health Bar.prefab` 的外观，但其原逻辑引用需重新连接。

### 4. 用现有背包资源做“小型面板”，不要直接复刻完整 RPG 背包

**现有素材**
- `Game/UI/Inventory Canvas.prefab`。
- `InventorySlot.prefab`、`ItemTooltip.prefab`、`InventoryItem.prefab`。
- Fantasy RPG UI 的装备槽按钮与背景/边框 PNG。
- Universal Gold Icons 的药水和装备图标。

**第一版只做**：药水数量、当前武器、当前护甲、物品说明。拖拽、堆叠拆分、商店和网格整理都延后。按 I 开关，打开时仍可暂停战斗或保持实时，但两者必须明确选一个并统一行为。

### 5. 补一条可消退的操作提示

开局显示一次：
`左键移动　右键攻击　滚轮缩放　中键旋转　Q 药水　I 背包　Esc 菜单`

使用 `Background_02.png` 做浅底，数秒后淡出，也可在 Esc 中重新查看。避免长期占据屏幕。

## 三、推荐实施顺序

| 批次 | 内容 | 原因 |
|---|---|---|
| 第一批 | 伤害数字、命中特效、死亡重开、HUD 基础布局 | 投入较小，立刻改善完整度与手感 |
| 第二批 | 药水掉落/拾取/使用、当前目标提示、简化背包 | 形成基础资源循环和目标感 |
| 第三批 | 弓箭手、箭矢、剑/弓切换、三段关卡 | 增加真正的战斗与流程深度 |
| 暂缓 | 大型装备系统、开放世界、随机地图、大量 NPC | 当前内容量与底层系统尚不足以支撑 |

## 四、不要误用的资源

- 不要直接把 GameDev.tv 的 HUD/背包预制体拖进场景就视为功能完成；缺失脚本必须补齐并重新接线。
- 不要为了使用素材而同时加入所有武器、图标和粒子；先围绕一条可完成的短流程选择资源。
- 地形树的 Soft Occlusion 告警与上述玩法/UI 工作无关，仍建议保持现状。
