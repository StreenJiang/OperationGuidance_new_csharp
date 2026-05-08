using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using Newtonsoft.Json;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.HttpObjects;
using OperationGuidance_new.HttpObjects.Requests.SCII_XT;
using OperationGuidance_new.PLC;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Tasks.DeviceTypes;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Utils.IIPSC;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Attributes;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using OperationGuidance_new.Views.SubViews;
using System.Reflection;
using OperationGuidance_new.Extensions;
using CustomLibrary.Forms;

namespace OperationGuidance_new.Views {
    public class WorkplaceMissionView_SCII_XT: AWorkplaceMissionView<WorkplaceContentPanel_SCII_XT, WorkplaceTopBar_SCII> {
        public WorkplaceMissionView_SCII_XT() { }
        public WorkplaceMissionView_SCII_XT(bool operatorOpenning) : base(operatorOpenning) { }

        protected override WorkplaceContentPanel_SCII_XT GetWrokplacePanel(int? missionId, WorkplaceTopBar topBar) {
            return new(missionId, missionName => {
                topBar.Title = missionName;
            }) {
                BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND_2,
                Margin = new Padding(0),
                PaddingWithoutBorder = true,
                View = this,
            };
        }

        protected override void OpenMissionListView() {
            // Initialize
            _missionListPanel = new("任务列表", "进入工作台", (s, e) => OpenWorkplaceViewDirectly()) {
                Margin = new Padding(0),
                Parent = this,
            };
        }
    }

    public class WorkplaceContentPanel_SCII_XT: WorkplaceContentPanel_SCII {
        private WorkplaceMissionView_SCII_XT _view;
        public new WorkplaceMissionView_SCII_XT View { get => _view; set => _view = value; }

        private List<OperationDataDTO> _operationDataDTOs;
        private CancellationTokenSource? _plcLoopCts;
        private bool _inBoundStationOk = false;

        private WaitDialog? _barcodeDialog;
        private volatile bool _canReceiveBarcode = false;

        public WaitDialog? BarcodeDialog { get => _barcodeDialog; set => _barcodeDialog = value; }
        public bool CanReceiveBarcode { get => _canReceiveBarcode; set => _canReceiveBarcode = value; }

        public WorkplaceContentPanel_SCII_XT() { }
        public WorkplaceContentPanel_SCII_XT(int? missionId, Action<string> resetMissionName) : base(missionId, resetMissionName) {
            _productBatch.Enabled = false; // Will fetch from config file for SCII XT

            Task.Run(() => {
                string batchNo = _getBatchNo();
                if (string.IsNullOrEmpty(batchNo)) {
                    _productBatch.GetTextBox(0).IsError = true;

                    // Looping to check config and back-fill
                    _ = _loopingToCheckBatchNo();
                }

                _productBatch.GetTextBox(0).Box.Text = batchNo;
            });

            SciiXtController.ActionAfterReceivedRecipe = ActionAfterReceivedRecipe;
            SciiXtController.ActionAfterReceivedBatch = ActionAfterReceivedBatch;
        }

        protected override void ActivateMissionAutomatically() {
            string recipeCode = _getRecipeCode();
            if (!string.IsNullOrEmpty(recipeCode)) {
                var rsp = _apis.QueryProductMissionDetail(new(recipeCode));
                if (rsp != null && rsp.ProductMissionDTO != null) {
                    if (_mission != null && _mission.id == rsp.ProductMissionDTO.id) {
                        base.ActivateMissionAutomatically();
                    }
                }
            }
        }

        protected override async Task ActionAfterActivatingMission() {
            await base.ActionAfterActivatingMission();

            _operationDataDTOs = new();
        }

        public override async Task TerminateMission(WorkplaceProcessStatus status) {
            await SendDataToMES(_operationDataDTOs);
            _inBoundStationOk = false;

            if (await OutBound()) {
                await base.TerminateMission(status);

                SwitchMissionByRecipe(_getRecipeCode());
            }
        }

