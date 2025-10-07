namespace OperationGuidance_new.Utils {
    public class MESMessageData_TZYX {
        public int Index { get; set; }
        public int TaskNo { get; set; }
        public double Torsion { get; set; }
        public double Stroke { get; set; }
        public bool Result { get; set; }
        public string? ResultMsg { get; set; } = null;
        public int TightTime { get; set; }
        public int Unit { get; set; }
        public double NeedTorsion { get; set; }
    }
}
