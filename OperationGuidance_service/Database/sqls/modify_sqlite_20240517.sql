-- ----------------------------
-- Table structure for device_io
-- ----------------------------
DROP TABLE IF EXISTS "device_io";
CREATE TABLE "device_io" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(128),
  "description" text(512),
  "ip" text(16),
  "port" integer(8),
  "type" integer(4),
  "macs_id" integer,
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);
