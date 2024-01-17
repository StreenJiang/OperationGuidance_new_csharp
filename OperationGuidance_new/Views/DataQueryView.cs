using CustomLibrary.Buttons;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class DataQueryView: CustomContentPanel {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        // DataGridView panel
        private DataGridViewGroup<OperationDataVO> _operationDataGridView;
        // Add new pop up form
        // private EditEntityPopUpForm<> _addNewPopUpForm;
        #endregion

        #region Constructors
        public DataQueryView() {
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
                        id = 111,
                        procedure_set = 1,
                        tightened_status = "拧紧完成 - 测试数据 - 查询以后",
                    }
                };
            };
            CustomTextBoxGroup dateFitler = _operationDataGridView.AddSeparateTextBox("日期", "~", false, 
                    (OperationDataVO vo, DateTime? value) => vo.create_time = value, 
                    (OperationDataVO vo, DateTime? value) => vo.create_time = value);
            CommonButton commonButton = _operationDataGridView.AddExtraButton("导出");
            commonButton.Click += (sender, eventArgs) => {
                WidgetUtils.ShowNoticePopUp("Export button has not been set.");
            };
            _operationDataGridView.AddNewButtonVisible = false;
            _operationDataGridView.ModifyButtonVisible = false;
            _operationDataGridView.DeleteButtonVisible = false;

            // Initialization
            InitializeGridView();
        }
        #endregion

        #region Initialize methods
        private void InitializeGridView() {
            List<OperationDataVO> vos = new() {
                new() {
                    id = 1,
                    procedure_set = 1,
                    tightened_status = "拧紧完成",
                },
                new() {
                    id = 2,
                    procedure_set = 2,
                    tightened_status = "拧紧失败",
                },
                new() {
                    id = 3,
                    procedure_set = 3,
                    tightened_status = "拧紧完成",
                },
                new() {
                    id = 4,
                    procedure_set = 4,
                    tightened_status = "拧紧出错",
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
