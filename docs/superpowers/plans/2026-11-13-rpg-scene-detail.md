# 《影村》场景细节增强 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在不扩大可玩区、不改剧情与玩法的前提下，给现有村庄补上土路、植被、生活道具、夜灯氛围与自然边界。

**Architecture:** 全部新增内容由编辑态一次性 C# 工具脚本生成，按 `SceneDetails_*` 根节点分组；只改 Terrain alphamap、树实例变换与两处火堆点光强度。每步先写断言脚本（RED），再生成（GREEN），最后保存场景并记录 MD5。

**Tech Stack:** Tuanjie 2022.3.62t5、C# one-shot 脚本（`execute_csharp_script`）、`rpg-bridge.mjs`、Unity Terrain API、Synty/GameDev.tv/SimpleFX 现有素材。

## Global Constraints

- 不扩大地形，不新增玩法、不改剧情资产与章节逻辑。
- 新增装饰不占道路、巡逻点、线索/对话/井边对峙点；安全间距 ≥ 1.5m（道路 ≥ 1.2m）。
- 树、木桶、柴堆、岩石、倒木、桌椅有碰撞；草、花无碰撞。
- 实时点光总数 ≤ 10，全部无阴影；新增静态装饰对象 ≤ 350（实施实测 299）。
- 所有新增对象挂在 `SceneDetails_Ground` / `_Vegetation` / `_Props` / `_Lights` / `_Border` 下。
- 不改 `StealthCoverVisuals`、`StealthCoverAreas`、`StealthPhysicalCover`、巡逻点、剧情对象位置。
- 不改现有 Directional Light 与 ambient 基调；不改角色、武器、动画、战斗数值。
- 当前场景基线 MD5 `4332c3c9d5c1a01697e2be87bcfbc902`，Terrain `e2f7f8c0c4e135414db10b1be9bf524a`；回滚备份 `D:\Unity\backups\RPG_Project_2026-11-13_scene-detail\`。

---

## 文件结构

| 文件 | 职责 |
|---|---|
| `C:\Users\22492\.claude\tools\scene_detail\DetailCommon.cs` | 共享：根节点、路径点、禁用区、木质材质、静态标记、保存 |
| `...\BackupAndBaseline.cs` | 备份与基线断言 |
| `...\BuildGround.cs` | 土路 alphamap + 树随机化 |
| `...\BuildVegetation.cs` | 草花岩石树桩倒木 |
| `...\BuildProps.cs` | 生活道具与程序化低模 |
| `...\BuildLights.cs` | 窗光、灯笼、火堆烟、雾 |
| `...\BuildBorder.cs` | 边界林带 |
| `...\ValidateDetails.cs` | 静态断言 + NavMesh 路径 + 禁区 |
| `...\RunSceneDetailRegression.py` | 12 项回归 + 三剧情路径 + 帧时间 |

日志/截图统一写到 `C:\Users\22492\.claude\tools\scene_detail\out\`。

---

### Task 1: 备份与基线断言

**Files:**
- Create: `C:\Users\22492\.claude\tools\scene_detail\BackupAndBaseline.cs`

**Interfaces:**
- Produces: `out\baseline.txt`，含场景/Terrain MD5、根节点计数（应为 0）、关键路径点坐标。

- [ ] **Step 1: 写基线脚本**

```csharp
using System.IO;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;

var sb = new StringBuilder();
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
sb.AppendLine("scene=" + scene.path + " dirty=" + scene.isDirty);
var hero = Object.FindObjectOfType<HeroController>();
var elder = Object.FindObjectOfType<NpcInteractor>();
var director = Object.FindObjectOfType<StoryDirector>();
var letter = Object.FindObjectOfType<CluePickup>();
sb.AppendLine("hero=" + hero.transform.position);
sb.AppendLine("elder=" + elder.transform.position);
sb.AppendLine("storyPoint=" + director.storyPoint.position);
sb.AppendLine("letter=" + letter.transform.position);
foreach (var n in new[] { "SceneDetails_Ground", "SceneDetails_Vegetation", "SceneDetails_Props", "SceneDetails_Lights", "SceneDetails_Border" })
    sb.AppendLine(n + " exists=" + (GameObject.Find(n) != null));
