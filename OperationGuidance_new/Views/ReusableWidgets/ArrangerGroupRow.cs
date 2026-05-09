using CustomLibrary.Buttons;
using CustomLibrary.ComboBoxes;
using CustomLibrary.TextBoxes;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Views.ReusableWidgets {
    /// <summary>
    /// 螺丝机开盖配置的单行容器，用 Panel 内嵌名称/条码/点位文本框组 + 螺丝机组下拉框 + 操作按钮并排显示
    /// </summary>
    public class ArrangerGroupRow {
        public Panel Panel { get; }
        public CustomTextBoxButtonGroup TextBoxGroup { get; }
        public CustomComboBoxGroup<DeviceIoDTO> ArrangerBox { get; }
        public SignButton? ActionButton { get; set; }

        public string TextName {
            get => TextBoxGroup.TextName;
            set => TextBoxGroup.TextName = value;
        }

        public ArrangerGroupRow(string textName) {
            Panel = new() {
                Margin = new(0),
                Padding = new(0),
            };

            TextBoxGroup = new(textName) {
                Parent = Panel,
                Separator = "->",
                Ratio = null,
            };
            TextBoxGroup.AddTextBox();
            TextBoxGroup.AddTextBox();

            ArrangerBox = new("螺丝机组") {
                Parent = Panel,
                NeedDefaultLabel = true,
            };
        }

        public void Dispose() {
            ActionButton?.Dispose();
            ArrangerBox.Dispose();
            TextBoxGroup.Dispose();
            Panel.Dispose();
        }
    }
}
