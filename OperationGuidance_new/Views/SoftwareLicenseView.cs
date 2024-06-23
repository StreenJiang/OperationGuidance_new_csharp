using CustomLibrary.Buttons;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using log4net;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.ReusableWidgets;

namespace OperationGuidance_new.Views {
    public class SoftwareLicenseView : CustomContentPanel {
        private ILog logger = MainUtils.GetLogger(typeof(SoftwareLicenseView));

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

            string macsStr = string.Join(",", MainUtils.Macs);
            _multiLineGroup.AddButton("读取物理地址").Click += (s, e) => _multiLineGroup.TextBox.Box.Text = macsStr;

            _multiLineGroup.AddButton("确定").Click += (sender, eventArgs) => {
                string content = _multiLineGroup.TextBox.Box.Text;
                if (string.IsNullOrEmpty(content) || content == macsStr) {
                    WidgetUtils.ShowErrorPopUp("请输入许可证信息");
                } else {
                    try {
                        File.WriteAllText(MainUtils.LicensePath, content);

                        // Check license
                        MainUtils.CheckLicense();

                        if (MainUtils.LicenseOk) {
                            WidgetUtils.ShowNoticePopUp("许可证认证成功！");

                            // Set flag to re-create all forms
                            MainUtils.Self.AllCreated = false;

                            // Back to login view
                            Action<bool>? backToLoginView = WidgetUtils.BackToLoginView;
                            if (backToLoginView != null) {
                                backToLoginView(false);
                            }
                        } else {
                            WidgetUtils.ShowErrorPopUp("许可证认证失败，请确认MAC物理地址是否正确");
                        }
                    } catch (Exception e) {
                        logger.Warn($"License write failed, e = {e}");
                        WidgetUtils.ShowWarningPopUp("license.lic文件无法写入，请确认文件是否处于打开状态");
                    }
                }
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
