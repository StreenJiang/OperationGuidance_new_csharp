using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Configs.DTOs;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;

namespace OperationGuidance_new.Views.SubViews {
    public class ArrangerOperationPopUpForm: CustomPopUpForm {

        private IoBoxTask ioBoxTask;
        private AWorkplaceContentPanel _workplace;
        private string _categoryName;

        private Panel _contentInnerPanel;
        private CommonButton _ioTestButton;
        private TitlePanel _openLidTitle;
        private List<CommonButtonGroup> _openLidGroups;

        private SciiXtArrangerConfig _config;
        private int _boxHeight;
        private int _boxMargin;

        public int OpenLidButtonCount => _config.GroupList.Count;

        public ArrangerOperationPopUpForm(string categoryName,
                                          AWorkplaceContentPanel workplace,
                                          IoBoxTask ioBoxTask) {
            this.ioBoxTask = ioBoxTask;
            _workplace = workplace;
            _categoryName = categoryName;

            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "螺丝机信号点测试 - " + categoryName;

            _config = ConfigUtils.LoadConfig<SciiXtArrangerConfig>();

            _contentInnerPanel = new() {
                Parent = ContentPanel,
            };

            // IO test button at top, centered
            _ioTestButton = new() {
                Label = "IO点位测试",
                Parent = _contentInnerPanel,
            };
            _ioTestButton.Click += IoTestClick;

            // Subtitle using TitlePanel (same as system config)
            _openLidTitle = new("螺丝机开盖") {
                Parent = _contentInnerPanel,
                UnderlineColor = ColorConfigs.COLOR_TITLE_UNDERLINE,
            };

            // Open-lid groups
            _openLidGroups = new();
            foreach (var group in _config.GroupList) {
                CommonButtonGroup btnGroup = new(group.name) {
                    Parent = _contentInnerPanel,
                };
                btnGroup.GetButton(0).Label = "点击开盖";
                var capturedGroup = group;
                btnGroup.GetButton(0).Click += (s, e) => OpenLidScan(capturedGroup);
                _openLidGroups.Add(btnGroup);
            }
        }

        private void OpenLidScan(ArrangerGroupDTO group) {
            using ArrangerOpenLidScanPopUpForm scanForm = new(group, ioBoxTask, _workplace);
            scanForm.PretendToShowToCreateHandlesForChildren();

            int contentWidth = (int)(WidgetUtils.MainSize.Width * .65);
            Padding contentPadding = scanForm.ContentPanel.Padding;
            int boxHeight = WidgetUtils.TextOrComboBoxHeight();
            int boxMargin = boxHeight / 5;
            scanForm.BarcodeBox.Size = new(contentWidth - contentPadding.Size.Width - boxMargin * 2, boxHeight);
            scanForm.BarcodeBox.Margin = new(boxMargin);
            int contentHeight = boxHeight + boxMargin * 2 + contentPadding.Size.Height;

            scanForm.SetContentSizeAndSelfSize(new(contentWidth, contentHeight));
            scanForm.ShowDialog();
        }

        private void IoTestClick(object? sender, EventArgs e) {
            bool confirmed = _workplace.OpenAdminPasswordPopUpForm("IO点位测试需要管理员操作密码", false);
            if (confirmed) {
                int panelHeight = WidgetUtils.TextOrComboBoxHeight();
                int boxMargin = panelHeight / 5;
                int tableHeight = 2 * (panelHeight + boxMargin * 2) + boxMargin;

                using ArrangerIoTestPopUpForm ioTestForm = new(_categoryName, ioBoxTask);
                ioTestForm.PretendToShowToCreateHandlesForChildren();
                Size contentSize = new((int)(WidgetUtils.MainSize.Width * .5),
                    tableHeight + ioTestForm.ContentPanel.Padding.Size.Height);
                ioTestForm.SetContentSizeAndSelfSize(contentSize);
                ioTestForm.ShowDialog();
            }
        }

        public void ResizeSelf() {
            CalculateSizes();
            Invalidate();
        }

        private void CalculateSizes() {
            Padding contentPadding = ContentPanel.Padding;
            int contentWidth = ContentPanel.Width - contentPadding.Size.Width;

            _boxHeight = WidgetUtils.TextOrComboBoxHeight();
            _boxMargin = _boxHeight / 5;
            int boxWithMargin = _boxHeight + _boxMargin * 2;

            int fullWidth = contentWidth - _boxMargin * 2;
            int titleHeight = (int)(_boxHeight * 1.25);
            int titleBoxWithMargin = titleHeight + _boxMargin * 2;
            int y = _boxMargin;

            // IO test button — full width with margin
            _ioTestButton.Location = new(_boxMargin, y);
            _ioTestButton.Size = new(fullWidth, _boxHeight);
            y += boxWithMargin;

            // TitlePanel — slightly taller for larger font
            _openLidTitle.Location = new(0, y);
            _openLidTitle.Size = new(contentWidth, titleHeight);
            y += titleBoxWithMargin;

            // Open-lid groups — full width with margin, suitable ratio
            foreach (CommonButtonGroup grp in _openLidGroups) {
                grp.Ratio = null;
                grp.ButtonFlowDirection = FlowDirection.RightToLeft;
                grp.Location = new(_boxMargin, y);
                grp.Size = new(fullWidth, _boxHeight);
                y += boxWithMargin;
            }

            int panelHeight = y + _boxMargin;

            _contentInnerPanel.Location = new(contentPadding.Left, contentPadding.Top);
            _contentInnerPanel.Size = new(contentWidth, panelHeight);
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            CalculateSizes();
        }
    }
}
