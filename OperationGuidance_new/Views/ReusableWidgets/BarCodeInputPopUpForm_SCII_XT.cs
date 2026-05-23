using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.HttpObjects.Requests.SCII_XT;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Utils.IIPSC;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BarCodeInputPopUpForm_SCII_XT: ABarCodeInputPopUpForm {
        private string _productBatch;
        private bool inBoundStationOk;

        public BarCodeInputPopUpForm_SCII_XT(AWorkplaceContentPanel workplace,
                string initStr, ProductMissionDTO mission, bool activated,
                Dictionary<int, List<BarCodeMatchingRuleDTO>> productBarCodeRules,
                Dictionary<int, List<BarCodeMatchingRuleDTO>> partsBarCodeRules,
                string? barCode, List<BarCodeMatchingRuleDTO> boltRules, bool isForBolt, string productBatch, bool inBoundStationOk)
            : base(workplace, initStr, mission, activated, productBarCodeRules, partsBarCodeRules, barCode, boltRules, isForBolt) {
            _productBatch = productBatch;
            this.inBoundStationOk = inBoundStationOk;
        }

        protected override async void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);

            await Task.Run(() => {
                if (string.IsNullOrEmpty(_getProcedureCode()) || string.IsNullOrEmpty(_getEquipmentCode())) {
                    // this.Close(); // Don't use this, this will cause thread error
                    _ = this.BeginInvoke((Action) (() => this.Close()));
                }
            });
        }

        public override bool CheckCanActivateMission() {
            if (!base.CheckCanActivateMission() || !inBoundStationOk) {
                return false;
            }

            // 校验：有物料码规则但没有产品码规则时，阻止激活任务
            // 否则第一个物料码会被当作产品码"吃掉"，导致物料码数量不匹配
            if (_partsBarCodeRules.ContainsKey(_mission.id)
                && _partsBarCodeRules[_mission.id].Count > 0
                && !_productBarCodeRules.ContainsKey(_mission.id)) {
                WidgetUtils.ShowWarningPopUp("当前任务配置了物料码规则但未配置产品码（追溯码）规则，请先配置产品码规则后再操作。");
                return false;
            }

            // 点检任务跳过后续逻辑，直接通过
            if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
                return true;
            }

            // 向 MES 上传配件绑定
            if (_partsBarCodeRules.ContainsKey(_mission.id)) {
                var partsRules = _partsBarCodeRules[_mission.id]
                    .Where(r => r.type == BarCodeTypes.PARTS.Id)
                    .OrderBy(r => r.id)
                    .ToList();

                if (partsRules.Count > 0 && _workplace.BarCodeObj.PartsBarCodes.Count > 0) {
                    var accessories = new List<SCII_XT_BindAccessoryReq.Accessory>();
                    for (int i = 0; i < _workplace.BarCodeObj.PartsBarCodes.Count; i++) {
                        int ruleId = _workplace.BarCodeObj.PartsMatchingRulesCached[i];
                        var rule = partsRules.FirstOrDefault(r => r.id == ruleId);
                        if (rule != null) {
                            accessories.Add(new SCII_XT_BindAccessoryReq.Accessory {
                                accessoryCode = _workplace.BarCodeObj.PartsBarCodes[i],
                                accessoryType = rule.name ?? "",
                                partNo = rule.part_no ?? "",
                                orderId = partsRules.IndexOf(rule) + 1,
                            });
                        }
                    }

                    if (accessories.Count > 0) {
                        SciiXtConfig config = ConfigUtils.LoadConfig<SciiXtConfig>();
                        var req = new SCII_XT_BindAccessoryReq() {
                            productCode = _workplace.BarCodeObj.ProductBarCode,
                            procedureCode = config.procedure_code,
                            recipeCode = _mission.name,
                            accessorys = accessories,
                            createBy = SystemUtils.UserInfo.staff_id,
                            employeeNumber = SystemUtils.UserInfo.account,
                        };

                        var dto = Task.Run(async () => await Workflow_SCII_XT.BindAccessory(req))
                                      .GetAwaiter()
                                      .GetResult();
                        if (!dto.bindSuccess) {
                            string msg = $"配件绑定请求失败，详细信息：{dto.message}";
                            logger.Warn(msg);
                            WidgetUtils.ShowWarningPopUp(msg);
                            return false;
                        }
                        logger.Info("配件绑定成功。");
                    }
                }
            }

            // 第二打印机扫码打印
            var printerConfig = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();
            if (printerConfig.enabled_second.ToYesOrNoBool()) {
                if (printerConfig.second_printer_name == null) {
                    WidgetUtils.ShowWarningPopUp("条码打印机（第二台）名称配置未设置，请先配置打印机。");
                    return false;
                }
                CheckSecondBarCode();
            }

            logger.Info($"All checks passed (version SCII_XT) for mission = [id = {_mission.id}]...");
            return true;
        }

        private void CheckSecondBarCode() {
            WorkplaceContentPanel_SCII_XT workplace = (WorkplaceContentPanel_SCII_XT) _workplace;

            WaitDialog dialog = new("二维码信息") {
                Title = "二维码信息录入",
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
                BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND,
            };
            dialog.PretendToShowToCreateHandlesForChildren();

            workplace.BarcodeDialog = dialog;
            workplace.CanReceiveBarcode = true;

            dialog.TextBox.GetTextBox(0).Box.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    workplace.ProcessSecondBarCode();
                }
            };

            int contentWidth = (int) (WidgetUtils.MainSize.Width * .65);
            Padding contentPadding = dialog.ContentPanel.Padding;
            int boxHeight = WidgetUtils.TextOrComboBoxHeight();
            int boxMargin = boxHeight / 5;
            dialog.TextBox.Size = new(contentWidth - contentPadding.Size.Width - boxMargin * 2, boxHeight);
            dialog.TextBox.Margin = new(boxMargin);
            int contentHeight = boxHeight + boxMargin * 2 + contentPadding.Size.Height;
            dialog.SetContentSizeAndSelfSize(new(contentWidth, contentHeight));
            dialog.Show();
        }


        protected override bool ProductBarCodeExtraCheck(string barCode) {
            if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
                inBoundStationOk = true;
                return true;
            }
            // 向 MES 发出进站请求
            WorkplaceContentPanel_SCII_XT workplace = (WorkplaceContentPanel_SCII_XT) _workplace;
            inBoundStationOk = workplace.InBound(barCode, _productBatch);

            if (inBoundStationOk) {
                // 向打印机发送指令
                _ = workplace.SendToPrinter();
            }

            return inBoundStationOk;
        }
        protected override bool PartsBarCodeExtraCheck(int ruleId) {
            if (!base.PartsBarCodeExtraCheck(ruleId)) {
                return false;
            }
            return true;
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
    }
}
