-- operation_data
SET @sql := IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'operation_data' 
     AND INDEX_NAME = 'idx_operation_data_deleted_user_id') = 0,
    'CREATE INDEX idx_operation_data_deleted_user_id ON operation_data(deleted, user_id, id)',
    'SELECT ''index idx_operation_data_deleted_user_id already exists, skip this'' AS message'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- mission_record
SET @sql := IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'mission_record' 
     AND INDEX_NAME = 'idx_mission_record_mission_id_parts') = 0,
    'CREATE INDEX idx_mission_record_mission_id_parts ON mission_record(mission_id, parts_bar_code)',
    'SELECT ''index idx_mission_record_mission_id_parts already exists, skip this'' AS message'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql := IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'mission_record' 
     AND INDEX_NAME = 'idx_mission_record_deleted_time_mission') = 0,
    'CREATE INDEX idx_mission_record_deleted_time_mission ON mission_record(deleted, create_time, mission_id)',
    'SELECT ''index idx_mission_record_deleted_time_mission already exists, skip this'' AS message'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
