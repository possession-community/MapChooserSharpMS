using System.Collections.Generic;
using MapChooserSharpMS.Shared.Events;
using Xunit;

namespace MapChooserSharpMS.Tests.Events;

public class McsCancellableEventTests
{
    [Fact]
    public void DefaultValue_IsContinue()
    {
        McsCancellableEvent e = default;
        Assert.Equal(McsCancellableEvent.Continue, e);
    }

    [Fact]
    public void EnumValues_AreDistinct()
    {
        Assert.NotEqual(McsCancellableEvent.Continue, McsCancellableEvent.Handled);
        Assert.NotEqual(McsCancellableEvent.Continue, McsCancellableEvent.Stop);
        Assert.NotEqual(McsCancellableEvent.Handled, McsCancellableEvent.Stop);
    }

    [Fact]
    public void Continue_IsZero()
    {
        Assert.Equal(0, (int)McsCancellableEvent.Continue);
    }
}

public class McsValueOverrideEventTests
{
    [Fact]
    public void NoOverride_HasValueIsFalse()
    {
        var e = McsValueOverrideEvent<string>.NoOverride;
        Assert.False(e.HasValue);
    }

    [Fact]
    public void Default_HasValueIsFalse()
    {
        McsValueOverrideEvent<int> e = default;
        Assert.False(e.HasValue);
    }

    [Fact]
    public void WithValue_HasValueIsTrue()
    {
        var e = new McsValueOverrideEvent<string>("hello");
        Assert.True(e.HasValue);
        Assert.Equal("hello", e.Value);
    }

    [Fact]
    public void WithNullValue_HasValueIsFalse()
    {
        var e = new McsValueOverrideEvent<string>(null!);
        Assert.False(e.HasValue);
    }

    [Fact]
    public void WithValueType_HasValueIsTrue()
    {
        var e = new McsValueOverrideEvent<int>(42);
        Assert.True(e.HasValue);
        Assert.Equal(42, e.Value);
    }

    [Fact]
    public void WithList_HasValueIsTrue()
    {
        var list = new List<string> { "a", "b" };
        var e = new McsValueOverrideEvent<List<string>>(list);
        Assert.True(e.HasValue);
        Assert.Same(list, e.Value);
    }

    [Fact]
    public void WithEmptyList_HasValueIsTrue()
    {
        var list = new List<string>();
        var e = new McsValueOverrideEvent<List<string>>(list);
        Assert.True(e.HasValue);
        Assert.Empty(e.Value);
    }

    [Fact]
    public void WithValueTypeZero_HasValueIsTrue()
    {
        var e = new McsValueOverrideEvent<int>(0);
        Assert.True(e.HasValue);
        Assert.Equal(0, e.Value);
    }

    [Fact]
    public void WithValueTypeDefault_HasValueIsTrue()
    {
        var e = new McsValueOverrideEvent<double>(0.0);
        Assert.True(e.HasValue);
        Assert.Equal(0.0, e.Value);
    }
}
