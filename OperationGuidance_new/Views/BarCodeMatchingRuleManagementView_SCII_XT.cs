using CustomLibrary.Buttons;
using CustomLibrary.ComboBoxes;
using CustomLibrary.Configs;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Views {
    public class BarCodeMatchingRuleManagementView_SCII_XT: ABarCodeMatchingRuleManagementView<BarCodeMatchingRuleVO_SCII_XT> {
        protected override void OpenEditEntityPopUpForm(string title, BarCodeMatchingRuleDTO dto, Action callBackAction) {
            _editEntityPopUpForm = new(dto) {
                Title = title,
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            CustomComboBoxGroup<int> type = _editEntityPopUpForm.AddComboBox("条码类型",
                (BarCodeMatchingRuleDTO dto, int value) => dto.type = value, new());
            foreach (BarCodeType t in BarCodeTypes.Elements) {
                type.AddItem(t.Name, t.Id);
            }
            type.ItemSelected += () => type.SetError(type.IsDefaultValue());

            CustomComboBoxGroup<int> missionName = _editEntityPopUpForm.AddComboBox("应用任务",
                (BarCodeMatchingRuleDTO dto, int value) => dto.mission_id = value, new());
            foreach (ProductMissionDTO mission in _missions) {
                missionName.AddItem(mission.name, mission.id);
            }
            missionName.ItemSelected += () => {
                missionName.SetError(missionName.IsDefaultValue());
            };

            // 添加字段
            CustomTextBoxGroup partsName = _editEntityPopUpForm.AddTextBox("物料名称", false,
                (BarCodeMatchingRuleDTO dto, string? value) => dto.name = value ?? null);
            partsName.Hide();
            if (!string.IsNullOrEmpty(dto.name)) {
                partsName.SetValue(0, dto.name);
            }

            CustomTextBoxGroup partNo = _editEntityPopUpForm.AddTextBox("料号", false,
                (BarCodeMatchingRuleDTO dto, string? value) => dto.part_no = value ?? null);
            partNo.Hide();
            if (!string.IsNullOrEmpty(dto.part_no)) {
                partNo.SetValue(0, dto.part_no);
            } else if (dto.id <= 0) {
                partNo.SetValue(0, "1");
            }

            CustomTextBoxGroup serialNum = _editEntityPopUpForm.AddTextBox<int>("物料序号", false, null);
            serialNum.Enabled = false;
            serialNum.Hide();
            // Show or Hide
            type.ItemSelected += () => {
                if (type.Value == BarCodeTypes.PARTS.Id) {
                    partsName.Show();
                    partNo.Show();
                    serialNum.Show();
                    partsName.ResizeChildren();
                    partNo.ResizeChildren();
                    serialNum.ResizeChildren();
                } else {
                    partsName.Hide();
                    partNo.Hide();
                    serialNum.Hide();
                }
                ResizePopUpForm();
            };
            missionName.ItemSelected += () => {
                if (serialNum.Visible && !missionName.IsDefaultValue()) {
                    List<int> ids = _dataDTOList.Where(rule => rule.mission_id == missionName.Value && rule.type == BarCodeTypes.PARTS.Id).OrderBy(rule => rule.id).Select(rule => rule.id).ToList();

                    if (ids.Count > 0) {
                        if (dto.id > 0) {
                            if (ids.Contains(dto.id)) {
                                serialNum.SetValue(0, (ids.IndexOf(dto.id) + 1) + "");
                            } else {
                                serialNum.SetValue(0, "1");
                            }
                        } else {
                            serialNum.SetValue(0, (ids.Count + 1) + "");
                        }
                    } else {
                        serialNum.SetValue(0, "1");
                    }
                } else {
                    serialNum.SetValue(0, "");
                }
            };

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
                CustomTextBox partsNameBox = partsName.GetTextBox(0);
                CustomTextBox partNoBox = partNo.GetTextBox(0);
                CustomTextBox keyCharBox = keyMatchingBox.GetTextBox(1);
                string len = lengthBox.Box.Text;
                string enChar = endCharBox.Box.Text;
                string keyPosition = keyPositionBox.Box.Text;
                string partsNameStr = partsNameBox.Box.Text;
                string partNoStr = partNoBox.Box.Text;
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

                if (type.Value == BarCodeTypes.PARTS.Id) {
                    if (string.IsNullOrEmpty(partsNameStr)) {
                        partsNameBox.IsError = true;
                        check = false;
                        warningMsg += $"{warningIndex++}. 物料名称（物料类型）为必填项\r\n";
                    }
                    if (string.IsNullOrEmpty(partNoStr)) {
                        partNoBox.IsError = true;
                        check = false;
                        warningMsg += $"{warningIndex++}. 料号为必填项\r\n";
                    }
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

            // Set value of type and mission here to check extra boxes
            type.SetCurrent(type.IndexOf(dto.type));
            if (missionName.IndexOf(dto.mission_id) >= 0) {
                missionName.SetCurrent(missionName.IndexOf(dto.mission_id));
            } else if (dto.id > 0) {
                missionName.SetError(true);
                Task.Run(async () => {
                    await Task.Delay(500);
                    WidgetUtils.ShowWarningPopUp("所配置任务不存在或已被删除");
                });
            }

            // Resize all widgets
            ResizePopUpForm();
            // Real show
            _editEntityPopUpForm.Show();
        }
    }
}
