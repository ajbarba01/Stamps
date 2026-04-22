namespace Stamps.Core;

/// <summary>
/// Declarative description of a single user-configurable setting on a tweak or action.
/// The host renders these; the tweak never touches UI code directly. Concrete subclasses
/// carry type-specific metadata (defaults, constraints, enumerations).
/// </summary>
/// <remarks>
/// Instances are immutable; add a new concrete record when a new editor type is needed.
/// The <see cref="Key"/> is the identifier used to read and write the value through
/// <see cref="SettingsValues"/>, and must be unique within its owning collection.
/// </remarks>
public abstract record SettingDescriptor(string Key, string Label, string? Description = null);

/// <summary>On/off switch. Default controls the initial value when no saved value exists.</summary>
public sealed record ToggleSetting(
    string Key, string Label, bool Default = false, string? Description = null)
    : SettingDescriptor(Key, Label, Description);

/// <summary>Single-line text input.</summary>
public sealed record TextSetting(
    string Key, string Label, string Default = "", string? Placeholder = null, string? Description = null)
    : SettingDescriptor(Key, Label, Description);

/// <summary>A filesystem path chooser. <see cref="Filter"/> is a Win32 file-dialog filter
/// (e.g., <c>"Executable (*.exe)|*.exe"</c>); <c>null</c> accepts any file.</summary>
public sealed record FilePathSetting(
    string Key, string Label, string? Filter = null, string Default = "", string? Description = null)
    : SettingDescriptor(Key, Label, Description);

/// <summary>Numeric input, optionally constrained to a range.</summary>
public sealed record NumberSetting(
    string Key, string Label, double Default = 0,
    double Min = double.MinValue, double Max = double.MaxValue,
    string? Description = null)
    : SettingDescriptor(Key, Label, Description);

/// <summary>Closed set of string options; the chosen value is stored verbatim.</summary>
public sealed record DropdownSetting(
    string Key, string Label, IReadOnlyList<string> Options, string Default, string? Description = null)
    : SettingDescriptor(Key, Label, Description);

/// <summary>A global hotkey binding. A null default means "unbound".</summary>
public sealed record HotkeySetting(
    string Key, string Label, Hotkey? Default = null, string? Description = null)
    : SettingDescriptor(Key, Label, Description);
