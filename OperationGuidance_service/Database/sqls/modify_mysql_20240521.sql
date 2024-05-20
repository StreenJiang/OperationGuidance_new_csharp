ALTER TABLE `product_bolt` 
  ADD COLUMN `arranger_id` int(11) NULL AFTER `name`,
  ADD COLUMN `setter_selector_id` int(11) NULL AFTER `location_y_percent`;
