using CustomLibrary.Buttons;
using CustomLibrary.Utils;
using System.ComponentModel;

namespace CustomLibrary.ComboBoxes {
    [DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class CustomComboBoxButtonGroup<T>: CustomComboBoxGroup<T> {
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

        private void SetButtonsProperties(Action<CommonButton> setProperty) {
            foreach (CommonButton button in _buttons) {
                setProperty(button);
            }
        }

        public CustomComboBoxButtonGroup(string textName) : base(textName) {
            _buttons = new();
        }

        public CommonButton AddButton(string label) {
            CommonButton button = new() {
                Parent = ElementsPanel,
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

        protected override int GetBoxWidth() {
            int boxesRangeTemp = base.GetBoxWidth();
            // New range for just boxes
            int buttonHeight = WidgetUtils.CommonButtonHeight();
            int buttonWidthSum = 0;
            using (Graphics g = CreateGraphics()) {
                // Resize button label
                foreach (CommonButton button in _buttons) {
                    int labelWidth = (int) (g.MeasureString(button.Label, button.Font).Width + buttonHeight * 1.2);
                    button.Size = new(labelWidth, buttonHeight);
                    button.Margin = new(GapNameAndBox, 0, 0, 0);
                    buttonWidthSum += labelWidth;
                }
            }
            return boxesRangeTemp - buttonWidthSum - GapNameAndBox * _buttons.Count;
        }
    }
}
