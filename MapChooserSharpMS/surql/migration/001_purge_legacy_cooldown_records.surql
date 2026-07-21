-- Removes pre-server-key cooldown records (single-key schema, id = map/group name).
-- Runs on every boot (schema manager applies schema -> function -> migration each
-- start), so this must stay idempotent: new records always carry a non-empty
-- server_key and are never touched.
DELETE mcs_map_cooldown WHERE server_key = NONE OR server_key = '';
DELETE mcs_group_cooldown WHERE server_key = NONE OR server_key = '';
