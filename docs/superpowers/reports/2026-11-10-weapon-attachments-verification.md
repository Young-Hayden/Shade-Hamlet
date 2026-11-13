# 武器挂点修复验收

日期：2026-11-10

## 根因

- StealthRapierVisual 原挂 Hero/Attack1pos，ArcherBowVisual 原挂 Archer/Attack2pos；这些是攻击偏移点，不是手骨。
- 正确右手为 `.../Clavicle_R/Shoulder_R/Elbow_R/Hand_R`。
- 角色脊柱上的 Item_Sword/Item_SwordHolder 渲染器还造成背部重复武器。

## 修复

- 细剑和弓统一挂到 `Animator.GetBoneTransform(HumanBodyBones.RightHand)`，局部位置/旋转/缩放归零。
- 禁用 Item_Sword/Item_SwordHolder 渲染器；不改变攻击盒、箭矢发射逻辑或动画事件。

## 验证

- 修复前 `FAIL: rapier is not attached to right hand`。
- 修复后 `WEAPON ATTACHMENT TESTS PASS`。
- DirectAttackTests、AssassinationTests 通过。
- `weapons-fixed.png` 与 `archer-weapon-fixed.png` 已归档；背部重复武器已移除。

备份：`D:\Unity\backups\RPG_Project_2026-11-10_weapon-attachments\`。
