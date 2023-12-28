using CustomLibrary.TextBoxes;
using OperationGuidance_new.Configs;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views.ReusableWidgets
{
    public class BoltEditionPopUpForm: BoltPopUpForm {
        private bool _modified;

        private CustomTextBoxGroup _positionX;
        private CustomTextBoxGroup _positionY;

        public bool Modified { get => _modified; }
        protected CustomTextBoxGroup PositionX { get => _positionX; set => _positionX = value; }
        protected CustomTextBoxGroup PositionY { get => _positionY; set => _positionY = value; }

        public BoltEditionPopUpForm(ProductBoltDTO boltDTO) : base(boltDTO) {
            _modified = false;
            
            DescriptionBox.Enabled = true;
            SpecificationBox.Enabled = true;
            ToolIdComboBox.Enabled = true;
            // ToolDescriptionBox.Enabled = true; // tool_description will be filling in automatically after filling in tool_id
            BitSpecificationBox.Enabled = true;
            ProcedureSetBox.Enabled = true;
            TorqueBox.Enabled = true;
            AngleBox.Enabled = true;
            _positionX = new("点位X坐标") {
                Parent = TablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 6.5,
                NameAlignment = HorizontalAlignment.Right,
            };
            _positionX.GetTextBox(0).NumberValidate = true;
            _positionY = new("点位Y坐标") {
                Parent = TablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                Ratio = 6.5,
                NameAlignment = HorizontalAlignment.Right,
            };
            _positionY.GetTextBox(0).NumberValidate = true;

            CustomTextBox descriptionBox = DescriptionBox.GetTextBox(0);
            descriptionBox.TextChanged += (s, e) => {
                if (!DescriptionBox.HasError) {
                    ModifiedBoltDTO.description = descriptionBox.Text;
                }
            };

            CustomTextBox specificationBox = SpecificationBox.GetTextBox(0);
            specificationBox.TextChanged += (s, e) => {
                if (!SpecificationBox.HasError) {
                    if (specificationBox.Text != "" && specificationBox.Text != string.Empty) {
                        ModifiedBoltDTO.specification = float.Parse(specificationBox.Text);
                    } else {
                        ModifiedBoltDTO.specification = 0;
                    }
                }
            };

            ToolIdComboBox.ItemSelected += () => {
                ModifiedBoltDTO.tool_id = ToolIdComboBox.Value;
                // TODO: query tool info (device maybe) to set tool description
                // ModifiedBoltDTO.description = "";
            };

            CustomTextBox bitSpecificationBox = BitSpecificationBox.GetTextBox(0);
            bitSpecificationBox.TextChanged += (s, e) => {
                if (!BitSpecificationBox.HasError) {
                    if (bitSpecificationBox.Text != "" && bitSpecificationBox.Text != string.Empty) {
                        ModifiedBoltDTO.bit_specification = float.Parse(bitSpecificationBox.Text);
                    } else {
                        ModifiedBoltDTO.bit_specification = 0;
                    }
                }
            };

            CustomTextBox procedureSetBox = ProcedureSetBox.GetTextBox(0);
            procedureSetBox.TextChanged += (s, e) => {
                if (!ProcedureSetBox.HasError) {
                    if (procedureSetBox.Text != "" && procedureSetBox.Text != string.Empty) {
                        ModifiedBoltDTO.procedure_set = int.Parse(procedureSetBox.Text);
                    } else {
                        ModifiedBoltDTO.procedure_set = 0;
                    }
                }
            };

            CustomTextBox torqueMinBox = TorqueBox.GetTextBox(0);
            torqueMinBox.TextChanged += (s, e) => {
                if (!TorqueBox.HasError) {
                    if (torqueMinBox.Text != "" && torqueMinBox.Text != string.Empty) {
                        ModifiedBoltDTO.torque_min = float.Parse(torqueMinBox.Text);
                    } else {
                        ModifiedBoltDTO.torque_min = 0;
                    }
                }
            };
            CustomTextBox torqueMaxBox = TorqueBox.GetTextBox(1);
            torqueMaxBox.TextChanged += (s, e) => {
                if (!TorqueBox.HasError) {
                    if (torqueMaxBox.Text != "" && torqueMaxBox.Text != string.Empty) {
                        ModifiedBoltDTO.torque_max = float.Parse(torqueMaxBox.Text);
                    } else {
                        ModifiedBoltDTO.torque_max = 0;
                    }
                }
            };

            CustomTextBox angleMinBox = AngleBox.GetTextBox(0);
            angleMinBox.TextChanged += (s, e) => {
                if (!AngleBox.HasError) {
                    if (angleMinBox.Text != "" && angleMinBox.Text != string.Empty) {
                        ModifiedBoltDTO.angle_min = float.Parse(angleMinBox.Text);
                    } else {
                        ModifiedBoltDTO.angle_min = 0;
                    }
                }
            };
            CustomTextBox angleMaxBox = AngleBox.GetTextBox(1);
            angleMaxBox.TextChanged += (s, e) => {
                if (!AngleBox.HasError) {
                    if (angleMaxBox.Text != "" && angleMaxBox.Text != string.Empty) {
                        ModifiedBoltDTO.angle_max = float.Parse(angleMaxBox.Text);
                    } else {
                        ModifiedBoltDTO.angle_max = 0;
                    }
                }
            };

            Point position = CommonUtils.PointStringToPoint(boltDTO.position);
            CustomTextBox positionX = _positionX.GetTextBox(0);
            positionX.Text = position.X + "";
            positionX.TextChanged += (s, e) => {
                if (!_positionX.HasError) {
                    if (positionX.Text != "" && positionX.Text != string.Empty) {
                        position.X = int.Parse(positionX.Text);
                    } else {
                        position.X = 0;
                    }
                    ModifiedBoltDTO.position = position.ToString();
                }
            };
            CustomTextBox positionY = _positionY.GetTextBox(0);
            positionY.Text = position.Y + "";
            positionY.TextChanged += (s, e) => {
                if (!_positionY.HasError) {
                    if (positionY.Text != "" && positionY.Text != string.Empty) {
                        position.Y = int.Parse(positionY.Text);
                    } else {
                        position.Y = 0;
                    }
                    ModifiedBoltDTO.position = position.ToString();
                }
            };
        }

        public bool ConfirmSave() {
            return DescriptionBox.HasError 
                && SpecificationBox.HasError
                && !ToolIdComboBox.IsError
                && ToolDescriptionBox.HasError
                && BitSpecificationBox.HasError
                && ProcedureSetBox.HasError
                && TorqueBox.HasError
                && AngleBox.HasError
                && _positionX.HasError
                && _positionY.HasError;
        }

    }
}
