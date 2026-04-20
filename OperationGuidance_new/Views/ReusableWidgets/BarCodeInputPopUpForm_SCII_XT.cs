using CustomLibrary.Utils;
using Newtonsoft.Json;
using OperationGuidance_new.Configs;
using OperationGuidance_new.HttpObjects.Requests.SCII_XT;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BarCodeInputPopUpForm_SCII_XT: BarCodeInputPopUpForm_SCII {
        private string _productBatch;
        private bool inBoundStationOk;
        private bool bindAccessoryOk;

        public BarCodeInputPopUpForm_SCII_XT(AWorkplaceContentPanel workplace,
                string initStr, ProductMissionDTO mission, bool activated,
                Dictionary<int, List<BarCodeMatchingRuleDTO>> productBarCodeRules,
                Dictionary<int, List<BarCodeMatchingRuleDTO>> partsBarCodeRules,
                string? barCode, List<BarCodeMatchingRuleDTO> boltRules, bool isForBolt, string productBatch, bool inBoundStationOk)
            : base(workplace, initStr, mission, activated, productBarCodeRules, partsBarCodeRules, barCode, boltRules, isForBolt) {
            _productBatch = productBatch;
            this.inBoundStationOk = inBoundStationOk;
            bindAccessoryOk = false;
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
            if (base.CheckCanActivateMission() && inBoundStationOk) {

                // 向 MES 发送绑定上盖请求
                SciiXtConfig config = ConfigUtils.LoadConfig<SciiXtConfig>();
                // 只有一个工站会用这个，所以要加一个配置
                if (config.send_upper_cover == (int) YesOrNo.YES) {
                    if (_partsBarCodeRules.ContainsKey(_mission.id) && _partsBarCodeRules[_mission.id].Count > 0) {
                        var req = new SCII_XT_BindUpperCoverReq() {
                            productCode = _workplace.BarCodeObj.ProductBarCode,
                            upperCoverCode = _workplace.BarCodeObj.PartsBarCodes[0],
                            employeeNumber = SystemUtils.UserInfo.account,
                        };

                        // Send request
                        var dto = Task.Run(async () => await Workflow_SCII_XT.BindUppderCover(req))
                                      .GetAwaiter()
                                      .GetResult();
                        if (!dto.bindSuccess) {
                            string msg = $"上盖绑定请求失败，详细信息：{dto.message}";
                            logger.Warn(msg);
                            WidgetUtils.ShowWarningPopUp(msg);
                        } else {
                            logger.Info($"【{_workplace.BarCodeObj.PartsBarCodes[0]}】上盖绑定成功。");
                            bindAccessoryOk = true;
                        }
                        return dto.bindSuccess;
                    }
                }

                logger.Info($"All checks passed (version SCII_XT) for mission = [id = {_mission.id}]...");
                return true;
            }

            return false;
        }

        protected override bool ProductBarCodeExtraCheck(string barCode) {
            // 向 MES 发出进站请求
            WorkplaceContentPanel_SCII_XT workplace = (WorkplaceContentPanel_SCII_XT) _workplace;
            inBoundStationOk = workplace.InBound(barCode, _productBatch);

            if (inBoundStationOk) {
                // 向打印机发送指令
                _ = workplace.SendToPrinter();
            }

            return inBoundStationOk;
        }
        protected override bool PartsBarCodeExtraCheck(int ruleId) => true;

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
