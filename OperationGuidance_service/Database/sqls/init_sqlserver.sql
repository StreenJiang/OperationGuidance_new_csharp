/*
 Navicat Premium Data Transfer

 Source Server         : SqlServer_OperationGuidanceNew
 Source Server Type    : SQL Server
 Source Server Version : 16001000
 Source Host           : 127.0.0.1:1433
 Source Catalog        : aneng
 Source Schema         : dbo

 Target Server Type    : SQL Server
 Target Server Version : 16001000
 File Encoding         : 65001

 Date: 20/06/2024 22:02:14
*/


-- ----------------------------
-- Table structure for bar_code_matching_rule
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[bar_code_matching_rule]') AND type IN ('U'))
	DROP TABLE [dbo].[bar_code_matching_rule]
GO

CREATE TABLE [dbo].[bar_code_matching_rule] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [length] int  NULL,
  [end_char] nvarchar(8) COLLATE Chinese_PRC_CI_AS  NULL,
  [key_position] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NULL,
  [key_char] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NULL,
  [type] int  NOT NULL,
  [mission_id] int  NULL,
  [macs_id] int  NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[bar_code_matching_rule] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of bar_code_matching_rule
-- ----------------------------
SET IDENTITY_INSERT [dbo].[bar_code_matching_rule] ON
GO

SET IDENTITY_INSERT [dbo].[bar_code_matching_rule] OFF
GO


-- ----------------------------
-- Table structure for curve_data
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[curve_data]') AND type IN ('U'))
	DROP TABLE [dbo].[curve_data]
GO

CREATE TABLE [dbo].[curve_data] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [operation_data_id] int  NOT NULL,
  [result_data_identifier] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [time_stamp] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [data_type] int  NOT NULL,
  [data_samples] nvarchar(max) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[curve_data] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of curve_data
-- ----------------------------
SET IDENTITY_INSERT [dbo].[curve_data] ON
GO

SET IDENTITY_INSERT [dbo].[curve_data] OFF
GO


-- ----------------------------
-- Table structure for device_arm
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[device_arm]') AND type IN ('U'))
	DROP TABLE [dbo].[device_arm]
GO

CREATE TABLE [dbo].[device_arm] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [name] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NULL,
  [description] nvarchar(512) COLLATE Chinese_PRC_CI_AS  NULL,
  [ip] nvarchar(16) COLLATE Chinese_PRC_CI_AS  NULL,
  [port] int  NULL,
  [type] int  NULL,
  [macs_id] int  NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[device_arm] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of device_arm
-- ----------------------------
SET IDENTITY_INSERT [dbo].[device_arm] ON
GO

SET IDENTITY_INSERT [dbo].[device_arm] OFF
GO


-- ----------------------------
-- Table structure for device_communication
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[device_communication]') AND type IN ('U'))
	DROP TABLE [dbo].[device_communication]
GO

CREATE TABLE [dbo].[device_communication] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [name] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NULL,
  [description] nvarchar(512) COLLATE Chinese_PRC_CI_AS  NULL,
  [ip] nvarchar(16) COLLATE Chinese_PRC_CI_AS  NULL,
  [port] int  NULL,
  [type] int  NULL,
  [macs_id] int  NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[device_communication] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of device_communication
-- ----------------------------
SET IDENTITY_INSERT [dbo].[device_communication] ON
GO

SET IDENTITY_INSERT [dbo].[device_communication] OFF
GO


-- ----------------------------
-- Table structure for device_io
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[device_io]') AND type IN ('U'))
	DROP TABLE [dbo].[device_io]
GO

CREATE TABLE [dbo].[device_io] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [name] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NULL,
  [description] nvarchar(512) COLLATE Chinese_PRC_CI_AS  NULL,
  [ip] nvarchar(16) COLLATE Chinese_PRC_CI_AS  NULL,
  [port] int  NULL,
  [type] int  NULL,
  [macs_id] int  NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[device_io] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of device_io
