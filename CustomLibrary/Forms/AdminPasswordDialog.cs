using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;

namespace CustomLibrary.Forms {
    public class AdminPasswordDialog : WaitDialog {
        private readonly Func<string, bool> _passwordValidator;

        public bool IsPasswordCorrect { get; private set; }

        /// <param name="passwordValidator">密码验证回调，返回 true 表示密码正确</param>
        /// <param name="allowCancel">true=显示取消按钮，false=强制输入正确密码</param>
        public AdminPasswordDialog(string title, Func<string, bool> passwordValidator, bool allowCancel)
            : base("管理员密码") {

            _passwordValidator = passwordValidator;
            Title = title;

            var tb = TextBox.GetTextBox(0);
            tb.Box.PasswordChar = '*';
            tb.TextChanged += (s, e) => {
                tb.IsError = false;
            };
            tb.Box.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    OnConfirm(s, e);
                }
            };

            var confirmButton = AddButton("确定");
            confirmButton.Click += OnConfirm;

            if (allowCancel) {
                var cancelButton = AddButton("取消");
                cancelButton.Click += (s, e) => {
                    IsClosingAllowed = true;
                    Close();
                };
            }

            CloseButton.Visible = false;
        }

        private void OnConfirm(object? sender, EventArgs e) {
            var tb = TextBox.GetTextBox(0);
            string password = tb.Box.Text;
            if (!string.IsNullOrEmpty(password) && _passwordValidator(password)) {
                IsPasswordCorrect = true;
                SignalComplete();
            } else {
                WidgetUtils.ShowErrorPopUp("密码错误");
                tb.IsError = true;
            }
        }
    }
}
