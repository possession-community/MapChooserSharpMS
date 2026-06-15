using System.Collections.Generic;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Modules.Ui.Menu;

namespace MapChooserSharpMS.Modules.PluginConfig.Models;

internal class NominationConfig(
    List<McsSupportedMenuType> availableMenuTypes,
    McsSupportedMenuType currentMenuType)
    : IMcsNominationConfig
{
    public List<McsSupportedMenuType> AvailableMenuTypes { get; } = availableMenuTypes;
    public McsSupportedMenuType CurrentMenuType { get; } = currentMenuType;
}
