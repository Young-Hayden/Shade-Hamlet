# 2026-11-14 第一章收尾 polish 验收

范围：第一章遗留的夜景植被可读性、灯笼安装和 UI 越界复核。不新增玩法，不涉及第二章。

## 修改

- 新增 `Vegetation_Adventure_Night.mat`、`Vegetation_Vikings_Night.mat`，仅用于草、灌木和花，保留原贴图，加入低强度夜间补光；100 个相关 Renderer 全部覆盖，原 Synty 共享材质未改。
- 三盏灯笼各新增 `LanternArm`，连接挂钩与最近房屋墙面，无碰撞体，不参与 NavMesh。
- UI 未改动：正确的屏幕投影复核未发现 Overlay 控件真正越界；只有离屏角色的世界空间血条位于屏幕外，属正常行为。此前将世界坐标当屏幕像素的报告是误报。

## 验证

修复前：

- `FAIL vegetation night materials 0/100`
- `FAIL lantern arms 0/3`

修复后：

- `PASS vegetation night materials 100/100`
- `PASS lantern arms 3/3`

截图：

- `polish_before_vegetation.png` / `polish_after_vegetation.png`
- `polish_lantern_0.png`、`polish_lantern_1.png`、`polish_lantern_2.png`

文件：`docs/verification/2026-11-14-chapter1-polish/`

## 回归

- 既有 12 项玩法回归 **12/12 PASS**（`regression_chapter_polish.log`）。
- 第一章三条剧情路径 **kill / kill_after_truth / spare 全部 PASS**（`story_paths_chapter_polish.log`）。
- 同视角亮度抽样：暗部像素占比由 36.3% 降至 13.7%（luminance < 0.15），仅用于本次前后对比。
- 最终编辑态、场景无未保存改动。


## 指纹与回滚

- 场景 MD5 `154103b8e911b95ad1c3aebf30a76d6e`
- `Vegetation_Adventure_Night.mat` MD5 `7ef3caf6b6e8109acc51ca309a31d32a`
- `Vegetation_Vikings_Night.mat` MD5 `b84a7f2c6715222f55072921fe468905`
- 修改前场景：`D:\Unity\backups\RPG_Project_2026-11-14_chapter1-polish\RPG游戏.before-polish.unity`

限制：亮度按截图目检选择，真人实机对夜景的主观评价仍待用户确认。
