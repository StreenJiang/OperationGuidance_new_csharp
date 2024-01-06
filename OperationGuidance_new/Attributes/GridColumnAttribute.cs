namespace OperationGuidance_new.Attributes {
    [AttributeUsage(AttributeTargets.Property)]
    public class GridColumnAttribute: Attribute {
        #region Properties
        public string? ColumnName { get; set; }
        public Type? CellType { get; set; }
        #endregion

        #region Constructors
        public GridColumnAttribute() {
        }
        public GridColumnAttribute(string columnName) {
            ColumnName = columnName;
        }
        public GridColumnAttribute(string columnName, Type cellType) {
            ColumnName = columnName;
            CellType = cellType;
        }
        #endregion
    }
}