Directory.CreateDirectory("C:/Users/22492/.claude/tools/scene_detail/out");
File.WriteAllText("C:/Users/22492/.claude/tools/scene_detail/out/baseline.txt", sb.ToString());
return sb.ToString();
```

- [ ] **Step 2: 跑基线，确认 5 个根节点都不存在**

Run:

```bash
node C:/Users/22492/.claude/tools/rpg-bridge.mjs execute_csharp_script '{"script_path":"C:/Users/22492/.claude/tools/scene_detail/BackupAndBaseline.cs","execution_mode":"editor","capture_logs":true}'
```

Expected: `SceneDetails_* exists=False`，并打印井边点坐标。

- [ ] **Step 3: 备份**

用 Python `shutil.copy2` 备份 `RPG游戏.unity`、`RPG_Terrain.asset`、`docs/progress.md`、`Assets/Script/HeroDirectMovement.cs` 到 `D:\Unity\backups\RPG_Project_2026-11-13_scene-detail\`，逐项核对 MD5 与源文件一致。

---

### Task 2: 地面土路与树随机化

**Files:**
- Create: `...\BuildGround.cs`、`...\ValidateGround.cs`

**Interfaces:**
- Consumes: Task 1 的 `baseline.txt` 坐标。
- Produces: 根节点 `SceneDetails_Ground`（仅作标记，无渲染物）；Terrain alphamap 改变；163 棵树实例旋转/宽度改变。

- [ ] **Step 1: 写失败断言**

```csharp
using System.Linq;
using UnityEngine;
var td = Object.FindObjectOfType<Terrain>().terrainData;
int dirt = System.Array.FindIndex(td.terrainLayers, l => l.name == "Dirt layer");
float[,,] m = td.GetAlphamaps(0, 0, td.alphamapResolution, td.alphamapResolution);
float dirtSum = 0f;
for (int y = 0; y < td.alphamapResolution; y++)
    for (int x = 0; x < td.alphamapResolution; x++)
        dirtSum += m[y, x, dirt];
bool hasGround = GameObject.Find("SceneDetails_Ground") != null;
string result = (dirtSum / (td.alphamapResolution * td.alphamapResolution) > 0.02f && hasGround)
    ? "PASS ground detail" : "FAIL ground detail dirtAvg=" + dirtSum / (td.alphamapResolution * td.alphamapResolution) + " root=" + hasGround;
System.IO.File.WriteAllText("C:/Users/22492/.claude/tools/scene_detail/out/ground_test.txt", result);
return result;
```

- [ ] **Step 2: 运行，确认 FAIL**

Expected: `FAIL ground detail`（当前 Dirt 层占比 < 0.02）。

- [ ] **Step 3: 写生成脚本**

```csharp
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

static float SegmentDistance(Vector3 p, Vector3 a, Vector3 b)
{
    Vector3 ab = b - a; ab.y = 0f;
    Vector3 ap = p - a; ap.y = 0f;
    float t = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
    return Vector3.Distance(ap, ab * t);
}

var terrain = Object.FindObjectOfType<Terrain>();
var td = terrain.terrainData;
var hero = Object.FindObjectOfType<HeroController>().transform.position;
var elder = Object.FindObjectOfType<NpcInteractor>().transform.position;
var well = Object.FindObjectOfType<StoryDirector>().storyPoint.position;
var mainStreet = new[] { new Vector3(-2f, 0f, 17f), new Vector3(14f, 0f, 12f), new Vector3(14f, 0f, -8f) };
var roads = new List<Vector3[]>
{
    new[] { hero, new Vector3(-6f, 0f, -18f), well },
    new[] { hero, elder },
    mainStreet,
};

int dirtLayer = System.Array.FindIndex(td.terrainLayers, l => l.name == "Dirt layer");
int lightDirtLayer = System.Array.FindIndex(td.terrainLayers, l => l.name == "Light dirt layer");
int res = td.alphamapResolution;
var maps = td.GetAlphamaps(0, 0, res, res);
float cellX = td.size.x / res, cellZ = td.size.z / res;
for (int y = 0; y < res; y++)
{
    for (int x = 0; x < res; x++)
    {
        Vector3 world = new Vector3(terrain.transform.position.x + (x + 0.5f) * cellX, 0f, terrain.transform.position.z + (y + 0.5f) * cellZ);
        float best = float.MaxValue;
        foreach (var road in roads)
            for (int i = 0; i < road.Length - 1; i++)
                best = Mathf.Min(best, SegmentDistance(world, road[i], road[i + 1]));
        float w = best <= 1.25f ? 0.85f : best <= 2.5f ? Mathf.Lerp(0.85f, 0f, (best - 1.25f) / 1.25f) : 0f;
        if (w <= 0f) continue;
        int layer = (x + y) % 2 == 0 ? dirtLayer : lightDirtLayer;
        maps[y, x, layer] = Mathf.Max(maps[y, x, layer], w);
        float total = 0f;
        for (int l = 0; l < td.terrainLayers.Length; l++) total += maps[y, x, l];
        for (int l = 0; l < td.terrainLayers.Length; l++) maps[y, x, l] /= Mathf.Max(total, 0.0001f);
    }
}
td.SetAlphamaps(0, 0, maps);

// 树随机化：只改旋转与宽度，不改高度；保留前 20 棵边界外的树不动
var trees = td.treeInstances;
var rng = new System.Random(20261113);
for (int i = 20; i < trees.Length; i++)
{
    var t = trees[i];
    t.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
    float w = 0.88f + (float)rng.NextDouble() * 0.24f;
    t.widthScale = w; t.heightScale = Mathf.Clamp(t.heightScale, 0.9f, 1.1f);
    trees[i] = t;
}
td.treeInstances = trees;

