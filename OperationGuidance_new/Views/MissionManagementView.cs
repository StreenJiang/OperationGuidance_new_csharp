using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class MissionManagementView: CustomContentPanel {
        private readonly int _tableColumns = 4;
        private readonly float _cellGapRatio = 0.02F;
        private readonly float _cellHightRatio = 0.21F;
        private MissionListPanel _missionListPanel;
        private List<ProductMissionDTO> _productMissionDTOs;
        private readonly OperationGuidanceApis apis;
        private MissionEditionView? _editionView;

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
                (sender, eventArgs) => {
                    OpenEditionPageView(new ProductMissionDTO() {
                        name = "新建任务",
                        ProductSides = new() {
                            new() {
                                name = "产品面1",
                            },
                        },
                    });
                }
            ) {
                Margin = new Padding(0),
                Parent = this,
            };
        }

        private void CheckAndDisplay() {
            // Fetch data
            FetchData();
            // If there is no any mission, so show the big button
            _missionListPanel.RefreshMissionBlocks(_productMissionDTOs, OpenEditionPageView);
        }

        public override void VisibleToTrue() {
            // Check and display view
            CheckAndDisplay();
            // Invoke base, it will resize all children
            // base.VisibleToTrue();
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            // Resize mission list panel
            _missionListPanel.Size = new(Width, Height);
            _missionListPanel.ResizeChildren(eventArgs);
            if (_missionListPanel.Visible) {
                _missionListPanel.Invalidate();
            }
        }

        private void OpenEditionPageView(ProductMissionDTO missionDTO) {
            if (EditionView.EditionPage == null || !EditionView.EditionPage.Modified || WidgetUtils.ShowConfirmPopUp("编辑界面存在未保存内容，是否打开新的界面？")) {
                EditionView.OpenEditionPage(missionDTO);
                // Hide current view and release corresponding menu button
                CommonUtils.CannotBeNull(EditionView.CorrespondingMenuButton).TriggerClick(EventArgs.Empty);
            }
        }

        private void FetchData() {
            QueryProductMissionsWithCoverReq req = new();
            QueryProductMissionsWithCoverRsp rsp;

            rsp = apis.QueryProductMissionsWithCover(req);
            _productMissionDTOs = rsp.ProductMissionsDTOs;
        }
    }
}
