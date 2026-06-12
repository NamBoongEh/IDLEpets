using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AnimalSelectorUI : MonoBehaviour
{
    static readonly Color PanelBg    = new Color(0.07f, 0.07f, 0.07f, 0.88f);
    static readonly Color HeaderBg   = new Color(0.14f, 0.14f, 0.14f, 0.96f);
    static readonly Color BtnNormal  = new Color(0.18f, 0.18f, 0.18f, 0.92f);
    static readonly Color BtnActive  = new Color(0.15f, 0.50f, 1.00f, 0.96f);
    static readonly Color BtnHover   = new Color(0.28f, 0.28f, 0.28f, 0.95f);
    static readonly Color DividerCol = new Color(0.30f, 0.30f, 0.30f, 0.70f);
    static readonly Color TitleCol   = new Color(0.80f, 0.80f, 0.80f, 1.00f);
    static readonly Color TextCol    = new Color(0.95f, 0.95f, 0.95f, 1.00f);

    const float PanelW  = 110f;
    const float HeaderH = 28f;
    const float ToggleW = 22f;
    const float BtnH    = 26f;
    const float DivH    = 1f;
    const int   PadH    = 6;
    const int   PadV    = 5;
    const int   Spacing = 3;
    const int   FontSz  = 12;

    GameObject[]  _animals;
    Image[]       _btnImages;
    int           _current   = 0;
    bool          _minimized = false;

    GameObject    _canvasGO;
    RectTransform _panelRect;
    Canvas        _canvas;
    Text          _toggleText;
    GameObject[]  _collapsibles;

    Vector2 _dragStartPanel;
    Vector2 _dragStartMouse;

    static Font _font;

    void Start()
    {
        AnimalController[] controllers = FindObjectsOfType<AnimalController>(true);
        if (controllers.Length == 0) return;

        System.Array.Sort(controllers,
            (a, b) => a.transform.GetSiblingIndex()
                                  .CompareTo(b.transform.GetSiblingIndex()));

        _animals = new GameObject[controllers.Length];
        for (int i = 0; i < controllers.Length; i++)
            _animals[i] = controllers[i].gameObject;

        for (int i = 0; i < _animals.Length; i++)
            _animals[i].SetActive(i == 0);
        _current = 0;

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUI();
    }

    void OnDestroy()
    {
        if (_canvasGO != null)
            Destroy(_canvasGO);
    }

    void BuildUI()
    {
        _canvasGO = new GameObject("[AnimalSelectorCanvas]");
        _canvas   = _canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        var scaler = _canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;
        _canvasGO.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGO = new GameObject("[EventSystem]");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        var panelGO = MakeRect("Panel", _canvasGO.transform);
        _panelRect = panelGO.GetComponent<RectTransform>();
        _panelRect.anchorMin        = new Vector2(1f, 1f);
        _panelRect.anchorMax        = new Vector2(1f, 1f);
        _panelRect.pivot            = new Vector2(1f, 1f);
        _panelRect.anchoredPosition = new Vector2(-14f, -14f);
        _panelRect.sizeDelta        = new Vector2(PanelW, 0f);

        panelGO.AddComponent<Image>().color = PanelBg;

        var vlg = panelGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding               = new RectOffset(0, 0, 0, PadV);
        vlg.spacing               = 0;
        vlg.childAlignment        = TextAnchor.UpperCenter;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = false;
        vlg.childForceExpandWidth = true;

        var csf = panelGO.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        var headerGO = MakeRect("Header", panelGO.transform);
        headerGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, HeaderH);
        headerGO.AddComponent<Image>().color = HeaderBg;

        var hlg = headerGO.AddComponent<HorizontalLayoutGroup>();
        hlg.padding               = new RectOffset(PadH, 4, 0, 0);
        hlg.spacing               = 0;
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childControlWidth     = true;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth = false;

        var headerTrigger = headerGO.AddComponent<EventTrigger>();
        AddTrigger(headerTrigger, EventTriggerType.BeginDrag, OnBeginDrag);
        AddTrigger(headerTrigger, EventTriggerType.Drag,      OnDrag);

        var titleAreaGO = MakeRect("TitleArea", headerGO.transform);
        titleAreaGO.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var titleText = titleAreaGO.AddComponent<Text>();
        titleText.text      = "PETS";
        titleText.font      = _font;
        titleText.fontSize  = 11;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color     = TitleCol;
        titleText.alignment = TextAnchor.MiddleLeft;

        var toggleGO = MakeRect("ToggleBtn", headerGO.transform);
        var toggleLE = toggleGO.AddComponent<LayoutElement>();
        toggleLE.minWidth       = ToggleW;
        toggleLE.preferredWidth = ToggleW;
        toggleLE.flexibleWidth  = 0f;
        toggleGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

        var toggleBtn = toggleGO.AddComponent<Button>();
        toggleBtn.transition = Selectable.Transition.ColorTint;
        var cb = toggleBtn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        cb.pressedColor     = new Color(0.5f, 0.5f, 0.5f, 1f);
        toggleBtn.colors    = cb;
        toggleBtn.onClick.AddListener(ToggleMinimize);

        var toggleTextGO = MakeRect("Text", toggleGO.transform);
        var tRT = toggleTextGO.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;
        _toggleText = toggleTextGO.AddComponent<Text>();
        _toggleText.text      = "▼";
        _toggleText.font      = _font;
        _toggleText.fontSize  = 12;
        _toggleText.color     = TitleCol;
        _toggleText.alignment = TextAnchor.MiddleCenter;

        var collapsibles = new System.Collections.Generic.List<GameObject>();

        var divGO = MakeRect("Divider", panelGO.transform);
        divGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, DivH);
        divGO.AddComponent<Image>().color = DividerCol;
        collapsibles.Add(divGO);

        var topSpacer = MakeRect("TopSpacer", panelGO.transform);
        topSpacer.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, PadV);
        collapsibles.Add(topSpacer);

        _btnImages = new Image[_animals.Length];

        for (int i = 0; i < _animals.Length; i++)
        {
            int idx = i;

            var btnGO = MakeRect($"Btn_{_animals[i].name}", panelGO.transform);
            btnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, BtnH);

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color  = (i == _current) ? BtnActive : BtnNormal;
            _btnImages[i] = btnImg;

            var et = btnGO.AddComponent<EventTrigger>();
            AddTrigger(et, EventTriggerType.PointerEnter,
                _ => { if (idx != _current) _btnImages[idx].color = BtnHover; });
            AddTrigger(et, EventTriggerType.PointerExit,
                _ => { if (idx != _current) _btnImages[idx].color = BtnNormal; });

            var btn = btnGO.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => SelectAnimal(idx));

            var textGO = MakeRect("Text", btnGO.transform);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(PadH + 2f, 0f);
            textRT.offsetMax = Vector2.zero;

            var text = textGO.AddComponent<Text>();
            text.text      = _animals[i].name;
            text.font      = _font;
            text.fontSize  = FontSz;
            text.color     = TextCol;
            text.alignment = TextAnchor.MiddleLeft;

            collapsibles.Add(btnGO);

            if (i < _animals.Length - 1)
            {
                var spacer = MakeRect($"Spacer_{i}", panelGO.transform);
                spacer.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, Spacing);
                collapsibles.Add(spacer);
            }
        }

        _collapsibles = collapsibles.ToArray();
    }

    void SelectAnimal(int idx)
    {
        if (idx == _current) return;

        _animals[_current].SetActive(false);
        _btnImages[_current].color = BtnNormal;

        _current = idx;
        _animals[_current].SetActive(true);
        _btnImages[_current].color = BtnActive;
    }

    void ToggleMinimize()
    {
        _minimized = !_minimized;

        foreach (var go in _collapsibles)
            go.SetActive(!_minimized);

        _toggleText.text = _minimized ? "▶" : "▼";

        LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRect);
    }

    void OnBeginDrag(BaseEventData data)
    {
        var ped = (PointerEventData)data;
        _dragStartPanel = _panelRect.anchoredPosition;
        _dragStartMouse = ped.position;
    }

    void OnDrag(BaseEventData data)
    {
        var ped = (PointerEventData)data;
        Vector2 delta = ped.position - _dragStartMouse;
        _panelRect.anchoredPosition = _dragStartPanel + delta / _canvas.scaleFactor;
    }

    static GameObject MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static void AddTrigger(EventTrigger et, EventTriggerType type,
                           UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        et.triggers.Add(entry);
    }
}
