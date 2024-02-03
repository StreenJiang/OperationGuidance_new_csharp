using System.Collections;
using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.TextBoxes;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BoltPopUpForm: CustomPopUpForm {
        protected OperationGuidanceApis apis;
        private ProductBoltDTO _originalBoltDTO;
        private ProductBoltDTO _modifiedBoltDTO;
        List<DeviceToolDTO> _deviceToolDTOs;

        private int _boxHeight;
        private int _boxMargin;
        private int _buttonHeight;
        private TableLayoutPanel _tablePanel;
        private CustomTextBoxGroup _nameBox;
        private CustomTextBoxGroup _specificationBox;
        private CustomComboBoxGroup<int> _toolIdComboBox;
        private CustomTextBoxGroup _toolDescriptionBox;
        private CustomTextBoxGroup _bitSpecificationBox;
        private CustomTextBoxGroup _procedureSetBox;
        private CustomTextBoxGroup _torqueBox;
        private CustomTextBoxGroup _angleBox;

        public ProductBoltDTO OriginalBoltDTO { get => _originalBoltDTO; set => _originalBoltDTO = value; }
        public ProductBoltDTO ModifiedBoltDTO { get => _modifiedBoltDTO; set => _modifiedBoltDTO = value; }
        public List<DeviceToolDTO> DeviceToolDTOs { get => _deviceToolDTOs; set => _deviceToolDTOs = value; }

        public int BoxHeight { get => _boxHeight; set => _boxHeight = value; }
        public int BoxMargin { get => _boxMargin; set => _boxMargin = value; }
        public int ButtonHeight { get => _buttonHeight; set => _buttonHeight = value; }
        public TableLayoutPanel TablePanel { get => _tablePanel; set => _tablePanel = value; }
        protected CustomTextBoxGroup NameBox { get => _nameBox; set => _nameBox = value; }
        protected CustomTextBoxGroup SpecificationBox { get => _specificationBox; set => _specificationBox = value; }
        protected CustomComboBoxGroup<int> ToolIdComboBox { get => _toolIdComboBox; set => _toolIdComboBox = value; }
        protected CustomTextBoxGroup ToolDescriptionBox { get => _toolDescriptionBox; set => _toolDescriptionBox = value; }
        protected CustomTextBoxGroup BitSpecificationBox { get => _bitSpecificationBox; set => _bitSpecificationBox = value; }
        protected CustomTextBoxGroup ProcedureSetBox { get => _procedureSetBox; set => _procedureSetBox = value; }
        protected CustomTextBoxGroup TorqueBox { get => _torqueBox; set => _torqueBox = value; }
        protected CustomTextBoxGroup AngleBox { get => _angleBox; set => _angleBox = value; }

        public BoltPopUpForm(ProductBoltDTO boltDTO) : base() {
            apis = SystemUtils.GetApis();
            _originalBoltDTO = boltDTO;
            _modifiedBoltDTO = boltDTO.Clone<ProductBoltDTO>();

            // 添加文本框显示信息
            _tablePanel = new() {
                Parent = ContentPanel,
                Margin = new(0),
                Padding = new(0),
                ColumnCount = 2,
            };
            _nameBox = new("点位名称") {
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
            QueryDeviceToolListRsp queryDeviceToolListRsp = apis.QueryDeviceToolList(new());
            _deviceToolDTOs = queryDeviceToolListRsp.DeviceToolDTOs;
            Dictionary<string, int> toolIds = _deviceToolDTOs.ToDictionary(dto => CommonUtils.CannotBeNull(dto.name), dto => dto.id);
            _toolIdComboBox = new("工具") {
                Parent = _tablePanel,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 6.25,
                NameAlignment = HorizontalAlignment.Right,
                ShowRealValue = false,
                Enabled = false,
            };
            foreach (KeyValuePair<string, int> pair in toolIds) {
                _toolIdComboBox.AddItem(pair.Key, pair.Value);
            }
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
                NumberOnly = true,
            };
            _procedureSetBox = new("程序号") {
                Parent = _tablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 6.25,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
                NumberOnly = true,
            };
            _torqueBox = new("扭矩范围") {
                Parent = _tablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 6.25,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
                NumberOnly = true,
            };
            _torqueBox.Separator = "~";
            _torqueBox.GetTextBox(0);
            _torqueBox.AddTextBox();
            _angleBox = new("角度范围") {
                Parent = _tablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 6.25,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
                NumberOnly = true,
            };
            _angleBox.Separator = "~";
            _angleBox.GetTextBox(0);
            _angleBox.AddTextBox();

            _nameBox.SetValue(0, boltDTO.name);
            _specificationBox.SetValue(0, boltDTO.specification + "");
            if (boltDTO.tool_id != null && boltDTO.tool_id > 0) {
                _toolIdComboBox.SetCurrent(_toolIdComboBox.IndexOf(boltDTO.tool_id.Value));
                _toolDescriptionBox.SetValue(0, _deviceToolDTOs.Single(dto => dto.id == boltDTO.tool_id.Value).description);
            }
            _bitSpecificationBox.SetValue(0, boltDTO.bit_specification + "");
            _procedureSetBox.SetValue(0, boltDTO.parameters_set + "");
            _torqueBox.SetValue(0, boltDTO.torque_min + "");
            _torqueBox.SetValue(1, boltDTO.torque_max + "");
            _angleBox.SetValue(0, boltDTO.angle_min + "");
            _angleBox.SetValue(1, boltDTO.angle_max + "");
        }

        public void SaveTo(ProductBoltDTO dto) {
            CommonUtils.ObjectConverter<ProductBoltDTO, ProductBoltDTO>(_modifiedBoltDTO, _originalBoltDTO);
            CommonUtils.ObjectConverter<ProductBoltDTO, ProductBoltDTO>(_modifiedBoltDTO, dto);
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            int boxW = _tablePanel.Width / _tablePanel.ColumnCount - _boxMargin * 2;
            foreach (Control control in _tablePanel.Controls) {
                control.Margin = new(_boxMargin);
                if (control.GetType() == typeof(CommonButton)) {
                    control.Size = new((int) (_buttonHeight * 3.5), _buttonHeight);
                } else {
                    control.Size = new(boxW, _boxHeight);
                }
            }
        }

    }
}
