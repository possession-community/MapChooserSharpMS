DEFINE TABLE IF NOT EXISTS mcs_audit_cooldown_expired SCHEMAFULL;

DEFINE FIELD IF NOT EXISTS name ON mcs_audit_cooldown_expired TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS cooldown_type ON mcs_audit_cooldown_expired TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS became_available_at ON mcs_audit_cooldown_expired TYPE datetime DEFAULT d'1970-01-01T00:00:00Z';
DEFINE FIELD IF NOT EXISTS server_id ON mcs_audit_cooldown_expired TYPE string DEFAULT '';
