using log4net;
using log4net.Config;
using Microsoft.Extensions.DependencyInjection;
using OperationGuidance_service.Configurations;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Database;
using OperationGuidance_service.Models.DTOs;
using System.Security.Cryptography;
using System.Text;

namespace OperationGuidance_service.Utils {
    public static class SystemUtils {
        private static UserAccountInfoDTO? _user;
        private static OperationGuidanceApis? apis;
        public static MacAddressesDTO MacAddressesDTO { get; set; }

        static SystemUtils() {
            XmlConfigurator.Configure();
        }
        public static ILog GetLogger(Type type) => LogManager.GetLogger(type);

        public static UserAccountInfoDTO UserInfo {
            set {
                _user = value;
            }
            get {
                if (_user == null) {
                    _user = new() {
                        id = 1,
                        name = "UnknownUser",
                    };
                }
                return _user;
            }
        }
        public static int LoggedUserId => _user != null && _user.id > 0 ? _user.id : 1;
        public static string LoggedUserName => _user != null && _user.name != null ? _user.name : "UnKnownUser";
        public static bool IsAdmin => _user != null && (_user.role_type == (int) Roles.DEVELOPER || _user.role_type == (int) Roles.ADMIN);

        private static IniFileUtil DatabaseConfigs { get; } = new();

        // Data base type
        public static DBTypes GetDBTypes() {
            string dbType = DatabaseConfigs.Read(IniFileKeys.DatabaseType);
            if (string.IsNullOrEmpty(dbType)) {
                dbType = DBTypes.SQLITE + "";
                DatabaseConfigs.Write(IniFileKeys.DatabaseType, dbType);
            }
            return (DBTypes) Enum.Parse(typeof(DBTypes), dbType);
        }
        public static string GetDataBase() => DatabaseConfigs.Read(IniFileKeys.DatabaseConfigMYSQL_database);
        public static bool GetDBInitEnabled() {
            string initEnabled = DatabaseConfigs.Read(IniFileKeys.InitEnabled);
            if (string.IsNullOrEmpty(initEnabled)) {
                initEnabled = (int) YesOrNo.NO + "";
                DatabaseConfigs.Write(IniFileKeys.InitEnabled, initEnabled);
            }
            return initEnabled == (int) YesOrNo.YES + "";
        }
        public static void SetDBInitEnabled(bool enabled) {
            if (enabled) {
                DatabaseConfigs.Write(IniFileKeys.InitEnabled, (int) YesOrNo.YES + "");
            } else {
                DatabaseConfigs.Write(IniFileKeys.InitEnabled, (int) YesOrNo.NO + "");
            }
        }
        // Mysql and SqlServer
        public static void InitMySqlAndSqlServerConfigs() {
            string server = DatabaseConfigs.Read(IniFileKeys.DatabaseConfigMYSQL_server);
            string port = DatabaseConfigs.Read(IniFileKeys.DatabaseConfigMYSQL_port);
            string database = DatabaseConfigs.Read(IniFileKeys.DatabaseConfigMYSQL_database);
            string user = DatabaseConfigs.Read(IniFileKeys.DatabaseConfigMYSQL_user);
            string password = DatabaseConfigs.Read(IniFileKeys.DatabaseConfigMYSQL_password);

            if (string.IsNullOrEmpty(server)
                || string.IsNullOrEmpty(port)
                || string.IsNullOrEmpty(database)
                || string.IsNullOrEmpty(user)
                || string.IsNullOrEmpty(password)
            ) {
                MySqlConnector.Server = "localhost";
                MySqlConnector.Port = "3306";
                MySqlConnector.Database = "aneng";
                MySqlConnector.User = "aneng";
                MySqlConnector.Password = "aneng123";
                DatabaseConfigs.Write(IniFileKeys.DatabaseConfigMYSQL_server, MySqlConnector.Server);
                DatabaseConfigs.Write(IniFileKeys.DatabaseConfigMYSQL_port, MySqlConnector.Port);
                DatabaseConfigs.Write(IniFileKeys.DatabaseConfigMYSQL_database, MySqlConnector.Database);
                DatabaseConfigs.Write(IniFileKeys.DatabaseConfigMYSQL_user, MySqlConnector.User);
                DatabaseConfigs.Write(IniFileKeys.DatabaseConfigMYSQL_password, MySqlConnector.Password);
            } else {
                MySqlConnector.Server = server;
                MySqlConnector.Port = port;
                MySqlConnector.Database = database;
                MySqlConnector.User = user;
                MySqlConnector.Password = password;
            }

            // SqlServer
            SqlServerConnector.Server = MySqlConnector.Server;
            SqlServerConnector.Port = MySqlConnector.Port;
            SqlServerConnector.Database = MySqlConnector.Database;
            SqlServerConnector.User = MySqlConnector.User;
            SqlServerConnector.Password = MySqlConnector.Password;
        }
        // Sqlite
        public static void InitSQLiteConfigs() {
            string database = DatabaseConfigs.Read(IniFileKeys.DatabaseConfigSQLITE_database);
            string path = DatabaseConfigs.Read(IniFileKeys.DatabaseConfigSQLITE_path);

            if (string.IsNullOrEmpty(database) || string.IsNullOrEmpty(path)) {
                SQLiteConnector.Database = "database.db";
                SQLiteConnector.Path = "OperationGuidance_service\\Database\\";
                DatabaseConfigs.Write(IniFileKeys.DatabaseConfigSQLITE_database, SQLiteConnector.Database);
                DatabaseConfigs.Write(IniFileKeys.DatabaseConfigSQLITE_path, SQLiteConnector.Path);
            } else {
                SQLiteConnector.Database = database;
                SQLiteConnector.Path = path;
            }
        }

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

        // MD4加密
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

        public static bool ShowConfirmPopUp(string message) => MessageBox.Show(null, message, "请确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        public static DialogResult ShowNoticePopUp(string message) => MessageBox.Show(null, message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        public static DialogResult ShowWarningPopUp(string message) => MessageBox.Show(null, message, "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        public static DialogResult ShowErrorPopUp(string message) => MessageBox.Show(null, message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
