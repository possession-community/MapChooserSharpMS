using System.Security;
using MapChooserSharpMS.Modules.PluginConfig.Enums;

namespace MapChooserSharpMS.Modules.PluginConfig.Interfaces;

internal interface IMcsSqlConfig
{
    internal McsSupportedSqlType DataBaseType { get; }

    internal string Host { get; }

    internal string Port { get; }

    internal string DatabaseName { get; }

    internal string UserName { get; }

    internal SecureString Password { get; }
}
