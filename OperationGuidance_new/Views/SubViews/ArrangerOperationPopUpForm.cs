using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Configs.DTOs;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Views.SubViews {
    public class ArrangerOperationPopUpForm: CustomPopUpForm {

        private AWorkplaceContentPanel _workplace;
        private string _categoryName;
        private Dictionary<string, IoBoxTask> _ioBoxTasks;
        private List<DeviceIoDTO> _deviceIoDTOs;

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
                                          Dictionary<string, IoBoxTask> ioBoxTasks,
                                          List<DeviceIoDTO> deviceIoDTOs) {
            _workplace = workplace;
            _categoryName = categoryName;
            _ioBoxTasks = ioBoxTasks;
            _deviceIoDTOs = deviceIoDTOs;

            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "螺丝机信号点测试 - " + categoryName;

            _config = ConfigUtils.LoadConfig<SciiXtArrangerConfig>();

            _contentInnerPanel = new() {
                Parent = ContentPanel,
            };

            // IO test button at top
            _ioTestButton = new() {
                Label = "IO点位测试",
                Parent = _contentInnerPanel,
            };
            _ioTestButton.Click += IoTestClick;

            // Subtitle
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

                IoBoxTask? resolvedTask = ResolveIoBoxTask(group);
                btnGroup.GetButton(0).Label = "点击开盖";
                if (resolvedTask != null) {
                    var capturedGroup = group;
                    var capturedTask = resolvedTask;
                    btnGroup.GetButton(0).Click += (s, e) => OpenLidScan(capturedGroup, capturedTask);
                } else {
                    btnGroup.GetButton(0).Enabled = false;
                    btnGroup.TextName = group.name + "(已删除)";
                    btnGroup.TextColor = Color.Red;
                }
                _openLidGroups.Add(btnGroup);
            }
        }

        private IoBoxTask? ResolveIoBoxTask(ArrangerGroupDTO group) {
            if (group.arranger_id == null)
                return null;

            DeviceIoDTO? dto = _deviceIoDTOs.FirstOrDefault(d => d.id == group.arranger_id.Value);
            if (dto == null)
                return null;

            string key = MainUtils.GetTCPClientKey(dto.ip, dto.port);
            if (!_ioBoxTasks.TryGetValue(key, out IoBoxTask? task))
                return null;

            if (task.ArrangerType == null)
                return null;

            return task;
        }

        private void OpenLidScan(ArrangerGroupDTO group, IoBoxTask ioBoxTask) {
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
            // Find any available arranger for IO test
            IoBoxTask? ioBoxTask = null;
            foreach (var task in _ioBoxTasks.Values) {
                if (task.ArrangerType != null) {
                    ioBoxTask = task;
                    break;
                }
            }
            if (ioBoxTask == null) {
                WidgetUtils.ShowConfirmPopUp("没有可用的螺丝机设备");
                return;
            }

            bool confirmed = _workplace.OpenAdminPasswordPopUpForm("IO点位测试需要管理员操作密码");
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

            _ioTestButton.Location = new(_boxMargin, y);
            _ioTestButton.Size = new(fullWidth, _boxHeight);
            y += boxWithMargin;

            _openLidTitle.Location = new(0, y);
            _openLidTitle.Size = new(contentWidth, titleHeight);
            y += titleBoxWithMargin;

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
