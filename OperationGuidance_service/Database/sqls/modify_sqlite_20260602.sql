DROP INDEX IF EXISTS ix_opdata_ws;
CREATE INDEX ix_opdata_ws ON operation_data(workstation_id, mission_record_id);
CREATE INDEX IF NOT EXISTS ix_mr_deleted_user ON mission_record(deleted, user_id, id);
