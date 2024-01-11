using OperationGuidance_new.ViewObjects.AbstractClasses;
using OperationGuidance_new.Attributes;
using CustomLibrary.Buttons;

namespace OperationGuidance_new.ViewObjects {
    public class DeviceCategoryVO: AVOBase {
        [GridColumn("设备类型名称")]
        public string? name { get; set; }
        [GridColumn("设备类型描述")]
        public string? description { get; set; }
        [GridColumn("是否允许手动控制", typeof(ToggleButton))]
        public bool? bool_can_manipulate { get; set; }
        public int? can_manipulate { get; set; }
        [GridColumn("状态正常图标", typeof(Image))]
        public Image? image_icon_normal { get; set; }
        public string? icon_normal { get; set; }
        [GridColumn("状态错误图标", typeof(Image))]
        public Image? image_icon_error { get; set; }
        public string? icon_error { get; set; }
    }
}
