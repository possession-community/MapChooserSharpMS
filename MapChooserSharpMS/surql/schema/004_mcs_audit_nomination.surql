DEFINE TABLE IF NOT EXISTS mcs_audit_nomination SCHEMAFULL;

DEFINE FIELD IF NOT EXISTS nominated_at ON mcs_audit_nomination TYPE datetime DEFAULT d'1970-01-01T00:00:00Z';
DEFINE FIELD IF NOT EXISTS map_name ON mcs_audit_nomination TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS workshop_id ON mcs_audit_nomination TYPE option<int> DEFAULT NONE;
DEFINE FIELD IF NOT EXISTS nominator_steam_id ON mcs_audit_nomination TYPE option<int> DEFAULT NONE;
DEFINE FIELD IF NOT EXISTS nomination_type ON mcs_audit_nomination TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS nomination_result ON mcs_audit_nomination TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS group_name ON mcs_audit_nomination TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS server_id ON mcs_audit_nomination TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS created_at ON mcs_audit_nomination TYPE datetime DEFAULT time::now();
