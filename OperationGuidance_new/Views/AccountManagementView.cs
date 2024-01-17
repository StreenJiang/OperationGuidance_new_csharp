using CustomLibrary.Panels;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class AccountManagementView: CustomContentPanel {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        // DataGridView panel
        private DataGridViewGroup<UserAccountInfoVO> _operationDataGridView;
        // Add new pop up form
        // private AddNewPopUpForm _addNewPopUpForm;
        #endregion

        #region Constructors
        public AccountManagementView() {
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
                        staff_id = 111,
                        name = "张三",
                        account = "account",
                        password = "password",
                    }
                };
            };
            _operationDataGridView.AddTextBox("账号", false, (UserAccountInfoVO vo, string? value) => vo.account = value);
            _operationDataGridView.AddTextBox("角色", false, (UserAccountInfoVO vo, string? value) => vo.position = value);

            // Initialization
            InitializeGridView();
        }
        #endregion

        #region Initialize methods
        private void InitializeGridView() {
            List<UserAccountInfoVO> vos = new() {
                new() {
                    id = 1,
                    staff_id = 1,
                    name = "张三",
                    account = "account",
                    password = "password",
                },
                new() {
                    id = 2,
                    staff_id = 2,
                    name = "李四",
                    account = "account22222",
                    password = "password222222",
                },
                new() {
                    id = 3,
                    staff_id = 3,
                    name = "王五",
                    account = "account333333",
                    password = "password33333333",
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