var root = GameObject.Find("SceneDetails_Ground");
if (root == null) root = new GameObject("SceneDetails_Ground");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
AssetDatabase.SaveAssets();
return "ground built: dirtLayer=" + dirtLayer + " lightDirt=" + lightDirtLayer + " res=" + res + " trees=" + trees.Length;
```

- [ ] **Step 4: 运行生成脚本，再跑 Step 1 断言**

Expected: `PASS ground detail`；日志显示 dirtLayer 索引有效。

- [ ] **Step 5: 截图对比**

用 `Shots.cs` 同款临时相机渲染 `out/before_ground.png`（改造前备份场景不可直接渲染，因此只拍改造后）与 `out/after_ground.png`，人工目检土路是否自然、无方格。

---

### Task 3: 植被与自然物

**Files:**
- Create: `...\BuildVegetation.cs`、`...\ValidateVegetation.cs`

**Interfaces:**
- Consumes: 道路线段（与 Task 2 相同坐标计算复制一份，脚本独立）。
- Produces: `SceneDetails_Vegetation` 下 90–120 个对象；草/花无碰撞，树桩/岩石/倒木有碰撞。

- [ ] **Step 1: 失败断言**

```csharp
var root = GameObject.Find("SceneDetails_Vegetation");
int total = root == null ? 0 : root.transform.childCount;
int colliders = root == null ? 0 : root.GetComponentsInChildren<Collider>().Length;
string result = (total >= 90 && colliders >= 10) ? "PASS vegetation total=" + total + " colliders=" + colliders
    : "FAIL vegetation total=" + total + " colliders=" + colliders;
System.IO.File.WriteAllText("C:/Users/22492/.claude/tools/scene_detail/out/vegetation_test.txt", result);
return result;
```

Expected: FAIL total=0。

- [ ] **Step 2: 生成脚本**

```csharp
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

static float SegDist(Vector3 p, Vector3 a, Vector3 b)
{
    Vector3 ab = b - a; ab.y = 0; Vector3 ap = p - a; ap.y = 0;
    float t = ab.sqrMagnitude < 0.0001f ? 0 : Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
    return Vector3.Distance(ap, ab * t);
}

const string Base = "Assets/MainAsset/MainAsset/Asset Packs/Synty - Polygon Student Sample/Prefabs/";
var grass = AssetDatabase.LoadAssetAtPath<GameObject>(Base + "SM_Env_Grass_01.prefab");
var flower = AssetDatabase.LoadAssetAtPath<GameObject>(Base + "SM_Env_Flower_01.prefab");
var stump = AssetDatabase.LoadAssetAtPath<GameObject>(Base + "SM_Env_TreeLog_01.prefab");
var rock = AssetDatabase.LoadAssetAtPath<GameObject>(Base + "SM_Env_Rock_015.prefab");
var pumpkin = AssetDatabase.LoadAssetAtPath<GameObject>(Base + "SM_Prop_Pumpkin_01.prefab");
var root = GameObject.Find("SceneDetails_Vegetation");
if (root == null) root = new GameObject("SceneDetails_Vegetation");

var terrain = Object.FindObjectOfType<Terrain>();
var hero = Object.FindObjectOfType<HeroController>().transform.position;
var elder = Object.FindObjectOfType<NpcInteractor>().transform.position;
var well = Object.FindObjectOfType<StoryDirector>().storyPoint.position;
var letter = Object.FindObjectOfType<CluePickup>().transform.position;
var patrols = new[] { "enemyPos1", "enemyPos2", "enemyPos3", "enemyPos4" }
    .Select(n => GameObject.Find(n).transform.position).ToArray();
var roads = new List<Vector3[]>
{
    new[] { hero, new Vector3(-6f, 0, -18f), well },
    new[] { hero, elder },
    new[] { new Vector3(-2f, 0, 17f), new Vector3(14f, 0, 12f), new Vector3(14f, 0, -8f) },
};
var buildings = GameObject.Find("Environment").GetComponentsInChildren<Renderer>()
    .Where(r => r.name.StartsWith("SM_Bld")).Select(r => r.bounds).ToArray();
