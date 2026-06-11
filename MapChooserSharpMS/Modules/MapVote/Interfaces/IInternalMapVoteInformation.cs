using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapVote;
using Sharp.Shared.Units;

namespace MapChooserSharpMS.Modules.MapVote.Interfaces;

internal interface IInternalMapVoteInformation : IMapVoteInformation
{
    new McsMapVoteState CurrentState { get; set; }

    void SetVoteOptions(IReadOnlyList<IMapVoteOption> options);

    void SetWinner(IMapVoteOption? winner);

    bool AddVote(PlayerSlot slot, IMapVoteOption option);

    bool RemoveVote(PlayerSlot slot);
}