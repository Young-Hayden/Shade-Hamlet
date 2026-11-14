using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    public HeroController hero;
    public Sprite panelSprite;
    public Sprite barSprite;
    public Slider heroHealth;
    public Text heroHealthText;
    public GameObject targetPanel;
    public RectTransform playerPanel;
    public PotionInventory potionInventory;
    public Sprite potionSprite;
    public Text potionCountText;
    public Text objectiveText;
    public RectTransform objectivePanel;
    public InventoryUI inventoryUI;
    public Sprite actionSlotSprite;
    public Sprite weaponSprite;
    public Sprite inventoryIconSprite;
    public RectTransform actionBar;
    public GameObject assassinationPrompt;
    public Text assassinationPromptText;
    public CanvasGroup controlsHint;
    public Button potionButton;
    public Button inventoryButton;
    public Button smokeButton;
    public Text smokeCooldownText;
    public Text stealthStatusText;
    public Text alertnessText;
    private SmokeAbility smokeAbility;
    private float hintAge;
    private Text targetName;
    private Slider targetHealth;

    void Awake()
    {
        if (hero == null) hero = FindObjectOfType<HeroController>();
        if (potionInventory == null && hero != null) potionInventory = hero.GetComponent<PotionInventory>();
        if (inventoryUI == null) inventoryUI = FindObjectOfType<InventoryUI>();
        if (smokeAbility == null && hero != null) smokeAbility = hero.GetComponent<SmokeAbility>();
        Build();
    }

    private void Build()
    {
        var canvas = RpgUI.CreateCanvas("GameHUDCanvas", transform, false);
        playerPanel = RpgUI.CreateImage("PlayerPanel", canvas.transform, panelSprite, new Color(1f, 1f, 1f, 0.92f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(360f, 140f)).rectTransform;
        RpgUI.CreateText("Title", playerPanel, "生命", 24, Color.white, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -8f), new Vector2(320f, 28f));
        heroHealth = RpgUI.CreateSlider("HeroHealth", playerPanel, barSprite, new Color(0.25f, 0.9f, 0.3f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -40f), new Vector2(324f, 22f));
        heroHealthText = RpgUI.CreateText("Value", playerPanel, "100 / 100", 20, Color.white, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -64f), new Vector2(324f, 24f));
        stealthStatusText = RpgUI.CreateText("StealthStatus", playerPanel, "状态：正常", 18, new Color(0.86f, 0.94f, 1f), TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -88f), new Vector2(324f, 24f));
        alertnessText = RpgUI.CreateText("Alertness", playerPanel, "警戒：0%", 18, new Color(1f, 0.9f, 0.65f), TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -112f), new Vector2(324f, 24f));

        var targetImage = RpgUI.CreateImage("TargetPanel", canvas.transform, panelSprite, new Color(1f, 1f, 1f, 0.92f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(360f, 88f));
        targetPanel = targetImage.gameObject;
        targetName = RpgUI.CreateText("Name", targetPanel.transform, "敌人", 24, new Color(1f, 0.9f, 0.65f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(340f, 30f));
        targetHealth = RpgUI.CreateSlider("TargetHealth", targetPanel.transform, barSprite, new Color(0.9f, 0.22f, 0.18f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -48f), new Vector2(324f, 22f));
        targetPanel.SetActive(false);

        RpgUI.CreateImage("PotionIcon", playerPanel, potionSprite, Color.white,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -10f), new Vector2(36f, 36f));
        potionCountText = RpgUI.CreateText("PotionCount", playerPanel, "× 0", 20, Color.white, TextAnchor.MiddleRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-52f, -14f), new Vector2(60f, 28f));
        var objectiveImage = RpgUI.CreateImage("ObjectivePanel", canvas.transform, panelSprite, new Color(1f, 1f, 1f, 0.9f),
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -230f), new Vector2(340f, 54f));
        objectivePanel = objectiveImage.rectTransform;
        objectiveText = RpgUI.CreateText("ObjectiveText", objectivePanel, "目标：取得卷轴 0 / 1", 20, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(330f, 44f));

        actionBar = RpgUI.CreateRect("ActionBar", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(384f, 84f));
        string[] slotNames = { "AttackSlot", "PotionSlot", "InventorySlot", "SmokeSlot" };
        Sprite[] icons = { weaponSprite, potionSprite, inventoryIconSprite, null };
        string[] keys = { "右键", "Q", "I", "G" };
        for (int i = 0; i < slotNames.Length; i++)
        {
            float x = (i - 1.5f) * 100f;
            var slot = RpgUI.CreateImage(slotNames[i], actionBar, actionSlotSprite, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(84f, 84f));
            if (icons[i] != null)
                RpgUI.CreateImage("Icon", slot.transform, icons[i], Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(52f, 52f));
            else
                RpgUI.CreateText("Icon", slot.transform, "烟雾", 21, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(76f, 30f));
            var keyText = RpgUI.CreateText("Key", slot.transform, keys[i], 18, new Color(1f, 0.94f, 0.78f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Vector2(76f, 22f));
            if (i == 1) potionButton = slot.gameObject.AddComponent<Button>();
            if (i == 2) inventoryButton = slot.gameObject.AddComponent<Button>();
            if (i == 3)
            {
                smokeButton = slot.gameObject.AddComponent<Button>();
                smokeCooldownText = keyText;
            }
        }
        potionButton.onClick.AddListener(() => { if (potionInventory != null) potionInventory.TryUse(); });
        inventoryButton.onClick.AddListener(() => { if (inventoryUI != null) inventoryUI.Toggle(); });
        smokeButton.onClick.AddListener(() => { if (smokeAbility != null) smokeAbility.TryActivate(); });

        var hintImage = RpgUI.CreateImage("ControlsHint", canvas.transform, panelSprite, new Color(1f, 1f, 1f, 0.88f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 126f), new Vector2(760f, 42f));
        controlsHint = hintImage.gameObject.AddComponent<CanvasGroup>();
        controlsHint.blocksRaycasts = false;
        RpgUI.CreateText("Text", hintImage.transform, "WASD 移动　Shift 奔跑　Ctrl 潜行　右键攻击　鼠标视角　滚轮缩放　Q 药水　I 背包　G 烟雾　Esc 菜单", 20,
            Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(750f, 34f));

        var assassinationImage = RpgUI.CreateImage("AssassinationPrompt", canvas.transform, panelSprite, new Color(1f, 1f, 1f, 0.9f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(220f, 48f));
        assassinationPrompt = assassinationImage.gameObject;
        assassinationPromptText = RpgUI.CreateText("Text", assassinationImage.transform, "E 刺杀", 22, new Color(1f, 0.9f, 0.65f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(210f, 38f));
        assassinationPrompt.SetActive(false);
    }

    void LateUpdate()
    {
        if (smokeAbility == null && hero != null) smokeAbility = hero.GetComponent<SmokeAbility>();
        if (smokeCooldownText != null)
            smokeCooldownText.text = smokeAbility != null && smokeAbility.RemainingCooldown > 0f
                ? Mathf.CeilToInt(smokeAbility.RemainingCooldown) + "s" : "G";

        hintAge += Time.unscaledDeltaTime;
        if (controlsHint != null)
        {
            controlsHint.alpha = 1f - Mathf.Clamp01((hintAge - 6f) / 2f);
            if (hintAge >= 8f) controlsHint.gameObject.SetActive(false);
        }

        if (hero == null) hero = FindObjectOfType<HeroController>();
        float heroRatio = hero != null && hero.MaxHP > 0f ? hero.CurrentHP / hero.MaxHP : 0f;
        heroHealth.value = Mathf.Clamp01(heroRatio);
        heroHealthText.text = hero != null ? Mathf.CeilToInt(hero.CurrentHP) + " / " + Mathf.RoundToInt(hero.MaxHP) : "0 / 100";

        var stealthState = hero != null ? hero.GetComponent<PlayerStealthState>() : null;
        if (stealthStatusText != null)
        {
            string status = "正常";
            if (stealthState != null)
            {
                switch (stealthState.Status)
                {
                    case PlayerStealthStatus.Sneaking: status = "潜行中"; break;
                    case PlayerStealthStatus.Seen: status = "已被发现"; break;
                    case PlayerStealthStatus.Hidden: status = "隐藏中"; break;
                    case PlayerStealthStatus.InSmoke: status = "烟雾掩护"; break;
                }
            }
            stealthStatusText.text = "状态：" + status;
        }
        float maxAlertness = 0f;
        string alertState = "已脱离视线";
        foreach (var guard in FindObjectsOfType<GuardSenses>())
        {
            if (guard == null) continue;
            maxAlertness = Mathf.Max(maxAlertness, guard.Alertness);
            if (guard.State == GuardState.Chase) alertState = "已发现";
            else if (guard.State == GuardState.Search && alertState != "已发现") alertState = "正在搜索";
            else if (guard.State == GuardState.Suspicious && alertState == "已脱离视线") alertState = "正在暴露";
        }
        if (alertnessText != null)
            alertnessText.text = "警戒：" + Mathf.RoundToInt(maxAlertness * 100f) + "%　" + alertState;

        if (potionInventory == null && hero != null) potionInventory = hero.GetComponent<PotionInventory>();
        potionCountText.text = potionInventory != null ? "× " + potionInventory.Count : "× 0";
        int kills = GameFlow.Instance != null ? GameFlow.Instance.KillCount : 0;
        int target = GameFlow.Instance != null ? GameFlow.Instance.KillTarget : 1;
        if (StoryDirector.Instance != null) objectiveText.text = StoryDirector.Instance.GetObjectiveText();
        else if (MissionController.Instance != null) objectiveText.text = MissionController.Instance.GetObjectiveText();
        else objectiveText.text = kills >= target ? "目标完成：敌人已全部击败" : "目标：击败敌人 " + kills + " / " + target;

        var assassination = hero != null ? hero.GetComponent<AssassinationController>() : null;
        EnemyController assassinationTarget;
        bool canAssassinate = assassination != null && assassination.CanAssassinate(out assassinationTarget);
        bool storyUiOpen = StoryDirector.Instance != null && StoryDirector.Instance.IsUiOpen;
        string prompt = !storyUiOpen && canAssassinate ? "E 刺杀" : null;
        if (prompt == null && !storyUiOpen)
        {
            foreach (var npc in FindObjectsOfType<NpcInteractor>())
            {
                if (npc.CanInteract()) { prompt = "E 对话"; break; }
            }
        }
        if (prompt == null && !storyUiOpen)
        {
            foreach (var clue in FindObjectsOfType<CluePickup>())
            {
                if (clue.CanInteract()) { prompt = "E 查看家书"; break; }
            }
        }
        if (prompt == null && MissionController.Instance != null)
        {
            foreach (var intel in FindObjectsOfType<IntelPickup>())
            {
                if (intel.CanInteract()) { prompt = "E 取得卷轴"; break; }
            }
            if (prompt == null)
            {
                foreach (var escape in FindObjectsOfType<EscapePoint>())
                {
                    if (escape.CanInteract()) { prompt = "E 撤离"; break; }
                }
            }
        }
        assassinationPrompt.SetActive(prompt != null);
        if (prompt != null) assassinationPromptText.text = prompt;

        EnemyController enemy = null;
        if (hero != null && hero.tarEnemy != null) enemy = hero.tarEnemy.GetComponent<EnemyController>();
        bool show = enemy != null && !enemy.isDeath;
        targetPanel.SetActive(show);
        if (!show) return;
        targetName.text = enemy.name;
        targetHealth.value = enemy.MaxHP > 0f ? Mathf.Clamp01(enemy.CurrentHP / enemy.MaxHP) : 0f;
    }
}
