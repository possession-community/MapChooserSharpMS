-- Migrates pre-server-key cooldown records (single-key schema, id = map/group
-- name) into the (server_key, name) composite-key schema, claiming them for
-- the executing server. $server_id is injected by Wuling's schema manager.
--
-- Runs on every boot (schema manager applies schema -> function -> migration
-- each start), so this must stay idempotent:
--   * migrated legacy records are deleted, so the second run finds nothing
--   * new records always carry a non-empty server_key and are never touched
--   * INSERT IGNORE keeps an existing composite-key record over legacy data
--     (the composite record is always newer)
-- Legacy field last_nominated_at was dropped from the new schema and is
-- intentionally not carried over.
--
-- NOTE: written set-based on purpose — FOR loops with statements in their
-- body are unreliable on the embedded engine (SurrealDb.Embedded 0.10.2,
-- "Cannot execute statement using value"), so do not rewrite this with FOR.
IF $server_id != NONE AND $server_id != '' {
    INSERT IGNORE INTO mcs_map_cooldown (
        SELECT
            [$server_id, <string> record::id(id)] AS id,
            $server_id AS server_key,
            <string> record::id(id) AS name,
            cooldown,
            timed_cooldown_end,
            last_played_at,
            unplayed_count,
            nom_cooldown,
            nom_timed_cooldown_end
        FROM mcs_map_cooldown
        WHERE server_key = NONE OR server_key = ''
    );
    DELETE mcs_map_cooldown WHERE server_key = NONE OR server_key = '';
};

IF $server_id != NONE AND $server_id != '' {
    INSERT IGNORE INTO mcs_group_cooldown (
        SELECT
            [$server_id, <string> record::id(id)] AS id,
            $server_id AS server_key,
            <string> record::id(id) AS name,
            cooldown,
            timed_cooldown_end,
            last_played_at,
            unplayed_count,
            nom_cooldown,
            nom_timed_cooldown_end
        FROM mcs_group_cooldown
        WHERE server_key = NONE OR server_key = ''
    );
    DELETE mcs_group_cooldown WHERE server_key = NONE OR server_key = '';
};
