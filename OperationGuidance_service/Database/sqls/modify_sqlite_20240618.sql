ALTER TABLE outer_database_config RENAME TO outer_database_config_glb;
ALTER TABLE "outer_database_config_glb" RENAME TO "_outer_database_config_glb_old_20240618";

CREATE TABLE "outer_database_config_glb" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "host" text(64),
  "port" integer(8),
  "database_name" text(255),
  "username" text(255),
  "password" text(255),
  "database_type" integer(2),
  "workstation_name" text(128),
  "macs_id" integer,
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

INSERT INTO "outer_database_config_glb" ("id", "host", "port", "database_name", "username", "password", "database_type", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time") SELECT "id", "host", "port", "database_name", "username", "password", "database_type", "user_id", "deleted", "creator", "modifier", "create_time", "modify_time" FROM "_outer_database_config_glb_old_20240618";


DROP TABLE "_outer_database_config_glb_old_20240618";
