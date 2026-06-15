-- ok: array<object{ id, cooldown, timed_cooldown_end, last_played_at, nom_cooldown, nom_timed_cooldown_end, last_nominated_at }>
-- err: None
DEFINE FUNCTION OVERWRITE fn::mcs::get_all_group_cooldowns() {
    RETURN fn::core::ok(
        SELECT * FROM mcs_group_cooldown
    );
};
