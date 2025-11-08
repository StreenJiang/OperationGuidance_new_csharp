-- 1. 创建目标表
DROP TABLE IF EXISTS parts_bar_code;
CREATE TABLE parts_bar_code (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    mission_record_id INTEGER NOT NULL,
    parts_bar_code TEXT NOT NULL,
    user_id INTEGER NOT NULL,
    deleted INTEGER NOT NULL,
    creator TEXT NOT NULL,
    modifier TEXT NOT NULL,
    create_time TEXT NOT NULL,
    modify_time TEXT NOT NULL,
    FOREIGN KEY (mission_record_id) REFERENCES mission_record(id)
);
CREATE INDEX idx_parts_bar_code ON parts_bar_code(parts_bar_code);
CREATE INDEX idx_mission_record_id ON parts_bar_code(mission_record_id);

-- 2. 使用递归 CTE 拆分并插入
WITH RECURSIVE split(mr_id, user_id, deleted, creator, modifier, create_time, modify_time, part, rest) AS (
  -- 初始：取第一条记录的第一个部分
  SELECT 
    id,
    user_id,
    deleted,
    creator,
    modifier,
    create_time,
    modify_time,
    CASE 
      WHEN INSTR(parts_bar_code, ',') > 0 THEN 
        TRIM(SUBSTR(parts_bar_code, 1, INSTR(parts_bar_code, ',') - 1))
      ELSE 
        TRIM(parts_bar_code)
    END,
    CASE 
      WHEN INSTR(parts_bar_code, ',') > 0 THEN 
        SUBSTR(parts_bar_code, INSTR(parts_bar_code, ',') + 1)
      ELSE 
        ''
    END
  FROM mission_record
  WHERE parts_bar_code IS NOT NULL 
    AND parts_bar_code != ''
    AND deleted = 2

  UNION ALL

  -- 递归：继续拆分 rest
  SELECT 
    mr_id,
    user_id,
    deleted,
    creator,
    modifier,
    create_time,
    modify_time,
    CASE 
      WHEN INSTR(rest, ',') > 0 THEN 
        TRIM(SUBSTR(rest, 1, INSTR(rest, ',') - 1))
      ELSE 
        TRIM(rest)
    END,
    CASE 
      WHEN INSTR(rest, ',') > 0 THEN 
        SUBSTR(rest, INSTR(rest, ',') + 1)
      ELSE 
        ''
    END
  FROM split
  WHERE rest != ''
)
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
    mr_id,
    part,
    user_id,
    deleted,
    creator,
    modifier,
    create_time,
    modify_time
FROM split
WHERE part != '';  -- 过滤空部分

-- 3. （可选）清理不需要的表
-- SQLite 无辅助表，无需清理
