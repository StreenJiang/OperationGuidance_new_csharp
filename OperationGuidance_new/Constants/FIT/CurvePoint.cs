namespace OperationGuidance_new.Constants.FIT {
    /// <summary>
    /// 曲线点数据
    /// </summary>
    public class CurvePoint {
        public uint Time { get; set; }      // 时间 (4字节)
        public float Torque { get; set; }   // 扭矩 (4字节)
        public float Angle { get; set; }    // 角度 (4字节)
    }
}
