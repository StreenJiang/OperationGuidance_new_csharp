using CustomLibrary.Buttons;
using CustomLibrary.ComboBoxs;
using CustomLibrary.Panels;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;

namespace OperationGuidance_new.Views {
    public class StationSettingsView: CustomContentPanel {
        private TitlePanel _titlePanel;
        private ToggleButton _toggleButton;
        private ToggleButton _toggleButton2;

        public StationSettingsView() {
            _titlePanel = new("测试标题") {
                Parent = this,
            };
            _titlePanel.UnderlineColor = Color.Gray;
            _titlePanel.AddRightButton<ToggleButton>("");
            // _titlePanel.AddRightButton<TitlePanel.RightButton>("111");
            _toggleButton = new() {
                Parent = this,
                       Margin = new(10),
            };
            _toggleButton2 = new() {
                Parent = this,
                Size = new(300, 30),
                       Margin = new(10),
            };
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            _titlePanel.Size = new(Width, (int) (Height / 15.5));
        }

        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            return false;
        }
    }
}
