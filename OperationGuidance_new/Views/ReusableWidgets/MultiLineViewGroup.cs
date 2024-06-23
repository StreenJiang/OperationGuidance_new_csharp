using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using CustomLibrary.TextBoxes;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class MultiLineViewGroup : CustomContentPanel {
        #region Feilds 
        // Common fields
        private int _contentVerticalGap;
        private int _contentHerticalGap;
        // Buttons panel
        private Panel _buttonsPanel;
        private CustomContentPanel _buttonsRightInnerPanel;
        // Multi-line text box
        private CustomTextBox _textBox;
        #endregion

        #region Properties
        public CustomTextBox TextBox { get => _textBox; set => _textBox = value; }
        public bool ReadOnly { get => _textBox.ReadOnly; set => _textBox.ReadOnly = value; }
        public new string Text { get => _textBox.Text; set => _textBox.Text = value; }
        #endregion

        #region Constructors
        public MultiLineViewGroup() {
            // Self properties
            FlowDirection = FlowDirection.TopDown;

            // Initialization
            InitializeButtonsPanel();
            InitializeMultiLineTextBox();
        }
        #endregion

        #region Initialization methods
        private void InitializeButtonsPanel() {
            _buttonsPanel = new() {
                Parent = this,
                Margin = new(0),
                Padding = new(0),
            };
            _buttonsRightInnerPanel = new() {
                Parent = _buttonsPanel,
                Padding = new(0),
                Dock = DockStyle.Right,
            };
        }
        private void InitializeMultiLineTextBox() {
            _textBox = new() {
                Parent = this,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Multiline = true,
            };
            _textBox.Box.ScrollBars = ScrollBars.Vertical;
        }
        #endregion

        #region Reusable methods
        public CommonButton AddButton(string buttonName) => new() {
            Parent = _buttonsRightInnerPanel,
            Label = buttonName,
        };
        #endregion

        #region Override methods
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            Size contentSize = new(Width - Padding.Size.Width, Height - Padding.Size.Height);
            // Calculate gaps
            _contentVerticalGap = (int)(contentSize.Height * .015);
            _contentHerticalGap = (int)(contentSize.Width * .015);
            // Buttons panel
            int buttonHeight = WidgetUtils.CommonButtonHeight();
            _buttonsPanel.Size = new(contentSize.Width, buttonHeight);
            _buttonsPanel.Margin = new(0, 0, 0, _contentVerticalGap);
            // Resize buttons
            // Right panel width
            int rightPanelWidht = 0;
            int count = 0;
            foreach (Control control in _buttonsRightInnerPanel.Controls) {
                if (control.Visible && control is CommonButton btn) {
                    btn.Height = buttonHeight;
                    btn.Width = WidgetUtils.MeasureString(btn.Label, btn.Font).Width + buttonHeight * 2;
                    rightPanelWidht += btn.Width;
                    // Calculate margin
                    if (count != 0) {
                        control.Margin = new(_contentHerticalGap, 0, 0, 0);
                        rightPanelWidht += _contentHerticalGap;
                    }
                    count++;
                }
            }
            _buttonsRightInnerPanel.Size = new(rightPanelWidht, buttonHeight);
            // Multi-line text box panel
            int textBoxHeight = contentSize.Height - buttonHeight - _contentVerticalGap;
            _textBox.Size = new(contentSize.Width, textBoxHeight);
        }
        #endregion
    }
}