        public bool InBound(string barCode, string batchCode) {
            var req = new SCII_XT_InOrOutBoundStationReq() {
                productCode = barCode,
                passType = (int) SCII_XT_PassType.PRODUCT,
                recipeCode = _mission.name,
                procedureCode = _getProcedureCode(),
                equipmentCode = _getEquipmentCode(),
                batchNo = batchCode,
            };

            // Send request
            var dto = Task.Run(async () => await Workflow_SCII_XT.InBoundStation(req))
                          .GetAwaiter()
                          .GetResult();
            if (!dto.inOrOutSuccess) {
                logger.Warn($"进站失败，详细信息：{dto.message}");
                WidgetUtils.ShowWarningPopUp($"进站请求失败，详细信息：{dto.message}");
            } else {
                logger.Info($"【{_mission.name}】进站成功。");
            }
            _inBoundStationOk = dto.inOrOutSuccess;
            return dto.inOrOutSuccess;
        }

        private async Task<bool> OutBound() {
            SCII_XT_InOrOutBoundStationReq req = new() {
                productCode = _missionRecord.product_bar_code,
                passType = (int) SCII_XT_PassType.PRODUCT,
                recipeCode = _mission.name,
                procedureCode = _getProcedureCode(),
                equipmentCode = _getEquipmentCode(),
                batchNo = _missionRecord.product_batch,
                result = _missionRecord.mission_result == (int) TighteningStatus.OK,
            };

            var dto = await Workflow_SCII_XT.OutBoundStation(req);
            if (!dto.inOrOutSuccess) {
                logger.Warn($"出站失败，详细信息：{dto.message}");
                if (WidgetUtils.ShowConfirmPopUp($"出战请求失败！点击【是】重试。\n\n详细信息：{dto.message}")) {
                    return await OutBound();
                }
                // 点“否”不 return false，因为终究是要结束任务的，true 才能结束任务
            } else {
                logger.Info($"【{_mission.name}】出站成功。");
            }

            return true;
        }

        protected override void InitializeAfterHandelCreated() {
            base.InitializeAfterHandelCreated();

            LoadPlc();

            _missionSelectedName.DeleteButton<CommonButton>(0);

            SwitchMissionByRecipe(_getRecipeCode());
        }

        private void SwitchMissionByRecipe(string recipeCode) {
            if (_activated) {
                WidgetUtils.ShowWarningPopUp("接收到配方变更请求，将在当前未结束任务结束以后自动切换至指定配方所对应的任务。");
                return;
            }

            if (!string.IsNullOrEmpty(recipeCode) && (_mission == null || _mission.name != recipeCode)) {
                var rsp = _apis.QueryProductMissionDetail(new(recipeCode));
                if (rsp != null && rsp.ProductMissionDTO != null) {
                    if (_mission == null || _mission.id != rsp.ProductMissionDTO.id) {
                        BeginInvoke(() => {
                            SwitchToMission(rsp.ProductMissionDTO);
                        });
                    }
                } else {
                    WidgetUtils.ShowWarningPopUp($"未找到配方[{recipeCode}]对应的任务，请检查是否配置了该任务。");
                }
            }
        }

        private async Task ActionAfterReceivedRecipe(string recipeCode) {
            await Task.Run(() => SwitchMissionByRecipe(recipeCode));
        }

        private async Task ActionAfterReceivedBatch(string batchNo) {
            BeginInvoke(() => {
                _productBatch.GetTextBox(0).Box.Text = batchNo;
            });

            await Task.CompletedTask;
        }

        protected override async Task StoreTighteningData(OperationDataDTO operationDataDTO) {
            logger.Info("StoreTighteningData start ........");

            // Use task to store data asynchronously
            await StoreDataToDatabaseAsync(operationDataDTO);

            // 将数据暂存，用于发送给 MES
            _operationDataDTOs.Add(operationDataDTO);

            // 先将VOs加入到实时显示数据列表中
            OperationDataVO dataFormatted = new();
            CommonUtils.ObjectConverter<OperationDataDTO, OperationDataVO>(operationDataDTO, dataFormatted);
            _tighteningDataVOs.Add(dataFormatted);

            RefreshTighteningDataPanel(_tighteningDataVOs);
            logger.Info("StoreTighteningData showing to panel end ........");

            // 最后再存进本地文件
            await StoreDataToFilesAsync(operationDataDTO);

            logger.Info("StoreTighteningData end ........");
        }

