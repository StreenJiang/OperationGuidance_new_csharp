using CustomLibrary.Utils;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class WorkplaceTopBar_SCII: WorkplaceTopBar {
        protected override void ExitConfirm() {
            if (OperatorOpenning) {
                Workplace.AdminConfirmed = false;
                bool isChecked = false;
                Workplace.OpenAdminPasswordPopUpForm("退出登录，请管理员输入权限密码", false, yes => isChecked = yes);
                if (isChecked) {
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
