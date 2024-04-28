using CustomLibrary.Buttons;
using CustomLibrary.Utils;
using System.ComponentModel;

namespace CustomLibrary.TextBoxes {
    [DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class CustomTextBoxButtonGroup: CustomTextBoxGroup {
        private int _gapButtons;
        private List<CommonButton> _buttons;

        public new bool Enabled {
            get => base.Enabled;
            set {
                base.Enabled = value;
                SetButtonsProperties((button) => button.Enabled = value);
            }
        }
        public List<CommonButton> Buttons { get => _buttons; }
        public new Color BackColor { get; private set; }

        private void SetTextBoxesProperties(Action<CustomTextBox> setProperty) {
            foreach (CustomTextBox textBox in TextBoxes) {
                setProperty(textBox);
            }
        }
        private void SetSeparatorsProperties(Action<SeparatorControl> setProperty) {
            foreach (SeparatorControl separator in Separators) {
                setProperty(separator);
            }
        }
        private void SetButtonsProperties(Action<CommonButton> setProperty) {
            foreach (CommonButton button in _buttons) {
                setProperty(button);
            }
        }

        public CustomTextBoxButtonGroup(string textName) : base(textName) {
            _buttons = new();
        }

        public CommonButton AddButton(string label) {
            CommonButton button = new() {
                Parent = TextBoxesPanel,
                Enabled = Enabled,
                Label = label,
            };
            _buttons.Add(button);
            if (IsHandleCreated) {
                ResizeChildren();
            }
            return button;
        }

        public CommonButton GetButton(int index) {
            return _buttons[index];
        }

        protected override int GetBoxesRange() {
            int boxesRangeTemp = base.GetBoxesRange();
            // New range for just boxes
            int buttonHeight = Height;
            int buttonWidthSum = 0;
            using (Graphics g = CreateGraphics()) {
                // Resize button label
                foreach (CommonButton button in _buttons) {
                    // Change height first then Font will change to a new size
                    button.Height = buttonHeight;
                    int buttonWidth = WidgetUtils.MeasureString(button.Label, button.Font).Width + buttonHeight * 2;
                    button.Width = buttonWidth;
                    button.Margin = new(GapNameAndBox, 0, 0, 0);
                    buttonWidthSum += buttonWidth;
                }
            }
            return boxesRangeTemp - buttonWidthSum - GapNameAndBox * _buttons.Count;
        }

        protected override int GetExtraWidth() {
            int extraWidth = 0;
            foreach (CommonButton button in _buttons) {
                extraWidth += button.Width;
            }
            extraWidth += GapNameAndBox * _buttons.Count;
            return extraWidth;
        }
    }
}
