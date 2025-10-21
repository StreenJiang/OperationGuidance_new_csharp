using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using CustomLibrary.TextBoxes;
using CustomLibrary.Forms;
using OperationGuidance_service.Utils;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_new.Utils;
using System.Diagnostics;
using log4net;

namespace OperationGuidance_new.Views {
    public class LoginView: CustomContentPanel {
        #region Fields
        protected ILog log;

        private Image _back;
        private Image _backShowing;
        private LoginPopUpForm? _loginForm;
        private Action<Size> _afterLogin;
        private Size _mainFormSize;
        private bool _isLoggedIn = false;
        #endregion

        public Image Back { get => _back; set => _back = value; }
        public Image BackShowing { get => _backShowing; set => _backShowing = value; }
        public Size MainFormSize { get => _mainFormSize; set => _mainFormSize = value; }
        public Action<Size> AfterLogin { get => _afterLogin; set => _afterLogin = value; }

        #region Constructors
        public LoginView(Size size, Image back, Action<Size> afterLogin, Size mainFormSize) {
            log = LogManager.GetLogger(this.GetType());

            Size = size;
            _back = back;
            _backShowing = WidgetUtils.ResizeImage(_back, size);
            _afterLogin = afterLogin;
            _mainFormSize = mainFormSize;
            ShowLoginForm(true);
        }
        #endregion

        #region Override methods
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            e.Graphics.DrawImage(_backShowing, new Point((Width - _backShowing.Width) / 2, (Height - _backShowing.Height) / 2));
        }
        #endregion

