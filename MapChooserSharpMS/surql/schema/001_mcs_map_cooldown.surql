DEFINE TABLE IF NOT EXISTS mcs_map_cooldown SCHEMAFULL;

DEFINE FIELD IF NOT EXISTS name ON mcs_map_cooldown TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS cooldown ON mcs_map_cooldown TYPE int DEFAULT 0;
DEFINE FIELD IF NOT EXISTS timed_cooldown_end ON mcs_map_cooldown TYPE datetime DEFAULT d'1970-01-01T00:00:00Z';
DEFINE FIELD IF NOT EXISTS last_played_at ON mcs_map_cooldown TYPE datetime DEFAULT d'1970-01-01T00:00:00Z';
DEFINE FIELD IF NOT EXISTS unplayed_count ON mcs_map_cooldown TYPE int DEFAULT 0;
DEFINE FIELD IF NOT EXISTS nom_cooldown ON mcs_map_cooldown TYPE int DEFAULT 0;
DEFINE FIELD IF NOT EXISTS nom_timed_cooldown_end ON mcs_map_cooldown TYPE datetime DEFAULT d'1970-01-01T00:00:00Z';
DEFINE FIELD IF NOT EXISTS last_nominated_at ON mcs_map_cooldown TYPE datetime DEFAULT d'1970-01-01T00:00:00Z';
