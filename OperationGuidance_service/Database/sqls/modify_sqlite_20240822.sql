ALTER TABLE "product_bolt" RENAME TO "_product_bolt_old_20240822";

CREATE TABLE "product_bolt" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "side_id" integer NOT NULL,
  "serial_num" integer NOT NULL,
  "name" text(128),
  "arranger_id" integer,
  "specification" real(16),
  "arranger_id2" integer,
  "specification2" real(16),
  "workstation_id" integer,
  "position" text(32),
  "location_x_percent" real(8) NOT NULL,
  "location_y_percent" real(8) NOT NULL,
  "setter_selector_id" integer,
  "bit_specification" real(16),
  "parameters_set" integer(8),
  "torque_min" real(16),
  "torque_max" real(16),
  "angle_min" real(32),
  "angle_max" real(32),
  "parts_bar_code_ids" text(64),
  "enabled" integer(1),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

INSERT INTO "sqlite_sequence" (name, seq) VALUES ('product_bolt', '5');

INSERT INTO "product_bolt" ("id", "side_id", "serial_num", "name", "arranger_id", "specification", "arranger_id2", "specification2", "workstation_id", "position", "location_x_percent", "location_y_percent", "setter_selector_id", "bit_specification", "parameters_set", "torque_min", "torque_max", "angle_min", "angle_max", "enabled", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time") SELECT "id", "side_id", "serial_num", "name", "arranger_id", "specification", "arranger_id2", "specification2", "workstation_id", "position", "location_x_percent", "location_y_percent", "setter_selector_id", "bit_specification", "parameters_set", "torque_min", "torque_max", "angle_min", "angle_max", "enabled", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time" FROM "_product_bolt_old_20240822";

DROP TABLE "_product_bolt_old_20240822";
