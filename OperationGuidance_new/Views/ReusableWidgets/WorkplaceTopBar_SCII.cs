using CustomLibrary.Utils;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class WorkplaceTopBar_SCII: WorkplaceTopBar {
        protected override void ExitConfirm() {
            if (OperatorOpenning) {
                bool confirmed = Workplace.OpenAdminPasswordPopUpForm("退出登录，请管理员输入权限密码", false);
                if (confirmed) {
                    if (WidgetUtils.BackToLoginView != null) {
                        MainUtils.ActionAfterLogout = CloseWorkplace;
                        WidgetUtils.BackToLoginView(false);
                    }
                }
            } else {
                CloseWorkplace();
            }
        }
    }
}
