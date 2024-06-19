ALTER TABLE `outer_database_config` 
  RENAME `outer_database_config_glb`,
  ADD COLUMN `workstation_name` varchar(128) NULL AFTER `database_type`,
  ADD COLUMN `macs_id` int(11) NULL AFTER `workstation_name`;
