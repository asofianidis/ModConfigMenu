using TMPro;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SpiritVale.ModConfig;

internal sealed class UiFactory
{
    internal static Color Surface { get; private set; } = new(0.035f, 0.045f, 0.055f, 0.98f);
    internal static Color SurfaceRaised { get; private set; } = new(0.065f, 0.075f, 0.082f, 0.98f);
    internal static Color SurfaceHover { get; private set; } = new(0.11f, 0.115f, 0.115f, 1f);
    internal static Color Accent { get; private set; } = new(0.73f, 0.58f, 0.30f, 1f);
    internal static Color AccentMuted { get; private set; } = new(0.29f, 0.245f, 0.16f, 1f);
    internal static Color Text { get; private set; } = new(0.89f, 0.91f, 0.90f, 1f);
    internal static Color TextMuted { get; private set; } = new(0.59f, 0.64f, 0.64f, 1f);
    internal static Color Divider { get; private set; } = new(0.17f, 0.19f, 0.19f, 1f);
    internal static float BodyFontSize { get; private set; } = 14f;
    internal static float TitleFontSize { get; private set; } = 20f;

    private readonly TMP_FontAsset _font;
    private readonly Sprite _panelSprite;
    private readonly Sprite _buttonSprite;
    private readonly Color _actionButtonColor;
    private readonly Color _actionTextColor;
    private readonly ColorBlock _actionButtonColors;
    private readonly Sprite _navigationOnSprite;
    private readonly Color _navigationOnColor;
    private readonly Sprite _navigationOffSprite;
    private readonly Color _navigationOffColor;
    private readonly Sprite _sliderBackgroundSprite;
    private readonly Sprite _sliderFillSprite;
    private readonly Sprite _sliderHandleSprite;
    private readonly Color _sliderBackgroundColor;
    private readonly Color _sliderFillColor;
    private readonly Color _sliderHandleColor;
    private readonly ColorBlock _nativeButtonColors;

    internal UiFactory(UISettings settings, UITabMenuItem sourceTab, GameObject templateScreen)
    {
        // Stable semantic colors sampled from SpiritVale's native settings screen.
        // Runtime objects still supply the game's sprites, font, and dimensions.
        Surface = new Color32(220, 248, 250, 255);
        SurfaceRaised = new Color32(239, 239, 239, 255);
        SurfaceHover = new Color32(226, 239, 244, 255);
        Accent = new Color32(31, 158, 242, 255);
        AccentMuted = new Color32(191, 231, 248, 255);
        Text = new Color32(50, 63, 61, 255);
        TextMuted = new Color32(128, 137, 133, 255);
        Divider = new Color32(139, 215, 244, 255);

        TMP_Text bodyText = sourceTab?.Off?.GetComponentInChildren<TMP_Text>(true)
            ?? sourceTab?.On?.GetComponentInChildren<TMP_Text>(true)
            ?? templateScreen.GetComponentsInChildren<TMP_Text>(true)
                .Where(text => text is not null && text.fontSize > 0f)
                .OrderByDescending(text => text.fontSize)
                .FirstOrDefault();
        TMP_Text titleText = sourceTab?.On?.GetComponentInChildren<TMP_Text>(true) ?? bodyText;

        _font = bodyText?.font ?? titleText?.font;
        if (bodyText is not null)
        {
            BodyFontSize = Mathf.Clamp(bodyText.fontSize, 18f, 52f);
            Text = bodyText.color;
        }
        TitleFontSize = Mathf.Clamp(titleText?.fontSize ?? BodyFontSize,
            BodyFontSize, BodyFontSize * 1.2f);

        Image panel = FindClosestAncestorImage(sourceTab?.transform) ?? FindLargestSlicedImage(settings.gameObject);
        _panelSprite = panel?.sprite;

        TMP_Dropdown nativeDropdown = settings.GetComponentInChildren<TMP_Dropdown>(true);
        Image dropdownImage = nativeDropdown?.targetGraphic as Image;
        _buttonSprite = dropdownImage?.sprite ?? _panelSprite;
        ColorBlock neutralColors = ColorBlock.defaultColorBlock;
        neutralColors.normalColor = SurfaceRaised;
        neutralColors.highlightedColor = SurfaceHover;
        neutralColors.pressedColor = AccentMuted;
        neutralColors.selectedColor = SurfaceHover;
        neutralColors.disabledColor = new Color32(226, 226, 226, 150);
        neutralColors.colorMultiplier = 1f;
        _nativeButtonColors = neutralColors;

        Image navOn = FindLargestImage(sourceTab?.On);
        Image navOff = FindLargestImage(sourceTab?.Off);
        _navigationOnSprite = navOn?.sprite ?? _buttonSprite;
        _navigationOnColor = new Color32(250, 250, 250, 255);
        _navigationOffSprite = navOff?.sprite;
        _navigationOffColor = new Color(Surface.r, Surface.g, Surface.b, 0f);

        _actionButtonColor = new Color32(38, 42, 46, 255);
        _actionTextColor = new Color32(248, 249, 249, 255);
        ColorBlock actionColors = ColorBlock.defaultColorBlock;
        actionColors.normalColor = _actionButtonColor;
        actionColors.highlightedColor = new Color32(55, 61, 66, 255);
        actionColors.pressedColor = new Color32(25, 28, 31, 255);
        actionColors.selectedColor = new Color32(55, 61, 66, 255);
        actionColors.disabledColor = new Color32(38, 42, 46, 125);
        actionColors.colorMultiplier = 1f;
        _actionButtonColors = actionColors;

        UISlider nativeSlider = settings.GetComponentInChildren<UISlider>(true);
        Image sliderBackground = nativeSlider?.slider?.targetGraphic as Image;
        Image sliderFill = nativeSlider?.slider?.fillRect?.GetComponent<Image>();
        Image sliderHandle = nativeSlider?.slider?.handleRect?.GetComponent<Image>();
        _sliderBackgroundSprite = sliderBackground?.sprite ?? _buttonSprite;
        _sliderFillSprite = sliderFill?.sprite;
        _sliderHandleSprite = sliderHandle?.sprite ?? _buttonSprite;
        _sliderBackgroundColor = SurfaceRaised;
        _sliderFillColor = Accent;
        _sliderHandleColor = Accent;
    }

