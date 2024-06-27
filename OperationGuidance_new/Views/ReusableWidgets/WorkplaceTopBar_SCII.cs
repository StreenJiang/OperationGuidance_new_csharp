using CustomLibrary.Utils;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class WorkplaceTopBar_SCII: WorkplaceTopBar {
        protected override void ExitConfirm() {
            if (OperatorOpenning) {
                Workplace.AdminConfirmed = false;
                Workplace.OpenAdminPasswordPopUpForm("退出登录，请管理员输入权限密码", false);
                if (Workplace.AdminConfirmed.Value) {
                    if (WidgetUtils.BackToLoginView != null) {
                        MainUtils.ActionAfterLogout = CloseWorkplace;
                        WidgetUtils.BackToLoginView(false);
                    }
                }
                Workplace.AdminConfirmed = null;
            } else {
                CloseWorkplace();
            }
        }
    }
}
