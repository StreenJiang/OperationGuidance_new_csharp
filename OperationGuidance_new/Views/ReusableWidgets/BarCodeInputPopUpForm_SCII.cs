using CustomLibrary.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BarCodeInputPopUpForm_SCII: ABarCodeInputPopUpForm {
        public BarCodeInputPopUpForm_SCII(AWorkplaceContentPanel workplace, string initStr, ProductMissionDTO mission, bool activated, Dictionary<int, List<BarCodeMatchingRuleDTO>> productBarCodeRules, Dictionary<int, List<BarCodeMatchingRuleDTO>> partsBarCodeRules, string? barCode) : base(workplace, initStr, mission, activated, productBarCodeRules, partsBarCodeRules, barCode) {
        }

        protected override bool PartsBarCodeExtraCheck(int ruleId) {
            List<BarCodeMatchingRuleDTO> rules = _partsBarCodeRules[_mission.id];
            int index = rules.IndexOf(rules.Single(rule => rule.id == ruleId));
            if (_workplace.BarCodeObj.PartsMatchingRulesCached.Count == index) {
                return true;
            }

            WidgetUtils.ShowWarningPopUp("请按顺序依次录入物料码");
            return false;
        }
    }
}
