-- operation_data 索引
CREATE INDEX ix_opdata_ws ON operation_data(workstation_id, deleted);
CREATE INDEX ix_opdata_ct ON operation_data(create_time, deleted);
CREATE INDEX ix_opdata_mr ON operation_data(mission_record_id, deleted);
CREATE INDEX ix_opdata_vin ON operation_data(vin_number, deleted);

-- mission_record 索引
CREATE INDEX ix_mr_mission ON mission_record(mission_id, deleted);
CREATE INDEX ix_mr_ct ON mission_record(create_time, deleted);

-- product_mission 索引
CREATE INDEX ix_pm_filter ON product_mission(deleted, is_challenge_mission);

-- create_time 类型迁移
ALTER TABLE operation_data ALTER COLUMN create_time datetime2(0);
ALTER TABLE mission_record ALTER COLUMN create_time datetime2(0);
ALTER TABLE operation_data ALTER COLUMN modify_time datetime2(0);
ALTER TABLE mission_record ALTER COLUMN modify_time datetime2(0);
