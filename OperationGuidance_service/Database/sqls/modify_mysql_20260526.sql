-- operation_data indexes
CREATE INDEX ix_opdata_ws ON operation_data(workstation_id, deleted);
CREATE INDEX ix_opdata_ct ON operation_data(create_time, deleted);
CREATE INDEX ix_opdata_mr ON operation_data(mission_record_id, deleted);
CREATE INDEX ix_opdata_vin ON operation_data(vin_number, deleted);

-- mission_record indexes
CREATE INDEX ix_mr_mission ON mission_record(mission_id, deleted);
CREATE INDEX ix_mr_ct ON mission_record(create_time, deleted);

-- product_mission indexes
CREATE INDEX ix_pm_filter ON product_mission(deleted, is_challenge_mission);

-- Type migrations (safe to re-run - no-op if already correct type)
ALTER TABLE operation_data MODIFY COLUMN create_time datetime;
ALTER TABLE mission_record MODIFY COLUMN create_time datetime;
ALTER TABLE operation_data MODIFY COLUMN modify_time datetime;
ALTER TABLE mission_record MODIFY COLUMN modify_time datetime;
