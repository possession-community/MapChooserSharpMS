using MapChooserSharpMS.Shared.Nomination.Services;

namespace MapChooserSharpMS.Modules.Nomination.Interfaces;

internal interface IMcsInternalMapNominationService: IMapNominationService
{
    int NominationCountLimit { get; }
}