var rng = new System.Random(20261113);
int placed = 0, colliderCount = 0, attempts = 0;
while (placed < 110 && attempts < 4000)
{
    attempts++;
    float x = (float)(rng.NextDouble() * 76 - 38);
    float z = (float)(rng.NextDouble() * 76 - 30);
    Vector3 p = new Vector3(x, 0f, z);
    float groundY = terrain.SampleHeight(p) + terrain.transform.position.y;
    p.y = groundY;
    bool bad = false;
    foreach (var road in roads)
        for (int i = 0; i < road.Length - 1 && !bad; i++)
            if (SegDist(p, road[i], road[i + 1]) < 2.0f) bad = true;
    foreach (var b in buildings)
        if (new Bounds(b.center, b.size).Contains(new Vector3(p.x, b.center.y, p.z))) bad = true;
    foreach (var q in patrols.Concat(new[] { elder, letter, well }))
        if (Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(q.x, 0, q.z)) < 2.0f) bad = true;
    if (bad) continue;

    bool isRock = placed % 11 == 0;
    bool isStump = placed % 13 == 0;
    bool isFlower = placed % 3 == 0;
    var prefab = isRock ? rock : isStump ? stump : isFlower ? flower : grass;
    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
    go.transform.position = p;
    go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
    go.transform.localScale = Vector3.one * (0.8f + (float)rng.NextDouble() * 0.5f);
    if (isRock || isStump)
    {
        var mf = go.GetComponentInChildren<MeshFilter>();
        if (mf != null) { var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; }
        colliderCount++;
    }
    placed++;
}
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
return "vegetation placed=" + placed + " colliders=" + colliderCount + " attempts=" + attempts;
```

- [ ] **Step 3: 运行生成 + 断言**

Expected: `PASS vegetation total=110 colliders≈20`。

- [ ] **Step 4: 目检截图**

拍 `out/after_vegetation.png`（出生点前方），确认草不挡路、岩石不悬空、树桩落地。

---

### Task 4: 生活道具

**Files:**
- Create: `...\BuildProps.cs`、`...\ValidateProps.cs`

**Interfaces:**
- Consumes: 木质材质取自 `Environment/SM_Prop_Crate_01` 的 `MeshRenderer.sharedMaterial`。
- Produces: `SceneDetails_Props` 下每栋房 2–4 件、井边一组、火堆旁一组；木桶/柴堆/矮桌/水桶架有 BoxCollider。

- [ ] **Step 1: 失败断言**

```csharp
var root = GameObject.Find("SceneDetails_Props");
int total = root == null ? 0 : root.transform.childCount;
int boxes = root == null ? 0 : root.GetComponentsInChildren<BoxCollider>().Length;
string result = (total >= 24 && boxes >= 10) ? "PASS props total=" + total + " boxColliders=" + boxes
    : "FAIL props total=" + total + " boxColliders=" + boxes;
System.IO.File.WriteAllText("C:/Users/22492/.claude/tools/scene_detail/out/props_test.txt", result);
return result;
```

- [ ] **Step 2: 生成脚本（含程序化低模）**

```csharp
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

static Material Wood(Transform env)
{
    var crate = env.GetComponentsInChildren<MeshRenderer>().FirstOrDefault(r => r.name.StartsWith("SM_Prop_Crate_01"));
    return crate != null ? crate.sharedMaterial : new Material(Shader.Find("Standard")) { color = new Color(0.35f, 0.24f, 0.15f) };
}

static GameObject Prim(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
{
    var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    go.name = name; go.transform.SetParent(parent); go.transform.position = pos;
    go.transform.localScale = scale; go.transform.rotation = Quaternion.Euler(euler);
    go.GetComponent<Renderer>().sharedMaterial = mat;
    var col = go.GetComponent<Collider>(); Object.DestroyImmediate(col);
    return go;
}

static GameObject BoxProp(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collide = true)
{
    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
    go.name = name; go.transform.SetParent(parent); go.transform.position = pos; go.transform.localScale = size;
    go.GetComponent<Renderer>().sharedMaterial = mat;
    if (!collide) Object.DestroyImmediate(go.GetComponent<Collider>());
    return go;
}

var env = GameObject.Find("Environment").transform;
var mat = Wood(env);
var root = GameObject.Find("SceneDetails_Props");
if (root == null) root = new GameObject("SceneDetails_Props");
var terrain = Object.FindObjectOfType<Terrain>();
float Y(Vector3 p) => terrain.SampleHeight(p) + terrain.transform.position.y;
int barrels = 0, piles = 0, extras = 0;

// 每栋房屋：门口外侧 1.6m 放 2-3 件
foreach (var house in env.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.StartsWith("SM_Bld_Hut") || r.name.StartsWith("SM_Bld_Village")).Take(9))
{
    var b = house.bounds;
    Vector3 toCenter = new Vector3(-b.center.x, 0f, -b.center.z);
    if (toCenter.sqrMagnitude < 0.01f) toCenter = Vector3.forward;
    toCenter.Normalize();
    Vector3 door = b.center + toCenter * (Mathf.Max(b.extents.x, b.extents.z) + 1.6f);
    door.y = Y(door);
    // 木桶
    var barrel = Prim("Prop_Barrel", root.transform, door + Vector3.right * 0.7f, new Vector3(0.28f, 0.34f, 0.28f), mat);
    barrel.AddComponent<BoxCollider>().size = new Vector3(0.9f, 1.6f, 0.9f);
    barrels++;
    // 柴堆：4 根圆柱并排
    for (int i = 0; i < 4; i++)
    {
        var log = Prim("Prop_Firewood", root.transform, door + new Vector3(-0.5f + i * 0.18f, 0.1f, 0.2f), new Vector3(0.09f, 0.45f, 0.09f), mat, new Vector3(0f, 0f, 90f));
        piles++;
    }
    // 南瓜/木箱：交替
    if (barrels % 2 == 0) BoxProp("Prop_Crate", root.transform, door + new Vector3(0.7f, 0.25f, 0.7f), Vector3.one * 0.55f, mat);
    else Prim("Prop_Bucket", root.transform, door + new Vector3(-0.7f, 0.18f, 0.6f), new Vector3(0.2f, 0.18f, 0.2f), mat);
    extras++;
}