-- ----------------------------
SET IDENTITY_INSERT [dbo].[device_io] ON
GO

SET IDENTITY_INSERT [dbo].[device_io] OFF
GO


-- ----------------------------
-- Table structure for device_serial_port
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[device_serial_port]') AND type IN ('U'))
	DROP TABLE [dbo].[device_serial_port]
GO

CREATE TABLE [dbo].[device_serial_port] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [name] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NULL,
  [description] nvarchar(512) COLLATE Chinese_PRC_CI_AS  NULL,
  [type] int  NULL,
  [port_name] nvarchar(32) COLLATE Chinese_PRC_CI_AS  NULL,
  [port_full_name] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NULL,
  [baud_rate] int  NULL,
  [data_bit] int  NULL,
  [parity] int  NULL,
  [stop_bit] int  NULL,
  [data_type] int  NULL,
  [invalid_char] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NULL,
  [macs_id] int  NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[device_serial_port] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of device_serial_port
-- ----------------------------
SET IDENTITY_INSERT [dbo].[device_serial_port] ON
GO

SET IDENTITY_INSERT [dbo].[device_serial_port] OFF
GO


-- ----------------------------
-- Table structure for device_tool
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[device_tool]') AND type IN ('U'))
	DROP TABLE [dbo].[device_tool]
GO

CREATE TABLE [dbo].[device_tool] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [name] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NULL,
  [description] nvarchar(512) COLLATE Chinese_PRC_CI_AS  NULL,
  [ip] nvarchar(16) COLLATE Chinese_PRC_CI_AS  NULL,
  [port] int  NULL,
  [type] int  NULL,
  [macs_id] int  NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[device_tool] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of device_tool
-- ----------------------------
SET IDENTITY_INSERT [dbo].[device_tool] ON
GO

SET IDENTITY_INSERT [dbo].[device_tool] OFF
GO


-- ----------------------------
-- Table structure for mac_addresses
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[mac_addresses]') AND type IN ('U'))
	DROP TABLE [dbo].[mac_addresses]
GO

CREATE TABLE [dbo].[mac_addresses] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [macs] nvarchar(512) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[mac_addresses] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of mac_addresses
-- ----------------------------
SET IDENTITY_INSERT [dbo].[mac_addresses] ON
GO

SET IDENTITY_INSERT [dbo].[mac_addresses] OFF
GO


-- ----------------------------
-- Table structure for mission_record
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[mission_record]') AND type IN ('U'))
	DROP TABLE [dbo].[mission_record]
GO

CREATE TABLE [dbo].[mission_record] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [mission_id] int  NOT NULL,
  [product_batch] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [product_bar_code] nvarchar(512) COLLATE Chinese_PRC_CI_AS  NULL,
  [parts_bar_code] nvarchar(512) COLLATE Chinese_PRC_CI_AS  NULL,
  [mission_result] int  NOT NULL,
  [is_redo] int  NOT NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[mission_record] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of mission_record
-- ----------------------------
SET IDENTITY_INSERT [dbo].[mission_record] ON
GO

SET IDENTITY_INSERT [dbo].[mission_record] OFF
GO


-- ----------------------------
-- Table structure for operation_data
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[operation_data]') AND type IN ('U'))
	DROP TABLE [dbo].[operation_data]
GO

