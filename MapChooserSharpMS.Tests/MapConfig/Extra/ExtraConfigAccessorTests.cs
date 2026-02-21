using System;
using System.Collections.Generic;
using MapChooserSharpMS.Modules.MapConfig.Extra;
using Xunit;

namespace MapChooserSharpMS.Tests.MapConfig.Extra;

public class ExtraConfigAccessorTests
{
    [Fact]
    public void GetValue_ExistingKey_ReturnsValue()
    {
        var data = new Dictionary<string, Dictionary<string, object>>
        {
            ["shop"] = new() { ["cost"] = 100L }
        };
        var accessor = new ExtraConfigAccessor(data);

        Assert.Equal(100, accessor.GetValue<int>("shop", "cost", 0));
    }

    [Fact]
    public void GetValue_MissingKey_ReturnsDefault()
    {
        var accessor = ExtraConfigAccessor.Empty;

        Assert.Equal(42, accessor.GetValue("shop", "cost", 42));
    }

    [Fact]
    public void GetValue_StringType_ReturnsString()
    {
        var data = new Dictionary<string, Dictionary<string, object>>
        {
            ["shop"] = new() { ["name"] = "MyShop" }
        };
        var accessor = new ExtraConfigAccessor(data);

        Assert.Equal("MyShop", accessor.GetValue<string>("shop", "name", ""));
    }

    [Fact]
    public void GetValue_BoolType_ReturnsBool()
    {
        var data = new Dictionary<string, Dictionary<string, object>>
        {
            ["shop"] = new() { ["enabled"] = true }
        };
        var accessor = new ExtraConfigAccessor(data);

        Assert.True(accessor.GetValue("shop", "enabled", false));
    }

    [Fact]
    public void GetValue_DoubleType_ReturnsDouble()
    {
        var data = new Dictionary<string, Dictionary<string, object>>
        {
            ["shop"] = new() { ["rate"] = 1.5 }
        };
        var accessor = new ExtraConfigAccessor(data);

        Assert.Equal(1.5, accessor.GetValue("shop", "rate", 0.0));
    }

    [Fact]
    public void TryGetValue_ExistingKey_ReturnsTrueAndValue()
    {
        var data = new Dictionary<string, Dictionary<string, object>>
        {
            ["shop"] = new() { ["cost"] = 100L }
        };
        var accessor = new ExtraConfigAccessor(data);

        Assert.True(accessor.TryGetValue<long>("shop", "cost", out var value));
        Assert.Equal(100L, value);
    }

    [Fact]
    public void TryGetValue_MissingKey_ReturnsFalse()
    {
        var accessor = ExtraConfigAccessor.Empty;

        Assert.False(accessor.TryGetValue<int>("shop", "cost", out _));
    }

    [Fact]
    public void HasSection_ExistingSection_ReturnsTrue()
    {
        var data = new Dictionary<string, Dictionary<string, object>>
        {
            ["shop"] = new() { ["cost"] = 100L }
        };
        var accessor = new ExtraConfigAccessor(data);

        Assert.True(accessor.HasSection("shop"));
    }

    [Fact]
    public void HasSection_MissingSection_ReturnsFalse()
    {
        var accessor = ExtraConfigAccessor.Empty;

        Assert.False(accessor.HasSection("shop"));
    }

    [Fact]
    public void HasKey_ExistingKey_ReturnsTrue()
    {
        var data = new Dictionary<string, Dictionary<string, object>>
        {
            ["shop"] = new() { ["cost"] = 100L }
        };
        var accessor = new ExtraConfigAccessor(data);

        Assert.True(accessor.HasKey("shop", "cost"));
    }

    [Fact]
    public void HasKey_MissingKey_ReturnsFalse()
    {
        var data = new Dictionary<string, Dictionary<string, object>>
        {
            ["shop"] = new() { ["cost"] = 100L }
        };
        var accessor = new ExtraConfigAccessor(data);

        Assert.False(accessor.HasKey("shop", "missing"));
    }

    [Fact]
    public void GetKeys_ExistingSection_ReturnsKeys()
    {
        var data = new Dictionary<string, Dictionary<string, object>>
        {
            ["shop"] = new() { ["cost"] = 100L, ["name"] = "Shop" }
        };
        var accessor = new ExtraConfigAccessor(data);

        var keys = accessor.GetKeys("shop");
        Assert.Contains("cost", keys);
        Assert.Contains("name", keys);
        Assert.Equal(2, keys.Count);
    }

    [Fact]
    public void GetKeys_MissingSection_ReturnsEmpty()
    {
        var accessor = ExtraConfigAccessor.Empty;

        Assert.Empty(accessor.GetKeys("shop"));
    }

    [Fact]
    public void GetSections_ReturnsAllSectionNames()
    {
        var data = new Dictionary<string, Dictionary<string, object>>
        {
            ["shop"] = new() { ["cost"] = 100L },
            ["rewards"] = new() { ["bonus"] = 10L }
        };
        var accessor = new ExtraConfigAccessor(data);

        var sections = accessor.GetSections();
        Assert.Contains("shop", sections);
        Assert.Contains("rewards", sections);
        Assert.Equal(2, sections.Count);
    }

    [Fact]
    public void GetArray_ExistingArray_ReturnsValues()
    {
        var data = new Dictionary<string, Dictionary<string, object>>
        {
            ["rewards"] = new() { ["item_ids"] = new List<object> { 1L, 2L, 3L } }
        };
        var accessor = new ExtraConfigAccessor(data);

        var items = accessor.GetArray<long>("rewards", "item_ids");
        Assert.Equal(3, items.Count);
        Assert.Equal(1L, items[0]);
        Assert.Equal(2L, items[1]);
        Assert.Equal(3L, items[2]);
    }

    [Fact]
    public void GetArray_MissingKey_ReturnsEmpty()
    {
        var accessor = ExtraConfigAccessor.Empty;

        Assert.Empty(accessor.GetArray<int>("rewards", "item_ids"));
    }

    [Fact]
    public void Empty_HasNoSections()
    {
        var accessor = ExtraConfigAccessor.Empty;

        Assert.Empty(accessor.GetSections());
        Assert.False(accessor.HasSection("anything"));
    }

    [Fact]
    public void GetValue_TypeConversion_LongToInt_Works()
    {
        var data = new Dictionary<string, Dictionary<string, object>>
        {
            ["shop"] = new() { ["cost"] = 100L }
        };
        var accessor = new ExtraConfigAccessor(data);

        // long → int via Convert.ChangeType
        Assert.Equal(100, accessor.GetValue<int>("shop", "cost", 0));
    }
}
