namespace OperationGuidance_service.Attributes {
    [AttributeUsage(AttributeTargets.Property)]
    public class GridColumnAttribute: Attribute {
        public string? ColumnName { get; set; }

        public GridColumnAttribute() {
        }

        public GridColumnAttribute(string sourceName) {
            ColumnName = sourceName;
        }
    }
}
