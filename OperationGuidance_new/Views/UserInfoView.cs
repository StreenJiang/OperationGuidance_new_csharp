using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_service.Utils;
using CustomLibrary.TextBoxes;
using CustomLibrary.Buttons;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Constants;

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
        private CustomContentPanel _userInfoPanel;
        private TitlePanel _userInfoTitlePanel;
        private CustomContentPanel _userInfoContentPanel;
        private CustomTextBoxGroup _staffIdBox;
        private CustomTextBoxGroup _userNameBox;
        private CustomTextBoxGroup _positionBox;
        private CustomTextBoxGroup _roleTypeBox;
        #endregion

        #region Constructors
        public UserInfoView() {
            // Default values
            FlowDirection = FlowDirection.TopDown;

            // Initilizations
            InitializeUserInfoPanel();
        }
        #endregion

        #region Override methods
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            Size mainSize = WidgetUtils.MainSize;
            _titleHeight = WidgetUtils.ContentTitleHeight();
            _contentHGap = (int) (mainSize.Height * _contentHGapRatio);
            _contentVGap = (int) (mainSize.Height * _contentVGapRatio);
            _contentHPadding = (int) (mainSize.Width * .015);
            _contentVPadding = (int) (mainSize.Height * .03);

            // Resizes
            ResizeUserNamePanel();
        }
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            CustomChildMenuFirstButton childButton = WidgetUtils.GetChildMenu(602);
            // MenuConfig menuConfig = SystemConfigs.MenuCongfigs.Single(c => c.Id == 600).Children.Single(c => c.Id == 602);
            childButton.Click += (sender, eventArgs) => {
                Action<bool>? backToLoginView = WidgetUtils.BackToLoginView;
                if (backToLoginView != null) {
                    backToLoginView(true);
                }
            };

        }
        #endregion

        #region Initialization methods
        private void InitializeUserInfoPanel() {
            _userInfoPanel = new() {
                Parent = this,
                FlowDirection = FlowDirection.TopDown,
            };
            _userInfoTitlePanel = new("用户基本信息") {
                Parent = _userInfoPanel,
                UnderlineColor = ColorConfigs.COLOR_TITLE_UNDERLINE,
            };
            _userInfoContentPanel = new() {
                Parent = _userInfoPanel,
            };
            _staffIdBox = new("员工ID") {
                Parent = _userInfoContentPanel,
                NameAlignment = HorizontalAlignment.Right,
                ReadOnly = true,
                Ratio = 7.25,
            };
            _userNameBox = new("姓名") {
                Parent = _userInfoContentPanel,
                NameAlignment = HorizontalAlignment.Right,
                ReadOnly = true,
                Ratio = 7.25,
            };
            _positionBox = new("职位") {
                Parent = _userInfoContentPanel,
                NameAlignment = HorizontalAlignment.Right,
                ReadOnly = true,
                Ratio = 7.25,
            };
            _roleTypeBox = new("权限角色") {
                Parent = _userInfoContentPanel,
                NameAlignment = HorizontalAlignment.Right,
                ReadOnly = true,
                Ratio = 7.25,
            };
            UserAccountInfoDTO userInfo = SystemUtils.UserInfo;
            _staffIdBox.GetTextBox(0).Box.Text = userInfo.staff_id + "";
            _userNameBox.GetTextBox(0).Box.Text = userInfo.name;
            _positionBox.GetTextBox(0).Box.Text = userInfo.position;
            _roleTypeBox.GetTextBox(0).Box.Text = Enum.GetName(typeof(Roles), userInfo.role_type);
        }
        #endregion

        #region Resize methods
        private void ResizeUserNamePanel() {
            // Resize title
            _userInfoTitlePanel.Size = new(Width, _titleHeight);
            // Resize Content
            int boxHeight = WidgetUtils.TextOrComboBoxHeight();
            int contentHeight = boxHeight * 2 + _contentVPadding * 2 + _contentVGap / 2;
            _userInfoContentPanel.Size = new(Width, contentHeight);
            _userInfoContentPanel.Padding = new(_contentHPadding, _contentVPadding, _contentHPadding, _contentVPadding);
            // Resize box and button
            int boxWidth = (Width - _userInfoContentPanel.Padding.Size.Width - _contentHGap) / 2;
            _staffIdBox.Size = new(boxWidth, boxHeight);
            _staffIdBox.Margin = new(0, 0, _contentHGap / 2, 0);
            _userNameBox.Size = new(boxWidth, boxHeight);
            _userNameBox.Margin = new(0, 0, _contentHGap / 2, 0);
            _positionBox.Size = new(boxWidth, boxHeight);
            _positionBox.Margin = new(0, _contentVGap / 2, _contentHGap / 2, 0);
            _roleTypeBox.Size = new(boxWidth, boxHeight);
            _roleTypeBox.Margin = new(0, _contentVGap / 2, _contentHGap / 2, 0);
            // Resize outer panel
            _userInfoPanel.Size = new(Width, _userInfoTitlePanel.Height + _userInfoContentPanel.Height);
        }
        #endregion

        public override void VisibleToTrue() {
            base.VisibleToTrue();
            UserAccountInfoDTO userInfo = SystemUtils.UserInfo;
            _staffIdBox.GetTextBox(0).Box.Text = userInfo.staff_id + "";
            _userNameBox.GetTextBox(0).Box.Text = userInfo.name;
            _positionBox.GetTextBox(0).Box.Text = userInfo.position;
            _roleTypeBox.GetTextBox(0).Box.Text = Enum.GetName(typeof(Roles), userInfo.role_type);
            if (WidgetUtils.RefreshLoginUserName != null) {
                WidgetUtils.RefreshLoginUserName(SystemUtils.LoggedUserName);
            }
        }
    }
}
