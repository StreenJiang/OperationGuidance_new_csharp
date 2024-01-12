using System.Collections;
using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.TextBoxes;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BoltPopUpForm: CustomPopUpForm {
        private ProductBoltDTO _originalBoltDTO;
        private ProductBoltDTO _modifiedBoltDTO;

        private int _boxHeight;
        private int _boxMargin;
        private TableLayoutPanel _tablePanel;
        private CustomTextBoxGroup _descriptionBox;
        private CustomTextBoxGroup _specificationBox;
        private CustomComboBoxGroup<int> _toolIdComboBox;
        private CustomTextBoxGroup _toolDescriptionBox;
        private CustomTextBoxGroup _bitSpecificationBox;
        private CustomTextBoxGroup _procedureSetBox;
        private CustomTextBoxGroup _torqueBox;
        private CustomTextBoxGroup _angleBox;

        public ProductBoltDTO OriginalBoltDTO { get => _originalBoltDTO; set => _originalBoltDTO = value; }
        public ProductBoltDTO ModifiedBoltDTO { get => _modifiedBoltDTO; set => _modifiedBoltDTO = value; }

        public int BoxHeight { get => _boxHeight; set => _boxHeight = value; }
        public int BoxMargin { get => _boxMargin; set => _boxMargin = value; }
        public TableLayoutPanel TablePanel { get => _tablePanel; set => _tablePanel = value; }
        protected CustomTextBoxGroup DescriptionBox { get => _descriptionBox; set => _descriptionBox = value; }
        protected CustomTextBoxGroup SpecificationBox { get => _specificationBox; set => _specificationBox = value; }
        protected CustomComboBoxGroup<int> ToolIdComboBox { get => _toolIdComboBox; set => _toolIdComboBox = value; }
        protected CustomTextBoxGroup ToolDescriptionBox { get => _toolDescriptionBox; set => _toolDescriptionBox = value; }
        protected CustomTextBoxGroup BitSpecificationBox { get => _bitSpecificationBox; set => _bitSpecificationBox = value; }
        protected CustomTextBoxGroup ProcedureSetBox { get => _procedureSetBox; set => _procedureSetBox = value; }
        protected CustomTextBoxGroup TorqueBox { get => _torqueBox; set => _torqueBox = value; }
        protected CustomTextBoxGroup AngleBox { get => _angleBox; set => _angleBox = value; }

        public BoltPopUpForm(ProductBoltDTO boltDTO) : base() {
            _originalBoltDTO = boltDTO;
            _modifiedBoltDTO = boltDTO.Clone<ProductBoltDTO>();

            // 添加文本框显示信息
            _tablePanel = new() {
                Parent = ContentPanel,
                Margin = new(0),
                Padding = new(0),
                ColumnCount = 2,
            };
            _descriptionBox = new("螺栓点位描述") {
                Parent = _tablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 6.25,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            _specificationBox = new("螺栓规格") {
                Parent = _tablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 6.25,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            _specificationBox.GetTextBox(0).NumberValidate = true;
            _toolIdComboBox = new("工具ID") {
                Parent = _tablePanel,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 6.25,
                NameAlignment = HorizontalAlignment.Right,
                ShowRealValue = true,
                Enabled = false,
            };
            _toolDescriptionBox = new("工具描述") {
                Parent = _tablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 6.25,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            _bitSpecificationBox = new("批头规格") {
                Parent = _tablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 6.25,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            _bitSpecificationBox.GetTextBox(0).NumberValidate = true;
            _procedureSetBox = new("Pset") {
                Parent = _tablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 6.25,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            _procedureSetBox.GetTextBox(0).NumberValidate = true;
            _torqueBox = new("扭矩范围") {
                Parent = _tablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 6.25,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            _torqueBox.Separator = "~";
            _torqueBox.GetTextBox(0).NumberValidate = true;
            _torqueBox.AddTextBox().NumberValidate = true;
            _angleBox = new("角度范围") {
                Parent = _tablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 6.25,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            _angleBox.Separator = "~";
            _angleBox.GetTextBox(0).NumberValidate = true;
            _angleBox.AddTextBox().NumberValidate = true;

            _descriptionBox.SetValue(0, boltDTO.description);
            _specificationBox.SetValue(0, boltDTO.specification + "");
            List<DeviceDTO> list = new() {
                new() {
                    id = 1,
                    name = "啥啥啥力臂",
                },
                new() {
                    id = 2,
                    name = "啥啥啥工具",
                },
            };
            _toolIdComboBox.AddItem(list[0].id + "（" + list[0].name + "）", list[0].id);
            _toolIdComboBox.AddItem(list[1].id + "（" + list[1].name + "）", list[1].id);
            _toolIdComboBox.SetDefault(0);
            _bitSpecificationBox.SetValue(0, boltDTO.bit_specification + "");
            _procedureSetBox.SetValue(0, boltDTO.procedure_set + "");
            _torqueBox.SetValue(0, boltDTO.torque_min + "");
            _torqueBox.SetValue(1, boltDTO.torque_max + "");
            _angleBox.SetValue(0, boltDTO.angle_min + "");
            _angleBox.SetValue(1, boltDTO.angle_max + "");
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            int boxW = _tablePanel.Width / _tablePanel.ColumnCount - _boxMargin * 2;
            IList list = _tablePanel.Controls;
            for (int i = 0; i < list.Count; i++) {
                Control? control = (Control?) list[i];
                if (control != null) {
                    control.Margin = new(_boxMargin);
                    control.Size = new(boxW, _boxHeight);
                }
            }
        }

    }
}
