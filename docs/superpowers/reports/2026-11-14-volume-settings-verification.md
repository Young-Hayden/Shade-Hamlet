# 2026-11-14 音量设置功能验收

范围：主菜单和暂停菜单增加“设置”入口，调整游戏主音量并持久化。

## 实现

- `AudioSettings`：主音量 0–1，应用到 `AudioListener.volume`；通过 `PlayerPrefs` 键 `masterVolume` 持久化，启动前自动加载。
- `VolumeSettingsPanel`：统一的音量面板，包含“主音量”滑条、百分比显示和关闭按钮；关闭时保存。
- `VolumeSettingsLauncher`：主菜单打开面板。
- 主菜单新增“设置”按钮，菜单按钮顺序调整为“开始游戏 / 设置 / 退出游戏”。
- 暂停菜单新增“音量设置”按钮；面板作为暂停画布的子层显示，Esc 优先关闭音量面板，再关闭暂停菜单。
- 顺带清理了主菜单重建时残留的旧 `Button (Legacy)`、`StartSubtitle`，并补齐 `MenuSubtitle`。

## 验证

静态/接口：

- `PASS AudioSettings type`
- `PASS volume API`
- `PASS menu settings button`
- `PASS menu launcher`
- 主菜单布局 7 项全 PASS（含 3 个按钮及持久化接线）

主菜单运行时：

- 设置按钮监听器 = 1
- 面板打开、滑条存在
- 设置为 35%：`AudioListener.volume=0.35`、标签 `35%`、`PlayerPrefs masterVolume=0.35`
- 关闭后面板隐藏

暂停菜单运行时：

- 暂停打开、音量按钮存在
- 面板打开、滑条生效为 60% 且标签 `60%`
- 关闭后回到暂停菜单
- Esc 优先关闭音量面板，暂停菜单保持打开

截图：

- `volume_panel_menu.png`
- `volume_panel_pause.png`

## 用户反馈修复（2026-11-14 复测）

1. **滑条实际上不可拖动**：`RpgUI.CreateSlider` 默认把 `interactable` 设为 false（原用于血条），音量面板直接复用了它，且没有拖拽手柄。已改为 `interactable=true`，添加 30×30 手柄并绑定 `handleRect`。
2. **主界面没有音乐**：`界面.scene` 有 AudioListener 但没有任何 AudioSource。已添加 `MenuMusic`，播放 `BGM1`、循环、`playOnAwake=true`、基础音量 0.65，并受主音量统一控制。
3. **面板排版**：`主音量` 与百分比文本原先以中心点定位，文字会跑出面板；改为左右锚点并修正位置。

真实指针事件验证：

- 菜单 BGM `isPlaying=True`、clip=BGM1
- 用 PointerEventData 对滑条执行 `pointerDown + drag`，值变为 **0.25**、`AudioListener.volume=0.25`、标签 `25%`
- `interactable=True`、`handleRect` 存在
- `volume_panel_menu.png` 已更新为带手柄、文字在面板内的版本

## 回归


- 既有玩法回归 **11/11 PASS**（含暂停菜单、HUD、死亡重开、场景切换；滑条与 BGM 修复后复跑）。
- 第一章三条剧情路径 **kill / kill_after_truth / spare 全部 PASS**。
- 最终编辑态、`RPG游戏` 场景无未保存改动。

指纹：

- `RPG游戏.unity`：`0bba043a98a82fccf3f2009b386e31b9`
- `界面.scene`：`d1c38646b9ed2ef48bdee53042cf9fc2`
- `AudioSettings.cs`：`c468b52cac630612b7180d36a428ee15`
- `VolumeSettingsPanel.cs`：`dab82d24cc89d650db2cde45ec2cc1ed`
- `VolumeSettingsLauncher.cs`：`81ebbd349c3e27ca30992813341d39ef`
- `PauseMenu.cs`：`05af6f164a72e7cf0b58cb41d0ac0c4b`


## 说明与限制

- 音量通过 `AudioListener.volume` 统一控制 BGM 与现有攻击/死亡音效；没有拆成音乐/音效两个滑条。
- 自动化只能验证数值与 UI 行为，实际响度平衡仍需用户戴耳机/音箱试听。
- 主菜单“开始游戏”按钮在新布局下已重新验证可正常触发黑场过渡并进入 RPG。
