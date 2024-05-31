ALTER TABLE `product_mission` 
  ADD COLUMN `predecessor_part_mission_ids` varchar(256) NULL AFTER `predecessor_mission_id`;
