# 2026-11-13 场景细节增强 验收报告

工程：`D:\Unity\projects\RPG_Project`
范围：在既有村庄内补土路、植被、生活道具、夜间灯光与自然边界；不扩大可玩区、不改剧情与玩法。

## 1. 交付内容

| 根节点 | 对象数 | 内容 |
|---|---|---|
| `SceneDetails_Ground` | 1（空标记） | Terrain alphamap 刷三条土路；163 棵树随机旋转/宽度 |
| `SceneDetails_Vegetation` | 110 | 草、大草、花、树桩、岩石、南瓜、灌木；15 个实体带碰撞 |
| `SceneDetails_Props` | 104 | 9 个木桶、9 处柴堆（含碰撞条）、4 个木箱、矮桌/水桶、井架/水井桶/绳、火堆长凳 |
| `SceneDetails_Lights` | 10 | 5 盏窗光、3 盏灯笼、2 组火堆烟 |
| `SceneDetails_Border` | 75 | 59 棵带碰撞的边界树 + 16 块岩石，遮住 ±47 隐形墙 |
| 合计 | 299 | 全部新增对象均标记 Static（Batching/Occludee） |

其它改动：

- 场景 Terrain 改用独立副本 `Assets/RPG/RPG_Terrain.asset`；原始 Synty 地形资产已还原为刷路前内容并恢复内部名。
- `basemapDistance` 1000 → 25，使 alphamap 土路在玩家视距内可见。
- `RenderSettings` 开启指数平方雾，density 0.006，颜色 (0.09, 0.11, 0.19)。
- 新增材质资产：`Assets/RPG/Materials/Prop_Wood.mat`、`Prop_Lantern_Glow.mat`。

## 2. 静态校验（`out/validation.txt`）

全部 PASS：

- 对象预算 total=299（≤350）。
- 细节灯光 8 盏、场景点光总数 10（≤10）。
- 雾 density=0.006。
- NavMesh 路径 hero → 长老、井边、4 个巡逻点全部 `PathComplete`。
- 房屋中心仍不在 NavMesh 上（室内不可行走）。
- 新增碰撞体与巡逻点/交互点最小间距 1.8m（≥1.5m）。

## 3. 运行时回归

- 既有 12 项玩法回归：**12/12 PASS**（`out/regression.log`）。
- 第一章剧情三路径冒烟：见第 4 节。

## 4. 剧情路径冒烟

三条第一章路径全部 PASS（`C:\Users\22492\.claude\tools\scene_detail\out\story_paths.log` 与 `story_path_*.json`）：

| 路径 | 结果 | 队长死亡 | 结局后主角控制 |
|---|---|---|---|
| kill | PASS | True | True |
| kill_after_truth | PASS | True | True |
| spare | PASS | False | True |

## 5. 帧时间

Play 模式连续采样 839 帧（编辑器未锁帧，绝对帧率偏高，仅用于横向自查）：

- 平均 `2.905 ms`，p95 `3.44 ms`，最大 `12.05 ms`（均值约 344 fps）。
- **没有同条件的“细节前”基线**（备份场景未在本轮测帧时间），因此不宣称相对退幅；只报告绝对值。若需要严格退幅数据，可把备份场景临时导入 Assets 复测。
- 结论：299 个静态低模对象、10 盏无阴影点光与轻雾没有造成可感知的帧时间负担。

## 6. 资产指纹与备份

- 场景 `RPG游戏.unity` MD5：`50d1b39ba1e84465eac5aabbd4495b51`
- 独立地形 `Assets/RPG/RPG_Terrain.asset` MD5：`43b87f944ec14e03c4774384765c875f`
- 原始 Synty 地形资产已还原，出生点权重 `Green=0.84 / Dirt=0 / LightDirt=0`（刷路前状态），全图泥土均值 0.09。
- 回滚备份：`D:\Unity\backups\RPG_Project_2026-11-13_scene-detail\`

## 7. 截图

归档于 `docs/verification/2026-11-13-scene-detail/`：

- `final_spawn.png`、`final_road.png`、`final_well.png`、`final_houses.png`、`final_border.png`、`final_overview.png`
- `road_v2_spawn.png`、`road_v2_top.png`（土路重刷时的近景/俯视）

## 8. 已知限制与未覆盖项

- 真人键鼠手感与主观美术评价仍需用户实玩。
- 草/花无碰撞（刻意，保持可穿行）；灌木在夜景中呈深色剪影，是素材本身的表现。
- 房屋模型从上方看可看穿屋顶，属既有素材问题，本轮未改。
- 主角头顶世界空间血条过宽、潜行仍播普通走路动画、攻击动画为双手剑动作——三项既有观感问题本轮未处理。
- 雾密度与灯光亮度按截图目检调过，若用户实机觉得过暗，可调 `RenderSettings.fogDensity` 与窗光 intensity。

## 9. 回滚

1. 删除场景根节点 `SceneDetails_Ground`、`_Vegetation`、`_Props`、`_Lights`、`_Border`。
2. 从 `D:\Unity\backups\RPG_Project_2026-11-13_scene-detail\` 还原 `RPG游戏.unity`、`RPG_Terrain.asset`、`progress.md`、`HeroDirectMovement.cs`。
3. `basemapDistance`、`RenderSettings`、树实例变换与火堆光强一并随场景备份还原。
