namespace OperationGuidance_new.Constants {
    // 工作台状态显示窗状态
    public enum WorkplaceProcessStatus {
        UNACTIVATED,            // 未激活
        OPERATION_ENABLE,       // 允许操作
        OPERATION_DISABLE_ARM,  // 禁止操作 - 力臂未在指定位置
        OPERATION_DISABLE_PSET, // 禁止操作 - 未配置程序号
        OPERATION_DISABLE_NG,   // 禁止操作 - 点位操作错误，错误信息需要指定
        FINISHED_OK,            // 完成 - OK
        FINISHED_NG,            // 完成 - NG
    }
}
