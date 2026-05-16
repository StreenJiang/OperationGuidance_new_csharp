using CustomLibrary.Buttons;
using CustomLibrary.Forms;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using log4net;
using Newtonsoft.Json;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.SubViews;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views.AbstractViews {
    public abstract class ABarCodeInputPopUpForm: CustomPopUpForm {
        protected ILog logger;

        protected AWorkplaceContentPanel _workplace;
        protected string _initStr;
        protected ProductMissionDTO _mission;
        protected Dictionary<int, List<BarCodeMatchingRuleDTO>> _productBarCodeRules;
        protected Dictionary<int, List<BarCodeMatchingRuleDTO>> _partsBarCodeRules;
        protected int _partsIndex = 1;
        protected CustomTextBoxButtonGroup _productBarCodeBox;
        protected CustomTextBoxButtonGroup? _focusedBox;
        protected TitlePanel _productBarCodeTitle;
        protected CustomContentPanel _productBarCodeContentPanel;
        protected TitlePanel _partsBarCodeTitle;
        protected CustomContentPanel _partsBarCodeContentPanel;
        protected List<BarCodeMatchingRuleDTO> _rulesExcluded;
        protected string? _barCode;
        protected bool _isForBolt = false;

        public CustomTextBoxButtonGroup ProductBarCodeBox { get => _productBarCodeBox; set => _productBarCodeBox = value; }
        public CustomContentPanel PartsBarCodeContentPanel { get => _partsBarCodeContentPanel; set => _partsBarCodeContentPanel = value; }
        public List<BarCodeMatchingRuleDTO> RulesExcluded => _rulesExcluded;

        public ABarCodeInputPopUpForm(AWorkplaceContentPanel workplace, string initStr, ProductMissionDTO mission, bool activated,
                Dictionary<int, List<BarCodeMatchingRuleDTO>> productBarCodeRules,
                Dictionary<int, List<BarCodeMatchingRuleDTO>> partsBarCodeRules, string? barCode,
                List<BarCodeMatchingRuleDTO> rulesExcluded, bool isForBolt) : base() {
            logger = MainUtils.GetLogger(this.GetType());
            DoubleBuffered = true;

            _workplace = workplace;
            _initStr = initStr;
            _mission = mission;
            _productBarCodeRules = productBarCodeRules;
            _partsBarCodeRules = partsBarCodeRules;
            _barCode = barCode;
            _rulesExcluded = rulesExcluded;
            _isForBolt = isForBolt;

            _productBarCodeTitle = new("产品条码") {
                Parent = ContentPanel,
            };
            _productBarCodeContentPanel = new() {
                Parent = ContentPanel,
            };
            _partsBarCodeTitle = new("物料条码") {
                Parent = ContentPanel,
            };
            _partsBarCodeContentPanel = new() {
                Parent = ContentPanel,
            };

            _productBarCodeBox = AddProductBarCodeTextBox(); // 产品码/追溯码框
            AddPartsBoxes(_mission.id);
            FillExistingData();
        }

        // 根据任务ID找到其对应的物料码匹配规则，并根据此规则添加相应的输入框
        protected virtual void AddPartsBoxes(int missionId) {
            if (missionId > 0 && _partsBarCodeRules.ContainsKey(missionId)) {
                List<BarCodeMatchingRuleDTO> rulesTemp = _partsBarCodeRules[missionId];

                // Ignore excluded rules
                if (_rulesExcluded.Count > 0) {
                    rulesTemp = rulesTemp.Where(rule => _workplace.BarCodeObj.PartsMatchingRulesCached.Contains(rule.id) || !_rulesExcluded.Any(r => r.id == rule.id)).ToList();
                }

                // Need to check the order
                List<BarCodeMatchingRuleDTO> rulesTempNewOrder = new();
                foreach (int id in _workplace.BarCodeObj.PartsMatchingRulesCached) {
                    BarCodeMatchingRuleDTO? ruleTemp = rulesTemp.SingleOrDefault(rule => rule.id == id);
                    if (ruleTemp != null) {
                        rulesTempNewOrder.Add(ruleTemp);
                    }
                }
                rulesTempNewOrder.AddRange(rulesTemp.Where(rule => !_workplace.BarCodeObj.PartsMatchingRulesCached.Contains(rule.id)));

                // Add text boxes according to remaining rules
                for (int i = 0; i < rulesTempNewOrder.Count(); i++) {
                    AddPartsBarCodeTextBox(rulesTempNewOrder[i].name);
                }
            }
        }
        // 根据当前缓存的条码数据，回填到弹窗里的所有输入框
        private void FillExistingData() {
            _productBarCodeBox.SetValue(0, _workplace.BarCodeObj.ProductBarCode);
            for (int i = 0; i < _workplace.BarCodeObj.PartsBarCodes.Count; i++) {
                ((CustomTextBoxButtonGroup) _partsBarCodeContentPanel.Controls[i]).SetValue(0, _workplace.BarCodeObj.PartsBarCodes[i]);
            }
            if (!_workplace.Activated) {
                if (_mission.id <= 0 || string.IsNullOrEmpty(_workplace.BarCodeObj.ProductBarCode)) {
                    _productBarCodeBox.Enabled = true;
                    _productBarCodeBox.GetTextBox(0).Box.Focus();
                    ActiveControl = _productBarCodeBox.GetTextBox(0).Box;
                } else if (_partsBarCodeRules.ContainsKey(_mission.id) && _partsBarCodeRules[_mission.id].Count > 0) {
                    int okCount = _workplace.BarCodeObj.PartsBarCodes.Count;
                    if (okCount < _partsBarCodeRules[_mission.id].Count) {
                        CustomTextBoxButtonGroup focusingBox = (CustomTextBoxButtonGroup) _partsBarCodeContentPanel.Controls[okCount];
                        focusingBox.Enabled = true;
                        focusingBox.GetTextBox(0).Box.Focus();
                        ActiveControl = focusingBox.GetTextBox(0).Box;
                    }
                }
            } else {
                if (_workplace.BarCodeObj.PartsBarCodes.Count < _partsBarCodeContentPanel.Controls.Count) {
                    CustomTextBoxButtonGroup focusingBox = (CustomTextBoxButtonGroup) _partsBarCodeContentPanel.Controls[_workplace.BarCodeObj.PartsBarCodes.Count];
                    focusingBox.Enabled = true;
                    focusingBox.GetTextBox(0).Box.Focus();
                    ActiveControl = focusingBox.GetTextBox(0).Box;
                }
            }
        }
        // 切换任务
        private void SwitchToMission(ProductMissionDTO mission) {
            logger.Info($"Switching mission [id = {_mission.id}] to new mission [id = {mission.id}]...");

            _mission = mission;
            _workplace.SwitchToMission(mission);
            _partsIndex = 1;

            // Back fill product bar code
            _productBarCodeBox.SetValue(0, _workplace.BarCodeObj.ProductBarCode);

            // Clear all text boxes of parts
            _partsBarCodeContentPanel.Controls.Clear();

            // Check excluded parts
            _rulesExcluded = _workplace.GetCurrentExcludedRules();

            // Add new text boxes of parts
            AddPartsBoxes(_mission.id);

            // Resize
            ResizeSelf();
        }
        // 添加产品条码输入框
        private CustomTextBoxButtonGroup AddProductBarCodeTextBox() {
            CustomTextBoxButtonGroup box = new($"产品条码") {
                Parent = _productBarCodeContentPanel,
                Ratio = 8.5,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            SetupBox(box);
            CommonButton btn = box.AddButton<CommonButton>("确定");
            btn.Click += (s, e) => ValidateProductBarCodeAsync();
            box.GetTextBox(0).Box.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    ValidateProductBarCodeAsync();
                }
            };
            return box;
        }
        // 添加物料条码输入框
        protected CustomTextBoxButtonGroup AddPartsBarCodeTextBox(string? name) {
            CustomTextBoxButtonGroup box;
            if (string.IsNullOrEmpty(name)) {
                box = new($"物料条码{_partsIndex++}");
            } else {
                _partsIndex++;
                box = new(name);
            }
            box.Parent = _partsBarCodeContentPanel;
            box.Ratio = 8.5;
            box.NameAlignment = HorizontalAlignment.Right;
            box.Enabled = false;
            SetupBox(box);
            CommonButton btn = box.AddButton<CommonButton>("确定");
            btn.Click += (s, e) => ValidatePartsBarCode(box);
            box.GetTextBox(0).Box.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    ValidatePartsBarCode(box);
                }
            };
            return box;
        }
        private void SetupBox(CustomTextBoxButtonGroup box) {
            CustomTextBox innerBox = box.GetTextBox(0);
            innerBox.TabStop = false;
            innerBox.TextChanged += (s, e) => {
                if (!string.IsNullOrEmpty(innerBox.Text) && innerBox.Text != _initStr) {
                    innerBox.IsError = false;
                }
            };
            TextBox iBox = innerBox.Box;
            iBox.GotFocus += (s, e) => {
                _focusedBox = box;
                if (iBox.Text == _initStr) {
                    iBox.Text = "";
                }
            };
            iBox.LostFocus += (s, e) => {
                if (iBox.Text == "") {
                    iBox.Text = _initStr;
                }
            };
        }
        private async void ValidateProductBarCodeAsync() {
            if (!_workplace.CheckErrorPromptForArmEnabled()) {
                return;
            }

            string barCode = _productBarCodeBox.GetTextBox(0).Box.Text;
            if (string.IsNullOrEmpty(barCode)) {
                WidgetUtils.ShowWarningPopUp($"请输入或扫描条码");
                _productBarCodeBox.GetTextBox(0).IsError = true;
                return;
            }
            // 对产品码进行校验
            bool checkPassed = true;
            ProductMissionDTO? mission = null;
            bool needToSwtich = false;
            // 已选任务
            if (_mission.id > 0) {
                logger.Info($"Already chosen mission, mission id = [{_mission.id}], barcode = [{barCode}]...");

                mission = _mission;

                // 校验不通过，检查是否匹配其他任务
                if (!CheckBarCodeMatchedMission(barCode)) {
                    logger.Info($"Validation fails for mission id = [{_mission.id}], barcode = [{barCode}], checking for other missions...");

                    // Add this because sometimes it will go inside this check after activated, no idea why, then add a check here 
                    if (_workplace.Activated) {
                        return;
                    }

                    CheckProductBarCodeErrorForChallenge();

                    logger.Info($"Finding another mission that matchs this mission id [{_mission.id}], barcode = [{barCode}]...");
                    mission = FindBarCodeMatchedMission(barCode);
                    // 没匹配到其他任务，不专门提示，只提示与当前任务不匹配
                    if (mission == null) {
                        logger.Info($"Can not find any other mission that match this barcode [{barCode}]...");

                        checkPassed = false;
                        _productBarCodeBox.GetTextBox(0).IsError = true;
                        ShowWrongBarcodeGate($"当前条码【{barCode}】与选择的任务不匹配");
                    }
                    // 如果匹配到其他任务，则做出特定提示
                    else {
                        logger.Info($"Found another mission that matches this barcode [{barCode}], asking the user switch or not...");
                        checkPassed = WidgetUtils.ShowConfirmPopUp($"检测到当前条码【{barCode}】与另一任务【{mission.name}】匹配，是否切换任务？");

                        // 不为空且如果确定切换，则需要切换任务
                        needToSwtich = checkPassed;
                    }
                }
            }
            // 没选任务
            else {
                logger.Info("Haven't chosen any mission, finding matching mission, barcode = [{barCode}]...");

                mission = FindBarCodeMatchedMission(barCode);
                // 匹配不到任何任务，则提示没匹配上
                if (mission == null) {
                    logger.Info($"Can not find any other mission that match this barcode [{barCode}]...");

                    CheckProductBarCodeErrorForChallenge();

                    checkPassed = false;
                    _productBarCodeBox.GetTextBox(0).IsError = true;
                    ShowWrongBarcodeGate($"没有检索到匹配条码【{barCode}】的任务");
                } else {
                    // 不为空则需要切换任务
                    needToSwtich = true;
                }
            }

            // 挑战任务校验挪到这里，需要先保证任务与条码的匹配是否成功才能进行挑战任务的校验
            if (checkPassed) {
                // 匹配到任务后需要再次检查挑战任务
                if (!_workplace.CheckChallengeMissionConfirmation()) {
                    return;
                }
            }

            if (checkPassed) {
                // Extra check
                checkPassed = ProductBarCodeExtraCheck(barCode);
            }

            // 条码校验通过，再检查下是否需要返工
            if (checkPassed) {
                mission = CommonUtils.CannotBeNull(mission);

                // 如果存在前置任务，则先查询前置任务是否完成
                if (mission.predecessor_mission_id != null) {
                    logger.Info($"Checking predecessor mission for mission id = [{mission.id}], predecessor_mission_id = [{mission.predecessor_mission_id}], barcode = [{barCode}]...");

                    CheckIfBarCodeExistsInMissionRecordRsp rsp = _workplace.Apis.CheckIfBarCodeExistsInMissionRecord(new(mission.predecessor_mission_id.Value, (int) TighteningStatus.OK) { ProductBarCode = barCode });
                    bool yes = rsp.Yes;

                    CheckProductPredecessorForChallenge(rsp.Yes, rsp.MissionRecordDTO?.create_time);

                    if (!yes) {
                        logger.Info($"Validation fails for predecessor mission, mission id = [{mission.id}], predecessor_mission_id = [{mission.predecessor_mission_id}], barcode = [{barCode}]...");

                        checkPassed = false;
                        ShowWrongBarcodeGate("未检测到前置任务的加工完成记录，请先完成前置任务");
                    }
                }
                // 不管是否有前置任务，只要前面的校验过了，就查询自身的加工记录
                if (checkPassed && _workplace._checkRedo && _workplace.Apis.CheckIfBarCodeExistsInMissionRecord(new(null) { ProductBarCode = barCode }).Yes) {
                    logger.Info($"Checking REDO from recordings for matched mission id [{mission.id}], barcode = [{barCode}]...");

                    bool needRedo;
                    CheckProductBarCodeRedoForChallenge();

                    if (WidgetUtils.ShowConfirmPopUp("检测到已对该产品进行过加工，是否需要返工？")) {
                        logger.Info($"Current mission needs REDO, mission id = [{mission.id}], barcode = [{barCode}], waiting for administrators to confirm...");

                        // 需要管理员密码弹窗
                        needRedo = _workplace.OpenAdminPasswordPopUpForm("产品返工确认，请输入管理员密码解锁", allowCancel: false);
                    } else {
                        logger.Info($"Current mission doesn't need REDO, mission id = [{mission.id}], barcode = [{barCode}]...");
                        needRedo = false;
                    }
                    // 需要返工，修改是否返工的标识
                    if (needRedo) {
                        _workplace.IsRedo = (int) YesOrNo.YES;
                    } else {
                        logger.Info($"Current mission needs REDO, mission id = [{mission.id}], barcode = [{barCode}], administrators refused to continue, so won't start this mission...");

                        _workplace.IsRedo = (int) YesOrNo.NO;
                        // 存在确认返工的情况但取消返工，则将校验结果改为不通过
                        checkPassed = false;
                    }
                }
            }

            // 所有检查完毕，回填、或切换任务后再回填
            if (checkPassed) {
                logger.Info($"All checks passed for product barcode = [{barCode}], will start for mission id = [{mission.id}]...");

                // 存入缓存并回填到主界面
                _workplace.BarCodeObj.ProductBarCode = barCode;
                WriteBackBarCodes();
                // 禁用产品条码输入框
                _productBarCodeBox.Enabled = false;
                // 是否需要切换任务
                if (needToSwtich) {
                    mission = CommonUtils.CannotBeNull(mission);
                    SwitchToMission(mission);
                }
                if (_partsBarCodeRules.ContainsKey(_mission.id)) {
                    logger.Info($"Recalculate count of parts barcodes for mission [id = {_mission.id}]...");
                    RecalcPartsRemainingCount();
                }
                if (_workplace.BarCodeObj.PartsRulesCount > 0) {
                    CustomTextBoxButtonGroup firstPartsBox = (CustomTextBoxButtonGroup) _partsBarCodeContentPanel.Controls[0];
                    firstPartsBox.Enabled = true;
                    firstPartsBox.GetTextBox(0).Box.Focus();
                    ActiveControl = firstPartsBox.GetTextBox(0).Box;
                }
                // 检查是否可以激活任务
                if (CheckCanActivateMission()) {
                    // 激活任务
                    _workplace.ActivateMission();
                    await Task.Delay(200);
                    Hide();
                }
            } else {
                logger.Info($"Check fails for mission = [id = {_mission.id}], barcode = [{barCode}]...");
                _productBarCodeBox.GetTextBox(0).IsError = true;
            }
        }

        public void ValidateProductBarCode(string barCode) {
            logger.Info($"Checking product barcode [{barCode}]...");

            // 先回填，不然校验不到
            _productBarCodeBox.SetValue(0, barCode);
            // 校验条码
            ValidateProductBarCodeAsync();
        }
        private void RecalcPartsRemainingCount() {
            List<BarCodeMatchingRuleDTO> rulesTemp = _partsBarCodeRules[_mission.id];
            // Filer out all rules that bound to bolts
            rulesTemp = rulesTemp.Where(rule => !_rulesExcluded.Any(r => r.id == rule.id)).ToList();

            _workplace.BarCodeObj.PartsRulesCount = rulesTemp.Count;
            logger.Info($"Count of all parts barcodes that not bound to any bolts = [{rulesTemp.Count}], mission id = [{_mission.id}]...");
        }

        public async void ValidatePartsBarCode(CustomTextBoxButtonGroup box) {
            string barCode = box.GetTextBox(0).Box.Text;
            logger.Info($"Checking parts barcode = [{barCode}] for mission [id = {_mission.id}]...");

            if (string.IsNullOrEmpty(barCode)) {
                WidgetUtils.ShowWarningPopUp($"请输入或扫描条码", 2);
                box.GetTextBox(0).IsError = true;
                return;
            } else if (_workplace.BarCodeObj.PartsBarCodes.Contains(barCode)) {
                box.GetTextBox(0).IsError = true;
                ShowWrongBarcodeGate($"请勿重复录入物料");
                return;
            }

            // 物料条码校验不通过
            int ruleId = CheckPartsBarCodeMatchedMission(barCode);
            if (ruleId < 0) {
                // Add this because sometimes it will go inside this check after activated, no idea why, then add a check here 
                if (_workplace.Activated && !_isForBolt) {
                    return;
                }

                CheckPartsBarCodeErrorForChallenge();

                box.GetTextBox(0).IsError = true;
                ShowWrongBarcodeGate($"当前物料条码【{barCode}】与当前任务所配置的物料条码不匹配");
            }
            // 物料条码校验通过
            else {
                bool checkPassed = true;

                // 如果存在前置物料任务，则先查询前置物料任务是否完成
                if (checkPassed && _mission.predecessor_part_mission_ids != null) {
                    logger.Info($"Checking predecessor parts mission for mission id = [{_mission.id}], predecessor_part_mission_ids = [{string.Join(",", _mission.predecessor_part_mission_ids)}], barcode = [{barCode}]...");

                    Dictionary<int, int>? idsDict = JsonConvert.DeserializeObject<Dictionary<int, int>>(_mission.predecessor_part_mission_ids);
                    if (idsDict != null) {
                        foreach (KeyValuePair<int, int> pair in idsDict) {
                            if (pair.Key == ruleId) {
                                logger.Info($"Checking REDO from recordings for matched mission id [{_mission.id}], barcode = [{barCode}]...");

                                CheckIfBarCodeExistsInMissionRecordRsp rsp = _workplace.Apis.CheckIfBarCodeExistsInMissionRecord(new(pair.Value, (int) TighteningStatus.OK) { ProductBarCode = barCode });
                                bool yes = rsp.Yes;

                                CheckPartsPredecessorForChallenge(rsp.Yes, rsp.MissionRecordDTO?.create_time);

                                if (!yes) {
                                    logger.Info($"Validation fails for predecessor mission, mission id = [{_mission.id}], predecessor_part_mission_ids = [{string.Join(",", _mission.predecessor_part_mission_ids)}], barcode = [{barCode}]...");

                                    WidgetUtils.ShowWarningPopUp("未检测到前置任务的加工完成记录，请先完成前置任务");
                                    checkPassed = false;
                                }
                            }
                        }

                    }
                }

                if (checkPassed) {
                    // Extra check
                    checkPassed = PartsBarCodeExtraCheck(ruleId);
                }

                // 物料码返工确认
                if (_workplace.IsRedo != (int) YesOrNo.YES || _mission.is_challenge_mission == (int) YesOrNo.YES) {
                    if (checkPassed
                            && _workplace.Apis.CheckPartsBarCode(new(null, barCode)).Yes) {
                        logger.Info($"Checking REDO from recordings for matched mission id [{_mission.id}], parts barcode = [{barCode}]...");

                        bool needRedo;
                        CheckPartsBarCodeRedoForChallenge();

                        if (WidgetUtils.ShowConfirmPopUp($"检测到数据库已存在此物料，是否需要返工？")) {
                            logger.Info($"Current mission needs REDO, mission id = [{_mission.id}], parts barcode = [{barCode}], waiting for administrators to confirm...");

                            // 需要管理员密码弹窗
                            needRedo = _workplace.OpenAdminPasswordPopUpForm("物料返工确认。请输入管理员密码解锁。", allowCancel: false);
                        } else {
                            logger.Info($"Current mission doesn't need REDO, mission id = [{_mission.id}], parts barcode = [{barCode}]...");
                            needRedo = false;
                        }
                        // 需要返工，修改是否返工的标识
                        if (needRedo) {
                            // 由于追溯码也有这个校验，因此如果不需要返工，则不动已经校验过的状态
                            _workplace.IsRedo = (int) YesOrNo.YES;
                        } else {
                            logger.Info($"Current mission needs REDO, mission id = [{_mission.id}], parts barcode = [{barCode}], administrators refused to continue, so won't start this mission...");

                            // Don't set this here because it might be a REDO mission because of product barcode
                            // _workplace.IsRedo = (int) YesOrNo.NO;
                            checkPassed = false;
                        }
                    }
                }

                if (checkPassed) {
                    logger.Info($"All checks passed for parts barcode = [{barCode}], mission id = [{_mission.id}]...");

                    // 存入缓存并回填到主界面
                    _workplace.BarCodeObj.PartsBarCodes.Add(barCode);
                    _workplace.BarCodeObj.PartsMatchingRulesCached.Add(ruleId);
                    WriteBackBarCodes();
                    // 禁用当前输入框
                    box.Enabled = false;
                    // 如果还有下一个物料需要录入，则自动聚焦到下一个物料输入框
                    bool hasNext = false;
                    RecalcPartsRemainingCount();
                    if (_workplace.BarCodeObj.PartsBarCodes.Count < _workplace.BarCodeObj.PartsRulesCount) {
                        CustomTextBoxButtonGroup nextBox = (CustomTextBoxButtonGroup) _partsBarCodeContentPanel.Controls[_workplace.BarCodeObj.PartsBarCodes.Count];
                        nextBox.Enabled = true;
                        nextBox.GetTextBox(0).Box.Focus();
                        ActiveControl = nextBox.GetTextBox(0).Box;

                        hasNext = true;
                    }
                    // 检查是否可以激活任务
                    if (!_workplace.Activated) {
                        if (CheckCanActivateMission()) {
                            // 激活任务
                            _workplace.ActivateMission();
                            await Task.Delay(1000);
                        }
                    } else {
                        // Check partsBarCode first, then updagte
                        List<string> partsBarCodes = _workplace.MissionRecord.parts_bar_code.Split(",").ToList();
                        if (_workplace.BarCodeObj.PartsBarCodes.Count > 0) {
                            foreach (string partsBarCode in _workplace.BarCodeObj.PartsBarCodes) {
                                if (partsBarCodes.IndexOf(partsBarCode) == -1) {
                                    PartsBarCodeDTO partsBarCodeDTO = new PartsBarCodeDTO() {
                                        mission_record_id = _workplace.MissionRecord.id,
                                        parts_bar_code = partsBarCode,
                                    };
                                    _workplace.Apis.AddOrUpdatePartsBarCode(new AddOrUpdatePartsBarCodeReq(partsBarCodeDTO));
                                }
                            }
                        }

                        _workplace.RemoveLockMsg(WorkingProcessPanel.LockedBoltBarCode);
                        _workplace.MissionRecord.parts_bar_code = string.Join(",", _workplace.BarCodeObj.PartsBarCodes);
                        _workplace.Apis.AddOrUpdateMissionRecord(new(_workplace.MissionRecord));

                        await Task.Delay(300);
                    }

                    if (!hasNext) {
                        // Hide/Close pop up form
                        // 没有下一个就关闭弹窗
                        Hide();
                    }
                } else {
                    logger.Info($"Check fails for mission = [id = {_mission.id}], parts barcode = [{barCode}]...");
                    box.GetTextBox(0).IsError = true;
                }
            }
        }
        protected virtual bool ProductBarCodeExtraCheck(string barCode) => true;
        protected virtual bool PartsBarCodeExtraCheck(int ruleId) {
            // Check if current bar code is bound to a bolt (or current bolt)
            if (_rulesExcluded.Count > 0 && _rulesExcluded.Any(rule => rule.id == ruleId)) {
                if (!_workplace.Activated) {
                    logger.Info($"Current parts barcode is bound to a bolt, can only enter after mission is activated...");
                    WidgetUtils.ShowWarningPopUp("此物料与点位绑定，任务进行时才需录入");
                } else {
                    logger.Info($"Current parts barcode is not bound to current bolt...");
                    WidgetUtils.ShowWarningPopUp("此物料不是当前点位绑定的物料，请重新录入");
                }
                return false;
            }

            return true;
        }
        public void ValidatePartsBarCode(string barCode) {
            logger.Info($"Checking parts barcode [{barCode}]...");

            try {
                logger.Info($"Count on PopUp: {_partsBarCodeContentPanel.Controls.Count}");
                logger.Info($"Count on Saved: {_workplace.BarCodeObj.PartsBarCodes.Count}");
                logger.Info($"Saved Bar codes: [{string.Join(", ", _workplace.BarCodeObj.PartsBarCodes)}]");

                if (_focusedBox == null) {
                    if (_partsBarCodeContentPanel.Controls.Count > _workplace.BarCodeObj.PartsBarCodes.Count) {
                        _focusedBox = (CustomTextBoxButtonGroup) _partsBarCodeContentPanel.Controls[_workplace.BarCodeObj.PartsBarCodes.Count];
                    } else {
                        return;
                    }

                    // Check enabled and null of last text box, if is disabled or not null, then no need to process
                    if (!_focusedBox.Enabled || !string.IsNullOrEmpty(_focusedBox.GetTextBox(0).Text)) {
                        // If it's disabled or not null, means it's already been back fill, no need to process
                        return;
                    }
                }

                // 先回填，不然校验不到
                _focusedBox.SetValue(0, barCode);
                // 校验条码
                ValidatePartsBarCode(_focusedBox);
            } catch (Exception e) {
                logger.Error($"Error occurred while focusing parts bar code input box, e = {e}");
                throw e;
            }
        }
        private void WriteBackBarCodes() {
            string barCodes = _workplace.BarCodeObj.ProductBarCode;
            if (_workplace.BarCodeObj.PartsBarCodes.Count > 0) {
                barCodes += " | " + string.Join(", ", _workplace.BarCodeObj.PartsBarCodes);
            }
            _workplace.BarCodeTextBox.Text = barCodes;
        }

        // 检查当前条码是否与当前已选择任务匹配（没有选择任务则不匹配）
        private bool CheckBarCodeMatchedMission(string barCode) {
            if (_productBarCodeRules.ContainsKey(_mission.id)) {
                foreach (BarCodeMatchingRuleDTO rule in _productBarCodeRules[_mission.id]) {
                    if (MainUtils.CheckBarCodeIsMatched(barCode, rule)) {
                        return true;
                    }
                }
                return false;
            }
            // 如果没有配置条码匹配规则，则也可以通过
            return true;
        }
        // 检查当前条码是否与数据库中任意任务的产品码/追溯码匹配
        private ProductMissionDTO? FindBarCodeMatchedMission(string barCode) {
            ProductMissionDTO? mission = null;
            foreach (KeyValuePair<int, List<BarCodeMatchingRuleDTO>> pair in _productBarCodeRules) {
                foreach (BarCodeMatchingRuleDTO rule in pair.Value) {
                    if (MainUtils.CheckBarCodeIsMatched(barCode, rule.end_char, rule.length, MainUtils.GetKeyMatchingRule(rule.key_position, rule.key_char))) {
                        mission = _workplace.Apis.QueryProductMissionDetail(new(pair.Key)).ProductMissionDTO;
                        break;
                    }
                }
                if (mission != null) {
                    break;
                }
            }
            return mission;
        }
        // 检查当前物料条码是否匹配当前任务
        private int CheckPartsBarCodeMatchedMission(string barCode) {
            if (_partsBarCodeRules.ContainsKey(_mission.id)) {
                // 校验时需要剔除已经校验过的规则，以免扫过的码重复也能通过
                foreach (BarCodeMatchingRuleDTO rule in _partsBarCodeRules[_mission.id].Where(r => !_workplace.BarCodeObj.PartsMatchingRulesCached.Contains(r.id))) {
                    if (MainUtils.CheckBarCodeIsMatched(barCode, rule)) {
                        logger.Info($"Check passes for barcode = [{barCode}], mission [id = {_mission.id}], rule id = [{rule.id}]...");
                        return rule.id;
                    }
                }

                logger.Info($"Check fails for barcode = [{barCode}], mission [id = {_mission.id}]...");
                return -1;
            }

            // 如果没有配置条码匹配规则，则也可以通过
            logger.Info($"No any rules for parts barcodes, checking barcode = [{barCode}], mission [id = {_mission.id}]...");
            return 0;
        }

        protected virtual void CheckProductBarCodeErrorForChallenge() { }
        protected virtual void CheckProductPredecessorForChallenge(bool predecessorExists, DateTime? createTime) { }
        protected virtual void CheckProductBarCodeRedoForChallenge() { }
        protected virtual void CheckPartsBarCodeErrorForChallenge() { }
        protected virtual void CheckPartsPredecessorForChallenge(bool predecessorExists, DateTime? createTime) { }
        protected virtual void CheckPartsBarCodeRedoForChallenge() { }

        private void ShowWrongBarcodeGate(string message) {
            if (_workplace.CheckErrorPromptForWrongBarcodeEnabled()) {
                _workplace.OpenAdminPasswordPopUpForm(message, allowCancel: false);
            } else {
                WidgetUtils.ShowWarningPopUp(message);
            }
        }

        // 检查是否可以激活任务
        public virtual bool CheckCanActivateMission() {
            logger.Info($"Checking for mission [id = {_mission.id}] if can activate it, right now...");

            // 没选任务，pass
            if (_mission.id <= 0) {
                return false;
            }

            // 没录入产品码，pass
            if (string.IsNullOrEmpty(_workplace.BarCodeObj.ProductBarCode)) {
                logger.Info($"Haven't entered product barcode for mission [id = {_mission.id}]...");
                return false;
            }

            if (_partsBarCodeRules.ContainsKey(_mission.id)) {
                List<BarCodeMatchingRuleDTO> rulesTemp = _partsBarCodeRules[_mission.id];
                // Filer out all rules that bound to bolts
                rulesTemp = rulesTemp.Where(rule => _workplace.BarCodeObj.PartsMatchingRulesCached.Contains(rule.id) || !_rulesExcluded.Any(r => r.id == rule.id)).ToList();
                // 配置了物料码但是录入的物料码与配置的数量不一致，fails
                if (rulesTemp.Count != _workplace.BarCodeObj.PartsBarCodes.Count) {
                    logger.Info($"Count of entered parts barcodes for mission [id = {_mission.id}] are not enough. rulesTemp.Count = [{rulesTemp.Count}], _workplace.BarCodeObj.PartsBarCodes.Count = [{_workplace.BarCodeObj.PartsBarCodes.Count}]...");
                    return false;
                }

                // 重置所有带红框提示的输入框
                _productBarCodeBox.GetTextBox(0).IsError = false;
                foreach (Control ctrl in _partsBarCodeContentPanel.Controls) {
                    ((CustomTextBoxButtonGroup) ctrl).GetTextBox(0).IsError = false;
                }
            }

            logger.Info($"All checks passed for mission = [id = {_mission.id}]...");
            return true;
        }
        // 根据传入的条码智能校验
        public void ValidateBarCode(string barCode) {
            // 如果没有录入产品码，则校验产品码
            if (string.IsNullOrEmpty(_workplace.BarCodeObj.ProductBarCode)) {
                ValidateProductBarCode(barCode);
            }
            // 否则校验物料码
            else {
                ValidatePartsBarCode(barCode);
            }
        }


        protected override void AfterShown() {
            base.AfterShown();
            // 弹窗显示后异步执行条码校验，避免阻塞弹窗显示
            BeginInvoke(async () => {
                if (_barCode != null) {
                    await Task.Delay(500);
                    ValidateBarCode(_barCode);
                }
            });
        }

        public void ResizeSelf() {
            CalculateDetailProperties();

            Size mainSize = WidgetUtils.MainSize;
            Padding contentPadding = ContentPanel.Padding;
            int contentWidth = (int) (mainSize.Width * .7);
            int contentHeight = 0;

            int titleHeight = WidgetUtils.PopUpOrFloatingFormSubTitle();
            int titleVPadding = titleHeight / 5;
            Size titleSize = new(contentWidth - contentPadding.Size.Width, titleHeight);

            int boxHeight = WidgetUtils.TextOrComboBoxHeight();
            int boxPadding = boxHeight / 5;
            int innerContentWidth = contentWidth - contentPadding.Size.Width;
            Size boxSize = new(innerContentWidth - boxPadding * 2, boxHeight);

            int productPanelHeight = (boxHeight + boxPadding * 2) * _productBarCodeContentPanel.Controls.Count;
            int partsPanelHeight = (boxHeight + boxPadding * 2) * _partsBarCodeContentPanel.Controls.Count;

            _productBarCodeTitle.Size = titleSize;
            _partsBarCodeTitle.Size = titleSize;
            _partsBarCodeTitle.Margin = new(0, titleVPadding, 0, 0);

            foreach (Control ctrl in _productBarCodeContentPanel.Controls) {
                CustomTextBoxButtonGroup box = (CustomTextBoxButtonGroup) ctrl;
                box.Size = boxSize;
                box.Margin = new(boxPadding);
            }
            _productBarCodeContentPanel.Size = new(innerContentWidth, productPanelHeight);

            foreach (Control ctrl in _partsBarCodeContentPanel.Controls) {
                CustomTextBoxButtonGroup box = (CustomTextBoxButtonGroup) ctrl;
                box.Size = boxSize;
                box.Margin = new(boxPadding);
            }
            _partsBarCodeContentPanel.Size = new(innerContentWidth, partsPanelHeight);

            contentHeight += (titleHeight + titleVPadding) * 2 + contentPadding.Size.Height;
            contentHeight += productPanelHeight + partsPanelHeight;
            SetContentSizeAndSelfSize(new(contentWidth, contentHeight));
        }
    }
}
