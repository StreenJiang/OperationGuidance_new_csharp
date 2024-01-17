using CustomLibrary.Panels;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class DeviceManagementView: CustomContentPanel {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        // DataGridView panel
        private DataGridViewGroup<DeviceVO> _operationDataGridView;
        // Add new pop up form
        // private AddNewPopUpForm _addNewPopUpForm;
        #endregion

        #region Constructors
        public DeviceManagementView() {
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
                        name = "拧紧工具1号",
                        description = "这是个拧紧工具来的",
                        brand_name = "阿特拉斯",
                        category_name = "控制器（工具）",
                        model_name = "PF6000",
                    }
                };
            };
            _operationDataGridView.AddTextBox("设备名称", false, (DeviceVO vo, string? value) => vo.name = value);
            _operationDataGridView.AddTextBox("设备描述", false, (DeviceVO vo, string? value) => vo.description = value);
            _operationDataGridView.AddComboBox("设备品牌", (DeviceVO vo, int? value) => { }, new() { { "阿特拉斯", 1 }, { "速动", 2 }, { "安维能", 3 } });
            _operationDataGridView.AddComboBox("设备类型", (DeviceVO vo, int? value) => { }, new() { { "控制器（工具）", 1 }, { "力臂", 2 } });
            _operationDataGridView.AddComboBox("设备型号", (DeviceVO vo, int? value) => { }, new() { { "PF6000", 1 }, { "PF4000", 2 }, { "XXXXXX", 3 } });

            // Initialization
            InitializeGridView();
        }
        #endregion

        #region Initialize methods
        private void InitializeGridView() {
            List<DeviceVO> vos = new() {
                new() {
                    id = 1,
                    name = "拧紧工具1号",
                    description = "这是个拧紧工具来的",
                    brand_name = "阿特拉斯",
                    category_name = "控制器（工具）",
                    model_name = "PF6000",
                },
                new() {
                    id = 2,
                    name = "拧紧工具2号",
                    description = "这是个拧紧工具来的",
                    brand_name = "速动",
                    category_name = "控制器（工具）",
                    model_name = "SD300",
                },
                new() {
                    id = 3,
                    name = "力臂1号",
                    description = "这是个力臂",
                    brand_name = "安维能",
                    category_name = "力臂",
                    model_name = "xxxxx",
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