CREATE TABLE [dbo].[operation_data] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [mission_record_id] int  NOT NULL,
  [workstation_id] int  NULL,
  [workstation_name] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NULL,
  [tool_name] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NULL,
  [tool_ip] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NULL,
  [tool_type] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NULL,
  [gun_num] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NULL,
  [product_sied_id] int  NULL,
  [bolt_serial_num] int  NULL,
  [arm_position] nvarchar(32) COLLATE Chinese_PRC_CI_AS  NULL,
  [tightening_count] int  NULL,
  [work_group_name] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NULL,
  [work_group_count] int  NULL,
  [work_group_size] int  NULL,
  [work_group_status] int  NULL,
  [cell_id] int  NULL,
  [channel_id] int  NULL,
  [torque_controller_name] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NULL,
  [vin_number] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NULL,
  [job_id] int  NULL,
  [parameter_set_number] int  NULL,
  [strategy] int  NULL,
  [strategy_options] int  NULL,
  [batch_size] int  NULL,
  [batch_counter] int  NULL,
  [tightening_status] int  NULL,
  [batch_status] int  NULL,
  [torque_status] int  NULL,
  [angle_status] int  NULL,
  [rundown_status] int  NULL,
  [rundown_torque_status] int  NULL,
  [rundown_angle_status] int  NULL,
  [current_monitoring_status] int  NULL,
  [self_tap_status] int  NULL,
  [prevail_torque_monitoring_status] int  NULL,
  [prevail_torque_compensate_status] int  NULL,
  [tightening_error_status] int  NULL,
  [torque_min_limit] float(53)  NULL,
  [torque_max_limit] float(53)  NULL,
  [torque_final_target] float(53)  NULL,
  [torque] float(53)  NULL,
  [angle_min] int  NULL,
  [angle_max] int  NULL,
  [angle_final_target] int  NULL,
  [angle] int  NULL,
  [rundown_angle_min] int  NULL,
  [rundown_angle_max] int  NULL,
  [rundown_angle] int  NULL,
  [rundown_angle_target] int  NULL,
  [rundown_torque_min] int  NULL,
  [rundown_torque_max] int  NULL,
  [rundown_torque] int  NULL,
  [rundown_torque_target] int  NULL,
  [current_monitoring_min] int  NULL,
  [current_monitoring_max] int  NULL,
  [current_monitoring_value] int  NULL,
  [self_tap_min] float(53)  NULL,
  [self_tap_max] float(53)  NULL,
  [self_tap_torque] float(53)  NULL,
  [prevail_torque_monitoring_min] float(53)  NULL,
  [prevail_torque_monitoring_max] float(53)  NULL,
  [prevail_torque] float(53)  NULL,
  [tightening_id] int  NULL,
  [job_sequence_number] int  NULL,
  [sync_tightening_id] int  NULL,
  [tool_serial_number] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NULL,
  [timestamp] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NULL,
  [date_or_time_of_last_change_in_parameter_set_settings] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NULL,
  [parameter_set_name] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NULL,
  [torque_values_unit] int  NULL,
  [result_type] int  NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[operation_data] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of operation_data
-- ----------------------------
SET IDENTITY_INSERT [dbo].[operation_data] ON
GO

SET IDENTITY_INSERT [dbo].[operation_data] OFF
GO


-- ----------------------------
-- Table structure for outer_database_config_glb
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[outer_database_config_glb]') AND type IN ('U'))
	DROP TABLE [dbo].[outer_database_config_glb]
GO

CREATE TABLE [dbo].[outer_database_config_glb] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [host] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NULL,
  [port] int  NULL,
  [database_name] nvarchar(255) COLLATE Chinese_PRC_CI_AS  NULL,
  [username] nvarchar(255) COLLATE Chinese_PRC_CI_AS  NULL,
  [password] nvarchar(255) COLLATE Chinese_PRC_CI_AS  NULL,
  [database_type] int  NULL,
  [workstation_name] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NULL,
  [macs_id] int  NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[outer_database_config_glb] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of outer_database_config_glb
-- ----------------------------
SET IDENTITY_INSERT [dbo].[outer_database_config_glb] ON
GO

SET IDENTITY_INSERT [dbo].[outer_database_config_glb] OFF
GO


-- ----------------------------
-- Table structure for product_bolt
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[product_bolt]') AND type IN ('U'))
	DROP TABLE [dbo].[product_bolt]
GO

