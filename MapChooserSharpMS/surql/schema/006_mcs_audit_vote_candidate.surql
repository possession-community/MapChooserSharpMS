DEFINE TABLE IF NOT EXISTS mcs_audit_vote_candidate SCHEMAFULL;

DEFINE FIELD IF NOT EXISTS vote_id ON mcs_audit_vote_candidate TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS map_name ON mcs_audit_vote_candidate TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS workshop_id ON mcs_audit_vote_candidate TYPE option<int> DEFAULT NONE;
DEFINE FIELD IF NOT EXISTS vote_count ON mcs_audit_vote_candidate TYPE int DEFAULT 0;
DEFINE FIELD IF NOT EXISTS is_winner ON mcs_audit_vote_candidate TYPE bool DEFAULT false;
DEFINE FIELD IF NOT EXISTS is_nominated ON mcs_audit_vote_candidate TYPE bool DEFAULT false;
DEFINE FIELD IF NOT EXISTS candidate_type ON mcs_audit_vote_candidate TYPE string DEFAULT '';
DEFINE FIELD IF NOT EXISTS created_at ON mcs_audit_vote_candidate TYPE datetime DEFAULT time::now();
