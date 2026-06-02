-- Replace ix_opdata_ws with covering index for GROUP BY query
SET @sql := IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'operation_data'
     AND INDEX_NAME = 'ix_opdata_ws') > 0,
    'ALTER TABLE operation_data DROP INDEX ix_opdata_ws',
    'SELECT "index ix_opdata_ws does not exist" AS message'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

CREATE INDEX ix_opdata_ws ON operation_data(workstation_id, mission_record_id);

-- Add user-filter index for mission_record max(id) queries
SET @sql := IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'mission_record'
     AND INDEX_NAME = 'ix_mr_deleted_user') = 0,
    'CREATE INDEX ix_mr_deleted_user ON mission_record(deleted, user_id, id)',
    'SELECT "index ix_mr_deleted_user already exists" AS message'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
