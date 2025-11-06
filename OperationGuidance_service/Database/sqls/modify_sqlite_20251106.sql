ALTER TABLE "main"."device_io" RENAME TO "_device_io_old_20251106";

CREATE TABLE "main"."device_io" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(128),
  "description" text(512),
  "ip" text(16),
  "port" integer(8),
  "type" integer(4),
  "barcode" text(512),
  "open_pos" integer(4),
  "macs_id" integer,
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

INSERT INTO "main"."sqlite_sequence" (name, seq) VALUES ('device_io', '2');

INSERT INTO "main"."device_io" ("id", "name", "description", "ip", "port", "type", "barcode", "macs_id", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time") SELECT "id", "name", "description", "ip", "port", "type", "barcode", "macs_id", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time" FROM "main"."_device_io_old_20251106";

DROP TABLE "_device_io_old_20251106";
