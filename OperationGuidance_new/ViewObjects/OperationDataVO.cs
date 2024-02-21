using OperationGuidance_new.Attributes;
using OperationGuidance_new.ViewObjects.AbstractClasses;

namespace OperationGuidance_new.ViewObjects {
    public class OperationDataVO: AVOBase {
        [GridColumn("站点名称")]
        public string? workstation_name { get; set; }                                       // 站点名称
        [GridColumn("工具名称")]
        public string? tool_name { get; set; }                                              // 工具名称
        [GridColumn("工具IP")]
        public string? tool_ip { get; set; }                                                // 工具IP
        [GridColumn("工具类型")]
        public string? tool_type { get; set; }                                              // 工具类别
        [GridColumn("产品面ID")]
        public int? product_sied_id { get; set; }                                           // 产品面号
        [GridColumn("点位序号")]
        public int? bolt_serial_num { get; set; }                                           // 点位号
        [GridColumn("程序号")]
        public int? parameter_set_number { get; set; }                                      // 程序号
        [GridColumn("程序名称")]
        public string? parameter_set_name { get; set; }                                     // 程序名
        [GridColumn("批次总数量")]
        public int? batch_size { get; set; }                                                // 批次总数量
        [GridColumn("批次计数")]
        public int? batch_counter { get; set; }                                             // 批次计数
        [GridColumn("最终状态")]
        public int? tightening_status { get; set; }                                         // 最终状态（待确认）
        [GridColumn("批次状态")]
        public int? batch_status { get; set; }                                              // 批次状态
        [GridColumn("最终扭力状态")]
        public int? torque_status { get; set; }                                             // 最终扭力状态
        [GridColumn("最终角度状态")]
        public int? angle_status { get; set; }                                              // 最终角度状态
        [GridColumn("最终扭力下限")]
        public float? torque_min_limit { get; set; }                                        // 最终扭力下限
        [GridColumn("最终扭力上限")]
        public float? torque_max_limit { get; set; }                                        // 最终扭力上限
        [GridColumn("最终扭力目标值")]
        public float? torque_final_target { get; set; }                                     // 最终扭力目标值
        [GridColumn("最终扭力")]
        public float? torque { get; set; }                                                  // 最终扭力
        [GridColumn("最终角度下限")]
        public int? angle_min { get; set; }                                                 // 最终角度下限
        [GridColumn("最终角度上限")]
        public int? angle_max { get; set; }                                                 // 最终角度上限
        [GridColumn("最终角度目标值")]
        public int? angle_final_target { get; set; }                                        // 最终角度目标值
        [GridColumn("最终角度")]
        public int? angle { get; set; }                                                     // 最终角度
        [GridColumn("贴合角度下限")]
        public int? rundown_angle_min { get; set; }                                         // 贴合角度下限
        [GridColumn("贴合角度上限")]
        public int? rundown_angle_max { get; set; }                                         // 贴合角度上限
        [GridColumn("贴合角度")]
        public int? rundown_angle { get; set; }                                             // 贴合角度
        [GridColumn("贴合角度目标值")]
        public int? rundown_angle_target { get; set; } = 0;                                 // * 贴合角度目标值
        [GridColumn("贴合角度")]
        public int? rundown_torque_min { get; set; } = 0;                                   // * 贴合扭力下限
        [GridColumn("贴合扭力上限")]
        public int? rundown_torque_max { get; set; } = 0;                                   // * 贴合扭力上限
        [GridColumn("贴合扭力")]
        public int? rundown_torque { get; set; } = 0;                                       // * 贴合扭力
        [GridColumn("贴合扭力目标值")]
        public int? rundown_torque_target { get; set; } = 0;                                // * 贴合扭力目标值
        [GridColumn("拧紧时间戳记")]
        public string? timestamp { get; set; }                                              // 拧紧时间戳记

        public DateTime? filter_create_time_min { get; set; }
        public DateTime? filter_create_time_max { get; set; }

        // 覆盖base的属性
        [GridColumn("操作员")]
        public new string? creator { get; set; }
        [GridColumn("数据生成时间")]
        public override string? string_create_time { get; set; }
        public new string? modifier { get; set; }
        public new string? string_modify_time { get; set; }
    }
}
