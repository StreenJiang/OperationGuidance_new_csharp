using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class EditEntityPopUpForm<T>: CustomPopUpForm {
        #region Fields
        private T _dto;
        private TableLayoutPanel _tablePanel;
        private readonly int _columnCount = 2;
        #endregion

        #region Properties
        public TableLayoutPanel TablePanel { get => _tablePanel; }
        #endregion

        #region Constructors
        public EditEntityPopUpForm(T dto) {
            _tablePanel = new() {
                Parent = ContentPanel,
                Margin = new(0),
                ColumnCount = _columnCount,
            };
            _dto = dto;
        }
        #endregion

        #region Reusable methods
        public SubPanel<T> AddSubPanel(string title) {
            SubPanel<T> subPanel = new(_dto, title, _columnCount);
            _tablePanel.Controls.Add(subPanel);
            _tablePanel.SetColumnSpan(subPanel, 2);
            return subPanel;
        }
        public CustomTextBoxGroup AddTextBox<V>(string boxName, bool numberOnly, Action<T, V?> propertySetter) {
            CustomTextBoxGroup customTextBoxGroup = WidgetUtils.AddTextBox(_tablePanel, _dto, boxName, numberOnly, propertySetter);
            customTextBoxGroup.NameAlignment = HorizontalAlignment.Right;
            customTextBoxGroup.Ratio = 7;
            return customTextBoxGroup;
        }
        public CustomComboBoxGroup<V> AddComboBox<V>(string boxName, Action<T, V?> propertySetter, Dictionary<string, V> items) {
            CustomComboBoxGroup<V> customComboBoxGroup = WidgetUtils.AddComboBox(_tablePanel, _dto, boxName, propertySetter, items);
            customComboBoxGroup.NameAlignment = HorizontalAlignment.Right;
            customComboBoxGroup.Ratio = 7;
            return customComboBoxGroup;
        }
        public ToggleButtonGroup AddToggleButton(string toggleButtonName, Action<T, bool> propertySetter) {
            ToggleButtonGroup toggleButtonGroup = WidgetUtils.AddToggleButton(_tablePanel, _dto, toggleButtonName, propertySetter);
            toggleButtonGroup.NameAlignment = HorizontalAlignment.Right;
            toggleButtonGroup.Ratio = 7;
            return toggleButtonGroup;
        }
        public PictureBoxGroup AddPictureBox(string boxName, Action<T, Image> imageSetter, Action<T, string> fileNameSetter) {
            PictureBoxGroup pictureBoxGroup = WidgetUtils.AddPictureBox(_tablePanel, _dto, boxName, imageSetter, fileNameSetter);
            pictureBoxGroup.NameAlignment = HorizontalAlignment.Right;
            pictureBoxGroup.Ratio = 7;
            _tablePanel.SetColumnSpan(pictureBoxGroup, 2);
            return pictureBoxGroup;
        }
        public void ResizeTablePanelAndItsChildren() {
            CalculateDetailProperties();

            Control mainForm = WidgetUtils.MainPanel.Parent;
            Padding contentPadding = ContentPanel.Padding;
            int boxHeight = WidgetUtils.TextOrComboBoxHeight();
            int boxMargin = boxHeight / 5;
            int subTitleHeight = WidgetUtils.PopUpFormSubTitle();
            int subTitleMargin = subTitleHeight / 5;
            int tableHeight = 0;
            int previousRowIndex = -1;
            int cntentWidth = (int) (mainForm.Width * .75);
            int tableWidth = cntentWidth - contentPadding.Size.Width;
            int contentPieceWidth = tableWidth / _tablePanel.ColumnCount - boxMargin * 2;
            foreach (Control control in _tablePanel.Controls) {
                if (control.Visible) {
                    int currentRowIndex = _tablePanel.GetPositionFromControl(control).Row;
                    if (currentRowIndex != previousRowIndex) {
                        previousRowIndex = currentRowIndex;
                        if (control is TitlePanel titlePanel) {
                            tableHeight += subTitleHeight + subTitleMargin * 2;
                        } else if (control is SubPanel<T> subPanel) {
                            subPanel.ResizeSelf(tableWidth);
                            tableHeight += subPanel.Height;
                        } else if (control is PictureBoxGroup pictureBox) {
                            pictureBox.SetSize(contentPieceWidth, boxHeight, WidgetUtils.PictureBoxGroupBaseHeight(), 1, contentPieceWidth + boxMargin * 2);
                            pictureBox.Margin = new(boxMargin);
                            tableHeight += pictureBox.Height + subTitleMargin * 2;
                        } else {
                            tableHeight += boxHeight + boxMargin * 2;
                        }
                    }
                }
            }
            Size contentSize = new(cntentWidth, tableHeight + contentPadding.Size.Height);
            _tablePanel.Size = new(tableWidth, tableHeight);
            foreach (Control control in _tablePanel.Controls) {
                if (control is TitlePanel titlePanel) {
                    titlePanel.Margin = new(0, boxMargin, 0, boxMargin);
                    titlePanel.Size = new(_tablePanel.Width, subTitleHeight);
                } else if (control is SubPanel<T> subPanel) {
                    continue;
                } else if (control is PictureBoxGroup pictureBox) {
                    continue;
                } else {
                    control.Margin = new(boxMargin);
                    control.Size = new(contentPieceWidth, boxHeight);
                }
            }

            SetContentSizeAndSelfSize(contentSize);
        }
        #endregion

        #region Override methods
        #endregion
    }

    public class SubPanel<T>: CustomContentPanel {
        private T _dto;
        private TitlePanel _titlePanel;
        private TableLayoutPanel _tablePanel;

        public TitlePanel TitlePanel { get => _titlePanel; set => _titlePanel = value; }
        public TableLayoutPanel TablePanel { get => _tablePanel; set => _tablePanel = value; }

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

        public CustomTextBoxGroup AddTextBox<V>(string boxName, bool numberOnly, Action<T, V?> propertySetter) {
            CustomTextBoxGroup boxGroup = WidgetUtils.AddTextBox(_tablePanel, _dto, boxName, numberOnly, propertySetter);
            boxGroup.NameAlignment = HorizontalAlignment.Right;
            boxGroup.Ratio = 7;
            boxGroup.GetTextBox(0).Box.TextChanged += (sender, eventArgs) => WidgetUtils.HandleTextChanged(_dto, boxGroup, 0, propertySetter);
            return boxGroup;
        }
        public CustomComboBoxGroup<V> AddComboBox<V>(string boxName, Action<T, V?> propertySetter, Dictionary<string, V> items) {
            CustomComboBoxGroup<V> boxGroup = WidgetUtils.AddComboBox(_tablePanel, _dto, boxName, propertySetter, items);
            boxGroup.NameAlignment = HorizontalAlignment.Right;
            boxGroup.Ratio = 7;
            return boxGroup;
        }
        public void ResizeSelf(int width) {
            int boxHeight = WidgetUtils.TextOrComboBoxHeight();
            int boxMargin = boxHeight / 5;
            int titleHeight = WidgetUtils.PopUpFormSubTitle();
            int titleMargin = titleHeight / 5;
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
            foreach (Control control in _tablePanel.Controls) {
                control.Margin = new(boxMargin);
                control.Size = new(_tablePanel.Width / _tablePanel.ColumnCount - boxMargin * 2, boxHeight);
            }
            Size = new(width, tableHeight + titleHeight + titleMargin * 2);
            Invalidate();
        }
    }
}
