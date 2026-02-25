using MapChooserSharpMS.Modules.MapConfig.Extra;
using MapChooserSharpMS.Tests.Helpers;
using Xunit;

namespace MapChooserSharpMS.Tests.MapConfig.Extra;

public class ExtraConfigBuilderTests
{
    [Fact]
    public void Merge_FromTomlNode_BuildsAccessor()
    {
        var doc = TomlTestHelper.LoadToml("ExtraConfigBuilder/01_basic_extra.toml");
        var extraNode = doc.RootNode["extra"u8];

        var accessor = new ExtraConfigBuilder()
            .Merge(extraNode)
            .Build();

        Assert.True(accessor.HasSection("shop"));
        Assert.Equal(100, accessor.GetValue<int>("shop", "cost", 0));
        Assert.Equal("MyShop", accessor.GetValue<string>("shop", "name", ""));
    }

    [Fact]
    public void Merge_MultipleTomlNodes_LastWins()
    {
        var doc1 = TomlTestHelper.LoadToml("ExtraConfigBuilder/02a_multi_first.toml");
        var doc2 = TomlTestHelper.LoadToml("ExtraConfigBuilder/02b_multi_second.toml");

        var accessor = new ExtraConfigBuilder()
            .Merge(doc1.RootNode["extra"u8])
            .Merge(doc2.RootNode["extra"u8])
            .Build();

        // cost overridden by second merge
        Assert.Equal(200, accessor.GetValue<int>("shop", "cost", 0));
        // discount preserved from first merge
        Assert.Equal(10, accessor.GetValue<int>("shop", "discount", 0));
    }

    [Fact]
    public void Merge_DifferentSections_BothPreserved()
    {
        var doc = TomlTestHelper.LoadToml("ExtraConfigBuilder/03_different_sections.toml");

        var accessor = new ExtraConfigBuilder()
            .Merge(doc.RootNode["extra"u8])
            .Build();

        Assert.True(accessor.HasSection("shop"));
        Assert.True(accessor.HasSection("rewards"));
        Assert.Equal(100, accessor.GetValue<int>("shop", "cost", 0));
        Assert.Equal(50, accessor.GetValue<int>("rewards", "bonus", 0));
    }

    [Fact]
    public void Merge_FromAccessor_LastWins()
    {
        // Build first accessor
        var doc1 = TomlTestHelper.LoadToml("ExtraConfigBuilder/04a_accessor_first.toml");
        var accessor1 = new ExtraConfigBuilder()
            .Merge(doc1.RootNode["extra"u8])
            .Build();

        // Build second accessor
        var doc2 = TomlTestHelper.LoadToml("ExtraConfigBuilder/04b_accessor_second.toml");
        var accessor2 = new ExtraConfigBuilder()
            .Merge(doc2.RootNode["extra"u8])
            .Build();

        // Merge both — accessor2 wins for overlapping keys
        var merged = new ExtraConfigBuilder()
            .Merge(accessor1)
            .Merge(accessor2)
            .Build();

        Assert.Equal(200, merged.GetValue<int>("shop", "cost", 0));
        Assert.Equal(10, merged.GetValue<int>("shop", "discount", 0));
    }

    [Fact]
    public void Merge_DefaultNode_NoEffect()
    {
        // default TomlDocumentNode has HasValue = false
        var accessor = new ExtraConfigBuilder()
            .Merge(default(CsToml.TomlDocumentNode))
            .Build();

        Assert.Empty(accessor.GetSections());
    }

    [Fact]
    public void Merge_NullAccessor_NoEffect()
    {
        var doc = TomlTestHelper.LoadToml("ExtraConfigBuilder/06_null_accessor.toml");

        var accessor = new ExtraConfigBuilder()
            .Merge(doc.RootNode["extra"u8])
            .Merge((MapChooserSharpMS.Shared.MapConfig.IExtraConfigAccessor?)null)
            .Build();

        Assert.Equal(100, accessor.GetValue<int>("shop", "cost", 0));
    }

    [Fact]
    public void Merge_SameSectionSameKey_LastOverwritesPrevious()
    {
        var doc1 = TomlTestHelper.LoadToml("ExtraConfigBuilder/07a_same_key_first.toml");
        var doc2 = TomlTestHelper.LoadToml("ExtraConfigBuilder/07b_same_key_second.toml");

        var accessor = new ExtraConfigBuilder()
            .Merge(doc1.RootNode["extra"u8])
            .Merge(doc2.RootNode["extra"u8])
            .Build();

        Assert.Equal(50, accessor.GetValue<int>("shop", "cost", 0));
    }

    [Fact]
    public void Build_EmptyBuilder_ReturnsEmptyAccessor()
    {
        var accessor = new ExtraConfigBuilder().Build();

        Assert.Empty(accessor.GetSections());
    }
}
