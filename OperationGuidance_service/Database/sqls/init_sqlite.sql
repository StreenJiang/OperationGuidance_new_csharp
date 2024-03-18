/*
 Navicat Premium Data Transfer

 Source Server         : SQLite
 Source Server Type    : SQLite
 Source Server Version : 3030001
 Source Schema         : main

 Target Server Type    : SQLite
 Target Server Version : 3030001
 File Encoding         : 65001

 Date: 09/03/2024 14:52:35
*/

PRAGMA foreign_keys = false;

-- ----------------------------
-- Table structure for device_arm
-- ----------------------------
DROP TABLE IF EXISTS "device_arm";
CREATE TABLE "device_arm" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(128),
  "description" text(512),
  "ip" text(16),
  "port" integer(8),
  "type" integer(4),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);



-- ----------------------------
-- Table structure for device_communication
-- ----------------------------
DROP TABLE IF EXISTS "device_communication";
CREATE TABLE "device_communication" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(128),
  "description" text(512),
  "ip" text(16),
  "port" integer(8),
  "type" integer(4),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

-- ----------------------------
-- Records of device_communication
-- ----------------------------

-- ----------------------------
-- Table structure for device_serial_port
-- ----------------------------
DROP TABLE IF EXISTS "device_serial_port";
CREATE TABLE "device_serial_port" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(128),
  "description" text(512),
  "type" integer(4),
  "port_name" text(32),
  "port_full_name" text(128),
  "baud_rate" integer(32),
  "data_bit" integer(4),
  "parity" integer(4),
  "stop_bit" integer(4),
  "data_type" integer(4),
  "invalid_char" text(128),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);



-- ----------------------------
-- Table structure for device_tool
-- ----------------------------
DROP TABLE IF EXISTS "device_tool";
CREATE TABLE "device_tool" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(128),
  "description" text(512),
  "ip" text(16),
  "port" integer(8),
  "type" integer(4),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);


-- ----------------------------
-- Table structure for operation_data
-- ----------------------------
DROP TABLE IF EXISTS "operation_data";
CREATE TABLE "operation_data" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "mission_record_id" integer NOT NULL,
  "workstation_id" integer,
  "workstation_name" text(64),
  "tool_name" text(64),
  "tool_ip" text(64),
  "tool_type" text(64),
  "gun_num" text(64),
  "product_sied_id" integer(16),
  "bolt_serial_num" integer(16),
  "arm_position" text(32),
  "tightening_count" integer(16),
  "work_group_name" text(64),
  "work_group_count" integer(16),
  "work_group_size" integer(16),
  "work_group_status" integer(16),
  "cell_id" integer(16),
  "channel_id" integer(16),
  "torque_controller_name" text(64),
  "vin_number" text(64),
  "job_id" integer(16),
  "parameter_set_number" integer(16),
  "strategy" integer(16),
  "strategy_options" integer(16),
  "batch_size" integer(16),
  "batch_counter" integer(16),
  "tightening_status" integer(16),
  "batch_status" integer(16),
  "torque_status" integer(16),
  "angle_status" integer(16),
  "rundown_status" integer(16),
  "rundown_torque_status" integer(16),
  "rundown_angle_status" integer(16),
  "current_monitoring_status" integer(16),
  "self_tap_status" integer(16),
  "prevail_torque_monitoring_status" integer(16),
  "prevail_torque_compensate_status" integer(16),
  "tightening_error_status" integer(16),
  "torque_min_limit" real(16),
  "torque_max_limit" real(16),
  "torque_final_target" real(16),
  "torque" real(16),
  "angle_min" integer(16),
  "angle_max" integer(16),
  "angle_final_target" integer(16),
  "angle" integer(16),
  "rundown_angle_min" integer(16),
  "rundown_angle_max" integer(16),
  "rundown_angle" integer(16),
  "rundown_angle_target" integer(16),
  "rundown_torque_min" integer(16),
  "rundown_torque_max" integer(16),
  "rundown_torque" integer(16),
  "rundown_torque_target" integer(16),
  "current_monitoring_min" integer(16),
  "current_monitoring_max" integer(16),
  "current_monitoring_value" integer(16),
  "self_tap_min" real(16),
  "self_tap_max" real(16),
  "self_tap_torque" real(16),
  "prevail_torque_monitoring_min" real(16),
  "prevail_torque_monitoring_max" real(16),
  "prevail_torque" real(16),
  "tightening_id" integer(16),
  "job_sequence_number" integer(16),
  "sync_tightening_id" integer(16),
  "tool_serial_number" text(64),
  "timestamp" text(64),
  "date_or_time_of_last_change_in_parameter_set_settings" text(64),
  "parameter_set_name" text(64),
  "torque_values_unit" integer(16),
  "result_type" integer(16),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

-- ----------------------------
-- Records of operation_data
-- ----------------------------

