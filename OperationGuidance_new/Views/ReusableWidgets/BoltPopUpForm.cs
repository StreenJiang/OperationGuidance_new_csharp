using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Forms;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using CustomLibrary.TextBoxes;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BoltPopUpForm: CustomPopUpForm {
        protected OperationGuidanceApis apis;
        private ProductBoltDTO _originalBoltDTO;
        private ProductBoltDTO _modifiedBoltDTO;

        private int _boxHeight;
        private int _boxMargin;
        private int _buttonHeight;
        private TableLayoutPanel _tablePanel;
        private CustomTextBoxGroup _nameBox;
        private CustomTextBoxGroup _specificationBox;
        private CustomTextBoxGroup _bitSpecificationBox;
        private CustomTextBoxGroup _parameterSetBox;
        private CustomTextBoxGroup _torqueBox;
        private CustomTextBoxGroup _angleBox;

        public ProductBoltDTO OriginalBoltDTO { get => _originalBoltDTO; set => _originalBoltDTO = value; }
        public ProductBoltDTO ModifiedBoltDTO { get => _modifiedBoltDTO; set => _modifiedBoltDTO = value; }

        public int BoxHeight { get => _boxHeight; set => _boxHeight = value; }
        public int BoxMargin { get => _boxMargin; set => _boxMargin = value; }
        public int ButtonHeight { get => _buttonHeight; set => _buttonHeight = value; }
        public TableLayoutPanel TablePanel { get => _tablePanel; set => _tablePanel = value; }
        public CustomTextBoxGroup NameBox { get => _nameBox; set => _nameBox = value; }
        public CustomTextBoxGroup SpecificationBox { get => _specificationBox; set => _specificationBox = value; }
        public CustomTextBoxGroup BitSpecificationBox { get => _bitSpecificationBox; set => _bitSpecificationBox = value; }
        public CustomTextBoxGroup ParameterSetBox { get => _parameterSetBox; set => _parameterSetBox = value; }
        public CustomTextBoxGroup TorqueBox { get => _torqueBox; set => _torqueBox = value; }
        public CustomTextBoxGroup AngleBox { get => _angleBox; set => _angleBox = value; }

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
                Ratio = 7,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            _specificationBox = new("螺栓规格") {
                Parent = _tablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 7,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            _specificationBox.GetTextBox(0).NumberValidate = true;
            _bitSpecificationBox = new("批头规格") {
                Parent = _tablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 7,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
                NumberOnly = true,
            };
            _parameterSetBox = new("程序号") {
                Parent = _tablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 7,
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
                Ratio = 7,
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
                Ratio = 7,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
                NumberOnly = true,
            };
            _angleBox.Separator = "~";
            _angleBox.GetTextBox(0);
            _angleBox.AddTextBox();

            _nameBox.SetValue(0, boltDTO.name);
            _specificationBox.SetValue(0, boltDTO.specification + "");
            _bitSpecificationBox.SetValue(0, boltDTO.bit_specification + "");
            _parameterSetBox.SetValue(0, boltDTO.parameters_set + "");
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
