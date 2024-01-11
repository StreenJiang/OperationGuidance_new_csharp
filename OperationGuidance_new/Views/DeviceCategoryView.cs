using CustomLibrary.Panels;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class DeviceCategoryView: CustomContentPanel {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        // DataGridView panel
        private DataGridViewGroup<DeviceCategoryVO> _operationDataGridView;
        // Add new pop up form
        private AddNewPopUpForm _addNewPopUpForm;
        #endregion

        #region Constructors
        public DeviceCategoryView() {
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
                        name = "力臂 - after searching",
                        description = "这个类型就叫力臂",
                        bool_can_manipulate = false,
                    }
                };
            };
            _operationDataGridView.AddTextBox("设备类型名称", false, (DeviceCategoryVO vo, string? value) => vo.name = value);
            _operationDataGridView.AddTextBox("设备类型描述", false, (DeviceCategoryVO vo, string? value) => vo.description = value);
            _operationDataGridView.AddComboBox("是否运行手动控制", false, (DeviceCategoryVO vo, bool? value) => vo.bool_can_manipulate = value, new() { {"是", true}, {"否", false}});

            // Initialization
            InitializeGridView();
        }
        #endregion

        #region Initialize methods
        private void InitializeGridView() {
            List<DeviceCategoryVO> vos = new() {
                new() {
                    id = 1,
                    name = "控制器（工具）",
                    description = "这个类型是控制器/工具/螺丝枪",
                    bool_can_manipulate = false,
                },
                new() {
                    id = 2,
                    name = "力臂",
                    description = "这个类型就叫力臂",
                    bool_can_manipulate = false,
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