CREATE TABLE [dbo].[product_bolt] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [side_id] int  NOT NULL,
  [serial_num] int  NOT NULL,
  [name] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NULL,
  [arranger_id] int  NULL,
  [specification] float(53)  NULL,
  [arranger_id2] int  NULL,
  [specification2] float(53)  NULL,
  [workstation_id] int  NULL,
  [position] nvarchar(32) COLLATE Chinese_PRC_CI_AS  NULL,
  [location_x_percent] float(53)  NOT NULL,
  [location_y_percent] float(53)  NOT NULL,
  [setter_selector_id] int  NULL,
  [bit_specification] float(53)  NULL,
  [parameters_set] int  NULL,
  [torque_min] float(53)  NULL,
  [torque_max] float(53)  NULL,
  [angle_min] float(53)  NULL,
  [angle_max] float(53)  NULL,
  [enabled] int  NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[product_bolt] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of product_bolt
-- ----------------------------
SET IDENTITY_INSERT [dbo].[product_bolt] ON
GO

SET IDENTITY_INSERT [dbo].[product_bolt] OFF
GO


-- ----------------------------
-- Table structure for product_mission
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[product_mission]') AND type IN ('U'))
	DROP TABLE [dbo].[product_mission]
GO

CREATE TABLE [dbo].[product_mission] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [name] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NULL,
  [pn_code] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NULL,
  [max_ng_num] int  NULL,
  [password_need_time] int  NULL,
  [enabled] int  NULL,
  [macs_id] int  NULL,
  [predecessor_mission_id] int  NULL,
  [predecessor_part_mission_ids] nvarchar(256) COLLATE Chinese_PRC_CI_AS  NULL,
  [multi_device_independence] int  NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[product_mission] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of product_mission
-- ----------------------------
SET IDENTITY_INSERT [dbo].[product_mission] ON
GO

SET IDENTITY_INSERT [dbo].[product_mission] OFF
GO


-- ----------------------------
-- Table structure for product_side
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[product_side]') AND type IN ('U'))
	DROP TABLE [dbo].[product_side]
GO

CREATE TABLE [dbo].[product_side] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [name] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NULL,
  [mission_id] int  NULL,
  [image] nvarchar(256) COLLATE Chinese_PRC_CI_AS  NULL,
  [max_rectangle_width] int  NULL,
  [max_rectangle_height] int  NULL,
  [max_rectangle_location] nvarchar(32) COLLATE Chinese_PRC_CI_AS  NULL,
  [center_location] nvarchar(32) COLLATE Chinese_PRC_CI_AS  NULL,
  [location_offset] nvarchar(32) COLLATE Chinese_PRC_CI_AS  NULL,
  [location_offset_moving] nvarchar(32) COLLATE Chinese_PRC_CI_AS  NULL,
  [zooming_ratio] float(53)  NULL,
  [zooming_ratio_extra] float(53)  NULL,
  [rotate_angle] float(53)  NULL,
  [cropped] int  NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[product_side] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of product_side
-- ----------------------------
SET IDENTITY_INSERT [dbo].[product_side] ON
GO

SET IDENTITY_INSERT [dbo].[product_side] OFF
GO


-- ----------------------------
-- Table structure for sql_execute_record
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[sql_execute_record]') AND type IN ('U'))
	DROP TABLE [dbo].[sql_execute_record]
GO

CREATE TABLE [dbo].[sql_execute_record] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [file_name] nvarchar(255) COLLATE Chinese_PRC_CI_AS  NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[sql_execute_record] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of sql_execute_record
-- ----------------------------
SET IDENTITY_INSERT [dbo].[sql_execute_record] ON
GO

SET IDENTITY_INSERT [dbo].[sql_execute_record] OFF
GO


-- ----------------------------
-- Table structure for user_account_info
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[user_account_info]') AND type IN ('U'))
	DROP TABLE [dbo].[user_account_info]
