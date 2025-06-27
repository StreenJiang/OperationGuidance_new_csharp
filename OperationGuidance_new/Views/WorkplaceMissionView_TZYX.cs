using CustomLibrary.Configs;
using Newtonsoft.Json;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Views.ReusableWidgets;
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
            string stationCode = MainUtils.MesConfig_TZYX.Read(Config_TZYX.StationCode);
            string ip = MainUtils.MesConfig_TZYX.Read(Config_TZYX.MESIP);
            string port = MainUtils.MesConfig_TZYX.Read(Config_TZYX.MESPort);
            if ((stationCode == null || stationCode == "") && (ip == null || ip == "") && (port == null || port == "")) {
                MainUtils.MesConfig_TZYX.Write(Config_TZYX.StationCode, "");
                MainUtils.MesConfig_TZYX.Write(Config_TZYX.MESIP, "");
                MainUtils.MesConfig_TZYX.Write(Config_TZYX.MESPort, "0");
            }
        }
        public WorkplaceMissionView_TZYX(bool operatorOpenning) : base(operatorOpenning) {
            // Read to check if file exists
            string stationCode = MainUtils.MesConfig_TZYX.Read(Config_TZYX.StationCode);
            string ip = MainUtils.MesConfig_TZYX.Read(Config_TZYX.MESIP);
            string port = MainUtils.MesConfig_TZYX.Read(Config_TZYX.MESPort);
            if ((stationCode == null || stationCode == "") && (ip == null || ip == "") && (port == null || port == "")) {
                MainUtils.MesConfig_TZYX.Write(Config_TZYX.StationCode, "");
                MainUtils.MesConfig_TZYX.Write(Config_TZYX.MESIP, "");
                MainUtils.MesConfig_TZYX.Write(Config_TZYX.MESPort, "0");
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

        // Connection status
        private Socket _socketClient;
        private bool _isConnected;
        private bool _isDisposed;

        // Mission
        private Task _heartbeatTask;
        private Task _receiveTask;


        // Barcode handling status
        private volatile bool _isProcessingBarcode;
        private string _lastBarcode;

        // Data
        private string _stationCode;
        private List<OperationDataDTO> OperationDataDTOs = new();

        public WorkplaceContentPanel_TZYX() {
            _stationCode = MainUtils.MesConfig_TZYX.Read(Config_TZYX.StationCode);
        }
        public WorkplaceContentPanel_TZYX(int? missionId, Action<string> resetMissionName) : base(missionId, resetMissionName) {
            _stationCode = MainUtils.MesConfig_TZYX.Read(Config_TZYX.StationCode);
        }

        // Initialize mod bus server
        protected override void InitializeAfterHandelCreated() {
            Task.Run(() => RunTCPClient(_cts.Token));
        }

        // Connect to MES via TCP
        private async Task RunTCPClient(CancellationToken ct) {
            string ip = MainUtils.MesConfig_TZYX.Read(Config_TZYX.MESIP);
            int port = int.Parse(MainUtils.MesConfig_TZYX.Read(Config_TZYX.MESPort));

            while (!ct.IsCancellationRequested) {
                try {
                    // 1. 检查并建立连接
                    await CheckAndConnectAsync(ip, port, ct);

                    // 2. 启动心跳任务
                    if (_heartbeatTask?.IsCompleted != false) {
                        _heartbeatTask = Task.Run(() => SendHeartbeatsAsync(ct), ct);
                    }

                    // 3. 启动接收任务
                    if (_receiveTask?.IsCompleted != false && !_isProcessingBarcode) {
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

        private async Task SendHeartbeatsAsync(CancellationToken ct) {
            var heartbeatData = Encoding.ASCII.GetBytes("\0");

            try {
                while (!ct.IsCancellationRequested && IsConnected()) {
                    // 发送心跳包
                    await SendDataAsync(heartbeatData, ct);

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
            var buffer = new byte[4096];

            try {
                while (!ct.IsCancellationRequested && IsConnected()) {
                    if (_isProcessingBarcode) {
                        await Task.Delay(1000);
                        continue;
                    }

                    // 接收数据
                    int bytesRead = await _socketClient.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        SocketFlags.None);

                    // 连接关闭
                    if (bytesRead == 0) {
                        logger.Info("Connection closed by remote host");
                        break;
                    }

                    // 处理接收到的数据
                    ProcessReceivedData(buffer, bytesRead);
                }
            } catch (Exception ex) {
                if (!ct.IsCancellationRequested) {
                    logger.Error("Data receiving error", ex);
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

                // 处理条码
                ProcessBarcode(barCode);
            } catch (Exception ex) {
                logger.Error("Data processing error", ex);
            }
        }

        private void ProcessBarcode(string barCode) {
            try {
                // 设置处理标志
                _isProcessingBarcode = true;
                _lastBarcode = barCode;

                // 处理条码
                ActionAfterRecevingBarCode(barCode);
            } catch (Exception ex) {
                logger.Error("Barcode processing error", ex);
                // 处理失败后重置状态
                ResetBarcodeProcessing();
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

        // 重置条码处理状态（由外部调用）
        public void ResetBarcodeProcessing() {
            _isProcessingBarcode = false;
            logger.Info("Barcode processing reset. Ready for next barcode.");
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
                MESMessage_TZYX mESMessage = new() {
                    StationCode = _stationCode,
                    BarCode = _missionRecord.product_bar_code,
                    Product = _mission.name,
                    Operator = SystemUtils.LoggedUserName,
                    Result = _missionRecord.mission_result == (int) YesOrNo.YES,
                };

                OperationDataDTOs.ForEach(data => {
                    MESMessageData_TZYX mESMessageData = new() {
                        Index = (int) data.bolt_serial_num,
                        TaskNo = 0,
                        Torsion = (double) data.torque,
                        Stroke = (double) (data.rundown_angle / 360),
                        Result = data.tightening_status == (int) TighteningStatus.OK,
                        ResultMsg = _errorMsg,
                        TightTime = (int) _rundownTime,
                        Unit = (int) Unit_TZYX.Nm,
                        NeedTorsion = (double) data.torque_final_target,
                    };

                    mESMessage.Data.Add(mESMessageData);
                });

                string message = JsonConvert.SerializeObject(mESMessage);
                _ = SendDataAsync(Encoding.ASCII.GetBytes(message));
            }

            _ = base.TerminateMission(status);
        }
    }
}
