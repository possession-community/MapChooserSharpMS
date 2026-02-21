using System.Collections.Generic;
using System.Collections.ObjectModel;
using CsToml;
using CsToml.Values;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Modules.MapConfig.Extra;

internal sealed class ExtraConfigBuilder
{
    private readonly Dictionary<string, Dictionary<string, object>> _data = new();

    /// <summary>
    /// Merges extra configuration from a CsToml node representing the "extra" table.
    /// The node should contain section subtables, each with key-value pairs.
    /// Later merges override earlier ones (last-write-wins).
    /// </summary>
    public ExtraConfigBuilder Merge(TomlDocumentNode extraNode)
    {
        try
        {
            if (!extraNode.HasValue)
                return this;
        }
        catch
        {
            // default(TomlDocumentNode) has null internals — HasValue throws
            return this;
        }

        foreach (var section in extraNode.GetNodeEnumerator())
        {
            var sectionName = section.Key.GetString();

            if (!_data.TryGetValue(sectionName, out var existingSection))
            {
                existingSection = new Dictionary<string, object>();
                _data[sectionName] = existingSection;
            }

            foreach (var kv in section.Value.GetNodeEnumerator())
            {
                var key = kv.Key.GetString();
                var value = ConvertToClrValue(kv.Value);
                if (value is not null)
                {
                    existingSection[key] = value;
                }
            }
        }

        return this;
    }

    /// <summary>
    /// Merges from another ExtraConfigAccessor (last-write-wins).
    /// </summary>
    public ExtraConfigBuilder Merge(IExtraConfigAccessor? other)
    {
        if (other is not ExtraConfigAccessor accessor)
            return this;

        foreach (var section in accessor.GetSections())
        {
            if (!_data.TryGetValue(section, out var existingSection))
            {
                existingSection = new Dictionary<string, object>();
                _data[section] = existingSection;
            }

            foreach (var key in accessor.GetKeys(section))
            {
                if (accessor.TryGetRaw(section, key, out var value))
                {
                    existingSection[key] = value;
                }
            }
        }

        return this;
    }

    public IExtraConfigAccessor Build()
    {
        return new ExtraConfigAccessor(_data);
    }

    internal static object? ConvertToClrValue(TomlDocumentNode node)
    {
        if (node.TryGetString(out var str))
            return str;

        if (node.TryGetInt64(out var longVal))
            return longVal;

        if (node.TryGetDouble(out var doubleVal))
            return doubleVal;

        if (node.TryGetBool(out var boolVal))
            return boolVal;

        // Array
        if (node.TryGetArray(out var array))
        {
            var list = new List<object>(array.Count);
            foreach (var item in array)
            {
                if (item.TryGetString(out var s))
                    list.Add(s);
                else if (item.TryGetInt64(out var l))
                    list.Add(l);
                else if (item.TryGetDouble(out var d))
                    list.Add(d);
                else if (item.TryGetBool(out var b))
                    list.Add(b);
            }
            return list;
        }

        return null;
    }
}
