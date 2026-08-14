using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP.Configuration;
using UnityEngine;

namespace SpiritVale.ModConfig;

public sealed class ModPage
{
    private readonly List<ModSetting> _settings = new();

    internal ModPage(string id, string title, string description, string version)
    {
        Id = id;
        Title = title;
        Description = description ?? string.Empty;
        Version = version ?? string.Empty;
    }

    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public string Version { get; }
    public IReadOnlyList<ModSetting> Settings => _settings;

    public ModPage AddHeader(string label, string description = null)
    {
        Add(new HeaderSetting(label, description));
        return this;
    }

    public SettingHandle<bool> AddToggle(string key, string label, bool defaultValue,
        string description = null, Action<bool> onChanged = null) =>
        AddValue(new SettingHandle<bool>(ModSettingKind.Toggle, key, label, description, defaultValue), onChanged);

    public SettingHandle<bool> BindToggle(ConfigEntry<bool> entry, string label,
        string description = null, Action<bool> onChanged = null) =>
        AddValue(new SettingHandle<bool>(ModSettingKind.Toggle, entry.Definition.Key, label,
            description ?? entry.Description.Description, (bool)entry.DefaultValue, entry), onChanged);

    public IntegerSliderSetting AddIntegerSlider(string key, string label, int defaultValue,
        int minimum, int maximum, int step = 1, string description = null, Action<int> onChanged = null)
    {
        ValidateRange(minimum, maximum);
        var setting = new IntegerSliderSetting(key, label, description,
            Math.Clamp(defaultValue, minimum, maximum), minimum, maximum, step);
        return AddValue(setting, onChanged);
    }

    public IntegerSliderSetting BindIntegerSlider(ConfigEntry<int> entry, string label,
        int minimum, int maximum, int step = 1, string description = null, Action<int> onChanged = null)
    {
        ValidateRange(minimum, maximum);
        var setting = new IntegerSliderSetting(entry.Definition.Key, label,
            description ?? entry.Description.Description, (int)entry.DefaultValue,
            minimum, maximum, step, entry);
        return AddValue(setting, onChanged);
    }

    public FloatSliderSetting AddFloatSlider(string key, string label, float defaultValue,
        float minimum, float maximum, float step = 0.05f, string valueFormat = "0.##",
        string description = null, Action<float> onChanged = null)
    {
        ValidateRange(minimum, maximum);
        var setting = new FloatSliderSetting(key, label, description,
            Mathf.Clamp(defaultValue, minimum, maximum), minimum, maximum, step, valueFormat);
        return AddValue(setting, onChanged);
    }

    public FloatSliderSetting BindFloatSlider(ConfigEntry<float> entry, string label,
        float minimum, float maximum, float step = 0.05f, string valueFormat = "0.##",
        string description = null, Action<float> onChanged = null)
    {
        ValidateRange(minimum, maximum);
        var setting = new FloatSliderSetting(entry.Definition.Key, label,
            description ?? entry.Description.Description, (float)entry.DefaultValue,
            minimum, maximum, step, valueFormat, entry);
        return AddValue(setting, onChanged);
    }

    public ChoiceSetting AddChoice(string key, string label, string defaultValue,
        IEnumerable<string> options, string description = null, Action<string> onChanged = null) =>
        AddValue(new ChoiceSetting(key, label, description, defaultValue, options), onChanged);

    public ChoiceSetting BindChoice(ConfigEntry<string> entry, string label,
        IEnumerable<string> options, string description = null, Action<string> onChanged = null) =>
        AddValue(new ChoiceSetting(entry.Definition.Key, label,
            description ?? entry.Description.Description, (string)entry.DefaultValue, options, entry), onChanged);

    public SettingHandle<string> AddText(string key, string label, string defaultValue = "",
        string description = null, Action<string> onChanged = null) =>
        AddValue(new SettingHandle<string>(ModSettingKind.Text, key, label, description,
            defaultValue ?? string.Empty), onChanged);

    public SettingHandle<string> BindText(ConfigEntry<string> entry, string label,
        string description = null, Action<string> onChanged = null) =>
        AddValue(new SettingHandle<string>(ModSettingKind.Text, entry.Definition.Key, label,
            description ?? entry.Description.Description, (string)entry.DefaultValue, entry), onChanged);

    public SettingHandle<KeyCode> AddKeybind(string key, string label, KeyCode defaultValue,
        string description = null, Action<KeyCode> onChanged = null) =>
        AddValue(new SettingHandle<KeyCode>(ModSettingKind.Keybind, key, label, description,
            defaultValue), onChanged);