// 井边：木架 + 水桶 + 绳索
var well = Object.FindObjectOfType<StoryDirector>().storyPoint.position;
var wellBase = new Vector3(well.x + 1.2f, Y(well), well.z);
BoxProp("Prop_WellFrameL", root.transform, wellBase + new Vector3(-0.55f, 0.7f, 0f), new Vector3(0.12f, 1.4f, 0.12f), mat);
BoxProp("Prop_WellFrameR", root.transform, wellBase + new Vector3(0.55f, 0.7f, 0f), new Vector3(0.12f, 1.4f, 0.12f), mat);
BoxProp("Prop_WellBeam", root.transform, wellBase + new Vector3(0f, 1.45f, 0f), new Vector3(1.3f, 0.1f, 0.1f), mat);
Prim("Prop_WellBucket", root.transform, wellBase + new Vector3(0f, 0.2f, 0.35f), new Vector3(0.22f, 0.2f, 0.22f), mat);
extras += 4;

// 火堆旁：柴堆 + 两块坐石（不放渲染？用木凳代替，模型更统一）
foreach (var fireName in new[] { "StealthLighting/Route Fire" })
{
    var fire = GameObject.Find(fireName);
    if (fire == null) continue;
    for (int i = 0; i < 3; i++)
        Prim("Prop_Firewood", root.transform, fire.transform.position + new Vector3(0.6f + i * 0.16f, 0.1f, 0.4f), new Vector3(0.08f, 0.4f, 0.08f), mat, new Vector3(0f, 0f, 90f));
    BoxProp("Prop_Bench", root.transform, fire.transform.position + new Vector3(-1.1f, 0.22f, 0.5f), new Vector3(1.0f, 0.12f, 0.3f), mat);
    extras += 2;
}

foreach (var go in root.GetComponentsInChildren<Transform>()) if (go != root.transform) GameObjectUtility.SetStaticEditorFlags(go.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
return "props barrels=" + barrels + " firewood=" + piles + " extras=" + extras + " total=" + root.transform.childCount;
```

- [ ] **Step 3: 运行 + 断言**

Expected: `PASS props total≈40+ boxColliders≥10`。

- [ ] **Step 4: 目检**

拍 `out/after_props_house.png`、`out/after_props_well.png`，确认不悬空、不挡门洞、木桶比例合理。

---

### Task 5: 光照与氛围

**Files:**
- Create: `...\BuildLights.cs`、`...\ValidateLights.cs`

**Interfaces:**
- Produces: `SceneDetails_Lights` 下 6 盏窗光 + 4 盏灯笼 + 2 组火堆烟；`RenderSettings.fog=true`。

- [ ] **Step 1: 失败断言**

```csharp
var root = GameObject.Find("SceneDetails_Lights");
int lights = root == null ? 0 : root.GetComponentsInChildren<Light>().Where(l => l.enabled).Count();
string result = (lights >= 10 && RenderSettings.fog) ? "PASS lights=" + lights + " fog=true"
    : "FAIL lights=" + lights + " fog=" + RenderSettings.fog;
System.IO.File.WriteAllText("C:/Users/22492/.claude/tools/scene_detail/out/lights_test.txt", result);
return result;
```

- [ ] **Step 2: 生成脚本**

```csharp
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

static GameObject Lantern(string name, Transform parent, Vector3 pos, Material mat)
{
    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
    go.name = name; go.transform.SetParent(parent); go.transform.position = pos + Vector3.up * 0.9f;
    go.transform.localScale = new Vector3(0.18f, 0.22f, 0.18f);
    Object.DestroyImmediate(go.GetComponent<Collider>());
    var r = go.GetComponent<Renderer>();
    r.sharedMaterial = mat;
    var light = new GameObject(name + "_Light").AddComponent<Light>();
    light.transform.SetParent(parent); light.transform.position = pos + Vector3.up * 0.9f;
    light.type = LightType.Point; light.range = 3.2f; light.intensity = 0.8f;
    light.color = new Color(1f, 0.66f, 0.36f); light.shadows = LightShadows.None;
    return go;
}

var env = GameObject.Find("Environment").transform;
var root = GameObject.Find("SceneDetails_Lights");
if (root == null) root = new GameObject("SceneDetails_Lights");
var glow = new Material(Shader.Find("Standard")) { color = new Color(1f, 0.72f, 0.38f), };
glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.25f) * 1.5f);

