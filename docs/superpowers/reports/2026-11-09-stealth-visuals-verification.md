# RPG_Project 潜行场景与视觉优化验收

日期：2026-11-09

## 已交付

### 第三人称探索相机

- HeroCamera.offset 改为 `(0,3.2,4.2)`。
- 初始相机 pitch 40.20°→25°，yaw 保持 178.02°。
- 保留中键旋转、滚轮 4～12 米、平滑跟随与遮挡检测。
- 前后截图显示更低、更近的探索视角，能看到更多前方环境。

### 黄昏光照与路线火点

- 使用现有 `Skybox Cubemap Extended Night.mat`，无新增外部资源。
- ambientIntensity 1.12→0.85；方向光 1.08→0.65，颜色改为柔和冷蓝。
- 新增 StealthLighting 下两个 SimpleFX 火点；Point Light range=5、intensity=1、warm color、无实时阴影。
- 截图中角色、地面、房屋轮廓仍可辨认；未出现材质缺失的粉色物体。

### 可通行掩体与击杀反馈

- StealthCoverVisuals 下新增 10 处高草/灌木簇，按地形高度落地。
- 所有视觉植被 Collider 均禁用，不改 NavMesh，不阻挡直接移动。
- 击杀效果由显眼黄色烟花改为 `FX_Dust_Prefab_02`，保留 2.5 秒自动销毁。
- 近景与拉远截图均已归档；植被当前只是视觉掩体，尚无隐藏判定。

## 验证证据

- 修改前相机断言按预期失败，应用后 `STEALTH CAMERA EDITOR PASS`。
- 修改前夜景/火点断言按预期失败，应用后 `STEALTH LIGHTING EDITOR PASS`。
- 修改前掩体/击杀资源断言按预期失败，应用后 `STEALTH COVER EDITOR PASS`。
- CameraControlTests 通过；固定步长移动保持 walk=0.4、run=0.6、diagonal=0.4。
- DirectCollisionTest 通过，along=1.42；DirectAttackTests 通过。
- 击杀尘土重复致命 Hurt 只生成一次，2.7 秒后检查为 0。
- PotionRuntimeTests、FullHudTests、InventoryUITests、PauseMenuTests、DeathFlowTests、SceneSwitchProbe 均通过。
- 旧点选模式延长观察后敌人 DESTROYED、主角 HP=80；一次 18 秒快照仍处于交战，不作为失败判据，也未修改攻击参数。

## 测试夹具修正

直接控制测试曾把 directControl=false 但保持 NavMeshAgent 禁用，可能让旧点选分支接到点击并报 SetDestination 错误。已改为只暂停 HeroController 输入 Update，保持组件状态一致；重跑移动、碰撞和奔跑动画均通过。生产代码未因此改动。

## 限制与遗留

- 本阶段没有实现卫兵视野、警戒、搜索、草丛隐藏、刺杀或任务。
- 真实视觉体验和键鼠手感仍需用户试玩。
- 既有烘焙后端、地形树 Soft Occlusion、场景切换 Unknown Missing Script 提示仍存在，未宣称清零。
- 夜景目前偏冷暗；若用户试玩觉得过暗，应优先小幅提高 ambient，而不是一次改更多光照参数。

## 归档

- 截图：`docs/verification/2026-11-09-stealth-visuals/`
- 备份：`D:\Unity\backups\RPG_Project_2026-11-09_stealth-visuals\`
- 进度：`docs/progress.md`
- 下一项：卫兵感知 AI；植被隐藏判定属于后续潜行阶段。
