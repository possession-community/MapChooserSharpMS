using System.Globalization;
using MapChooserSharpMS.Shared.Events.MapCycle.Params;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.MapCycle;

internal sealed class ExtCommandExecuteParams : IExtCommandExecuteEventParams
{
    private readonly TnmsPlugin _plugin;
    private readonly PluginModuleBase _moduleBase;
    private StringCommand _command;

    internal ExtCommandExecuteParams(
        TnmsPlugin plugin,
        PluginModuleBase moduleBase,
        IGameClient? client,
        StringCommand command,
        int currentRequiredVotes,
        int currentExtVotes)
    {
        _plugin = plugin;
        _moduleBase = moduleBase;
        Client = client;
        _command = command;
        CurrentRequiredVotes = currentRequiredVotes;
        CurrentExtVotes = currentExtVotes;
    }

    public string ModulePrefix(CultureInfo? culture = null)
        => _plugin.Localizer.ForCulture(_moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture);

    public IGameClient? Client { get; }

    public ref StringCommand Command => ref _command;

    public int CurrentRequiredVotes { get; }

    public int CurrentExtVotes { get; }
}
