using CustomLibrary.Panels;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class DeviceModelView: CustomContentPanel {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        // DataGridView panel
        private DataGridViewGroup<DeviceModelVO> _operationDataGridView;
        // Add new pop up form
        // private AddNewPopUpForm _addNewPopUpForm;
        #endregion

        #region Constructors
        public DeviceModelView() {
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
                        name = "PF6000 - after searching",
                        description = "这是就是个型号，创建新设备的时候可以用来选的（不是填的）",
                        brand_name = "阿特拉斯",
                        category_name = "控制器（工具）",
                    }
                };
            };
            _operationDataGridView.AddTextBox("设备型号名称", false, (DeviceModelVO vo, string? value) => vo.name = value);
            _operationDataGridView.AddComboBox("设备品牌", (DeviceModelVO vo, int? value) => vo.brand_id = value, new() { { "阿特拉斯", 1 }, { "速动", 2 }, { "安维能", 3 } });
            _operationDataGridView.AddComboBox("设备类型", (DeviceModelVO vo, int? value) => vo.category_id = value, new() { { "控制器（工具）", 1 }, { "力臂", 2 } });

            // Initialization
            InitializeGridView();
        }
        #endregion

        #region Initialize methods
        private void InitializeGridView() {
            List<DeviceModelVO> vos = new() {
                new() {
                    id = 1,
                    name = "PF6000",
                    description = "这是高级型号",
                    brand_name = "阿特拉斯",
                    category_name = "控制器（工具）",
                },
                new() {
                    id = 2,
                    name = "PF4000",
                    description = "这是低级型号",
                    brand_name = "阿特拉斯",
                    category_name = "控制器（工具）",
                },
                new() {
                    id = 3,
                    name = "xxxxxx",
                    description = "这是低级型号",
                    brand_name = "安维能",
                    category_name = "力臂",
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
