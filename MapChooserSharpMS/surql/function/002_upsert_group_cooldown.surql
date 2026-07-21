DEFINE FUNCTION OVERWRITE fn::mcs::upsert_group_cooldown(
    $server_key: string,
    $group_name: string,
    $cooldown: int,
    $timed_cooldown_end: datetime,
    $last_played_at: datetime,
    $unplayed_count: int,
    $nom_cooldown: int,
    $nom_timed_cooldown_end: datetime
) {
    UPSERT type::thing('mcs_group_cooldown', [$server_key, $group_name]) SET
        server_key = $server_key,
        name = $group_name,
        cooldown = $cooldown,
        timed_cooldown_end = $timed_cooldown_end,
        last_played_at = $last_played_at,
        unplayed_count = $unplayed_count,
        nom_cooldown = $nom_cooldown,
        nom_timed_cooldown_end = $nom_timed_cooldown_end;
};
