# RPG_Project 表现层修复验收记录

日期：2026-11-08

## 已实施

1. `HeroCamera.cs`：滚轮缩放 4～12 米、中键拖动水平旋转、保留右键攻击、简单遮挡检测、平滑跟随。
2. `RPG游戏.unity` 血条：Hero 锚点高度 2.05、Enemy 2.00；缩放均由 0.01 改为 0.008。
3. 同场景光照：环境光强度 1.00→1.12，方向光 1.00→1.08。未改天空盒、材质、烘焙或画质。

## 验证证据

### 相机

- 修复前运行 `CameraControlTests.cs`：按预期失败，`camera zoom/orbit implementation is missing`。
- 修复后编译成功，测试输出 `CAMERA CONTROL TESTS PASS`。
- Play 内运行时测试输出 `CAMERA RUNTIME TEST PASS`：
  - MIN：目标/实际距离均为 4.000。
  - MAX：目标/实际距离均为 12.000。
  - ORBIT：yaw=50.000，相机 Y 角由 178.02 变为 228.02，`tarEnemy=NULL`。
  - RESTORED：距离与朝向回到默认值。
- **限制**：Bridge 的真实鼠标模拟需要项目未安装的 Input System 包。本轮没有新增依赖，因此只证明相机逻辑和 Play 内运行结果，**不算真人鼠标操作验收**。

### 血条

- 第一次保存后重载发现高度回退：RectTransform 的旧 `anchoredPosition` 覆盖了 localPosition。
- 改为写 `anchoredPosition` 后再次保存、从磁盘重载，两个高度均保持目标值；场景文件中的 scale 均为 0.008。
- `hp-after.png` 显示血条更短、更低，不遮挡角色头部。
- 后续战斗回归中 Slider 数值仍随双方伤害变化。

### 战斗与流程

- 最终战斗回归：敌人血量出现 100/80/60/40/20/0 并最终销毁；主角血量 100→80；无新增战斗异常。
- 最终死亡回归：双次致命伤害后 HP=0、只死亡一次，尸体位移 0.000，死亡动画播放，2 秒后销毁。
- 最终场景切换：`RPG游戏 → 界面 → RPG游戏` 成功，返回后 Hero 与 Enemy 均重新存在。
- 最终编译成功，相机测试再次通过。

### 光照

- 同视角对比 `hp-after.png` 与 `lighting-after.png`：略亮，无过曝，暗部和阴影仍可辨认。
- 光照保存前后 YAML 差异只有 `m_AmbientIntensity: 1→1.12` 和方向光 `m_Intensity: 1→1.08`。

## 仍未解决/不属本轮范围

- 控制台仍有既有的 Progressive GPU 烘焙后端提示和地形树 Soft Occlusion 提示。
- 场景切换仍出现一次 `The referenced script (Unknown) on this Behaviour is missing!`，无堆栈；此前对两个场景的全量 Missing Script 扫描均为 0，本轮未将其宣称为已修复。
- 真实鼠标、键盘手感仍需用户亲自操作验收。
- 玩法和 UI 建议见 `docs/2026-11-08-gameplay-ui-opportunities.md`，这些系统尚未实现。

## 文件

- 截图：`docs/verification/2026-11-08/`。
- 回滚备份：`D:\Unity\backups\RPG_Project_2026-11-08_presentation\`。
  - `HeroCamera.before.cs`
  - `RPG游戏.before-hp.unity`
  - `RPG游戏.before-lighting.unity`
