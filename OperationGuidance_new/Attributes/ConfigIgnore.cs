namespace OperationGuidance_new.Attributes {
    [AttributeUsage(AttributeTargets.Property)]
    public class ConfigIgnore: Attribute {
        #region Properties
        public bool IsIgnored { get; set; }
        #endregion

        #region Constructors
        public ConfigIgnore(bool isIgnored = true) {
            IsIgnored = isIgnored;
        }
        #endregion
    }
}
