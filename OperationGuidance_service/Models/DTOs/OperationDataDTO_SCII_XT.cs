using OperationGuidance_service.Attributes;
using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.DTOs {
    public class OperationDataDTO_SCII_XT: ADTOBase {
        [Description("任务记录ID")]
        public int? mission_record_id { get; set; }                                         // 任务记录ID
        [Description("站点编号")]
        public int? workstation_id { get; set; }                                            // 站点编号
        [Description("站点名称")]
        public string? workstation_name { get; set; }                                       // 站点名称
        [Description("点位号")]
        public int? bolt_serial_num { get; set; }                                           // 点位号
        [Description("条码")]
        public string? vin_number { get; set; }                                             // 条码
        [Description("程序号")]
        public int? parameter_set_number { get; set; }                                      // 程序号
        [Description("最终扭力下限")]
        public float? torque_min_limit { get; set; }                                        // 最终扭力下限
        [Description("最终扭力上限")]
        public float? torque_max_limit { get; set; }                                        // 最终扭力上限
        [Description("最终扭力目标值")]
        public float? torque_final_target { get; set; }                                     // 最终扭力目标值
        [Description("最终扭力")]
        public float? torque { get; set; }                                                  // 最终扭力
        [Description("最终扭力状态")]
        public int? torque_status { get; set; }                                             // 最终扭力状态
        [Description("最终角度下限")]
        public int? angle_min { get; set; }                                                 // 最终角度下限
        [Description("最终角度上限")]
        public int? angle_max { get; set; }                                                 // 最终角度上限
        [Description("最终角度目标值")]
        public int? angle_final_target { get; set; }                                        // 最终角度目标值
        [Description("最终角度")]
        public int? angle { get; set; }                                                     // 最终角度
        [Description("最终角度状态")]
        public int? angle_status { get; set; }                                              // 最终角度状态
        [Description("贴合角度下限")]
        public int? rundown_angle_min { get; set; }                                         // 贴合角度下限
        [Description("贴合角度上限")]
        public int? rundown_angle_max { get; set; }                                         // 贴合角度上限
        [Description("贴合角度")]
        public int? rundown_angle { get; set; }                                             // 贴合角度
        [Description("贴合角度状态")]
        public int? rundown_angle_status { get; set; }                                      // 贴合角度状态
        [Description("最终状态")]
        public int? tightening_status { get; set; }                                         // 最终状态
        [Description("拧紧时间戳记")]
        public string? timestamp { get; set; }                                              // 拧紧时间戳记
    }
}