        public async Task SendToPrinter() {
            await Task.Run(() => BeginInvoke(() => {
                var config = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();
                if (config.enabled == (int) YesOrNo.YES) {
                    if (config.printer_name == null) {
                        WidgetUtils.ShowWarningPopUp("打印机名称配置未设置，请先配置打印机。");
                    } else {
                        int _okSumToday = int.Parse(_okSumPerDay.GetTextBox(0).Box.Text);
                        config.batch_code = DateTime.Now.ToString(MainUtils.DATETIME_FORMAT_YYYYMMDD);
                        config.sn = _okSumToday + 1;

                        using (ZplQrCodePrinter printer = new()) {
                            List<string> list = printer.GetAvailablePrinters();
                            if (list.Count > 0) {
                                string? printerName = list.Find(p => p == config.printer_name);
                                if (printerName == null) {
                                    WidgetUtils.ShowWarningPopUp("未找到指定配置的打印机，请检查配置或打印机。");
                                } else if (!printer.QuickPrint(config)) {
                                    WidgetUtils.ShowWarningPopUp("发送指令至打印机失败！请检查日志信息定位问题。");
                                }
                            } else {
                                WidgetUtils.ShowWarningPopUp("未找到任何打印机设备！");
                            }
                        }
                    }
                }
            }));
        }
        public async Task SendQRCodeToPrinter(string qrContent) {
            await Task.Run(() => BeginInvoke(() => {
                var config = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();
                if (config.enabled == (int) YesOrNo.YES) {
                    using (ZplQrCodePrinter printer = new()) {
                        List<string> list = printer.GetAvailablePrinters();
                        if (list.Count > 0) {
                            string? printerName = list.Find(p => p == config.second_printer_name);
                            if (printerName == null) {
                                WidgetUtils.ShowWarningPopUp("未找到指定配置的条码打印机（第二台），请检查配置或打印机。");
                            } else {
                                var detailConfig = ConfigUtils.LoadConfig<SecondPrinterDetailConfig>();
                                if (!printer.PrintQrContent(qrContent, printerName,
                                    detailConfig.dpmm, detailConfig.label_size_mm,
                                    detailConfig.qr_size_mm, detailConfig.margin_x_factor, detailConfig.margin_y_factor)) {
                                    WidgetUtils.ShowWarningPopUp("发送指令至条码打印机（第二台）失败！请检查日志信息定位问题。");
                                }
                            }
                        } else {
                            WidgetUtils.ShowWarningPopUp("未找到任何打印机设备！");
                        }
                    }
                }
            }));
        }
        private async Task SendDataToMES(List<OperationDataDTO> operationDataDTOs) {
            if (operationDataDTOs.Count > 0) {

                SCII_XT_BindProductDataReq req = new() {
                    bingType = (int) SCII_XT_BindType.PRODUCT,
                    productInfos = new(),
                    procedureCode = _getProcedureCode(),
                    recipeCode = _mission.name,
                };
                SCII_XT_BindProductDataReq.ProductInfo productInfos = new() {
                    productCode = operationDataDTOs[0].vin_number ?? "NULL",
                    attributeList = new(),
                };

                // Set values
                List<Dictionary<string, object>> value = new();
                foreach (OperationDataDTO operationDataDTO in operationDataDTOs) {
                    Dictionary<string, object> eachValue = new();
                    OperationDataDTO_SCII_XT data = new OperationDataDTO_SCII_XT();
                    CommonUtils.ObjectConverter<OperationDataDTO, OperationDataDTO_SCII_XT>(operationDataDTO, data);

                    data.parts_bar_codes = _missionRecord?.parts_bar_code;
                    data.batch_code = _getBatchNo();
                    data.time = DateTime.Now.ToString(MainUtils.DATETIME_FORMAT_YYYY_MM_DD_HH_MM_SS);

                    PropertyInfo[] propertyInfos = data.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (PropertyInfo property in propertyInfos) {

                        IEnumerable<Attribute> fieldAttrs = property.GetCustomAttributes();
                        foreach (Attribute fieldAttr in fieldAttrs) {

                            if (fieldAttr is SCII_XT_Column column) {

                                NewAttribute(eachValue,
                                             property,
                                             data,
                                             column.Name ?? property.Name,
                                             column.Type);
                            }
                        }
                    }

                    value.Add(eachValue);
                }
                productInfos.attributeList.Add(new() {
                    attributeName = $"{_mission.name}_拧紧数据",
                    attributeCode = $"{_mission.name}_Screw",
                    attributeUnit = "json",
                    attributeType = 2,
                    value = JsonConvert.SerializeObject(value),
                });
                req.productInfos.Add(productInfos);

                var dto = await Workflow_SCII_XT.BindProductData(req);
                if (!dto.bindSuccess) {
                    logger.Warn($"数据上传 MES 失败！[任务（配方)：{_mission.name}, 产品条码：{operationDataDTOs[0].vin_number}] 错误信息：{dto.message}");
                } else {
                    logger.Info($"数据上传 MES 成功！[任务（配方)：{_mission.name}, 产品条码：{operationDataDTOs[0].vin_number}] 。");
                }

                _operationDataDTOs = new();
            }
        }

