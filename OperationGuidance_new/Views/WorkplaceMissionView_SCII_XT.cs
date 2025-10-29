using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using Newtonsoft.Json;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.HttpObjects;
using OperationGuidance_new.HttpObjects.Requests.SCII_XT;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Tasks.DeviceTypes;
using OperationGuidance_new.Utils;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;

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
        public WorkplaceMissionView_SCII_XT View { get => _view; set => _view = value; }

        private Dictionary<int, CustomTextBoxGroup> _screwBitCounterBoxes;
        private Dictionary<int, ScrewBitCounterDTO> _screwBitCounterDtos;

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

        public override async Task TerminateMission(WorkplaceProcessStatus status) {
            if (await OutBound()) {
                await base.TerminateMission(status);

                SwitchMissionByRecipe(_getRecipeCode());
            }
        }

        private async Task<bool> OutBound() {
            SCII_XT_InOrOutBoundStationReq req = new() {
                productCode = _missionRecord.product_bar_code,
                passType = (int) SCII_XT_ProductType.PRODUCT,
                recipeCode = _mission.name,
                procedureCode = _getProcedureCode(),
                equipmentCode = _getEquipmentCode(),
                batchNo = _missionRecord.product_batch,
            };

            var dto = await Workflow_SCII_XT.OutBoundStation(req);
            if (!dto.inOrOutSuccess) {
                logger.Warn($"出站失败，详细信息：{dto.message}");
                if (WidgetUtils.ShowConfirmPopUp($"进站请求失败！点击【是】重试。\n\n详细信息：{dto.message}")) {
                    return await OutBound();
                }
            }

            return true;
        }

        protected override void InitializeAfterHandelCreated() {
            base.InitializeAfterHandelCreated();

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
                    BeginInvoke(() => {
                        SwitchToMission(rsp.ProductMissionDTO);
                    });
                } else {
                    WidgetUtils.ShowWarningPopUp($"未找到配方[{recipeCode}]对应的任务，请检查是否配置了该任务。");
                }
            }
        }

        private void ActionAfterReceivedRecipe(string recipeCode) {
            SwitchMissionByRecipe(recipeCode);
        }

        private void ActionAfterReceivedBatch(string batchNo) {
            BeginInvoke(() => {
                _productBatch.GetTextBox(0).Box.Text = batchNo;
            });
        }

        protected override void StoreTighteningData(OperationDataDTO operationDataDTO) {
            logger.Info("StoreTighteningData start ........");

            // Use task to store data asynchronously
            StoreDataToDatabase(operationDataDTO);

            // 将数据发送给 MES
            _ = SendDataToMES(operationDataDTO);

            // 先将VOs加入到实时显示数据列表中
            OperationDataVO dataFormatted = new();
            CommonUtils.ObjectConverter<OperationDataDTO, OperationDataVO>(operationDataDTO, dataFormatted);
            _tighteningDataVOs.Add(dataFormatted);

            RefreshTighteningDataPanel(_tighteningDataVOs);
            logger.Info("StoreTighteningData showing to panel end ........");

            // 最后再存进本地文件
            StoreDataToFiles(operationDataDTO);

            logger.Info("StoreTighteningData end ........");
        }

        private async Task SendDataToMES(OperationDataDTO operationDataDTO) {
            var data = new OperationDataDTO_SCII_XT();
            CommonUtils.ObjectConverter<OperationDataDTO, OperationDataDTO_SCII_XT>(operationDataDTO, data);

            SCII_XT_BindProductDataReq req = new() {
                bingType = (int) SCII_XT_ProductType.PRODUCT,
                productInfos = new(),
                procedureCode = _getProcedureCode(),
                recipeCode = _mission.name,
            };
            SCII_XT_BindProductDataReq.ProductInfo productInfos = new() {
                productCode = operationDataDTO.vin_number,
                attributeList = new(),
            };
            productInfos.attributeList.Add(new() {
                value = JsonConvert.SerializeObject(data),
            });
            req.productInfos.Add(productInfos);

            var dto = await Workflow_SCII_XT.BindProductData(req);
            if (!dto.bindSuccess) {
                logger.Warn($"数据上传 MES 失败！[任务（配方)：{_mission.name}，点位：{data.bolt_serial_num}] 错误信息：{dto.message}");
            }
        }

        protected override void InitSerialPortTasks(KeyValuePair<int, SerialPortTask> pair) {
            SerialPortTask serialPortTask = pair.Value;
            serialPortTask.ActionAfterDataReceived = async msg => {
                await Task.Run(() => {
                    BeginInvoke(() => {
                        if (!IsDisposed) {
                            DeviceIoDTO? deviceIoDTO = _ioBoxes.SingleOrDefault(dto => dto.barcode == msg);
                            if (deviceIoDTO != null) {
                                IoBoxTask ioBoxTask = _ioBoxTasks[MainUtils.GetTCPClientKey(deviceIoDTO.ip, deviceIoDTO.port)];
                                if (ioBoxTask != null) {
                                    IoBoxTypeArranger? arrangerType = ioBoxTask.ArrangerType;
                                    if (arrangerType != null) {
                                        arrangerType.OpenDoor();
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

                                // 交给弹窗处理
                                if (_barCodePopUpForm == null || _barCodePopUpForm.IsDisposed) {
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

        protected override void InitializeTopRightBottom() {
            base.InitializeTopRightBottom();

            _screwBitCounterBoxes = new();
            _screwBitCounterDtos = new();

            List<ScrewBitCounterDTO> screwBitCounterDTOs = _apis.FindScrewBitCounterByMissionId(new(_mission.id)).ScrewBitCounterDTOs;
            if (screwBitCounterDTOs.Count > 0) {
                for (int i = 0; i < screwBitCounterDTOs.Count; i++) {
                    ScrewBitCounterDTO dto = screwBitCounterDTOs[i];
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
            int padding = Padding.Left / 2;
            int workplaceWidth = Width - Padding.Left * 2;
            int workplaceHeight = Height - Padding.Top * 2;
            int barCodeHeight = (int) (workplaceHeight * WidgetUtils.WorkplaceBarCodeHeightRatio());
            int imagePanelHeight = (int) (workplaceHeight * WidgetUtils.WorkplaceImagePanelHeightRatio());
            int topHeight = barCodeHeight + imagePanelHeight + padding;
            int bottomHeight = (int) (workplaceHeight * .045);
            int middleHeight = workplaceHeight - topHeight - bottomHeight - padding * 2; // 为了取整
            int topLeftWidth = (int) (workplaceWidth * WidgetUtils.WorkplaceLeftWidthRatio());
            int topRightWidth = workplaceWidth - topLeftWidth - padding;
            int topRightTopHeight = titleHeight + boxHeight + contentVPadding * 2;
            int topRightBottomHeight = titleHeight + boxHeight * 4 + contentVPadding * 5;
            if (_screwBitCounterBoxes.Count > 0 && _screwBitCounterBoxes.Count <= 2) {
                topRightBottomHeight += boxHeight + contentVPadding;
            } else if (_screwBitCounterBoxes.Count > 2) {
                topRightBottomHeight += boxHeight * 2 + contentVPadding * 2;
            }
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
                                                                      batchNum) {
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

        private async Task _loopingToCheckBatchNo() {
            await Task.Run(() => {
                BeginInvoke(async () => {
                    string batchNo = MainUtils.Config_SCII_XT.Read(ConfigName_SCII_XT.BatchNo);
                    while (string.IsNullOrEmpty(batchNo)) {
                        _productBatch.GetTextBox(0).IsError = true;

                        await Task.Delay(1000);

                        // Fetch again
                        batchNo = MainUtils.Config_SCII_XT.Read(ConfigName_SCII_XT.BatchNo);
                    }

                    _productBatch.GetTextBox(0).IsError = false;
                    _productBatch.GetTextBox(0).Box.Text = batchNo;
                });
            });
        }

        private string _getProcedureCode() {
            string procedureCode = MainUtils.Config_SCII_XT.Read(ConfigName_SCII_XT.ProcedureCode);
            if (string.IsNullOrEmpty(procedureCode)) {
                WidgetUtils.ShowWarningPopUp(this, "【工序编码】未配置，请检查配置信息。");
            }
            return procedureCode;
        }

        private string _getEquipmentCode() {
            string equipmentCode = MainUtils.Config_SCII_XT.Read(ConfigName_SCII_XT.EquipmentCode);
            if (string.IsNullOrEmpty(equipmentCode)) {
                WidgetUtils.ShowWarningPopUp(this, "【设备编码】未配置，请检查配置信息。");
            }
            return equipmentCode;
        }

        private string _getBatchNo() {
            string batchNo = MainUtils.Config_SCII_XT.Read(ConfigName_SCII_XT.BatchNo);
            if (string.IsNullOrEmpty(batchNo)) {
                WidgetUtils.ShowWarningPopUp("【批次号】未配置，请检查配置信息。");
            }
            return batchNo;
        }

        private string _getRecipeCode() {
            string recipeCode = MainUtils.Config_SCII_XT.Read(ConfigName_SCII_XT.RecipeCode);
            if (string.IsNullOrEmpty(recipeCode)) {
                WidgetUtils.ShowWarningPopUp(this, "【配方编码】未配置，请检查配置信息。");
            }
            return recipeCode;
        }
    }
}
