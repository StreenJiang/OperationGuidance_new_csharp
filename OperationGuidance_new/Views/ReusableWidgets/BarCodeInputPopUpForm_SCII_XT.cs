using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.HttpObjects.Requests.SCII_XT;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BarCodeInputPopUpForm_SCII_XT: BarCodeInputPopUpForm_SCII {
        private string _productBatch;

        public BarCodeInputPopUpForm_SCII_XT(AWorkplaceContentPanel workplace,
                string initStr, ProductMissionDTO mission, bool activated,
                Dictionary<int, List<BarCodeMatchingRuleDTO>> productBarCodeRules,
                Dictionary<int, List<BarCodeMatchingRuleDTO>> partsBarCodeRules,
                string? barCode, List<BarCodeMatchingRuleDTO> boltRules, bool isForBolt, string productBatch)
            : base(workplace, initStr, mission, activated, productBarCodeRules, partsBarCodeRules, barCode, boltRules, isForBolt) {
            _productBatch = productBatch;
        }

        public override bool CheckCanActivateMission() {
            if (base.CheckCanActivateMission()) {
                // 向 MES 发送配件绑定请求
                if (_partsBarCodeRules.ContainsKey(_mission.id) && _partsBarCodeRules[_mission.id].Count > 0) {
                    var req = new SCII_XT_BindAccessoryReq() {
                        productCode = _workplace.BarCodeObj.ProductBarCode,
                        procedureCode = "", // TODO: 工序编码
                        recipeCode = _mission.name,
                    };
                    var accessorys = new List<SCII_XT_BindAccessoryReq.Accessory>();

                    for (int i = 0; i < _partsBarCodeRules[_mission.id].Count; i++) {
                        BarCodeMatchingRuleDTO rule = _partsBarCodeRules[_mission.id][i];
                        string partsBarCode = _workplace.BarCodeObj.PartsBarCodes[i];

                        accessorys.Add(new() {
                            accessoryCode = partsBarCode,
                            accessoryType = rule.name ?? "",
                            partNo = "?", // TODO: 需要澄清
                            orderId = i + 1, // TODO: 同上
                        });
                    }
                    req.accessorys = accessorys;

                    // Send request
                    var dto = Task.Run(async () => await Workflow_SCII_XT.BindAccessory(req))
                                  .GetAwaiter()
                                  .GetResult();
                    if (!dto.bindSuccess) {
                        WidgetUtils.ShowWarningPopUp($"配件绑定请求失败，详细信息：{dto.message}");
                    }
                    return dto.bindSuccess;
                }
            }

            logger.Info($"All checks passed (version SCII_XT) for mission = [id = {_mission.id}]...");
            return true;
        }

        protected override bool ProductBarCodeExtraCheck(string barCode) {
            // 向 MES 发出进站请求
            var req = new SCII_XT_InOrOutBoundStationReq() {
                productCode = barCode,
                passType = (int) SCII_XT_ProductType.PRODUCT,
                recipeCode = _mission.name,
                procedureCode = "", // TODO: 工序编码
                equipmentCode = "", // TODO: 设备编码
                batchNo = _productBatch,
            };

            // Send request
            var dto = Task.Run(async () => await Workflow_SCII_XT.InBoundStation(req))
                          .GetAwaiter()
                          .GetResult();
            if (!dto.inOrOutSuccess) {
                WidgetUtils.ShowWarningPopUp($"进站请求失败，详细信息：{dto.message}");
            }
            return dto.inOrOutSuccess;
        }
        protected override bool PartsBarCodeExtraCheck(int ruleId) => true;

        private string _getProcedureCode() {
            string defaultProcedureCode = MainUtils.Config_SCII_XT.Read(ConfigName_SCII_XT.DefaultProcedureCode);
            if (string.IsNullOrEmpty(defaultProcedureCode)) {
                WidgetUtils.ShowWarningPopUp("【默认工序编码】未配置，请检查配置信息。");
            }
            return defaultProcedureCode;
        }

        private string _getBatchNo() {
            string defaultBatchNo = MainUtils.Config_SCII_XT.Read(ConfigName_SCII_XT.DefaultBatchNo);
            if (string.IsNullOrEmpty(defaultBatchNo)) {
                WidgetUtils.ShowWarningPopUp("【默认批次号】未配置，请检查配置信息。");
            }
            return defaultBatchNo;
        }
    }
}
