ALTER TABLE "mission_record" RENAME TO "_mission_record_old_20260603_2";

CREATE TABLE "mission_record" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "mission_id" integer NOT NULL,
  "product_batch" text(64) NOT NULL,
  "product_bar_code" text(512),
  "parts_bar_code" text(512),
  "mission_result" integer(2) NOT NULL,
  "is_redo" integer(2) NOT NULL,
  "workstation_id" integer,
  "workstation_name" text(200),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

INSERT INTO "sqlite_sequence" (name, seq) VALUES ('mission_record', (SELECT COALESCE(MAX(id), 0) FROM "_mission_record_old_20260603_2"));

INSERT INTO "mission_record" ("id", "mission_id", "product_batch", "product_bar_code", "parts_bar_code", "mission_result", "is_redo", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time")
SELECT "id", "mission_id", "product_batch", "product_bar_code", "parts_bar_code", "mission_result", "is_redo", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time"
FROM "_mission_record_old_20260603_2";

DROP TABLE "_mission_record_old_20260603_2";

-- 重建索引（DROP TABLE 会导致索引丢失）
CREATE INDEX IF NOT EXISTS idx_mission_record_mission_id_parts ON mission_record(mission_id, parts_bar_code);
CREATE INDEX IF NOT EXISTS idx_mission_record_deleted_time_mission ON mission_record(deleted, create_time, mission_id);
CREATE INDEX ix_mr_mission ON mission_record(mission_id, deleted);
CREATE INDEX ix_mr_ct ON mission_record(create_time, deleted);
CREATE INDEX IF NOT EXISTS ix_mr_deleted_user ON mission_record(deleted, user_id, id);
