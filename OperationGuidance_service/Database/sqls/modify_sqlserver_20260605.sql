-- modify_sqlserver_20260605.sql
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'ix_mr_ws' AND object_id = OBJECT_ID('mission_record'))
    CREATE INDEX ix_mr_ws ON mission_record(workstation_id, deleted);
