using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.ComboBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;
using CustomLibrary.TextBoxes;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BoltEditionPopUpForm: BoltPopUpForm {
        private bool _modified;

        private List<WorkstationDTO> _workstationsDTOs;
        private CustomComboBoxGroup<WorkstationDTO> _workstation;
        private CustomTextBoxGroup _position;
        private string _retrievePositionButtonLabel = "读取坐标";
        private string _retrievePositionButtonLabelLoading = "正在读取";
        private CommonButton _retrieveCoordinatesBtn;

        public bool Modified => _modified;
        protected CustomTextBoxGroup Position { get => _position; set => _position = value; }

        public BoltEditionPopUpForm(ProductBoltDTO boltDTO) : base(boltDTO) {
            _modified = false;
            
            NameBox.Enabled = true;
            SpecificationBox.Enabled = true;
            BitSpecificationBox.Enabled = true;
            ProcedureSetBox.Enabled = true;
            TorqueBox.Enabled = true;
            AngleBox.Enabled = true;
            // 站点
            _workstation = new("站点") {
                Parent = TablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 7,
                NameAlignment = HorizontalAlignment.Right,
            };
            QueryWorkstationListRsp queryWorkstationListRsp = apis.QueryWorkstationList(new());
            _workstationsDTOs = queryWorkstationListRsp.WorkstationsDTOs;
            foreach (WorkstationDTO dto in _workstationsDTOs) {
                _workstation.AddItem(CommonUtils.CannotBeNull(dto.name), dto);
            }
            // 点位坐标
            _position = new("点位坐标") {
                Parent = TablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 7,
                Separator = ",",
                NameAlignment = HorizontalAlignment.Right,
                NumberOnly = true,
            };
            _position.GetTextBox(0);
            _position.AddTextBox();
            _position.AddTextBox();
            // 获取坐标按钮
            _retrieveCoordinatesBtn = new() {
                Parent = TablePanel,
                Label = _retrievePositionButtonLabel,
            };
            TablePanel.SetColumnSpan(_retrieveCoordinatesBtn, 2);
            _retrieveCoordinatesBtn.SizeChanged += (sender, eventArgs) => {
                _retrieveCoordinatesBtn.Margin = new(TablePanel.Width - _retrieveCoordinatesBtn.Width - TablePanel.Padding.Right - BoxMargin, BoxMargin, 0, 0);
            };
            _retrieveCoordinatesBtn.Click += async (sender, eventArgs) => {
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
                        _retrieveCoordinatesBtn.Label = _retrievePositionButtonLabelLoading + dotStr;
                    } else {
                        timer.Stop();
                    }
                    _retrieveCoordinatesBtn.Invalidate();
                };
                _retrieveCoordinatesBtn.Enabled = false;
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
                    
                    labelChanging = false;
                    if (coordinates != null) {
                        _position.SetValue(0, coordinates.X + "");
                        _position.SetValue(1, coordinates.Y + "");
                        _position.SetValue(2, coordinates.Z + "");
                        WidgetUtils.ShowNoticePopUp("读取成功！");
                    } else {
                        WidgetUtils.ShowWarningPopUp("读取失败，可能原因：\r\n1. 当前站点不存在\r\n2. 当前站点没有配置力臂\r\n3. 没有连接至指定力臂");
                    }
                }
                _retrieveCoordinatesBtn.Enabled = true;
                _retrieveCoordinatesBtn.Label = _retrievePositionButtonLabel;
                _retrieveCoordinatesBtn.Invalidate();
            };

            // 点位描述
            SetValue<string>(NameBox, 0, val => ModifiedBoltDTO.name = val);
            // 螺栓规格
            SetValue<float>(SpecificationBox, 0, val => ModifiedBoltDTO.specification = val);
            // 批头规格
            SetValue<float>(BitSpecificationBox, 0, val => ModifiedBoltDTO.bit_specification = val);
            // pset 程序号
            SetValue<int>(ProcedureSetBox, 0, val => ModifiedBoltDTO.parameters_set = val);
            // 扭矩上下限
            SetValue<float>(TorqueBox, 0, val => ModifiedBoltDTO.torque_min = val);
            SetValue<float>(TorqueBox, 1, val => ModifiedBoltDTO.torque_max = val);
            // 角度上下限
            SetValue<float>(AngleBox, 0, val => ModifiedBoltDTO.angle_min = val);
            SetValue<float>(AngleBox, 1, val => ModifiedBoltDTO.angle_max = val);
            // 站点选择
            _workstation.ItemSelected += () => {
                if (!_workstation.IsDefaultValue() && _workstation.Value != null) {
                    ModifiedBoltDTO.workstation_id = _workstation.Value.id;
                } else {
                    ModifiedBoltDTO.workstation_id = null;
                }
            };
            // 螺栓坐标
            SetValue<int>(_position, 0, val => {
                Coordinates3D position = Coordinates3D.FromString(ModifiedBoltDTO.position);
                position.X = val;
                ModifiedBoltDTO.position = position.ToString();
            });
            SetValue<int>(_position, 1, val => {
                Coordinates3D position = Coordinates3D.FromString(ModifiedBoltDTO.position);
                position.Y = val;
                ModifiedBoltDTO.position = position.ToString();
            });
            SetValue<int>(_position, 2, val => {
                Coordinates3D position = Coordinates3D.FromString(ModifiedBoltDTO.position);
                position.Z = val;
                ModifiedBoltDTO.position = position.ToString();
            });

            // 检查是否DTO中已经有值，有的话则回填
            if (boltDTO.workstation_id != null) {
                WorkstationDTO? workstationDTO = _workstationsDTOs.SingleOrDefault(dto => dto.id == boltDTO.workstation_id.Value);
                if (workstationDTO != null) {
                    _workstation.SetCurrent(_workstation.IndexOf(workstationDTO));
                }
            }
            Coordinates3D position = Coordinates3D.FromString(boltDTO.position);
            _position.SetValue(0, position.X + "");
            _position.SetValue(1, position.Y + "");
            _position.SetValue(2, position.Z + "");
        }

        private void SetValue<V>(CustomTextBoxGroup boxGroup, int index, Action<V?> action1) {
            CustomTextBox box = boxGroup.GetTextBox(index);
            box.Box.TextChanged += (sender, eventArgs) => {
                if (!boxGroup.HasError) {
                    if (box.Text != "" && box.Text != string.Empty) {
                        action1((V) Convert.ChangeType(box.Text, typeof(V)));
                    } else {
                        action1(default(V));
                    }
                }
            };
        }

        public bool ConfirmSave() {
            return NameBox.HasError 
                && SpecificationBox.HasError
                && BitSpecificationBox.HasError
                && ProcedureSetBox.HasError
                && TorqueBox.HasError
                && AngleBox.HasError
                && _position.HasError;
        }
    }
}