        private async Task PlcStatusTask() {
            _plcLoopCts = new CancellationTokenSource();
            var token = _plcLoopCts.Token;

            logger.Info("PLC 状态轮询任务已启动");

            // 一次性验证所有静态依赖
            if (_communicationTask == null) {
                logger.Error("PLC 通信任务未初始化，无法启动轮询任务");
                return;
            }

            if (_communicationTask.CommunicationType is not CommunicationModBusTcp) {
                logger.Error("当前通信类型不是 Modbus TCP，无法启动轮询任务");
                return;
            }

            var plcClient = _communicationTask.PlcTcpClient as SCII_XT_PlcClient;
            if (plcClient == null) {
                logger.Error("PLC 客户端为空或类型不匹配，无法启动轮询任务");
                return;
            }

            // 高效循环：只做核心业务
            while (!token.IsCancellationRequested) {
                try {
                    if (await plcClient.IsReadyToWrite()) {
                        bool result = !_activated && _missionRecord != null &&
                                      _missionRecord.mission_result == (int) TighteningStatus.OK;
                        await plcClient.WriteResult(result);
                    }

                    await Task.Delay(200, token);
                } catch (OperationCanceledException) {
                    logger.Info("PLC 状态轮询任务已取消");
                    break;
                } catch (Exception ex) {
                    logger.Warn($"PLC 读写周期失败: {ex.Message}", ex);
                    await Task.Delay(1000, token);
                }
            }
        }

        protected override void InitSerialPortTasks(KeyValuePair<int, SerialPortTask> pair) {
            SerialPortTask serialPortTask = pair.Value;
            serialPortTask.ActionAfterDataReceived = async msg => {
                await Task.Run(() => {
                    this.SafeInvoke(async () => {
                        if (!IsDisposed) {
                            // Check barcode interceptor first (for arranger open-lid scan popup)
                            if (ActiveBarcodeInterceptor != null) {
                                DeviceSerialPortDTO serialDto = _serialPorts.Single(dto => dto.id == pair.Key);
                                if (serialDto.invalid_char != null) {
                                    msg = String.Concat(msg.Where(c => !serialDto.invalid_char.Contains(c)));
                                }
                                if (ActiveBarcodeInterceptor(msg))
                                    return;
                            }

                            DeviceIoDTO? deviceIoDTO = _ioBoxes.SingleOrDefault(dto => dto.barcode == msg);
                            if (deviceIoDTO != null) {
                                IoBoxTask ioBoxTask = _ioBoxTasks[MainUtils.GetTCPClientKey(deviceIoDTO.ip, deviceIoDTO.port)];
                                if (ioBoxTask != null) {
                                    IoBoxTypeArranger? arrangerType = ioBoxTask.ArrangerType;
                                    if (arrangerType != null
                                        && deviceIoDTO.open_pos != null
                                        && deviceIoDTO.open_pos > 0
                                        && deviceIoDTO.open_pos <= IoBoxArranger.max) {
                                        int?[] pos = { null, null, null, null, null, null, null, null };
                                        pos[deviceIoDTO.open_pos.Value - 1] = 1;
                                        arrangerType.OpenDoor(pos);
                                        await Task.Delay(200);
                                        arrangerType.Reset();
                                    }
                                }
                            } else {
                                DeviceSerialPortDTO dto = _serialPorts.Single(dto => dto.id == pair.Key);
                                // 如果有空的数据进来，则跳过
                                if (string.IsNullOrEmpty(msg) || string.IsNullOrWhiteSpace(msg)) {
                                    logger.Warn("Message is null from serial port device, please check.");
                                    return;
                                }
                                if (dto.invalid_char != null) {
                                    msg = String.Concat(msg.Where(c => !dto.invalid_char.Contains(c)));
                                }

                                if (_barcodeDialog != null && !_barcodeDialog.IsDisposed) {
                                    _barcodeDialog.TextBox.GetTextBox(0).Text = msg;
                                    ProcessSecondBarCode();
                                }
                                // 交给弹窗处理
                                else if (_barCodePopUpForm == null || _barCodePopUpForm.IsDisposed) {
                                    OpenBarCodePopUpForm(msg);
                                } else {
                                    _barCodePopUpForm.ValidateBarCode(msg);
                                }
                            }
                        }
                    });
                });
            };
        }

