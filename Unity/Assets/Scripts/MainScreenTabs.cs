using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Main screen with three tabs: UPGRADE | PLAY | SHOP (black background, minimal UI).
/// </summary>
public class MainScreenTabs : MonoBehaviour
{
    [SerializeField] string gameplaySceneName = "Gameplay";

    enum Tab
    {
        Upgrade,
        Play,
        Shop
    }

    Tab _current = Tab.Play;
    Button _btnUpgrade;
    Button _btnPlayTab;
    Button _btnShop;
    GameObject _panelUpgrade;
    GameObject _panelPlay;
    GameObject _panelShop;

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
            cam.backgroundColor = Color.black;
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

        BuildContent(canvas.transform);
        BuildTabBar(canvas.transform);
        SelectTab(_current);
    }

    void BuildContent(Transform canvas)
    {
        var area = CreateUIObject("ContentArea", canvas);
        StretchFull(area);
        var areaRt = area.GetComponent<RectTransform>();
        areaRt.offsetMin = new Vector2(0, 140);
        areaRt.offsetMax = Vector2.zero;

        _panelUpgrade = CreatePanel(area.transform, "UPGRADE", "Upgrade tree — coming soon.");
        _panelPlay = CreatePanel(area.transform, "PLAY", "Ready when you are.");
        _panelShop = CreatePanel(area.transform, "SHOP", "Cosmetics & boosts — coming soon.");

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
        startText.font = BuiltinFont();
        startText.fontSize = 36;
        startText.fontStyle = FontStyle.Bold;
        startText.alignment = TextAnchor.MiddleCenter;
        startText.color = Color.black;
        startText.text = "START";
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
    }

    static readonly Color TabUpgradeNormal = new Color(1f, 0.55f, 0.05f, 1f);
    static readonly Color TabPlayNormal = new Color(0.1f, 0.85f, 1f, 1f);
    static readonly Color TabShopNormal = new Color(1f, 0.2f, 0.55f, 1f);

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
        tx.fontSize = 28;
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

        SetTabStyle(_btnUpgrade, tab == Tab.Upgrade, TabUpgradeNormal);
        SetTabStyle(_btnPlayTab, tab == Tab.Play, TabPlayNormal);
        SetTabStyle(_btnShop, tab == Tab.Shop, TabShopNormal);
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
