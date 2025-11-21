using OperationGuidance_new.Attributes;
using OperationGuidance_new.Constants;
using OperationGuidance_new.ViewObjects.AbstractClasses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.ViewObjects {
    public class OperationDataVO: AVOBase {
        [GridColumn("程序号")]
        public int? parameter_set_number { get; set; }                                      // 程序号
        [GridColumn("程序名称")]
        public string? parameter_set_name { get; set; }                                     // 程序名
        [GridColumn("贴合状态")]
        public string? rundown_status_str { get; set; }
        public int? _rundown_status;                                                        // 贴合状态
        public int? rundown_status {
            get => _rundown_status;
            set {
                _rundown_status = value;
                if (value != null) {
                    if (value == (int) TighteningCommonStatus.OK) {
                        rundown_status_str = Enum.GetName(TighteningCommonStatus.OK);
                    } else if (value == (int) TighteningCommonStatus.HIGH) {
                        rundown_status_str = Enum.GetName(TighteningCommonStatus.HIGH);
                    } else if (value == (int) TighteningCommonStatus.LOW) {
                        rundown_status_str = Enum.GetName(TighteningCommonStatus.LOW);
                    }
                }
            }
        }
        [GridColumn("贴合扭力状态")]
        public string? rundown_torque_status_str { get; set; }
        public int? _rundown_torque_status;                                                 // 贴合扭力状态
        public int? rundown_torque_status {
            get => _rundown_torque_status;
            set {
                _rundown_torque_status = value;
                if (value != null) {
                    if (value == (int) TighteningCommonStatus.OK) {
                        rundown_torque_status_str = Enum.GetName(TighteningCommonStatus.OK);
                    } else if (value == (int) TighteningCommonStatus.HIGH) {
                        rundown_torque_status_str = Enum.GetName(TighteningCommonStatus.HIGH);
                    } else if (value == (int) TighteningCommonStatus.LOW) {
                        rundown_torque_status_str = Enum.GetName(TighteningCommonStatus.LOW);
                    }
                }
            }
        }
        [GridColumn("贴合角度状态")]
        public string? rundown_angle_status_str { get; set; }
        public int? _rundown_angle_status;                                                  // 贴合角度状态
        public int? rundown_angle_status {
            get => _rundown_angle_status;
            set {
                _rundown_angle_status = value;
                if (value != null) {
                    if (value == (int) TighteningCommonStatus.OK) {
                        rundown_angle_status_str = Enum.GetName(TighteningCommonStatus.OK);
                    } else if (value == (int) TighteningCommonStatus.HIGH) {
                        rundown_angle_status_str = Enum.GetName(TighteningCommonStatus.HIGH);
                    } else if (value == (int) TighteningCommonStatus.LOW) {
                        rundown_angle_status_str = Enum.GetName(TighteningCommonStatus.LOW);
                    }
                }
            }
        }
        [GridColumn("贴合扭力")]
        public float? rundown_torque { get; set; }                                          // 贴合扭力
        [GridColumn("贴合扭力上限")]
        public float? rundown_torque_max { get; set; }                                      // 贴合扭力上限
        [GridColumn("贴合扭力目标值")]
        public float? rundown_torque_target { get; set; }                                   // 贴合扭力目标值
        [GridColumn("贴合扭力下限")]
        public float? rundown_torque_min { get; set; }                                      // 贴合扭力下限
        [GridColumn("贴合角度")]
        public int? rundown_angle { get; set; }                                             // 贴合角度
        [GridColumn("贴合角度上限")]
        public int? rundown_angle_max { get; set; }                                         // 贴合角度上限
        [GridColumn("贴合角度目标值")]
        public int? rundown_angle_target { get; set; }                                      // 贴合角度目标值
        [GridColumn("贴合角度下限")]
        public int? rundown_angle_min { get; set; }                                         // 贴合角度下限
        [GridColumn("最终状态")]
        public string? tightening_status_str { get; set; }
        public int? _tightening_status;                                                     // 最终状态
        public int? tightening_status {
            get => _tightening_status;
            set {
                _tightening_status = value;
                if (value != null) {
                    if (value == (int) TighteningStatus.OK) {
                        tightening_status_str = Enum.GetName(TighteningStatus.OK);
                    } else if (value == (int) TighteningStatus.NG) {
                        tightening_status_str = Enum.GetName(TighteningStatus.NG);
                    }
                }
            }
        }
        [GridColumn("最终扭力状态")]
        public string? torque_status_str { get; set; }
        public int? _torque_status;                                                         // 最终扭力状态
        public int? torque_status {
            get => _torque_status;
            set {
                _torque_status = value;
                if (value != null) {
                    if (value == (int) TighteningCommonStatus.OK) {
                        torque_status_str = Enum.GetName(TighteningCommonStatus.OK);
                    } else if (value == (int) TighteningCommonStatus.HIGH) {
                        torque_status_str = Enum.GetName(TighteningCommonStatus.HIGH);
                    } else if (value == (int) TighteningCommonStatus.LOW) {
                        torque_status_str = Enum.GetName(TighteningCommonStatus.LOW);
                    }
                }
            }
        }
        [GridColumn("最终角度状态")]
        public string? angle_status_str { get; set; }
        public int? _angle_status;                                                          // 最终角度状态
        public int? angle_status {
            get => _angle_status;
            set {
                _angle_status = value;
                if (value != null) {
                    if (value == (int) TighteningCommonStatus.OK) {
                        angle_status_str = Enum.GetName(TighteningCommonStatus.OK);
                    } else if (value == (int) TighteningCommonStatus.HIGH) {
                        angle_status_str = Enum.GetName(TighteningCommonStatus.HIGH);
                    } else if (value == (int) TighteningCommonStatus.LOW) {
                        angle_status_str = Enum.GetName(TighteningCommonStatus.LOW);
                    }
                }
            }
        }
        [GridColumn("最终扭力")]
        public float? torque { get; set; }                                                  // 最终扭力
        [GridColumn("最终扭力上限")]
        public float? torque_max_limit { get; set; }                                        // 最终扭力上限
        [GridColumn("最终扭力目标值")]
        public float? torque_final_target { get; set; }                                     // 最终扭力目标值
        [GridColumn("最终扭力下限")]
        public float? torque_min_limit { get; set; }                                        // 最终扭力下限
        [GridColumn("最终角度")]
        public int? angle { get; set; }                                                     // 最终角度
        [GridColumn("最终角度上限")]
        public int? angle_max { get; set; }                                                 // 最终角度上限
        [GridColumn("最终角度目标值")]
        public int? angle_final_target { get; set; }                                        // 最终角度目标值
        [GridColumn("最终角度下限")]
        public int? angle_min { get; set; }                                                 // 最终角度下限
        [GridColumn("批次计数")]
        public int? batch_counter { get; set; }                                             // 批次计数
        [GridColumn("批次总数量")]
        public int? batch_size { get; set; }                                                // 批次总数量
        [GridColumn("批次状态")]
        public string? batch_status_str { get; set; }
        public int? _batch_status;                                                          // 批次状态
        public int? batch_status {
            get => _batch_status;
            set {
                _batch_status = value;
                if (value != null) {
                    if (value == (int) BatchStatus.OK) {
                        batch_status_str = Enum.GetName(BatchStatus.OK);
                    } else if (value == (int) BatchStatus.NOK) {
                        batch_status_str = Enum.GetName(BatchStatus.NOK);
                    } else if (value == (int) BatchStatus.NOT_USED) {
                        batch_status_str = Enum.GetName(BatchStatus.NOT_USED);
                    }
                }
            }
        }
        [GridColumn("工作组号")]
        public int? job_id { get; set; }                                                    // 工作组号
        [GridColumn("工作组号")]
        public string? work_group_name { get; set; }                                        // 工作组名
        [GridColumn("工作组计数")]
        public int? work_group_count { get; set; }                                          // 工作组计数
        [GridColumn("工作组总数量")]
        public int? work_group_size { get; set; }                                           // 工作组总数量
        [GridColumn("工作组状态")]
        public string? work_group_status_str { get; set; }
        public int? _work_group_status;                                                          // 工作组状态
        public int? work_group_status {
            get => _work_group_status;
            set {
                _work_group_status = value;
                if (value != null) {
                    if (value == (int) TighteningCommonStatus.OK) {
                        work_group_status_str = Enum.GetName(TighteningCommonStatus.OK);
                    } else if (value == (int) TighteningCommonStatus.HIGH) {
                        work_group_status_str = Enum.GetName(TighteningCommonStatus.HIGH);
                    }
                }
            }
        }
        [GridColumn("条码")]
        public string? vin_number { get; set; }                                             // 条码
        [GridColumn("拧紧时间戳记")]
        public string? timestamp { get; set; }                                              // 拧紧时间戳记
        [GridColumn("数据包戳记")]
        public int? tightening_id { get; set; }                                             // 数据包戳记
        [GridColumn("站点名称")]
        public string? workstation_name { get; set; }                                       // 站点名称
        [GridColumn("站点ID")]
        public int? workstation_id { get; set; }                                            // 站点ID
        [GridColumn("工具IP")]
        public string? tool_ip { get; set; }                                                // 工具IP
        [GridColumn("工具名称")]
        public string? tool_name { get; set; }                                              // 工具名称
        [GridColumn("工具类型")]
        public string? tool_type { get; set; }                                              // 工具类别
        [GridColumn("枪(轴)号")]
        public string? gun_num { get; set; }                                                // 枪(轴)号（多把枪的情况）
        [GridColumn("拧紧计数")]
        public int? tightening_count { get; set; }                                          // 拧紧计数（根据需求来）
        [GridColumn("产品面号")]
        public int? product_sied_id { get; set; }                                           // 产品面号
        [GridColumn("点位号")]
        public int? bolt_serial_num { get; set; }                                           // 点位号
        [GridColumn("力臂架X坐标")]
        public string? str_arm_position_x { get; set; }
        [GridColumn("力臂架Y坐标")]
        public string? str_arm_position_y { get; set; }
        private string? _arm_position;
        public string? arm_position {                                                       // 力臂架实时坐标
            get => _arm_position;
            set {
                _arm_position = value;
                Coordinates3D coordinates = Coordinates3D.FromString(value);
                str_arm_position_x = coordinates.X + "";
                str_arm_position_y = coordinates.Y + "";
            }
        }
        public int? mission_record_id { get; set; }                                         // 任务记录ID

        public DateTime? filter_create_time_min { get; set; }
        public DateTime? filter_create_time_max { get; set; }

        // 覆盖base的属性
        [GridColumn("操作员")]
        public new string? creator { get; set; }
        [GridColumn("数据生成时间")]
        public override string? string_create_time { get; set; }
        // 这些父类定义的会显示，但是这里不显示
        public new string? modifier { get; set; }
        public new string? string_modify_time { get; set; }
    }
}
