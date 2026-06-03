ALTER TABLE `mission_record`
  ADD COLUMN `workstation_id` int(11) NULL AFTER `is_redo`,
  ADD COLUMN `workstation_name` varchar(200) NULL AFTER `workstation_id`;