var houses = env.GetComponentsInChildren<MeshRenderer>().Where(r => r.name.StartsWith("SM_Bld_Hut") || r.name.StartsWith("SM_Bld_Village")).Take(6).ToList();
foreach (var h in houses)
{
    var b = h.bounds;
    Vector3 dir = new Vector3(-b.center.x, 0f, -b.center.z).normalized;
    Vector3 p = b.center + dir * (Mathf.Max(b.extents.x, b.extents.z) + 0.3f);
    p.y = b.center.y + 0.4f;
    var l = new GameObject("WindowGlow").AddComponent<Light>();
    l.transform.SetParent(root.transform); l.transform.position = p;
    l.type = LightType.Point; l.range = 4f; l.intensity = 0.9f; l.shadows = LightShadows.None;
    l.color = new Color(1f, 0.65f, 0.35f);
}
foreach (var h in houses.Take(4))
{
    var b = h.bounds;
    Vector3 dir = new Vector3(-b.center.x, 0f, -b.center.z).normalized;
    Vector3 p = b.center + dir * (Mathf.Max(b.extents.x, b.extents.z) + 1.5f);
    p.y = b.min.y + 0.1f;
    Lantern("Prop_Lantern", root.transform, p, glow);
}

// 火堆烟：复用 SimpleFX 尘土粒子，改为慢速灰烟
var dust = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MainAsset/MainAsset/Asset Packs/SimpleFX/Prefabs/FX_Dust_Prefab_01.prefab");
foreach (var fireName in new[] { "StealthLighting/Route Fire" })
{
    var fire = GameObject.Find(fireName);
    if (fire == null || dust == null) continue;
    var smoke = (GameObject)PrefabUtility.InstantiatePrefab(dust, root.transform);
    smoke.name = "Prop_Smoke"; smoke.transform.position = fire.transform.position + Vector3.up * 0.3f;
    var ps = smoke.GetComponent<ParticleSystem>();
    if (ps != null)
    {
        var main = ps.main;
        main.loop = true; main.startLifetime = 6f; main.startSpeed = 0.35f; main.startSize = 1.6f;
        main.startColor = new Color(0.55f, 0.55f, 0.6f, 0.18f); main.gravityModifier = -0.01f; main.maxParticles = 30;
    }
    var fireLight = fire.GetComponent<Light>();
    if (fireLight != null) fireLight.intensity = 1.2f;
}

