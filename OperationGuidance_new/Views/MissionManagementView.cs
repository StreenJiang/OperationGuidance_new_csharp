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
        private int _titleHeight;
        private int _cellHorizontalMargin;
        private int _cellVerticalMargin;
        private Size _cellSize;
        private MissionNewButtonPanel _bigButtonPanel;
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
            _bigButtonPanel = new() {
                Margin = new Padding(0),
                Parent = this,
                Visible = false,
            };
            _missionListPanel = new(
                "任务列表", 
                _tableColumns, 
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
                Visible = false,
            };

            // Check and display view
            CheckAndDisplay();
        }

        private void CheckAndDisplay() {
            // Fetch data
            FetchData();
            // If there is no any mission, so show the big button
            if (_productMissionDTOs.Count == 0) {
                _missionListPanel.Visible = false;
                _bigButtonPanel.Visible = true;
            } else {
                _bigButtonPanel.Visible = false;
                _missionListPanel.Visible = true;
                _missionListPanel.RefreshMissionBlocks(_productMissionDTOs, OpenEditionPageView);
            }
        }

        public override void VisibleToTrue() {
            // Check and display view
            CheckAndDisplay();
            // Invoke base, it will resize all children
            base.VisibleToTrue();
        }

        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            _titleHeight = WidgetUtils.ContentTitle();
            // Calculate height of cells: use height of top level control is because self height will automatically change because of scroll bar
            _cellSize = new(0, (int) (this.TopLevelControl.Height * _cellHightRatio));
            _cellVerticalMargin = _cellSize.Height / 15;
            // If there is no any mission, then don't need scroll bar
            if (_productMissionDTOs.Count == 0) {
                NewHeight = 0;
                return false;
            }
            // Calculate table's size, depends on all cells
            int rowsCount = (int) Math.Ceiling(_productMissionDTOs.Count / (double) _tableColumns);
            NewHeight = _titleHeight + (rowsCount + 1) * _cellVerticalMargin + rowsCount * _cellSize.Height;
            return this.NewHeight > parentNewHeight;
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            // Resize big button panel
            _bigButtonPanel.Size = new(Parent.Width, Parent.Height);
            if (_bigButtonPanel.Visible) {
                _bigButtonPanel.Invalidate();
            }
            // Calculate width of cells
            _cellHorizontalMargin = (int) (this.Width * _cellGapRatio);
            int gapNum = _tableColumns + 1; // Including outer margin
            _cellSize.Width = (this.Width - _cellHorizontalMargin * gapNum) / _tableColumns;
            // Set properties before resize mission list panel
            _missionListPanel.TitleHeight = _titleHeight;
            _missionListPanel.CellSize = _cellSize;
            _missionListPanel.CellHorizontalMargin = _cellHorizontalMargin;
            _missionListPanel.CellVerticalMargin = _cellVerticalMargin;
            // Resize mission list panel
            _missionListPanel.Size = new(this.Width, this.Height);
            _missionListPanel.ResizeChildren(eventArgs);
            if (_missionListPanel.Visible) {
                _missionListPanel.Invalidate();
            }
        }

        private void OpenEditionPageView(ProductMissionDTO missionDTO) {
            EditionView.OpenEditionPage(missionDTO);
            // Hide current view and release corresponding menu button
            CommonUtils.CannotBeNull(EditionView.CorrespondingMenuButton).TriggerClick(EventArgs.Empty);
        }

        private void FetchData() {
            QueryProductMissionListReq req = new();
            QueryProductMissionListRsp rsp;

            req.UserId = SystemUtils.LoggedUserId();
            rsp = apis.QueryProductMissionListRsp(req);
            _productMissionDTOs = rsp.ProductMissionsDTOs;
        }
    }
}
