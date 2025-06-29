using System.Runtime.InteropServices;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BarCodeInputPopUpForm_TZYX: ABarCodeInputPopUpForm {
        [DllImport("user32.dll")]
        private static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        public BarCodeInputPopUpForm_TZYX(AWorkplaceContentPanel workplace, string initStr, ProductMissionDTO mission, bool activated, Dictionary<int, List<BarCodeMatchingRuleDTO>> productBarCodeRules, Dictionary<int, List<BarCodeMatchingRuleDTO>> partsBarCodeRules, string? barCode, List<BarCodeMatchingRuleDTO> boltRules) : base(workplace, initStr, mission, activated, productBarCodeRules, partsBarCodeRules, barCode, boltRules) {
        }

        protected override bool PartsBarCodeExtraCheck(int ruleId) => true;

        protected override void OnShown(EventArgs e) {
            base.OnShown(e);

            // 保持窗口在当前位置，不激活，不改变Z顺序
            SetWindowPos(this.Handle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOZORDER | SWP_NOACTIVATE);
        }
    }
}
