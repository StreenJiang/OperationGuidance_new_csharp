using System.IO.Ports;
using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.ComboBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Constants;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;
using CustomLibrary.TextBoxes;

namespace OperationGuidance_new.Views {
    public class DeviceSerialPortView: CustomDataGridViewOuterPanel<DeviceSerialPortDTO, ViewObjects.DeviceSerialPortVO> {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        private List<DeviceSerialPortDTO> _dataDTOList;
        // DataGridView panel
        private DataGridViewGroup<DeviceSerialPortVO> _dataGridView;
        // Add new pop up form
        private EditEntityPopUpForm<DeviceSerialPortDTO> _editEntityPopUpForm;
        #endregion

        #region Constructors
        public DeviceSerialPortView() {
            // Default values
            FlowDirection = FlowDirection.TopDown;
            
            // Get Apis
            apis = SystemUtils.GetApis();

            // Initialization
            InitializeGridView();
        }
        #endregion

        #region Initialize methods
        private void InitializeGridView() {
            _dataGridView = new() {
                Parent = this,
            };
            _dataGridView.AddTextBox("设备名称", false, (DeviceSerialPortVO vo, string? value) => vo.name = value);
            _dataGridView.AddTextBox("设备描述", false, (DeviceSerialPortVO vo, string? value) => vo.description = value);
            CustomComboBoxGroup<int?> toolTypeComboBox = _dataGridView.AddComboBox("设备类型", (DeviceSerialPortVO vo, int? value) => vo.type = value, new());
            foreach (DeviceTypeBase type in DeviceType_SerialPort.Elements) {
                toolTypeComboBox.AddItem(type.Name, type.Id);
            }
            
            // 按钮逻辑
            _dataGridView.QueryData = (vo) => {
                List<DeviceSerialPortVO> workstationVOs = QueryList();
                return workstationVOs
                    .Where(o => vo.name == null || o.name != null && o.name.Contains(vo.name))
                    .Where(o => vo.description == null || o.description != null && o.description.Contains(vo.description))
                    .Where(o => vo.type == null || vo.type.Value == 0 || o.type != null && o.type == vo.type)
                    .ToList();
            };
            _dataGridView.AddNewClick = (action) => {
                DeviceSerialPortDTO dto = new();
                OpenEditEntityPopUpForm("新增设备", dto, action);
            };
            _dataGridView.ModifyClick = (ids, action) => {
                if (ids.Count <= 0) {
                    WidgetUtils.ShowNoticePopUp("请选择要编辑的数据。");
                } else if (ids.Count > 1) {
                    WidgetUtils.ShowNoticePopUp("只能选择一条数据进行修改操作。");
                } else {
                    if (_dataDTOList.Count > 0) {
                        DeviceSerialPortDTO dto = _dataDTOList.Single(dto => dto.id == ids[0]);
                        OpenEditEntityPopUpForm("更新设备信息", dto, action);
                    }
                }
            };
            _dataGridView.DeleteClick = (ids, action) => {
                // 删除选择的数据
                Delete(ids);
                // 删除后再触发一次查询操作
                action();
            };
        }
        #endregion

