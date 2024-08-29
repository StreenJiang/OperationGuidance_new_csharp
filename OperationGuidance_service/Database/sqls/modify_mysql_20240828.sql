-- ----------------------------
-- Table structure for mat_code_map_whyc
-- ----------------------------
DROP TABLE IF EXISTS `mat_code_map_whyc`;
CREATE TABLE `mat_code_map_whyc`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `mat_code` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `parameter_set` int NOT NULL,
  `macs_id` int(8) NULL DEFAULT NULL,
  `user_id` int NOT NULL,
  `deleted` int NOT NULL,
  `creator` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifier` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `create_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modify_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;
