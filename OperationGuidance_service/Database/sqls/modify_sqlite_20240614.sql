-- ----------------------------
-- Table structure for curve_data
-- ----------------------------
DROP TABLE IF EXISTS "curve_data";
CREATE TABLE "curve_data" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "operation_data_id" integer NOT NULL,
  "result_data_identifier" text(64) NOT NULL,
  "time_stamp" text(64) NOT NULL,
  "data_type" integer(2) NOT NULL,
  "data_samples" text NOT NULL,
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);