        #region Initialization methods
        public async void ShowLoginForm(bool firstLogin = false) {
            WidgetUtils.RefreshMainSize(MainUtils.GetSettingResolution());
            _isLoggedIn = false;
            await Task.Delay(300);
            _loginForm = new(ClickLogin, firstLogin);
            WidgetUtils.MakeControlDraggable(_loginForm.ContentPanel, WidgetUtils.MainForm);
            _loginForm.TitlePanel.Hide();
            _loginForm.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    ClickLogin();
                }
            };
            _loginForm.FormClosing += (s, e) => {
                if (!_loginForm.IsDisposed) {
                    if (WidgetUtils.ShowConfirmPopUp("确认退出？")) {
                        Process.GetCurrentProcess().Kill();
                    } else {
                        e.Cancel = true;
                    }
                }
            };
            _loginForm.AddButton("登录").Click += (s, e) => ClickLogin();
            _loginForm.AddButton("退出").Click += (s, e) => _loginForm.Close();

            _loginForm.PretendToShowToCreateHandlesForChildren();
            _loginForm.ResizeSelf();
            _loginForm.Show();

            void ClickLogin() {
                string account = _loginForm.AccountBox.GetTextBox(0).Box.Text;
                string password = _loginForm.PasswordBox.GetTextBox(0).Box.Text;
                CheckLoginByApi(account, password);
            }
        }
        protected virtual void CheckLoginByApi(string account, string password) {
            LoginValidateRsp rsp = SystemUtils.GetApis().LoginValidate(new(account, password));
            if (!rsp.Succeed) {
                WidgetUtils.ShowErrorPopUp(rsp.FailedReason);
            } else {
                SystemUtils.UserInfo = CommonUtils.CannotBeNull(rsp.UserAccountInfoDTO);
                ActionAfterLogin();
            }
        }
        protected void ActionAfterLogin() {
            _isLoggedIn = true;
            _loginForm.Dispose();
            // Dispose();
            Hide();
            MainUtils.LoginFlag = true;
            _afterLogin(_mainFormSize);

            // Store current account info
            if (MainUtils.IsAutoLoginEnabled()) {
                String loginInfo = $"{SystemUtils.UserInfo.account},{SystemUtils.UserInfo.password}";
                MainUtils.SetAutoLoginInfo(loginInfo);
            }

        }
        #endregion

        #region Resize methods
        #endregion

        #region private class
        private class LoginPopUpForm: CustomPopUpForm {
            private CustomContentPanel _bigTitlePanel;
            private Label _bigTitle;
            private CustomContentPanel _inputPanel;
            private CustomTextBoxGroup _accountBox;
            private CustomTextBoxGroup _passwordBox;

            public CustomTextBoxGroup AccountBox { get => _accountBox; set => _accountBox = value; }
            public CustomTextBoxGroup PasswordBox { get => _passwordBox; set => _passwordBox = value; }

            public LoginPopUpForm(Action clickLogin, bool firstLogin) {
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
                ContentPanel.FlowDirection = FlowDirection.TopDown;
                ButtonAlignment = HorizontalAlignment.Center;

                _bigTitlePanel = new() {
                    Parent = ContentPanel,
                };
                _inputPanel = new() {
                    Parent = ContentPanel,
                };
                _bigTitle = new() {
                    Parent = _bigTitlePanel,
                    Text = "用户登录",
                    AutoSize = true,
                };
                _accountBox = new("账户名") {
                    Parent = _inputPanel,
                    NameAlignment = HorizontalAlignment.Right,
                    Ratio = 8,
                };
                _accountBox.GetTextBox(0).Box.ImeMode = ImeMode.Disable;
                _accountBox.GetTextBox(0).Box.KeyDown += (s, e) => ClickLogin(e);
                _passwordBox = new("密码") {
                    Parent = _inputPanel,
                    NameAlignment = HorizontalAlignment.Right,
                    Ratio = 8,
                };
                _passwordBox.GetTextBox(0).Box.PasswordChar = '*';
                _passwordBox.GetTextBox(0).Box.KeyDown += (s, e) => ClickLogin(e);

                CheckAutoLogin(clickLogin, firstLogin);

                void ClickLogin(KeyEventArgs e) {
                    if (e.KeyCode == Keys.Enter) {
                        clickLogin();
                    }
                }
            }

            private async void CheckAutoLogin(Action clickLogin, bool firstLogin) {
                await Task.Run(async () => {
                    while (!IsHandleCreated) {
                        await Task.Delay(100);
                    }

                    Invoke(() => {
                        if (firstLogin && MainUtils.IsAutoLoginEnabled()) {
                            string loginInfo = MainUtils.GetAutoLoginInfo();
                            if (!string.IsNullOrEmpty(loginInfo)) {
                                string[] accountInfo = loginInfo.Split(",");
                                var (accountStr, passwordStr) = (accountInfo[0], accountInfo[1]);

                                _accountBox.SetValue(0, accountStr);
                                _passwordBox.SetValue(0, passwordStr);

                                clickLogin();
                            }
                        }
                    });
                });
            }

            public void ResizeSelf() {
                Size mainSize = WidgetUtils.MainSize;

                Padding contentPadding = ContentPanel.Padding;
                int boxHeight = WidgetUtils.TextOrComboBoxHeight();
                int boxVMargin = boxHeight / 5;

                int bigTitleHeight = (int) (boxHeight * 1.5);
                int bigTitleVPadding = bigTitleHeight / 10;

                int contentWidth = (int) (mainSize.Width * .4);
                int boxWidth = contentWidth - contentPadding.Size.Width;

                _bigTitlePanel.Size = new(boxWidth, bigTitleHeight + bigTitleVPadding * 2);
                _bigTitlePanel.Padding = new(0, bigTitleVPadding, 0, bigTitleVPadding);
                _bigTitle.Font = new(WidgetsConfigs.SystemFontFamily, bigTitleHeight * .54F, FontStyle.Bold, GraphicsUnit.Pixel);
                _bigTitle.Margin = new((boxWidth - _bigTitle.Width) / 2, (bigTitleHeight - _bigTitle.Height) / 2, 0, 0);

                _accountBox.Size = new(boxWidth, boxHeight);
                _accountBox.Margin = new(0, boxVMargin, 0, boxVMargin);
                _passwordBox.Size = new(boxWidth, boxHeight);
                _passwordBox.Margin = new(0, boxVMargin, 0, boxVMargin);
                _inputPanel.Size = new(boxWidth, (boxHeight + boxVMargin * 2) * 2);

                SetContentSizeAndSelfSize(new(contentWidth, _bigTitlePanel.Height + _inputPanel.Height + contentPadding.Size.Height));
            }
        }
        #endregion
    }
}
