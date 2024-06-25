using CustomLibrary.Buttons;
using CustomLibrary.Buttons.AbstractClasses;
using CustomLibrary.Utils;
using System.ComponentModel;

namespace CustomLibrary.TextBoxes {
    [DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class CustomTextBoxButtonGroup: CustomTextBoxGroup {
        private int _gapButtons;
        private List<AbstractCustomButton> _buttons;

        public new bool Enabled {
            get => base.Enabled;
            set {
                base.Enabled = value;
                SetButtonsProperties((button) => button.Enabled = value);
            }
        }
        public List<AbstractCustomButton> Buttons { get => _buttons; }
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

        public T AddButton<T>(string? label = null) where T : AbstractCustomButton, new() {
            T button = new() {
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

        public T GetButton<T>(int index) where T : AbstractCustomButton {
            if (_buttons[index] is T) {
                return (T) _buttons[index];
            }
            throw new InvalidCastException($"Button type of _buttons[{index}] is not <{typeof(T).Name}>, please make sure the index is correct.");
        }

        protected override int GetBoxesRange() {
            int boxesRangeTemp = base.GetBoxesRange();
            // New range for just boxes
            int buttonHeight = Height;
            int buttonWidthSum = 0;
            using (Graphics g = CreateGraphics()) {
                // Resize button label
                foreach (AbstractCustomButton button in _buttons) {
                    int buttonWidth;
                    if (button is SignButton) {
                        buttonWidth = buttonHeight;
                        button.Size = new(buttonWidth, buttonHeight);
                    } else {
                        // Change height first then Font will change to a new size
                        button.Height = buttonHeight;
                        buttonWidth = WidgetUtils.MeasureString(button.Label, button.Font).Width + buttonHeight * 2;
                        button.Width = buttonWidth;
                    }
                    button.Margin = new(GapNameAndBox, 0, 0, 0);
                    buttonWidthSum += buttonWidth;
                }
            }
            return boxesRangeTemp - buttonWidthSum - GapNameAndBox * _buttons.Count;
        }

        protected override int GetExtraWidth() {
            int extraWidth = 0;
            foreach (AbstractCustomButton button in _buttons) {
                extraWidth += button.Width;
            }
            extraWidth += GapNameAndBox * _buttons.Count;
            return extraWidth;
        }
    }
}
