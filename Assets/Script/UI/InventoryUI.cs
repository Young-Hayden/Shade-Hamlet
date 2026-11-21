using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 简化背包面板：显示药水数量、当前武器与护甲。
/// I 键开关；面板通过 RpgUI 运行时构建。
/// </summary>
public class InventoryUI : MonoBehaviour
{
    /// <summary>药水库存。</summary>
    public PotionInventory inventory;
    /// <summary>面板底图。</summary>
    public Sprite panelSprite;
    /// <summary>药水图标。</summary>
    public Sprite potionSprite;
    /// <summary>武器图标。</summary>
    public Sprite weaponSprite;
    /// <summary>护甲图标。</summary>
    public Sprite armorSprite;
    /// <summary>关闭按钮图标。</summary>
    public Sprite closeSprite;
    /// <summary>背包面板根对象。</summary>
    public GameObject InventoryPanel;
    /// <summary>药水数量文本。</summary>
    public Text PotionValue;

    void Awake()
    {
        // 自动获取主角身上的库存组件。
        if (inventory == null)
        {
            var hero = FindObjectOfType<HeroController>();
            if (hero != null) inventory = hero.GetComponent<PotionInventory>();
        }
        Build();
    }

    void Update()
    {
        if (GameplayInputGate.Blocked) return;
        if (Input.GetKeyDown(KeyCode.I)) Toggle();
    }
    /// <summary>开关背包面板；剧情/暂停/死亡/过渡期间忽略。</summary>
    public void Toggle()
    {
        if (GameplayInputGate.Blocked) return;
        InventoryPanel.SetActive(!InventoryPanel.activeSelf);
    }
    /// <summary>关闭背包面板。</summary>
    public void Close() { InventoryPanel.SetActive(false); }

    /// <summary>构建背包控件：标题、药水/武器/护甲条目、操作提示与关闭按钮。</summary>
    private void Build()
    {
        var canvas = RpgUI.CreateCanvas("InventoryCanvas", transform, true);
        var panel = RpgUI.CreateImage("InventoryPanel", canvas.transform, panelSprite, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 420f));
        InventoryPanel = panel.gameObject;
        RpgUI.CreateText("Title", panel.transform, "背包", 38, new Color(1f, 0.92f, 0.72f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(540f, 50f));
        // 药水条目。
        RpgUI.CreateImage("PotionIcon", panel.transform, potionSprite, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(45f, -100f), new Vector2(64f, 64f));
        RpgUI.CreateText("PotionName", panel.transform, "治疗药水", 24, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(125f, -105f), new Vector2(200f, 32f));
        PotionValue = RpgUI.CreateText("PotionValue", panel.transform, "数量：0", 22, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(125f, -140f), new Vector2(200f, 32f));
        // 武器/护甲为固定文本展示。
        RpgUI.CreateImage("WeaponIcon", panel.transform, weaponSprite, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(45f, -210f), new Vector2(64f, 64f));
        RpgUI.CreateText("Weapon", panel.transform, "当前武器：细剑", 24, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(125f, -220f), new Vector2(360f, 36f));
        RpgUI.CreateImage("ArmorIcon", panel.transform, armorSprite, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(45f, -290f), new Vector2(64f, 64f));
        RpgUI.CreateText("Armor", panel.transform, "当前护甲：无", 24, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(125f, -300f), new Vector2(360f, 36f));
        RpgUI.CreateText("Hint", panel.transform, "Q 使用药水　I 关闭背包", 20, new Color(1f, 0.9f, 0.7f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(540f, 32f));
        var closeImage = RpgUI.CreateImage("CloseButton", panel.transform, closeSprite, Color.white, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -12f), new Vector2(48f, 48f));
        closeImage.gameObject.AddComponent<Button>().onClick.AddListener(Close);
        InventoryPanel.SetActive(false);
    }

    void LateUpdate() { PotionValue.text = "数量：" + (inventory != null ? inventory.Count : 0); }
}
