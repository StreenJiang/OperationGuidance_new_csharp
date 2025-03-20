using CustomLibrary.Configs;
using CustomLibrary.Constants;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views.AbstractViews {
    public abstract class AWorkplaceMissionView<T, V>: CustomContentPanel where T : AWorkplaceContentPanel where V : WorkplaceTopBar, new() {
        private MissionListPanel? _missionListPanel;
        private List<ProductMissionDTO>? _productMissionDTOs;
        private OperationGuidanceApis? apis;
        private T? _workplacePanel;
        private bool _operatorOpenning = false;

        public AWorkplaceMissionView() => Initialize(false);
        public AWorkplaceMissionView(bool operatorOpenning) : base() {
            Initialize(operatorOpenning);
        }
        private void Initialize(bool operatorOpenning) {
            // Get apis
            apis = SystemUtils.GetApis();

            _operatorOpenning = operatorOpenning;
            // Open workplace directly if this is opened by operators
            if (_operatorOpenning) {
                OpenWorkplaceViewDirectly();
            } else {
                OpenMissionListView();
            }
        }

        private void OpenMissionListView() {
            // Initialize
            _missionListPanel = new("选择任务", "直接进入工作台", (s, e) => OpenWorkplaceViewDirectly()) {
                Margin = new Padding(0),
                Parent = this,
            };
        }
        private void OpenWorkplaceViewDirectly() => OpenWorkplaceView(null);

        private void CheckAndDisplay() {
            if (_missionListPanel != null) {
                // Fetch data
                FetchData();
                // If there is no any mission, so show the big button
                if (_productMissionDTOs != null) {
                    _missionListPanel.RefreshMissionBlocks(_productMissionDTOs, OpenWorkplaceView);
                }
            }
        }

        public override void VisibleToTrue() {
            if (_workplacePanel != null && !_workplacePanel.IsDisposed) {
                System.Console.WriteLine($"_workplacePanel.Activated: {_workplacePanel.Activated}");
                // TODO: 这里或许可以做一个“任务中断”的效果，即不是每次进入都打开一个新的任务
            }
            // Check and display view
            CheckAndDisplay();
            // Invoke base, it will resize all children
            base.VisibleToTrue();
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            // Resize mission list panel
            if (_missionListPanel != null) {
                _missionListPanel.Size = new(Width, Height);
                _missionListPanel.ResizeChildren(eventArgs);
                if (_missionListPanel.Visible) {
                    _missionListPanel.Invalidate();
                }
            }
        }

        public void OpenWorkplaceView(int? missionId) {
            if (_workplacePanel != null && !_workplacePanel.IsDisposed) {
                _workplacePanel.Dispose();
            }
            // Create a new view
            CustomTabPanel pagePanel = new() {
                Parent = WidgetUtils.MainForm,
                Size = WidgetUtils.MainForm.ClientSize,
            };
            V topBar = new() {
                BackColor = ColorConfigs.COLOR_MAIN_MENU_BACKGROUND,
                MainMenuLogo = Properties.Resources.logo,
                Margin = new Padding(0),
                PanelDirection = MenuPanelDirection.TOP,
                TitleColor = ColorConfigs.COLOR_WORKPLACE_TITLE,
                OperatorOpenning = _operatorOpenning,
            };
            _workplacePanel = GetWrokplacePanel(missionId, topBar);
            topBar.Workplace = _workplacePanel;

            pagePanel.Controls.Add(topBar);
            pagePanel.Controls.Add(_workplacePanel);
            pagePanel.ResizeChildren();

            // Hide main panel
            if (WidgetUtils.MainPanel != null) {
                WidgetUtils.MainPanel.Visible = false;
            }

            if (_operatorOpenning) {
                WidgetUtils.MainForm.SizeChanged += (s, e) => {
                    pagePanel.Size = WidgetUtils.MainSize;
                };
            }

            pagePanel.Size = new(WidgetUtils.MainSize.Width - 2, WidgetUtils.MainSize.Height - 2);
            pagePanel.Location = new(1, 1);
        }

        protected abstract T GetWrokplacePanel(int? missionId, WorkplaceTopBar topBar);

        private void FetchData() {
            if (apis != null) {
                _productMissionDTOs = apis.QueryProductMissionList(new(SystemUtils.MacAddressesDTO.id)).ProductMissionDTOs;
            }
        }
    }
}
