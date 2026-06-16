using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using System.Threading;
using System.Threading.Tasks;

namespace OperationGuidance_new.Views {
    public class MissionManagementView: CustomContentPanel {
        private MissionListPanel _missionListPanel;
        private List<ProductMissionDTO> _productMissionDTOs;
        private readonly OperationGuidanceApis apis;
        private MissionEditionView? _editionView;
        private CancellationTokenSource? _checkCts;

        public MissionEditionView EditionView {
            get {
                if (_editionView == null) {
                    _editionView = WidgetUtils.GetView<MissionEditionView>();
                }
                return _editionView;
            }
        }

        public MissionManagementView() : base() {
            // Get apis
            apis = SystemUtils.GetApis();
            // Initialize
            _missionListPanel = new(
                "任务列表",
                "新建任务",
                (sender, eventArgs) => OpenEditionPageView(null)
            ) {
                Margin = new Padding(0),
                Parent = this,
            };
        }

        private async Task CheckAndDisplayAsync() {
            _checkCts?.Cancel();
            _checkCts?.Dispose();
            _checkCts = new CancellationTokenSource();
            var ct = _checkCts.Token;

            try {
                await FetchDataAsync();
                ct.ThrowIfCancellationRequested();
            } catch (OperationCanceledException) {
                return;
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"CheckAndDisplayAsync error: {ex.Message}");
                return;
            }

            if (IsDisposed || !Visible) return;
            _missionListPanel.RefreshMissionBlocks(_productMissionDTOs, OpenEditionPageView);
        }

        public override async void VisibleToTrue() {
            await CheckAndDisplayAsync();
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            // Resize mission list panel
            _missionListPanel.Size = new(Width, Height);
            _missionListPanel.ResizeChildren(eventArgs);
            if (_missionListPanel.Visible) {
                _missionListPanel.Invalidate();
            }
        }

        private void OpenEditionPageView(int? missionId) {
            if (EditionView.EditionPage == null || !EditionView.EditionPage.Modified || WidgetUtils.ShowConfirmPopUp("编辑界面存在未保存内容，是否打开新的界面？")) {
                EditionView.OpenEditionPage(missionId);
                // Hide current view and release corresponding menu button
                CommonUtils.CannotBeNull(EditionView.CorrespondingMenuButton).TriggerClick(EventArgs.Empty);
            }
        }

        private async Task FetchDataAsync() {
            _productMissionDTOs = await Task.Run(() =>
                apis.QueryProductMissionList(new(SystemUtils.MacAddressesDTO.id) { IsEditing = true }).ProductMissionDTOs
            );
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _checkCts?.Cancel();
                _checkCts?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
