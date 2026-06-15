DEFINE FUNCTION OVERWRITE fn::mcs::upsert_group_cooldown(
    $group_name: string,
    $cooldown: int,
    $timed_cooldown_end: datetime,
    $last_played_at: datetime,
    $nom_cooldown: int,
    $nom_timed_cooldown_end: datetime,
    $last_nominated_at: datetime
) {
    UPSERT type::record('mcs_group_cooldown', $group_name) SET
        cooldown = $cooldown,
        timed_cooldown_end = $timed_cooldown_end,
        last_played_at = $last_played_at,
        nom_cooldown = $nom_cooldown,
        nom_timed_cooldown_end = $nom_timed_cooldown_end,
        last_nominated_at = $last_nominated_at;
};
