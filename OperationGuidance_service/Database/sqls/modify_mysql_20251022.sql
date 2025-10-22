ALTER TABLE `aneng`.`device_io` 
  ADD COLUMN `barcode` varchar(512) NULL AFTER `type`;

ALTER TABLE `aneng`.`bar_code_matching_rule` 
  ADD COLUMN `part_no` varchar(128) NULL AFTER `mission_id`;
