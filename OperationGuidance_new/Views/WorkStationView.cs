using CustomLibrary.Buttons;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Utils;
using CustomLibrary.Configs;
using CustomLibrary.ComboBoxes;
using CustomLibrary.Utils;
using OperationGuidance_service.Constants;
using CustomLibrary.DataGridViewRelateds;
using OperationGuidance_new.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Responses;
using CustomLibrary.TextBoxes;
using System.IO.Ports;
using CustomLibrary.Panels.AbstractClasses;

namespace OperationGuidance_new.Views
{
    public class WorkStationView: ACustomDataGridViewOuterPanel<WorkstationDTO, WorkstationVO> {
        #region Fields
        // Apis
        protected OperationGuidanceApis apis;
        protected List<WorkstationDTO> _dataDTOList;
        // DataGridView panel
        private DataGridViewGroup<WorkstationVO> _dataGridView;
        // Add new pop up form
        private EditEntityPopUpForm<WorkstationDTO> _editEntityPopUpForm;
        // Combo options
        // Tool related
        List<DeviceToolDTO> _toolDTOs;
        Dictionary<string, int> _toolIdOptions;
        Dictionary<string, int?> _toolTypeOptions;
        // Arm related
        List<DeviceArmDTO> _armDTOs;
        Dictionary<string, int> _armIdOptions;
        Dictionary<string, int?> _armTypeOptions;
        // Communication related
        List<DeviceCommunicationDTO> _communicationDTOs;
        Dictionary<string, int> _communicationIdOptions;
        Dictionary<string, int?> _communicationTypeOptions;
        // Serial port related
        List<DeviceSerialPortDTO> _serialPortDTOs;
        Dictionary<string, int> _serialPortIdOptions;
        Dictionary<string, int?> _serialPortTypeOptions;
        #endregion

        protected DataGridViewGroup<WorkstationVO> DataGridView => _dataGridView;

        #region Constructors
        public WorkStationView() {
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

            // 获取所有设备的类型
            _toolTypeOptions = DeviceType_Tool.Elements.ToDictionary(t => t.Name, t => (int?) t.Id);
            _armTypeOptions = DeviceType_Arm.Elements.ToDictionary(t => t.Name, t => (int?) t.Id);
            _communicationTypeOptions = DeviceType_Communication.Elements.ToDictionary(t => t.Name, t => (int?) t.Id);
            _serialPortTypeOptions = DeviceType_SerialPort.Elements.ToDictionary(t => t.Name, t => (int?) t.Id);
            // 获取串口设备的一些参数选项

            _dataGridView.AddTextBox("站点名称", false, (WorkstationVO vo, string? value) => vo.name = value).Ratio = 6.25;
            _dataGridView.AddComboBox("工具类型", (WorkstationVO vo, int? value) => vo.tool_type = value, _toolTypeOptions).Ratio = 6.25;
            _dataGridView.AddComboBox("力臂类型", (WorkstationVO vo, int? value) => vo.arm_type = value, _armTypeOptions).Ratio = 6.25;
            _dataGridView.AddComboBox("串口设备类型", (WorkstationVO vo, int? value) => vo.serial_port_type = value, _serialPortTypeOptions).Ratio = 6.25;
            _dataGridView.AddComboBox("通讯设备类型", (WorkstationVO vo, int? value) => vo.communication_type = value, _communicationTypeOptions).Ratio = 6.25;

            // 按钮逻辑
            _dataGridView.QueryData = (vo) => {
                List<WorkstationVO> vos = QueryList();
                return vos
                    .Where(o => vo.name == null || o.name != null && o.name.Contains(vo.name))
                    .Where(o => vo.tool_type == null || vo.tool_type == 0 || o.tool_type != null && o.tool_type == vo.tool_type)
                    .Where(o => vo.arm_type == null || vo.arm_type == 0 || o.arm_type != null && o.arm_type == vo.arm_type)
                    .Where(o => vo.serial_port_type == null || vo.serial_port_type == 0 || o.serial_port_type != null && o.serial_port_type == vo.serial_port_type)
                    .Where(o => vo.communication_type == null || vo.communication_type == 0 || o.communication_type != null && o.communication_type == vo.communication_type)
                    .ToList();
            };
            _dataGridView.AddNewClick = (action) => {
                WorkstationDTO dto = new();
                OpenEditEntityPopUpForm("新增站点", dto, action);
            };
            _dataGridView.ModifyClick = (ids, action) => {
                if (ids.Count <= 0) {
                    WidgetUtils.ShowNoticePopUp("请选择要编辑的数据。");
                } else if (ids.Count > 1) {
                    WidgetUtils.ShowNoticePopUp("只能选择一条数据进行编辑操作。");
                } else {
                    if (_dataDTOList.Count > 0) {
                        WorkstationDTO dto = _dataDTOList.Single(dto => dto.id == ids[0]);
                        OpenEditEntityPopUpForm("更新站点信息", dto, action);
                        // 更新后再触发一次查询操作
                        action();
                    }
                }
            };
            _dataGridView.DeleteClick = (ids, action) => {
                // 删除选择的数据
                Delete(ids);
                // 删除后再触发一次查询操作
                action();
            };
            // Toggle button check changed
            _dataGridView.VoGridView.GridView.CellValueChanged += (sender, eventArgs) => {
                if (sender != null && sender is DataGridView view) {
                    DataGridViewCell cell = view.Rows[eventArgs.RowIndex].Cells[eventArgs.ColumnIndex];
                    if (cell is DataGridViewToggleButtonCell tCell) {
                        DataGridViewRow row = view.Rows[eventArgs.RowIndex];
                        WorkstationVO vo = (WorkstationVO) row.DataBoundItem;
                        WorkstationDTO dto = _dataDTOList.Single(dto => vo.id != null && dto.id == vo.id.Value);
                        bool value = (bool) cell.Value;
                        dto.enabled = value ? (int) YesOrNo.YES : (int) YesOrNo.NO;
                        apis.AddOrUpdateWorkstation(new(dto));
                    }
                }
            };
        }
        #endregion

