namespace OperationGuidance_service.Configurations {
    public static class DBConfig {
        // public static readonly DBTypes DBType = DBTypes.SQLITE;
        public static readonly DBTypes DBType = DBTypes.MYSQL;
    }

    public enum DBTypes { 
        SQLITE,
        MYSQL,
    }
}
