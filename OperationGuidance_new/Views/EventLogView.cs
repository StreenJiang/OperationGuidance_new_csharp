using CustomLibrary.Buttons;
using CustomLibrary.Panels;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.ReusableWidgets;

namespace OperationGuidance_new.Views {
    public class EventLogView: CustomContentPanel {
        #region Fields
        private MultiLineViewGroup _multiLineGroup;
        #endregion

        #region Contructors
        public EventLogView() {
            _multiLineGroup = new() {
                Parent = this,
                ReadOnly = true,
            };
            MainUtils.EventLogTextArea = _multiLineGroup.TextBox.Box;
            CommonButton netButton = _multiLineGroup.AddButton("网络");
            netButton.Click += (sender, eventArgs) => {
                _multiLineGroup.Text += "<网络>按钮点击...\r\n";
            };
            CommonButton clearButton = _multiLineGroup.AddButton("清除");
            clearButton.Click += (sender, eventArgs) => {
                _multiLineGroup.Text = string.Empty;
            };
        }
        #endregion

        #region Override methods
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            Size contentSize = new(Width - Padding.Size.Width, Height - Padding.Size.Height);
            _multiLineGroup.Size = contentSize;
        }
        #endregion
    }
}
