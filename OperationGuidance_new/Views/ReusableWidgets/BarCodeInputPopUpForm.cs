using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BarCodeInputPopUpForm: ABarCodeInputPopUpForm {
        public BarCodeInputPopUpForm(AWorkplaceContentPanel workplace, string initStr, ProductMissionDTO mission, bool activated, Dictionary<int, List<BarCodeMatchingRuleDTO>> productBarCodeRules, Dictionary<int, List<BarCodeMatchingRuleDTO>> partsBarCodeRules, string? barCode, List<BarCodeMatchingRuleDTO> boltRules) : base(workplace, initStr, mission, activated, productBarCodeRules, partsBarCodeRules, barCode, boltRules) {
        }

        protected override bool PartsBarCodeExtraCheck(int ruleId) => true;
    }
}
