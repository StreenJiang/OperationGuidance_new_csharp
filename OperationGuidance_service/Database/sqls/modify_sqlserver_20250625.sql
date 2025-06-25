-- operation_data 表索引
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_operation_data_deleted_user_id' AND object_id = OBJECT_ID('operation_data'))
    CREATE INDEX idx_operation_data_deleted_user_id ON operation_data(deleted, user_id, id);
GO

-- mission_record 表索引
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_mission_record_mission_id_parts' AND object_id = OBJECT_ID('mission_record'))
    CREATE INDEX idx_mission_record_mission_id_parts ON mission_record(mission_id, parts_bar_code);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_mission_record_deleted_time_mission' AND object_id = OBJECT_ID('mission_record'))
    CREATE INDEX idx_mission_record_deleted_time_mission ON mission_record(deleted, create_time, mission_id);
GO