-- ----------------------------
-- Table structure for product_bolt
-- ----------------------------
DROP TABLE IF EXISTS "product_bolt";
CREATE TABLE "product_bolt" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "side_id" integer NOT NULL,
  "serial_num" integer NOT NULL,
  "name" text(128),
  "specification" real(16),
  "workstation_id" integer,
  "position" text(32),
  "location_x_percent" real(8),
  "location_y_percent" real(8),
  "bit_specification" real(16),
  "parameters_set" integer(8),
  "torque_min" real(16),
  "torque_max" real(16),
  "angle_min" real(32),
  "angle_max" real(32),
  "enabled" integer(1),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);



-- ----------------------------
-- Table structure for product_mission
-- ----------------------------
DROP TABLE IF EXISTS "product_mission";
CREATE TABLE "product_mission" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(128),
  "pn_code" text(64),
  "max_ng_num" integer(4),
  "enabled" integer(1),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);



-- ----------------------------
-- Table structure for product_side
-- ----------------------------
DROP TABLE IF EXISTS "product_side";
CREATE TABLE "product_side" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(128),
  "mission_id" integer,
  "image" text(2048),
  "max_rectangle_width" integer(16),
  "max_rectangle_height" integer(16),
  "max_rectangle_location" text(32),
  "center_location" text(32),
  "location_offset" text(32),
  "location_offset_moving" text(32),
  "zooming_ratio" real(8),
  "zooming_ratio_extra" real(8),
  "rotate_angle" real(8),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);



-- ----------------------------
-- Table structure for user_account_info
-- ----------------------------
DROP TABLE IF EXISTS "user_account_info";
CREATE TABLE "user_account_info" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "staff_id" integer NOT NULL,
  "name" text(128) NOT NULL,
  "position" text(128),
  "account" text(64) NOT NULL,
  "password" text(64),
  "role_type" integer(2) NOT NULL,
  "operation_password" text(64),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

-- ----------------------------
-- Records of user_account_info
-- ----------------------------
INSERT INTO "user_account_info" VALUES (1, -1, 'Developr', NULL, 'sys', 'aneng135', 1, NULL, -1, 2, 'Admin', 'Admin', '2023-12-26 00:00:00', '2023-12-26 00:00:00');


-- ----------------------------
-- Table structure for workstation
-- ----------------------------
DROP TABLE IF EXISTS "workstation";
CREATE TABLE "workstation" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(128),
  "tool_id" integer,
  "arm_id" integer,
  "serial_port_id" integer,
  "communication_id" integer,
  "enabled" integer(1),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);


-- ----------------------------
-- Table structure for bar_code_matching_rule
-- ----------------------------
DROP TABLE IF EXISTS "bar_code_matching_rule";
CREATE TABLE "bar_code_matching_rule" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "length" integer(8),
  "end_char" text(8),
  "key_position" text(64),
  "key_char" text(64),
  "type" integer(2) NOT NULL,
  "mission_id" integer,
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);


-- ----------------------------
-- Table structure for mission_record
-- ----------------------------
DROP TABLE IF EXISTS "mission_record";
CREATE TABLE "mission_record" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "mission_id" integer NOT NULL,
  "product_batch" text(64) NOT NULL,
  "product_bar_code" text(512),
  "parts_bar_code" text(2048),
  "mission_result" integer(2) NOT NULL,
  "is_redo" integer(2) NOT NULL,
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

-- ----------------------------
-- Indexes structure for table mission_record
-- ----------------------------
CREATE INDEX "index_product_bar_code"
ON "mission_record" (
  "product_bar_code" ASC
);

-- ----------------------------
-- Auto increment value for device_arm
-- ----------------------------
UPDATE "sqlite_sequence" SET seq = 1 WHERE name = 'device_arm';

-- ----------------------------
-- Auto increment value for device_serial_port
-- ----------------------------
UPDATE "sqlite_sequence" SET seq = 1 WHERE name = 'device_serial_port';

-- ----------------------------
-- Auto increment value for device_tool
-- ----------------------------
UPDATE "sqlite_sequence" SET seq = 1 WHERE name = 'device_tool';

-- ----------------------------
-- Auto increment value for product_bolt
-- ----------------------------
UPDATE "sqlite_sequence" SET seq = 1 WHERE name = 'product_bolt';

-- ----------------------------
-- Auto increment value for product_mission
-- ----------------------------
UPDATE "sqlite_sequence" SET seq = 1 WHERE name = 'product_mission';

-- ----------------------------
-- Auto increment value for product_side
-- ----------------------------
UPDATE "sqlite_sequence" SET seq = 1 WHERE name = 'product_side';

-- ----------------------------
-- Auto increment value for user_account_info
-- ----------------------------
UPDATE "sqlite_sequence" SET seq = 2 WHERE name = 'user_account_info';

-- ----------------------------
-- Auto increment value for workstation
-- ----------------------------
UPDATE "sqlite_sequence" SET seq = 1 WHERE name = 'workstation';

-- ----------------------------
-- Auto increment value for bar_code_matching_rule
-- ----------------------------
UPDATE "sqlite_sequence" SET seq = 1 WHERE name = 'bar_code_matching_rule';

-- ----------------------------
-- Auto increment value for mission_record
-- ----------------------------
UPDATE "sqlite_sequence" SET seq = 1 WHERE name = 'mission_record';

PRAGMA foreign_keys = true;
