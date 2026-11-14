# 2026-11-14 《影村》人物与地图碰撞验收报告

范围：主角、队长、弓箭手、老药师与地形、房屋、树木、岩石、道具、边界的碰撞；包括静止、玩家移动、NPC 巡逻及 NavMesh 可达性。

## 查明并修复的问题

1. **敌人胶囊底部低于脚底**：队长和弓箭手原 `CapsuleCollider.center.y=0.8`、`height=2.45`，底部比根节点低 0.425m，会插入地面/土坡。统一改为 `center=(0,1.2,0)`、`height=2.4`、`radius=0.4`。
2. **新增场景细节未进入导航**：装饰只标了 Batching/Occludee Static，没有 Navigation Static，且场景细节加入后未重烘。给 125 个实体装饰补标 Navigation Static 并重烘 NavMesh。
3. **岩石被当作可行走斜坡**：巡逻中弓箭手与岩石有 0.07–0.12m 瞬时擦入。给 27 个岩石增加 `NavMeshObstacle`，启用 `carving` 和 `carveOnlyStationary`，角色改为绕行。土坡仍保留可行走，不改变玩法地形。
4. **老药师缺身体碰撞**（前一轮已修）：补非触发胶囊后，玩家最近停在 0.78m，不再能与 NPC 重合。

## 验证结果

修复前：

- 老药师可与玩家重合至距离 0；队长/弓箭手有地面或土坡穿入。
- 木桶、树桩、边界树内部 `NavMesh.SamplePosition=True`，巡逻可穿过新增装饰。
- 20 秒巡逻记录到岩石、土坡、长椅等重叠。

修复后（`map_collision_tests.txt`）：

- `PASS no actor intersects map at rest count=0`：所有人物静止时与地图穿插为 0。
- `PASS solid details are NavMesh obstacles missing=0`：木桶、树桩、边界树内部均不可行走。
- `PASS hero blocked by barrel`：玩家撞木桶最近停在 0.500m。
- `PASS patrol agents do not intersect map uniquePairs=0`：20 秒实时巡逻未检测到人物与地图穿入。
- 关键路径 hero → 长老、井边、4 个巡逻点全部 `PathComplete`；房屋内部仍不可行走；交互禁区间距 1.8m。

## 全量回归

首轮回归发现 `P1ArcherEventOnlyTests` 失败：弓箭手在后退时被追击目的地覆盖，并且 NavMesh 把当前位置当作最近后撤点，导致持续后撤无法恢复射击。追加两处修复：

- `EnemyController.DoMove` 在 `RangedAttack.IsRetreating` 时不再覆盖后撤目的地。
- 后撤候选点必须比当前位置至少多拉开 0.25m，且 `SetDestination` 必须成功，否则退出后撤并恢复射击。

修复后逐帧实测：1.5 秒进入攻击，2.0 秒生成箭，2.25 秒玩家 HP 100→95。旧测试将命中窗口固定为 1.5 秒，已仅把窗口调整为 3 秒，仍严格断言恰好一次命中；没有改伤害、射程或射击事件来迁就测试。原测试备份在 `P1ArcherEventOnlyTests.before-timeout.cs`。

最终结果：

- 既有 12 项玩法回归 **12/12 PASS**（`regression_map_collision_final.log`）。
- 第一章三条剧情路径 **kill / kill_after_truth / spare 全部 PASS**（`story_paths_map_collision_final.log`）。
- 真实战斗后撤射击复测：后撤至 4.757m，箭生成，玩家 HP 100→80（`p2_retreat_integration2.txt`）。
- `RangedAttack.cs` MD5 `b20ef3ed6c563c59dbab18d060eac433`；`EnemyController.cs` MD5 `6c0f22d9f92908ef1d76283b504c820b`。


## 回滚与指纹

备份：`D:\Unity\backups\RPG_Project_2026-11-14_character-map-collision\`（修改前场景及 NavMesh）。

最终：

- 场景 MD5 `e5532d6b006634a432f4a1caef1aba86`
- NavMesh MD5 `065bd3bb5b75d642c5601d1dac0a1a07`
- 证据：`docs/verification/2026-11-14-character-map-collision/`

限制：20 秒巡逻与静态查询不能数学上保证所有动画帧、所有位置都绝无接触；但已覆盖当前场景全部角色、主要地图障碍和实际巡逻路线。真人跑图仍建议用户实机复核。
