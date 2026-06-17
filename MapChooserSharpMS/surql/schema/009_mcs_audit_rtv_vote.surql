DEFINE TABLE IF NOT EXISTS mcs_audit_rtv_vote SCHEMAFULL;

DEFINE FIELD IF NOT EXISTS rtv_id ON mcs_audit_rtv_vote TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS steam_id ON mcs_audit_rtv_vote TYPE int DEFAULT 0;
DEFINE FIELD IF NOT EXISTS voted_at ON mcs_audit_rtv_vote TYPE datetime DEFAULT d'1970-01-01T00:00:00Z';
DEFINE FIELD IF NOT EXISTS server_id ON mcs_audit_rtv_vote TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS created_at ON mcs_audit_rtv_vote TYPE datetime DEFAULT time::now();
