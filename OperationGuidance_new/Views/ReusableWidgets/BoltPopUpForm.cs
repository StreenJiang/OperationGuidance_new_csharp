using System.Collections;
using CustomLibrary.Forms;
using CustomLibrary.TextBoxes;
using OperationGuidance_new.Configs;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BoltPopUpForm: CustomPopUpForm {
        private ProductBoltDTO _originalBoltDTO;
        private ProductBoltDTO _modifiedBoltDTO;

        private TableLayoutPanel _tablePanel;
        private CustomTextBoxGroup _descriptionBox;
        private CustomTextBoxGroup _specificationBox;
        private CustomTextBoxGroup _toolIdBox;
        private CustomTextBoxGroup _toolDescriptionBox;
        private CustomTextBoxGroup _bitSpecificationBox;
        private CustomTextBoxGroup _procedureSetBox;
        private CustomTextBoxGroup _torqueBox;
        private CustomTextBoxGroup _angleBox;

        public ProductBoltDTO OriginalBoltDTO { get => _originalBoltDTO; set => _originalBoltDTO = value; }
        public ProductBoltDTO ModifiedBoltDTO { get => _modifiedBoltDTO; set => _modifiedBoltDTO = value; }

        protected TableLayoutPanel TablePanel { get => _tablePanel; set => _tablePanel = value; }
        protected CustomTextBoxGroup DescriptionBox { get => _descriptionBox; set => _descriptionBox = value; }
        protected CustomTextBoxGroup SpecificationBox { get => _specificationBox; set => _specificationBox = value; }
        protected CustomTextBoxGroup ToolIdBox { get => _toolIdBox; set => _toolIdBox = value; }
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
                Parent = this,
                Margin = new(0),
                Padding = new(0),
                ColumnCount = 2,
            };
            _descriptionBox = new("螺栓点位描述", ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR) {
                Parent = _tablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BoxForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                Ratio = 6.5,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            _specificationBox = new("螺栓规格", ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR) {
                Parent = _tablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BoxForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                Ratio = 6.5,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            _specificationBox.SetNumberValidate(0, true);
            _toolIdBox = new("工具ID", ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR) {
                Parent = _tablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BoxForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                Ratio = 6.5,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            _toolIdBox.SetNumberValidate(0, true);
            _toolDescriptionBox = new("工具描述", ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR) {
                Parent = _tablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BoxForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                Ratio = 6.5,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            _bitSpecificationBox = new("批头规格", ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR) {
                Parent = _tablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BoxForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                Ratio = 6.5,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            _bitSpecificationBox.SetNumberValidate(0, true);
            _procedureSetBox = new("Pset", ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR) {
                Parent = _tablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BoxForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                Ratio = 6.5,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            _procedureSetBox.SetNumberValidate(0, true);
            _torqueBox = new("扭矩范围", ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR) {
                Parent = _tablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BoxForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                Ratio = 6.5,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            _torqueBox.Separator = "~";
            _torqueBox.AddBoxes();
            _torqueBox.SetNumberValidate(0, true);
            _torqueBox.SetNumberValidate(1, true);
            _angleBox = new("角度范围", ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR) {
                Parent = _tablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BoxForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                Ratio = 6.5,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            _angleBox.Separator = "~";
            _angleBox.AddBoxes();
            _angleBox.SetNumberValidate(0, true);
            _angleBox.SetNumberValidate(1, true);

            _descriptionBox.SetValue(0, boltDTO.description);
            _specificationBox.SetValue(0, boltDTO.specification + "");
            _toolIdBox.SetValue(0, boltDTO.tool_id + "");
            _bitSpecificationBox.SetValue(0, boltDTO.bit_specification + "");
            _procedureSetBox.SetValue(0, boltDTO.procedure_set + "");
            _torqueBox.SetValue(0, boltDTO.torque_min + "");
            _torqueBox.SetValue(1, boltDTO.torque_max + "");
            _angleBox.SetValue(0, boltDTO.angle_min + "");
            _angleBox.SetValue(1, boltDTO.angle_max + "");
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            InvokeResizing();
        }

        private void InvokeResizing() {
            int contentH = ContentSize.Height - this.VirtualVerticalPadding * 2;
            int verticalGap = (int) (contentH * .05);
            int tableH = contentH - verticalGap * 2;
            int tableW = this.Width - this.VirtualHorizontalPadding * 2;
            int horizontalGap = (int) (tableW * .01F);
            int boxHGap = (int) (tableH * .04);
            _tablePanel.Size = new(tableW, tableH);
            _tablePanel.Location = new(this.VirtualHorizontalPadding, this.HasTitleExtraHeight + this.VirtualVerticalPadding + verticalGap);

            int boxW = _tablePanel.Width / _tablePanel.ColumnCount;
            int boxH = tableH / (_tablePanel.Controls.Count / _tablePanel.ColumnCount);
            IList list = _tablePanel.Controls;
            for (int i = 0; i < list.Count; i++) {
                Control? control = (Control?) list[i];
                if (control != null) {
                    CustomTextBoxGroup? box = control as CustomTextBoxGroup;
                    if (box != null) {
                        box.Padding = new Padding(horizontalGap, boxHGap, horizontalGap, boxHGap);
                        box.GapBetweenNameNBoxes = horizontalGap;
                        box.Size = new(boxW, boxH);
                    }
                }
            }
        }

    }
}
