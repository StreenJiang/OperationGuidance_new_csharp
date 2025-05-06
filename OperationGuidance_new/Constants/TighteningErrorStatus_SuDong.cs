namespace OperationGuidance_new.Constants {
    public enum TighteningErrorStatus_SuDong {
        NO_ERROR = 0,                   // 没有错误
        SLIPPAGE = 1,                   // 滑丝/滑牙
        FALSE_LOCKING = 2,              // 浮锁
        TORQUE_NOK = 3,                 // 扭矩不良
        ANGLE_NOK = 4,                  // 拧紧角度不良
        SEND_UNLOCK_IN_TIGTHENING = 5,  // 中途提前释放启动信号
    }
}
