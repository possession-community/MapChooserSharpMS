using System;
using System.IO;
using MapChooserSharpMS.Modules.MapVote.State;
using Microsoft.Extensions.Configuration;
using Sharp.Shared;
using TnmsPluginFoundation;

namespace MapChooserSharpMS;

public sealed class MapChooserSharpMs(
    ISharedSystem sharedSystem,
    string dllPath,
    string sharpPath,
    Version? version,
    IConfiguration coreConfiguration,
    bool hotReload)
    : TnmsPlugin(sharedSystem, dllPath, sharpPath, version, coreConfiguration, hotReload)
{
    public override string DisplayName => "MapChooserSharp - ModSharp";
    public override string DisplayAuthor => "faketuna A.K.A fltuna or tuna, Spitice, uru";
    public override string BaseCfgDirectoryPath => ModuleDirectory;
    public override string ConVarConfigPath => "";
    public override string PluginPrefix => "Plugin.Prefix";
    public override bool UseTranslationKeyInPluginPrefix => true;

    /// <summary>
    /// Single vote-state holder for the whole plugin lifetime. Owned here at
    /// the plugin root so modules that need to mutate state (MapVote for the
    /// main vote, MapCycle for the extend vote) can receive the same concrete
    /// instance — typed down to the narrow writer interface at each call
    /// site — without going through DI for writable access.
    ///
    /// <para>
    /// <b>Wiring checklist</b> (when <see cref="TnmsOnPluginLoad"/> is filled in):
    /// <list type="bullet">
    ///   <item>Pass <see cref="VoteState"/> to <c>McsMapVoteController</c>'s
    ///   ctor — it auto-narrows to <c>IMcsInternalMainVoteState</c>.</item>
    ///   <item>Pass <see cref="VoteState"/> to <c>McsMapCycleController</c>'s
    ///   ctor — it auto-narrows to <c>IMcsInternalExtendVoteState</c>.</item>
    ///   <item><b>Don't forget consumers</b>: MapVote's RegisterServices
    ///   registers <c>IMcsReadOnlyVoteState</c> pointing at this instance so
    ///   consumer modules (Nomination, RTV, …) can query state through DI.
    ///   If MapVote ever stops being responsible for that registration, make
    ///   sure someone else registers the reader — otherwise consumer
    ///   <c>GetRequiredService&lt;IMcsReadOnlyVoteState&gt;()</c> calls will
    ///   throw.<br/>
    ///   <i>(Wishlist: ideally we'd also pass the reader via constructor to
    ///   consumer modules for the same type-safety benefit writers already
    ///   enjoy, instead of resolving through <c>ServiceProvider</c> — do
    ///   that when <see cref="TnmsOnPluginLoad"/> wires everything up.)</i>
    ///   </item>
    /// </list>
    /// </para>
    /// </summary>
    internal McsVoteStateManager VoteState { get; } = new();


    protected override void TnmsOnPluginLoad(bool hotReload)
    {
        // TODO() Should Initialize One By One
    }
}
