namespace OperationGuidance_service.Attributes {
    [AttributeUsage(AttributeTargets.Property)]
    public class Description: Attribute {
        public string? Name { get; set; }

        public Description() { }
        public Description(string? name) {
            Name = name;
        }
    }
}