RenderSettings.fog = true;
RenderSettings.fogMode = FogMode.ExponentialSquared;
RenderSettings.fogDensity = 0.006f;
RenderSettings.fogColor = new Color(0.09f, 0.11f, 0.19f);
foreach (var go in root.GetComponentsInChildren<Transform>(true)) if (go != root.transform) GameObjectUtility.SetStaticEditorFlags(go.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
return "lights built: window=" + houses.Count + " lantern=4 fog=" + RenderSettings.fog;
```

- [ ] **Step 3: 运行 + 断言**

Expected: `PASS lights>=10 fog=true`。

- [ ] **Step 4: 目检与雾校准**

拍 `out/after_night.png`（出生点）、`out/after_border_night.png`。若远景发灰或看不清房屋轮廓，把 `fogDensity` 调至 0.004 并重保存；若灯光刺眼，窗光 intensity 降到 0.7。

---

### Task 6: 边界林带

**Files:**
- Create: `...\BuildBorder.cs`、`...\ValidateBorder.cs`

**Interfaces:**
- Consumes: `Assets/RPG/Prefabs/SM_Env_Tree_03_Collider.prefab`（带胶囊碰撞）；`SM_Env_Rock_015`。
- Produces: `SceneDetails_Border` 下 50–70 棵树 + 10–15 块岩石，全部有碰撞。

- [ ] **Step 1: 失败断言**

```csharp
var root = GameObject.Find("SceneDetails_Border");
int count = root == null ? 0 : root.transform.childCount;
int cols = root == null ? 0 : root.GetComponentsInChildren<Collider>().Length;
string result = (count >= 60 && cols >= 60) ? "PASS border count=" + count + " colliders=" + cols
    : "FAIL border count=" + count + " colliders=" + cols;
System.IO.File.WriteAllText("C:/Users/22492/.claude/tools/scene_detail/out/border_test.txt", result);
return result;
```

- [ ] **Step 2: 生成脚本**

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG/Prefabs/SM_Env_Tree_03_Collider.prefab");
var rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MainAsset/MainAsset/Asset Packs/Synty - Polygon Student Sample/Prefabs/SM_Env_Rock_015.prefab");
var root = GameObject.Find("SceneDetails_Border");
if (root == null) root = new GameObject("SceneDetails_Border");
var terrain = Object.FindObjectOfType<Terrain>();
var rng = new System.Random(20261113);
int trees = 0, rocks = 0;
for (int side = 0; side < 4; side++)
{
    for (float t = -42f; t <= 42f; t += 6f)
    {
        for (int row = 0; row < 2; row++)
        {
            float offset = 44f + row * 3f + (float)rng.NextDouble() * 1.2f;
            Vector3 p = side == 0 ? new Vector3(t + (float)rng.NextDouble() * 2f, 0f, offset)
                : side == 1 ? new Vector3(t + (float)rng.NextDouble() * 2f, 0f, -offset)
                : side == 2 ? new Vector3(offset, 0f, t + (float)rng.NextDouble() * 2f)
                : new Vector3(-offset, 0f, t + (float)rng.NextDouble() * 2f);
            p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, root.transform);
            go.transform.position = p;
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            float s = 0.9f + (float)rng.NextDouble() * 0.3f;
            go.transform.localScale = new Vector3(s, s, s);
            trees++;
        }
    }
}
for (int i = 0; i < 14; i++)
{
    float angle = i * Mathf.PI * 2f / 14f;
    Vector3 p = new Vector3(Mathf.Cos(angle) * 43f, 0f, Mathf.Sin(angle) * 43f);
    p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
    var go = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, root.transform);
    go.transform.position = p;
    go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
    float s = 1.5f + (float)rng.NextDouble() * 1.5f;
    go.transform.localScale = new Vector3(s, s * 0.8f, s);
    var mf = go.GetComponentInChildren<MeshFilter>();
    if (mf != null) { var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; }
    rocks++;
}
foreach (var go in root.GetComponentsInChildren<Transform>(true)) if (go != root.transform) GameObjectUtility.SetStaticEditorFlags(go.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
return "border trees=" + trees + " rocks=" + rocks;
```

- [ ] **Step 3: 运行 + 断言**

Expected: `PASS border count≈28+14`（5 步 ×2 排 ×4 边 = 20 树 + 14 石，实际计数以日志为准；若 count<60 把 `t` 步长改 4f）。

- [ ] **Step 4: 目检**

拍 `out/after_border.png`，确认林带遮住边界、不穿入可玩区。

---

### Task 7: 静态校验与路径断言

**Files:**
- Create: `...\ValidateDetails.cs`

**Interfaces:**
- Consumes: 全部 `SceneDetails_*`。
- Produces: `out\validation.txt`，含 6 条 PASS/FAIL。

- [ ] **Step 1: 写校验脚本**

```csharp
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

var sb = new StringBuilder();
var roots = new[] { "SceneDetails_Ground", "SceneDetails_Vegetation", "SceneDetails_Props", "SceneDetails_Lights", "SceneDetails_Border" };
int total = 0, lights = 0;
foreach (var n in roots)
{
    var go = GameObject.Find(n);
    sb.AppendLine(n + " exists=" + (go != null) + " children=" + (go == null ? 0 : go.transform.childCount));
    if (go != null) { total += go.transform.childCount; lights += go.GetComponentsInChildren<Light>().Where(l => l.enabled).Count(); }
}
sb.AppendLine(total <= 220 ? "PASS object budget total=" + total : "FAIL object budget total=" + total);
sb.AppendLine(lights <= 10 ? "PASS light budget lights=" + lights : "FAIL light budget lights=" + lights);
sb.AppendLine(RenderSettings.fog && RenderSettings.fogDensity <= 0.007f ? "PASS fog" : "FAIL fog");

var hero = Object.FindObjectOfType<HeroController>().transform.position;
var elder = Object.FindObjectOfType<NpcInteractor>().transform.position;
var well = Object.FindObjectOfType<StoryDirector>().storyPoint.position;
var targets = new (string name, Vector3 pos)[]
{
    ("elder", elder), ("well", well),
    ("patrol1", GameObject.Find("enemyPos1").transform.position),
    ("patrol2", GameObject.Find("enemyPos2").transform.position),
    ("patrol3", GameObject.Find("enemyPos3").transform.position),
    ("patrol4", GameObject.Find("enemyPos4").transform.position),
};
int ok = 0;
foreach (var t in targets)
{
    var path = new NavMeshPath();
    bool reachable = NavMesh.CalculatePath(hero, t.pos, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
    sb.AppendLine("path hero->" + t.name + " " + reachable);
    if (reachable) ok++;
}
sb.AppendLine(ok == targets.Length ? "PASS nav paths" : "FAIL nav paths " + ok + "/" + targets.Length);

var hut = GameObject.Find("Environment/SM_Bld_Hut_01");
NavMeshHit hit;
bool hutWalkable = NavMesh.SamplePosition(hut.GetComponent<Renderer>().bounds.center, out hit, 0.6f, NavMesh.AllAreas);
sb.AppendLine(!hutWalkable ? "PASS house still blocked" : "FAIL house became walkable");

// 禁区：所有新增碰撞体与巡逻点/交互点距离 ≥1.5m
float minDist = float.MaxValue;
var forbidden = targets.Select(t => t.pos).Concat(new[] { Object.FindObjectOfType<CluePickup>().transform.position }).ToArray();
foreach (var c in roots.SelectMany(n => GameObject.Find(n) == null ? new Collider[0] : GameObject.Find(n).GetComponentsInChildren<Collider>()))
    foreach (var f in forbidden)
        minDist = Mathf.Min(minDist, Vector3.Distance(new Vector3(c.transform.position.x, 0, c.transform.position.z), new Vector3(f.x, 0, f.z)));
sb.AppendLine(minDist >= 1.5f ? "PASS forbidden spacing min=" + minDist : "FAIL forbidden spacing min=" + minDist);

System.IO.File.WriteAllText("C:/Users/22492/.claude/tools/scene_detail/out/validation.txt", sb.ToString());
return sb.ToString();
```

- [ ] **Step 2: 运行**

Expected: 6 项全 PASS；若 spacing FAIL，删除离交互点最近的那件装饰后重跑。

- [ ] **Step 3: 保存场景并记录 MD5**

```bash
python -I -c "import hashlib;print(hashlib.md5(open(r'D:\\Unity\\projects\\RPG_Project\\Assets\\Scenes\\RPG游戏.unity','rb').read()).hexdigest())"
```

写入 `out\validation.txt` 顶部与 `docs/progress.md`。

---

### Task 8: 运行时回归与验收

**Files:**
- Create: `...\MeasureFrameTime.cs`、`...\RunSceneDetailRegression.py`
- Modify: `docs/progress.md`、`docs/superpowers/reports/2026-11-13-scene-detail-verification.md`

- [ ] **Step 1: 帧时间采样脚本**

```csharp
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

public class MeasureFrameTime : MonoBehaviour
{
    public IEnumerator Run()
    {
        var sb = new StringBuilder();
        yield return null;
        for (int i = 0; i < 900; i++)
        {
            yield return new WaitForEndOfFrame();
            if (i > 60) sb.AppendLine(Time.unscaledDeltaTime.ToString("F5"));
        }
        File.WriteAllText("C:/Users/22492/.claude/tools/scene_detail/out/frame_times.csv", sb.ToString());
        Destroy(gameObject);
    }
}
var go = new GameObject("MeasureFrameTime");
var m = go.AddComponent<MeasureFrameTime>();
m.StartCoroutine(m.Run());
return "started";
```

- [ ] **Step 2: 跑 12 项回归**

```bash
cd C:/Users/22492/.claude/tools/story && python -I run_story_regression.py > C:/Users/22492/.claude/tools/scene_detail/out/regression.log 2>&1
```

Expected: SUMMARY 12/12 PASS。

- [ ] **Step 3: 三剧情路径冒烟**

依次运行 `StoryChapterOneTests.cs`（kill / kill_after_truth / spare 三种参数，复用既有 `story` 目录流程），记录结果到 `out\story_paths.txt`。

- [ ] **Step 4: 帧时间对比**

分别测“细节前（备份场景 `RPG游戏.before-detail.unity`）”与“细节后”的 900 帧。平均退幅 ≤10%、p95 退步 ≤2ms 视为通过；否则减半窗光、降低植被密度 20%。

- [ ] **Step 5: 截图矩阵与验收报告**

拍出生点、村口主路、井边、边界林带、夜景全景 5 张到 `docs/verification/2026-11-13-scene-detail/`；写 `2026-11-13-scene-detail-verification.md`，含所有 PASS/FAIL 原始数据、MD5、备份路径、未覆盖项（真人键鼠与主观美术评价）。

- [ ] **Step 6: 更新进度**

在 `docs/progress.md` 勾选阶段 3 完成，写入新场景/Terrain MD5 与回滚方法（删除 `SceneDetails_*` 根节点 + 还原备份）。

---

## Self-Review 记录

- **Spec 覆盖**：地面/植被/道具/光照/边界 → Task 2/3/4/5/6；禁区与预算 → Task 7；回归/帧时间/截图 → Task 8；回滚 → Task 1 与 Task 8 Step 6。
- **占位符**：无 TBD；所有生成脚本含可执行代码。
- **类型/接口一致**：根节点名、`SampleHeight` 用法、`SM_Env_Tree_03_Collider.prefab` 路径、`y` 坐标地面计算在各任务一致。
- **已知偏差**：Task 6 的树计数按 6m 间距为 4 边 ×15 段 ×2 排 = 120 棵，超过 spec 的 50–70；实施时把 `t += 6f` 改为 `t += 12f`（4×7×2=56 棵），断言阈值 60 改为 50。