GO

CREATE TABLE [dbo].[user_account_info] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [staff_id] int  NOT NULL,
  [name] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [position] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NULL,
  [account] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [password] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NULL,
  [role_type] int  NOT NULL,
  [operation_password] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[user_account_info] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of user_account_info
-- ----------------------------
SET IDENTITY_INSERT [dbo].[user_account_info] ON
GO

INSERT INTO [dbo].[user_account_info] ([id], [staff_id], [name], [position], [account], [password], [role_type], [operation_password], [user_id], [deleted], [creator], [modifier], [create_time], [modify_time]) VALUES (N'1', N'-1', N'Developr', NULL, N'sys', N'8BA05BCA959209F6CC8C4409C66E2CB5', N'1', NULL, N'-1', N'2', N'Developr', N'Developr', N'2023-12-26 00:00:00', N'2023-12-26 00:00:00')
GO

INSERT INTO [dbo].[user_account_info] ([id], [staff_id], [name], [position], [account], [password], [role_type], [operation_password], [user_id], [deleted], [creator], [modifier], [create_time], [modify_time]) VALUES (N'2', N'-2', N'Admin', NULL, N'admin', N'21232F297A57A5A743894A0E4A801FC3', N'2', N'21232F297A57A5A743894A0E4A801FC3', N'-1', N'2', N'Developr', N'Developr', N'2023-12-26 00:00:00', N'2023-12-26 00:00:00')
GO

SET IDENTITY_INSERT [dbo].[user_account_info] OFF
GO


-- ----------------------------
-- Table structure for workstation
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[workstation]') AND type IN ('U'))
	DROP TABLE [dbo].[workstation]
GO

CREATE TABLE [dbo].[workstation] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [name] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NULL,
  [tool_id] int  NULL,
  [arm_id] int  NULL,
  [serial_port_id] int  NULL,
  [communication_id] int  NULL,
  [enabled] int  NULL,
  [macs_id] int  NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[workstation] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of workstation
-- ----------------------------
SET IDENTITY_INSERT [dbo].[workstation] ON
GO

SET IDENTITY_INSERT [dbo].[workstation] OFF
GO


-- ----------------------------
-- Auto increment value for bar_code_matching_rule
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[bar_code_matching_rule]', RESEED, 1)
GO


-- ----------------------------
-- Primary Key structure for table bar_code_matching_rule
-- ----------------------------
ALTER TABLE [dbo].[bar_code_matching_rule] ADD CONSTRAINT [PK__bar_code__3213E83FE547EAFE] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for curve_data
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[curve_data]', RESEED, 1)
GO


-- ----------------------------
-- Primary Key structure for table curve_data
-- ----------------------------
ALTER TABLE [dbo].[curve_data] ADD CONSTRAINT [PK__curve_da__3213E83F5648C3FF] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for device_arm
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[device_arm]', RESEED, 1)
GO


-- ----------------------------
-- Primary Key structure for table device_arm
-- ----------------------------
ALTER TABLE [dbo].[device_arm] ADD CONSTRAINT [PK__device_a__3213E83F9FC528E9] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for device_communication
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[device_communication]', RESEED, 1)
GO


-- ----------------------------
-- Primary Key structure for table device_communication
-- ----------------------------
ALTER TABLE [dbo].[device_communication] ADD CONSTRAINT [PK__device_c__3213E83F2486CB8E] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for device_io
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[device_io]', RESEED, 1)
GO


-- ----------------------------
-- Primary Key structure for table device_io
-- ----------------------------
ALTER TABLE [dbo].[device_io] ADD CONSTRAINT [PK__device_i__3213E83F348AAB1B] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for device_serial_port
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[device_serial_port]', RESEED, 1)
GO


-- ----------------------------
-- Primary Key structure for table device_serial_port
-- ----------------------------
ALTER TABLE [dbo].[device_serial_port] ADD CONSTRAINT [PK__device_s__3213E83F45313E9B] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for device_tool
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[device_tool]', RESEED, 1)
GO


