using CustomLibrary.Buttons;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_new.Views.ReusableWidgets;

namespace OperationGuidance_new.Views {
    public class SoftwareLicenseView: CustomContentPanel {
        #region Fields
        private MultiLineViewGroup _multiLineGroup;
        private string _defaultText;
        #endregion

        #region Contructors
        public SoftwareLicenseView() {
            _defaultText = "请在此输入许可证";
            _multiLineGroup = new() {
                Parent = this,
                Text = _defaultText,
            };
            _multiLineGroup.TextBox.Box.GotFocus += (sender, eventArgs) => {
                if (_multiLineGroup.Text == _defaultText) {
                    _multiLineGroup.Text = string.Empty;
                }
            };
            _multiLineGroup.TextBox.Box.LostFocus += (sender, eventArgs) => {
                if (_multiLineGroup.Text == "" || _multiLineGroup.Text == string.Empty) {
                    _multiLineGroup.Text = _defaultText;
                }
            };
            CommonButton netButton = _multiLineGroup.AddButton("读取");
            netButton.Click += (sender, eventArgs) => {
                _multiLineGroup.Text = "ALKDJFKLJKLJ2314123KL5J08909-=123=0=0=1234123JNKL5JLKJADZFASD=FA=S-D0R-=10234=123=12359KL12HKLJASDLFKJASDLK;FJKL15=123123561=234";
            };
            CommonButton clearButton = _multiLineGroup.AddButton("保存");
            clearButton.Click += (sender, eventArgs) => {
                WidgetUtils.ShowNoticePopUp("许可证认证成功（假装）！");
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
