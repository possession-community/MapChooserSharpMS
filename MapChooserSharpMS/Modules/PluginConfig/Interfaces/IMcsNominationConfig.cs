using System.Collections.Generic;
using MapChooserSharpMS.Modules.Ui.Menu;

namespace MapChooserSharpMS.Modules.PluginConfig.Interfaces;

internal interface IMcsNominationConfig
{
    internal List<McsSupportedMenuType> AvailableMenuTypes { get; }

    internal McsSupportedMenuType CurrentMenuType { get; }
}
