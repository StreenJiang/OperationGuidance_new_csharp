namespace OperationGuidance_service.Attributes {
    [AttributeUsage(AttributeTargets.Property)]
    public class SCII_XT_Column: Attribute {
        public string? Name { get; set; }
        public string? Unit { get; set; }

        public SCII_XT_Column() { }
        public SCII_XT_Column(string? name = null, string? unit = null) {
            Name = name;
            Unit = unit;
        }
    }
}
