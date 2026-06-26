DEFINE TABLE IF NOT EXISTS mcs_user_nom_cooldown SCHEMAFULL;

DEFINE FIELD IF NOT EXISTS steam_id ON mcs_user_nom_cooldown TYPE int DEFAULT 0;
DEFINE FIELD IF NOT EXISTS remaining_count ON mcs_user_nom_cooldown TYPE int DEFAULT 0;
DEFINE FIELD IF NOT EXISTS cooldown_until ON mcs_user_nom_cooldown TYPE datetime DEFAULT d'1970-01-01T00:00:00Z';
DEFINE FIELD IF NOT EXISTS server_id ON mcs_user_nom_cooldown TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS created_at ON mcs_user_nom_cooldown TYPE datetime DEFAULT time::now();