        public void ProcessSecondBarCode() {
            if (_barcodeDialog != null) {
                string barcode = _barcodeDialog.TextBox.GetTextBox(0).Text;
                var config = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();

                // Check barcode length if configured
                if (config.enabled_second == (int) YesOrNo.YES && config.second_barcode_length > 0) {
                    if (barcode.Length != config.second_barcode_length) {
                        WidgetUtils.ShowWarningPopUp($"条码长度不匹配！当前长度为 {barcode.Length}，要求长度为 {config.second_barcode_length}。");
                        return; // Do not close dialog or reset state
                    }
                }

                _ = SendQRCodeToPrinter(barcode);
                _barcodeDialog.SignalComplete();
                _barcodeDialog.Dispose();

                _barcodeDialog = null;
                _canReceiveBarcode = false;
            }
        }

        // Send bit position to setter selector
        protected override async void SendSignalToSetterSelector(BoltButton boltButton) {
            await Task.Run(() => {
                BeginInvoke(() => {
                    Task.Run(() => {
                        ProductBoltDTO boltDTO = boltButton.BoltDTO;
                        if (boltDTO.bit_specification != null && boltDTO.bit_specification > 0) {
                            DeviceIoDTO ioDto = _ioBoxes.Single(box => box.id == boltDTO.setter_selector_id);
                            IoBoxTypeSetterSelector? setterSelectorType = _ioBoxTasks[MainUtils.GetTCPClientKey(ioDto.ip, ioDto.port)].SetterSelectorType;
                            if (setterSelectorType != null) {
                                _bitPositionOk = false;

                                if (setterSelectorType is IoBoxTypeSetterSelectorPlus setterSelectorPlus) {
                                    _bitPositionTimedOut = false;
                                    boltButton.SendSignalToSetterSelectorPlus(boltDTO.bit_specification.Value, setterSelectorPlus, isOk => {
                                        _bitPositionOk = isOk;
                                        _updateCounter((int) boltDTO.bit_specification.Value, isOk);
                                    });
                                } else {
                                    boltButton.SendSignalToSetterSelector(boltDTO.bit_specification.Value, setterSelectorType, (isOk, isTimedOut) => {
                                        _bitPositionOk = isOk;
                                        _bitPositionTimedOut = isTimedOut;
                                        _updateCounter((int) boltDTO.bit_specification.Value, isOk);
                                    });
                                }
                            }
                        } else {
                            _bitPositionOk = null;
                            _bitPositionTimedOut = false;
                        }
                    });
                });
            });

            void _updateCounter(int bitPosition, bool isOk) {
                if (isOk && _screwBitCounterBoxes.ContainsKey(bitPosition)) {
                    int counts = int.Parse(_screwBitCounterBoxes[bitPosition].GetTextBox(0).Box.Text);

                    _screwBitCounterBoxes[bitPosition].GetTextBox(0).Box.Text = ++counts + "";

                    if (_screwBitCounterDtos.Count > 0) {
                        ScrewBitCounterDTO dto = _screwBitCounterDtos[bitPosition];
                        dto.current_counts = counts;
                        _apis.AddOrUpdateScrewBitCounter(new(dto));
                    }
                }
            }
        }

        protected override void DoAfterTighteningOk() {
            int counts = int.Parse(_screwBitCounterBoxes[-1].GetTextBox(0).Box.Text);
            _screwBitCounterBoxes[-1].GetTextBox(0).Box.Text = ++counts + "";
            ScrewBitCounterDTO dto = _screwBitCounterDtos[-1];
            dto.current_counts = counts;
            _apis.AddOrUpdateScrewBitCounter(new(dto));
        }

        protected override Task<bool> CheckScrewBitCount() => Task.FromResult(true);

