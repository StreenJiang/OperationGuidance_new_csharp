-- operation_data indexes (idempotent)
SET @sql := IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'operation_data' AND INDEX_NAME = 'ix_opdata_ws') = 0,
    'CREATE INDEX ix_opdata_ws ON operation_data(workstation_id, deleted)',
    'SELECT "index ix_opdata_ws already exists" AS message'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'operation_data' AND INDEX_NAME = 'ix_opdata_ct') = 0,
    'CREATE INDEX ix_opdata_ct ON operation_data(create_time, deleted)',
    'SELECT "index ix_opdata_ct already exists" AS message'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'operation_data' AND INDEX_NAME = 'ix_opdata_mr') = 0,
    'CREATE INDEX ix_opdata_mr ON operation_data(mission_record_id, deleted)',
    'SELECT "index ix_opdata_mr already exists" AS message'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'operation_data' AND INDEX_NAME = 'ix_opdata_vin') = 0,
    'CREATE INDEX ix_opdata_vin ON operation_data(vin_number, deleted)',
    'SELECT "index ix_opdata_vin already exists" AS message'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- mission_record indexes (idempotent)
SET @sql := IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'mission_record' AND INDEX_NAME = 'ix_mr_mission') = 0,
    'CREATE INDEX ix_mr_mission ON mission_record(mission_id, deleted)',
    'SELECT "index ix_mr_mission already exists" AS message'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'mission_record' AND INDEX_NAME = 'ix_mr_ct') = 0,
    'CREATE INDEX ix_mr_ct ON mission_record(create_time, deleted)',
    'SELECT "index ix_mr_ct already exists" AS message'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- product_mission indexes (idempotent)
SET @sql := IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'product_mission' AND INDEX_NAME = 'ix_pm_filter') = 0,
    'CREATE INDEX ix_pm_filter ON product_mission(deleted, is_challenge_mission)',
    'SELECT "index ix_pm_filter already exists" AS message'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Type migrations (skip if already datetime)
SET @sql := IF(
    (SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'operation_data' AND COLUMN_NAME = 'create_time') != 'datetime',
    'ALTER TABLE operation_data MODIFY COLUMN create_time datetime(0)',
    'SELECT "column operation_data.create_time already datetime" AS message'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    (SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'mission_record' AND COLUMN_NAME = 'create_time') != 'datetime',
    'ALTER TABLE mission_record MODIFY COLUMN create_time datetime(0)',
    'SELECT "column mission_record.create_time already datetime" AS message'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    (SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'operation_data' AND COLUMN_NAME = 'modify_time') != 'datetime',
    'ALTER TABLE operation_data MODIFY COLUMN modify_time datetime(0)',
    'SELECT "column operation_data.modify_time already datetime" AS message'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    (SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'mission_record' AND COLUMN_NAME = 'modify_time') != 'datetime',
    'ALTER TABLE mission_record MODIFY COLUMN modify_time datetime(0)',
    'SELECT "column mission_record.modify_time already datetime" AS message'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
