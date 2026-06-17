using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MapChooserSharpMS.Modules.Audit.Services;

internal sealed class NullAuditPersistence : IAuditPersistence
{
    internal static readonly NullAuditPersistence Instance = new();

    public Task EnsureSchemasAsync(CancellationToken ct = default) => Task.CompletedTask;

    public void InsertMapPlayFireAndForget(AuditMapPlay record) { }

    public void InsertNominationsFireAndForget(IReadOnlyList<AuditNomination> records) { }

    public void InsertVoteFireAndForget(AuditVote vote, IReadOnlyList<AuditVoteCandidate> candidates) { }

    public void InsertExtendVoteFireAndForget(AuditExtendVote record) { }

    public void InsertRtvFireAndForget(AuditRtv rtv, IReadOnlyList<AuditRtvVote> votes) { }

    public void InsertExtFireAndForget(AuditExt ext, IReadOnlyList<AuditExtVote> votes) { }
}
