using OperationGuidance_new.Attributes;
using OperationGuidance_new.ViewObjects.AbstractClasses;

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
        public int? mission_result { get; set; }
        [GridColumn("是否返工")]
        public int? is_redo { get; set; }
        public int? workstation_id { get; set; }
        [GridColumn("站点名称")]
        public string? workstation_name { get; set; }

        // 用于数据查询时的过滤
        public List<int?>? ids { get; set; }
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