        protected override void HandleScrewBitCounter() {
            if (_topRightBottom == null) {
                return;
            }

            if (_screwBitCounterBoxes != null && _screwBitCounterBoxes.Count > 0) {
                foreach (var pair in _screwBitCounterBoxes) {
                    _topRightBottom.Controls.Remove(_screwBitCounterBoxes[pair.Key]);
                }
            }

            _screwBitCounterBoxes = new();
            _screwBitCounterDtos = new();

            screwBitCounterDTOsCached = _apis.FindScrewBitCounterByMissionId(new(_mission.id)).ScrewBitCounterDTOs;
            if (screwBitCounterDTOsCached.Count > 0) {
                for (int i = 0; i < screwBitCounterDTOsCached.Count; i++) {
                    ScrewBitCounterDTO dto = screwBitCounterDTOsCached[i];
                    if (_screwBitCounterDtos.ContainsKey(dto.bit_position)) {
                        continue;
                    }
                    if (i >= 4) { // More than 4 are not supported.
                        break;
                    }
                    _screwBitCounterDtos.Add(dto.bit_position, dto);

                    CustomTextBoxGroup boxGroup = new("批头计数" + dto.bit_position) {
                        ReadOnly = true,
                        Enabled = false,
                        NameAlignment = HorizontalAlignment.Right,
                        Ratio = 6.8,
                    };
                    boxGroup.GetTextBox(0).Box.Text = dto.current_counts > 0 ? dto.current_counts + "" : "0";

                    _screwBitCounterBoxes.Add(dto.bit_position, boxGroup);
                    _topRightBottom.Controls.Add(boxGroup);
                }
            } else {
                CustomTextBoxGroup boxGroup = new("批头计数") {
                    ReadOnly = true,
                    Enabled = false,
                    NameAlignment = HorizontalAlignment.Right,
                    Ratio = 6.8,
                };
                boxGroup.GetTextBox(0).Box.Text = "0";

                _screwBitCounterBoxes.Add(-1, boxGroup);
                _topRightBottom.Controls.Add(boxGroup);

                ScrewBitCounterDTO dto = new() {
                    mission_id = _mission.id,
                    bit_position = -1,
                    count_each_time = 1,
                    current_counts = 0,
                };
                _apis.AddOrUpdateScrewBitCounter(new(dto));
                _screwBitCounterDtos.Add(dto.bit_position, dto);
            }

            _productSumPerDay.Ratio = 6.8;
            _okSumPerDay.Ratio = 6.8;
            _ngRatePerDay.Ratio = 6.8;
            _pset.Ratio = 6.8;

            _missionSelectedName.Ratio = 8.425;
            _productBatch.Ratio = 8.425;
        }

        protected override void ResizeOuters(int boxHeight, int titleHeight, int contentVPadding) {
            int extraHeightTopRightBottom = 0;
            if (_screwBitCounterBoxes.Count > 0 && _screwBitCounterBoxes.Count <= 2) {
                extraHeightTopRightBottom += boxHeight + contentVPadding;
            } else if (_screwBitCounterBoxes.Count > 2) {
                extraHeightTopRightBottom += boxHeight * 2 + contentVPadding * 2;
            }

            int padding = Padding.Left / 2;
            int workplaceWidth = Width - Padding.Left * 2;
            int workplaceHeight = Height - Padding.Top * 2;
            int barCodeHeight = (int) (workplaceHeight * WidgetUtils.WorkplaceBarCodeHeightRatio());
            int imagePanelHeight = (int) (workplaceHeight * WidgetUtils.WorkplaceImagePanelHeightRatio());
            imagePanelHeight += extraHeightTopRightBottom;
            int topHeight = barCodeHeight + imagePanelHeight + padding;
            int bottomHeight = (int) (workplaceHeight * .045);
            int middleHeight = workplaceHeight - topHeight - bottomHeight - padding * 2; // 为了取整
            int topLeftWidth = (int) (workplaceWidth * WidgetUtils.WorkplaceLeftWidthRatio());
            int topRightWidth = workplaceWidth - topLeftWidth - padding;
            int topRightTopHeight = titleHeight + boxHeight + contentVPadding * 2;
            int topRightBottomHeight = titleHeight + boxHeight * 4 + contentVPadding * 5;
            topRightBottomHeight += extraHeightTopRightBottom;
            int topRightMiddleHeight = topHeight - topRightTopHeight - topRightBottomHeight - padding * 2;
            int topRightMiddleLeftWidth = (int) (topRightWidth * .55);
            int topRightMiddleRightWidth = topRightWidth - topRightMiddleLeftWidth - padding;

            // 上方
            _top.Size = new(workplaceWidth, topHeight);
            _top.Margin = new(0, 0, 0, padding);
            // 上方左边
            _topLeft.Size = new(topLeftWidth, topHeight);
            _topLeft.Margin = new(0, 0, padding, 0);
            // 上方左边上面
            _barCodeOuter.Size = new(topLeftWidth, barCodeHeight);
            _barCodeOuter.Margin = new(0, 0, 0, padding);
            // 上方左边下面
            _imageDisplayOuter.Size = new(topLeftWidth, imagePanelHeight);
            // 上方右边
            _topRight.Size = new(topRightWidth, topHeight);
            // 上方右边的上面
            _topRightTop.Size = new(topRightWidth, topRightTopHeight);
            _topRightTop.Margin = new(0, 0, 0, padding);
            // 上方右边的中间
            _topRightMiddle.Size = new(topRightWidth, topRightMiddleHeight);
            _topRightMiddle.Margin = new(0, 0, 0, padding);
            // 上方右边的中间的左边
            _topRightMiddleLeft.Size = new(topRightMiddleLeftWidth, topRightMiddleHeight);
            _topRightMiddleLeft.Margin = new(0, 0, padding, 0);
            // 上方右边的中间的右边
            _topRightMiddleRight.Size = new(topRightMiddleRightWidth, topRightMiddleHeight);
            // 上方右边的下面
            _topRightBottom.Size = new(topRightWidth, topRightBottomHeight);

            // 中间
            _middle.Size = new(workplaceWidth, middleHeight);
            _middle.Margin = new(0, 0, 0, padding);

            // 下方
            _bottom.Size = new(workplaceWidth, bottomHeight);
            _bottom.Padding = new(0, 0, 1, 0);
        }

