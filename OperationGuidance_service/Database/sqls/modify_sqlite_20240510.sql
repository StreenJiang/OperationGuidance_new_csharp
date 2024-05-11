ALTER TABLE "main"."product_side" RENAME TO "_product_side_old_20240510";

CREATE TABLE "main"."product_side" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(128),
  "mission_id" integer,
  "image" text(256),
  "max_rectangle_width" integer(16),
  "max_rectangle_height" integer(16),
  "max_rectangle_location" text(32),
  "center_location" text(32),
  "location_offset" text(32),
  "location_offset_moving" text(32),
  "zooming_ratio" real(8),
  "zooming_ratio_extra" real(8),
  "rotate_angle" real(8),
  "cropped" integer(1),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

INSERT INTO "main"."sqlite_sequence" (name, seq) VALUES ('product_side', '1');

INSERT INTO "main"."product_side" ("id", "name", "mission_id", "image", "max_rectangle_width", "max_rectangle_height", "max_rectangle_location", "center_location", "location_offset", "location_offset_moving", "zooming_ratio", "zooming_ratio_extra", "rotate_angle", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time") SELECT "id", "name", "mission_id", "image", "max_rectangle_width", "max_rectangle_height", "max_rectangle_location", "center_location", "location_offset", "location_offset_moving", "zooming_ratio", "zooming_ratio_extra", "rotate_angle", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time" FROM "main"."_product_side_old_20240510";


DROP TABLE "main"."_product_side_old_20240510";
