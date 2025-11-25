using OperationGuidance_service.Attributes;

namespace OperationGuidance_service.Models.DTOs {
    public class OperationDataDTO_SCII_XT {
        public string? workstation_name { get; set; }                                       // 站点名称
        public string? vin_number { get; set; }                                             // 条码
        public string? parts_bar_codes { get; set; }                                        // 核心物料码
        public string? batch_code { get; set; }                                             // 条码

        [SCII_XT_Column("点位")]
        public int bolt_serial_num { get; set; }                                            // 点位号
        [SCII_XT_Column("程序号")]
        public int? parameter_set_number { get; set; }                                      // 程序号
        [SCII_XT_Column("最终扭力下限")]
        public float? torque_min_limit { get; set; }                                        // 最终扭力下限
        [SCII_XT_Column("最终扭力目标")]
        public float? torque_final_target { get; set; }                                     // 最终扭力目标值
        [SCII_XT_Column("最终扭力上限")]
        public float? torque_max_limit { get; set; }                                        // 最终扭力上限
        [SCII_XT_Column("最终扭力")]
        public float? torque { get; set; }                                                  // 最终扭力
        [SCII_XT_Column("最终扭力判定结果", SCII_XT_ColumnType.RESULT)]
        public int? torque_status { get; set; }                                             // 最终扭力状态
        [SCII_XT_Column("最终角度下限")]
        public int? angle_min { get; set; }                                                 // 最终角度下限
        [SCII_XT_Column("最终角度上限")]
        public int? angle_max { get; set; }                                                 // 最终角度上限
        [SCII_XT_Column("最终角度")]
        public int? angle { get; set; }                                                     // 最终角度
        [SCII_XT_Column("最终角度判定结果", SCII_XT_ColumnType.RESULT)]
        public int? angle_status { get; set; }                                              // 最终角度状态
        [SCII_XT_Column("贴合角度下限")]
        public int? rundown_angle_min { get; set; }                                         // 贴合角度下限
        [SCII_XT_Column("贴合角度上限")]
        public int? rundown_angle_max { get; set; }                                         // 贴合角度上限
        [SCII_XT_Column("贴合角度")]
        public int? rundown_angle { get; set; }                                             // 贴合角度
        [SCII_XT_Column("贴合角度判定结果", SCII_XT_ColumnType.RESULT)]
        public int? rundown_angle_status { get; set; }                                      // 贴合角度状态
        [SCII_XT_Column("拧紧时间")]
        public string? time { get; set; }                                                   // 拧紧时间（锁付时间）
        [SCII_XT_Column("最终判定结果", SCII_XT_ColumnType.FINAL_RESULT)]
        public int? tightening_status { get; set; }                                         // 最终状态（待确认）
    }
}
