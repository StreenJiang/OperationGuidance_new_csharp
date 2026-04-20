namespace OperationGuidance_new.Constants.FIT {
    /// <summary>
    /// 拧紧曲线数据包
    /// </summary>
    public class TighteningCurvePacket {
        public int TighteningId { get; set; }      // 拧紧ID (4字节)
        public ushort TotalPackets { get; set; }    // 总包数 (2字节)
        public ushort CurrentPacket { get; set; }   // 当前包号 (2字节)
        public List<CurvePoint> Points { get; set; } = new List<CurvePoint>(); // 曲线点

        public const int PointsPerPacket = 20; // 每包20个点
    }
}
