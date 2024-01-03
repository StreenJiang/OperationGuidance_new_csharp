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
<<<<<<< HEAD
            ToggleButton toggleButton = _titlePanel.AddRightButton<ToggleButton>();
            toggleButton.CheckedChanged += (sender, eventArgs) => {
                if (toggleButton.Checked) {
                    System.Console.WriteLine("我开喽~");
                } else {
                    System.Console.WriteLine("我关喽~");
                }
            };
=======
            _titlePanel.AddRightButton<ToggleButton>("");
>>>>>>> 6b187e5e6096d2f8441247118013644007768858
            // _titlePanel.AddRightButton<TitlePanel.RightButton>("111");
            _toggleButton = new() {
                Parent = this,
                       Margin = new(10),
<<<<<<< HEAD
                       ShowText = false,
=======
>>>>>>> 6b187e5e6096d2f8441247118013644007768858
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