        #region Reusable methods
        private void OpenEditEntityPopUpForm(string title, DeviceSerialPortDTO dto, Action callBackAction) {
            _editEntityPopUpForm = new(dto) {
                Title = title,
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            // 添加字段
            // 设备名称
            CustomTextBoxGroup brandName = _editEntityPopUpForm.AddTextBox("设备名称", false, 
                (DeviceSerialPortDTO dto, string? value) => dto.name = value ?? "");
            brandName.Ratio = 6;
            if (dto.name != null) {
                brandName.SetValue(0, dto.name);
            }
            // 设备描述
            CustomTextBoxGroup description = _editEntityPopUpForm.AddTextBox("设备描述", false, 
                (DeviceSerialPortDTO dto, string? value) => dto.description = value ?? "");
            description.Ratio = 6;
            if (dto.description != null) {
                description.SetValue(0, dto.description);
            }
            // 串口号
            Dictionary<string, string> portNames = new();
            Dictionary<string, string> portNamesDict = ConnectionUtils.GetSerialPorts();
            foreach (KeyValuePair<string, string> pair in portNamesDict) {
                portNames.Add(pair.Value, pair.Key);
            }
            CustomComboBoxGroup<string>? portFullName = null;
            portFullName = _editEntityPopUpForm.AddComboBox("串口", SetPortName, portNames);
            void SetPortName(DeviceSerialPortDTO dto, string? value) {
                if (portFullName != null) {
                    dto.port_full_name = value == null ? "" : portFullName.Key;
                }
            }
            portFullName.Ratio = 6;
            // 串口全名
            CustomTextBoxGroup portName = _editEntityPopUpForm.AddTextBox("串口号", false, 
                (DeviceSerialPortDTO dto, string? value) => dto.port_name = value ?? "");
            portName.Ratio = 6;
            portName.Enabled = false;
            if (dto.port_name != null) {
                portFullName.SetCurrent(portFullName.IndexOf(dto.port_name));
                portName.SetValue(0, dto.port_name);
            }
            portFullName.ItemSelected += () => {
                if (!portFullName.IsDefaultValue() && portFullName.Value != null) {
                    portName.SetValue(0, portFullName.Value);
                    if (portFullName.IsError) {
                        portFullName.SetError(false);
                    }
                } else {
                    portName.SetValue(0, "");
                }
            };
            // 设备类型
            Dictionary<string, int> toolTypes = DeviceType_SerialPort.Elements.ToDictionary(e => e.Name, e => e.Id);
            CustomComboBoxGroup<int> type = _editEntityPopUpForm.AddComboBox("设备类型", 
                (DeviceSerialPortDTO dto, int value) => dto.type = value, toolTypes);
            type.Ratio = 6;
            type.SetCurrent(type.IndexOf(dto.type));
            // 波特率
            Dictionary<string, int> baudRates = new() {
                { "4800", 4800 },
                { "9600", 9600 },
                { "19200", 19200 },
                { "38400", 38400 },
                { "43000", 43000 },
                { "56000", 56000 },
                { "115200", 115200 },
            };
            CustomComboBoxGroup<int> baudRate = _editEntityPopUpForm.AddComboBox("波特率", 
                (DeviceSerialPortDTO dto, int value) => dto.baud_rate = value, baudRates);
            baudRate.Ratio = 6;
            baudRate.SetCurrent(baudRate.IndexOf(dto.baud_rate));
            // 数据位
            Dictionary<string, int> dataBits = new() {
                // { "5", 5 },
                { "6", 6 },
                { "7", 7 },
                { "8", 8 },
            };
            CustomComboBoxGroup<int> dataBit = _editEntityPopUpForm.AddComboBox("数据位", 
                (DeviceSerialPortDTO dto, int value) => dto.data_bit = value, dataBits);
            dataBit.Ratio = 6;
            dataBit.SetCurrent(dataBit.IndexOf(dto.data_bit));
            // 校验位
            Dictionary<string, int> parities = new();
            Parity[] parityValues = Enum.GetValues<Parity>();
            foreach (Parity value in parityValues) {
                parities.Add(value.ToString(), (int) value);
            }
            CustomComboBoxGroup<int> parity = _editEntityPopUpForm.AddComboBox("校验位", 
                (DeviceSerialPortDTO dto, int value) => dto.parity = value, parities);
            parity.Ratio = 6;
            parity.SetCurrent(parity.IndexOf(dto.parity));
            // 停止位
            Dictionary<string, int> stopBits = new();
            StopBits[] stopBitsValues = Enum.GetValues<StopBits>();
            foreach (StopBits value in stopBitsValues) {
                stopBits.Add(value.ToString(), (int) value);
            }
            CustomComboBoxGroup<int> stopBit = _editEntityPopUpForm.AddComboBox("停止位", 
                (DeviceSerialPortDTO dto, int value) => dto.stop_bit = value, stopBits);
            stopBit.Ratio = 6;
            stopBit.SetCurrent(stopBit.IndexOf(dto.stop_bit));
            // 数据类型
            Dictionary<string, int> dataTypes = new();
            DataTypes[] dataTypesValues = Enum.GetValues<DataTypes>();
            foreach (DataTypes value in dataTypesValues) {
                dataTypes.Add(value.ToString(), (int) value);
            }
            CustomComboBoxGroup<int> dataType = _editEntityPopUpForm.AddComboBox("数据类型", 
                (DeviceSerialPortDTO dto, int value) => dto.data_type = value, dataTypes);
            dataType.Ratio = 6;
            dataType.SetCurrent(dataType.IndexOf(dto.data_type));
            // 无效字符
            CustomTextBoxGroup invalidChar = _editEntityPopUpForm.AddTextBox("无效字符", false, 
                (DeviceSerialPortDTO dto, string? value) => dto.invalid_char = value ?? "");
            invalidChar.Ratio = 6;
            if (dto.invalid_char != null) {
                invalidChar.SetValue(0, dto.invalid_char);
            }

            // 添加按钮
            CommonButton confirmButton = _editEntityPopUpForm.AddButton("保存");
            confirmButton.Click += (s, e) => {
                bool check = true;
                if (dto.id <= 0) {
                    if (!string.IsNullOrEmpty(dto.port_name) && _dataDTOList.Where(data => data.port_name == dto.port_name).Count() > 0) {
                        portFullName.SetError(true);
                        WidgetUtils.ShowWarningPopUp($"此串口[{dto.port_name}]已存在设备[{dto.port_full_name}]，无法多次配置同一个串口！");
                        check = false;
                    }
                }
                if (check) {
                    callBackAction += _editEntityPopUpForm.Dispose;
                    AddOrUpdate(dto, callBackAction);
                    _editEntityPopUpForm.Hide();
                }
            };
            CommonButton cancelButton = _editEntityPopUpForm.AddButton("取消");
            cancelButton.Click += (s, e) => {
                _editEntityPopUpForm.Dispose();
            };
            // Show form but make it transparent to create handles for its children
            _editEntityPopUpForm.PretendToShowToCreateHandlesForChildren();
            // Resize all widgets
            ResizePopUpForm();
            // Real show
            _editEntityPopUpForm.Show();
            callBackAction += _editEntityPopUpForm.Dispose;
        }
        private void ResizePopUpForm() {
            if (_editEntityPopUpForm != null) {
                _editEntityPopUpForm.ResizeTablePanelAndItsChildren();
                _editEntityPopUpForm.Invalidate();
            }
        }
        #endregion

        #region Override methods
        protected override List<DeviceSerialPortVO> QueryList() {
            QueryDeviceSerialPortListRsp rsp = apis.QueryDeviceSerialPortList(new() {
                UserId = SystemUtils.LoggedUserId(),
            });
            _dataDTOList = rsp.DeviceSerialPortDTOs;
            List<DeviceSerialPortVO> vos = new();
            CommonUtils.ObjectConverter<DeviceSerialPortDTO, DeviceSerialPortVO>(_dataDTOList, vos);
            // TODO: can use BackgroundWorker to do this
            // 后续再优化数据加载时的延迟、卡顿问题，现在先不管
            // for (int i = 0; i < 5000; i++) {
            //     workstationVOs.Add(workstationVOs[0]);
            // }
            return vos;
        }
        protected override void AddOrUpdate(DeviceSerialPortDTO dto, Action action) {
            AddOrUpdateDeviceSerialPortRsp rsp = apis.AddOrUpdateDeviceSerialPort(new(dto));
            if (rsp.RsponseCode == HttpResponseCode.OK) {
                WidgetUtils.ShowNoticePopUp("保存成功！");
            } else {
                WidgetUtils.ShowErrorPopUp($"保存失败！错误信息：{rsp.RsponseMessage}");
            }
            action();
        }
        protected override void Delete(List<int> ids) {
            if (ids.Count <= 0) {
                WidgetUtils.ShowNoticePopUp("请选择要删除的数据。");
            } else if (WidgetUtils.ShowConfirmPopUp($"确认要删除已选择的{ids.Count}条数据吗？")) {
                DeleteDeviceSerialPortByIdsRsp rsp = apis.DeleteDeviceSerialPort(new(ids));
                if (rsp.RsponseCode == HttpResponseCode.OK) {
                    WidgetUtils.ShowNoticePopUp($"成功删除{ids.Count}条数据！");
                }
            }
        }
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            _dataGridView.DataSource = QueryList();
        }
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            Size contentSize = new(Width - Padding.Size.Width, Height - Padding.Size.Height);
            _dataGridView.Size = contentSize;
        }
        public override void VisibleToTrue() {
            base.VisibleToTrue();
        }
        #endregion
    }
}
