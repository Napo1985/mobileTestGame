using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Main screen with tabs: UPGRADE | PLAY | SHOP | SKINS (StreamingAssets/Skins per-object folders).
/// </summary>
public class MainScreenTabs : MonoBehaviour
{
    [SerializeField] string gameplaySceneName = "Gameplay";

    enum Tab
    {
        Upgrade,
        Play,
        Shop,
        Skins
    }

    static readonly (GameSkinSlot slot, string label)[] SkinRowDefs =
    {
        (GameSkinSlot.Player, "Player ship"),
        (GameSkinSlot.EnemyShip, "Enemy ship"),
        (GameSkinSlot.Asteroid, "Asteroid"),
        (GameSkinSlot.Bullet, "Bullet"),
        (GameSkinSlot.Background, "Background"),
        (GameSkinSlot.PickupHealth, "Pickup: health"),
        (GameSkinSlot.PickupPositive, "Pickup: positive"),
        (GameSkinSlot.PickupNegative, "Pickup: negative")
    };

    Tab _current = Tab.Play;
    Button _btnUpgrade;
    Button _btnPlayTab;
    Button _btnShop;
    Button _btnSkins;
    GameObject _panelUpgrade;
    GameObject _panelPlay;
    GameObject _panelShop;
    GameObject _panelSkins;
    Text _nextWaveLabel;
    GameObject _infoOverlay;
    ScrollRect _infoScroll;
    ScrollRect _skinScrollRect;
    RectTransform _skinScrollContentRt;
    Image _shipPreviewImg;
    static Sprite _cachedUiRaycastSprite;
    readonly List<(GameSkinSlot slot, Text value)> _skinRowTexts = new List<(GameSkinSlot, Text)>();

    const string InfoHelpText =
        "YOUR SHIP\n" +
        "Drag with finger or mouse to move. You shoot upward automatically. Green bar at top is shield (HP).\n\n" +
        "PICKUPS (drops when enemies are destroyed)\n\n" +
        "Green — Health pack\n" +
        "Restores a portion of your max shield. More likely when your shield is not full.\n\n" +
        "Cyan / light blue — Positive weapon chip\n" +
        "Randomly boosts one of: bullet damage, bullet speed, fire rate, or bullet size.\n\n" +
        "Red — Negative weapon chip\n" +
        "Same stats as above, but weakens instead (watch the color).\n\n" +
        "ATOM BOMB (in gameplay only)\n" +
        "Button lower-left: damages every enemy on screen. Not a flying pickup.\n\n" +
        "TIP\n" +
        "Large slow rocks are asteroids (high HP). Smaller ships drift sideways.";

    static Font BuiltinFont()
    {
        // Unity 2022+ / 6.x: Arial.ttf is not a valid built-in path; use LegacyRuntime first.
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f != null) return f;
        foreach (var name in new[] { "Arial", "Segoe UI", "Helvetica" })
        {
            try
            {
                var df = Font.CreateDynamicFontFromOSFont(name, 24);
                if (df != null)
                    return df;
            }
            catch
            {
                // try next
            }
        }

