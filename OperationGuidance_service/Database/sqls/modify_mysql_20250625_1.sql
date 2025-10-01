-- ===============
-- 1. 创建数字辅助表 (仅需一次)
-- ===============
DROP TABLE IF EXISTS numbers;
CREATE TABLE numbers (n INT PRIMARY KEY);

-- 生成 1 到 1000 的数字（可根据需要调整上限）
INSERT INTO numbers (n)
SELECT @row := @row + 1
FROM (
    SELECT 0 UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL
    SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
) t1,
(
    SELECT 0 UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL
    SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
) t2,
(
    SELECT 0 UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL
    SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
) t3,
(SELECT @row := 0) r
WHERE @row < 50;

-- ===============
-- 2. 创建目标表（完全按照您的原始结构）
-- ===============
DROP TABLE IF EXISTS parts_bar_code;

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
    INDEX idx_parts_bar_code (parts_bar_code),
    INDEX idx_mission_record_id (mission_record_id) -- 建议添加，提升查询性能
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ===============
-- 3. 高效插入数据（不再做时间格式转换！直接插入原始字符串）
-- ===============
INSERT INTO parts_bar_code (
    mission_record_id,
    parts_bar_code,
    user_id,
    deleted,
    creator,
    modifier,
    create_time,
    modify_time
)
SELECT 
    mr.id AS mission_record_id,
    TRIM(SUBSTRING_INDEX(SUBSTRING_INDEX(mr.parts_bar_code, ',', n.n), ',', -1)) AS parts_bar_code,
    mr.user_id,
    mr.deleted, -- 直接使用原值（您原逻辑是写死 2，但这里更通用）
    mr.creator,
    mr.modifier,
    mr.create_time, 
    mr.modify_time  
FROM 
    mission_record mr
    INNER JOIN numbers n 
        ON n.n <= (CHAR_LENGTH(mr.parts_bar_code) - CHAR_LENGTH(REPLACE(mr.parts_bar_code, ',', '')) + 1)
WHERE 
    mr.parts_bar_code IS NOT NULL 
    AND mr.parts_bar_code != '' 
    AND mr.deleted = 2 -- 保持您原来的过滤条件
    AND TRIM(SUBSTRING_INDEX(SUBSTRING_INDEX(mr.parts_bar_code, ',', n.n), ',', -1)) != '';

-- ===============
-- 4. （可选）清理辅助表
-- ===============
DROP TABLE IF EXISTS numbers;
