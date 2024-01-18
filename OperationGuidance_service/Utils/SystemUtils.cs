using Microsoft.Extensions.DependencyInjection;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Configurations;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Utils {
    public static class SystemUtils {
        private static UserAccountInfoDTO? _user;
        private static OperationGuidanceApis? apis;

        public static UserAccountInfoDTO UserInfo {
            set {
                _user = value;
            } get {
                if (_user == null) {
                    _user = new() {
                        id = 1,
                        name = "UnknownUser",
                    };
                }
                return _user;
            }
        }

        public static int LoggedUserId() {
            return _user != null && _user.id > 0 ? _user.id : 0;
        }

        public static string LoggedUserName() {
            return _user != null && _user.name != null ? _user.name : "UnKnownUser";
        }

        // 获取Apis
        public static OperationGuidanceApis GetApis() {
            apis ??= DependencyInjector.Provider.GetService<OperationGuidanceApis>();
            return apis ?? throw new NullReferenceException("Apis can not be null, please check the Dependency Injector.");
        }
    }
}
