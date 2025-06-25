-- 1. Create new table
CREATE TABLE IF NOT EXISTS parts_bar_code (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `mission_record_id` INT NOT NULL,
    `parts_bar_code` VARCHAR(255) NOT NULL,
    `user_id` int(11) NOT NULL,
    `deleted` int(1) NOT NULL,
    `creator` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `modifier` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `create_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `modify_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    FOREIGN KEY (mission_record_id) REFERENCES mission_record(id),
    INDEX idx_parts_bar_code (parts_bar_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 2. Delete procedure if exists
DROP PROCEDURE IF EXISTS SplitPartsBarCode;

-- 3. Create procedure (don't use DELIMITER)
CREATE PROCEDURE SplitPartsBarCode()
BEGIN
    DECLARE done INT DEFAULT FALSE;
    DECLARE record_id INT;
    DECLARE codes_text TEXT;
    DECLARE usr_id INT;
    DECLARE creator_val VARCHAR(128);
    DECLARE modifier_val VARCHAR(128);
    DECLARE create_time_val VARCHAR(64);
    DECLARE modify_time_val VARCHAR(64);
    DECLARE code_value VARCHAR(255);
    DECLARE start_pos INT;
    DECLARE end_pos INT;
    DECLARE comma_count INT;
    DECLARE i INT;
    
    -- Declare cursor for looping data in the old table
    DECLARE cur CURSOR FOR 
        SELECT 
	    id, parts_bar_code, user_id, creator, modifier, create_time, modify_time 
	FROM mission_record 
	    WHERE parts_bar_code IS NOT NULL AND parts_bar_code != '' AND deleted = 2;
    
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;
    
    OPEN cur;
    
    read_loop: LOOP
        FETCH cur INTO record_id, codes_text, usr_id, creator_val, modifier_val, create_time_val, modify_time_val;
        IF done THEN
            LEAVE read_loop;
        END IF;
        
	-- Count for comma
        SET comma_count = LENGTH(codes_text) - LENGTH(REPLACE(codes_text, ',', ''));
        
	-- If no comma, then insert the complete value
        IF comma_count = 0 THEN
            INSERT INTO parts_bar_code 
		(mission_record_id, parts_bar_code, user_id, deleted, creator, modifier, create_time, modify_time)
            VALUES (record_id, TRIM(codes_text), usr_id, 2, creator_val, modifier_val, create_time_val, modify_time_val);
        ELSE
	    -- Initialize variables
            SET start_pos = 1;
            SET i = 1;
            
	    -- Loop to handle values between each comma
            WHILE i <= comma_count + 1 DO
		-- Find next position of comma
                SET end_pos = LOCATE(',', codes_text, start_pos);
                
                IF end_pos = 0 THEN
		    -- No more comma, get the rest of the value
                    SET code_value = TRIM(SUBSTRING(codes_text, start_pos));
                ELSE
		    -- Get current value
                    SET code_value = TRIM(SUBSTRING(codes_text, start_pos, end_pos - start_pos));
                END IF;
                
		-- If the value is not null, add it to the new table
                IF code_value != '' THEN
                    INSERT INTO parts_bar_code 
			(mission_record_id, parts_bar_code, user_id, deleted, creator, modifier, create_time, modify_time)
                    VALUES (record_id, code_value, usr_id, 2, creator_val, modifier_val, create_time_val, modify_time_val);
                END IF;
                
		-- Update position
                IF end_pos = 0 THEN
                    SET start_pos = LENGTH(codes_text) + 1;
                ELSE
                    SET start_pos = end_pos + 1;
                END IF;
                
                SET i = i + 1;
            END WHILE;
        END IF;
    END LOOP;
    
    CLOSE cur;
END;

-- 4. Call procedure
CALL SplitPartsBarCode();

-- 5. Drop the procedure
DROP PROCEDURE IF EXISTS SplitPartsBarCode;