        #region Reusable methods
        private void GetComboOptions() {
            // 获取工具信息
            QueryDeviceToolListRsp queryDeviceToolListRsp = apis.QueryDeviceToolList(new(SystemUtils.MacAddressesDTO.id));
            _toolDTOs = queryDeviceToolListRsp.DeviceToolDTOs;
            _toolIdOptions = _toolDTOs.ToDictionary(dto => CommonUtils.CannotBeNull(dto.name), dto => dto.id);
            // 获取力臂信息
            QueryDeviceArmListRsp queryDeviceArmListRsp = apis.QueryDeviceArmList(new(SystemUtils.MacAddressesDTO.id));
            _armDTOs = queryDeviceArmListRsp.DeviceArmDTOs;
            _armIdOptions = _armDTOs.ToDictionary(dto => CommonUtils.CannotBeNull(dto.name), dto => dto.id);
            // 获取通讯设备信息
            QueryDeviceCommunicationListRsp queryDeviceCommunicationListRsp = apis.QueryDeviceCommunicationList(new(SystemUtils.MacAddressesDTO.id));
            _communicationDTOs = queryDeviceCommunicationListRsp.DeviceCommunicationDTOs;
            _communicationIdOptions = _communicationDTOs.ToDictionary(dto => CommonUtils.CannotBeNull(dto.name), dto => dto.id);
            // 获取串口设备信息
            QueryDeviceSerialPortListRsp queryDeviceSerialPortListRsp = apis.QueryDeviceSerialPortList(new(SystemUtils.MacAddressesDTO.id));
            _serialPortDTOs = queryDeviceSerialPortListRsp.DeviceSerialPortDTOs;
            _serialPortIdOptions = _serialPortDTOs.ToDictionary(dto => CommonUtils.CannotBeNull(dto.name), dto => dto.id);
        }
        private void OpenEditEntityPopUpForm(string title, WorkstationDTO dto, Action callBackAction) {
            // 获取所有设备的类型选项
            GetComboOptions();
            _editEntityPopUpForm = new(dto) {
                Title = title,
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            // 添加字段
            CustomTextBoxGroup stationName = _editEntityPopUpForm.AddTextBox("站点名称", false,
                (WorkstationDTO dto, string? value) => dto.name = value ?? "");
            stationName.Ratio = null;
            _editEntityPopUpForm.TablePanel.SetColumnSpan(stationName, 2);
            if (dto.name != null) {
                stationName.SetValue(0, dto.name);
            }
            stationName.GetTextBox(0).TextChanged += (sender, eventArgs) => {
                stationName.GetTextBox(0).IsError = string.IsNullOrEmpty(stationName.GetTextBox(0).Box.Text);
            };
            // 工具部分
            SubPanel<WorkstationDTO> toolSubPanel = _editEntityPopUpForm.AddSubPanel("工具");
            // 工具选择
            ToggleButton toolToggle = toolSubPanel.TitlePanel.AddRightButton<ToggleButton>();
            CustomComboBoxGroup<int> toolOptions = toolSubPanel.AddComboBox("选择工具", (WorkstationDTO dto, int value) => dto.tool_id = value, _toolIdOptions);
            // 工具类型
            CustomComboBoxGroup<int?> toolTypeTextBox = toolSubPanel.AddComboBox("工具类型", (WorkstationDTO dto, int? value) => dto.tool_type = value, _toolTypeOptions);
            toolTypeTextBox.Enabled = false;
            // 工具IP
            CustomTextBoxGroup toolIPTextBox = toolSubPanel.AddTextBox("工具IP", false, (WorkstationDTO dto, string? value) => dto.tool_ip = value ?? "");
            toolIPTextBox.Enabled = false;
            // 工具端口
            CustomTextBoxGroup toolPortTextBox = toolSubPanel.AddTextBox("工具端口", false, (WorkstationDTO dto, int? value) => dto.tool_port = value ?? 0);
            toolPortTextBox.Enabled = false;
            if (dto.tool_id != null) {
                if (toolOptions.IndexOf(dto.tool_id.Value) >= 0) {
                    toolOptions.SetCurrent(toolOptions.IndexOf(dto.tool_id.Value));
                } else if (dto.id > 0) {
                    toolOptions.SetError(true);
                    Task.Run(async () => {
                        await Task.Delay(500);
                        WidgetUtils.ShowWarningPopUp("所配置工具不存在或已被删除");
                    });
                }
                SetToolValues();
                toolToggle.Checked = true;
            } else {
                toolSubPanel.TablePanel.Hide();
                ResetToolValues();
            }
            // 工具选择事件：选择后自动填入
            toolOptions.ItemSelected += () => {
                if (!toolOptions.IsDefaultValue()) {
                    SetToolValues();
                } else {
                    ResetToolValues();
                }
            };
            void SetToolValues() {
                DeviceToolDTO? deviceToolDTO = _toolDTOs.SingleOrDefault(t => t.id == toolOptions.Value);
                if (deviceToolDTO != null) {
                    toolTypeTextBox.SetCurrent(toolTypeTextBox.IndexOf(deviceToolDTO.type));
                    toolIPTextBox.SetValue(0, deviceToolDTO.ip);
                    toolPortTextBox.SetValue(0, deviceToolDTO.port + "");
                    // Set for show warning message
                    dto.tool_name = toolOptions.Key;
                    if (toolOptions.IsError) {
                        toolOptions.SetError(false);
                    }
                } else {
                    dto.tool_id = null;
                    dto.tool_name = null;
                }
            }
            void ResetToolValues() {
                toolTypeTextBox.Reset();
                toolIPTextBox.SetValue(0, null);
                toolPortTextBox.SetValue(0, null);
                dto.tool_name = null;
            }
            // 是否显示工具开关事件
            int toolChosenIndexCache = -1;
            toolToggle.CheckedChanged += (sender, eventArgs) => {
                if (toolToggle.Checked) {
                    toolSubPanel.TablePanel.Show();
                    toolOptions.SetCurrent(toolChosenIndexCache);
                } else {
                    toolSubPanel.TablePanel.Hide();
                    toolChosenIndexCache = toolOptions.GetCurrentIndex();
                    toolOptions.Reset();
                }
                ResizePopUpForm();
                toolTypeTextBox.ResizeChildren();
                toolIPTextBox.ResizeChildren();
                toolPortTextBox.ResizeChildren();
                toolOptions.ResizeChildren();
            };
            // 力臂部分
            SubPanel<WorkstationDTO> armSubPanel = _editEntityPopUpForm.AddSubPanel("力臂");
            // 力臂选择
            ToggleButton armToggle = armSubPanel.TitlePanel.AddRightButton<ToggleButton>();
            CustomComboBoxGroup<int> armOptions = armSubPanel.AddComboBox("选择力臂", (WorkstationDTO dto, int value) => dto.arm_id = value, _armIdOptions);
            // 力臂类型
            CustomComboBoxGroup<int?> armTypeTextBox = armSubPanel.AddComboBox("力臂类型", (WorkstationDTO dto, int? value) => dto.arm_type = value, _armTypeOptions);
            armTypeTextBox.Enabled = false;
            // 力臂IP
            CustomTextBoxGroup armIPTextBox = armSubPanel.AddTextBox("力臂IP", false, (WorkstationDTO dto, string? value) => dto.arm_ip = value == null ? "" : value);
            armIPTextBox.Enabled = false;
            // 力臂端口
            CustomTextBoxGroup armPortTextBox = armSubPanel.AddTextBox("力臂端口", false, (WorkstationDTO dto, int? value) => dto.arm_port = value == null ? 0 : value);
            armPortTextBox.Enabled = false;
            if (dto.arm_id != null) {
                if (armOptions.IndexOf(dto.arm_id.Value) >= 0) {
                    armOptions.SetCurrent(armOptions.IndexOf(dto.arm_id.Value));
                } else if (dto.id > 0) {
                    armOptions.SetError(true);
                    Task.Run(async () => {
                        await Task.Delay(500);
                        WidgetUtils.ShowWarningPopUp("所配置力臂不存在或已被删除");
                    });
                }
                SetArmValues();
                armToggle.Checked = true;
            } else {
                armSubPanel.TablePanel.Hide();
                ResetArmValues();
            }
            // 力臂选择事件：选择后自动填入
            armOptions.ItemSelected += () => {
                if (!armOptions.IsDefaultValue()) {
                    SetArmValues();
                } else {
                    ResetArmValues();
                }
            };
            void SetArmValues() {
                DeviceArmDTO? deviceArmDTO = _armDTOs.SingleOrDefault(dto => dto.id == armOptions.Value);
                if (deviceArmDTO != null) {
                    armTypeTextBox.SetCurrent(armTypeTextBox.IndexOf(deviceArmDTO.type));
                    armIPTextBox.SetValue(0, deviceArmDTO.ip);
                    armPortTextBox.SetValue(0, deviceArmDTO.port + "");
                    // Set for show warning message
                    dto.arm_name = armOptions.Key;
                    if (armOptions.IsError) {
                        armOptions.SetError(false);
                    }
                } else {
                    dto.arm_id = null;
                    dto.arm_name = null;
                }
            }
            void ResetArmValues() {
                armTypeTextBox.Reset();
                armIPTextBox.SetValue(0, null);
                armPortTextBox.SetValue(0, null);
                dto.arm_name = null;
            }
            // 是否显示力臂开关事件
            int armChosenIndexCache = -1;
            armToggle.CheckedChanged += (sender, eventArgs) => {
                if (armToggle.Checked) {
                    armSubPanel.TablePanel.Show();
                    armOptions.SetCurrent(armChosenIndexCache);
                } else {
                    armSubPanel.TablePanel.Hide();
                    armChosenIndexCache = armOptions.GetCurrentIndex();
                    armOptions.Reset();
                }
                ResizePopUpForm();
                armTypeTextBox.ResizeChildren();
                armIPTextBox.ResizeChildren();
                armPortTextBox.ResizeChildren();
                armOptions.ResizeChildren();
            };
            // 通讯设备部分
            SubPanel<WorkstationDTO> communicationSubPanel = _editEntityPopUpForm.AddSubPanel("通讯设备");
            // 通讯设备选择
            ToggleButton communicationToggle = communicationSubPanel.TitlePanel.AddRightButton<ToggleButton>();
            CustomComboBoxGroup<int> communicationOptions = communicationSubPanel.AddComboBox("选择设备", (WorkstationDTO dto, int value) => dto.communication_id = value, _communicationIdOptions);
            // 通讯设备类型
            CustomComboBoxGroup<int?> communicationTypeTextBox = communicationSubPanel.AddComboBox("设备类型",
                (WorkstationDTO dto, int? value) => dto.communication_type = value, _communicationTypeOptions);
            communicationTypeTextBox.Enabled = false;
            // 通讯设备IP
            CustomTextBoxGroup communicationIPTextBox = communicationSubPanel.AddTextBox("设备IP", false,
                (WorkstationDTO dto, string? value) => dto.communication_ip = value ?? "");
            communicationIPTextBox.Enabled = false;
            // 通讯设备端口
            CustomTextBoxGroup communicationPortTextBox = communicationSubPanel.AddTextBox("设备端口", false,
                (WorkstationDTO dto, int? value) => dto.communication_port = value == null ? 0 : value);
            communicationPortTextBox.Enabled = false;
            if (dto.communication_id != null) {
                if (communicationOptions.IndexOf(dto.communication_id.Value) >= 0) {
                    communicationOptions.SetCurrent(communicationOptions.IndexOf(dto.communication_id.Value));
                } else if (dto.id > 0) {
                    communicationOptions.SetError(true);
                    Task.Run(async () => {
                        await Task.Delay(500);
                        WidgetUtils.ShowWarningPopUp("所配置通讯设备不存在或已被删除");
                    });
                }
                SetCommunicationValues();
                communicationToggle.Checked = true;
            } else {
                communicationSubPanel.TablePanel.Hide();
                ResetCommunicationValues();
            }
            // 通讯设备选择事件：选择后自动填入
            communicationOptions.ItemSelected += () => {
                if (!communicationOptions.IsDefaultValue()) {
                    SetCommunicationValues();
                } else {
                    ResetCommunicationValues();
                }
            };
            void SetCommunicationValues() {
                DeviceCommunicationDTO? deviceCommunicationDTO = _communicationDTOs.SingleOrDefault(dto => dto.id == communicationOptions.Value);
                if (deviceCommunicationDTO != null) {
                    communicationTypeTextBox.SetCurrent(communicationTypeTextBox.IndexOf(deviceCommunicationDTO.type));
                    communicationIPTextBox.SetValue(0, deviceCommunicationDTO.ip);
                    communicationPortTextBox.SetValue(0, deviceCommunicationDTO.port + "");
                    // Set for show warning message
                    dto.communication_name = communicationOptions.Key;
                    if (communicationOptions.IsError) {
                        communicationOptions.SetError(false);
                    }
                } else {
                    dto.communication_id = null;
                    dto.communication_name = null;
                }
            }
            void ResetCommunicationValues() {
                communicationTypeTextBox.Reset();
                communicationIPTextBox.SetValue(0, null);
                communicationPortTextBox.SetValue(0, null);
                dto.communication_name = null;
            }
            // 是否显示通讯设备开关事件
            int communicationChosenIndexCache = -1;
            communicationToggle.CheckedChanged += (sender, eventArgs) => {
                if (communicationToggle.Checked) {
                    communicationSubPanel.TablePanel.Show();
                    communicationOptions.SetCurrent(communicationChosenIndexCache);
                } else {
                    communicationSubPanel.TablePanel.Hide();
                    communicationChosenIndexCache = communicationOptions.GetCurrentIndex();
                    communicationOptions.Reset();
                }
                ResizePopUpForm();
                communicationTypeTextBox.ResizeChildren();
                communicationIPTextBox.ResizeChildren();
                communicationPortTextBox.ResizeChildren();
                communicationOptions.ResizeChildren();
            };
            // 串口设备部分
            SubPanel<WorkstationDTO> serialPortSubPanel = _editEntityPopUpForm.AddSubPanel("串口设备");
            // 串口选择
            ToggleButton serialPortToggle = serialPortSubPanel.TitlePanel.AddRightButton<ToggleButton>();
            CustomComboBoxGroup<int> serialPortOptions = serialPortSubPanel.AddComboBox("选择设备",
                (WorkstationDTO dto, int value) => dto.serial_port_id = value, _serialPortIdOptions);
            // 串口类型
            CustomComboBoxGroup<int?> serialPortTypeTextBox = serialPortSubPanel.AddComboBox("串口类型",
                (WorkstationDTO dto, int? value) => dto.serial_port_type = value, _serialPortTypeOptions);
            serialPortTypeTextBox.Enabled = false;
            // 串口号
            CustomTextBoxGroup serialPortPortNameTextBox = serialPortSubPanel.AddTextBox("串口号", false,
                (WorkstationDTO dto, string? value) => dto.serial_port_port_name = value ?? "");
            serialPortPortNameTextBox.Enabled = false;
            // 波特率
            CustomTextBoxGroup serialPortBaudRateTextBox = serialPortSubPanel.AddTextBox("波特率", false,
                (WorkstationDTO dto, int? value) => dto.serial_port_baud_rate = value ?? 0);
            serialPortBaudRateTextBox.Enabled = false;
            // 数据位
            CustomTextBoxGroup serialPortDataBitTextBox = serialPortSubPanel.AddTextBox("数据位", false,
                (WorkstationDTO dto, int? value) => dto.serial_port_data_bit = value == null ? 0 : value);
            serialPortDataBitTextBox.Enabled = false;
            // 校验位
            CustomTextBoxGroup serialPortParityTextBox = serialPortSubPanel.AddTextBox<int>("校验位", false, null);
            serialPortParityTextBox.Enabled = false;
            // 停止位
            CustomTextBoxGroup serialPortStopBitTextBox = serialPortSubPanel.AddTextBox<int>("停止位", false, null);
            serialPortStopBitTextBox.Enabled = false;
            // 数据类型
            CustomTextBoxGroup serialPortDataTypeTextBox = serialPortSubPanel.AddTextBox<int>("数据类型", false, null);
            serialPortDataTypeTextBox.Enabled = false;
            if (dto.serial_port_id != null) {
                if (serialPortOptions.IndexOf(dto.serial_port_id.Value) >= 0) {
                    serialPortOptions.SetCurrent(serialPortOptions.IndexOf(dto.serial_port_id.Value));
                } else if (dto.id > 0) {
                    serialPortOptions.SetError(true);
                    Task.Run(async () => {
                        await Task.Delay(500);
                        WidgetUtils.ShowWarningPopUp("所配置串口设备不存在或已被删除");
                    });
                }
                SetSerialPortValues();
                serialPortToggle.Checked = true;
            } else {
                serialPortSubPanel.TablePanel.Hide();
                ResetSerialPortValues();
            }
            // 串口选择事件：选择后自动填入
            serialPortOptions.ItemSelected += () => {
                if (!serialPortOptions.IsDefaultValue()) {
                    SetSerialPortValues();
                } else {
                    ResetSerialPortValues();
                }
            };
            void SetSerialPortValues() {
                DeviceSerialPortDTO? deviceSerialPortDTO = _serialPortDTOs.SingleOrDefault(dto => dto.id == serialPortOptions.Value);
                if (deviceSerialPortDTO != null) {
                    serialPortTypeTextBox.SetCurrent(serialPortTypeTextBox.IndexOf(deviceSerialPortDTO.type));
                    serialPortPortNameTextBox.SetValue(0, deviceSerialPortDTO.port_name);
                    serialPortBaudRateTextBox.SetValue(0, deviceSerialPortDTO.baud_rate + "");
                    serialPortDataBitTextBox.SetValue(0, deviceSerialPortDTO.data_bit + "");
                    serialPortParityTextBox.SetValue(0, Enum.GetName(typeof(Parity), deviceSerialPortDTO.parity) + "");
                    serialPortStopBitTextBox.SetValue(0, Enum.GetName(typeof(StopBits), deviceSerialPortDTO.stop_bit) + "");
                    serialPortDataTypeTextBox.SetValue(0, Enum.GetName(typeof(DataTypes), deviceSerialPortDTO.data_type) + "");
                    // Set for show warning message
                    dto.serial_port_name = serialPortOptions.Key;
                    if (serialPortOptions.IsError) {
                        serialPortOptions.SetError(false);
                    }
                } else {
                    dto.serial_port_id = null;
                    dto.serial_port_name = null;
                }
            }
            void ResetSerialPortValues() {
                serialPortTypeTextBox.Reset();
                serialPortPortNameTextBox.SetValue(0, null);
                serialPortBaudRateTextBox.SetValue(0, null);
                serialPortDataBitTextBox.SetValue(0, null);
                serialPortParityTextBox.SetValue(0, null);
                serialPortStopBitTextBox.SetValue(0, null);
                serialPortDataTypeTextBox.SetValue(0, null);
                dto.serial_port_name = null;
            }
            // 是否显示串口开关事件
            int serialPortChosenIndexCache = -1;
            serialPortToggle.CheckedChanged += (sender, eventArgs) => {
                if (serialPortToggle.Checked) {
                    serialPortSubPanel.TablePanel.Show();
                    serialPortOptions.SetCurrent(serialPortChosenIndexCache);
                } else {
                    serialPortSubPanel.TablePanel.Hide();
                    serialPortChosenIndexCache = serialPortOptions.GetCurrentIndex();
                    serialPortOptions.Reset();
                }
                ResizePopUpForm();
                serialPortTypeTextBox.ResizeChildren();
                serialPortPortNameTextBox.ResizeChildren();
                serialPortBaudRateTextBox.ResizeChildren();
                serialPortDataBitTextBox.ResizeChildren();
                serialPortParityTextBox.ResizeChildren();
                serialPortStopBitTextBox.ResizeChildren();
                serialPortDataTypeTextBox.ResizeChildren();
                serialPortOptions.ResizeChildren();
            };

            // 添加按钮
            CommonButton confirmButton = _editEntityPopUpForm.AddButton("保存");
            confirmButton.Click += (s, e) => {
                bool check = true;
                string warningMsg = "";
                int warningIndex = 1;
                List<WorkstationDTO> allData = apis.QueryWorkstationList(new(SystemUtils.MacAddressesDTO.id)).WorkstationsDTOs;
                if (string.IsNullOrEmpty(stationName.GetTextBox(0).Box.Text)) {
                    check = false;
                    stationName.GetTextBox(0).IsError = true;
                    warningMsg += $"{warningIndex++}. 站点名称不能为空\r\n";
                }
                if (allData.Exists(d => d.id != dto.id && d.name == dto.name)) {
                    check = false;
                    stationName.GetTextBox(0).IsError = true;
                    warningMsg += $"{warningIndex++}. 站点名称已存在\r\n";
                }
                if (toolToggle.Checked && (dto.tool_id == null || dto.tool_id == 0)) {
                    toolOptions.SetError(true);
                    warningMsg += $"{warningIndex++}. 请选择工具\r\n";
                    check = false;
                }
                if (armToggle.Checked && (dto.arm_id == null || dto.arm_id == 0)) {
                    armOptions.SetError(true);
                    warningMsg += $"{warningIndex++}. 请选择力臂\r\n";
                    check = false;
                }
                if (communicationToggle.Checked && (dto.communication_id == null || dto.communication_id == 0)) {
                    communicationOptions.SetError(true);
                    warningMsg += $"{warningIndex++}. 请选择通讯设备\r\n";
                    check = false;
                }
                if (serialPortToggle.Checked && (dto.serial_port_id == null || dto.serial_port_id == 0)) {
                    serialPortOptions.SetError(true);
                    warningMsg += $"{warningIndex++}. 请选择串口设备\r\n";
                    check = false;
                }
                if (dto.id <= 0) {
                    // Use data without filters to check
                    if (dto.tool_id > 0 && allData.Where(data => data.tool_id == dto.tool_id).Count() > 0) {
                        toolOptions.SetError(true);
                        warningMsg += $"{warningIndex++}. 已有站点配置了工具[{dto.tool_name}]，无法多次配置同一个工具\r\n";
                        check = false;
                    }
                    if (dto.arm_id > 0 && allData.Where(data => data.arm_id == dto.arm_id).Count() > 0) {
                        armOptions.SetError(true);
                        warningMsg += $"{warningIndex++}. 已有站点配置了力臂[{dto.arm_name}]，无法多次配置同一个力臂\r\n";
                        check = false;
                    }
                    if (dto.serial_port_id > 0 && allData.Where(data => data.serial_port_id == dto.serial_port_id).Count() > 0) {
                        serialPortOptions.SetError(true);
                        warningMsg += $"{warningIndex++}. 已有站点配置了串口设备[{dto.serial_port_name}]，无法多次配置同一个串口设备\r\n";
                        check = false;
                    }
                    if (dto.communication_id > 0 && allData.Where(data => data.communication_id == dto.communication_id).Count() > 0) {
                        communicationOptions.SetError(true);
                        warningMsg += $"{warningIndex++}. 已有站点配置了通讯设备[{dto.communication_name}]，无法多次配置同一个通讯设备\r\n";
                        check = false;
                    }
                }
                if (!check) {
                    WidgetUtils.ShowWarningPopUp($"保存失败：\r\n{warningMsg}");
                } else {
                    toolOptions.SetError(false);
                    armOptions.SetError(false);
                    serialPortOptions.SetError(false);
                    communicationOptions.SetError(false);
                    if (dto.tool_id <= 0) dto.tool_id = null;
                    if (dto.arm_id <= 0) dto.arm_id = null;
                    if (dto.serial_port_id <= 0) dto.serial_port_id = null;
                    if (dto.communication_id <= 0) dto.communication_id = null;

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
        }
        private void ResizePopUpForm() {
            if (_editEntityPopUpForm != null) {
                _editEntityPopUpForm.ResizeTablePanelAndItsChildren();
                _editEntityPopUpForm.Invalidate();
            }
        }
        #endregion

        #region Override methods
        protected override List<WorkstationVO> QueryList() {
            QueryWorkstationListRsp rsp = apis.QueryWorkstationList(new(SystemUtils.MacAddressesDTO.id));
            _dataDTOList = rsp.WorkstationsDTOs;
            List<WorkstationVO> vos = new();
            CommonUtils.ObjectConverter<WorkstationDTO, WorkstationVO>(_dataDTOList, vos);
            // TODO: can use BackgroundWorker to do this
            // 后续再优化数据加载时的延迟、卡顿问题，现在先不管
            // for (int i = 0; i < 5000; i++) {
            //     workstationVOs.Add(workstationVOs[0]);
            // }
            return vos;
        }
        protected override void AddOrUpdate(WorkstationDTO dto, Action action) {
            AddOrUpdateWorkstationRsp rsp = apis.AddOrUpdateWorkstation(new(dto));
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
                DeleteWorkstationByIdsRsp rsp = apis.DeleteWorkstation(new(ids));
                if (rsp.RsponseCode == HttpResponseCode.OK) {
                    WidgetUtils.ShowNoticePopUp($"成功删除{ids.Count}条数据！");
                } else {
                    WidgetUtils.ShowErrorPopUp($"删除失败！错误信息：{rsp.RsponseMessage}");
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
        #endregion
    }
}
