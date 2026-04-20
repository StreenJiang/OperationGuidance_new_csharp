namespace OperationGuidance_new.Constants.FIT {
    /// <summary>
    /// 完整的拧紧曲线数据（重组后）
    /// </summary>
    public class CompleteTighteningCurve {
        public int TighteningId { get; set; }
        public List<CurvePoint> AllPoints { get; set; } = new List<CurvePoint>();
        public DateTime ReceiveStartTime { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }
}
