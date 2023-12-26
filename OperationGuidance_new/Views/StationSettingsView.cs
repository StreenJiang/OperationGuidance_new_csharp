using CustomLibrary.ComboBoxs;
using CustomLibrary.Panels;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.TextBoxes;
using OperationGuidance_new.Configs;

namespace OperationGuidance_new.Views {
    public class StationSettingsView: CustomContentPanel {
        private CustomTextBox _textBox;
        private CustomComboBox<string> _comboBox;
        private CustomComboBoxGroup<int> _comboBoxGroup;

        public StationSettingsView() {
            _textBox = new() {
                Parent = this,
                BackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
            };
            _textBox.NumberValidate = true;

            _comboBox = new() {
                Parent = this,
                BackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
                ShowRealValue = true,
            };
            _comboBox.AddItem("item1", "这是item1");
            _comboBox.AddItem("item2", "这是item2");
            _comboBox.AddItem("item3item3item3item3", "这是item3");
            _comboBox.AddItem("item4", "这是item4");
            _comboBox.AddItem("item5", "这是item5");
            _comboBox.AddItem("item6", "这是item6");
            _comboBox.AddItem("item7", "这是item7");
            // _comboBox.SetDefault(3);

            _comboBox.ItemSelected += () => {
                if (_comboBox.GetChosenItem() == null) {
                    _comboBox.IsError = true;
                } else {
                    _comboBox.IsError = false;
                }
            };
            _textBox.Box.TextChanged += (s, e) => {
                // _comboBox.RemoveItem(3);
            };

            _comboBoxGroup = new("工具ID") {
                Parent = this,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
                ShowRealValue = true,
            };
            _comboBoxGroup.AddItem("1（一号工具）", 1);
            _comboBoxGroup.AddItem("2（二号工具）", 2);
            _comboBoxGroup.AddItem("3（三号工具）", 3);
        }

        public override void InvokeResizing() {
            _textBox.Size = new(Width / 4, Height / 16);
            _comboBox.Size = new(Width / 4, Height / 16);
            _comboBoxGroup.Size = new(Width / 2, Height / 16);
        }

        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            return false;
        }
    }
}
