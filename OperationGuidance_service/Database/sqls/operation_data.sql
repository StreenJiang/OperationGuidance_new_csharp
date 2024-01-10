/*
 Navicat Premium Data Transfer

 Source Server         : SQLite
 Source Server Type    : SQLite
 Source Server Version : 3030001
 Source Schema         : main

 Target Server Type    : SQLite
 Target Server Version : 3030001
 File Encoding         : 65001

 Date: 10/01/2024 21:33:42
*/

PRAGMA foreign_keys = false;

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
-- Auto increment value for operation_data
-- ----------------------------

PRAGMA foreign_keys = true;
