ALTER TABLE `product_mission` 
  ADD COLUMN `is_challenge_mission` int(1) NULL AFTER `multi_device_independence`;
ALTER TABLE `product_mission` 
  ADD COLUMN `is_first_mission` int(1) NULL AFTER `is_challenge_mission`;
ALTER TABLE `product_mission` 
  ADD COLUMN `challenge_mission_id` int(11) NULL AFTER `is_first_mission`;
