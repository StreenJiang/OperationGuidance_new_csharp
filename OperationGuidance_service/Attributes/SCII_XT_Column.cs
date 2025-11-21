namespace OperationGuidance_service.Attributes {
    [AttributeUsage(AttributeTargets.Property)]
    public class SCII_XT_Column: Attribute {
        public string? Name { get; set; }
        public SCII_XT_ColumnType Type { get; set; }

        public SCII_XT_Column() { }
        public SCII_XT_Column(string? name = null, SCII_XT_ColumnType type = SCII_XT_ColumnType.NULL) {
            Name = name;
            Type = type;
        }
    }

    public enum SCII_XT_ColumnType {
        NULL,
        RESULT,
        FINAL_RESULT,
    }
}
