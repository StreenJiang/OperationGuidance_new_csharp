DROP INDEX IF EXISTS ix_opdata_ws ON operation_data;
CREATE INDEX ix_opdata_ws ON operation_data(workstation_id, mission_record_id);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'ix_mr_deleted_user' AND object_id = OBJECT_ID('mission_record'))
    CREATE INDEX ix_mr_deleted_user ON mission_record(deleted, user_id, id);
