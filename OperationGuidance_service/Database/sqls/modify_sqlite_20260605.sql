-- modify_sqlite_20260605.sql
CREATE INDEX IF NOT EXISTS ix_mr_ws ON mission_record(workstation_id, deleted);
