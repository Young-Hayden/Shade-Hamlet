# RPG_Project 药水、目标与简化背包验收

日期：2026-11-09

## 已交付

- `PotionInventory.cs`：库存、Q 对应 TryUse、30 点治疗；满血/空库存/死亡不消耗。
- `HeroController.Heal`：安全治疗并同步原头顶血条。
- `HealthPotionPickup.prefab`：复用原 Health Pickup 模型，带 Trigger SphereCollider 和自动拾取。
- `LootDropper.cs`：敌人死亡只掉落一次。
- `GameFlow`：按敌人实例 ID 去重的 KillCount/KillTarget。
- `GameHUD`：红药水图标与 ×N、目标 0/1 与完成文本。
- `InventoryUI.cs`：I/Toggle 开关只读背包，显示药水、铁剑、无护甲；不暂停。

## 测试证据

### 药水

实现前按预期失败：`potion inventory/pickup/dropper types missing`。实现后 `POTION RUNTIME TESTS PASS`，覆盖：

- 初始 0；Add(2)=2；空库存使用失败。
- 满血使用失败且不消耗。
- HP 100→50 后两次使用：50→80→100，库存 2→1→0。
- 敌人双次致命伤害只生成一个拾取物。
- Hero 接近后通过真实物理 Trigger 入库一次，拾取物销毁。
- 死亡后 TryUse 失败，HP 保持 0，库存不减少。

### 目标 HUD

实现前按预期失败：`potion/objective HUD fields missing`。实现后 `OBJECTIVE HUD TESTS PASS`：

- 初始 `× 0`、`目标：击败巡逻敌人 0 / 1`。
- Add(2) 后显示 `× 2`。
- 敌人死亡并重复注册相同实例 ID 后 KillCount 仍为 1，显示完成文本。
- 新增 Rect 不相交断言先抓到目标提示与退出按钮重叠；下移后测试通过并截图。

### 简化背包

实现前按预期失败：`InventoryUI type missing`。实现后 `INVENTORY UI TESTS PASS`：

- Toggle 打开/关闭均正确，数量显示 `数量：3`。
- Time.timeScale=1。
- 面板与主角 HUD、退出按钮均不重叠。
- `inventory-open.png` 确认中文、药水/武器/护甲图标和布局可见。

### 重开与战斗回归

- 扩展 DeathFlowTests 后通过：重开 Hero/Enemy HP=100，PotionInventory.Count=0，KillCount=0，背包默认关闭，DeathCanvas=0；返回主界面正常。
- 最终战斗：敌人 100→80→60→40→20→0；尸体延迟后销毁；主角正常受伤。
- 实战结束后读取到 `potions=1 kills=1`，证明击杀掉落和近身自动拾取在完整战斗中生效。
- 最终死亡：HP=0、位移 0.000、死亡动画、2 秒销毁。
- 场景往返 RPG游戏→界面→RPG游戏成功。
- CameraControlTests、FeedbackRuntimeTests、HudRuntimeTests 均在干净状态中再次通过。

## 实施中发现的问题与处理

1. 原 Health Pickup 内嵌两个缺失脚本，Unity 拒绝保存新预制体；只在新副本中移除无效脚本，原资源未修改。
2. 初版拾取测试使用固定等待且异常时未退订，造成错误刷屏；诊断证明真实拾取可用后，将测试改为条件等待和单次异常清理。
3. 死亡测试最初把“拾取 1 + Add 1”误写成期望 1；修正为 2，未改生产逻辑。
4. 目标提示与退出按钮重叠由新增 Rect 断言实际抓到并修正，不是仅凭截图宣称通过。

## 持久化与遗留

停止 Play、从磁盘重载后 `POTION SYSTEM PERSISTENCE PASS`：Hero/PotionInventory、Enemy/LootDropper、拾取预制体/Trigger、GameHUD 药水引用、InventoryUI 全部 Sprite、GameFlow.KillTarget 均保留。

- Q/I 使用对应方法验证，真实键盘与鼠标仍需用户试玩。
- 新 UI 仍由运行时代码生成，编辑态不会显示完整界面。
- 既有烘焙后端、地形树 shader、场景切换 Unknown Missing Script 提示仍存在，未冒充本轮修复。
- 当前只有一个敌人与一种药水；更多敌人需重复配置 LootDropper，后续再考虑统一敌人预制体。

## 归档

- 截图：`docs/verification/2026-11-09-potions/`
- 备份：`D:\Unity\backups\RPG_Project_2026-11-09_potions\`
- 进度：`docs/progress.md`
