using OperationGuidance_service.Attributes;

namespace OperationGuidance_service.Models.DTOs {
    public class OperationDataDTO_SCII_XT {
        public int bolt_serial_num { get; set; }                                            // 点位号
        public int torque_values_unit { get; set; }                                         // 扭矩单位

        [SCII_XT_Column("最终扭力下限")]
        public float? torque_min_limit { get; set; }                                        // 最终扭力下限
        [SCII_XT_Column("最终扭力上限")]
        public float? torque_max_limit { get; set; }                                        // 最终扭力上限
        [SCII_XT_Column("最终扭力目标值")]
        public float? torque_final_target { get; set; }                                     // 最终扭力目标值
        [SCII_XT_Column("最终扭力")]
        public float? torque { get; set; }                                                  // 最终扭力
        [SCII_XT_Column("最终扭力状态")]
        public int? torque_status { get; set; }                                             // 最终扭力状态
        [SCII_XT_Column("最终角度下限", "度（°）")]
        public int? angle_min { get; set; }                                                 // 最终角度下限
        [SCII_XT_Column("最终角度上限", "度（°）")]
        public int? angle_max { get; set; }                                                 // 最终角度上限
        [SCII_XT_Column("最终角度目标值", "度（°）")]
        public int? angle_final_target { get; set; }                                        // 最终角度目标值
        [SCII_XT_Column("最终角度", "度（°）")]
        public int? angle { get; set; }                                                     // 最终角度
        [SCII_XT_Column("最终角度状态")]
        public int? angle_status { get; set; }                                              // 最终角度状态
        [SCII_XT_Column("贴合角度下限", "度（°）")]
        public int? rundown_angle_min { get; set; }                                         // 贴合角度下限
        [SCII_XT_Column("贴合角度上限", "度（°）")]
        public int? rundown_angle_max { get; set; }                                         // 贴合角度上限
        [SCII_XT_Column("贴合角度", "度（°）")]
        public int? rundown_angle { get; set; }                                             // 贴合角度
        [SCII_XT_Column("贴合角度状态")]
        public int? rundown_angle_status { get; set; }                                      // 贴合角度状态
        [SCII_XT_Column("最终状态")]
        public int? tightening_status { get; set; }                                         // 最终状态（待确认）
    }
}
