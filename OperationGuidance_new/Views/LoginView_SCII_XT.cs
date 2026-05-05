using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class LoginView_SCII_XT: LoginView {
        public LoginView_SCII_XT(Size size, Image back, Action<Size> afterLogin, Size mainFormSize) :
          base(size, back, afterLogin, mainFormSize) {
        }

        protected override void CheckLoginByApi(string account, string password) {
            if (account == "admin") {
                base.CheckLoginByApi(account, password);
            } else {
                var config = ConfigUtils.LoadConfig<SciiXtConfig>();
                if (string.IsNullOrEmpty(config.equipment_code)) {
                    WidgetUtils.ShowErrorPopUp(this, "设备编码不能为空，无法登录。请检查 SciiXtConfig 配置。");
                    return;
                }
                var dto = Task.Run(async () => await Workflow_SCII_XT.OperatorLogin(new(account, password, config.equipment_code)))
                              .GetAwaiter()
                              .GetResult();

                if (!dto.loginSuccess) {
                    string msg = $"登录出错。详细信息：{dto.message}";
                    log.Warn(msg);
                    WidgetUtils.ShowErrorPopUp(this, msg);
                } else {
                    var dto2 = Task.Run(async () => await Workflow_SCII_XT.UserInfo(dto.userId))
                                  .GetAwaiter()
                                  .GetResult();

                    if (dto2 != null) {
                        var userAccountInfoDTO = new UserAccountInfoDTO();
                        userAccountInfoDTO.id = dto.userId;
                        userAccountInfoDTO.staff_id = dto.userId;
                        userAccountInfoDTO.name = !string.IsNullOrEmpty(dto2.employeeName) ? dto2.employeeName : "未知";
                        userAccountInfoDTO.position = !string.IsNullOrEmpty(dto2.roleName) ? dto2.roleName : "未知";
                        userAccountInfoDTO.account = account;
                        SystemUtils.UserInfo = userAccountInfoDTO;

                        log.Info($"【{userAccountInfoDTO.id}-{userAccountInfoDTO.staff_id}-{userAccountInfoDTO.name}-{userAccountInfoDTO.account}】成功登录。");
                        ActionAfterLogin();
                    } else {
                        string errMsg = $"SCII_XT：找不到 [id = {dto.userId}] 对应的用户信息！";
                        log.Error(errMsg);
                        // WidgetUtils.ShowErrorPopUp(this, errMsg);
                    }
                }

            }
        }
    }
}
