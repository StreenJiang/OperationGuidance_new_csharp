using CustomLibrary.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BarCodeInputPopUpForm_SCII: ABarCodeInputPopUpForm {

        public BarCodeInputPopUpForm_SCII(AWorkplaceContentPanel workplace, string initStr, ProductMissionDTO mission, bool activated, Dictionary<int, List<BarCodeMatchingRuleDTO>> productBarCodeRules, Dictionary<int, List<BarCodeMatchingRuleDTO>> partsBarCodeRules, string? barCode, List<BarCodeMatchingRuleDTO> boltRules) : base(workplace, initStr, mission, activated, productBarCodeRules, partsBarCodeRules, barCode, boltRules) { }

        protected override bool PartsBarCodeExtraCheck(int ruleId) {
            if (base.PartsBarCodeExtraCheck(ruleId)) {
                // Check the order of all parts bar codes
                List<BarCodeMatchingRuleDTO> rulesTemp = _partsBarCodeRules[_mission.id];

                // Filter out all rules that had been saved
                rulesTemp = rulesTemp.Where(rule => _workplace.BarCodeObj.PartsMatchingRulesCached.Any(id => id == rule.id)).ToList();

                // Filter out all rules that bound to bolts
                rulesTemp = rulesTemp.Where(rule => !_rulesExcluded.Any(r => r.id == rule.id)).ToList();

                // Checking
                BarCodeMatchingRuleDTO? barCodeMatchingRuleDTO = rulesTemp.SingleOrDefault(rule => rule.id == ruleId);
                if (barCodeMatchingRuleDTO != null) {
                    int index = rulesTemp.IndexOf(barCodeMatchingRuleDTO);
                    if (_workplace.BarCodeObj.PartsMatchingRulesCached.Count != index) {
                        WidgetUtils.ShowWarningPopUp("请按顺序依次录入物料码");
                        return false;
                    }
                }
            } else {
                return false;
            }

            return true;
        }
    }
}
