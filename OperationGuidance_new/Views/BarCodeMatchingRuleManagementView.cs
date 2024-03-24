using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;
using CustomLibrary.TextBoxes;
using CustomLibrary.ComboBoxes;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Views {
    public class BarCodeMatchingRuleManagementView: CustomDataGridViewOuterPanel<BarCodeMatchingRuleDTO, BarCodeMatchingRuleVO> {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        private List<BarCodeMatchingRuleDTO> _dataDTOList;
        // DataGridView panel
        private DataGridViewGroup<BarCodeMatchingRuleVO> _dataGridView;
        // Add new pop up form
        private EditEntityPopUpForm<BarCodeMatchingRuleDTO> _editEntityPopUpForm;
        private List<ProductMissionDTO> _missions;
        private CustomComboBoxGroup<int?> _missionNameComboBox;
        #endregion

        #region Constructors
        public BarCodeMatchingRuleManagementView() {
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
            CustomComboBoxGroup<int?> barCodeTypeComboBox = _dataGridView.AddComboBox("条码类型", (BarCodeMatchingRuleVO vo, int? value) => vo.type = value, new());
            foreach (BarCodeType type in BarCodeTypes.Elements) {
                barCodeTypeComboBox.AddItem(type.Name, type.Id);
            }
            _missionNameComboBox = _dataGridView.AddComboBox("应用任务", (BarCodeMatchingRuleVO vo, int? value) => vo.mission_id = value, new());
            RefreshMissionOptions();

            // 按钮逻辑
            _dataGridView.QueryData = (vo) => {
                List<BarCodeMatchingRuleVO> vos = QueryList();
                return vos
                    .Where(o => vo.type == null || vo.type.Value == 0 || o.type != null && o.type == vo.type)
                    .Where(o => vo.mission_id == null || vo.mission_id.Value == 0 || o.mission_id != null && o.mission_id == vo.mission_id)
                    .ToList();
            };
            _dataGridView.AddNewClick = (action) => {
                BarCodeMatchingRuleDTO dto = new();
                OpenEditEntityPopUpForm("新增条码匹配规则", dto, action);
            };
            _dataGridView.ModifyClick = (ids, action) => {
                if (ids.Count <= 0) {
                    WidgetUtils.ShowNoticePopUp("请选择要编辑的数据。");
                } else if (ids.Count > 1) {
                    WidgetUtils.ShowNoticePopUp("只能选择一条数据进行编辑操作。");
                } else {
                    if (_dataDTOList.Count > 0) {
                        BarCodeMatchingRuleDTO dto = _dataDTOList.Single(dto => dto.id == ids[0]);
                        OpenEditEntityPopUpForm("更新条码匹配规则", dto, action);
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
        }
        #endregion

        #region Reusable methods
        private void RefreshMissionOptions() {
            _missions = apis.QueryProductMissions(new()).ProductMissionsDTOs;
            foreach (ProductMissionDTO mission in _missions.Where(dto => !_missionNameComboBox.Items.Contains(dto.id))) {
                _missionNameComboBox.AddItem(mission.name, mission.id);
            }
        }
        private void OpenEditEntityPopUpForm(string title, BarCodeMatchingRuleDTO dto, Action callBackAction) {
            _editEntityPopUpForm = new(dto) {
                Title = title,
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            // 添加字段
            CustomTextBoxGroup length = _editEntityPopUpForm.AddTextBox("长度", false, 
                (BarCodeMatchingRuleDTO dto, int value) => dto.length = value);
            length.PositiveIntOnly = true;
            if (dto.length != null) {
                length.SetValue(0, dto.length + "");
            }
            CustomTextBoxGroup endChar = _editEntityPopUpForm.AddTextBox("结束符", false, 
                (BarCodeMatchingRuleDTO dto, string? value) => dto.end_char = value ?? "");
            if (dto.end_char != null) {
                endChar.SetValue(0, dto.end_char);
            }
            CustomComboBoxGroup<int> type = _editEntityPopUpForm.AddComboBox("条码类型", 
                (BarCodeMatchingRuleDTO dto, int value) => dto.type = value, new());
            foreach (BarCodeType t in BarCodeTypes.Elements) {
                type.AddItem(t.Name, t.Id);
            }
            type.SetCurrent(type.IndexOf(dto.type));
            type.ItemSelected += () => {
                type.SetError(type.IsDefaultValue());
            };
            CustomComboBoxGroup<int> missionName = _editEntityPopUpForm.AddComboBox("应用任务", 
                (BarCodeMatchingRuleDTO dto, int value) => dto.mission_id = value, new());
            foreach (ProductMissionDTO mission in _missions) {
                missionName.AddItem(mission.name, mission.id);
            }
            if (missionName.IndexOf(dto.mission_id) >= 0) {
                missionName.SetCurrent(missionName.IndexOf(dto.mission_id));
            } else if (dto.id > 0) {
                missionName.SetError(true);
                Task.Run(async () => {
                    await Task.Delay(500);
                    WidgetUtils.ShowWarningPopUp("所配置任务不存在或已被删除");
                });
            }
            missionName.ItemSelected += () => {
                missionName.SetError(missionName.IsDefaultValue());
            };
            CustomTextBoxGroup keyMatchingBox = _editEntityPopUpForm.AddSeparateTextBox("关键位匹配", ">", false, 
                    (BarCodeMatchingRuleDTO dto, string? value) => dto.key_position = value ?? null,
                    (BarCodeMatchingRuleDTO dto, string? value) => dto.key_char = value ?? null);
            _editEntityPopUpForm.TablePanel.SetColumnSpan(keyMatchingBox, 2);
            keyMatchingBox.Ratio = 8.52;
            keyMatchingBox.GetTextBox(0).TextChanged += (s, e) => {
                keyMatchingBox.GetTextBox(0).IsError = false;
            };
            keyMatchingBox.GetTextBox(1).TextChanged += (s, e) => {
                keyMatchingBox.GetTextBox(1).IsError = false;
            };
            if (dto.key_position != null) {
                keyMatchingBox.SetValue(0, dto.key_position);
            }
            if (dto.key_char != null) {
                keyMatchingBox.SetValue(1, dto.key_char);
            }

            // 添加按钮
            CommonButton confirmButton = _editEntityPopUpForm.AddButton("保存");
            confirmButton.Click += (s, e) => {
                bool check = true;
                string warningMsg = "";
                int warningIndex = 1;
                if (type.IsDefaultValue()) {
                    type.SetError(true);
                    check = false;
                    warningMsg += $"{warningIndex++}. 没有选择条码类型\r\n";
                }
                if (missionName.IsDefaultValue()) {
                    missionName.SetError(true);
                    check = false;
                    warningMsg += $"{warningIndex++}. 没有选择要配置此规则的任务\r\n";
                }
                if (type.Value == BarCodeTypes.PRODUCT.Id) {
                    List<BarCodeMatchingRuleDTO> dtos = apis.FindBarCodeMatchingRulesByMissionId(new(missionName.Value) { Type = BarCodeTypes.PRODUCT.Id }).BarCodeMatchingRuleDTOs;
                    if (dtos.Count > 0 && dtos[0].id != dto.id) {
                        missionName.SetError(true);
                        check = false;
                        warningMsg += $"{warningIndex++}. 此任务已经配置过【{BarCodeTypes.PRODUCT.Name}】，每个任务仅能配置一个\r\n";
                    }
                }
                CustomTextBox lengthBox = length.GetTextBox(0);
                CustomTextBox endCharBox = endChar.GetTextBox(0);
                CustomTextBox keyPositionBox = keyMatchingBox.GetTextBox(0);
                CustomTextBox keyCharBox = keyMatchingBox.GetTextBox(1);
                string len = lengthBox.Box.Text;
                string enChar = endCharBox.Box.Text;
                string keyPosition = keyPositionBox.Box.Text;
                string keyChar = keyCharBox.Box.Text;
                if (string.IsNullOrEmpty(len) && string.IsNullOrEmpty(enChar) && string.IsNullOrEmpty(keyPosition) && string.IsNullOrEmpty(keyChar)) {
                    lengthBox.IsError = true;
                    endCharBox.IsError = true;
                    keyPositionBox.IsError = true;
                    keyCharBox.IsError = true;
                    check = false;
                    warningMsg += $"{warningIndex++}. 至少需要填写一种匹配规则\r\n";
                } else {
                    lengthBox.IsError = false;
                    endCharBox.IsError = false;
                    keyPositionBox.IsError = false;
                    keyCharBox.IsError = false;
                }
                if (!string.IsNullOrEmpty(keyPosition) && string.IsNullOrEmpty(keyChar)) {
                    keyCharBox.IsError = true;
                    check = false;
                    warningMsg += $"{warningIndex++}. 关键位没有填写对应的关键字符\r\n";
                } else if (string.IsNullOrEmpty(keyPosition) && !string.IsNullOrEmpty(keyChar)) {
                    keyPositionBox.IsError = true;
                    check = false;
                    warningMsg += $"{warningIndex++}. 关键字符没有填写对应的关键位\r\n";
                } else if (!string.IsNullOrEmpty(keyPosition) && !string.IsNullOrEmpty(keyChar)) {
                    string keyPositionError = MainUtils.CheckKeyPosition(keyPosition);
                    if (!string.IsNullOrEmpty(keyPositionError)) {
                        keyPositionBox.IsError = true;
                        check = false;
                        warningMsg += $"{warningIndex++}. {keyPositionError}\r\n";
                    } else {
                        if (keyPosition.StartsWith(',')) keyPosition = keyPosition.Remove(0);
                        if (keyPosition.EndsWith(',')) keyPosition = keyPosition.Remove(keyPosition.Length - 1);
                        if (keyChar.StartsWith(',')) keyChar = keyChar.Remove(0);
                        if (keyChar.EndsWith(',')) keyChar = keyChar.Remove(keyPosition.Length - 1);
                        List<int> keyPositionList = MainUtils.GetKeyPositionList(keyPosition);
                        if (dto.length != null && dto.length > 0 && keyPositionList.Where(p => p > dto.length).Count() > 0) {
                            keyPositionBox.IsError = true;
                            check = false;
                            warningMsg += $"{warningIndex++}. 关键位超出条码长度\r\n";
                        } else {
                            List<char> keyCharList = MainUtils.GetKeyCharList(keyChar);
                            if (keyPositionList.Count != keyCharList.Count || keyPosition.Count(c => c == ',') != keyChar.Count(c => c == ',')) {
                                keyPositionBox.IsError = true;
                                keyCharBox.IsError = true;
                                check = false;
                                warningMsg += $"{warningIndex++}. 关键位及关键字符数量不匹配\r\n";
                            }
                        }
                    }
                }

                if (!check) {
                    WidgetUtils.ShowWarningPopUp($"保存失败：\r\n{warningMsg}");
                } else {
                    keyPositionBox.IsError = false;
                    keyCharBox.IsError = false;
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
        protected override List<BarCodeMatchingRuleVO> QueryList() {
            QueryBarCodeMatchingRuleListRsp rsp = apis.QueryBarCodeMatchingRuleList(new());
            _dataDTOList = rsp.BarCodeMatchingRuleDTOs;
            List<BarCodeMatchingRuleVO> vos = new();
            CommonUtils.ObjectConverter<BarCodeMatchingRuleDTO, BarCodeMatchingRuleVO>(_dataDTOList, vos);
            foreach (ProductMissionDTO mission in _missions) {
                List<BarCodeMatchingRuleVO> vosTemp = vos.Where(v => v.mission_id == mission.id).ToList();
                vosTemp.ForEach(vo => vo.mission_name = mission.name);
            }

            // TODO: can use BackgroundWorker to do this
            // 后续再优化数据加载时的延迟、卡顿问题，现在先不管
            // for (int i = 0; i < 5000; i++) {
            //     workstationVOs.Add(workstationVOs[0]);
            // }
            return vos;
        }
        protected override void AddOrUpdate(BarCodeMatchingRuleDTO dto, Action action) {
            AddOrUpdateBarCodeMatchingRuleRsp rsp = apis.AddOrUpdateBarCodeMatchingRule(new(dto));
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
                DeleteBarCodeMatchingRuleByIdsRsp rsp = apis.DeleteBarCodeMatchingRule(new(ids));
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
            RefreshMissionOptions();
        }
        #endregion
    }
}
