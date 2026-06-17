DEFINE TABLE IF NOT EXISTS mcs_audit_extend_vote SCHEMAFULL;

DEFINE FIELD IF NOT EXISTS vote_started_at ON mcs_audit_extend_vote TYPE datetime DEFAULT d'1970-01-01T00:00:00Z';
DEFINE FIELD IF NOT EXISTS vote_ended_at ON mcs_audit_extend_vote TYPE datetime DEFAULT d'1970-01-01T00:00:00Z';
DEFINE FIELD IF NOT EXISTS vote_result ON mcs_audit_extend_vote TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS success_threshold ON mcs_audit_extend_vote TYPE float DEFAULT 0;
DEFINE FIELD IF NOT EXISTS yes_count ON mcs_audit_extend_vote TYPE int DEFAULT 0;
DEFINE FIELD IF NOT EXISTS no_count ON mcs_audit_extend_vote TYPE int DEFAULT 0;
DEFINE FIELD IF NOT EXISTS total_players ON mcs_audit_extend_vote TYPE int DEFAULT 0;
DEFINE FIELD IF NOT EXISTS passed ON mcs_audit_extend_vote TYPE bool DEFAULT false;
DEFINE FIELD IF NOT EXISTS initiator_steam_id ON mcs_audit_extend_vote TYPE option<int> DEFAULT NONE;
DEFINE FIELD IF NOT EXISTS server_id ON mcs_audit_extend_vote TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS created_at ON mcs_audit_extend_vote TYPE datetime DEFAULT time::now();
