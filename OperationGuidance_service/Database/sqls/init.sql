/*
 Navicat Premium Data Transfer

 Source Server         : SQLite
 Source Server Type    : SQLite
 Source Server Version : 3030001
 Source Schema         : main

 Target Server Type    : SQLite
 Target Server Version : 3030001
 File Encoding         : 65001

 Date: 26/12/2023 12:27:43
*/

PRAGMA foreign_keys = false;

-- ----------------------------
-- Table structure for brand
-- ----------------------------
DROP TABLE IF EXISTS "brand";
CREATE TABLE "brand" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(128),
  "description" text(512),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

-- ----------------------------
-- Table structure for device
-- ----------------------------
DROP TABLE IF EXISTS "device";
CREATE TABLE "device" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(128),
  "description" text(512),
  "model_id" integer,
  "ip" text(16),
  "port" integer(8),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

-- ----------------------------
-- Table structure for device_category
-- ----------------------------
DROP TABLE IF EXISTS "device_category";
CREATE TABLE "device_category" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(128),
  "description" text(512),
  "can_manipulate" integer(1),
  "icon_nromal" text(2048),
  "icon_error" text(2048),
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

-- ----------------------------
-- Table structure for device_model
-- ----------------------------
DROP TABLE IF EXISTS "device_model";
CREATE TABLE "device_model" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(128),
  "description" text(512),
  "category_id" integer,
  "brand_id" integer,
  "user_id" integer NOT NULL,
  "deleted" integer(1) NOT NULL,
  "creator" text(128) NOT NULL,
  "modifier" text(128) NOT NULL,
  "create_time" text(64) NOT NULL,
  "modify_time" text(64) NOT NULL
);

-- ----------------------------
-- Table structure for product_bolt
-- ----------------------------
DROP TABLE IF EXISTS "product_bolt";
CREATE TABLE "product_bolt" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "side_id" integer,
  "serial_num" integer,
  "name" text(128),
  "description" text(512),
  "specification" real(16),
  "position" text(32),
  "location_x_percent" real(8),
  "location_y_percent" real(8),
  "tool_id" integer,
  "bit_specification" real(16),
  "procedure_set" integer(8),
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
-- Table structure for workstation
-- ----------------------------
DROP TABLE IF EXISTS "workstation";
CREATE TABLE "workstation" (
  "id" integer NOT NULL PRIMARY KEY AUTOINCREMENT,
  "name" text(128),
  "tool_id" integer,
  "arm_id" integer,
  "enabled" integer(1),
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
  "procedure_set" integer(8),
  "tightened_status" text(32),
  "torque_status" text(32),
  "angle_status" text(32),
  "torque" real(32),
  "torque_max" real(32),
  "angle" real(32),
  "angle_max" real(32),
  "angle_target" real(32),
  "angle_min" real(32),
  "batch_current" integer(8),
  "batch_sum" integer(8),
  "batch_status" text(32),
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
  "staff_id" integer,
  "name" text(128),
  "position" text(128),
  "account" text(64) NOT NULL,
  "password" text(64) NOT NULL,
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
INSERT INTO "user_account_info" VALUES (1, NULL, 'Admin', NULL, 'root', 'root', -1, 2, 'Admin', 'Admin', '2023-12-26 00:00:00', '2023-12-26 00:00:00');

-- ----------------------------
-- Auto increment value for brand
-- ----------------------------

-- ----------------------------
-- Auto increment value for device
-- ----------------------------

-- ----------------------------
-- Auto increment value for device_category
-- ----------------------------

-- ----------------------------
-- Auto increment value for device_model
-- ----------------------------

-- ----------------------------
-- Auto increment value for product
-- ----------------------------

-- ----------------------------
-- Auto increment value for product_bolt
-- ----------------------------

-- ----------------------------
-- Auto increment value for product_mission
-- ----------------------------

-- ----------------------------
-- Auto increment value for product_side
-- ----------------------------

-- ----------------------------
-- Auto increment value for user_account_info
-- ----------------------------
UPDATE "sqlite_sequence" SET seq = 1 WHERE name = 'user_account_info';

PRAGMA foreign_keys = true;
