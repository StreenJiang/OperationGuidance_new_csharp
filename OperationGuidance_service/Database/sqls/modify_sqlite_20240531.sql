ALTER TABLE "product_mission" RENAME TO "_product_mission_old_20240531";

CREATE TABLE "product_mission" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(128),
  "pn_code" text(64),
  "max_ng_num" integer(4),
  "password_need_time" integer(4),
  "enabled" integer(1),
  "macs_id" integer,
  "predecessor_mission_id" integer,
  "predecessor_part_mission_ids" text(256),
  "multi_device_independence" integer(1),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

INSERT INTO "sqlite_sequence" (name, seq) VALUES ('product_mission', '1');

INSERT INTO "product_mission" ("id", "name", "pn_code", "max_ng_num", "password_need_time", "enabled", "macs_id", "predecessor_mission_id", "multi_device_independence", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time") SELECT "id", "name", "pn_code", "max_ng_num", "password_need_time", "enabled", "macs_id", "predecessor_mission_id", "multi_device_independence", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time" FROM "_product_mission_old_20240531";


DROP TABLE "_product_mission_old_20240531";
