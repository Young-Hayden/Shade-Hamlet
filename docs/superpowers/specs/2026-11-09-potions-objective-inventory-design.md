# RPG_Project 药水循环、目标提示与简化背包设计

日期：2026-11-09
状态：方案 A 已获用户批准

## 目标

在不恢复 GameDev.tv 原缺失脚本系统的前提下，用药水形成“击杀—拾取—受伤—使用”的基础资源循环，并补充击杀目标提示和只读简化背包。

## 范围与非目标

实现：药水数量、30 点治疗、Q 使用、敌人掉落、自动拾取、HUD 数量、1 个敌人的击杀目标、I 开关的简化背包。

不实现：拖拽、拆分、商店、装备更换、其他道具、弓箭手、武器切换、暂停、存档、对象池和真实按键自动注入。

## 组件

### `PotionInventory.cs`

- 挂在 Hero 上，持有非负药水数量和 30 点治疗量。
- `Add(int amount)` 只接受正数；`TryUse()` 在死亡、数量为 0 或满血时返回 false 且不消耗。
- 成功时先调用 `HeroController.Heal(30)`，只有实际治疗量大于 0 才扣 1 瓶。
- Update 检测 Q；无文本输入场景，不需要额外 UI 焦点判断。
- 场景重开时随 Hero 重建为 0 瓶。

### `HeroController.Heal(float amount)`

- 返回实际恢复量；死亡、非正数或满血返回 0。
- 生命 Clamp 到 `[0, MaxHP]`，同步原头顶血条；HUD 继续从 CurrentHP/MaxHP 读取。
- 不改 ATK、HP 上限、攻击和移动逻辑。

### `HealthPotionPickup.cs` 与拾取预制体

- 从现有 `Game/Pickups/Health Pickup.prefab` 创建 `Assets/Perfabs/HealthPotionPickup.prefab`，保留模型。
- 原资源没有碰撞器；新预制体添加 `SphereCollider(isTrigger=true)` 和本脚本。
- OnTriggerEnter 只接受带 HeroController/PotionInventory 的 Player；入库 1 瓶后销毁。
- 用 `picked` 守卫避免同帧多个碰撞事件重复入库。
- 缓慢旋转用于可发现性，不自动消失。

### `LootDropper.cs`

- 挂在当前 Enemy 上，持有拾取预制体引用。
- `Drop()` 有一次性守卫，在敌人位置附近实例化一瓶。
- `EnemyController.Die` 调用一次；重复 Hurt/Die 不会重复掉落。

### `GameFlow` 目标状态

- 增加 `KillCount`、`KillTarget=1` 和 `RegisterEnemyKill(int enemyId)`。
- 用 `HashSet<int>` 保存已计入的敌人实例 ID，重复信号不增加进度；目标进度只增不减，场景重开归零。
- 敌人 Die 同时调用击杀反馈和 `RegisterEnemyKill(GetInstanceID())`。
- 不改变现有死亡面板和场景切换逻辑。

### `GameHUD` 扩展

- 主角面板增加红药水图标和 `× N` 文本。
- 右上现有退出按钮下方增加目标提示，使用现有面板图片。
- 增加 `PotionInventory` 引用和公开只读显示字段供测试读取。
- 目标文本：未完成 `目标：击败巡逻敌人 0 / 1`；完成后 `目标完成：巡逻敌人已击败`。

### `InventoryUI.cs`

- GameSystems 上的独立组件，启动时创建但隐藏 `InventoryPanel`。
- I 或 `Toggle()` 切换；不修改 Time.timeScale。
- 显示药水图标/数量、当前武器“铁剑”、当前护甲“无”和固定说明。
- 复用 Background_01、红药水、`InventoryButton_weapon.png`、`T_Chest_04_Nobg.png`。
- 场景重开后面板默认关闭。

## 场景与资源接线

- Hero 添加 PotionInventory；Enemy 添加 LootDropper；GameSystems 添加 InventoryUI。
- GameHUD 绑定 PotionInventory 和红药水图标。
- GameFlow 不持有场景外的持久状态。
- 所有资源通过 AssetDatabase 显式绑定，不用运行时路径加载。
- 修改前备份脚本、场景和新建预制体所在目录；每个阶段保存并更新 progress.md。

## 验证

1. 新类型/行为测试在实现前按预期失败。
2. 初始 0 瓶；Add(2) 后为 2；满血 TryUse 不消耗；受伤 50 后两次使用恢复至 100 并消耗 2 瓶；空库存无效。
3. 死亡后 Add 可保留物品但 TryUse 不生效，HP 保持 0。
4. 敌人双次致命伤害只掉落一个拾取物；Hero 接近后只入库一次，拾取物销毁。
5. HUD 数量与库存一致；目标从 0/1 变为完成；重开后均复位。
6. I/Toggle 能开关面板，Time.timeScale 不变；面板与退出按钮/主角 HUD 不重叠。
7. 截图验证中文、图标、布局；完整回归编译、反馈、HUD、战斗、死亡和场景切换。
8. 真实 Q/I 键和鼠标点击仍由用户试玩验收；脚本只验证对应方法与按钮事件。

## 回滚

备份目录：`D:\Unity\backups\RPG_Project_2026-11-09_potions\`。三个阶段分别保存场景副本。若拾取或 UI 验证失败，停留在该阶段，不继续叠加下一功能。
