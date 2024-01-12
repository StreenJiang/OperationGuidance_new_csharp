using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_service.Utils;

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
            _titleHeight = WidgetUtils.ContentTitle();
            _contentHGap = (int) (mainParent.Height * _contentHGapRatio);
            _contentVGap = (int) (mainParent.Height * _contentVGapRatio);
            _contentHPadding = (int) (mainParent.Width * .015);
            _contentVPadding = (int) (mainParent.Height * .03);

            // Resizes
            ResizeUserNamePanel();
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
            _userNameBox.GetTextBox(0).Box.Text = SystemUtils.LoggedUserName();
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
    }
}