        return null;
    }

    void Awake()
    {
        var cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.025f, 0.07f, 1f);
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var canvasGo = new GameObject("MainMenuCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var menuBgGo = CreateUIObject("MenuStarfield", canvasGo.transform);
        StretchFull(menuBgGo);
        var menuBg = menuBgGo.AddComponent<Image>();
        menuBg.sprite = SpaceBackdrop.CreateSprite();
        menuBg.raycastTarget = false;

        BuildContent(canvasGo.transform);
        BuildTabBar(canvasGo.transform);
        BuildInfoUi(canvasGo.transform);
        SelectTab(_current);
    }

    void OnEnable()
    {
        RefreshNextWaveLabel();
    }

    void BuildContent(Transform canvas)
    {
        Font font = BuiltinFont();

        var area = CreateUIObject("ContentArea", canvas);
        StretchFull(area);
        var areaRt = area.GetComponent<RectTransform>();
        areaRt.offsetMin = new Vector2(0, 140);
        areaRt.offsetMax = Vector2.zero;

        _panelUpgrade = CreatePanel(area.transform, "UPGRADE", "Upgrade tree — coming soon.");
        _panelPlay = CreatePanel(area.transform, "PLAY", "Ready when you are.");
        _panelShop = CreatePanel(area.transform, "SHOP", "Cosmetics & boosts — coming soon.");
        _panelSkins = BuildSkinsPanel(area.transform, font);

        var startBtnGo = CreateUIObject("StartButton", _panelPlay.transform);
        var startRt = startBtnGo.GetComponent<RectTransform>();
        startRt.anchorMin = new Vector2(0.5f, 0.35f);
        startRt.anchorMax = new Vector2(0.5f, 0.35f);
        startRt.pivot = new Vector2(0.5f, 0.5f);
        startRt.sizeDelta = new Vector2(420, 100);

        var startImg = startBtnGo.AddComponent<Image>();
        startImg.color = new Color(0f, 1f, 0.35f, 1f);
        var startBtn = startBtnGo.AddComponent<Button>();
        startBtn.targetGraphic = startImg;
        startBtn.onClick.AddListener(() => SceneManager.LoadScene(gameplaySceneName));

        var startLabel = CreateUIObject("Label", startBtnGo.transform);
        StretchFull(startLabel);
        var startText = startLabel.AddComponent<Text>();
        startText.font = font;
        startText.fontSize = 36;
        startText.fontStyle = FontStyle.Bold;
        startText.alignment = TextAnchor.MiddleCenter;
        startText.color = Color.black;
        startText.text = "START";

        var nextWaveGo = CreateUIObject("NextWaveLabel", _panelPlay.transform);
        var nextRt = nextWaveGo.GetComponent<RectTransform>();
        nextRt.anchorMin = new Vector2(0.5f, 0.5f);
        nextRt.anchorMax = new Vector2(0.5f, 0.5f);
        nextRt.pivot = new Vector2(0.5f, 0.5f);
        nextRt.anchoredPosition = new Vector2(0f, 24f);
        nextRt.sizeDelta = new Vector2(720f, 48f);
        _nextWaveLabel = nextWaveGo.AddComponent<Text>();
        _nextWaveLabel.font = font;
        _nextWaveLabel.fontSize = 26;
        _nextWaveLabel.fontStyle = FontStyle.Bold;
        _nextWaveLabel.alignment = TextAnchor.MiddleCenter;
        _nextWaveLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        _nextWaveLabel.verticalOverflow = VerticalWrapMode.Overflow;
        _nextWaveLabel.color = new Color(0.88f, 0.95f, 1f, 1f);
        _nextWaveLabel.raycastTarget = false;
        var nextOutline = nextWaveGo.AddComponent<Outline>();
        nextOutline.effectColor = new Color(0f, 0f, 0f, 0.72f);
        nextOutline.effectDistance = new Vector2(2f, -2f);
        RefreshNextWaveLabel();

        var shipPrevGo = CreateUIObject("ShipPreview", _panelPlay.transform);
        var shipRt = shipPrevGo.GetComponent<RectTransform>();
        shipRt.anchorMin = new Vector2(0.5f, 0.7f);
        shipRt.anchorMax = new Vector2(0.5f, 0.7f);
        shipRt.pivot = new Vector2(0.5f, 0.5f);
        shipRt.sizeDelta = new Vector2(168f, 168f);
        _shipPreviewImg = shipPrevGo.AddComponent<Image>();
        _shipPreviewImg.color = new Color(0.55f, 0.92f, 1f, 1f);
        _shipPreviewImg.preserveAspect = true;
        _shipPreviewImg.raycastTarget = false;
        RefreshShipPreviewSprite();
    }

    void RefreshNextWaveLabel()
    {
        if (_nextWaveLabel == null)
            return;
        int next = Mathf.Max(1, PlayerPrefs.GetInt(GameBootstrap.PrefNextWave, 1));
        _nextWaveLabel.text = $"Next: Wave {next}";
    }

    GameObject BuildSkinsPanel(Transform area, Font font)
    {
        var go = CreateUIObject("Panel_Skins", area);
        StretchFull(go);

        var titleGo = CreateUIObject("Title", go.transform);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.88f);
        titleRt.anchorMax = new Vector2(0.5f, 0.88f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(900, 56);
        var titleTx = titleGo.AddComponent<Text>();
        titleTx.font = font;
        titleTx.fontSize = 40;
        titleTx.fontStyle = FontStyle.Bold;
        titleTx.alignment = TextAnchor.MiddleCenter;
        titleTx.color = new Color(0.55f, 1f, 0.65f, 1f);
        titleTx.text = "SKINS";

        var subGo = CreateUIObject("Subtitle", go.transform);
        var subRt = subGo.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.5f, 0.815f);
        subRt.anchorMax = new Vector2(0.5f, 0.815f);
        subRt.pivot = new Vector2(0.5f, 0.5f);
        subRt.sizeDelta = new Vector2(880, 80);
        var subTx = subGo.AddComponent<Text>();
        subTx.font = font;
        subTx.fontSize = 20;
        subTx.alignment = TextAnchor.MiddleCenter;
        subTx.color = new Color(0.75f, 0.82f, 0.9f, 1f);
        subTx.horizontalOverflow = HorizontalWrapMode.Wrap;
        subTx.verticalOverflow = VerticalWrapMode.Overflow;
        subTx.text = "Put images in StreamingAssets/Skins/<folder>. Only compatible files are listed. < > cycles choice.";

        var scrollGo = CreateUIObject("SkinScroll", go.transform);
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(32f, 16f);
        scrollRt.offsetMax = new Vector2(-32f, -220f);

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = CreateUIObject("Viewport", scrollGo.transform);
        StretchFull(viewport);
        // ScrollRect drag/scroll needs a Graphic on the viewport; RectMask2D alone does not receive pointers.
        var vpImage = viewport.AddComponent<Image>();
        vpImage.sprite = GetOrCreateUiRaycastSprite();
        vpImage.color = new Color(1f, 1f, 1f, 0.01f);
        vpImage.raycastTarget = true;
        viewport.AddComponent<RectMask2D>();
        var vpRt = viewport.GetComponent<RectTransform>();

        var content = CreateUIObject("Content", viewport.transform);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.padding = new RectOffset(4, 4, 8, 16);
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRt;
        scroll.viewport = vpRt;
        scroll.scrollSensitivity = 35f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;
        _skinScrollRect = scroll;
        _skinScrollContentRt = contentRt;

        GameSkinPaths.EnsureSkinDirectoriesExist();
        foreach (var row in SkinRowDefs)
            CreateSkinRow(content.transform, font, row.slot, row.label);

        go.SetActive(false);
        return go;
    }

    void CreateSkinRow(Transform parent, Font font, GameSkinSlot slot, string title)
    {
        var row = CreateUIObject("Row_" + slot, parent);
        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.minHeight = 76f;
        rowLe.preferredHeight = 76f;

        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 8;
        h.padding = new RectOffset(4, 4, 4, 4);
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childForceExpandHeight = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childControlWidth = true;

        var titleGo = CreateUIObject("Label", row.transform);
        var titleLe = titleGo.AddComponent<LayoutElement>();
        titleLe.minWidth = 220f;
        titleLe.preferredWidth = 260f;
        var titleTx = titleGo.AddComponent<Text>();
        titleTx.font = font;
        titleTx.fontSize = 22;
        titleTx.alignment = TextAnchor.MiddleLeft;
        titleTx.color = Color.white;
        titleTx.text = title;
        titleTx.raycastTarget = false;

        var valueGo = CreateUIObject("Value", row.transform);
        var valueLe = valueGo.AddComponent<LayoutElement>();
        valueLe.flexibleWidth = 1f;
        valueLe.minWidth = 80f;
        var valueTx = valueGo.AddComponent<Text>();
        valueTx.font = font;
        valueTx.fontSize = 20;
        valueTx.alignment = TextAnchor.MiddleLeft;
        valueTx.color = new Color(0.45f, 0.95f, 0.9f, 1f);
        valueTx.horizontalOverflow = HorizontalWrapMode.Overflow;
        valueTx.raycastTarget = false;
        _skinRowTexts.Add((slot, valueTx));

        void AddCycleButton(string symbol, int delta)
        {
            var bgo = CreateUIObject(symbol + "_" + slot, row.transform);
            var le = bgo.AddComponent<LayoutElement>();
            le.minWidth = 64f;
            le.preferredWidth = 64f;
            var img = bgo.AddComponent<Image>();
            img.color = new Color(0.22f, 0.38f, 0.52f, 1f);
            var btn = bgo.AddComponent<Button>();
            btn.targetGraphic = img;
            int d = delta;
            GameSkinSlot s = slot;
            btn.onClick.AddListener(() => CycleSkin(s, d));
            var lg = CreateUIObject("T", bgo.transform);
            StretchFull(lg);
            var t = lg.AddComponent<Text>();
            t.font = font;
            t.fontSize = 28;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = symbol;
        }

        AddCycleButton("<", -1);
        AddCycleButton(">", 1);
        RefreshSkinRow(slot);
    }

    void CycleSkin(GameSkinSlot slot, int delta)
    {
        string dir = GameSkinPaths.GetAbsoluteSlotDirectory(slot);
        List<string> files = GameSkinCompatibility.ListCompatibleFileNames(dir);
        string cur = GameSkinStore.GetSelectedFileName(slot);
        int idxInFiles = string.IsNullOrEmpty(cur) ? -1 : files.IndexOf(cur);
        int n = files.Count;
        int totalSlots = n + 1;
        int ordinal;
        if (idxInFiles < 0)
            ordinal = 0;
        else
            ordinal = idxInFiles + 1;
        ordinal = (ordinal + delta + totalSlots) % totalSlots;
        if (ordinal == 0)
            GameSkinStore.SetSelectedFileName(slot, string.Empty);
        else
            GameSkinStore.SetSelectedFileName(slot, files[ordinal - 1]);
        RefreshSkinRow(slot);
        RefreshShipPreviewSprite();
    }

    void RefreshSkinRow(GameSkinSlot slot)
    {
        string cur = GameSkinStore.GetSelectedFileName(slot);
        string display = string.IsNullOrEmpty(cur) ? "Default (built-in)" : cur;
        for (int i = 0; i < _skinRowTexts.Count; i++)
        {
            if (_skinRowTexts[i].slot != slot)
                continue;
            _skinRowTexts[i].value.text = display;
            return;
        }
    }

    void RefreshAllSkinRows()
    {
        foreach (var row in SkinRowDefs)
            RefreshSkinRow(row.slot);
    }

    void RefreshShipPreviewSprite()
    {
        if (_shipPreviewImg == null)
            return;
        string path = GameSkinResolver.ResolvePathForLoader(string.Empty, GameSkinSlot.Player);
        var spr = RuntimeSpriteLoader.LoadSpriteFlexible(path, 100f, RuntimeSpriteLoader.DefaultPlayerSkinPreviewMaxWorldUnits);
        _shipPreviewImg.sprite = spr != null ? spr : GameplaySprites.PlayerShip();
    }

    void BuildInfoUi(Transform canvas)
    {
        var font = BuiltinFont();

        var infoBtnGo = CreateUIObject("InfoButton", canvas);
        var infoBtnRt = infoBtnGo.GetComponent<RectTransform>();
        infoBtnRt.anchorMin = new Vector2(1f, 1f);
        infoBtnRt.anchorMax = new Vector2(1f, 1f);
        infoBtnRt.pivot = new Vector2(1f, 1f);
        infoBtnRt.anchoredPosition = new Vector2(-18f, -18f);
        infoBtnRt.sizeDelta = new Vector2(168f, 64f);
        var infoBtnImg = infoBtnGo.AddComponent<Image>();
        infoBtnImg.color = new Color(0.15f, 0.35f, 0.65f, 0.94f);
        var infoBtn = infoBtnGo.AddComponent<Button>();
        infoBtn.targetGraphic = infoBtnImg;
        var infoBtnLabelGo = CreateUIObject("Text", infoBtnGo.transform);
        StretchFull(infoBtnLabelGo);
        var infoBtnTx = infoBtnLabelGo.AddComponent<Text>();
        infoBtnTx.font = font;
        infoBtnTx.fontSize = 28;
        infoBtnTx.fontStyle = FontStyle.Bold;
        infoBtnTx.alignment = TextAnchor.MiddleCenter;
        infoBtnTx.color = Color.white;
        infoBtnTx.text = "INFO";

        _infoOverlay = CreateUIObject("InfoOverlay", canvas);
        StretchFull(_infoOverlay);
        var dim = _infoOverlay.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.82f);

        var panel = CreateUIObject("InfoPanel", _infoOverlay.transform);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(920f, 1320f);
        var panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.05f, 0.07f, 0.12f, 0.96f);

        var titleGo = CreateUIObject("Title", panel.transform);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.92f);
        titleRt.anchorMax = new Vector2(0.5f, 0.92f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(880f, 56f);
        var titleTx = titleGo.AddComponent<Text>();
        titleTx.font = font;
        titleTx.fontSize = 38;
        titleTx.fontStyle = FontStyle.Bold;
        titleTx.alignment = TextAnchor.MiddleCenter;
        titleTx.color = new Color(0.45f, 0.85f, 1f, 1f);
        titleTx.text = "How to play & pickups";

        var scrollGo = CreateUIObject("Scroll", panel.transform);
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.5f, 0.52f);
        scrollRt.anchorMax = new Vector2(0.5f, 0.52f);
        scrollRt.pivot = new Vector2(0.5f, 0.5f);
        scrollRt.sizeDelta = new Vector2(860f, 1070f);
        _infoScroll = scrollGo.AddComponent<ScrollRect>();
        _infoScroll.horizontal = false;
        _infoScroll.vertical = true;
        _infoScroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = CreateUIObject("Viewport", scrollGo.transform);
        StretchFull(viewport);
        var vpRt = viewport.GetComponent<RectTransform>();
        viewport.AddComponent<RectMask2D>();

        var content = CreateUIObject("Content", viewport.transform);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0.5f, 1f);
        contentRt.anchorMax = new Vector2(0.5f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(820f, 0f);

        var bodyTx = content.AddComponent<Text>();
        bodyTx.font = font;
        bodyTx.fontSize = 24;
        bodyTx.lineSpacing = 1.15f;
        bodyTx.color = new Color(0.88f, 0.9f, 0.95f);
        bodyTx.alignment = TextAnchor.UpperLeft;
        bodyTx.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyTx.verticalOverflow = VerticalWrapMode.Overflow;
        bodyTx.text = InfoHelpText;
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _infoScroll.content = contentRt;
        _infoScroll.viewport = vpRt;

        var closeGo = CreateUIObject("CloseInfoButton", panel.transform);
        var closeRt = closeGo.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(0.5f, 0.05f);
        closeRt.anchorMax = new Vector2(0.5f, 0.05f);
        closeRt.pivot = new Vector2(0.5f, 0.5f);
        closeRt.sizeDelta = new Vector2(300f, 72f);
        var closeImg = closeGo.AddComponent<Image>();
        closeImg.color = new Color(0.35f, 0.45f, 0.65f, 1f);
        var closeBtn = closeGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        var closeLabelGo = CreateUIObject("Text", closeGo.transform);
        StretchFull(closeLabelGo);
        var closeTx = closeLabelGo.AddComponent<Text>();
        closeTx.font = font;
        closeTx.fontSize = 30;
        closeTx.fontStyle = FontStyle.Bold;
        closeTx.alignment = TextAnchor.MiddleCenter;
        closeTx.color = Color.white;
        closeTx.text = "CLOSE";

        infoBtn.onClick.AddListener(() =>
        {
            _infoOverlay.SetActive(true);
            if (_infoScroll != null)
                _infoScroll.verticalNormalizedPosition = 1f;
        });
        closeBtn.onClick.AddListener(() => _infoOverlay.SetActive(false));
        _infoOverlay.SetActive(false);
    }

    void BuildTabBar(Transform canvas)
    {
        var bar = CreateUIObject("TabBar", canvas);
        var barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0, 0);
        barRt.anchorMax = new Vector2(1, 0);
        barRt.pivot = new Vector2(0.5f, 0);
        barRt.sizeDelta = new Vector2(0, 132);
        barRt.anchoredPosition = Vector2.zero;

        var barBg = bar.AddComponent<Image>();
        barBg.color = new Color(0.95f, 0.35f, 1f, 1f);

        var layout = bar.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 8;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        _btnUpgrade = CreateTabButton(bar.transform, "UPGRADE", () => SelectTab(Tab.Upgrade));
        _btnPlayTab = CreateTabButton(bar.transform, "PLAY", () => SelectTab(Tab.Play));
        _btnShop = CreateTabButton(bar.transform, "SHOP", () => SelectTab(Tab.Shop));
        _btnSkins = CreateTabButton(bar.transform, "SKINS", () => SelectTab(Tab.Skins));
    }

    static readonly Color TabUpgradeNormal = new Color(1f, 0.55f, 0.05f, 1f);
    static readonly Color TabPlayNormal = new Color(0.1f, 0.85f, 1f, 1f);
    static readonly Color TabShopNormal = new Color(1f, 0.2f, 0.55f, 1f);
    static readonly Color TabSkinsNormal = new Color(0.35f, 0.85f, 0.45f, 1f);

    Button CreateTabButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = CreateUIObject(label + "_Tab", parent);
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 88f;
        le.flexibleWidth = 1f;

        var textGo = CreateUIObject("Text", go.transform);
        StretchFull(textGo);
        var tx = textGo.AddComponent<Text>();
        tx.font = BuiltinFont();
        tx.fontSize = label.Length > 5 ? 22 : 28;
        tx.fontStyle = FontStyle.Bold;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.color = Color.black;
        tx.text = label;
        return btn;
    }

    GameObject CreatePanel(Transform parent, string title, string body)
    {
        var go = CreateUIObject("Panel_" + title, parent);
        StretchFull(go);

        var titleGo = CreateUIObject("Title", go.transform);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.82f);
        titleRt.anchorMax = new Vector2(0.5f, 0.82f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(900, 80);

        var titleText = titleGo.AddComponent<Text>();
        titleText.font = BuiltinFont();
        titleText.fontSize = 44;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.yellow;
        titleText.text = title;

        var bodyGo = CreateUIObject("Body", go.transform);
        var bodyRt = bodyGo.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0.5f, 0.58f);
        bodyRt.anchorMax = new Vector2(0.5f, 0.58f);
        bodyRt.pivot = new Vector2(0.5f, 0.5f);
        bodyRt.sizeDelta = new Vector2(920, 120);

        var bodyText = bodyGo.AddComponent<Text>();
        bodyText.font = BuiltinFont();
        bodyText.fontSize = 30;
        bodyText.alignment = TextAnchor.MiddleCenter;
        bodyText.color = new Color(0.4f, 1f, 1f, 1f);
        bodyText.text = body;

        go.SetActive(false);
        return go;
    }

    void SelectTab(Tab tab)
    {
        _current = tab;
        _panelUpgrade.SetActive(tab == Tab.Upgrade);
        _panelPlay.SetActive(tab == Tab.Play);
        _panelShop.SetActive(tab == Tab.Shop);
        _panelSkins.SetActive(tab == Tab.Skins);

        SetTabStyle(_btnUpgrade, tab == Tab.Upgrade, TabUpgradeNormal);
        SetTabStyle(_btnPlayTab, tab == Tab.Play, TabPlayNormal);
        SetTabStyle(_btnShop, tab == Tab.Shop, TabShopNormal);
        SetTabStyle(_btnSkins, tab == Tab.Skins, TabSkinsNormal);

        if (tab == Tab.Skins)
        {
            RefreshAllSkinRows();
            RefreshSkinScrollLayout();
        }
        if (tab == Tab.Play)
        {
            RefreshShipPreviewSprite();
            RefreshNextWaveLabel();
        }
    }

    void SetTabStyle(Button btn, bool selected, Color normalBright)
    {
        var img = btn.targetGraphic as Image;
        if (img != null)
            img.color = selected ? new Color(1f, 1f, 0f, 1f) : normalBright;

        var text = btn.GetComponentInChildren<Text>();
        if (text != null)
            text.color = Color.black;
    }

    void RefreshSkinScrollLayout()
    {
        if (_skinScrollContentRt == null)
            return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_skinScrollContentRt);
        if (_skinScrollRect != null)
            _skinScrollRect.verticalNormalizedPosition = 1f;
    }

    static Sprite GetOrCreateUiRaycastSprite()
    {
        if (_cachedUiRaycastSprite != null)
            return _cachedUiRaycastSprite;
        var tex = Texture2D.whiteTexture;
        _cachedUiRaycastSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f);
        return _cachedUiRaycastSprite;
    }

    static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static void StretchFull(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }
}
