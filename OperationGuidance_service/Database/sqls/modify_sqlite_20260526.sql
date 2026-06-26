CREATE INDEX ix_opdata_ws ON operation_data(workstation_id, deleted);
CREATE INDEX ix_opdata_ct ON operation_data(create_time, deleted);
CREATE INDEX ix_opdata_mr ON operation_data(mission_record_id, deleted);
CREATE INDEX ix_opdata_vin ON operation_data(vin_number, deleted);
CREATE INDEX ix_mr_mission ON mission_record(mission_id, deleted);
CREATE INDEX ix_mr_ct ON mission_record(create_time, deleted);
CREATE INDEX ix_pm_filter ON product_mission(deleted, is_challenge_mission);
