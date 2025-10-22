CREATE TABLE "main"."Untitled" (
  "id" INTEGER(11) NOT NULL,
  "mission_record_id" INTEGER(11),
  "parts_bar_code" TEXT(255),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL,
  PRIMARY KEY ("id")
);

ALTER TABLE "main"."device_io" RENAME TO "_device_io_old_20251023";

CREATE TABLE "main"."device_io" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(128),
  "description" text(512),
  "ip" text(16),
  "port" integer(8),
  "type" integer(4),
  "barcode" text(512),
  "macs_id" integer,
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

INSERT INTO "main"."sqlite_sequence" (name, seq) VALUES ('device_io', '2');

INSERT INTO "main"."device_io" ("id", "name", "description", "ip", "port", "type", "macs_id", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time") SELECT "id", "name", "description", "ip", "port", "type", "macs_id", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time" FROM "main"."_device_io_old_20251023";

DROP TABLE "_device_io_old_20251023";



ALTER TABLE "main"."bar_code_matching_rule" RENAME TO "_bar_code_matching_rule_old_20251023";

CREATE TABLE "main"."bar_code_matching_rule" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(64),
  "length" integer(8),
  "end_char" text(8),
  "key_position" text(64),
  "key_char" text(64),
  "type" integer(2) NOT NULL,
  "mission_id" integer,
  "part_no" text(128),
  "macs_id" integer,
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

INSERT INTO "main"."sqlite_sequence" (name, seq) VALUES ('bar_code_matching_rule', '6');

INSERT INTO "main"."bar_code_matching_rule" ("id", "name", "length", "end_char", "key_position", "key_char", "type", "mission_id", "macs_id", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time") SELECT "id", "name", "length", "end_char", "key_position", "key_char", "type", "mission_id", "macs_id", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time" FROM "main"."_bar_code_matching_rule_old_20251023";

DROP TABLE "_bar_code_matching_rule_old_20251023";