        // 计算尺寸： 任务信息框
        protected override void ResizeTopRightBottom(int boxHeight, int titleHeight, int contentVPadding,
                int contentHPadding, Font titleFont) {
            base.ResizeTopRightBottom(boxHeight, titleHeight, contentVPadding, contentHPadding, titleFont);

            int boxWidth = (_operatorInfoTitle.Parent.Width - contentHPadding * 3) / 2;
            foreach (KeyValuePair<int, CustomTextBoxGroup> pair in _screwBitCounterBoxes) {
                CustomTextBoxGroup boxGroup = pair.Value;
                boxGroup.Size = new(boxWidth, boxHeight);
                boxGroup.Margin = new(contentHPadding, contentVPadding, 0, 0);
            }
        }

        protected override void OpenBarCodePopUpForm(string? barCode = null) {
            string batchNum = "";
            if (!_activated) {
                batchNum = _productBatch.GetTextBox(0).Box.Text;
                if (string.IsNullOrEmpty(batchNum)) {
                    WidgetUtils.ShowErrorPopUp("批次号还未配置");
                    if (_barCodePopUpForm != null && !_barCodePopUpForm.IsDisposed) {
                        _barCodePopUpForm.Hide();
                    }
                    _productBatch.GetTextBox(0).IsError = true;
                    _productBatch.GetTextBox(0).Box.Focus();
                    return;
                }
            }

            if (_barCodePopUpForm == null || _barCodePopUpForm.IsDisposed) {
                if (_activated && _currentWorkingBolt != null) {
                    _rulesExcluded = GetCurrentExcludedRules(_currentWorkingBolt.BoltDTO);
                } else {
                    _rulesExcluded = GetCurrentExcludedRules();
                }

                _barCodePopUpForm = new BarCodeInputPopUpForm_SCII_XT(this,
                                                                      ConfigsVariables.BAR_CODE_NOTE,
                                                                      _mission,
                                                                      _activated,
                                                                      _productBarCodeMatchingRules,
                                                                      _partsBarCodeMatchingRules,
                                                                      barCode,
                                                                      _rulesExcluded,
                                                                      CheckLockMsg(WorkingProcessPanel.LockedBoltBarCode),
                                                                      batchNum,
                                                                      _inBoundStationOk) {
                    Title = "录入条码",
                    BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
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
                                // ActivateMission();
                                // _barCodePopUpForm.Dispose();
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

        // Load Communication tasks
        private void LoadPlc() {
            _communicationTasks = MainUtils.CommunicationTasks;
            foreach (CommunicationTask task in _communicationTasks.Values) {
                if (task.CommunicationType is CommunicationModBusTcp) {
                    _communicationTask = task;
                    break;
                }
            }

            if (_communicationTask != null) {
                // Close first if exists, because we need a new one each time
                if (_communicationTask.PlcTcpClient != null) {
                    StopPlcStatusTask();
                    _communicationTask.PlcTcpClient.Dispose();
                    _communicationTask.PlcTcpClient = null;
                }

                try {
                    _communicationTask.PlcTcpClient = new SCII_XT_PlcClient(_communicationTask.Ip, _communicationTask.Port);
                    _ = PlcStatusTask();
                } catch (InvalidOperationException ioe) {
                    logger.Error("Error while connecting to PLC", ioe);
                    WidgetUtils.ShowWarningPopUp($"连接 PLC 失败！{ioe.Message}");
                } catch (Exception ex) {
                    logger.Error("Error while connecting to PLC", ex);
                    WidgetUtils.ShowWarningPopUp("连接 PLC 失败！请检查配置/网络。");
                }
            }
        }

        private async Task _loopingToCheckBatchNo() {
            await Task.Run(() => {
                BeginInvoke(async () => {
                    string batchNo = ConfigUtils.LoadConfig<SciiXtConfig>().batch_no;
                    while (string.IsNullOrEmpty(batchNo)) {
                        _productBatch.GetTextBox(0).IsError = true;

                        await Task.Delay(1000);

                        // Fetch again
                        batchNo = ConfigUtils.LoadConfig<SciiXtConfig>().batch_no;
                    }

                    _productBatch.GetTextBox(0).IsError = false;
                    _productBatch.GetTextBox(0).Box.Text = batchNo;
                });
            });
        }

        private string _getProcedureCode() {
            string procedureCode = ConfigUtils.LoadConfig<SciiXtConfig>().procedure_code;
            if (string.IsNullOrEmpty(procedureCode)) {
                WidgetUtils.ShowWarningPopUp(this, "【工序编码】未配置，请检查配置信息。");
            }
            return procedureCode;
        }

        private string _getEquipmentCode() {
            string equipmentCode = ConfigUtils.LoadConfig<SciiXtConfig>().equipment_code;
            if (string.IsNullOrEmpty(equipmentCode)) {
                WidgetUtils.ShowWarningPopUp(this, "【设备编码】未配置，请检查配置信息。");
            }
            return equipmentCode;
        }

        private string _getBatchNo() {
            string batchNo = ConfigUtils.LoadConfig<SciiXtConfig>().batch_no;
            if (string.IsNullOrEmpty(batchNo)) {
                WidgetUtils.ShowWarningPopUp("【批次号】未配置，请检查配置信息。");
            }
            return batchNo;
        }

        private string _getRecipeCode() {
            string recipeCode = ConfigUtils.LoadConfig<SciiXtConfig>().recipe_code;
            if (string.IsNullOrEmpty(recipeCode)) {
                WidgetUtils.ShowWarningPopUp(this, "【配方编码】未配置，请检查配置信息。");
            }
            return recipeCode;
        }

        private void NewAttribute(Dictionary<string, object> eachValue,
                                  PropertyInfo property,
                                  OperationDataDTO_SCII_XT data,
                                  string attrName,
                                  SCII_XT_ColumnType? type) {
            object? propertyValue = property.GetValue(data);
            string value = propertyValue?.ToString() ?? "NULL";

            // 处理枚举转换
            if (value != "NULL" && propertyValue is int intValue) {
                if (type == SCII_XT_ColumnType.RESULT) {
                    if (Enum.IsDefined(typeof(TighteningCommonStatus), intValue)) {
                        value = ((TighteningCommonStatus) intValue).ToString();
                    } else {
                        value = TighteningCommonStatus.OK.ToString();
                    }
                } else if (type == SCII_XT_ColumnType.FINAL_RESULT) {
                    if (Enum.IsDefined(typeof(TighteningStatus), intValue)) {
                        value = ((TighteningStatus) intValue).ToString();
                    } else {
                        value = TighteningStatus.OK.ToString();
                    }
                }
            }

            eachValue.Add(attrName, value);
        }

        private void StopPlcStatusTask() {
            _plcLoopCts?.Cancel();
            _plcLoopCts?.Dispose();
            _plcLoopCts = null;
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                // 停止 PLC 轮询任务
                StopPlcStatusTask();

                // 如果有其他需要释放的资源，也在这里释放
                // 例如：_plcLoopCts 已在 StopPlcStatusTask() 中处理

                // 取消 actions 绑定
                SciiXtController.ActionAfterReceivedRecipe -= ActionAfterReceivedRecipe;
                SciiXtController.ActionAfterReceivedBatch -= ActionAfterReceivedBatch;
            }

            base.Dispose(disposing);
        }
    }
}
