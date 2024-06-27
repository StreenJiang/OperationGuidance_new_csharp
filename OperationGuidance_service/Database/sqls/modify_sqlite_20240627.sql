-- ----------------------------
-- Table structure for screw_bit_counter
-- ----------------------------
DROP TABLE IF EXISTS "screw_bit_counter";
CREATE TABLE "screw_bit_counter" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "mission_id" integer NOT NULL,
  "bit_position" integer NOT NULL,
  "max_num" integer NOT NULL,
  "count_each_time" integer NOT NULL,
  "current_counts" integer NOT NULL,
  "clear_times" integer NOT NULL,
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);
