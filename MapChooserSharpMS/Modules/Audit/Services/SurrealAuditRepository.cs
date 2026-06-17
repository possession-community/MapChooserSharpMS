using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wuling.Abstract.Tianshi.Surreal;

namespace MapChooserSharpMS.Modules.Audit.Services;

internal sealed class SurrealAuditRepository : IAuditPersistence
{
    private readonly IWulingSurreal _surreal;
    private readonly ILogger _logger;
    private readonly string _surqlDirectory;

    internal SurrealAuditRepository(IWulingSurreal surreal, ILogger logger, string moduleDirectory)
    {
        _surreal = surreal;
        _logger = logger;
        _surqlDirectory = Path.Combine(moduleDirectory, "surql");
    }

    public async Task EnsureSchemasAsync(CancellationToken ct = default)
    {
        await _surreal.EnsureSchemasAsync(_surqlDirectory, ct);
        _logger.LogInformation("[Audit] SurrealDB schemas ensured from {Path}", _surqlDirectory);
    }

    public void InsertMapPlayFireAndForget(AuditMapPlay r)
    {
        FireAndForget(async () =>
        {
            var surql = "CREATE mcs_audit_map_play SET map_name = $map_name, workshop_id = $workshop_id, group_names = $group_names, peak_player_count = $peak_player_count, end_player_count = $end_player_count, map_started_at = $map_started_at, map_ended_at = $map_ended_at, map_end_reason = $map_end_reason, round_count = $round_count, timelimit_type = $timelimit_type, configured_timelimit = $configured_timelimit, server_id = $server_id, extend_count = $extend_count, max_normal_extends = $max_normal_extends, normal_extends_used = $normal_extends_used, admin_vote_extend_count = $admin_vote_extend_count, user_ext_extends_used = $user_ext_extends_used, max_user_ext_extends = $max_user_ext_extends;";

            var vars = new Dictionary<string, object?>
            {
                ["map_name"] = r.MapName,
                ["workshop_id"] = r.WorkshopId.HasValue ? (object)r.WorkshopId.Value : null,
                ["group_names"] = r.GroupNames,
                ["peak_player_count"] = r.PeakPlayerCount,
                ["end_player_count"] = r.EndPlayerCount,
                ["map_started_at"] = r.MapStartedAt,
                ["map_ended_at"] = r.MapEndedAt,
                ["map_end_reason"] = r.MapEndReason,
                ["round_count"] = r.RoundCount,
                ["timelimit_type"] = r.TimelimitType,
                ["configured_timelimit"] = r.ConfiguredTimelimit,
                ["server_id"] = r.ServerId,
                ["extend_count"] = r.ExtendCount,
                ["max_normal_extends"] = r.MaxNormalExtends,
                ["normal_extends_used"] = r.NormalExtendsUsed,
                ["admin_vote_extend_count"] = r.AdminVoteExtendCount,
                ["user_ext_extends_used"] = r.UserExtExtendsUsed,
                ["max_user_ext_extends"] = r.MaxUserExtExtends,
            };

            await _surreal.WriteAsync(surql, vars);
        }, "map_play");
    }

    public void InsertNominationsFireAndForget(IReadOnlyList<AuditNomination> records)
    {
        if (records.Count == 0) return;

        FireAndForget(async () =>
        {
            foreach (var r in records)
            {
                var surql = "CREATE mcs_audit_nomination SET nominated_at = $nominated_at, map_name = $map_name, workshop_id = $workshop_id, nominator_steam_id = $nominator_steam_id, nomination_type = $nomination_type, nomination_result = $nomination_result, group_name = $group_name, server_id = $server_id;";
                var vars = new Dictionary<string, object?>
                {
                    ["nominated_at"] = r.NominatedAt,
                    ["map_name"] = r.MapName,
                    ["workshop_id"] = r.WorkshopId.HasValue ? (object)r.WorkshopId.Value : null,
                    ["nominator_steam_id"] = r.NominatorSteamId.HasValue ? (object)(long)r.NominatorSteamId.Value : null,
                    ["nomination_type"] = r.NominationType,
                    ["nomination_result"] = r.NominationResult,
                    ["group_name"] = r.GroupName,
                    ["server_id"] = r.ServerId,
                };
                await _surreal.WriteAsync(surql, vars);
            }
        }, "nominations");
    }

