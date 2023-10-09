namespace OperationGuidance_service.Attributes {
    [AttributeUsage(AttributeTargets.Property)]
    public class ConvertPropertyAttribute: Attribute {
        public string? SourceName { get; set; }

        public ConvertPropertyAttribute() {
        }

        public ConvertPropertyAttribute(string sourceName) {
            SourceName = sourceName;
        }
    }
}
