ALTER TABLE `product_bolt` 
  ADD COLUMN `arranger_id2` int NULL AFTER `specification`,
  ADD COLUMN `specification2` double NULL AFTER `arranger_id2`;
