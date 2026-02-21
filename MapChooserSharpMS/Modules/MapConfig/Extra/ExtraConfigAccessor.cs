using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Extra;

internal sealed class ExtraConfigAccessor : IExtraConfigAccessor
{
    private readonly Dictionary<string, Dictionary<string, object>> _data;

    public static ExtraConfigAccessor Empty { get; } = new(new());

    internal ExtraConfigAccessor(Dictionary<string, Dictionary<string, object>> data)
    {
        _data = data;
    }

    public T GetValue<T>(string section, string key, T defaultValue = default!)
    {
        if (TryGetValue<T>(section, key, out var value))
            return value;

        return defaultValue;
    }

    public bool TryGetValue<T>(string section, string key, out T value)
    {
        value = default!;

        if (!TryGetRaw(section, key, out var rawValue))
            return false;

        return TryConvert(rawValue, out value);
    }

    internal bool TryGetRaw(string section, string key, out object value)
    {
        value = default!;

        if (!_data.TryGetValue(section, out var sectionData))
            return false;

        return sectionData.TryGetValue(key, out value!);
    }

    public bool HasSection(string section) => _data.ContainsKey(section);

    public bool HasKey(string section, string key)
    {
        return _data.TryGetValue(section, out var sectionData)
            && sectionData.ContainsKey(key);
    }

    public IReadOnlyCollection<string> GetKeys(string section)
    {
        return _data.TryGetValue(section, out var sectionData)
            ? sectionData.Keys.ToList()
            : Array.Empty<string>();
    }

    public IReadOnlyCollection<string> GetSections() => _data.Keys.ToList();

    public IReadOnlyList<T> GetArray<T>(string section, string key)
    {
        if (!TryGetRaw(section, key, out var rawValue))
            return Array.Empty<T>();

        if (rawValue is not List<object> list)
            return Array.Empty<T>();

        try
        {
            return list
                .Select(item => (T)Convert.ChangeType(item, typeof(T))!)
                .ToList();
        }
        catch
        {
            return Array.Empty<T>();
        }
    }

    private static bool TryConvert<T>(object rawValue, out T value)
    {
        value = default!;

        try
        {
            if (rawValue is T directMatch)
            {
                value = directMatch;
                return true;
            }

            value = (T)Convert.ChangeType(rawValue, typeof(T));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
