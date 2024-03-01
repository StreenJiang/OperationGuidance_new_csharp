namespace OperationGuidance_new.Configs {
    public class OperationDataField {
        public int Id { get; set; }
        public string FieldName { get; set; }
        public string PropertyName { get; set; }
        public bool Visible { get; set; }

        public OperationDataField(int id, string fieldName, string propertyName, bool visible) {
            Id = id;
            FieldName = fieldName;
            PropertyName = propertyName;
            Visible = visible;
        }
    }
}
