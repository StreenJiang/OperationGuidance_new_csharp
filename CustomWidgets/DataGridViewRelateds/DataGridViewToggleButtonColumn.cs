using CustomLibrary.Buttons;

namespace CustomLibrary.DataGridViewRelateds {
    public class DataGridViewToggleButtonColumn: DataGridViewTextBoxColumn {
        #region Override properties
        public override DataGridViewCell CellTemplate {
            get => base.CellTemplate;
            set {
                // Ensure that the cell used for the template is a CalendarCell.
                if (value != null && !value.GetType().IsAssignableFrom(typeof(DataGridViewToggleButtonCell))) {
                    throw new InvalidCastException("Must be a CalendarCell");
                }
                base.CellTemplate = value;
            }
        }
        #endregion

        #region Constructors
        public DataGridViewToggleButtonColumn() {
            base.CellTemplate = new DataGridViewToggleButtonCell();
        }
        #endregion
    }

    public class DataGridViewToggleButtonCell: DataGridViewTextBoxCell {
        #region Fields
        private Panel _toggleButtonParentPanel;
        private ToggleButton _toggleButton;
        #endregion

        #region Properties
        public Panel ToggleButtonParentPanel { 
            get {
                if (_toggleButtonParentPanel.Parent == null && DataGridView != null) {
                    _toggleButtonParentPanel.Parent = DataGridView;
                }
                return _toggleButtonParentPanel; 
            }
            set => _toggleButtonParentPanel = value; 
        }
        public ToggleButton ToggleButton { get => _toggleButton; set => _toggleButton = value; }
        #endregion

        #region Constructors
        public DataGridViewToggleButtonCell() {
            _toggleButtonParentPanel = new() {
                Margin = new(0),
                Visible = false,
            };
            _toggleButton = new() {
                Parent = _toggleButtonParentPanel,
            };
            _toggleButton.CheckedChanged += (sender, eventArgs) => {
                Value = _toggleButton.Checked;
            };
            // Events
            _toggleButtonParentPanel.SizeChanged += (sender, eventArgs) => {
                _toggleButton.Size = _toggleButtonParentPanel.Size;
            };
        }
        #endregion

        #region Override methods 
        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, 
                DataGridViewElementStates cellState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle, 
                DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts) {
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);
            if (DataGridView != null) {
                if (Selected) {
                    _toggleButtonParentPanel.BackColor = DataGridView.DefaultCellStyle.SelectionBackColor;
                    // Hide any content
                    Style.SelectionForeColor = DataGridView.DefaultCellStyle.SelectionBackColor;
                } else {
                    _toggleButtonParentPanel.BackColor = DataGridView.DefaultCellStyle.BackColor;
                    // Hide any content
                    Style.ForeColor = DataGridView.DefaultCellStyle.BackColor;
                }
            }
        }
        #endregion
    }
}
