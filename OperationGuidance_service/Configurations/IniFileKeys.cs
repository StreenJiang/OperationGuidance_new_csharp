namespace OperationGuidance_service.Configurations {
    public static class IniFileKeys {
        public static string DatabaseType { get; set; } = "database_type";

        public static string DatabaseConfigMYSQL_server { get; set; } = "database_config_mysql_server";
        public static string DatabaseConfigMYSQL_port { get; set; } = "database_config_mysql_port";
        public static string DatabaseConfigMYSQL_database { get; set; } = "database_config_mysql_database";
        public static string DatabaseConfigMYSQL_user { get; set; } = "database_config_mysql_user";
        public static string DatabaseConfigMYSQL_password { get; set; } = "database_config_mysql_password";

        public static string DatabaseConfigSQLITE_database { get; set; } = "database_config_sqlite_database";
        public static string DatabaseConfigSQLITE_path { get; set; } = "database_config_sqlite_path";
    }
}
