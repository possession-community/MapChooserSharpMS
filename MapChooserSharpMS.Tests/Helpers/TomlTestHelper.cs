using System;
using System.IO;
using System.Text;
using CsToml;

namespace MapChooserSharpMS.Tests.Helpers;

internal static class TomlTestHelper
{
    public static TomlDocument ParseToml(string tomlString)
    {
        var bytes = Encoding.UTF8.GetBytes(tomlString);
        return CsTomlSerializer.Deserialize<TomlDocument>(bytes);
    }

    public static TomlDocument LoadToml(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", fileName);
        var bytes = File.ReadAllBytes(path);
        return CsTomlSerializer.Deserialize<TomlDocument>(bytes);
    }
}
