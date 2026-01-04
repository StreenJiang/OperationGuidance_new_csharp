using CustomLibrary.Configs;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using Newtonsoft.Json;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_new.Views.SubViews;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace OperationGuidance_new.Views {
    public class WorkplaceMissionView_TZYX: AWorkplaceMissionView<WorkplaceContentPanel_TZYX, WorkplaceTopBar> {
        public WorkplaceMissionView_TZYX() {
            // Read to check if file exists
            CheckConfig();
        }
        public WorkplaceMissionView_TZYX(bool operatorOpenning) : base(operatorOpenning) {
            // Read to check if file exists
            CheckConfig();
        }

        private void CheckConfig() {
            string stationCode = MainUtils.MesConfig_TZYX.Read(Config_TZYX.StationCode);
            string ip = MainUtils.MesConfig_TZYX.Read(Config_TZYX.MESIP);
            string port = MainUtils.MesConfig_TZYX.Read(Config_TZYX.MESPort);
            string barcodePopUpEnabled = MainUtils.MesConfig_TZYX.Read(Config_TZYX.BarcodePopUpEnabled);
            if ((stationCode == null || stationCode == "")
                    && (ip == null || ip == "")
                    && (port == null || port == "")) {
                MainUtils.MesConfig_TZYX.Write(Config_TZYX.StationCode, "");
                MainUtils.MesConfig_TZYX.Write(Config_TZYX.MESIP, "");
                MainUtils.MesConfig_TZYX.Write(Config_TZYX.MESPort, "0");
            }

            if (barcodePopUpEnabled == null || barcodePopUpEnabled == "") {
                MainUtils.MesConfig_TZYX.Write(Config_TZYX.BarcodePopUpEnabled, "false");
            }
        }

        protected override WorkplaceContentPanel_TZYX GetWrokplacePanel(int? missionId, WorkplaceTopBar topBar) {
            return new(missionId, missionName => {
                topBar.Title = missionName;
            }) {
                BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND_2,
                Margin = new Padding(0),
            };
        }
    }

    public class WorkplaceContentPanel_TZYX: WorkplaceContentPanel {
        private const int HEART_BEATING_TIME = 5000;
        private readonly object _syncLock = new object();
        private readonly CancellationTokenSource _cts = new();

        private const int MAX_RETRY_COUNT = 3;
        private const int RETRY_DELAY_MS = 1000; // 1秒重试间隔

        private volatile bool _clickEventAddedForBarcodePopUp = false;

        // Connection status
        private Socket _socketClient;
        private bool _isConnected;
        private bool _isDisposed;

        // Mission
        private Task _heartbeatTask;
        private Task _receiveTask;

        // Data
        private string _stationCode;
        private bool _barcodePopUpEnabled;
        private List<OperationDataDTO> OperationDataDTOs = new();

        public WorkplaceContentPanel_TZYX() { }
        public WorkplaceContentPanel_TZYX(int? missionId, Action<string> resetMissionName) : base(missionId, resetMissionName) { }

        // Initialize mod bus server
        protected override void InitializeAfterHandelCreated() {
            _stationCode = MainUtils.MesConfig_TZYX.Read(Config_TZYX.StationCode);
            Task.Run(() => RunTCPClient(_cts.Token));
        }

        protected override void ActionAfterLoadingDevices() {
            if (MainUtils.IsAutoLockToolEnabled()) {
                _toolTasks.Values.ToList().ForEach(toolTask => toolTask.ForceSendLock());
            }
        }

        // Connect to MES via TCP
        private async Task RunTCPClient(CancellationToken ct) {
            string ip = MainUtils.MesConfig_TZYX.Read(Config_TZYX.MESIP);
            int port = int.Parse(MainUtils.MesConfig_TZYX.Read(Config_TZYX.MESPort));

            while (!ct.IsCancellationRequested) {
                try {
                    // 1. 检查并建立连接
                    await CheckAndConnectAsync(ip, port, ct);

                    // 测试代码
                    // testSendData();

                    // 2. 启动心跳任务
                    if (_heartbeatTask?.IsCompleted != false) {
                        _heartbeatTask = Task.Run(() => SendHeartbeatsAsync(ct), ct);
                    }

                    // 3. 启动接收任务
                    if (_receiveTask?.IsCompleted != false) {
                        _receiveTask = Task.Run(() => ReceiveDataAsync(ct), ct);
                    }

                    // 4. 等待任意任务完成
                    await Task.WhenAny(_heartbeatTask, _receiveTask);
                } catch (OperationCanceledException) {
                    break;
                } catch (Exception e) {
                    logger.Error("TCP client main loop error", e);
                    // 等待后重试
                    await Task.Delay(2000, ct);
                }
            }

            // 清理资源
            CloseSocket();
        }

        // Test function
        private void testSendData() {
            MESMessage_TZYX mESMessage = new() {
                StationCode = _stationCode,
                BarCode = "barcode",
                Product = "name",
                Operator = SystemUtils.LoggedUserName,
                Result = true,
            };

            string message;


            double torque = 2.313449121;
            mESMessage.Data.Add(new MESMessageData_TZYX() {
                Index = 1,
                TaskNo = 0,
                Torsion = Math.Round((double) torque, 2),
                Stroke = 0,
                Result = true,
                ResultMsg = "",
                TightTime = 10213,
                Unit = (int) Unit_TZYX.Nm,
                NeedTorsion = 123123.451,
            });

            message = JsonConvert.SerializeObject(mESMessage);
            logger.Info($"Data = {message}");
            _ = SendDataAsync(Encoding.ASCII.GetBytes(message));

        }

        private async Task CheckAndConnectAsync(string ip, int port, CancellationToken ct) {
            lock (_syncLock) {
                if (_socketClient != null && _socketClient.Connected)
                    return;

                // 关闭旧连接
                CloseSocket();

                // 创建新连接
                _socketClient = new Socket(AddressFamily.InterNetwork,
                                         SocketType.Stream,
                                         ProtocolType.Tcp);
            }

            try {
                // 异步连接
                await _socketClient.ConnectAsync(IPAddress.Parse(ip), port);

                // 检查连接状态
                if (!_socketClient.Connected) {
                    throw new SocketException((int) SocketError.NotConnected);
                }

                logger.Info($"Connected to MES server: {ip}:{port}");
                _isConnected = true;
            } catch (Exception ex) {
                logger.Error("Connection failed", ex);
                CloseSocket();
                throw;
            }
        }

        protected override void InitializeBarCodePanel() {
            _barCodeImage = Properties.Resources.bar_code_icon;
            _barCodePictureBox = new() {
                Margin = new(0),
                Padding = new(0),
            };
            _barCodeTextBox = new() {
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                DisabledBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
            };
            _barCodeTextBox.Text = ConfigsVariables.BAR_CODE_NOTE;
            _barCodeTextBox.Enabled = false;

            if (bool.Parse(MainUtils.MesConfig_TZYX.Read(Config_TZYX.BarcodePopUpEnabled))) {
                _barCodePictureBox.Click += barCodePopUp;
                _barCodeTextBox.Click += barCodePopUp;
            }
        }

        private async Task SendHeartbeatsAsync(CancellationToken ct) {
            var heartbeatData = Encoding.ASCII.GetBytes("\0");

            try {
                while (!ct.IsCancellationRequested && IsConnected()) {
                    // 发送心跳包
                    await SendDataAsync(heartbeatData, ct);
                    logger.Info("Sent heartbeat");

                    // 精确等待
                    await Task.Delay(HEART_BEATING_TIME, ct);
                }
            } catch (Exception ex) {
                if (!ct.IsCancellationRequested) {
                    logger.Error("Heartbeat sending error", ex);
                }
            }
        }

        private async Task ReceiveDataAsync(CancellationToken ct) {
            while (!ct.IsCancellationRequested && IsConnected()) {
                try {
                    if (_activated) {
                        await Task.Delay(500);
                        continue;
                    }

                    // 接收数据
                    var buffer = new byte[4096];
                    int bytesRead = await _socketClient.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        SocketFlags.None);

                    // 连接关闭
                    if (bytesRead == 0) {
                        logger.Info("Connection closed by remote host");
                        break;
                    }
                    logger.Info($"Data received with length = {bytesRead}");

                    // 处理接收到的数据
                    ProcessReceivedData(buffer, bytesRead);
                } catch (Exception ex) {
                    if (!ct.IsCancellationRequested) {
                        logger.Error("Data receiving error", ex);
                    } else {
                        break;
                    }
                }
            }
        }

        private void ProcessReceivedData(byte[] buffer, int bytesRead) {
            try {
                // 复制数据
                byte[] receivedData = new byte[bytesRead];
                Buffer.BlockCopy(buffer, 0, receivedData, 0, bytesRead);

                // 转换为字符串
                string dataMessage = Encoding.ASCII.GetString(receivedData);
                logger.Info($"Received data: [{dataMessage}]");

                // 解析数据
                string[] parts = dataMessage.Split(',');
                if (parts.Length < 2) {
                    logger.Warn("Invalid data format");
                    return;
                }

                string barCode = parts[0].Trim();
                string stationCode = parts[1].Trim();

                // Won't handle if station code not matched
                if (_stationCode == stationCode) {
                    // 处理条码
                    ActionAfterRecevingBarCode(barCode);
                }
            } catch (Exception ex) {
                logger.Error("Data processing error", ex);
            }
        }

        private async Task SendDataAsync(byte[] data, CancellationToken ct = default) {
            if (data == null || data.Length == 0)
                return;

            int retryCount = 0;
            bool success = false;
            Exception lastException = null;

            while (retryCount < MAX_RETRY_COUNT && !success) {
                ct.ThrowIfCancellationRequested(); // 检查取消请求

                try {
                    Socket socketToUse;
                    lock (_syncLock) {
                        // 检查连接状态
                        if (_socketClient == null || !_socketClient.Connected) {
                            throw new InvalidOperationException("Not connected");
                        }
                        socketToUse = _socketClient;
                    }

                    // 创建发送任务和取消任务的组合
                    var sendTask = socketToUse.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
                    var cancellationTask = AsTask(ct); // 使用辅助方法

                    await Task.WhenAny(sendTask, cancellationTask);
                    ct.ThrowIfCancellationRequested(); // 如果取消被请求则抛出异常
                    await sendTask; // 确保发送任务完成

                    success = true; // 发送成功
                    logger.Info($"Data sent successfully (attempt {retryCount + 1})");
                } catch (OperationCanceledException) {
                    logger.Info("Send operation canceled");
                    throw;
                } catch (SocketException sex) when (sex.SocketErrorCode == SocketError.ConnectionReset) {
                    // 连接被重置，可能是服务器断开
                    logger.Warn($"Connection reset during send (attempt {retryCount + 1})");
                    lastException = sex;
                    CloseSocket(); // 关闭连接以便后续重连
                } catch (Exception ex) {
                    logger.Warn($"Send failed (attempt {retryCount + 1}): {ex.Message}");
                    lastException = ex;
                }

                if (!success && retryCount < MAX_RETRY_COUNT - 1) {
                    // 等待后重试
                    logger.Info($"Retrying in {RETRY_DELAY_MS}ms...");
                    await Task.Delay(RETRY_DELAY_MS, ct);
                }

                retryCount++;
            }

            // 如果所有重试都失败
            if (!success) {
                logger.Error($"All {MAX_RETRY_COUNT} send attempts failed. Saving data locally.");
                SaveDataToLocal(data);
                throw new InvalidOperationException("Send failed after all retries", lastException);
            }
        }

        // 将数据保存到本地文件
        private void SaveDataToLocal(byte[] data) {
            try {
                // 获取项目根目录
                string appRoot = AppDomain.CurrentDomain.BaseDirectory;
                string saveDirectory = Path.Combine(appRoot, "FailedData");

                // 确保目录存在
                Directory.CreateDirectory(saveDirectory);

                // 生成文件名（时间戳+随机数）
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string randomPart = Path.GetRandomFileName().Split('.')[0];
                string fileName = $"failed_data_{timestamp}_{randomPart}.dat";
                string filePath = Path.Combine(saveDirectory, fileName);

                // 写入文件
                File.WriteAllBytes(filePath, data);
                logger.Info($"Data saved to: {filePath}");
            } catch (Exception ex) {
                logger.Error($"Failed to save data locally: {ex.Message}");
            }
        }

        // 辅助方法：将 CancellationToken 转换为 Task
        private static Task AsTask(CancellationToken cancellationToken) {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(() => tcs.SetCanceled());
            return tcs.Task;
        }

        private bool IsConnected() {
            lock (_syncLock) {
                return _socketClient?.Connected == true;
            }
        }

        private void CloseSocket() {
            lock (_syncLock) {
                if (_socketClient != null) {
                    try {
                        if (_socketClient.Connected) {
                            _socketClient.Shutdown(SocketShutdown.Both);
                        }
                        _socketClient.Close();
                    } catch (Exception ex) {
                        logger.Error("Socket closing error", ex);
                    } finally {
                        _socketClient.Dispose();
                        _socketClient = null;
                        _isConnected = false;
                    }
                }
            }
        }

        protected override void OnHandleDestroyed(EventArgs e) {
            base.OnHandleDestroyed(e);

            // 请求取消所有操作
            _cts.Cancel();

            // 关闭Socket
            CloseSocket();

            _isDisposed = true;
        }

        // Action after receving bar code msg
        private void ActionAfterRecevingBarCode(string msg) {
            if (IsDisposed || _activated)
                return;

            // UI线程操作
            this.InvokeIfRequired(() => {
                if (_barCodePopUpForm == null || _barCodePopUpForm.IsDisposed) {
                    OpenBarCodePopUpForm(msg);
                } else {
                    _barCodePopUpForm.ValidateBarCode(msg);
                }
            });
        }

        // 线程安全调用UI
        private void InvokeIfRequired(Action action) {
            if (InvokeRequired) {
                Invoke(action);
            } else {
                action();
            }
        }

        protected override void OpenBarCodePopUpForm(string? barCode = null) {
            if (_barCodePopUpForm == null || _barCodePopUpForm.IsDisposed) {
                if (_activated && _currentWorkingBolt != null) {
                    _rulesExcluded = GetCurrentExcludedRules(_currentWorkingBolt.BoltDTO);
                } else {
                    _rulesExcluded = GetCurrentExcludedRules();
                }

                _barCodePopUpForm = new BarCodeInputPopUpForm_TZYX(this, ConfigsVariables.BAR_CODE_NOTE, _mission, _activated,
                        _productBarCodeMatchingRules, _partsBarCodeMatchingRules, barCode, _rulesExcluded, CheckLockMsg(WorkingProcessPanel.LockedBoltBarCode)) {
                    Title = "录入条码",
                    BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
                    ShowInFront = false,
                    TopMost = false,
                };
                if (!_activated) {
                    _barCodePopUpForm.AddButton("激活任务").Click += (sender, eventArgs) => {
                        if (!_activated) {
                            if (!_barCodePopUpForm.CheckCanActivateMission()) {
                                CustomTextBox customTextBox = _barCodePopUpForm.ProductBarCodeBox.GetTextBox(0);
                                if (string.IsNullOrEmpty(_barCodeObj.ProductBarCode)) {
                                    customTextBox.IsError = true;
                                }
                                for (int i = 0; i < _barCodePopUpForm.PartsBarCodeContentPanel.Controls.Count; i++) {
                                    if (i >= _barCodeObj.PartsBarCodes.Count) {
                                        ((CustomTextBoxButtonGroup) _barCodePopUpForm.PartsBarCodeContentPanel.Controls[i]).GetTextBox(0).IsError = true;
                                    }
                                }
                                WidgetUtils.ShowWarningPopUp("条码录入完成后才可激活任务");
                            } else {
                                ActivateMission();
                                _barCodePopUpForm.Dispose();
                            }
                        } else {
                            _barCodePopUpForm.Dispose();
                        }
                    };
                }
                _barCodePopUpForm.AddButton("关闭").Click += (sender, eventArgs) => _barCodePopUpForm.Dispose();
                _barCodePopUpForm.PretendToShowToCreateHandlesForChildren();
                _barCodePopUpForm.ResizeSelf();
            }
            _barCodePopUpForm.Show();
        }

        protected override List<DeviceCategory>? CustomCategories() {
            DeviceCategory deviceCategory = new(7, "MES连接",
                    Properties.Resources.MES,
                    Properties.Resources.MES_error,
                    Properties.Resources.MES_empty);
            return new() { deviceCategory };
        }

        protected override void CheckCustomConnections(DeviceBlock block, DeviceCategory category) {
            if (category.Id == 7) {
                if (IsConnected()) {
                    block.ResetIconByStatus(DeviceStatus.NORMAL);
                } else {
                    block.ResetIconByStatus(DeviceStatus.ERROR);
                }
            }
        }

        protected override void StoreTighteningData(OperationDataDTO operationDataDTO) {
            OperationDataDTOs.Add(operationDataDTO);
            base.StoreTighteningData(operationDataDTO);
        }

        public override async Task TerminateMission(WorkplaceProcessStatus status) {
            if (_missionRecord != null) {
                string message = "Not set";
                try {
                    MESMessage_TZYX mESMessage = new() {
                        StationCode = MainUtils.MesConfig_TZYX.Read(Config_TZYX.StationCode),
                        BarCode = _missionRecord.product_bar_code,
                        Product = _mission.name,
                        Operator = SystemUtils.LoggedUserName,
                        Result = _missionRecord.mission_result == (int) YesOrNo.YES,
                    };

                    foreach (OperationDataDTO data in OperationDataDTOs) {
                        // They don't need NOK data
                        if (data.tightening_status != (int) TighteningStatus.OK) {
                            continue;
                        }

                        MESMessageData_TZYX mESMessageData = new() {
                            Index = (int) data.bolt_serial_num,
                            TaskNo = 0,
                            Torsion = Math.Round((double) data.torque, 2),
                            Stroke = Math.Round((double) (data.rundown_angle / 360), 2),
                            Result = data.tightening_status == (int) TighteningStatus.OK,
                            ResultMsg = _errorMsg,
                            TightTime = (int) _rundownTime,
                            Unit = (int) Unit_TZYX.Nm,
                            NeedTorsion = Math.Round((double) data.torque_final_target, 2),
                        };

                        mESMessage.Data.Add(mESMessageData);
                    }

                    message = JsonConvert.SerializeObject(mESMessage);
                    logger.Info($"Data = {message}");
                    _ = SendDataAsync(Encoding.ASCII.GetBytes(message));

                    OperationDataDTOs = new();
                } catch (Exception e) {
                    logger.Error($"Error while sending data to MES server, Data = {message}", e);
                }
            }

            _ = base.TerminateMission(status);

            _barCodeTextBox.Text = ConfigsVariables.BAR_CODE_NOTE;
        }

        protected override void DoAfterRecevingTighteningDataAsync(TighteningData data, int deviceId) {
            BeginInvoke(() => {
                // Nonactivated or finished will not handle any received data
                if (!_activated) {
                    return;
                }

                try {
                    ToolTask toolTask = _toolTasks[deviceId];
                    // Lock first
                    if (MainUtils.IsArmLocatingEnabled()) {
                        toolTask.ForceSendLock();
                    }
                    if (toolTask.WorkstationId != null) {
                        int workstationId = toolTask.WorkstationId.Value;

                        List<WorkstationDTO> workstationDTOs;
                        if (CheckIfIsMultiDeviceIndependenceMode()) {
                            workstationDTOs = _workstationsDTOs.Where(dto => _currentWorkingBoltIndependence.Keys.Contains(dto.id)).ToList();
                        } else {
                            List<int> workstationIds = new();
                            foreach (List<BoltButton> bolts in _allBolts.Values) {
                                workstationIds.AddRange(bolts.Select(b => b.BoltDTO.workstation_id));
                            }
                            workstationIds = workstationIds.Distinct().ToList();
                            workstationDTOs = _workstationsDTOs.Where(dto => workstationIds.Contains(dto.id) && dto.arm_id != null).ToList();
                        }
                        List<int?> toolIds = workstationDTOs.Select(dto => dto.tool_id).ToList();

                        // Main display
                        _torquePanel.Data = data.torque + "";
                        _anglePanel.Data = data.angle + "";

                        // Get current bolt
                        BoltButton currentBolt;
                        if (CheckIfIsMultiDeviceIndependenceMode()) {
                            currentBolt = _currentWorkingBoltIndependence[workstationId];
                        } else {
                            currentBolt = CommonUtils.CannotBeNull(_currentWorkingBolt);
                        }

                        // Check if current showing side is equal to side of working bolt, if no then switch to the right side
                        if (currentBolt.BoltDTO.side_id != _sides[_currentSideIndex].id) {
                            ProductSideDTO? sideTemp = _sides.Find(s => s.id == currentBolt.BoltDTO.side_id);
                            if (sideTemp != null) {
                                _currentSideIndex = _sides.IndexOf(sideTemp);
                                ChangeSideAndInvalidate();
                            }
                        }

                        ProductBoltDTO boltDTO = currentBolt.BoltDTO;
                        OperationDataDTO dataDTO = new();
                        CommonUtils.ObjectConverter<TighteningData, OperationDataDTO>(data, dataDTO);
                        // Set pset manualy if tool type is sudong x7
                        if (toolTask.ToolType is ToolSudongX7 toolX7) {
                            dataDTO.parameter_set_number = currentBolt.CurrentParameterSet;
                        }

                        WorkstationDTO workstationDTO = _workstationsDTOs.Single(dto => dto.id == workstationId);
                        dataDTO.workstation_id = workstationDTO.id;
                        dataDTO.workstation_name = workstationDTO.name;

                        DeviceToolDTO toolDTO = _tools.Single(t => t.id == deviceId);
                        dataDTO.tool_name = toolDTO.name;
                        dataDTO.tool_ip = $"{toolDTO.ip}:{toolDTO.port}";
                        dataDTO.tool_type = DeviceType_Tool.GetById(toolDTO.type).Name;
                        dataDTO.product_sied_id = _sides[_currentSideIndex].id;
                        dataDTO.bolt_serial_num = boltDTO.serial_num;
                        MissionRecordDTO missionRecord = CommonUtils.CannotBeNull(_missionRecord);
                        dataDTO.mission_record_id = missionRecord.id;
                        dataDTO.vin_number = missionRecord.product_bar_code;
                        if (_realTimeArmCoordinates != null) {
                            dataDTO.arm_position = _realTimeArmCoordinates.ToString();
                        }

                        // WHYC
                        // TZYX
                        _rundownTime = data.rundown_time;

                        // If result type is tightening
                        if (data.result_type == (int) TightenOrLoosen.TIGHTENING) {
                            bool tighteningOK = true;
                            string errorMsg = "";
                            // Initialize color to ok
                            _torquePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_OK;
                            _anglePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_OK;

                            // Check tightening status
                            if (data.tightening_status != (int) TighteningStatus.OK) {
                                tighteningOK = false;
                                if (data.tightening_error_status != null &&
                                        data.tightening_error_status != (int) TighteningErrorStatus_SuDong.NO_ERROR) {
                                    if (!string.IsNullOrEmpty(errorMsg)) {
                                        errorMsg += "\r\n";
                                    }
                                    string errorMsgTemp;
                                    if (Enum.IsDefined(typeof(TighteningErrorStatus_SuDong), data.tightening_error_status)) {
                                        TighteningErrorStatus_SuDong errorStatus_SuDong = (TighteningErrorStatus_SuDong) data.tightening_error_status;
                                        switch (errorStatus_SuDong) {
                                            case TighteningErrorStatus_SuDong.SLIPPAGE:
                                                errorMsgTemp = "滑丝/滑牙";
                                                break;
                                            case TighteningErrorStatus_SuDong.FALSE_LOCKING:
                                                errorMsgTemp = "浮锁";
                                                break;
                                            case TighteningErrorStatus_SuDong.TORQUE_NOK:
                                                errorMsgTemp = "扭矩不良";
                                                break;
                                            case TighteningErrorStatus_SuDong.ANGLE_NOK:
                                                errorMsgTemp = "拧紧角度不良";
                                                break;
                                            case TighteningErrorStatus_SuDong.SEND_UNLOCK_IN_TIGTHENING:
                                                errorMsgTemp = "中途提前释放启动信号";
                                                break;
                                            default:
                                                errorMsgTemp = $"未知错误代码【{data.tightening_error_status}】";
                                                break;
                                        }
                                    } else {
                                        errorMsgTemp = $"未知错误代码【{data.tightening_error_status}】";
                                    }
                                    errorMsg += $"拧紧出错，错误信息：{errorMsgTemp}";
                                }
                                if (data.torque_status != (int) TighteningCommonStatus.OK) {
                                    _torquePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NOK;
                                    if (!string.IsNullOrEmpty(errorMsg)) {
                                        errorMsg += "\r\n";
                                    }
                                    errorMsg += $"扭矩未达标：{Enum.GetName(typeof(TighteningCommonStatus), data.torque_status)}";
                                }
                                if (data.angle_status != (int) TighteningCommonStatus.OK) {
                                    _anglePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NOK;
                                    if (!string.IsNullOrEmpty(errorMsg)) {
                                        errorMsg += "\r\n";
                                    }
                                    errorMsg += $"角度未达标：{Enum.GetName(typeof(TighteningCommonStatus), data.angle_status)}";
                                }
                            }

                            // Check torque
                            if (boltDTO.torque_max > 0 && (data.torque < boltDTO.torque_min || data.torque > boltDTO.torque_max)) {
                                tighteningOK = false;
                                _torquePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NOK;
                                if (!string.IsNullOrEmpty(errorMsg)) {
                                    errorMsg += "\r\n";
                                }
                                errorMsg += "扭矩与配置范围不符";
                            }

                            // Check angle
                            if (boltDTO.angle_max > 0 && (data.angle < boltDTO.angle_min || data.angle > boltDTO.angle_max)) {
                                tighteningOK = false;
                                _anglePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NOK;
                                if (!string.IsNullOrEmpty(errorMsg)) {
                                    errorMsg += "\r\n";
                                }
                                errorMsg += "角度与配置范围不符";
                            }

                            // Switch to next bolt
                            if (tighteningOK) {
                                _errorMsg = null;

                                // Reset tightening type to tightening in case somewhere did some changes
                                _needLoosening = false;
                                RemoveInformationMsg(_workingProcessPanel.NGReasons);
                                _workingProcessPanel.NGReasons = null;

                                currentBolt.BoltStatus = BoltStatus.DONE;

                                // Check next index
                                List<BoltButton> currentSideBolts;
                                if (CheckIfIsMultiDeviceIndependenceMode()) {
                                    currentSideBolts = _allBoltsIndependence[_sides[_currentSideIndex].id][workstationId];
                                } else {
                                    currentSideBolts = _allBolts[_sides[_currentSideIndex].id];
                                }
                                int nextIndex = currentSideBolts.IndexOf(currentBolt) + 1;
                                // 检查是否存在跳点的情况
                                while (nextIndex < currentSideBolts.Count && currentSideBolts[nextIndex].BoltStatus == BoltStatus.DONE) {
                                    nextIndex++;
                                }

                                // Store data
                                dataDTO.tightening_status = (int) TighteningStatus.OK;
                                StoreTighteningData(dataDTO);

                                if (nextIndex < currentSideBolts.Count) {
                                    if (CheckIfIsMultiDeviceIndependenceMode()) {
                                        _currentWorkingBoltIndependence[workstationId] = SwitchBolt(workstationId, nextIndex);
                                        ChangeBoltStatusToWorking(_currentWorkingBoltIndependence[workstationId]);
                                    } else {
                                        _currentWorkingBolt = SwitchBolt(nextIndex);
                                        ChangeBoltStatusToWorking(_currentWorkingBolt);
                                    }
                                } else {
                                    bool allDone = true;
                                    if (CheckIfIsMultiDeviceIndependenceMode()) {
                                        foreach (int id in _allBoltsIndependence[_sides[_currentSideIndex].id].Keys) {
                                            if (id != workstationId) {
                                                BoltButton? boltButton = _allBoltsIndependence[_sides[_currentSideIndex].id][id].Find(b => b.BoltStatus != BoltStatus.DONE);
                                                if (boltButton != null) {
                                                    allDone = false;
                                                    break;
                                                }
                                            }
                                        }
                                    } else {
                                        if (_currentSideIndex < _sides.Count - 1) {
                                            _currentSideIndex++;
                                            _currentWorkingBolt = SwitchBolt(0);
                                            ChangeBoltStatusToWorking(_currentWorkingBolt);
                                            ChangeSideAndInvalidate();
                                            allDone = false;
                                        }
                                    }

                                    if (allDone) {
                                        // Update mission result to ok
                                        _missionRecord.mission_result = (int) TighteningStatus.OK;
                                        _apis.AddOrUpdateMissionRecord(new(_missionRecord));

                                        // Checks for challenge mission
                                        if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
                                            AddChallengeResult(_mission.id, ChallengeTaskEnum.MISSION_OK);
                                        }

                                        TerminateMission(WorkplaceProcessStatus.FINISHED_OK);
                                    }
                                }
                            } else {
                                // Change bolt status
                                currentBolt.BoltStatus = BoltStatus.ERROR;

                                // Count ng times
                                currentBolt.NgTimes++;

                                // Set error message
                                _workingProcessPanel.NGReasons = errorMsg;
                                AddInformationMsg(_workingProcessPanel.NGReasons);

                                // WHYC
                                // TZYX
                                _errorMsg = errorMsg;

                                // Set status of data to ng
                                dataDTO.tightening_status = (int) TighteningStatus.NG;

                                // 记录数据
                                StoreTighteningData(dataDTO);

                                // Should not lock in the first place when it has error
                                if (MainUtils.IsArmLocatingEnabled()) {
                                    toolTask.ForceSendUnlock();
                                }
                            }
                        } else {
                            _needLoosening = false;

                            // 反松结束后把扭矩角度改回黑色
                            _torquePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NORMAL;
                            _anglePanel.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NORMAL;

                            // Remove error message
                            RemoveLockMsg(_workingProcessPanel.NGReasons);
                            _workingProcessPanel.NGReasons = null;

                            if (MainUtils.GetStoreLooseningData()) {
                                // 记录数据
                                StoreTighteningData(dataDTO);
                            }
                        }
                    }
                } catch (Exception e) {
                    logger.Error($"Error occurred while handling tightening data, e: {e}");
                }
            });
        }

        protected override void ToolOperationPopUpFormExtraActions(ToolOperationPopUpForm popUpForm) {
            if (MainUtils.IsAutoLockToolEnabled()) {
                popUpForm.BtnLock.Enabled = false;
                popUpForm.BtnUnlock.Enabled = false;
                popUpForm.BtnPSet.Enabled = false;
            }
        }
    }
}
