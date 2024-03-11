using Microsoft.Extensions.DependencyInjection;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Configurations;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Constants;
using System.Text;
using System.Security.Cryptography;

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
        public static int LoggedUserId => _user != null && _user.id > 0 ? _user.id : 0;
        public static string LoggedUserName => _user != null && _user.name != null ? _user.name : "UnKnownUser";
        public static bool IsAdmin => _user != null && (_user.role_type == (int) Roles.DEVELOPER || _user.role_type == (int) Roles.ADMIN);

        public static Roles? GetRoleNameByUserId(int id) {
            UserAccountInfoDTO? userAccountInfoDTO;
            if (id == LoggedUserId) {
                userAccountInfoDTO = _user;
            } else {
                userAccountInfoDTO = GetApis().FindUserById(new() { UserId = id }).UserAccountInfoDTO;
            }
            if (userAccountInfoDTO == null) {
                System.Console.WriteLine("Account is not exists");
                return null;
            }
            if (userAccountInfoDTO.role_type == (int) Roles.DEVELOPER) {
                return Roles.DEVELOPER;
            } else if (userAccountInfoDTO.role_type == (int) Roles.ADMIN) {
                return Roles.ADMIN;
            } else if (userAccountInfoDTO.role_type == (int) Roles.OPERATOR) {
                return Roles.OPERATOR;
            }
            System.Console.WriteLine($"Can't find role type: {userAccountInfoDTO.role_type}");
            return null;
        }


        // 获取Apis
        public static OperationGuidanceApis GetApis() {
            apis ??= DependencyInjector.Provider.GetService<OperationGuidanceApis>();
            return apis ?? throw new NullReferenceException("Apis can not be null, please check the Dependency Injector.");
        }

        // MD5加密
        public static string ToMD5String(string originalString) {
            return BitConverter.ToString(MD5.HashData(Encoding.UTF8.GetBytes(originalString))).Replace("-", "");
        }

        /// <summary>
        /// 验证指定长度的MD5
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="length">MD5长度（默认32）</param>
        /// <returns></returns>
        public static bool IsMD5(this string str, int length = 32) {
            if (str.Length < length || str.Length > length)
                return false;
         
            int count = 0;
            var charArray = "0123456789abcdefABCDEF".ToCharArray();
         
            foreach (var c in str.ToCharArray()) {
                if (charArray.Any(x => x == c)) {
                    ++count;
                }
            }
            return count == length;
        }
    }
}
