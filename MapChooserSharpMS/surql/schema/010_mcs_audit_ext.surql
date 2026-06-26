DEFINE TABLE IF NOT EXISTS mcs_audit_ext SCHEMAFULL;

DEFINE FIELD IF NOT EXISTS triggered_at ON mcs_audit_ext TYPE datetime DEFAULT d'1970-01-01T00:00:00Z';
DEFINE FIELD IF NOT EXISTS threshold ON mcs_audit_ext TYPE int DEFAULT 0;
DEFINE FIELD IF NOT EXISTS map_state ON mcs_audit_ext TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS server_id ON mcs_audit_ext TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS created_at ON mcs_audit_ext TYPE datetime DEFAULT time::now();
