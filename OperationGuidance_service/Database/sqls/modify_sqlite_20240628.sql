ALTER TABLE "bar_code_matching_rule" RENAME TO "_bar_code_matching_rule_old_20240628";

CREATE TABLE "bar_code_matching_rule" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(64),
  "length" integer(8),
  "end_char" text(8),
  "key_position" text(64),
  "key_char" text(64),
  "type" integer(2) NOT NULL,
  "mission_id" integer,
  "macs_id" integer,
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

INSERT INTO "sqlite_sequence" (name, seq) VALUES ('bar_code_matching_rule', '3');

INSERT INTO "bar_code_matching_rule" ("id", "length", "end_char", "key_position", "key_char", "type", "mission_id", "macs_id", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time") SELECT "id", "length", "end_char", "key_position", "key_char", "type", "mission_id", "macs_id", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time" FROM "_bar_code_matching_rule_old_20240628";

DROP TABLE "_bar_code_matching_rule_old_20240628";
