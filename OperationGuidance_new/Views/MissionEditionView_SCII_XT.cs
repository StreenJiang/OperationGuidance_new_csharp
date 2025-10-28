using CustomLibrary.Buttons;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using Newtonsoft.Json;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public partial class MissionEditionView_SCII_XT: MissionEditionView_SCII {
        protected new MissionEditionPage_SCII_XT? _editionPage;

        public new MissionEditionPage_SCII_XT? EditionPage { get => _editionPage; set => _editionPage = value; }

        public MissionEditionView_SCII_XT() : base() { }

        public override MissionEditionPage_SCII_XT CreateANewOne() {
            return OpenEditionPage(null);
        }

        public override MissionEditionPage_SCII_XT OpenEditionPage(int? missionId) {
            ProductMissionDTO NewMission() {
                return new() {
                    name = "新建任务",
                    ProductSides = new() {
                        new() {
                            name = "产品面1",
                        },
                    },
                };
            }
            if (missionId == null) {
                _missionDTO = NewMission();
            } else {
                _missionDTO = apis.QueryProductMissionDetail(new(missionId.Value)).ProductMissionDTO;
                if (_missionDTO == null) {
                    _missionDTO = NewMission();
                }
            }
            // Clear all child controls
            Controls.Clear();
            // Create a new page according to missionbody and show
            if (_editionPage != null) {
                _editionPage.Dispose();
            }
            _editionPage = new(this, _missionDTO);
            _editionPage.ResizeChildren();
            return _editionPage;
        }

        // Class: inner page panel
        public class MissionEditionPage_SCII_XT: MissionEditionPage_SCII {
            public MissionEditionPage_SCII_XT(MissionEditionView_SCII_XT parent, ProductMissionDTO missionDTO) : base(parent, missionDTO) { }

            protected override void OpenMissionDetailPopUp(object? s, EventArgs e) {
                List<BarCodeMatchingRuleDTO> barCodeMatchingRuleDTOs = _apis.QueryBarCodeMatchingRuleList(new(SystemUtils.MacAddressesDTO.id) { MissionId = _missionDTO.id }).BarCodeMatchingRuleDTOs;
                List<ProductMissionDTO> allOtherMissions = _apis.QueryProductMissions(new()).ProductMissionsDTOs.Where(m => m.id != _missionDTO.id).ToList();
                _detialPopUpForm = new MissionDetailPopUpForm_SCII_XT(_missionDTO, allOtherMissions, barCodeMatchingRuleDTOs, _screwBitCounterDTOs) {
                    Title = "编辑任务详情",
                };
                _detialPopUpForm.MissionName.GetTextBox(0).Box.Select(0, 0);
                _detialPopUpForm.AddButton("确定").Click += (s, e) => {
                    bool check = true;
                    string warningMsg = "";
                    int warningIndex = 1;

                    string missionName = _detialPopUpForm.MissionName.GetTextBox(0).Box.Text;
                    if (string.IsNullOrEmpty(missionName)) {
                        check = false;
                        _detialPopUpForm.MissionName.GetTextBox(0).IsError = true;
                        warningMsg += $"{warningIndex++}. 任务名称不能为空\r\n";
                    } else if (allOtherMissions.Find(m => m.name == missionName) != null) {
                        check = false;
                        _detialPopUpForm.MissionName.GetTextBox(0).IsError = true;
                        warningMsg += $"{warningIndex++}. 任务名称不能与现有任务名称重复\r\n";
                    }

                    string maxNGNum = _detialPopUpForm.MaxNGNum.GetTextBox(0).Box.Text;
                    if (string.IsNullOrEmpty(maxNGNum)) {
                        check = false;
                        _detialPopUpForm.MaxNGNum.GetTextBox(0).IsError = true;
                        warningMsg += $"{warningIndex++}. 最大NG数不能为空\r\n";
                    }

                    string passwordNeedTime = _detialPopUpForm.PasswordNeedTime.GetTextBox(0).Box.Text;
                    if (string.IsNullOrEmpty(passwordNeedTime)) {
                        check = false;
                        _detialPopUpForm.PasswordNeedTime.GetTextBox(0).IsError = true;
                        warningMsg += $"{warningIndex++}. 第几次起需密码不能为空\r\n";
                    }

                    int int_maxNGNum = int.Parse(maxNGNum);
                    int int_passwordNeedTime = int.Parse(passwordNeedTime);
                    if (int_maxNGNum > 0 && int_passwordNeedTime >= int_maxNGNum) {
                        check = false;
                        _detialPopUpForm.PasswordNeedTime.GetTextBox(0).IsError = true;
                        warningMsg += $"{warningIndex++}. 第几次起需密码必须小于最大NG数，NG次数达到最大时任务已经失败，无法再弹窗输入管理员密码\r\n";
                    }

                    // Check challenge mission settings
                    if (_detialPopUpForm.IsChallengeMission.Checked) {
                        // Check challenge mission is empty or not
                        if (_detialPopUpForm.ChallengMission.IsDefaultValue()) {
                            check = false;
                            _detialPopUpForm.ChallengMission.SetError(true);
                            warningMsg += $"{warningIndex++}. 挑战任务必须对应一个普通任务\r\n";
                        } else {
                            int selectedMissionId = _detialPopUpForm.ChallengMission.Value;

                            // Check if current selected challenge mission is already a challenge mission
                            if (allOtherMissions.SingleOrDefault(m => m.challenge_mission_id == selectedMissionId) != null) {
                                check = false;
                                _detialPopUpForm.ChallengMission.SetError(true);
                                warningMsg += $"{warningIndex++}. 当前选择的对应普通任务已存在挑战任务\r\n";
                            } else {
                                ProductMissionDTO selectedMission = allOtherMissions.Single(m => m.id == selectedMissionId);

                                // Check if setting isFirstMission = true
                                if (_detialPopUpForm.IsFirstMission.Checked) {
                                    if (selectedMission.predecessor_mission_id != null || selectedMission.predecessor_mission_id > 0) {
                                        check = false;
                                        _detialPopUpForm.ChallengMission.SetError(true);
                                        warningMsg += $"{warningIndex++}. 当前选择的对应普通任务存在前置任务，无法设置成首道岗位\r\n";
                                    }
                                }
                            }
                        }
                    }

                    // Check part predecessor mission
                    if (_detialPopUpForm.PredecessorPartMissionMaps.Count > 0) {
                        List<int> ids = new();
                        bool flag = false;
                        foreach (PredecessorPartMissionMap map in _detialPopUpForm.PredecessorPartMissionMaps) {
                            if (map.BarCodeRuleId.IsDefaultValue() && !map.MissionId.IsDefaultValue()) {
                                flag = true;
                                map.BarCodeRuleId.SetError(true);
                            } else {
                                map.BarCodeRuleId.SetError(false);
                            }

                            if (!map.BarCodeRuleId.IsDefaultValue() && map.MissionId.IsDefaultValue()) {
                                flag = true;
                                map.MissionId.SetError(true);
                            } else {
                                map.MissionId.SetError(false);
                            }

                            if (map.BarCodeRuleId.IsDefaultValue() && map.MissionId.IsDefaultValue()) {
                                map.BarCodeRuleId.SetError(false);
                                map.MissionId.SetError(false);
                            }
                        }

                        if (flag) {
                            check = false;
                            warningMsg += $"{warningIndex++}. 物料前置任务需要选择要限制的物料匹配规则ID\r\n";
                        }

                        List<int> missionIds = _detialPopUpForm.PredecessorPartMissionMaps.Where(map => !map.MissionId.IsDefaultValue()).Select(map => map.MissionId.Value).ToList();
                        if (!_detialPopUpForm.PredecessorMission.IsDefaultValue()) {
                            missionIds.Add(_detialPopUpForm.PredecessorMission.Value);
                        }

                        Dictionary<int, int> idsDict = missionIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
                        List<int> idKeys = idsDict.Where(pair => pair.Value > 1).Select(pair => pair.Key).ToList();
                        if (idKeys.Count > 0) {
                            check = false;
                            _detialPopUpForm.PredecessorPartMissionMaps.ForEach(map => {
                                if (idKeys.Contains(map.MissionId.Value)) {
                                    map.MissionId.SetError(true);
                                } else if (!map.MissionId.IsError) {
                                    map.MissionId.SetError(false);
                                }
                            });
                            if (!_detialPopUpForm.PredecessorMission.IsDefaultValue()) {
                                _detialPopUpForm.PredecessorMission.SetError(true);
                            }
                            warningMsg += $"{warningIndex++}. 任务的“前置任务”与“物料前置任务”无法选择重复的任务\r\n";
                        } else {
                            if (_detialPopUpForm.IsFirstMission.Checked && !_detialPopUpForm.PredecessorMission.IsDefaultValue()) {
                                check = false;
                                _detialPopUpForm.PredecessorMission.SetError(true);
                                warningMsg += $"{warningIndex++}. 首道岗位挑战任务无法设置前置任务\r\n";
                            } else {
                                _detialPopUpForm.PredecessorMission.SetError(false);
                            }
                        }

                        List<int> barCodeRuleIds = _detialPopUpForm.PredecessorPartMissionMaps.Where(map => !map.BarCodeRuleId.IsDefaultValue()).Select(map => map.BarCodeRuleId.Value).ToList();
                        Dictionary<int, int> barCodeRulesDict = barCodeRuleIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
                        List<int> ruleKeys = barCodeRulesDict.Where(pair => pair.Value > 1).Select(pair => pair.Key).ToList();
                        if (ruleKeys.Count > 0) {
                            check = false;
                            _detialPopUpForm.PredecessorPartMissionMaps.ForEach(map => {
                                if (ruleKeys.Contains(map.BarCodeRuleId.Value)) {
                                    map.BarCodeRuleId.SetError(true);
                                } else if (!map.BarCodeRuleId.IsError) {
                                    map.BarCodeRuleId.SetError(false);
                                }
                            });
                            warningMsg += $"{warningIndex++}. 物料条码规则不能重复设置\r\n";
                        }
                    }

                    // Check screw bit counter boxes
                    List<CustomTextBox> bitPositions = new();
                    List<CustomTextBox> counters = new();
                    _detialPopUpForm.ScrewBitCounters.ForEach(box => {
                        if (!box.GetTextBox(0).IsEmpty()) {
                            if (int.Parse(box.GetTextBox(0).Box.Text) <= 0) {
                                bitPositions.Add(box.GetTextBox(0));
                            }

                            if (box.GetTextBox(1).IsEmpty()) {
                                counters.Add(box.GetTextBox(1));
                            }
                        }
                    });
                    if (bitPositions.Count > 0) {
                        check = false;
                        foreach (CustomTextBox box in bitPositions) {
                            box.IsError = true;
                        }
                        warningMsg += $"{warningIndex++}. 套筒位不能小于0\r\n";
                    }
                    if (counters.Count > 0) {
                        check = false;
                        foreach (CustomTextBox box in counters) {
                            box.IsError = true;
                        }
                        warningMsg += $"{warningIndex++}. 套筒位不为空时，批头使用上限及每次任务计数也不能为空\r\n";
                    }

                    // Check if can save
                    if (!check) {
                        WidgetUtils.ShowWarningPopUp($"保存失败：\r\n{warningMsg}");
                    } else {
                        _missionName.SetValue(0, missionName);
                        _missionDTO.name = missionName;
                        _missionDTO.is_challenge_mission = (int) (_detialPopUpForm.IsChallengeMission.Checked ? YesOrNo.YES : YesOrNo.NO);
                        _missionDTO.is_first_mission = (int) (_detialPopUpForm.IsFirstMission.Checked ? YesOrNo.YES : YesOrNo.NO);
                        _missionDTO.challenge_mission_id = _detialPopUpForm.ChallengMission.Value;
                        _missionDTO.max_ng_num = int.Parse(maxNGNum);
                        _missionDTO.password_need_time = int.Parse(passwordNeedTime);
                        if (!_detialPopUpForm.PredecessorMission.IsDefaultValue()) {
                            _missionDTO.predecessor_mission_id = _detialPopUpForm.PredecessorMission.Value;
                        } else {
                            _missionDTO.predecessor_mission_id = null;
                        }
                        Dictionary<int, int> idsDict = new();
                        foreach (PredecessorPartMissionMap map in _detialPopUpForm.PredecessorPartMissionMaps) {
                            if (!map.BarCodeRuleId.IsDefaultValue() && !map.MissionId.IsDefaultValue()) {
                                idsDict.Add(map.BarCodeRuleId.Value, map.MissionId.Value);
                            }
                        }

                        if (idsDict.Count > 0) {
                            _missionDTO.predecessor_part_mission_ids = JsonConvert.SerializeObject(idsDict);
                        } else {
                            _missionDTO.predecessor_part_mission_ids = null;
                        }

                        _detialPopUpForm.ScrewBitCounters.ForEach(box => {
                            if (!box.GetTextBox(0).IsEmpty() && !box.GetTextBox(1).IsEmpty()) {
                                int bitPosition = int.Parse(box.GetTextBox(0).Box.Text);
                                int maxNum = int.Parse(box.GetTextBox(1).Box.Text);

                                ScrewBitCounterDTO? temp = _screwBitCounterDTOs.Find(dto => dto.bit_position == bitPosition);
                                if (temp == null) {
                                    temp = new() {
                                        mission_id = _missionDTO.id,
                                    };
                                    _screwBitCounterDTOs.Add(temp);
                                }

                                temp.bit_position = bitPosition;
                                temp.max_num = maxNum;
                            }
                        });
                        ScrewBitCounterDTO? screwBitCounterDTO = _screwBitCounterDTOs.Find(dto =>
                                _detialPopUpForm.ScrewBitCounters.Find(box =>
                                    !box.GetTextBox(0).IsEmpty() && dto.bit_position == int.Parse(box.GetTextBox(0).Box.Text)) == null);
                        if (screwBitCounterDTO != null) {
                            screwBitCounterDTO.deleted = (int) YesOrNo.YES;
                        }

                        _detialPopUpForm.Hide();

                        // Reset all serial numbers of all bolts
                        _sideButtons.ForEach(side => side.ResetSerialNumbers());
                        ForceResizeRight();
                    }
                };
                _detialPopUpForm.AddButton("关闭").Click += (s, e) => {
                    _detialPopUpForm.Hide();
                };
                _detialPopUpForm.PretendToShowToCreateHandlesForChildren();
                _detialPopUpForm.ResizeSelf();
                _detialPopUpForm.Show();
            }

            protected override void SaveClick(object? sender, EventArgs eventArgs) {
                List<ProductMissionDTO> allOtherMissions = _apis.QueryProductMissions(new()).ProductMissionsDTOs.Where(m => m.id != _missionDTO.id).ToList();
                string missionName = _missionName.GetTextBox(0).Box.Text;
                if (string.IsNullOrEmpty(missionName)) {
                    _missionName.GetTextBox(0).IsError = true;
                    WidgetUtils.ShowErrorPopUp("任务名称不能为空！");
                    return;
                } else if (allOtherMissions.Find(m => m.name == missionName) != null) {
                    _missionName.GetTextBox(0).IsError = true;
                    WidgetUtils.ShowErrorPopUp("任务名称不能与现有任务名称重复！");
                    return;
                }
                _missionDTO.name = missionName;
                _missionName.GetTextBox(0).IsError = false;

                _currentProductImageFile.SaveSideInfo();
                // Store to database
                var req = new AddOrUpdateProductMissionReq(_missionDTO);
                var rsp = _apis.AddOrUpdateProductMission(req);
                if (rsp.RsponseCode == HttpResponseCode.OK) {
                    // Save screw bit counters
                    _screwBitCounterDTOs.ForEach(dto => _apis.AddOrUpdateScrewBitCounter(new(dto)));

                    Modified = false;
                    _missionDTO = rsp.ProductMissionDTO;

                    // 数据保存成功后，保存图片到本地（需要循环保存每一个side的图片）
                    foreach (SideButton sideBtn in _sideButtons) {
                        MainUtils.SaveProductImage(sideBtn.ProductImageFileNew.Image, sideBtn.ProductImageFileNew.ImageFileName);
                    }
                    MessageBox.Show(null, "保存成功！", "保存任务", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 保存后跳转至任务列表界面
                    WidgetUtils.GetChildMenu(101).TriggerClick(EventArgs.Empty);
                    Dispose();
                } else {
                    MessageBox.Show(null, "保存失败！错误信息：" + rsp.RsponseMessage, "保存任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        public class MissionDetailPopUpForm_SCII_XT: MissionDetailPopUpForm {
            public MissionDetailPopUpForm_SCII_XT(ProductMissionDTO missionDTO,
                                                  List<ProductMissionDTO> allOtherMissions,
                                                  List<BarCodeMatchingRuleDTO> barCodeMatchingRuleDTOs,
                                                  List<ScrewBitCounterDTO> screwBitCounterDTOs)
              : base(missionDTO,
                     allOtherMissions,
                     barCodeMatchingRuleDTOs,
                     screwBitCounterDTOs) { }

            protected override void InitScrewCounterBoxes() {
                _screwBitCounters = new() {
                    new("批头计数器1") {
                        Parent = _tablePanel,
                        Ratio = _boxRatioOneLine,
                        NameAlignment = HorizontalAlignment.Right,
                        PositiveIntOnly = true,
                        Separator = "->",
                    },
                };
                _screwBitCounters[0].AddTextBox();
                _screwBitCounters[0].SetDefaultText(0, "套筒位");
                _screwBitCounters[0].SetDefaultText(1, "批头使用上限");
                _screwBitCounters[0].GetTextBox(0).Box.TextChanged += (s, e) => NotErrorIfNotEmpty(_screwBitCounters[0].GetTextBox(0));
                _screwBitCounters[0].GetTextBox(1).Box.TextChanged += (s, e) => NotErrorIfNotEmpty(_screwBitCounters[0].GetTextBox(1));
                SignButton addButton = _screwBitCounters[0].AddButton<SignButton>();
                addButton.Icon = Properties.Resources.sign_plus;
                addButton.Click += (s, e) => AddScrewBitCounter();
            }

            protected override void AddScrewBitCounter() {
                int currentCount = _screwBitCounters.Count;
                if (currentCount >= _screwBitCounterMax) {
                    WidgetUtils.ShowWarningPopUp($"批头计数器每个任务最多支持配置{_screwBitCounterMax}个");
                    return;
                }

                CustomTextBoxButtonGroup box = new($"批头计数器{currentCount + 1}") {
                    Parent = _tablePanel,
                    Ratio = _boxRatioOneLine,
                    NameAlignment = HorizontalAlignment.Right,
                    PositiveIntOnly = true,
                    Separator = "->",
                };
                box.AddTextBox();

                box.GetTextBox(0).Box.TextChanged += (s, e) => NotErrorIfNotEmpty(box.GetTextBox(0));
                box.GetTextBox(1).Box.TextChanged += (s, e) => NotErrorIfNotEmpty(box.GetTextBox(1));
                box.SetDefaultText(0, "套筒位");
                box.SetDefaultText(1, "批头使用上限");

                SignButton minusButton = box.AddButton<SignButton>();
                minusButton.Icon = Properties.Resources.sign_minus;
                minusButton.Click += (s, e) => {
                    _screwBitCounters.Remove(box);
                    box.Dispose();
                    for (int i = 0; i < _screwBitCounters.Count; i++) {
                        _screwBitCounters[i].TextName = $"批头计数器{i + 1}";
                        _screwBitCounters[i].ResizeChildren();
                    }
                    ResizeSelf();
                };

                _screwBitCounters.Add(box);
                for (int i = 0; i < _screwBitCounters.Count; i++) {
                    _screwBitCounters[i].TextName = $"批头计数器{i + 1}";
                }
                _tablePanel.SetColumnSpan(box, _columnCount);

                ResizeSelf();
            }

            protected override void AfterShown() {
                _missionName.SetValue(0, _missionDTO.name);
                _isChallengeMission.Checked = _missionDTO.is_challenge_mission == (int) YesOrNo.YES;
                _maxNGNum.SetValue(0, _missionDTO.max_ng_num + "");
                _passwordNeedTime.SetValue(0, _missionDTO.password_need_time + "");
                if (_missionDTO.predecessor_mission_id != null) {
                    _predecessorMission.SetCurrent(_predecessorMission.IndexOf(_missionDTO.predecessor_mission_id.Value));
                }

                for (int i = 0; i < _screwBitCounterDTOs.Count - 1; i++) {
                    AddScrewBitCounter();
                }

                // Data backfill
                for (int i = 0; i < _screwBitCounterDTOs.Count; i++) {
                    ScrewBitCounterDTO sbc = _screwBitCounterDTOs[i];
                    _screwBitCounters[i].SetValue(0, sbc.bit_position + "");
                    _screwBitCounters[i].SetValue(1, sbc.max_num + "");
                }
            }
        }
    }
}
