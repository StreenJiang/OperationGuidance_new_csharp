namespace OperationGuidance_new.Configs {
    public static class IniFileKeys {
        public static string Resolution => "resolution";

        public static string DataStoragePath => "data_storage_path";
        public static string DataStorageFieldsSort => "data_storage_fields";
        public static string DataStorageFieldsSortCurr => "data_storage_fields_curr";
        public static string DataStorageNameFormat => "data_storage_name_format";
        public static string DataStorageStoreLooseningData => "data_storage_store_loosening_data";

        public static string MissionArmLocatingEnabled => "mission_arm_locating_enabled";
        public static string MissionArmLocatingAccuracy => "mission_arm_locating_accuracy";
        public static string MissionErrorPromptForArmEnabled => "mission_error_prompt_for_arm";
        public static string MissionErrorPromptForWrongBarcode => "mission_error_prompt_for_wrong_barcode";
        public static string MissionBuzzerEnabled => "mission_buzzer_enabled";
        public static string MissionSelfLoopingMode => "mission_self_looping_mode";
        public static string PLCBarCodeSelfLooping => "plc_bar_code_self_looping";
        public static string PLCModel => "plc_model";
        public static string PLCDBAddress => "plc_address";
        public static string PLCDBRegisterNo => "plc_register_no";
        public static string PLCDBBitAddress => "plc_bit_address";
        public static string PLCBarCodeLength => "plc_bar_code_length";
        public static string AutoLockTool => "auto_lock_tool";
        public static string MatCodeApi => "mat_code_api";
        public static string UploadDataApi => "upload_data_api";
        public static string USBScannerEnabled => "usb_scanner";
        public static string Line_WHYC => "line_whyc";
        public static string Operator_WHYC => "operator_whyc";
        public static string AutoLaunchEnabled => "auto_launch";
        public static string AutoLoginEnabled => "auto_login";
        public static string AutoLoginInfo => "auto_login_info";
    }
}
