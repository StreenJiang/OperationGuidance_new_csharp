namespace OperationGuidance_new.Configs {
    public static class IniFileKeys {
        public static string Resolution { get; set; } = "resolution";

        public static string DataStoragePath { get; set; } = "data_storage_path";
        public static string DataStorageFieldsSort { get; set; } = "data_storage_fields";
        public static string DataStorageFieldsSortCurr { get; set; } = "data_storage_fields_curr";
        public static string DataStorageNameFormat { get; set; } = "data_storage_name_format";
        public static string DataStorageStoreLooseningData { get; set; } = "data_storage_store_loosening_data";

        public static string MissionArmLocatingEnabled { get; set; } = "mission_arm_locating_enabled";
        public static string MissionArmLocatingAccuracy { get; set; } = "mission_arm_locating_accuracy";
        public static string MissionSelfLoopingMode { get; set; } = "mission_self_looping_mode";
        public static string PLCBarCodeSelfLooping { get; internal set; } = "plc_bar_code_self_looping";
        public static string PLCBarCodeStartAddress { get; internal set; } = "plc_bar_code_start_address";
        public static string PLCBarCodeLength { get; internal set; } = "plc_bar_code_length";
    }
}
