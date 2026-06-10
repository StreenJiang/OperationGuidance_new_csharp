-- Replace ix_opdata_ws with covering index for GROUP BY query
ALTER TABLE operation_data DROP INDEX ix_opdata_ws;
CREATE INDEX ix_opdata_ws ON operation_data(workstation_id, mission_record_id);

-- Add user-filter index for mission_record max(id) queries
CREATE INDEX ix_mr_deleted_user ON mission_record(deleted, user_id, id);
