-- ok: array<object{ id, server_key, name, cooldown, timed_cooldown_end, last_played_at, unplayed_count, nom_cooldown, nom_timed_cooldown_end }>
-- err: None
DEFINE FUNCTION OVERWRITE fn::mcs::get_map_cooldowns_scoped($mode: string, $pattern: string) {
    RETURN fn::core::ok(
        IF $mode = 'starts_with' {
            (SELECT * FROM mcs_map_cooldown WHERE string::starts_with(server_key, $pattern))
        } ELSE {
            (SELECT * FROM mcs_map_cooldown WHERE server_key = $pattern)
        }
    );
};