-- ----------------------------
-- Primary Key structure for table device_tool
-- ----------------------------
ALTER TABLE [dbo].[device_tool] ADD CONSTRAINT [PK__device_t__3213E83F15161E38] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for mac_addresses
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[mac_addresses]', RESEED, 1)
GO


-- ----------------------------
-- Indexes structure for table mac_addresses
-- ----------------------------
CREATE NONCLUSTERED INDEX [index_macs]
ON [dbo].[mac_addresses] (
  [macs] ASC
)
GO


-- ----------------------------
-- Primary Key structure for table mac_addresses
-- ----------------------------
ALTER TABLE [dbo].[mac_addresses] ADD CONSTRAINT [PK__mac_addr__3213E83FDC32E1CB] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for mission_record
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[mission_record]', RESEED, 1)
GO


-- ----------------------------
-- Indexes structure for table mission_record
-- ----------------------------
CREATE NONCLUSTERED INDEX [index_product_bar_code]
ON [dbo].[mission_record] (
  [product_bar_code] ASC
)
GO

CREATE NONCLUSTERED INDEX [index_parts_bar_code]
ON [dbo].[mission_record] (
  [parts_bar_code] ASC
)
GO


-- ----------------------------
-- Primary Key structure for table mission_record
-- ----------------------------
ALTER TABLE [dbo].[mission_record] ADD CONSTRAINT [PK__mission___3213E83FBE78F01F] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for operation_data
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[operation_data]', RESEED, 1)
GO


-- ----------------------------
-- Primary Key structure for table operation_data
-- ----------------------------
ALTER TABLE [dbo].[operation_data] ADD CONSTRAINT [PK__operatio__3213E83FAA70CE9D] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for outer_database_config_glb
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[outer_database_config_glb]', RESEED, 2)
GO


-- ----------------------------
-- Primary Key structure for table outer_database_config_glb
-- ----------------------------
ALTER TABLE [dbo].[outer_database_config_glb] ADD CONSTRAINT [PK__outer_da__3213E83FC4A2BCC3] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for product_bolt
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[product_bolt]', RESEED, 1)
GO


-- ----------------------------
-- Primary Key structure for table product_bolt
-- ----------------------------
ALTER TABLE [dbo].[product_bolt] ADD CONSTRAINT [PK__product___3213E83F78D8E71A] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for product_mission
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[product_mission]', RESEED, 1)
GO


-- ----------------------------
-- Primary Key structure for table product_mission
-- ----------------------------
ALTER TABLE [dbo].[product_mission] ADD CONSTRAINT [PK__product___3213E83FE69EF54A] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for product_side
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[product_side]', RESEED, 1)
GO


-- ----------------------------
-- Primary Key structure for table product_side
-- ----------------------------
ALTER TABLE [dbo].[product_side] ADD CONSTRAINT [PK__product___3213E83FA18C4298] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for sql_execute_record
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[sql_execute_record]', RESEED, 1)
GO


-- ----------------------------
-- Primary Key structure for table sql_execute_record
-- ----------------------------
ALTER TABLE [dbo].[sql_execute_record] ADD CONSTRAINT [PK__sql_exec__3213E83F6EFB0F65] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for user_account_info
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[user_account_info]', RESEED, 2)
GO


-- ----------------------------
-- Primary Key structure for table user_account_info
-- ----------------------------
ALTER TABLE [dbo].[user_account_info] ADD CONSTRAINT [PK__user_acc__3213E83F9F404DB7] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for workstation
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[workstation]', RESEED, 1)
GO


-- ----------------------------
-- Primary Key structure for table workstation
-- ----------------------------
ALTER TABLE [dbo].[workstation] ADD CONSTRAINT [PK__workstat__3213E83F9AD3FAED] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO

