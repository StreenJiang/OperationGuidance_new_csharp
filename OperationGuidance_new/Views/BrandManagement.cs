using CustomLibrary.Panels;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class BrandManagementView: CustomContentPanel {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        // DataGridView panel
        private DataGridViewGroup<BrandVO> _operationDataGridView;
        // Add new pop up form
        // private AddNewPopUpForm _addNewPopUpForm;
        #endregion

        #region Constructors
        public BrandManagementView() {
            // Default values
            FlowDirection = FlowDirection.TopDown;
            
            // Get Apis
            apis = SystemUtils.GetApis();

            // Initialize grid view
            _operationDataGridView = new() {
                Parent = this,
            };
            _operationDataGridView.QueryData = (vo) => {
                return new() {
                    new() {
                        id = 1,
                        name = "安维能 - after searching",
                        description = "xxxxxxxxxxxxxxxx",
                    }
                };
            };
            _operationDataGridView.AddTextBox("品牌名称", false, (BrandVO vo, string? value) => vo.name = value);
            _operationDataGridView.AddTextBox("品牌描述", false, (BrandVO vo, string? value) => vo.description = value);

            // Initialization
            InitializeGridView();
        }
        #endregion

        #region Initialize methods
        private void InitializeGridView() {
            List<BrandVO> vos = new() {
                new() {
                    id = 1,
                    name = "安维能",
                    description = "xxxxxxxxxxxxxxxx",
                },
                new() {
                    id = 2,
                    name = "阿特拉斯",
                    description = "sadfasdfasdfasdfasdf",
                },
                new() {
                    id = 3,
                    name = "速动",
                    description = "zx.gj;ojklj;lj;lj",
                },
            };
            _operationDataGridView.DataSource = vos;
        }
        #endregion

        #region Reusable methods
        #endregion

        #region Override methods
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            Size contentSize = new(Width - Padding.Size.Width, Height - Padding.Size.Height);
            _operationDataGridView.Size = contentSize;
        }
        public override void VisibleToTrue() {
            base.VisibleToTrue();
        }
        #endregion
    }
}
