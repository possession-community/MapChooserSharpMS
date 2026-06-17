DEFINE TABLE IF NOT EXISTS mcs_audit_vote SCHEMAFULL;

DEFINE FIELD IF NOT EXISTS vote_started_at ON mcs_audit_vote TYPE datetime DEFAULT d'1970-01-01T00:00:00Z';
DEFINE FIELD IF NOT EXISTS vote_ended_at ON mcs_audit_vote TYPE datetime DEFAULT d'1970-01-01T00:00:00Z';
DEFINE FIELD IF NOT EXISTS vote_result ON mcs_audit_vote TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS map_vote_start_reason ON mcs_audit_vote TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS vote_duration_config ON mcs_audit_vote TYPE float DEFAULT 0;
DEFINE FIELD IF NOT EXISTS total_players ON mcs_audit_vote TYPE int DEFAULT 0;
DEFINE FIELD IF NOT EXISTS total_votes ON mcs_audit_vote TYPE int DEFAULT 0;
DEFINE FIELD IF NOT EXISTS server_id ON mcs_audit_vote TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS created_at ON mcs_audit_vote TYPE datetime DEFAULT time::now();
