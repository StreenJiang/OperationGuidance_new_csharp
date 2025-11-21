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
                string? barCode, List<BarCodeMatchingRuleDTO> boltRules, bool isForBolt, string productBatch)
            : base(workplace, initStr, mission, activated, productBarCodeRules, partsBarCodeRules, barCode, boltRules, isForBolt) {
            _productBatch = productBatch;
            inBoundStationOk = false;
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
                // 向 MES 发送配件绑定请求
                if (_partsBarCodeRules.ContainsKey(_mission.id) && _partsBarCodeRules[_mission.id].Count > 0) {
                    var req = new SCII_XT_BindAccessoryReq() {
                        productCode = _workplace.BarCodeObj.ProductBarCode,
                        procedureCode = _getProcedureCode(),
                        recipeCode = _mission.name,
                        createBy = SystemUtils.UserInfo.id,
                        employeeNumber = SystemUtils.UserInfo.account,
                    };
                    var accessorys = new List<SCII_XT_BindAccessoryReq.Accessory>();

                    for (int i = 0; i < _partsBarCodeRules[_mission.id].Count; i++) {
                        BarCodeMatchingRuleDTO rule = _partsBarCodeRules[_mission.id][i];
                        string partsBarCode = _workplace.BarCodeObj.PartsBarCodes[i];

                        accessorys.Add(new() {
                            accessoryCode = partsBarCode,
                            accessoryType = rule.name ?? "",
                            partNo = rule.part_no ?? "",
                            orderId = i + 1,
                        });
                    }
                    req.accessorys = accessorys;

                    // Send request
                    var dto = Task.Run(async () => await Workflow_SCII_XT.BindAccessory(req))
                                  .GetAwaiter()
                                  .GetResult();
                    if (!dto.bindSuccess) {
                        string msg = $"配件绑定请求失败，详细信息：{dto.message}";
                        logger.Warn(msg);
                        WidgetUtils.ShowWarningPopUp(msg);
                    } else {
                        logger.Info($"【{JsonConvert.SerializeObject(accessorys)}】配件绑定成功。");
                        bindAccessoryOk = true;
                    }
                    return dto.bindSuccess;
                }

                logger.Info($"All checks passed (version SCII_XT) for mission = [id = {_mission.id}]...");
                return true;
            }

            return false;
        }

        protected override bool ProductBarCodeExtraCheck(string barCode) {
            // 向 MES 发出进站请求
            var req = new SCII_XT_InOrOutBoundStationReq() {
                productCode = barCode,
                passType = (int) SCII_XT_PassType.PRODUCT,
                recipeCode = _mission.name,
                procedureCode = _getProcedureCode(),
                equipmentCode = _getEquipmentCode(),
                batchNo = _productBatch,
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
                inBoundStationOk = true;
            }
            return dto.inOrOutSuccess;
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