    public SettingHandle<KeyCode> BindKeybind(ConfigEntry<KeyCode> entry, string label,
        string description = null, Action<KeyCode> onChanged = null) =>
        AddValue(new SettingHandle<KeyCode>(ModSettingKind.Keybind, entry.Definition.Key, label,
            description ?? entry.Description.Description, (KeyCode)entry.DefaultValue, entry), onChanged);

    public SettingHandle<KeyboardShortcut> AddHotkey(string key, string label,
        KeyboardShortcut defaultValue, string description = null,
        Action<KeyboardShortcut> onChanged = null) =>
        AddValue(new SettingHandle<KeyboardShortcut>(ModSettingKind.Hotkey, key, label,
            description, defaultValue), onChanged);

    public SettingHandle<KeyboardShortcut> BindHotkey(ConfigEntry<KeyboardShortcut> entry,
        string label, string description = null, Action<KeyboardShortcut> onChanged = null) =>
        AddValue(new SettingHandle<KeyboardShortcut>(ModSettingKind.Hotkey, entry.Definition.Key,
            label, description ?? entry.Description.Description,
            (KeyboardShortcut)entry.DefaultValue, entry), onChanged);

    public ModPage AddButton(string key, string label, Action action, string description = null)
    {
        Add(new ButtonSetting(key, label, description, action));
        return this;
    }

    public void ResetAll()
    {
        foreach (ModSetting setting in _settings)
            setting.Reset();
    }

    private TSetting AddValue<TSetting, TValue>(TSetting setting, Action<TValue> onChanged)
        where TSetting : SettingHandle<TValue>
    {
        if (onChanged is not null)
            setting.Changed += onChanged;
        Add(setting);
        return setting;
    }

    private void Add(ModSetting setting)
    {
        if (setting.Kind is not ModSettingKind.Header &&
            _settings.Any(existing => string.Equals(existing.Key, setting.Key, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"The setting key '{setting.Key}' is already registered for '{Id}'.");

        _settings.Add(setting);
        ModConfigMenu.AttachStorage(this, setting);
        ModConfigMenu.NotifyRegistryChanged();
    }

    private static void ValidateRange(float minimum, float maximum)
    {
        if (minimum >= maximum)
            throw new ArgumentOutOfRangeException(nameof(maximum), "Maximum must be greater than minimum.");
    }
}

public static class ModConfigMenu
{
    private static readonly Dictionary<string, ModPage> PagesById =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly object Sync = new();
    private static ConfigFile _config;

    public static int Version => ModConfigPlugin.ApiVersion;
    public static bool IsReady => _config is not null;
    public static IReadOnlyList<ModPage> Pages
    {
        get
        {
            lock (Sync)
                return PagesById.Values.OrderBy(page => page.Title, StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }

    public static event Action RegistryChanged;

    public static ModPage Register(string id, string title, string description = null, string version = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("A stable, non-empty mod id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("A display title is required.", nameof(title));

        lock (Sync)
        {
            if (PagesById.TryGetValue(id, out ModPage existing))
                return existing;

            var page = new ModPage(id.Trim(), title.Trim(), description, version);
            PagesById.Add(page.Id, page);
            NotifyRegistryChanged();
            return page;
        }
    }

    public static bool TryGetPage(string id, out ModPage page)
    {
        lock (Sync)
            return PagesById.TryGetValue(id, out page);
    }

    public static bool Unregister(string id)
    {
        lock (Sync)
        {
            bool removed = PagesById.Remove(id);
            if (removed)
                NotifyRegistryChanged();
            return removed;
        }
    }

    internal static void Initialize(ConfigFile config)
    {
        _config = config;
        foreach (ModPage page in Pages)
            foreach (ModSetting setting in page.Settings)
                AttachStorage(page, setting);
    }

    internal static void AttachStorage(ModPage page, ModSetting setting)
    {
        if (_config is null)
            return;
        setting.Attach(_config, $"{Sanitize(page.Id)}");
    }

    internal static void NotifyRegistryChanged() => RegistryChanged?.Invoke();

    internal static void Shutdown()
    {
        RegistryChanged = null;
        _config = null;
        lock (Sync)
            PagesById.Clear();
    }

    private static string Sanitize(string value)
    {
        char[] result = value.Select(character =>
            char.IsLetterOrDigit(character) || character is '.' or '-' or '_' ? character : '_').ToArray();
        return new string(result);
    }
}
