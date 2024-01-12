using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Configs;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.Utils;

namespace CustomLibrary.Panels {
    public class UserInfoPanel: CustomContentPanelBase {
        #region Fields
        private AvatarButton _avatarButton;
        private Label _userName;
        #endregion

        #region Properties
        public string UserName { get => _userName.Text; set => _userName.Text = value; }
        #endregion

        #region Constructors
        public UserInfoPanel() {
            FlowDirection = FlowDirection.TopDown;
            _avatarButton = new() {
                Parent = this,
            };
            _userName = new() {
                Parent = this,
                Text = "xxxx",
                ForeColor = ColorConfigs.COLOR_USER_INFO_NAME,
                AutoSize = true,
            };
        }
        #endregion

        #region Override methods
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            // Font
            int textBoxHeight = WidgetUtils.TextOrComboBoxHeight();
            Font = new Font(WidgetsConfigs.SystemFontFamily, textBoxHeight * .55F, FontStyle.Bold, GraphicsUnit.Pixel);
            _userName.Font = Font;
            // Gap
            int gapBetweenAvatarAndName = textBoxHeight / 8;
            // Avatar button
            int avatarSide = (int) (Width * .7);
            _avatarButton.Size = new(avatarSide, avatarSide);
            _avatarButton.Margin = new((Width - avatarSide) / 2, (Height - avatarSide - gapBetweenAvatarAndName - textBoxHeight) / 2, 0, 0);
            // User name string
            _userName.Margin = new((Width - _userName.Width) / 2, gapBetweenAvatarAndName, 0, 0);
        }
        #endregion
    }
}
