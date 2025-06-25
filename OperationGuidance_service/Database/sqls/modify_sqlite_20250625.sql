-- operation_data 表索引
CREATE INDEX IF NOT EXISTS idx_operation_data_deleted_user_id ON operation_data(deleted, user_id, id);

-- mission_record 表索引
CREATE INDEX IF NOT EXISTS idx_mission_record_mission_id_parts ON mission_record(mission_id, parts_bar_code);
CREATE INDEX IF NOT EXISTS idx_mission_record_deleted_time_mission ON mission_record(deleted, create_time, mission_id);