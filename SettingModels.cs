using BepInEx.Configuration;
using UnityEngine;

namespace SpiritVale.ModConfig;

public enum ModSettingKind
{
    Header,
    Toggle,
    IntegerSlider,
    FloatSlider,
    Choice,
    Text,
    Keybind,
    Button,
    Hotkey
}

public abstract class ModSetting
{
    internal ModSetting(ModSettingKind kind, string key, string label, string description)
    {
        Kind = kind;
        Key = key ?? string.Empty;
        Label = string.IsNullOrWhiteSpace(label) ? key : label;
        Description = description ?? string.Empty;
    }

    public ModSettingKind Kind { get; }
    public string Key { get; }
    public string Label { get; }
    public string Description { get; }
    public abstract object BoxedValue { get; set; }
    public abstract object BoxedDefaultValue { get; }
    public abstract void Reset();
    public event Action<ModSetting> ValueChanged;

    internal abstract void Attach(ConfigFile config, string section);
    protected void RaiseValueChanged() => ValueChanged?.Invoke(this);
}

public class SettingHandle<T> : ModSetting
{
    private T _value;
    private ConfigEntry<T> _entry;
    private readonly T _defaultValue;
    private bool _updating;

    internal SettingHandle(ModSettingKind kind, string key, string label, string description,
        T defaultValue, ConfigEntry<T> entry = null)
        : base(kind, key, label, description)
    {
        _defaultValue = defaultValue;
        _value = entry is null ? defaultValue : entry.Value;
        if (entry is not null)
            UseEntry(entry);
    }

    public T Value
    {
        get => _entry is null ? _value : _entry.Value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(Value, value))
                return;

            if (_entry is null)
                _value = value;
            else
            {
                _updating = true;
                _entry.Value = value;
                _updating = false;
            }

            Changed?.Invoke(value);
            RaiseValueChanged();
        }
    }

    public T DefaultValue => _defaultValue;
    public event Action<T> Changed;
    public override object BoxedValue
    {
        get => Value;
        set => Value = (T)value;
    }
    public override object BoxedDefaultValue => DefaultValue;
    public override void Reset() => Value = DefaultValue;

    internal override void Attach(ConfigFile config, string section)
    {
        if (_entry is not null || Kind is ModSettingKind.Header or ModSettingKind.Button)
            return;

        ConfigEntry<T> entry = config.Bind(section, Key, _defaultValue,
            new ConfigDescription(string.IsNullOrWhiteSpace(Description) ? Label : Description));
        UseEntry(entry);
    }

    private void UseEntry(ConfigEntry<T> entry)
    {
        _entry = entry;
        _value = entry.Value;
        entry.SettingChanged += OnEntrySettingChanged;
    }

    private void OnEntrySettingChanged(object sender, EventArgs args)
    {
        if (_updating)
            return;
        Changed?.Invoke(_entry.Value);
        RaiseValueChanged();
    }
}

public sealed class IntegerSliderSetting : SettingHandle<int>
{
    internal IntegerSliderSetting(string key, string label, string description, int value,
        int minimum, int maximum, int step, ConfigEntry<int> entry = null)
        : base(ModSettingKind.IntegerSlider, key, label, description, value, entry)
    {
        Minimum = minimum;
        Maximum = maximum;
        Step = Math.Max(1, step);
    }

    public int Minimum { get; }
    public int Maximum { get; }
    public int Step { get; }
}

public sealed class FloatSliderSetting : SettingHandle<float>
{
    internal FloatSliderSetting(string key, string label, string description, float value,
        float minimum, float maximum, float step, string valueFormat, ConfigEntry<float> entry = null)
        : base(ModSettingKind.FloatSlider, key, label, description, value, entry)
    {
        Minimum = minimum;
        Maximum = maximum;
        Step = Math.Max(0.0001f, step);
        ValueFormat = string.IsNullOrWhiteSpace(valueFormat) ? "0.##" : valueFormat;
    }

    public float Minimum { get; }
    public float Maximum { get; }
    public float Step { get; }
    public string ValueFormat { get; }
}

public sealed class ChoiceSetting : SettingHandle<string>
{
    internal ChoiceSetting(string key, string label, string description, string value,
        IEnumerable<string> options, ConfigEntry<string> entry = null)
        : base(ModSettingKind.Choice, key, label, description, value, entry)
    {
        Options = options.Where(option => !string.IsNullOrWhiteSpace(option)).Distinct().ToArray();
        if (Options.Count == 0)
            throw new ArgumentException("At least one non-empty choice is required.", nameof(options));
        if (!Options.Contains(Value))
            Value = Options[0];
    }

    public IReadOnlyList<string> Options { get; }
}

public sealed class ButtonSetting : ModSetting
{
    private readonly Action _action;

    internal ButtonSetting(string key, string label, string description, Action action)
        : base(ModSettingKind.Button, key, label, description) =>
        _action = action ?? throw new ArgumentNullException(nameof(action));

    public void Invoke() => _action();
    public override object BoxedValue { get => null; set { } }
    public override object BoxedDefaultValue => null;
    public override void Reset() { }
    internal override void Attach(ConfigFile config, string section) { }
}

public sealed class HeaderSetting : ModSetting
{
    internal HeaderSetting(string label, string description)
        : base(ModSettingKind.Header, string.Empty, label, description) { }

    public override object BoxedValue { get => null; set { } }
    public override object BoxedDefaultValue => null;
    public override void Reset() { }
    internal override void Attach(ConfigFile config, string section) { }
}
