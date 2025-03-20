using OperationGuidance_new.Attributes;
using OperationGuidance_new.Constants;
using OperationGuidance_new.ViewObjects.AbstractClasses;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.ViewObjects {
    public class MissionRecordVO: AVOBase {
        [GridColumn("总成码/追溯码")]
        public string? product_bar_code { get; set; }
        [GridColumn("物料码")]
        public string? parts_bar_code { get; set; }
        [GridColumn("产品批次")]
        public string? product_batch { get; set; }
        public int? mission_id { get; set; }
        [GridColumn("任务名称")]
        public string? mission_name { get; set; }
        [GridColumn("任务结果")]
        public string? mission_result_str { get; set; }
        public int? _mission_result;
        public int? mission_result {
            get => _mission_result;
            set {
                _mission_result = value;
                if (value != null) {
                    if (value == (int) TighteningStatus.OK) {
                        mission_result_str = Enum.GetName(TighteningStatus.OK);
                    } else if (value == (int) TighteningStatus.NG) {
                        mission_result_str = Enum.GetName(TighteningStatus.NG);
                    }
                }
            }
        }
        [GridColumn("是否返工")]
        public string? is_redo_str { get; set; }
        public int? _is_redo;
        public int? is_redo {
            get => _is_redo;
            set {
                _is_redo = value;
                if (value != null) {
                    if (value == (int) YesOrNo.YES) {
                        is_redo_str = "是";
                    } else if (value == (int) YesOrNo.NO) {
                        is_redo_str = "否";
                    }
                }
            }
        }
        public int? workstation_id { get; set; }
        [GridColumn("站点名称")]
        public string? workstation_name { get; set; }

        // 用于数据查询时的过滤
        public List<int?>? ids { get; set; }
        public bool? is_challenge_mission { get; set; } = false;
        public DateTime? filter_create_time_min { get; set; }
        public DateTime? filter_create_time_max { get; set; }

        // 覆盖base的属性
        [GridColumn("操作员")]
        public new string? creator { get; set; }
        [GridColumn("任务时间")]
        public override string? string_create_time { get; set; }
        // 这些父类定义的会显示，但是这里不显示
        public new string? modifier { get; set; }
        public new string? string_modify_time { get; set; }
    }
}
