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
}
