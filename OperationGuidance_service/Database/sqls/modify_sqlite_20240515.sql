-- ----------------------------
-- Table structure for outer_database_config_glb
-- ----------------------------
DROP TABLE IF EXISTS "outer_database_config_glb";
CREATE TABLE "outer_database_config_glb" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "host" text(64),
  "port" integer(8),
  "database_name" text(255),
  "username" text(255),
  "password" text(255),
  "database_type" integer(2),
  "workstation_id" text(64),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);
