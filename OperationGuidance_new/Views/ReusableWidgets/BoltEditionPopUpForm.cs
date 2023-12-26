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
            ToolIdBox.Enabled = true;
            // ToolDescriptionBox.Enabled = true; // tool_description will be filling in automatically after filling in tool_id
            BitSpecificationBox.Enabled = true;
            ProcedureSetBox.Enabled = true;
            TorqueBox.Enabled = true;
            AngleBox.Enabled = true;
            _positionX = new("点位X坐标", ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR) {
                Parent = TablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BoxForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                Ratio = 6.5,
                NameAlignment = HorizontalAlignment.Right,
            };
            _positionX.SetNumberValidate(0, true);
            _positionY = new("点位Y坐标", ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR) {
                Parent = TablePanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BoxForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                Ratio = 6.5,
                NameAlignment = HorizontalAlignment.Right,
            };
            _positionY.SetNumberValidate(0, true);

            TextBox descriptionBox = DescriptionBox.GetBox(0);
            descriptionBox.TextChanged += (s, e) => {
                if (DescriptionBox.CurrentErrorBoxIndex == null) {
                    ModifiedBoltDTO.description = descriptionBox.Text;
                }
            };

            TextBox specificationBox = SpecificationBox.GetBox(0);
            specificationBox.TextChanged += (s, e) => {
                if (SpecificationBox.CurrentErrorBoxIndex == null) {
                    if (specificationBox.Text != "" && specificationBox.Text != string.Empty) {
                        ModifiedBoltDTO.specification = float.Parse(specificationBox.Text);
                    } else {
                        ModifiedBoltDTO.specification = 0;
                    }
                }
            };

            TextBox toolIdBox = ToolIdBox.GetBox(0);
            toolIdBox.TextChanged += (s, e) => {
                if (ToolIdBox.CurrentErrorBoxIndex == null) {
                    if (toolIdBox.Text != "" && toolIdBox.Text != string.Empty) {
                        ModifiedBoltDTO.tool_id = int.Parse(toolIdBox.Text);
                    } else {
                        ModifiedBoltDTO.tool_id = 0;
                    }
                }
            };

            TextBox bitSpecificationBox = BitSpecificationBox.GetBox(0);
            bitSpecificationBox.TextChanged += (s, e) => {
                if (BitSpecificationBox.CurrentErrorBoxIndex == null) {
                    if (bitSpecificationBox.Text != "" && bitSpecificationBox.Text != string.Empty) {
                        ModifiedBoltDTO.bit_specification = float.Parse(bitSpecificationBox.Text);
                    } else {
                        ModifiedBoltDTO.bit_specification = 0;
                    }
                }
            };

            TextBox procedureSetBox = ProcedureSetBox.GetBox(0);
            procedureSetBox.TextChanged += (s, e) => {
                if (ProcedureSetBox.CurrentErrorBoxIndex == null) {
                    if (procedureSetBox.Text != "" && procedureSetBox.Text != string.Empty) {
                        ModifiedBoltDTO.procedure_set = int.Parse(procedureSetBox.Text);
                    } else {
                        ModifiedBoltDTO.procedure_set = 0;
                    }
                }
            };

            TextBox torqueMinBox = TorqueBox.GetBox(0);
            torqueMinBox.TextChanged += (s, e) => {
                if (TorqueBox.CurrentErrorBoxIndex == null) {
                    if (torqueMinBox.Text != "" && torqueMinBox.Text != string.Empty) {
                        ModifiedBoltDTO.torque_min = float.Parse(torqueMinBox.Text);
                    } else {
                        ModifiedBoltDTO.torque_min = 0;
                    }
                }
            };
            TextBox torqueMaxBox = TorqueBox.GetBox(1);
            torqueMaxBox.TextChanged += (s, e) => {
                if (TorqueBox.CurrentErrorBoxIndex == null) {
                    if (torqueMaxBox.Text != "" && torqueMaxBox.Text != string.Empty) {
                        ModifiedBoltDTO.torque_max = float.Parse(torqueMaxBox.Text);
                    } else {
                        ModifiedBoltDTO.torque_max = 0;
                    }
                }
            };

            TextBox angleMinBox = AngleBox.GetBox(0);
            angleMinBox.TextChanged += (s, e) => {
                if (AngleBox.CurrentErrorBoxIndex == null) {
                    if (angleMinBox.Text != "" && angleMinBox.Text != string.Empty) {
                        ModifiedBoltDTO.angle_min = float.Parse(angleMinBox.Text);
                    } else {
                        ModifiedBoltDTO.angle_min = 0;
                    }
                }
            };
            TextBox angleMaxBox = AngleBox.GetBox(1);
            angleMaxBox.TextChanged += (s, e) => {
                if (AngleBox.CurrentErrorBoxIndex == null) {
                    if (angleMaxBox.Text != "" && angleMaxBox.Text != string.Empty) {
                        ModifiedBoltDTO.angle_max = float.Parse(angleMaxBox.Text);
                    } else {
                        ModifiedBoltDTO.angle_max = 0;
                    }
                }
            };

            Point position = CommonUtils.PointStringToPoint(boltDTO.position);
            TextBox positionX = _positionX.GetBox(0);
            positionX.Text = position.X + "";
            positionX.TextChanged += (s, e) => {
                if (_positionX.CurrentErrorBoxIndex == null) {
                    if (positionX.Text != "" && positionX.Text != string.Empty) {
                        position.X = int.Parse(positionX.Text);
                    } else {
                        position.X = 0;
                    }
                    ModifiedBoltDTO.position = position.ToString();
                }
            };
            TextBox positionY = _positionY.GetBox(0);
            positionY.Text = position.Y + "";
            positionY.TextChanged += (s, e) => {
                if (_positionY.CurrentErrorBoxIndex == null) {
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
            return DescriptionBox.CurrentErrorBoxIndex == null 
                && SpecificationBox.CurrentErrorBoxIndex == null
                && ToolIdBox.CurrentErrorBoxIndex == null
                && ToolDescriptionBox.CurrentErrorBoxIndex == null
                && BitSpecificationBox.CurrentErrorBoxIndex == null
                && ProcedureSetBox.CurrentErrorBoxIndex == null
                && TorqueBox.CurrentErrorBoxIndex == null
                && AngleBox.CurrentErrorBoxIndex == null
                && _positionX.CurrentErrorBoxIndex == null
                && _positionY.CurrentErrorBoxIndex == null;
        }

    }
}
