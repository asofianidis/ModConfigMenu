using TMPro;
using BepInEx.Unity.IL2CPP.Configuration;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SpiritVale.ModConfig;

internal sealed class SettingsMenuController
{
    private const string ScreenName = "ModConfigMenu.Screen";
    private static readonly List<SettingsMenuController> Controllers = new();
    private static readonly List<KeyCode> CapturedHotkeyKeys = new();
    private static Action<KeyCode> _keyCaptureCallback;
    private static Action<KeyboardShortcut> _hotkeyCaptureCallback;
    private static TMP_Text _captureLabel;
    private static string _capturePreviousText;
    private static int _captureStartFrame;

    private readonly GameObject _screen;
    private readonly UiFactory _ui;
    private readonly Transform _navigation;
    private readonly Transform _content;
    private readonly TMP_Text _title;
    private readonly TMP_Text _summary;
    private readonly TMP_Text _empty;
    private readonly Dictionary<string, Image> _navigationImages = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(ModSetting Setting, Action<ModSetting> Handler)> _settingListeners = new();
    private ModPage _selected;

    private SettingsMenuController(GameObject screen, UISettings settings, UITabMenuItem sourceTab,
        GameObject templateScreen)
    {
        _screen = screen;
        _ui = new UiFactory(settings, sourceTab, templateScreen);
        float body = UiFactory.BodyFontSize;
        float padding = Mathf.Clamp(body * 1.35f, 24f, 68f);

        GameObject sidebar = _ui.Rect("Navigation", screen.transform,
            new Vector2(0f, 0f), new Vector2(0.18f, 1f),
            new Vector2(padding * 0.28f, padding * 0.28f),
            new Vector2(-padding * 0.28f, -padding * 0.28f));
        _ui.Image(sidebar, UiFactory.Surface, true);

        TMP_Text navTitle = _ui.TextBlock("Title", sidebar.transform, "INSTALLED MODS", body * 0.86f,
            UiFactory.Accent, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
        navTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        navTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        navTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
        navTitle.rectTransform.offsetMin = new Vector2(padding, -body * 2.65f);
        navTitle.rectTransform.offsetMax = new Vector2(-padding, -padding * 0.55f);

        GameObject navScroll = _ui.Rect("Scroll", sidebar.transform, Vector2.zero, Vector2.one,
            new Vector2(padding * 0.55f, padding * 0.55f),
            new Vector2(-padding * 0.55f, -body * 3f));
        ScrollRect navScrollRect = navScroll.AddComponent<ScrollRect>();
        GameObject navViewport = _ui.Rect("Viewport", navScroll.transform, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        _ui.Image(navViewport, new Color(0f, 0f, 0f, 0.001f));
        navViewport.AddComponent<RectMask2D>();
        GameObject navContent = _ui.Rect("Content", navViewport.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        RectTransform navContentRect = navContent.GetComponent<RectTransform>();
        navContentRect.pivot = new Vector2(0.5f, 1f);
        VerticalLayoutGroup navLayout = navContent.AddComponent<VerticalLayoutGroup>();
        navLayout.spacing = Mathf.Max(3f, body * 0.3f);
        navLayout.childControlHeight = true;
        navLayout.childControlWidth = true;
        navLayout.childForceExpandHeight = false;
        navLayout.childForceExpandWidth = true;
        ContentSizeFitter navFitter = navContent.AddComponent<ContentSizeFitter>();
        navFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        navScrollRect.viewport = navViewport.GetComponent<RectTransform>();
        navScrollRect.content = navContentRect;
        navScrollRect.horizontal = false;
        navScrollRect.vertical = true;
        navScrollRect.scrollSensitivity = body * 2f;
        _navigation = navContent.transform;

        GameObject main = _ui.Rect("Main", screen.transform, Vector2.zero, Vector2.one,
            new Vector2(0f, padding), new Vector2(-padding, -padding));
        RectTransform mainRect = main.GetComponent<RectTransform>();
        mainRect.anchorMin = new Vector2(0.205f, 0f);
        _title = _ui.TextBlock("PageTitle", main.transform, "MOD CONFIGURATION", UiFactory.TitleFontSize,
            UiFactory.Text, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
        _title.rectTransform.anchorMin = new Vector2(0f, 1f);
        _title.rectTransform.anchorMax = new Vector2(1f, 1f);
        _title.rectTransform.pivot = new Vector2(0.5f, 1f);
        _title.rectTransform.offsetMin = new Vector2(0f, -body * 2.35f);
        _title.rectTransform.offsetMax = new Vector2(-body * 7.8f, 0f);

        _summary = _ui.TextBlock("Summary", main.transform, string.Empty, body * 0.82f,
            UiFactory.TextMuted, TextAlignmentOptions.TopLeft);
        _summary.rectTransform.anchorMin = new Vector2(0f, 1f);
        _summary.rectTransform.anchorMax = new Vector2(1f, 1f);
        _summary.rectTransform.pivot = new Vector2(0.5f, 1f);
        _summary.rectTransform.offsetMin = new Vector2(0f, -body * 5.1f);
        _summary.rectTransform.offsetMax = new Vector2(-padding, -body * 2.5f);

        Button reset = _ui.Button("Reset", main.transform, "RESET PAGE", UiFactory.SurfaceRaised,
            UiFactory.Text, () => _selected?.ResetAll(), body * 0.72f);
        RectTransform resetRect = reset.GetComponent<RectTransform>();
        resetRect.anchorMin = new Vector2(1f, 1f);
        resetRect.anchorMax = new Vector2(1f, 1f);
        resetRect.pivot = new Vector2(1f, 1f);
        resetRect.anchoredPosition = Vector2.zero;
        resetRect.sizeDelta = new Vector2(body * 7.2f, body * 2.15f);

        GameObject pageScroll = _ui.Rect("SettingsScroll", main.transform, Vector2.zero, Vector2.one,
            Vector2.zero, new Vector2(0f, -body * 5.65f));
        ScrollRect pageScrollRect = pageScroll.AddComponent<ScrollRect>();
        GameObject pageViewport = _ui.Rect("Viewport", pageScroll.transform, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        _ui.Image(pageViewport, new Color(0f, 0f, 0f, 0.001f));
        pageViewport.AddComponent<RectMask2D>();
        GameObject pageContent = _ui.Rect("Content", pageViewport.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        RectTransform contentRect = pageContent.GetComponent<RectTransform>();
        contentRect.pivot = new Vector2(0.5f, 1f);
        VerticalLayoutGroup contentLayout = pageContent.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 0f;
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;
        ContentSizeFitter contentFitter = pageContent.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        pageScrollRect.viewport = pageViewport.GetComponent<RectTransform>();
        pageScrollRect.content = contentRect;
        pageScrollRect.horizontal = false;
        pageScrollRect.vertical = true;
        pageScrollRect.scrollSensitivity = body * 2.25f;
        _content = pageContent.transform;

        _empty = _ui.TextBlock("Empty", main.transform,
            "No mod menus are registered yet.\nInstalled mods can register settings through the public MCM API.",
            body, UiFactory.TextMuted, TextAlignmentOptions.Center);
        _empty.rectTransform.anchorMin = new Vector2(0f, 0f);
        _empty.rectTransform.anchorMax = new Vector2(1f, 0.72f);
        _empty.rectTransform.offsetMin = new Vector2(padding, padding);
        _empty.rectTransform.offsetMax = new Vector2(-padding, -padding);

        RebuildNavigation();
        ModConfigMenu.RegistryChanged += RebuildNavigation;
    }

    internal static void TryInject(UITabMenu tabMenu)
    {
        UISettings settings = tabMenu.GetComponentInParent<UISettings>();
        if (settings is null || tabMenu.tabs is null || tabMenu.screens is null ||
            tabMenu.tabs.Count == 0 || tabMenu.screens.Count == 0)
            return;

        Transform screenParent = tabMenu.screens[0]?.transform?.parent;
        if (screenParent is null || screenParent.Find(ScreenName) is not null)
            return;

        UITabMenuItem sourceTab = tabMenu.tabs[tabMenu.tabs.Count - 1];
        GameObject tabObject = Object.Instantiate(sourceTab.gameObject, sourceTab.transform.parent, false);
        tabObject.name = "ModConfigMenu.Tab";
        foreach (TMP_Text label in tabObject.GetComponentsInChildren<TMP_Text>(true))
            label.text = "MODS";
        foreach (Button button in tabObject.GetComponentsInChildren<Button>(true))
            button.onClick.RemoveAllListeners();
        UITabMenuItem tab = tabObject.GetComponent<UITabMenuItem>();

        GameObject screen = new(ScreenName);
        screen.transform.SetParent(screenParent, false);
        RectTransform rect = screen.AddComponent<RectTransform>();
        RectTransform templateRect = tabMenu.screens[0].GetComponent<RectTransform>();
        rect.anchorMin = templateRect.anchorMin;
        rect.anchorMax = templateRect.anchorMax;
        rect.pivot = templateRect.pivot;
        rect.anchoredPosition3D = templateRect.anchoredPosition3D;
        rect.sizeDelta = templateRect.sizeDelta;
        rect.localScale = templateRect.localScale;
        rect.localRotation = templateRect.localRotation;
        screen.SetActive(false);

        tabMenu.tabs.Add(tab);
        tabMenu.screens.Add(screen);
    }

    internal static void TryCompleteInjection(UITabMenu tabMenu)
    {
        UISettings settings = tabMenu.GetComponentInParent<UISettings>();
        if (settings is null || tabMenu.tabs is null || tabMenu.screens is null ||
            tabMenu.tabs.Count < 2 || tabMenu.screens.Count < 2)
            return;

        Transform screenParent = tabMenu.screens[0]?.transform?.parent;
        GameObject screen = screenParent?.Find(ScreenName)?.gameObject;
        if (screen is null || Controllers.Any(controller => controller._screen == screen))
            return;

        UITabMenuItem sourceTab = tabMenu.tabs[tabMenu.tabs.Count - 2];
        ModConfigPlugin.Info("Native settings tabs initialized; building the MODS screen.");
        var controller = new SettingsMenuController(screen, settings, sourceTab, tabMenu.screens[0]);
        Controllers.Add(controller);
        RectTransform templateRect = tabMenu.screens[0].GetComponent<RectTransform>();
        Canvas canvas = settings.GetComponentInParent<Canvas>();
        ModConfigPlugin.Info($"Added the MODS tab to SpiritVale settings. Native content "
            + $"{templateRect.rect.width:0.#}x{templateRect.rect.height:0.#}, "
            + $"local scale {templateRect.localScale.x:0.###}, canvas scale {canvas?.scaleFactor ?? 1f:0.###}.");
    }

    internal static bool TickKeyCapture()
    {
        if (_keyCaptureCallback is null && _hotkeyCaptureCallback is null)
            return false;
        if (_captureLabel is null || !_captureLabel.gameObject.activeInHierarchy)
        {
            CancelKeyCapture();
            return false;
        }
        if (Time.frameCount <= _captureStartFrame)
            return false;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelKeyCapture();
            return true;
        }

        if (_keyCaptureCallback is not null)
        {
            if (!Input.anyKeyDown)
                return false;
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (!IsBindableKey(key) || !Input.GetKeyDown(key))
                    continue;

                Action<KeyCode> callback = _keyCaptureCallback;
                TMP_Text label = _captureLabel;
                ClearCapture();
                callback(key);
                if (label is not null)
                    label.text = FriendlyKey(key);
                return true;
            }
            return false;
        }

        bool consumed = false;
        if (Input.anyKeyDown)
        {
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (!IsBindableKey(key) || !Input.GetKeyDown(key) || CapturedHotkeyKeys.Contains(key))
                    continue;
                CapturedHotkeyKeys.Add(key);
                consumed = true;
            }
            if (CapturedHotkeyKeys.Count > 0)
                _captureLabel.text = FriendlyHotkey(CapturedHotkeyKeys);
        }

        if (CapturedHotkeyKeys.Count == 0 || CapturedHotkeyKeys.Any(Input.GetKey))
            return consumed;

        KeyCode mainKey = CapturedHotkeyKeys[^1];
        KeyCode[] modifiers = CapturedHotkeyKeys.Take(CapturedHotkeyKeys.Count - 1).ToArray();
        var hotkey = new KeyboardShortcut(mainKey, modifiers);
        Action<KeyboardShortcut> hotkeyCallback = _hotkeyCaptureCallback;
        TMP_Text hotkeyLabel = _captureLabel;
        ClearCapture();
        hotkeyCallback(hotkey);
        if (hotkeyLabel is not null)
            hotkeyLabel.text = FriendlyHotkey(hotkey);
        return true;
    }

    internal static void CancelKeyCapture()
    {
        if (_captureLabel is not null)
            _captureLabel.text = _capturePreviousText;
        ClearCapture();
    }

    internal static void Shutdown()
    {
        CancelKeyCapture();
        foreach (SettingsMenuController controller in Controllers)
        {
            ModConfigMenu.RegistryChanged -= controller.RebuildNavigation;
            controller.ClearPage();
            if (controller._screen is not null)
                Object.Destroy(controller._screen);
        }
        Controllers.Clear();
    }

    private static void BeginKeyCapture(TMP_Text label, Action<KeyCode> callback)
    {
        CancelKeyCapture();
        _captureLabel = label;
        _capturePreviousText = label.text;
        _keyCaptureCallback = callback;
        _captureStartFrame = Time.frameCount;
        label.text = "PRESS A KEY";
    }

    private static void BeginHotkeyCapture(TMP_Text label, Action<KeyboardShortcut> callback)
    {
        CancelKeyCapture();
        _captureLabel = label;
        _capturePreviousText = label.text;
        _hotkeyCaptureCallback = callback;
        _captureStartFrame = Time.frameCount;
        label.text = "PRESS HOTKEY";
    }

    private static void ClearCapture()
    {
        _keyCaptureCallback = null;
        _hotkeyCaptureCallback = null;
        CapturedHotkeyKeys.Clear();
        _captureLabel = null;
        _capturePreviousText = null;
        _captureStartFrame = 0;
    }

    private void RebuildNavigation()
    {
        if (_screen is null)
            return;
        ClearChildren(_navigation);
        _navigationImages.Clear();
        IReadOnlyList<ModPage> pages = ModConfigMenu.Pages;
        _empty.gameObject.SetActive(pages.Count == 0);

        foreach (ModPage page in pages)
        {
            Button button = _ui.Button($"Mod.{page.Id}", _navigation, page.Title,
                UiFactory.SurfaceRaised, UiFactory.Text, () => SelectPage(page), UiFactory.BodyFontSize * 0.9f);
            _ui.StyleNavigationButton(button, false);
            UiFactory.AddLayoutHeight(button.gameObject, UiFactory.BodyFontSize * 3f);
            _navigationImages[page.Id] = button.targetGraphic as Image;
        }

        if (pages.Count == 0)
        {
            _selected = null;
            _title.text = "MOD CONFIGURATION";
            _summary.text = string.Empty;
            ClearPage();
            return;
        }

        ModPage selection = _selected is not null
            ? pages.FirstOrDefault(page => page.Id == _selected.Id) ?? pages[0]
            : pages[0];
        SelectPage(selection);
    }

    private void SelectPage(ModPage page)
    {
        _selected = page;
        foreach ((string id, Image image) in _navigationImages)
            if (image is not null)
                _ui.StyleNavigationButton(image.GetComponent<Button>(),
                    id.Equals(page.Id, StringComparison.OrdinalIgnoreCase));
        RebuildPage();
    }

    private void RebuildPage()
    {
        ClearPage();
        if (_selected is null)
            return;
        _title.text = string.IsNullOrWhiteSpace(_selected.Version)
            ? _selected.Title
            : $"{_selected.Title}  v{_selected.Version}";
        _title.richText = false;
        _summary.text = _selected.Description;
        foreach (ModSetting setting in _selected.Settings)
            BuildSetting(setting);
    }

    private void BuildSetting(ModSetting setting)
    {
        if (setting is HeaderSetting)
        {
            float headerHeight = string.IsNullOrWhiteSpace(setting.Description)
                ? UiFactory.BodyFontSize * 2.4f : UiFactory.BodyFontSize * 3.25f;
            GameObject header = _ui.FixedRect($"Header.{setting.Label}", _content, 0f, headerHeight);
            TMP_Text label = _ui.TextBlock("Label", header.transform, setting.Label,
                UiFactory.BodyFontSize * 0.88f,
                UiFactory.Accent, TextAlignmentOptions.BottomLeft, FontStyles.Normal);
            label.rectTransform.anchorMin = new Vector2(0f, 0.42f);
            label.rectTransform.offsetMin = new Vector2(2f, 0f);
            label.rectTransform.offsetMax = new Vector2(-2f, -7f);
            if (!string.IsNullOrWhiteSpace(setting.Description))
            {
                TMP_Text note = _ui.TextBlock("Description", header.transform, setting.Description,
                    UiFactory.BodyFontSize * 0.75f,
                    UiFactory.TextMuted, TextAlignmentOptions.TopLeft);
                note.rectTransform.anchorMax = new Vector2(1f, 0.42f);
                note.rectTransform.offsetMin = new Vector2(2f, 5f);
                note.rectTransform.offsetMax = new Vector2(-2f, 0f);
            }
            return;
        }

        float rowHeight = UiFactory.BodyFontSize * 3.25f;
        GameObject row = _ui.FixedRect($"Setting.{setting.Key}", _content, 0f, rowHeight);
        GameObject divider = _ui.Rect("Divider", row.transform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            Vector2.zero, new Vector2(0f, 1f));
        _ui.Image(divider, UiFactory.Divider);
        TMP_Text labelText = _ui.TextBlock("Label", row.transform, setting.Label,
            UiFactory.BodyFontSize,
            UiFactory.Text, TextAlignmentOptions.BottomLeft, FontStyles.Normal);
        labelText.rectTransform.anchorMin = new Vector2(0f, 0.46f);
        labelText.rectTransform.anchorMax = new Vector2(0.55f, 1f);
        labelText.rectTransform.offsetMin = new Vector2(2f, 0f);
        labelText.rectTransform.offsetMax = new Vector2(-18f, -4f);
        TMP_Text description = _ui.TextBlock("Description", row.transform, setting.Description,
            UiFactory.BodyFontSize * 0.76f,
            UiFactory.TextMuted, TextAlignmentOptions.TopLeft);
        description.rectTransform.anchorMin = new Vector2(0f, 0f);
        description.rectTransform.anchorMax = new Vector2(0.55f, 0.46f);
        description.rectTransform.offsetMin = new Vector2(2f, 7f);
        description.rectTransform.offsetMax = new Vector2(-18f, 0f);

        GameObject control = _ui.Rect("Control", row.transform,
            new Vector2(0.62f, 0.16f), new Vector2(0.96f, 0.84f), Vector2.zero, Vector2.zero);
        switch (setting)
        {
            case SettingHandle<bool> toggle:
                BuildToggle(control, toggle);
                break;
            case IntegerSliderSetting integer:
                BuildIntegerSlider(control, integer);
                break;
            case FloatSliderSetting number:
                BuildFloatSlider(control, number);
                break;
            case ChoiceSetting choice:
                BuildChoice(control, choice);
                break;
            case SettingHandle<string> text:
                BuildText(control, text);
                break;
            case SettingHandle<KeyCode> keybind:
                BuildKeybind(control, keybind);
                break;
            case SettingHandle<KeyboardShortcut> hotkey:
                BuildHotkey(control, hotkey);
                break;
            case ButtonSetting button:
                BuildAction(control, button);
                break;
        }
    }

    private void BuildToggle(GameObject parent, SettingHandle<bool> setting)
    {
        Button button = _ui.Button("Toggle", parent.transform, setting.Value ? "ON" : "OFF",
            setting.Value ? UiFactory.Accent : UiFactory.SurfaceRaised,
            UiFactory.Text, () => setting.Value = !setting.Value, UiFactory.BodyFontSize * 0.9f);
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = new Vector2(UiFactory.BodyFontSize * 3.4f,
            UiFactory.BodyFontSize * 1.65f);
        TMP_Text value = button.GetComponentInChildren<TMP_Text>(true);
        void Refresh(bool current)
        {
            if (button is null) return;
            UiFactory.SetButtonColor(button, current ? UiFactory.Accent : UiFactory.SurfaceRaised);
            value.text = current ? "ON" : "OFF";
            value.color = current
                ? new Color32(248, 249, 249, 255) : UiFactory.Text;
        }
        Listen(setting, () => Refresh(setting.Value));
        Refresh(setting.Value);
    }

    private void BuildIntegerSlider(GameObject parent, IntegerSliderSetting setting)
    {
        (Slider slider, TMP_Text value) = BuildSlider(parent, setting.Minimum, setting.Maximum, setting.Value, true);
        value.text = setting.Value.ToString();
        slider.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<float>>(
            new Action<float>(raw =>
        {
            int stepped = Math.Clamp((int)Math.Round(raw / setting.Step) * setting.Step,
                setting.Minimum, setting.Maximum);
            setting.Value = stepped;
            value.text = stepped.ToString();
        })));
        Listen(setting, () =>
        {
            if (slider is null) return;
            slider.SetValueWithoutNotify(setting.Value);
            value.text = setting.Value.ToString();
        });
    }

    private void BuildFloatSlider(GameObject parent, FloatSliderSetting setting)
    {
        (Slider slider, TMP_Text value) = BuildSlider(parent, setting.Minimum, setting.Maximum, setting.Value, false);
        value.text = setting.Value.ToString(setting.ValueFormat);
        slider.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<float>>(
            new Action<float>(raw =>
        {
            float stepped = Mathf.Clamp(Mathf.Round(raw / setting.Step) * setting.Step,
                setting.Minimum, setting.Maximum);
            setting.Value = stepped;
            value.text = stepped.ToString(setting.ValueFormat);
        })));
        Listen(setting, () =>
        {
            if (slider is null) return;
            slider.SetValueWithoutNotify(setting.Value);
            value.text = setting.Value.ToString(setting.ValueFormat);
        });
    }

    private (Slider, TMP_Text) BuildSlider(GameObject parent, float minimum, float maximum,
        float current, bool wholeNumbers)
    {
        GameObject track = _ui.Rect("Track", parent.transform,
            new Vector2(0f, 0.34f), new Vector2(0.76f, 0.66f), Vector2.zero, Vector2.zero);
        Image trackImage = _ui.Image(track, UiFactory.SurfaceRaised, true);
        Slider slider = track.AddComponent<Slider>();
        GameObject fill = _ui.Rect("Fill", track.transform, new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(3f, 3f), new Vector2(-3f, -3f));
        Image fillImage = _ui.Image(fill, UiFactory.Accent);
        GameObject handle = _ui.Rect("Handle", track.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(-7f, -10f), new Vector2(7f, 10f));
        Image handleImage = _ui.Image(handle, UiFactory.Text, true);
        _ui.StyleSlider(trackImage, fillImage, handleImage);
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handleImage;
        ColorBlock sliderColors = ColorBlock.defaultColorBlock;
        sliderColors.normalColor = UiFactory.Accent;
        sliderColors.highlightedColor = new Color32(83, 187, 247, 255);
        sliderColors.pressedColor = new Color32(24, 132, 215, 255);
        sliderColors.selectedColor = new Color32(83, 187, 247, 255);
        sliderColors.disabledColor = new Color32(150, 197, 219, 140);
        sliderColors.colorMultiplier = 1f;
        slider.colors = sliderColors;
        slider.minValue = minimum;
        slider.maxValue = maximum;
        slider.wholeNumbers = wholeNumbers;
        slider.value = current;
        TMP_Text value = _ui.TextBlock("Value", parent.transform, string.Empty,
            UiFactory.BodyFontSize * 0.9f,
            UiFactory.Text, TextAlignmentOptions.Center, FontStyles.Normal);
        value.rectTransform.anchorMin = new Vector2(0.79f, 0f);
        value.rectTransform.anchorMax = Vector2.one;
        return (slider, value);
    }

    private void BuildChoice(GameObject parent, ChoiceSetting setting)
    {
        Button previous = _ui.Button("Previous", parent.transform, "<", UiFactory.SurfaceRaised,
            UiFactory.Text, () => MoveChoice(-1), UiFactory.BodyFontSize * 1.15f);
        previous.GetComponent<RectTransform>().anchorMax = new Vector2(0.18f, 1f);
        GameObject valueBackground = _ui.Rect("ValueBackground", parent.transform,
            new Vector2(0.2f, 0f), new Vector2(0.8f, 1f), Vector2.zero, Vector2.zero);
        Image valueImage = _ui.Image(valueBackground, UiFactory.SurfaceRaised, true);
        _ui.StyleControl(valueImage);
        TMP_Text value = _ui.TextBlock("Value", valueBackground.transform, setting.Value,
            UiFactory.BodyFontSize * 0.9f,
            UiFactory.Text, TextAlignmentOptions.Center, FontStyles.Normal);
        Button next = _ui.Button("Next", parent.transform, ">", UiFactory.SurfaceRaised,
            UiFactory.Text, () => MoveChoice(1), UiFactory.BodyFontSize * 1.15f);
        next.GetComponent<RectTransform>().anchorMin = new Vector2(0.82f, 0f);
        Listen(setting, () => { if (value is not null) value.text = setting.Value; });

        void MoveChoice(int delta)
        {
            int index = setting.Options.IndexOf(setting.Value);
            index = (index + delta + setting.Options.Count) % setting.Options.Count;
            setting.Value = setting.Options[index];
        }
    }

    private void BuildText(GameObject parent, SettingHandle<string> setting)
    {
        Image background = _ui.Image(parent, UiFactory.SurfaceRaised, true);
        _ui.StyleControl(background);
        TMP_InputField input = parent.AddComponent<TMP_InputField>();
        TMP_Text text = _ui.TextBlock("Text", parent.transform, setting.Value,
            UiFactory.BodyFontSize * 0.86f,
            UiFactory.Text, TextAlignmentOptions.MidlineLeft);
        text.rectTransform.offsetMin = new Vector2(12f, 3f);
        text.rectTransform.offsetMax = new Vector2(-12f, -3f);
        input.textViewport = parent.GetComponent<RectTransform>();
        input.textComponent = text;
        input.text = setting.Value;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.onEndEdit.AddListener(DelegateSupport.ConvertDelegate<UnityAction<string>>(
            new Action<string>(value => setting.Value = value ?? string.Empty)));
        Listen(setting, () =>
        {
            if (input is not null && input.text != setting.Value)
                input.SetTextWithoutNotify(setting.Value);
        });
    }

    private void BuildKeybind(GameObject parent, SettingHandle<KeyCode> setting)
    {
        Button button = _ui.Button("Keybind", parent.transform, FriendlyKey(setting.Value),
            UiFactory.SurfaceRaised, UiFactory.Text, null, UiFactory.BodyFontSize * 0.86f);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        UiFactory.WireButton(button, () => BeginKeyCapture(text, key => setting.Value = key));
        Listen(setting, () => { if (text is not null) text.text = FriendlyKey(setting.Value); });
    }

    private void BuildHotkey(GameObject parent, SettingHandle<KeyboardShortcut> setting)
    {
        Button button = _ui.Button("Hotkey", parent.transform, FriendlyHotkey(setting.Value),
            UiFactory.SurfaceRaised, UiFactory.Text, null, UiFactory.BodyFontSize * 0.78f);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        UiFactory.WireButton(button, () => BeginHotkeyCapture(text, hotkey => setting.Value = hotkey));
        Listen(setting, () => { if (text is not null) text.text = FriendlyHotkey(setting.Value); });
    }

    private void BuildAction(GameObject parent, ButtonSetting setting)
    {
        _ui.ActionButton("Action", parent.transform, setting.Label, () =>
            {
                setting.Invoke();
            }, UiFactory.BodyFontSize * 0.8f);
    }

    private static string FriendlyKey(KeyCode key)
    {
        string alias = key switch
        {
            KeyCode.LeftControl => "L CTRL",
            KeyCode.RightControl => "R CTRL",
            KeyCode.LeftShift => "L SHIFT",
            KeyCode.RightShift => "R SHIFT",
            KeyCode.LeftAlt => "L ALT",
            KeyCode.RightAlt => "R ALT",
            KeyCode.LeftCommand => "L CMD",
            KeyCode.RightCommand => "R CMD",
            KeyCode.LeftWindows => "L WIN",
            KeyCode.RightWindows => "R WIN",
            KeyCode.Return => "ENTER",
            KeyCode.KeypadEnter => "NUM ENTER",
            KeyCode.Backspace => "BACKSPACE",
            KeyCode.None => "UNBOUND",
            _ => null
        };
        if (alias is not null)
            return alias;
        string value = key.ToString();
        return value.StartsWith("Alpha", StringComparison.Ordinal) ? value[5..] : value;
    }

    private static string FriendlyHotkey(KeyboardShortcut hotkey)
    {
        if (hotkey.MainKey == KeyCode.None)
            return "UNBOUND";
        IEnumerable<KeyCode> modifiers = hotkey.Modifiers ?? Array.Empty<KeyCode>();
        return string.Join(" + ", modifiers.Append(hotkey.MainKey).Select(FriendlyKey));
    }

    private static string FriendlyHotkey(IEnumerable<KeyCode> keys) =>
        string.Join(" + ", keys.Select(FriendlyKey));

    private static bool IsBindableKey(KeyCode key) =>
        key != KeyCode.None && key is not (>= KeyCode.Mouse0 and <= KeyCode.Mouse6);

    private void Listen(ModSetting setting, Action refresh)
    {
        Action<ModSetting> handler = _ => refresh();
        setting.ValueChanged += handler;
        _settingListeners.Add((setting, handler));
    }

    private void ClearPage()
    {
        foreach ((ModSetting setting, Action<ModSetting> handler) in _settingListeners)
            setting.ValueChanged -= handler;
        _settingListeners.Clear();
        ClearChildren(_content);
    }

    private static void ClearChildren(Transform parent)
    {
        for (int index = parent.childCount - 1; index >= 0; index--)
            Object.Destroy(parent.GetChild(index).gameObject);
    }
}

internal static class ReadOnlyListExtensions
{
    internal static int IndexOf<T>(this IReadOnlyList<T> values, T value)
    {
        for (int index = 0; index < values.Count; index++)
            if (EqualityComparer<T>.Default.Equals(values[index], value))
                return index;
        return -1;
    }
}
