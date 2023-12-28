using CustomLibrary.ComboBoxs;
using CustomLibrary.Panels;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.TextBoxes;
using OperationGuidance_new.Configs;

namespace OperationGuidance_new.Views {
    public class StationSettingsView: CustomContentPanel {
        private CustomTextBoxGroup _textBoxGroup;
        private CustomTextBoxGroup _textBoxGroup1;
        private CustomTextBoxGroup _textBoxGroup2;
        private CustomTextBoxGroup _textBoxGroup3;
        private CustomTextBoxGroup _textBoxGroup4;
        private CustomTextBoxGroup _textBoxGroup5;

        public StationSettingsView() {
            _textBoxGroup = new("数据") {
                Parent = this,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 7,
                NameAlignment = HorizontalAlignment.Right,
                Separator = "~",
            };

            _textBoxGroup1 = new("数据") {
                Parent = this,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 7,
                NameAlignment = HorizontalAlignment.Right,
                Separator = "~",
            };
            _textBoxGroup1.AddTextBox().NumberValidate = true;

            _textBoxGroup2 = new("数据") {
                Parent = this,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 7,
                NameAlignment = HorizontalAlignment.Right,
                Separator = "~",
            };
            _textBoxGroup2.AddTextBox().NumberValidate = true;
            _textBoxGroup2.AddTextBox().NumberValidate = true;

            _textBoxGroup3 = new("数据") {
                Parent = this,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 7,
                NameAlignment = HorizontalAlignment.Right,
                Separator = "~",
            };
            _textBoxGroup3.AddTextBox().NumberValidate = true;
            _textBoxGroup3.AddTextBox().NumberValidate = true;
            _textBoxGroup3.AddTextBox().NumberValidate = true;

            _textBoxGroup4 = new("数据") {
                Parent = this,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 7,
                NameAlignment = HorizontalAlignment.Right,
                Separator = "~",
            };
            _textBoxGroup4.AddTextBox().NumberValidate = true;
            _textBoxGroup4.AddTextBox().NumberValidate = true;
            _textBoxGroup4.AddTextBox().NumberValidate = true;
            _textBoxGroup4.AddTextBox().NumberValidate = true;

            _textBoxGroup5 = new("数据") {
                Parent = this,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
                Ratio = 7,
                NameAlignment = HorizontalAlignment.Right,
                Separator = "~",
            };
            _textBoxGroup5.AddTextBox().NumberValidate = true;
            _textBoxGroup5.AddTextBox().NumberValidate = true;
            _textBoxGroup5.AddTextBox().NumberValidate = true;
            _textBoxGroup5.AddTextBox().NumberValidate = true;
            _textBoxGroup5.AddTextBox().NumberValidate = true;
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            _textBoxGroup.Size = new((int) (Width / 1.5), Height / 16);
            _textBoxGroup1.Size = new((int) (Width / 1.5), Height / 16);
            _textBoxGroup2.Size = new((int) (Width / 1.5), Height / 16);
            _textBoxGroup3.Size = new((int) (Width / 1.5), Height / 16);
            _textBoxGroup4.Size = new((int) (Width / 1.5), Height / 16);
            _textBoxGroup5.Size = new((int) (Width / 1.5), Height / 16);
        }

        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            return false;
        }
    }
}
