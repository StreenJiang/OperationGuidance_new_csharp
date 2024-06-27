-- ----------------------------
-- Table structure for screw_bit_counter
-- ----------------------------
DROP TABLE IF EXISTS `screw_bit_counter`;
CREATE TABLE `screw_bit_counter`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `mission_id` int NOT NULL,
  `bit_position` int NOT NULL,
  `max_num` int NOT NULL,
  `count_each_time` int NOT NULL,
  `current_counts` int NOT NULL,
  `clear_times` int NOT NULL,
  `user_id` int NOT NULL,
  `deleted` int NOT NULL,
  `creator` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifier` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `create_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modify_time` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;
