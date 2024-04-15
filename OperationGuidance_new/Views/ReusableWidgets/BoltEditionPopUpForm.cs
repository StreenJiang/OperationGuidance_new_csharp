using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using CustomLibrary.TextBoxes;
using OperationGuidance_service.Controllers;
using CustomLibrary.ComboBoxes;
using OperationGuidance_service.Models.Responses;
using CustomLibrary.Buttons;
using CustomLibrary.Utils;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Utils;
using CustomLibrary.Forms;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BoltEditionPopUpForm: CustomPopUpForm {
        private readonly int _columnCount = 1;
        protected OperationGuidanceApis apis;
        private ProductBoltDTO _originalBoltDTO;
        private ProductBoltDTO _modifiedBoltDTO;

        private int _boxHeight;
        private int _boxMargin;
        private int _buttonHeight;
        private TableLayoutPanel _tablePanel;

        private CustomTextBoxGroup _serialNumBox;
        private CustomTextBoxGroup _nameBox;
        private List<WorkstationDTO> _workstationsDTOs;
        private CustomComboBoxGroup<WorkstationDTO> _workstation;
        private SubPanel<ProductBoltDTO> _positionSubPanel;
        private ToggleButton _positionToggle;
        private CustomTextBoxButtonGroup _positionBox;
        private readonly string _retrievePositionButtonLabel = "读取坐标";
        private readonly string _retrievePositionButtonLabelLoading = "正在读取";
        private SubPanel<ProductBoltDTO> _parameterSetSubPanel;
        private ToggleButton _parameterSetToggle;
        private CustomTextBoxGroup _parameterSetBox;
        private SubPanel<ProductBoltDTO> _specificationSubPanel;
        private ToggleButton _specificationToggle;
        private CustomTextBoxGroup _specificationBox;
        private SubPanel<ProductBoltDTO> _bitSpecificationSubPanel;
        private ToggleButton _bitSpecificationToggle;
        private CustomTextBoxGroup _bitSpecificationBox;
        private SubPanel<ProductBoltDTO> _torqueSubPanel;
        private ToggleButton _torqueToggle;
        private CustomTextBoxGroup _torqueBox;
        private SubPanel<ProductBoltDTO> _angleSubPanel;
        private ToggleButton _angleToggle;
        private CustomTextBoxGroup _angleBox;

        public ProductBoltDTO OriginalBoltDTO { get => _originalBoltDTO; set => _originalBoltDTO = value; }
        public ProductBoltDTO ModifiedBoltDTO { get => _modifiedBoltDTO; set => _modifiedBoltDTO = value; }

        public int BoxHeight { get => _boxHeight; set => _boxHeight = value; }
        public int BoxMargin { get => _boxMargin; set => _boxMargin = value; }
        public int ButtonHeight { get => _buttonHeight; set => _buttonHeight = value; }
        public TableLayoutPanel TablePanel { get => _tablePanel; set => _tablePanel = value; }

        public CustomTextBoxGroup SerialNumBox { get => _serialNumBox; set => _serialNumBox = value; }
        public CustomTextBoxGroup NameBox { get => _nameBox; set => _nameBox = value; }
        public List<WorkstationDTO> WorkstationsDTOs { get => _workstationsDTOs; set => _workstationsDTOs = value; }
        public CustomComboBoxGroup<WorkstationDTO> Workstation { get => _workstation; set => _workstation = value; }
        public CustomTextBoxButtonGroup PositionBox { get => _positionBox; set => _positionBox = value; }
        public CustomTextBoxGroup ParameterSetBox { get => _parameterSetBox; set => _parameterSetBox = value; }
        public CustomTextBoxGroup SpecificationBox { get => _specificationBox; set => _specificationBox = value; }
        public CustomTextBoxGroup BitSpecificationBox { get => _bitSpecificationBox; set => _bitSpecificationBox = value; }
        public CustomTextBoxGroup TorqueBox { get => _torqueBox; set => _torqueBox = value; }
        public CustomTextBoxGroup AngleBox { get => _angleBox; set => _angleBox = value; }
        public SubPanel<ProductBoltDTO> PositionSubPanel { get => _positionSubPanel; set => _positionSubPanel = value; }
        public SubPanel<ProductBoltDTO> ParameterSetSubPanel { get => _parameterSetSubPanel; set => _parameterSetSubPanel = value; }
        public SubPanel<ProductBoltDTO> SpecificationSubPanel { get => _specificationSubPanel; set => _specificationSubPanel = value; }
        public SubPanel<ProductBoltDTO> BitSpecificationSubPanel { get => _bitSpecificationSubPanel; set => _bitSpecificationSubPanel = value; }
        public SubPanel<ProductBoltDTO> TorqueSubPanel { get => _torqueSubPanel; set => _torqueSubPanel = value; }
        public SubPanel<ProductBoltDTO> AngleSubPanel { get => _angleSubPanel; set => _angleSubPanel = value; }
        public ToggleButton PositionToggle { get => _positionToggle; set => _positionToggle = value; }
        public ToggleButton ParameterSetToggle { get => _parameterSetToggle; set => _parameterSetToggle = value; }
        public ToggleButton SpecificationToggle { get => _specificationToggle; set => _specificationToggle = value; }
        public ToggleButton BitSpecificationToggle { get => _bitSpecificationToggle; set => _bitSpecificationToggle = value; }
        public ToggleButton TorqueToggle { get => _torqueToggle; set => _torqueToggle = value; }
        public ToggleButton AngleToggle { get => _angleToggle; set => _angleToggle = value; }

        public BoltEditionPopUpForm(ProductBoltDTO boltDTO) {
            apis = SystemUtils.GetApis();
            _originalBoltDTO = boltDTO;
            _modifiedBoltDTO = boltDTO.Clone<ProductBoltDTO>();

            // 添加文本框显示信息
            _tablePanel = new() {
                Parent = ContentPanel,
                Margin = new(0),
                Padding = new(0),
                ColumnCount = _columnCount,
            };
            _serialNumBox = new("点位编号") {
                Parent = _tablePanel,
                Ratio = 8.25,
                NameAlignment = HorizontalAlignment.Right,
                PositiveIntOnly = true,
            };
            _nameBox = new("点位名称") {
                Parent = _tablePanel,
                Ratio = 8.25,
                NameAlignment = HorizontalAlignment.Right,
            };
            // 站点
            _workstation = new("站点") {
                Parent = TablePanel,
                Ratio = 8.25,
                NameAlignment = HorizontalAlignment.Right,
            };
            QueryWorkstationListRsp queryWorkstationListRsp = apis.QueryWorkstationList(new(SystemUtils.MacAddressesDTO.id));
            _workstationsDTOs = queryWorkstationListRsp.WorkstationsDTOs;
            foreach (WorkstationDTO dto in _workstationsDTOs) {
                _workstation.AddItem(CommonUtils.CannotBeNull(dto.name), dto);
            }
            _positionSubPanel = AddSubPanel("点位坐标");
            _positionToggle = _positionSubPanel.TitlePanel.AddRightButton<ToggleButton>();
            _positionToggle.Checked = boltDTO.position != null;
            // 点位坐标
            _positionBox = new("点位坐标") {
                Parent = _positionSubPanel.TablePanel,
                Ratio = 8.25,
                Separator = ",",
                NameAlignment = HorizontalAlignment.Right,
                IntOnly = true,
            };
            _positionSubPanel.TablePanel.SetColumnSpan(_positionBox, _columnCount);
            _positionBox.GetTextBox(0);
            _positionBox.AddTextBox();
            _positionBox.AddTextBox();
            CommonButton retrieveCoordinatesBtn = _positionBox.AddButton(_retrievePositionButtonLabel);
            retrieveCoordinatesBtn.Click += async (sender, eventArgs) => {
                bool labelChanging = true;
                string dotStr = "";
                using System.Windows.Forms.Timer timer = new();
                timer.Interval = 350;
                timer.Tick += (s, e) => {
                    if (labelChanging) {
                        if (dotStr.Length >= 3) {
                            dotStr = ".";
                        } else {
                            dotStr += ".";
                        }
                        retrieveCoordinatesBtn.Label = _retrievePositionButtonLabelLoading + dotStr;
                    } else {
                        timer.Stop();
                    }
                    retrieveCoordinatesBtn.Invalidate();
                };
                retrieveCoordinatesBtn.Enabled = false;
                timer.Start();
                WorkstationDTO? dto = _workstation.Value;
                if (dto == null || _workstation.IsDefaultValue()) {
                    WidgetUtils.ShowErrorPopUp("请先选择站点再尝试获取力臂坐标");
                    labelChanging = false;
                } else {
                    Coordinates3D? coordinates = null;
                    if (dto.arm_id != null) {
                        ArmTask? armTask = MainUtils.TryGetArmTask(dto.arm_id.Value);
                        if (armTask != null) {
                            coordinates = await armTask.GetCurrentCoordinates();
                        }
                    }

                    if (coordinates != null) {
                        _positionBox.SetValue(0, coordinates.X + "");
                        _positionBox.SetValue(1, coordinates.Y + "");
                        _positionBox.SetValue(2, coordinates.Z + "");
                        WidgetUtils.ShowNoticePopUp("读取成功！");
                    } else {
                        WidgetUtils.ShowWarningPopUp("读取失败，可能原因：\r\n1. 当前站点不存在\r\n2. 当前站点没有配置力臂\r\n3. 没有连接至指定力臂");
                    }
                    labelChanging = false;
                }
                retrieveCoordinatesBtn.Enabled = true;
                retrieveCoordinatesBtn.Label = _retrievePositionButtonLabel;
                retrieveCoordinatesBtn.Invalidate();
            };
            _positionToggle.CheckedChanged += (sender, eventArgs) => {
                if (_positionToggle.Checked) {
                    _positionSubPanel.TablePanel.Show();
                    if (string.IsNullOrEmpty(_positionBox.GetTextBox(0).Box.Text)) {
                        _positionBox.SetValue(0, "0");
                    }
                    if (string.IsNullOrEmpty(_positionBox.GetTextBox(1).Box.Text)) {
                        _positionBox.SetValue(1, "0");
                    }
                    if (string.IsNullOrEmpty(_positionBox.GetTextBox(2).Box.Text)) {
                        _positionBox.SetValue(2, "0");
                    }
                } else {
                    _positionSubPanel.TablePanel.Hide();
                    _positionBox.SetValue(0, "0");
                    _positionBox.SetValue(1, "0");
                    _positionBox.SetValue(2, "0");
                }
                ResizeSelf();
                _positionBox.ResizeChildren();
            };
            // 程序号 pset
            _parameterSetSubPanel = AddSubPanel("程序号");
            _parameterSetToggle = _parameterSetSubPanel.TitlePanel.AddRightButton<ToggleButton>();
            _parameterSetToggle.Checked = boltDTO.parameters_set != null;
            _parameterSetBox = new("程序号") {
                Parent = _parameterSetSubPanel.TablePanel,
                Ratio = 8.25,
                NameAlignment = HorizontalAlignment.Right,
                PositiveIntOnly = true,
            };
            _parameterSetSubPanel.TablePanel.SetColumnSpan(_parameterSetBox, _columnCount);
            _parameterSetToggle.CheckedChanged += (sender, eventArgs) => {
                if (_parameterSetToggle.Checked) {
                    _parameterSetSubPanel.TablePanel.Show();
                    if (string.IsNullOrEmpty(_parameterSetBox.GetTextBox(0).Box.Text)) {
                        _parameterSetBox.SetValue(0, "1");
                    }
                } else {
                    _parameterSetSubPanel.TablePanel.Hide();
                    _parameterSetBox.SetValue(0, null);
                }
                ResizeSelf();
                _parameterSetBox.ResizeChildren();
            };
            // 螺栓规格
            _specificationSubPanel = AddSubPanel("螺栓规格");
            _specificationToggle = _specificationSubPanel.TitlePanel.AddRightButton<ToggleButton>();
            _specificationToggle.Checked = boltDTO.specification != null;
            _specificationBox = new("螺栓规格") {
                Parent = _specificationSubPanel.TablePanel,
                Ratio = 8.25,
                NameAlignment = HorizontalAlignment.Right,
                PositiveIntOnly = true,
            };
            _specificationSubPanel.TablePanel.SetColumnSpan(_specificationBox, _columnCount);
            _specificationToggle.CheckedChanged += (sender, eventArgs) => {
                if (_specificationToggle.Checked) {
                    _specificationSubPanel.TablePanel.Show();
                    if (string.IsNullOrEmpty(_specificationBox.GetTextBox(0).Box.Text)) {
                        _specificationBox.SetValue(0, "0");
                    }
                } else {
                    _specificationSubPanel.TablePanel.Hide();
                    _specificationBox.SetValue(0, null);
                }
                ResizeSelf();
                _specificationBox.ResizeChildren();
            };
            // 套筒位数
            _bitSpecificationSubPanel = AddSubPanel("套筒位数");
            _bitSpecificationToggle = _bitSpecificationSubPanel.TitlePanel.AddRightButton<ToggleButton>();
            _bitSpecificationToggle.Checked = boltDTO.bit_specification != null;
            _bitSpecificationBox = new("套筒位数") {
                Parent = _bitSpecificationSubPanel.TablePanel,
                Ratio = 8.25,
                NameAlignment = HorizontalAlignment.Right,
                PositiveIntOnly = true,
            };
            _bitSpecificationSubPanel.TablePanel.SetColumnSpan(_bitSpecificationBox, _columnCount);
            _bitSpecificationToggle.CheckedChanged += (sender, eventArgs) => {
                if (_bitSpecificationToggle.Checked) {
                    _bitSpecificationSubPanel.TablePanel.Show();
                    if (string.IsNullOrEmpty(_bitSpecificationBox.GetTextBox(0).Box.Text)) {
                        _bitSpecificationBox.SetValue(0, "0");
                    }
                } else {
                    _bitSpecificationSubPanel.TablePanel.Hide();
                    _bitSpecificationBox.SetValue(0, null);
                }
                ResizeSelf();
                _bitSpecificationBox.ResizeChildren();
            };
            // 扭矩范围
            _torqueSubPanel = AddSubPanel("扭矩范围");
            _torqueToggle = _torqueSubPanel.TitlePanel.AddRightButton<ToggleButton>();
            _torqueToggle.Checked = boltDTO.torque_min != null && boltDTO.torque_max != null;
            _torqueBox = new("扭矩范围") {
                Parent = _torqueSubPanel.TablePanel,
                Ratio = 8.25,
                NameAlignment = HorizontalAlignment.Right,
                NumberOnly = true,
            };
            _torqueBox.Separator = "~";
            _torqueBox.GetTextBox(0);
            _torqueBox.AddTextBox();
            _torqueSubPanel.TablePanel.SetColumnSpan(_torqueBox, _columnCount);
            _torqueToggle.CheckedChanged += (sender, eventArgs) => {
                if (_torqueToggle.Checked) {
                    _torqueSubPanel.TablePanel.Show();
                    if (string.IsNullOrEmpty(_torqueBox.GetTextBox(0).Box.Text)) {
                        _torqueBox.SetValue(0, "0");
                    }
                    if (string.IsNullOrEmpty(_torqueBox.GetTextBox(1).Box.Text)) {
                        _torqueBox.SetValue(1, "0");
                    }
                } else {
                    _torqueSubPanel.TablePanel.Hide();
                    _torqueBox.SetValue(0, null);
                    _torqueBox.SetValue(1, null);
                }
                ResizeSelf();
                _torqueBox.ResizeChildren();
            };
            // 角度范围
            _angleSubPanel = AddSubPanel("角度范围");
            _angleToggle = _angleSubPanel.TitlePanel.AddRightButton<ToggleButton>();
            _angleToggle.Checked = boltDTO.angle_min != null && boltDTO.angle_max != null;
            _angleBox = new("角度范围") {
                Parent = _angleSubPanel.TablePanel,
                Ratio = 8.25,
                NameAlignment = HorizontalAlignment.Right,
                NumberOnly = true,
            };
            _angleBox.Separator = "~";
            _angleBox.GetTextBox(0);
            _angleBox.AddTextBox();
            _angleSubPanel.TablePanel.SetColumnSpan(_angleBox, _columnCount);
            _angleToggle.CheckedChanged += (sender, eventArgs) => {
                if (_angleToggle.Checked) {
                    _angleSubPanel.TablePanel.Show();
                    if (string.IsNullOrEmpty(_angleBox.GetTextBox(0).Box.Text)) {
                        _angleBox.SetValue(0, "0");
                    }
                    if (string.IsNullOrEmpty(_angleBox.GetTextBox(1).Box.Text)) {
                        _angleBox.SetValue(1, "0");
                    }
                } else {
                    _angleSubPanel.TablePanel.Hide();
                    _angleBox.SetValue(0, null);
                    _angleBox.SetValue(1, null);
                }
                ResizeSelf();
                _angleBox.ResizeChildren();
            };

            // 检查是否DTO中已经有值，有的话则回填
            _serialNumBox.SetValue(0, boltDTO.serial_num + "");
            _nameBox.SetValue(0, boltDTO.name);
            WorkstationDTO? workstationDTO = WorkstationsDTOs.SingleOrDefault(dto => dto.id == boltDTO.workstation_id);
            if (workstationDTO != null) {
                Workstation.SetCurrent(Workstation.IndexOf(workstationDTO));
            }
            if (boltDTO.position != null) {
                _positionSubPanel.TablePanel.Show();
                Coordinates3D position = Coordinates3D.FromString(boltDTO.position);
                PositionBox.SetValue(0, position.X + "");
                PositionBox.SetValue(1, position.Y + "");
                PositionBox.SetValue(2, position.Z + "");
            } else {
                _positionSubPanel.TablePanel.Hide();
            }
            if (boltDTO.parameters_set != null) {
                _parameterSetSubPanel.TablePanel.Show();
                _parameterSetBox.SetValue(0, boltDTO.parameters_set + "");
            } else {
                _parameterSetSubPanel.TablePanel.Hide();
            }
            if (boltDTO.specification != null) {
                _specificationSubPanel.TablePanel.Show();
                _specificationBox.SetValue(0, boltDTO.specification + "");
            } else {
                _specificationSubPanel.TablePanel.Hide();
            }
            if (boltDTO.bit_specification != null) {
                _bitSpecificationSubPanel.TablePanel.Show();
                _bitSpecificationBox.SetValue(0, boltDTO.bit_specification + "");
            } else {
                _bitSpecificationSubPanel.TablePanel.Hide();
            }
            if (boltDTO.torque_min != null && boltDTO.torque_max != null) {
                _torqueSubPanel.TablePanel.Show();
                _torqueBox.SetValue(0, boltDTO.torque_min + "");
                _torqueBox.SetValue(1, boltDTO.torque_max + "");
            } else {
                _torqueSubPanel.TablePanel.Hide();
            }
            if (boltDTO.angle_min != null && boltDTO.angle_max != null) {
                _angleSubPanel.TablePanel.Show();
                _angleBox.SetValue(0, boltDTO.angle_min + "");
                _angleBox.SetValue(1, boltDTO.angle_max + "");
            } else {
                _angleSubPanel.TablePanel.Hide();
            }

            // 点位编号
            TextBox serialNumBox = SerialNumBox.GetTextBox(0).Box;
            SerialNumBox.GetTextBox(0).TextChanged += (sender, eventArgs) => {
                if (!SerialNumBox.HasError)
                    if (!string.IsNullOrEmpty(serialNumBox.Text)) ModifiedBoltDTO.serial_num = int.Parse(serialNumBox.Text);
                    else ModifiedBoltDTO.serial_num = 0;
            };
            // 点位名称
            TextBox nameBox = NameBox.GetTextBox(0).Box;
            NameBox.GetTextBox(0).TextChanged += (sender, eventArgs) => {
                if (!NameBox.HasError)
                    if (!string.IsNullOrEmpty(nameBox.Text)) ModifiedBoltDTO.name = nameBox.Text;
                    else ModifiedBoltDTO.name = null;
            };
            // 站点选择
            Workstation.ItemSelected += () => {
                if (!Workstation.IsDefaultValue() && Workstation.Value != null) {
                    ModifiedBoltDTO.workstation_id = Workstation.Value.id;
                    Workstation.SetError(false);
                } else {
                    Workstation.SetError(true);
                }
            };
            // 螺栓点位
            TextBox positionXBox = PositionBox.GetTextBox(0).Box;
            PositionBox.GetTextBox(0).TextChanged += (sender, eventArgs) => {
                if (!PositionBox.HasError) {
                    Coordinates3D position = Coordinates3D.FromString(ModifiedBoltDTO.position);
                    position.X = string.IsNullOrEmpty(positionXBox.Text) ? 0 : int.Parse(positionXBox.Text);
                    ModifiedBoltDTO.position = position.ToString();
                }
            };
            TextBox positionYBox = PositionBox.GetTextBox(1).Box;
            PositionBox.GetTextBox(1).TextChanged += (sender, eventArgs) => {
                if (!PositionBox.HasError) {
                    Coordinates3D position = Coordinates3D.FromString(ModifiedBoltDTO.position);
                    position.Y = string.IsNullOrEmpty(positionYBox.Text) ? 0 : int.Parse(positionYBox.Text);
                    ModifiedBoltDTO.position = position.ToString();
                }
            };
            TextBox positionZBox = PositionBox.GetTextBox(2).Box;
            PositionBox.GetTextBox(2).TextChanged += (sender, eventArgs) => {
                if (!PositionBox.HasError) {
                    Coordinates3D position = Coordinates3D.FromString(ModifiedBoltDTO.position);
                    position.Z = string.IsNullOrEmpty(positionZBox.Text) ? 0 : int.Parse(positionZBox.Text);
                    ModifiedBoltDTO.position = position.ToString();
                }
            };
            // pset 程序号
            TextBox parametersBox = ParameterSetBox.GetTextBox(0).Box;
            ParameterSetBox.GetTextBox(0).TextChanged += (sender, eventArgs) => {
                if (!ParameterSetBox.HasError)
                    if (!string.IsNullOrEmpty(parametersBox.Text)) ModifiedBoltDTO.parameters_set = int.Parse(parametersBox.Text);
                    else ModifiedBoltDTO.parameters_set = null;
            };
            // 螺栓规格
            TextBox specificationBox = SpecificationBox.GetTextBox(0).Box;
            SpecificationBox.GetTextBox(0).TextChanged += (sender, eventArgs) => {
                if (!SpecificationBox.HasError)
                    if (!string.IsNullOrEmpty(specificationBox.Text)) ModifiedBoltDTO.specification = float.Parse(specificationBox.Text);
                    else ModifiedBoltDTO.specification = null;
            };
            // 批头规格
            TextBox bitSpecificationBox = BitSpecificationBox.GetTextBox(0).Box;
            BitSpecificationBox.GetTextBox(0).TextChanged += (sender, eventArgs) => {
                if (!BitSpecificationBox.HasError)
                    if (!string.IsNullOrEmpty(bitSpecificationBox.Text)) ModifiedBoltDTO.bit_specification = float.Parse(bitSpecificationBox.Text);
                    else ModifiedBoltDTO.bit_specification = null;
            };
            // 扭矩上下限
            TextBox torqueMinBox = TorqueBox.GetTextBox(0).Box;
            TorqueBox.GetTextBox(0).TextChanged += (sender, eventArgs) => {
                if (!TorqueBox.HasError)
                    if (!string.IsNullOrEmpty(torqueMinBox.Text)) ModifiedBoltDTO.torque_min = float.Parse(torqueMinBox.Text);
                    else ModifiedBoltDTO.torque_min = null;
            };
            TextBox torqueMaxBox = TorqueBox.GetTextBox(1).Box;
            TorqueBox.GetTextBox(1).TextChanged += (sender, eventArgs) => {
                if (!TorqueBox.HasError)
                    if (!string.IsNullOrEmpty(torqueMaxBox.Text)) ModifiedBoltDTO.torque_max = float.Parse(torqueMaxBox.Text);
                    else ModifiedBoltDTO.torque_max = null;
            };
            // 角度上下限
            TextBox angleMinBox = AngleBox.GetTextBox(0).Box;
            AngleBox.GetTextBox(0).TextChanged += (sender, eventArgs) => {
                if (!AngleBox.HasError)
                    if (!string.IsNullOrEmpty(angleMinBox.Text)) ModifiedBoltDTO.angle_min = float.Parse(angleMinBox.Text);
                    else ModifiedBoltDTO.angle_min = null;
            };
            TextBox angleMaxBox = AngleBox.GetTextBox(1).Box;
            AngleBox.GetTextBox(1).TextChanged += (sender, eventArgs) => {
                if (!AngleBox.HasError)
                    if (!string.IsNullOrEmpty(angleMaxBox.Text)) ModifiedBoltDTO.angle_max = float.Parse(angleMaxBox.Text);
                    else ModifiedBoltDTO.angle_max = null;
            };
        }

        public SubPanel<ProductBoltDTO> AddSubPanel(string title) {
            SubPanel<ProductBoltDTO> subPanel = new(_modifiedBoltDTO, title, _columnCount);
            _tablePanel.Controls.Add(subPanel);
            _tablePanel.SetColumnSpan(subPanel, _columnCount);
            return subPanel;
        }

        public void SaveTo(ProductBoltDTO dto) {
            CommonUtils.ObjectConverter<ProductBoltDTO, ProductBoltDTO>(_modifiedBoltDTO, _originalBoltDTO);
            CommonUtils.ObjectConverter<ProductBoltDTO, ProductBoltDTO>(_modifiedBoltDTO, dto);
        }

        public void ResizeSelf() {
            ResizeTablePanelAndItsChildren();
            Invalidate();
        }

        public void ResizeTablePanelAndItsChildren() {
            CalculateDetailProperties();

            Padding contentPadding = ContentPanel.Padding;
            int boxHeight = WidgetUtils.TextOrComboBoxHeight();
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
