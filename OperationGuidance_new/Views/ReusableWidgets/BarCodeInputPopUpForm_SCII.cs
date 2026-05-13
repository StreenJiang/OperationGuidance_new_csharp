using CustomLibrary.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BarCodeInputPopUpForm_SCII: ABarCodeInputPopUpForm {

        public BarCodeInputPopUpForm_SCII(AWorkplaceContentPanel workplace,
                string initStr, ProductMissionDTO mission, bool activated,
                Dictionary<int, List<BarCodeMatchingRuleDTO>> productBarCodeRules,
                Dictionary<int, List<BarCodeMatchingRuleDTO>> partsBarCodeRules,
                string? barCode, List<BarCodeMatchingRuleDTO> boltRules, bool isForBolt)
            : base(workplace, initStr, mission, activated, productBarCodeRules, partsBarCodeRules, barCode, boltRules, isForBolt) { }

        protected override bool PartsBarCodeExtraCheck(int ruleId) {
            // 1. 基础检查
            if (!base.PartsBarCodeExtraCheck(ruleId)) {
                return false;
            }

            // 2. 获取"所有有效规则"（按顺序），排除绑定的螺栓规则
            var allValidRules = _partsBarCodeRules[_mission.id]
                                    .Where(rule => !_rulesExcluded.Any(r => r.id == rule.id))
                                    .ToList();

            // 3. 找到当前规则在所有规则中的期望位置（索引）
            var currentRule = allValidRules.SingleOrDefault(r => r.id == ruleId);
            if (currentRule == null) {
                // 规则不存在或已被排除，跳过检查
                return true;
            }

            int expectedIndex = allValidRules.IndexOf(currentRule);

            // 4. 计算"已保存的有效规则"数量（只统计 allValidRules 中的）
            int savedCount = _workplace.BarCodeObj.PartsMatchingRulesCached
                                .Count(savedId => allValidRules.Any(r => r.id == savedId));

            // 5. 核心检查：已保存数量 == 当前规则的期望索引
            //    例如：录入第3个规则(索引2)时，应该已保存2个规则
            if (savedCount != expectedIndex) {
                WidgetUtils.ShowWarningPopUp("请按顺序依次录入物料码");
                return false;
            }

            return true;
        }

        protected override void CheckProductBarCodeErrorForChallenge() {
            if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
                logger.Info($"*Current mission id = [{_mission.id}] is a challenge mission...");
                _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PRODUCT_BAR_CODE_ERROR);
            }
        }

        protected override void CheckProductPredecessorForChallenge(bool predecessorExists, DateTime? createTime) {
            if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
                logger.Info($"*Current mission id = [{_mission.id}] is a challenge mission...");
                if (!predecessorExists || (createTime != null && createTime.Value.Date != DateTime.Now.Date)) {
                    _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PRODUCT_PREDECESSOR);
                }
            }
        }

        protected override void CheckProductBarCodeRedoForChallenge() {
            if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
                logger.Info($"*Current mission id = [{_mission.id}] is a challenge mission, checking REDO...");
                _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PRODUCT_BAR_CODE_REDO);
            }
        }

        protected override void CheckPartsBarCodeErrorForChallenge() {
            if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
                logger.Info($"*Current mission id = [{_mission.id}] is a challenge mission, checking parts bar code...");
                _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PARTS_BAR_CODE_ERROR);
            }
        }

        protected override void CheckPartsPredecessorForChallenge(bool predecessorExists, DateTime? createTime) {
            if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
                logger.Info($"*Current mission id = [{_mission.id}] is a challenge mission...");
                if (!predecessorExists || (createTime != null && createTime.Value.Date != DateTime.Now.Date)) {
                    _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PARTS_PREDECESSOR);
                }
            }
        }

        protected override void CheckPartsBarCodeRedoForChallenge() {
            if (_mission.is_challenge_mission == (int) YesOrNo.YES) {
                logger.Info($"*Current mission id = [{_mission.id}] is a challenge mission, checking REDO...");
                _workplace.AddChallengeResult(_mission.id, ChallengeTaskEnum.PARTS_BAR_CODE_REDO);
            }
        }
    }
}
