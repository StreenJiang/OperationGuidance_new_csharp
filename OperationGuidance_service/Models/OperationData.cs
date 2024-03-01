using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("operation_data")]
    public class OperationData: AEntityBase {
        // 其他信息字段, 带“*”号的不确定来源
        public int? workstation_id { get; set; }                                            // 站点编号
        public string? workstation_name { get; set; }                                       // 站点名称
        public string? tool_name { get; set; }                                              // 工具名称
        public string? tool_ip { get; set; }                                                // 工具IP
        public string? tool_type { get; set; }                                              // 工具类别
        public string? gun_num { get; set; }                                                // 枪(轴)号（多把枪的情况）
        public int? product_sied_id { get; set; }                                           // 产品面号
        public int? bolt_serial_num { get; set; }                                           // 点位号
        public int? tightening_count { get; set; }                                          // 拧紧计数（根据需求来）
        public string? work_group_name { get; set; }                                        // * 工作组名
        public int? work_group_count { get; set; }                                          // * 工作组计数
        public int? work_group_size { get; set; }                                           // * 工作组总数量
        public int? work_group_status { get; set; }                                         // * 工作组状态

        // 数据报文字段，带“*”号的都是返回报文中不存在的字段
        public int? cell_id { get; set; }                                                   //
        public int? channel_id { get; set; }                                                //
        public string? torque_controller_name { get; set; }                                 //
        public string? vin_number { get; set; }                                             // 条码
        public int? job_id { get; set; }                                                    // 工作组号
        public int? parameter_set_number { get; set; }                                      // 程序号
        public int? strategy { get; set; }                                                  //
        public int? strategy_options { get; set; }                                          //
        public int? batch_size { get; set; }                                                // 批次总数量
        public int? batch_counter { get; set; }                                             // 批次计数
        public int? tightening_status { get; set; }                                         // 最终状态
        public int? batch_status { get; set; }                                              // 批次状态
        public int? torque_status { get; set; }                                             // 最终扭力状态
        public int? angle_status { get; set; }                                              // 最终角度状态
        public int? rundown_status { get; set; } = 1;                                       // * 贴合状态
        public int? rundown_torque_status { get; set; } = 1;                                // * 贴合扭力状态
        public int? rundown_angle_status { get; set; }                                      // 贴合角度状态
        public int? current_monitoring_status { get; set; }                                 // 
        public int? self_tap_status { get; set; }                                           //
        public int? prevail_torque_monitoring_status { get; set; }                          // 
        public int? prevail_torque_compensate_status { get; set; }                          //
        public int? tightening_error_status { get; set; }                                   //
        public float? torque_min_limit { get; set; }                                        // 最终扭力下限
        public float? torque_max_limit { get; set; }                                        // 最终扭力上限
        public float? torque_final_target { get; set; }                                     // 最终扭力目标值
        public float? torque { get; set; }                                                  // 最终扭力
        public int? angle_min { get; set; }                                                 // 最终角度下限
        public int? angle_max { get; set; }                                                 // 最终角度上限
        public int? angle_final_target { get; set; }                                        // 最终角度目标值
        public int? angle { get; set; }                                                     // 最终角度
        public int? rundown_angle_min { get; set; }                                         // 贴合角度下限
        public int? rundown_angle_max { get; set; }                                         // 贴合角度上限
        public int? rundown_angle { get; set; }                                             // 贴合角度
        public int? rundown_angle_target { get; set; } = 0;                                 // * 贴合角度目标值
        public float? rundown_torque_min { get; set; } = 0;                                 // * 贴合扭力下限
        public float? rundown_torque_max { get; set; } = 0;                                 // * 贴合扭力上限
        public float? rundown_torque { get; set; } = 0;                                     // * 贴合扭力
        public float? rundown_torque_target { get; set; } = 0;                              // * 贴合扭力目标值
        public int? current_monitoring_min { get; set; }                                    //
        public int? current_monitoring_max { get; set; }                                    //
        public int? current_monitoring_value { get; set; }                                  //
        public float? self_tap_min { get; set; }                                            // 
        public float? self_tap_max { get; set; }                                            //
        public float? self_tap_torque { get; set; }                                         // 
        public float? prevail_torque_monitoring_min { get; set; }                           // 
        public float? prevail_torque_monitoring_max { get; set; }                           // 
        public float? prevail_torque { get; set; }                                          // 
        public int? tightening_id { get; set; }                                             // 数据包戳记
        public int? job_sequence_number { get; set; }                                       // 
        public int? sync_tightening_id { get; set; }                                        // 
        public string? tool_serial_number { get; set; }                                     //
        public string? timestamp { get; set; }                                              // 拧紧时间戳记
        public string? date_or_time_of_last_change_in_parameter_set_settings { get; set; }  // 
        public string? parameter_set_name { get; set; }                                     // 程序名
        public int? torque_values_unit { get; set; }                                        // 
        public int? result_type { get; set; }                                               //
    }
}
