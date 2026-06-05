-- modify_mysql_20260605.sql
SET @sql := IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'mission_record'
     AND INDEX_NAME = 'ix_mr_ws') = 0,
    'CREATE INDEX ix_mr_ws ON mission_record(workstation_id, deleted)',
    'SELECT "index ix_mr_ws already exists" AS message'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
