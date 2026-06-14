using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Modules.Nomination.Interfaces;
using MapChooserSharpMS.Shared.Nomination;

namespace MapChooserSharpMS.Modules.Nomination.Managers;

internal sealed class InternalNominationManager: IMcsInternalNominationManager
{
    private readonly ConcurrentDictionary<string, IMcsNominationData> _internalNominations = new();

    public IReadOnlyDictionary<string, IMcsNominationData> NominatedMaps => _internalNominations;

    public bool ClearNominations()
    {
        if (_internalNominations.IsEmpty)
            return false;

        _internalNominations.Clear();
        return true;
    }

    public bool AddNomination(IMcsNominationData nominationData)
    {
        return _internalNominations.TryAdd(nominationData.MapConfig.MapName, nominationData);
    }

    public bool RemoveNomination(IMcsNominationData nominationData)
    {
        return _internalNominations.TryRemove(nominationData.MapConfig.MapName, out _);
    }
}