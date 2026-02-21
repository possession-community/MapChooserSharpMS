using System.Collections.Generic;

namespace MapChooserSharpMS.Shared.MapConfig;

/// <summary>
/// Type-safe accessor for extra configuration defined in TOML. <br/>
/// <br/>
/// Extra configuration is defined like: <br/>
/// [ze_example.extra.shop] <br/>
/// cost = 100 <br/>
/// <br/>
/// And accessed like: <br/>
/// int cost = accessor.GetValue&lt;int&gt;("shop", "cost", 0);
/// </summary>
public interface IExtraConfigAccessor
{
    /// <summary>
    /// Gets a value from the extra configuration. Returns defaultValue if the key does not exist or type conversion fails.
    /// </summary>
    T GetValue<T>(string section, string key, T defaultValue = default!);

    /// <summary>
    /// Tries to get a value from the extra configuration.
    /// </summary>
    bool TryGetValue<T>(string section, string key, out T value);

    /// <summary>
    /// Returns true if the specified section exists.
    /// </summary>
    bool HasSection(string section);

    /// <summary>
    /// Returns true if the specified key exists in the section.
    /// </summary>
    bool HasKey(string section, string key);

    /// <summary>
    /// Gets all keys in the specified section.
    /// </summary>
    IReadOnlyCollection<string> GetKeys(string section);

    /// <summary>
    /// Gets all section names.
    /// </summary>
    IReadOnlyCollection<string> GetSections();

    /// <summary>
    /// Gets an array value from the extra configuration.
    /// </summary>
    IReadOnlyList<T> GetArray<T>(string section, string key);
}
