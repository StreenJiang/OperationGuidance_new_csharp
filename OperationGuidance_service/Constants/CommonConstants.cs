namespace OperationGuidance_service.Constants {
    internal class CommonConstants {
    }

    public enum YesOrNo {
        YES = 1, NO = 2
    }

    // 螺栓点位状态
    public enum BoltStatus {
        DEFAULT,                // 默认
        TIGHTENING,             // 拧紧中
        TIGHTENING_COMPLETE,    // 拧紧完成
        TIGHTENING_ERROR,       // 拧紧错误
        LOOSENING,              // 反松中
        LOOSENING_COMPLETE,     // 反松完成
        LOOSENING_ERROR,        // 反松错误
    }

    // 产品任务状态
    public enum ProductMissionStatus {
        DEFAULT,                // 默认
        READY,                  // 就绪
        WORKING,                // 工作中
        ERROR,                  // 错误
        FINISHED,               // 完成
    }

    // 工具操作指令类型
    public enum OperationEnum {
        DEFAULT,            // 默认
        KEEP_ALIVE,         // 心跳

        UNKNOWN_YET,        // 目前未知
        UNKNOWN_YET1,       // 目前未知
        UNKNOWN_YET2,       // 目前未知
        
        SWITCH_PSET,        // 下发PSET（切换程序）
        LOCK_DEVICE,        // 锁枪（禁用工具）
        UNLOCK_DEVICE,      // 解锁（使能工具）
        UPLOAD_IDENTIFY,    // 上传条码报文
        READ_DATA,          // 读数据（读取拧紧结果[无实时拧紧数据]
        RESPOND_DATA,       // 应答上载数据报文
        WRITE_CODE,         // 写入条码（或二维码）（阿特拉斯里是：下载条码报文）
        UPLOAD_INPUT,       // <input状态>上传
        RESPOND_INPUT,      // <input状态>应答报文

        RESPOND_INPUT1,     // Relayfunctionsubscribe
        RESPOND_INPUT2,     // Relayfunctionacknowledge
        RESPOND_INPUT3,     // 设置<input>报文
        RESPOND_INPUT4,     // 复位<input>报文
        RESPOND_INPUT5,     // 扭矩曲线/=PF6000
        RESPOND_INPUT6,     // 角度曲线/=PF6000
        RESPOND_INPUT7,     // 扭矩+角度曲线/=PF6000
        RESPOND_INPUT8,     // 曲线应答/=PF6000
    }
}
