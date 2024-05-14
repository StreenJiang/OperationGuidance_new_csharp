/*
 Navicat Premium Data Transfer

 Source Server         : AnengDB
 Source Server Type    : MySQL
 Source Server Version : 50744
 Source Host           : localhost:3307
 Source Schema         : aneng

 Target Server Type    : MySQL
 Target Server Version : 50744
 File Encoding         : 65001

 Date: 31/03/2024 23:44:48
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for bar_code_matching_rule
-- ----------------------------
DROP TABLE IF EXISTS `bar_code_matching_rule`;
CREATE TABLE `bar_code_matching_rule`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `length` int(8) NULL DEFAULT NULL,
  `end_char` varchar(8) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `key_position` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `key_char` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `type` int(2) NOT NULL,
  `mission_id` int(11) NULL DEFAULT NULL,
  `macs_id` int(8) NULL DEFAULT NULL,
  `user_id` int(11) NOT NULL,
  `deleted` int(1) NOT NULL,
  `creator` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifier` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `create_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modify_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of bar_code_matching_rule
-- ----------------------------

-- ----------------------------
-- Table structure for device_arm
-- ----------------------------
DROP TABLE IF EXISTS `device_arm`;
CREATE TABLE `device_arm`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `description` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `ip` varchar(16) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `port` int(8) NULL DEFAULT NULL,
  `type` int(4) NULL DEFAULT NULL,
  `macs_id` int(8) NULL DEFAULT NULL,
  `user_id` int(11) NOT NULL,
  `deleted` int(1) NOT NULL,
  `creator` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifier` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `create_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modify_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of device_arm
-- ----------------------------

-- ----------------------------
-- Table structure for device_communication
-- ----------------------------
DROP TABLE IF EXISTS `device_communication`;
CREATE TABLE `device_communication`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `description` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `ip` varchar(16) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `port` int(8) NULL DEFAULT NULL,
  `type` int(4) NULL DEFAULT NULL,
  `macs_id` int(8) NULL DEFAULT NULL,
  `user_id` int(11) NOT NULL,
  `deleted` int(1) NOT NULL,
  `creator` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifier` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `create_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modify_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of device_communication
-- ----------------------------

-- ----------------------------
-- Table structure for device_serial_port
-- ----------------------------
DROP TABLE IF EXISTS `device_serial_port`;
CREATE TABLE `device_serial_port`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `description` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `type` int(4) NULL DEFAULT NULL,
  `port_name` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `port_full_name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `baud_rate` int(32) NULL DEFAULT NULL,
  `data_bit` int(4) NULL DEFAULT NULL,
  `parity` int(4) NULL DEFAULT NULL,
  `stop_bit` int(4) NULL DEFAULT NULL,
  `data_type` int(4) NULL DEFAULT NULL,
  `invalid_char` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `macs_id` int(8) NULL DEFAULT NULL,
  `user_id` int(11) NOT NULL,
  `deleted` int(1) NOT NULL,
  `creator` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifier` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `create_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modify_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of device_serial_port
-- ----------------------------

-- ----------------------------
-- Table structure for device_tool
-- ----------------------------
DROP TABLE IF EXISTS `device_tool`;
CREATE TABLE `device_tool`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `description` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `ip` varchar(16) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `port` int(8) NULL DEFAULT NULL,
  `type` int(4) NULL DEFAULT NULL,
  `macs_id` int(8) NULL DEFAULT NULL,
  `user_id` int(11) NOT NULL,
  `deleted` int(1) NOT NULL,
  `creator` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifier` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `create_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modify_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of device_tool
-- ----------------------------

-- ----------------------------
-- Table structure for mac_addresses
-- ----------------------------
DROP TABLE IF EXISTS `mac_addresses`;
CREATE TABLE `mac_addresses`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `macs` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `user_id` int(11) NOT NULL,
  `deleted` int(1) NOT NULL,
  `creator` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifier` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `create_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modify_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `index_macs`(`macs`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of mac_addresses
-- ----------------------------

-- ----------------------------
-- Table structure for mission_record
-- ----------------------------
DROP TABLE IF EXISTS `mission_record`;
CREATE TABLE `mission_record`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `mission_id` int(11) NOT NULL,
  `product_batch` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `product_bar_code` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `parts_bar_code` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `mission_result` int(2) NOT NULL,
  `is_redo` int(2) NOT NULL,
  `user_id` int(11) NOT NULL,
  `deleted` int(1) NOT NULL,
  `creator` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifier` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `create_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modify_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `index_product_bar_code`(`product_bar_code`) USING BTREE,
  INDEX `index_parts_bar_code`(`parts_bar_code`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of mission_record
-- ----------------------------

-- ----------------------------
-- Table structure for operation_data
-- ----------------------------
DROP TABLE IF EXISTS `operation_data`;
CREATE TABLE `operation_data`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `mission_record_id` int(11) NOT NULL,
  `workstation_id` int(11) NULL DEFAULT NULL,
  `workstation_name` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `tool_name` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `tool_ip` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `tool_type` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `gun_num` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `product_sied_id` int(16) NULL DEFAULT NULL,
  `bolt_serial_num` int(16) NULL DEFAULT NULL,
  `arm_position` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `tightening_count` int(16) NULL DEFAULT NULL,
  `work_group_name` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `work_group_count` int(16) NULL DEFAULT NULL,
  `work_group_size` int(16) NULL DEFAULT NULL,
  `work_group_status` int(16) NULL DEFAULT NULL,
  `cell_id` int(16) NULL DEFAULT NULL,
  `channel_id` int(16) NULL DEFAULT NULL,
  `torque_controller_name` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `vin_number` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `job_id` int(16) NULL DEFAULT NULL,
  `parameter_set_number` int(16) NULL DEFAULT NULL,
  `strategy` int(16) NULL DEFAULT NULL,
  `strategy_options` int(16) NULL DEFAULT NULL,
  `batch_size` int(16) NULL DEFAULT NULL,
  `batch_counter` int(16) NULL DEFAULT NULL,
  `tightening_status` int(16) NULL DEFAULT NULL,
  `batch_status` int(16) NULL DEFAULT NULL,
  `torque_status` int(16) NULL DEFAULT NULL,
  `angle_status` int(16) NULL DEFAULT NULL,
  `rundown_status` int(16) NULL DEFAULT NULL,
  `rundown_torque_status` int(16) NULL DEFAULT NULL,
  `rundown_angle_status` int(16) NULL DEFAULT NULL,
  `current_monitoring_status` int(16) NULL DEFAULT NULL,
  `self_tap_status` int(16) NULL DEFAULT NULL,
  `prevail_torque_monitoring_status` int(16) NULL DEFAULT NULL,
  `prevail_torque_compensate_status` int(16) NULL DEFAULT NULL,
  `tightening_error_status` int(16) NULL DEFAULT NULL,
  `torque_min_limit` double NULL DEFAULT NULL,
  `torque_max_limit` double NULL DEFAULT NULL,
  `torque_final_target` double NULL DEFAULT NULL,
  `torque` double NULL DEFAULT NULL,
  `angle_min` int(16) NULL DEFAULT NULL,
  `angle_max` int(16) NULL DEFAULT NULL,
  `angle_final_target` int(16) NULL DEFAULT NULL,
  `angle` int(16) NULL DEFAULT NULL,
  `rundown_angle_min` int(16) NULL DEFAULT NULL,
  `rundown_angle_max` int(16) NULL DEFAULT NULL,
  `rundown_angle` int(16) NULL DEFAULT NULL,
  `rundown_angle_target` int(16) NULL DEFAULT NULL,
  `rundown_torque_min` int(16) NULL DEFAULT NULL,
  `rundown_torque_max` int(16) NULL DEFAULT NULL,
  `rundown_torque` int(16) NULL DEFAULT NULL,
  `rundown_torque_target` int(16) NULL DEFAULT NULL,
  `current_monitoring_min` int(16) NULL DEFAULT NULL,
  `current_monitoring_max` int(16) NULL DEFAULT NULL,
  `current_monitoring_value` int(16) NULL DEFAULT NULL,
  `self_tap_min` double NULL DEFAULT NULL,
  `self_tap_max` double NULL DEFAULT NULL,
  `self_tap_torque` double NULL DEFAULT NULL,
  `prevail_torque_monitoring_min` double NULL DEFAULT NULL,
  `prevail_torque_monitoring_max` double NULL DEFAULT NULL,
  `prevail_torque` double NULL DEFAULT NULL,
  `tightening_id` int(16) NULL DEFAULT NULL,
  `job_sequence_number` int(16) NULL DEFAULT NULL,
  `sync_tightening_id` int(16) NULL DEFAULT NULL,
  `tool_serial_number` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `timestamp` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `date_or_time_of_last_change_in_parameter_set_settings` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `parameter_set_name` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `torque_values_unit` int(16) NULL DEFAULT NULL,
  `result_type` int(16) NULL DEFAULT NULL,
  `user_id` int(11) NOT NULL,
  `deleted` int(1) NOT NULL,
  `creator` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifier` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `create_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modify_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of operation_data
-- ----------------------------

-- ----------------------------
-- Table structure for product_bolt
-- ----------------------------
DROP TABLE IF EXISTS `product_bolt`;
CREATE TABLE `product_bolt`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `side_id` int(11) NOT NULL,
  `serial_num` int(11) NOT NULL,
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `specification` double NULL DEFAULT NULL,
  `workstation_id` int(11) NULL DEFAULT NULL,
  `position` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `location_x_percent` double NOT NULL,
  `location_y_percent` double NOT NULL,
  `bit_specification` double NULL DEFAULT NULL,
  `parameters_set` int(8) NULL DEFAULT NULL,
  `torque_min` double NULL DEFAULT NULL,
  `torque_max` double NULL DEFAULT NULL,
  `angle_min` double NULL DEFAULT NULL,
  `angle_max` double NULL DEFAULT NULL,
  `enabled` int(1) NULL DEFAULT NULL,
  `user_id` int(11) NOT NULL,
  `deleted` int(1) NOT NULL,
  `creator` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifier` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `create_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modify_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of product_bolt
-- ----------------------------

-- ----------------------------
-- Table structure for product_mission
-- ----------------------------
DROP TABLE IF EXISTS `product_mission`;
CREATE TABLE `product_mission`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `pn_code` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `max_ng_num` int(4) NULL DEFAULT NULL,
  `password_need_time` int(4) NULL DEFAULT NULL,
  `enabled` int(1) NULL DEFAULT NULL,
  `macs_id` int(8) NULL DEFAULT NULL,
  `user_id` int(11) NOT NULL,
  `deleted` int(1) NOT NULL,
  `creator` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifier` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `create_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modify_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of product_mission
-- ----------------------------

-- ----------------------------
-- Table structure for product_side
-- ----------------------------
DROP TABLE IF EXISTS `product_side`;
CREATE TABLE `product_side`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `mission_id` int(11) NULL DEFAULT NULL,
  `image` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `max_rectangle_width` int(16) NULL DEFAULT NULL,
  `max_rectangle_height` int(16) NULL DEFAULT NULL,
  `max_rectangle_location` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `center_location` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `location_offset` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `location_offset_moving` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `zooming_ratio` double NULL DEFAULT NULL,
  `zooming_ratio_extra` double NULL DEFAULT NULL,
  `rotate_angle` double NULL DEFAULT NULL,
  `user_id` int(11) NOT NULL,
  `deleted` int(1) NOT NULL,
  `creator` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifier` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `create_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modify_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of product_side
-- ----------------------------

-- ----------------------------
-- Table structure for user_account_info
-- ----------------------------
DROP TABLE IF EXISTS `user_account_info`;
CREATE TABLE `user_account_info`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `staff_id` int(11) NOT NULL,
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `position` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `account` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `password` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `role_type` int(2) NOT NULL,
  `operation_password` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `user_id` int(11) NOT NULL,
  `deleted` int(1) NOT NULL,
  `creator` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifier` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `create_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modify_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 3 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of user_account_info
-- ----------------------------
INSERT INTO `user_account_info` VALUES (1, -1, 'Developr', NULL, 'sys', '8BA05BCA959209F6CC8C4409C66E2CB5', 1, NULL, -1, 2, 'Developr', 'Developr', '2023-12-26 00:00:00', '2023-12-26 00:00:00');
INSERT INTO `user_account_info` VALUES (2, -2, 'Admin', NULL, 'admin', '21232F297A57A5A743894A0E4A801FC3', 2, '21232F297A57A5A743894A0E4A801FC3', -1, 2, 'Developr', 'Developr', '2023-12-26 00:00:00', '2023-12-26 00:00:00');

-- ----------------------------
-- Table structure for workstation
-- ----------------------------
DROP TABLE IF EXISTS `workstation`;
CREATE TABLE `workstation`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `tool_id` int(11) NULL DEFAULT NULL,
  `arm_id` int(11) NULL DEFAULT NULL,
  `serial_port_id` int(11) NULL DEFAULT NULL,
  `communication_id` int(11) NULL DEFAULT NULL,
  `enabled` int(1) NULL DEFAULT NULL,
  `macs_id` int(8) NULL DEFAULT NULL,
  `user_id` int(11) NOT NULL,
  `deleted` int(1) NOT NULL,
  `creator` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifier` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `create_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modify_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of workstation
-- ----------------------------

-- ----------------------------
-- Table structure for sql_execute_record
-- ----------------------------
DROP TABLE IF EXISTS `sql_execute_record`;
CREATE TABLE `sql_execute_record`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `file_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `user_id` int(11) NOT NULL,
  `deleted` int(1) NOT NULL,
  `creator` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifier` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `create_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modify_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;


SET FOREIGN_KEY_CHECKS = 1;
