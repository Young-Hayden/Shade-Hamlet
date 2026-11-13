# RPG_Project 潜行场景与视觉优化设计

日期：2026-11-09
状态：方案 A（黄昏潜行）已获用户批准

## 目标

利用现有相机、夜景天空盒、SimpleFX 和 Synty 植被，让当前村庄更接近第三人称潜行动作游戏，同时保持角色可读性、通行和现有系统稳定。

## 阶段 1：第三人称探索相机

- HeroCamera.offset 从当前默认 `(0,5,4)` 改为 `(0,3.2,4.2)`。
- 场景 Camera 初始旋转 X 从 40.20°调整为 25°，Y 保持 178.02°，HeroCamera.Awake 继续读取它作为 baseRotation。
- 中键旋转、4～12 米缩放、SmoothDamp 和遮挡 Raycast 不改算法。
- 验收：角色完整可见、前方视野增加、缩放边界仍正确、旋转不穿入角色；对比截图。

## 阶段 2：黄昏光照与暖色引导

- RenderSettings.skybox 使用现有 `Assets/MainAsset/MainAsset/Asset Packs/BOXOPHOBIC/Skybox Cubemap Extended/Demo/Materials/Skybox Cubemap Extended Night.mat`。
- ambientIntensity 从 1.12 降为 0.85；方向光 intensity 从 1.08 降为 0.65，颜色使用柔和的冷蓝 `(0.65,0.75,1.0,1.0)`。
- 不改 LightingSettings、烘焙后端、烘焙贴图、阴影距离或地形树材质。
- 使用现有 `SimpleFX/Prefabs/FX_Fire.prefab` 或 Synty FX_Fire 作为视觉火点，只放少量；如添加 Light，则 warm color `(1.0,0.55,0.25)`、range 5、intensity 1.0，并逐张截图检查。
- 火点位置从现有建筑/路口的实际位置计算，不悬空、不挡角色；不新增会改变 NavMesh 的碰撞体。

## 阶段 3：潜行掩体视觉与特效收敛

- 新建场景父物体 `StealthCoverVisuals`，复用 `SM_Env_Grass_Large_01`、`SM_Env_Bush_01`。
- 在当前巡逻路线两侧布置少量高草/灌木簇，形成可通行的视觉遮蔽通道；不以密植堵路。
- 植被通过 Terrain.SampleHeight/地形高度落地；本阶段不新增 Collider，不写隐藏判定，不改 NavMesh。
- `CombatFeedback.killEffect` 从黄色烟花改为 `SimpleFX/Prefabs/FX_Dust_Prefab_02.prefab`；虽然该粒子可循环，现有 Destroy 2.5 秒仍负责回收。
- 保留命中弹字、血条和 HUD，不新增警戒图标或刺杀提示。

## 非目标

- 不实现卫兵视野、警戒、搜索、草丛隐藏、刺杀或任务。
- 不添加跑酷、攀爬、蹲伏动画。
- 不修改共享 Synty 材质，不处理既有地形树 Soft Occlusion 告警。
- 不新增外部素材或 Input System 依赖。

## 验证与保存

1. 每阶段先记录当前值/截图，修改后保存并回读。
2. 相机缩放/旋转测试、直接移动固定步长与墙体碰撞测试保持通过。
3. 检查天空盒无粉色、场景不过暗、火点不悬空、植被不堵主要通道。
4. 击杀尘土只生成一次并自动销毁；战斗、药水、HUD、背包、暂停、死亡和场景往返回归。
5. 每阶段备份场景、更新 progress.md；最终输出默认分辨率对比截图与验收报告。
6. 真人视觉体验和键鼠手感仍需用户试玩。

## 回滚

备份目录：`D:\Unity\backups\RPG_Project_2026-11-09_stealth-visuals\`。相机、光照、掩体分别保留修改前场景；任何阶段视觉变差可独立还原。工程无 Git，不提交。
