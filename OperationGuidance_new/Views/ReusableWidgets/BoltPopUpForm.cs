using CustomLibrary.Buttons;
using CustomLibrary.Forms;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using CustomLibrary.TextBoxes;
using CustomLibrary.ComboBoxes;
using OperationGuidance_service.Models.Responses;
using CustomLibrary.Utils;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BoltPopUpForm: CustomPopUpForm {
        private readonly int _columnCount = 1;
        protected OperationGuidanceApis apis;
        private ProductBoltDTO _originalBoltDTO;

        private int _boxHeight;
        private int _boxMargin;
        private int _buttonHeight;
        private TableLayoutPanel _tablePanel;

        private CustomTextBoxGroup _serialNumBox;
        private CustomTextBoxGroup _nameBox;
        private List<WorkstationDTO> _workstationsDTOs;
        private CustomComboBoxGroup<WorkstationDTO> _workstation;
        private CustomTextBoxButtonGroup _position;
        private CustomTextBoxGroup _parameterSetBox;
        private CustomTextBoxGroup _specificationBox;
        private CustomTextBoxGroup _bitSpecificationBox;
        private CustomTextBoxGroup _torqueBox;
        private CustomTextBoxGroup _angleBox;

        public ProductBoltDTO OriginalBoltDTO { get => _originalBoltDTO; set => _originalBoltDTO = value; }

        public int BoxHeight { get => _boxHeight; set => _boxHeight = value; }
        public int BoxMargin { get => _boxMargin; set => _boxMargin = value; }
        public int ButtonHeight { get => _buttonHeight; set => _buttonHeight = value; }
        public TableLayoutPanel TablePanel { get => _tablePanel; set => _tablePanel = value; }

        public CustomTextBoxGroup SerialNumBox { get => _serialNumBox; set => _serialNumBox = value; }
        public CustomTextBoxGroup NameBox { get => _nameBox; set => _nameBox = value; }
        public List<WorkstationDTO> WorkstationsDTOs { get => _workstationsDTOs; set => _workstationsDTOs = value; }
        public CustomComboBoxGroup<WorkstationDTO> Workstation { get => _workstation; set => _workstation = value; }
        public CustomTextBoxButtonGroup Position { get => _position; set => _position = value; }
        public CustomTextBoxGroup ParameterSetBox { get => _parameterSetBox; set => _parameterSetBox = value; }
        public CustomTextBoxGroup SpecificationBox { get => _specificationBox; set => _specificationBox = value; }
        public CustomTextBoxGroup BitSpecificationBox { get => _bitSpecificationBox; set => _bitSpecificationBox = value; }
        public CustomTextBoxGroup TorqueBox { get => _torqueBox; set => _torqueBox = value; }
        public CustomTextBoxGroup AngleBox { get => _angleBox; set => _angleBox = value; }

        public BoltPopUpForm(ProductBoltDTO boltDTO) : base() {
            apis = SystemUtils.GetApis();
            _originalBoltDTO = boltDTO;

            // 添加文本框显示信息
            _tablePanel = new() {
                Parent = ContentPanel,
                Margin = new(0),
                Padding = new(0),
                ColumnCount = _columnCount,
            };
            _serialNumBox = new("点位编号") {
                Parent = _tablePanel,
                Ratio = 8,
                NameAlignment = HorizontalAlignment.Right,
                PositiveIntOnly = true,
                Enabled = false,
            };
            _nameBox = new("点位名称") {
                Parent = _tablePanel,
                Ratio = 8,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            // 站点
            _workstation = new("站点") {
                Parent = TablePanel,
                Ratio = 8,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            QueryWorkstationListRsp queryWorkstationListRsp = apis.QueryWorkstationList(new(SystemUtils.MacAddressesDTO.id));
            _workstationsDTOs = queryWorkstationListRsp.WorkstationsDTOs;
            foreach (WorkstationDTO dto in _workstationsDTOs) {
                _workstation.AddItem(CommonUtils.CannotBeNull(dto.name), dto);
            }
            // 点位坐标
            _position = new("点位坐标") {
                Parent = _tablePanel,
                Ratio = 8,
                Separator = ",",
                NameAlignment = HorizontalAlignment.Right,
                NumberOnly = true,
                Enabled = false,
            };
            _position.GetTextBox(0);
            _position.AddTextBox();
            _position.AddTextBox();
            // 程序号 pset
            _parameterSetBox = new("程序号") {
                Parent = _tablePanel,
                Ratio = 8,
                NameAlignment = HorizontalAlignment.Right,
                NumberOnly = true,
                Enabled = false,
            };
            // 螺栓规格
            _specificationBox = new("螺栓规格") {
                Parent = _tablePanel,
                Ratio = 8,
                NameAlignment = HorizontalAlignment.Right,
                NumberOnly = true,
                Enabled = false,
            };
            // 套筒位数
            _bitSpecificationBox = new("套筒位数") {
                Parent = _tablePanel,
                Ratio = 8,
                NameAlignment = HorizontalAlignment.Right,
                NumberOnly = true,
                Enabled = false,
            };
            // 扭矩范围
            _torqueBox = new("扭矩范围") {
                Parent = _tablePanel,
                Ratio = 8,
                NameAlignment = HorizontalAlignment.Right,
                NumberOnly = true,
                Enabled = false,
            };
            _torqueBox.Separator = "~";
            _torqueBox.GetTextBox(0);
            _torqueBox.AddTextBox();
            // 角度范围
            _angleBox = new("角度范围") {
                Parent = _tablePanel,
                Ratio = 8,
                NameAlignment = HorizontalAlignment.Right,
                NumberOnly = true,
            };
            _angleBox.Separator = "~";
            _angleBox.GetTextBox(0);
            _angleBox.AddTextBox();

            // 检查是否DTO中已经有值，有的话则回填
            _serialNumBox.SetValue(0, boltDTO.serial_num + "");
            _nameBox.SetValue(0, boltDTO.name);
            WorkstationDTO? workstationDTO = WorkstationsDTOs.SingleOrDefault(dto => dto.id == boltDTO.workstation_id);
            if (workstationDTO != null) {
                Workstation.SetCurrent(Workstation.IndexOf(workstationDTO));
            }
            if (boltDTO.position != null) {
                _position.Show();
                Coordinates3D position = Coordinates3D.FromString(boltDTO.position);
                _position.SetValue(0, position.X + "");
                _position.SetValue(1, position.Y + "");
                _position.SetValue(2, position.Z + "");
            } else {
                _position.Hide();
            }
            if (boltDTO.parameters_set != null) {
                _parameterSetBox.Show();
                _parameterSetBox.SetValue(0, boltDTO.parameters_set + "");
            } else {
                _parameterSetBox.Hide();
            }
            if (boltDTO.specification != null) {
                _specificationBox.Show();
                _specificationBox.SetValue(0, boltDTO.specification + "");
            } else {
                _specificationBox.Hide();
            }
            if (boltDTO.bit_specification != null) {
                _bitSpecificationBox.Show();
                _bitSpecificationBox.SetValue(0, boltDTO.bit_specification + "");
            } else {
                _bitSpecificationBox.Hide();
            }
            if (boltDTO.torque_min != null && boltDTO.torque_max != null) {
                _torqueBox.Show();
                _torqueBox.SetValue(0, boltDTO.torque_min + "");
                _torqueBox.SetValue(1, boltDTO.torque_max + "");
            } else {
                _torqueBox.Hide();
            }
            if (boltDTO.angle_min != null && boltDTO.angle_max != null) {
                _angleBox.Show();
                _angleBox.SetValue(0, boltDTO.angle_min + "");
                _angleBox.SetValue(1, boltDTO.angle_max + "");
            } else {
                _angleBox.Hide();
            }
        }

        public void ResizeSelf() {
            ResizeTablePanelAndItsChildren();
            Invalidate();
        }

        public void ResizeTablePanelAndItsChildren() {
            CalculateDetailProperties();

            Padding contentPadding = ContentPanel.Padding;
            int boxHeight = WidgetUtils.PopUpOrFloatingFormTextOrComboBoxHeight();
            int boxMargin = boxHeight / 5;
            int subTitleHeight = WidgetUtils.PopUpOrFloatingFormSubTitle();
            int subTitleMargin = subTitleHeight / 5;
            int tableHeight = 0;
            int previousRowIndex = -1;
            int cntentWidth = (int) (WidgetUtils.MainSize.Width * .55);
            int tableWidth = cntentWidth - contentPadding.Size.Width;
            int contentPieceWidth = tableWidth / _tablePanel.ColumnCount - boxMargin * 2;
            foreach (Control control in _tablePanel.Controls) {
                if (control.Visible) {
                    int currentRowIndex = _tablePanel.GetPositionFromControl(control).Row;
                    if (currentRowIndex != previousRowIndex) {
                        previousRowIndex = currentRowIndex;
                        if (control is TitlePanel titlePanel) {
                            tableHeight += subTitleHeight + subTitleMargin * 2;
                        } else if (control is SubPanel<ProductBoltDTO> subPanel) {
                            subPanel.ResizeSelf(tableWidth);
                            tableHeight += subPanel.Height;
                        } else if (control is PictureBoxGroup pictureBox) {
                            pictureBox.SetSize(contentPieceWidth, boxHeight, WidgetUtils.PictureBoxGroupBaseHeight(), 1, contentPieceWidth + boxMargin * 2);
                            pictureBox.Margin = new(boxMargin);
                            tableHeight += pictureBox.Height + subTitleMargin * 2;
                        } else {
                            tableHeight += boxHeight + boxMargin * 2;
                        }
                    }
                }
            }
            Size contentSize = new(cntentWidth, tableHeight + contentPadding.Size.Height);
            _tablePanel.Size = new(tableWidth, tableHeight);
            foreach (Control control in _tablePanel.Controls) {
                if (control is TitlePanel titlePanel) {
                    titlePanel.Margin = new(0, boxMargin, 0, boxMargin);
                    titlePanel.Size = new(_tablePanel.Width, subTitleHeight);
                } else if (control is SubPanel<ProductBoltDTO> subPanel) {
                    continue;
                } else if (control is PictureBoxGroup pictureBox) {
                    continue;
                } else {
                    control.Margin = new(boxMargin);
                    control.Size = new(contentPieceWidth, boxHeight);
                }
            }

            SetContentSizeAndSelfSize(contentSize);
        }

    }
}
