using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.ComboBoxes;
using CustomLibrary.Utils;
using CustomLibrary.TextBoxes;
using static TitlePanel;
using CustomLibrary.Buttons;
using System.Reflection;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class SubPanel<T>: CustomContentPanel {
        private T _dto;
        private TitlePanel _titlePanel;
        private TableLayoutPanel _tablePanel;
        private bool _enabled = true;

        public TitlePanel TitlePanel { get => _titlePanel; set => _titlePanel = value; }
        public TableLayoutPanel TablePanel { get => _tablePanel; set => _tablePanel = value; }

        public new bool Enabled {
            get => _enabled;
            set {
                _enabled = value;
                foreach (Control ctrl in TitlePanel.RightButtons) {
                    if (ctrl is RightButton rbtn) {
                        rbtn.Enabled = value;
                    } else if (ctrl is ToggleButton tbtn) {
                        tbtn.Enabled = value;
                    } else {
                        ctrl.Enabled = value;
                    }
                }
                foreach (Control ctrl in TablePanel.Controls) {
                    if (ctrl is CustomTextBoxButtonGroup ctbbg) {
                        ctbbg.Enabled = value;
                    } else if (ctrl is CustomTextBoxGroup ctbg) {
                        ctbg.Enabled = value;
                    } else if (ctrl.GetType().Name == typeof(CustomComboBoxGroup<>).Name) {
                        Type type = ctrl.GetType();
                        PropertyInfo? propertyInfo = type.GetProperty("Enabled");
                        if (propertyInfo != null) {
                            propertyInfo.SetValue(ctrl, value);
                        }
                    } else {
                        ctrl.Enabled = value;
                    }
                }
            }
        }

        public SubPanel(T dto, string title, int columnCount) {
            _dto = dto;
            _titlePanel = new(title) {
                Parent = this,
                UnderlineColor = ColorConfigs.COLOR_TITLE_UNDERLINE,
            };
            _tablePanel = new() {
                Parent = this,
                Margin = new(0),
                ColumnCount = columnCount,
            };
        }

        public CustomTextBoxGroup AddTextBox<V>(string boxName, bool numberOnly, Action<T, V?>? propertySetter) {
            CustomTextBoxGroup boxGroup = WidgetUtils.AddTextBox(_tablePanel, _dto, boxName, numberOnly, propertySetter);
            boxGroup.NameAlignment = HorizontalAlignment.Right;
            boxGroup.Ratio = 7;
            return boxGroup;
        }
        public CustomComboBoxGroup<V> AddComboBox<V>(string boxName, Action<T, V?>? propertySetter, Dictionary<string, V> items) {
            CustomComboBoxGroup<V> boxGroup = WidgetUtils.AddComboBox(_tablePanel, _dto, boxName, propertySetter, items);
            boxGroup.NameAlignment = HorizontalAlignment.Right;
            boxGroup.Ratio = 7;
            return boxGroup;
        }
        public void ResizeSelf(int width) {
            int boxHeight = WidgetUtils.PopUpOrFloatingFormTextOrComboBoxHeight();
            int boxMargin = boxHeight / 5;
            int titleHeight = WidgetUtils.PopUpOrFloatingFormSubTitle();
            int titleMargin = titleHeight / 10;
            int tableHeight = 0;
            int previousRowIndex = -1;
            foreach (Control control in _tablePanel.Controls) {
                if (control.Visible) {
                    int currentRowIndex = _tablePanel.GetPositionFromControl(control).Row;
                    if (currentRowIndex != previousRowIndex) {
                        previousRowIndex = currentRowIndex;
                        tableHeight += boxHeight + boxMargin * 2;
                    }
                }
            }
            _titlePanel.Margin = new(0, boxMargin, 0, boxMargin);
            _titlePanel.Size = new(width, titleHeight);
            _tablePanel.Size = new(width, tableHeight);
            int contentPieceWidth = (_tablePanel.Width - boxMargin * (_tablePanel.ColumnCount + 1)) / _tablePanel.ColumnCount;
            foreach (Control control in _tablePanel.Controls) {
                control.Margin = new(boxMargin, boxMargin, 0, boxMargin);

                int columnSpan = _tablePanel.GetColumnSpan(control);
                if (columnSpan > 1) {
                    control.Size = new(contentPieceWidth * columnSpan + boxMargin * (columnSpan - 1), boxHeight);
                } else {
                    control.Size = new(contentPieceWidth, boxHeight);
                }
            }
            Size = new(width, tableHeight + titleHeight + titleMargin * 2);
            Invalidate();
        }
    }
}
