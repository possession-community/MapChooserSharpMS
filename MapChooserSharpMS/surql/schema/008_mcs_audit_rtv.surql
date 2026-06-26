DEFINE TABLE IF NOT EXISTS mcs_audit_rtv SCHEMAFULL;

DEFINE FIELD IF NOT EXISTS triggered_at ON mcs_audit_rtv TYPE datetime DEFAULT d'1970-01-01T00:00:00Z';
DEFINE FIELD IF NOT EXISTS threshold ON mcs_audit_rtv TYPE int DEFAULT 0;
DEFINE FIELD IF NOT EXISTS immediate_threshold ON mcs_audit_rtv TYPE option<int> DEFAULT NONE;
DEFINE FIELD IF NOT EXISTS is_forced ON mcs_audit_rtv TYPE bool DEFAULT false;
DEFINE FIELD IF NOT EXISTS map_state ON mcs_audit_rtv TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS server_id ON mcs_audit_rtv TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS created_at ON mcs_audit_rtv TYPE datetime DEFAULT time::now();