    private static Image FindLargestSlicedImage(GameObject root)
    {
        Image best = null;
        float bestArea = -1f;
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (image is null || image.sprite is null || image.type != UnityEngine.UI.Image.Type.Sliced)
                continue;
            Rect rect = image.rectTransform.rect;
            float area = Mathf.Abs(rect.width * rect.height);
            if (area <= bestArea)
                continue;
            best = image;
            bestArea = area;
        }
        return best;
    }

    private static Image FindClosestAncestorImage(Transform transform)
    {
        Transform current = transform?.parent;
        while (current is not null)
        {
            Image image = current.GetComponent<Image>();
            if (image is not null && image.sprite is not null)
                return image;
            current = current.parent;
        }
        return null;
    }

    private static Image FindLargestImage(GameObject root)
    {
        if (root is null)
            return null;
        Image best = null;
        float bestArea = -1f;
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (image is null)
                continue;
            Rect rect = image.rectTransform.rect;
            float area = Mathf.Abs(rect.width * rect.height);
            if (area <= bestArea)
                continue;
            best = image;
            bestArea = area;
        }
        return best;
    }

    internal GameObject Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject obj = new(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return obj;
    }

    internal Image Image(GameObject obj, Color color, bool sliced = false)
    {
        Image image = obj.AddComponent<Image>();
        image.color = color;
        if (sliced && _panelSprite is not null)
        {
            image.sprite = _panelSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
        }
        return image;
    }

    internal TextMeshProUGUI TextBlock(string name, Transform parent, string value, float size,
        Color color, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft,
        FontStyles style = FontStyles.Normal)
    {
        GameObject obj = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        if (_font is not null)
            text.font = _font;
        text.text = value ?? string.Empty;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = style;
        text.richText = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    internal Button Button(string name, Transform parent, string label, Color background,
        Color foreground, Action onClick, float fontSize = 14f)
    {
        GameObject obj = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image image = obj.AddComponent<Image>();
        image.color = background;
        if (_buttonSprite is not null)
        {
            image.sprite = _buttonSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
        }
        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = _nativeButtonColors;
        colors.normalColor = background;
        if (colors.highlightedColor == default)
            colors.highlightedColor = SurfaceHover;
        if (colors.pressedColor == default)
            colors.pressedColor = AccentMuted;
        if (colors.selectedColor == default)
            colors.selectedColor = SurfaceHover;
        button.colors = colors;
        TextMeshProUGUI text = TextBlock("Label", obj.transform, label, fontSize, foreground,
            TextAlignmentOptions.Center, FontStyles.Normal);
        text.rectTransform.offsetMin = new Vector2(10f, 3f);
        text.rectTransform.offsetMax = new Vector2(-10f, -3f);
        WireButton(button, onClick);
        return button;
    }

    internal static void WireButton(Button button, Action onClick)
    {
        button.onClick.RemoveAllListeners();
        if (onClick is null)
            return;
        button.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(onClick));
    }

    internal static void SetButtonColor(Button button, Color color)
    {
        if (button is null)
            return;
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        button.colors = colors;
        if (button.targetGraphic is not null)
            button.targetGraphic.color = color;
    }

    internal void StyleNavigationButton(Button button, bool selected)
    {
        Image image = button?.targetGraphic as Image;
        if (image is null)
            return;
        image.sprite = selected ? _navigationOnSprite : _navigationOffSprite;
        image.type = image.sprite is null
            ? UnityEngine.UI.Image.Type.Simple : UnityEngine.UI.Image.Type.Sliced;
        SetButtonColor(button, selected ? _navigationOnColor : _navigationOffColor);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label is not null)
            label.color = selected ? Accent : Text;
    }

    internal Button ActionButton(string name, Transform parent, string label, Action onClick,
        float fontSize)
    {
        Button button = Button(name, parent, label, _actionButtonColor, _actionTextColor, onClick, fontSize);
        ColorBlock colors = _actionButtonColors;
        colors.normalColor = _actionButtonColor;
        button.colors = colors;
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text is not null)
            text.fontStyle = FontStyles.Bold;
        return button;
    }

    internal GameObject FixedRect(string name, Transform parent, float width, float height)
    {
        GameObject obj = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        LayoutElement element = obj.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        element.flexibleWidth = 1f;
        element.flexibleHeight = 0f;
        return obj;
    }

    internal void StyleSlider(Image background, Image fill, Image handle)
    {
        ApplyImageStyle(background, _sliderBackgroundSprite, _sliderBackgroundColor, true);
        ApplyImageStyle(fill, _sliderFillSprite, _sliderFillColor, false);
        ApplyImageStyle(handle, _sliderHandleSprite, _sliderHandleColor, true);
    }

    internal void StyleControl(Image image) =>
        ApplyImageStyle(image, _buttonSprite, SurfaceRaised, true);

    private static void ApplyImageStyle(Image image, Sprite sprite, Color color, bool sliced)
    {
        image.color = color;
        if (sprite is null)
            return;
        image.sprite = sprite;
        image.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
    }

    internal static void AddLayoutHeight(GameObject obj, float height)
    {
        LayoutElement element = obj.GetComponent<LayoutElement>() ?? obj.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
        element.flexibleHeight = 0f;
    }
}