    public void InsertVoteFireAndForget(AuditVote vote, IReadOnlyList<AuditVoteCandidate> candidates)
    {
        FireAndForget(async () =>
        {
            var vSurql = "CREATE mcs_audit_vote SET vote_started_at = $vote_started_at, vote_ended_at = $vote_ended_at, vote_result = $vote_result, map_vote_start_reason = $map_vote_start_reason, vote_duration_config = $vote_duration_config, total_players = $total_players, total_votes = $total_votes, server_id = $server_id;";
            var vVars = new Dictionary<string, object?>
            {
                ["vote_started_at"] = vote.VoteStartedAt,
                ["vote_ended_at"] = vote.VoteEndedAt,
                ["vote_result"] = vote.VoteResult,
                ["map_vote_start_reason"] = vote.MapVoteStartReason,
                ["vote_duration_config"] = vote.VoteDurationConfig,
                ["total_players"] = vote.TotalPlayers,
                ["total_votes"] = vote.TotalVotes,
                ["server_id"] = vote.ServerId,
            };
            await _surreal.WriteAsync(vSurql, vVars);

            foreach (var c in candidates)
            {
                var cSurql = "CREATE mcs_audit_vote_candidate SET vote_id = $vote_id, map_name = $map_name, workshop_id = $workshop_id, vote_count = $vote_count, is_winner = $is_winner, is_nominated = $is_nominated, candidate_type = $candidate_type;";
                var cVars = new Dictionary<string, object?>
                {
                    ["vote_id"] = c.VoteId,
                    ["map_name"] = c.MapName,
                    ["workshop_id"] = c.WorkshopId.HasValue ? (object)c.WorkshopId.Value : null,
                    ["vote_count"] = c.VoteCount,
                    ["is_winner"] = c.IsWinner,
                    ["is_nominated"] = c.IsNominated,
                    ["candidate_type"] = c.CandidateType,
                };
                await _surreal.WriteAsync(cSurql, cVars);
            }
        }, "vote");
    }

    public void InsertExtendVoteFireAndForget(AuditExtendVote r)
    {
        FireAndForget(async () =>
        {
            var surql = "CREATE mcs_audit_extend_vote SET vote_started_at = $vote_started_at, vote_ended_at = $vote_ended_at, vote_result = $vote_result, success_threshold = $success_threshold, yes_count = $yes_count, no_count = $no_count, total_players = $total_players, passed = $passed, initiator_steam_id = $initiator_steam_id, server_id = $server_id;";
            var vars = new Dictionary<string, object?>
            {
                ["vote_started_at"] = r.VoteStartedAt,
                ["vote_ended_at"] = r.VoteEndedAt,
                ["vote_result"] = r.VoteResult,
                ["success_threshold"] = r.SuccessThreshold,
                ["yes_count"] = r.YesCount,
                ["no_count"] = r.NoCount,
                ["total_players"] = r.TotalPlayers,
                ["passed"] = r.Passed,
                ["initiator_steam_id"] = r.InitiatorSteamId.HasValue ? (object)(long)r.InitiatorSteamId.Value : null,
                ["server_id"] = r.ServerId,
            };
            await _surreal.WriteAsync(surql, vars);
        }, "extend_vote");
    }

    public void InsertRtvFireAndForget(AuditRtv rtv, IReadOnlyList<AuditRtvVote> votes)
    {
        FireAndForget(async () =>
        {
            var rSurql = $"CREATE type::record('mcs_audit_rtv', $id) SET triggered_at = $triggered_at, threshold = $threshold, immediate_threshold = $immediate_threshold, is_forced = $is_forced, map_state = $map_state, server_id = $server_id;";
            var rVars = new Dictionary<string, object?>
            {
                ["id"] = rtv.Id,
                ["triggered_at"] = rtv.TriggeredAt,
                ["threshold"] = rtv.Threshold,
                ["immediate_threshold"] = rtv.ImmediateThreshold.HasValue ? (object)rtv.ImmediateThreshold.Value : null,
                ["is_forced"] = rtv.IsForced,
                ["map_state"] = rtv.MapState,
                ["server_id"] = rtv.ServerId,
            };
            await _surreal.WriteAsync(rSurql, rVars);

            foreach (var v in votes)
            {
                var vSurql = "CREATE mcs_audit_rtv_vote SET rtv_id = $rtv_id, steam_id = $steam_id, voted_at = $voted_at, server_id = $server_id;";
                var vVars = new Dictionary<string, object?>
                {
                    ["rtv_id"] = v.RtvId,
                    ["steam_id"] = (long)v.SteamId,
                    ["voted_at"] = v.VotedAt,
                    ["server_id"] = rtv.ServerId,
                };
                await _surreal.WriteAsync(vSurql, vVars);
            }
        }, "rtv");
    }

    public void InsertExtFireAndForget(AuditExt ext, IReadOnlyList<AuditExtVote> votes)
    {
        FireAndForget(async () =>
        {
            var eSurql = $"CREATE type::record('mcs_audit_ext', $id) SET triggered_at = $triggered_at, threshold = $threshold, map_state = $map_state, server_id = $server_id;";
            var eVars = new Dictionary<string, object?>
            {
                ["id"] = ext.Id,
                ["triggered_at"] = ext.TriggeredAt,
                ["threshold"] = ext.Threshold,
                ["map_state"] = ext.MapState,
                ["server_id"] = ext.ServerId,
            };
            await _surreal.WriteAsync(eSurql, eVars);

            foreach (var v in votes)
            {
                var vSurql = "CREATE mcs_audit_ext_vote SET ext_id = $ext_id, steam_id = $steam_id, voted_at = $voted_at, server_id = $server_id;";
                var vVars = new Dictionary<string, object?>
                {
                    ["ext_id"] = v.ExtId,
                    ["steam_id"] = (long)v.SteamId,
                    ["voted_at"] = v.VotedAt,
                    ["server_id"] = ext.ServerId,
                };
                await _surreal.WriteAsync(vSurql, vVars);
            }
        }, "ext");
    }

    private void FireAndForget(Func<Task> action, string label)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Audit] Fire-and-forget insert failed for {Label}", label);
            }
        });
    }
}
