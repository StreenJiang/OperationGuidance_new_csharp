-- ----------------------------
-- Table structure for mat_code_map_whyc
-- ----------------------------
DROP TABLE IF EXISTS "mat_code_map_whyc";
CREATE TABLE "mat_code_map_whyc" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "mat_code" text(256) NOT NULL,
  "parameter_set" integer(2) NOT NULL,
  "macs_id" integer,
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);
