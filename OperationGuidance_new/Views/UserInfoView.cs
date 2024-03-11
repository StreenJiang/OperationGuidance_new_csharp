using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_service.Utils;
using CustomLibrary.TextBoxes;
using OperationGuidance_new.Utils;
using CustomLibrary.Buttons;

namespace OperationGuidance_new.Views {
    public class UserInfoView: CustomContentPanel {
        #region Fields
        private readonly float _contentHGapRatio = 0.025F;
        private readonly float _contentVGapRatio = 0.05F;
        private readonly float _contentHPaddingRatio = 0.15F;
        private readonly float _contentVPaddingRatio = 0.03F;
        private int _titleHeight;
        private int _contentHGap;
        private int _contentVGap;
        private int _contentHPadding;
        private int _contentVPadding;
        // User basic info panel
        private CustomContentPanel _userNamePanel;
        private TitlePanel _userNameTitlePanel;
        private CustomContentPanel _userNameContentPanel;
        private CustomTextBoxGroup _userNameBox;
        #endregion

        #region Constructors
        public UserInfoView() {
            // Default values
            FlowDirection = FlowDirection.TopDown;

            // Initilizations
            InitializeUserNamePanel();
        }
        #endregion

        #region Override methods
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            Control mainParent = WidgetUtils.MainPanel;
            _titleHeight = WidgetUtils.ContentTitleHeight();
            _contentHGap = (int) (mainParent.Height * _contentHGapRatio);
            _contentVGap = (int) (mainParent.Height * _contentVGapRatio);
            _contentHPadding = (int) (mainParent.Width * .015);
            _contentVPadding = (int) (mainParent.Height * .03);

            // Resizes
            ResizeUserNamePanel();
        }
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            CustomChildMenuFirstButton childButton = WidgetUtils.GetChildMenu(602);
            // MenuConfig menuConfig = SystemConfigs.MenuCongfigs.Single(c => c.Id == 600).Children.Single(c => c.Id == 602);
            childButton.Click += (sender, eventArgs) => {
                DialogResult result = MessageBox.Show(null, "确定要退出登录吗？", "退出登录", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes) {
                    Form mainForm = WidgetUtils.MainForm;
                    CustomTabPanel mainPanel = WidgetUtils.MainPanel;
                    LoginView loginView = MainUtils.LoginView;
                    // 先清理所有UI组件
                    mainPanel.Hide();
                    // mainPanel.Controls.Clear();
                    // 告诉登录界面现在的主界面尺寸
                    loginView.MainFormSize = mainForm.Size;
                    Size screenSize = WidgetUtils.GetScreenResolution();
                    loginView.AfterLogin = mainFormSize => {
                        if (mainFormSize == screenSize) {
                            mainForm.WindowState = FormWindowState.Maximized;
                        } else {
                            mainForm.Size = mainFormSize;
                            mainForm.ClientSize = mainFormSize;
                            mainForm.Location = new((screenSize.Width - mainFormSize.Width) / 2, (screenSize.Height - mainFormSize.Height) / 2);
                        }
                        MinimumSize = new Size(400, 300);
                        MaximumSize = screenSize;
                        mainPanel.Size = mainFormSize;
                        mainPanel.Show();
                    };
                    // 重设登录界面尺寸
                    Size loginViewSize = WidgetUtils.GetLoginViewSize(mainForm.Size);
                    mainForm.Size = loginViewSize;
                    mainForm.ClientSize = loginViewSize;
                    mainForm.Location = new((screenSize.Width - loginViewSize.Width) / 2, (screenSize.Height - loginViewSize.Height) / 2);
                    // mainPanel.Size = loginView.MainFormSize;
                    loginView.Size = loginViewSize;
                    loginView.BackShowing = WidgetUtils.ResizeImageWithoutLosingQuality(loginView.Back, loginViewSize);
                    // 显示登录界面
                    loginView.Show();
                    loginView.ShowLoginForm();
                }
            };

        }
        #endregion

        #region Initialization methods
        private void InitializeUserNamePanel() {
            _userNamePanel = new() {
                Parent = this,
                FlowDirection = FlowDirection.TopDown,
            };
            _userNameTitlePanel = new("用户基本信息") {
                Parent = _userNamePanel,
                UnderlineColor = ColorConfigs.COLOR_TITLE_UNDERLINE,
            };
            _userNameContentPanel = new() {
                Parent = _userNamePanel,
            };
            _userNameBox = new("用户名称") {
                Parent = _userNameContentPanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                NameAlignment = HorizontalAlignment.Right,
                ReadOnly = true,
                Ratio = 7.25,
            };
            _userNameBox.GetTextBox(0).Box.Text = SystemUtils.LoggedUserName;
        }
        #endregion

        #region Resize methods
        private void ResizeUserNamePanel() {
            // Resize title
            _userNameTitlePanel.Size = new(Width, _titleHeight);
            // Resize Content
            int boxHeight = WidgetUtils.TextOrComboBoxHeight();
            int contentHeight = boxHeight + _contentVPadding * 2;
            _userNameContentPanel.Size = new(Width, contentHeight);
            _userNameContentPanel.Padding = new(_contentHPadding, _contentVPadding, _contentHPadding, _contentVPadding);
            // Resize box and button
            _userNameBox.Size = new((int) (Width - _userNameContentPanel.Padding.Size.Width - _contentHGap) / 2, boxHeight);
            _userNameBox.Margin = new(0, 0, _contentHGap / 2, 0);
            // Resize outer panel
            _userNamePanel.Size = new(Width, _userNameTitlePanel.Height + _userNameContentPanel.Height);
        }
        #endregion

        public override void VisibleToTrue() {
            base.VisibleToTrue();
            _userNameBox.GetTextBox(0).Box.Text = SystemUtils.LoggedUserName;
            if (WidgetUtils.RefreshLoginUserName != null) {
                WidgetUtils.RefreshLoginUserName(SystemUtils.LoggedUserName);
            }
        }
    }
}
