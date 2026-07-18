using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapChooserSharpMS.Modules.MapCycle.Services;
using MapChooserSharpMS.Modules.PluginConfig.Enums;
using Xunit;

namespace MapChooserSharpMS.Tests.MapCycle;

public class MapConfigExecutionServiceTests
{
    private static string[] Files(params string[] names)
        => names.Select(n => Path.Combine("cfg", "mcsms", "maps", n + ".cfg")).ToArray();

    private static List<string> Names(List<string> paths)
        => paths.Select(Path.GetFileNameWithoutExtension).ToList()!;

    [Fact]
    public void ExactMatch_MatchesOnlyExactFileName()
    {
        var files = Files("de_dust2", "de_", "dust");

        var result = MapConfigExecutionService.MatchCfgFiles(files, "de_dust2", McsMapConfigExecutionType.ExactMatch);

        Assert.Equal(["de_dust2"], Names(result));
    }

    [Fact]
    public void StartWithMatch_ExecutesShortestFirst_ExactLast()
    {
        var files = Files("de_dust2", "de_", "de_dust", "cs_");

        var result = MapConfigExecutionService.MatchCfgFiles(files, "de_dust2", McsMapConfigExecutionType.StartWithMatch);

        Assert.Equal(["de_", "de_dust", "de_dust2"], Names(result));
    }

    [Fact]
    public void PartialMatch_ExecutesExactMatchLast()
    {
        var files = Files("de_dust2", "dust", "de_");

        var result = MapConfigExecutionService.MatchCfgFiles(files, "de_dust2", McsMapConfigExecutionType.PartialMatch);

        Assert.Equal(["de_", "dust", "de_dust2"], Names(result));
        Assert.Equal("de_dust2", Names(result).Last());
    }

    [Fact]
    public void PartialMatch_ExactMatchLast_RegardlessOfInputOrder()
    {
        var files = Files("de_dust2", "e_dust", "st2", "de_dust");

        var result = MapConfigExecutionService.MatchCfgFiles(files, "de_dust2", McsMapConfigExecutionType.PartialMatch);

        Assert.Equal("de_dust2", Names(result).Last());
        Assert.Equal(["st2", "e_dust", "de_dust", "de_dust2"], Names(result));
    }

    [Fact]
    public void Match_IgnoresDirectoryNames_MatchesByFileNameOnly()
    {
        string[] files =
        [
            Path.Combine("cfg", "mcsms", "maps", "ze", "de_dust2.cfg"),
            Path.Combine("cfg", "mcsms", "maps", "de_dust2_sub", "de_.cfg"),
            Path.Combine("cfg", "mcsms", "maps", "de_dust2", "unrelated.cfg"),
        ];

        var result = MapConfigExecutionService.MatchCfgFiles(files, "de_dust2", McsMapConfigExecutionType.StartWithMatch);

        Assert.Equal(["de_", "de_dust2"], Names(result));
    }

    [Fact]
    public void MatchIsCaseInsensitive()
    {
        var files = Files("DE_DUST2");

        var result = MapConfigExecutionService.MatchCfgFiles(files, "de_dust2", McsMapConfigExecutionType.PartialMatch);

        Assert.Equal(["DE_DUST2"], Names(result));
    }
}
