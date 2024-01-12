using CustomLibrary.Buttons;
using CustomLibrary.Events;
using CustomLibrary.Forms;
using CustomLibrary.Panels;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Utils;
using CustomLibrary.Configs;

namespace OperationGuidance_new.Views {
    public class WorkStationView: CustomContentPanel {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        // DataGridView panel
        private DataGridViewGroup<WorkstationVO> _workstationGridView;
        // Add new pop up form
        private AddNewPopUpForm _addNewPopUpForm;
        #endregion

        #region Constructors
        public WorkStationView() {
            // Default values
            FlowDirection = FlowDirection.TopDown;
            
            // Get Apis
            apis = SystemUtils.GetApis();

            // Initialize grid view
            _workstationGridView = new() {
                Parent = this,
            };
            _workstationGridView.AddTextBox("站点名称", false, (WorkstationVO vo, string? value) => vo.name = value);
            _workstationGridView.AddTextBox("工具名称", false, (WorkstationVO vo, string? value) => vo.tool_name = value);
            _workstationGridView.AddComboBox("工具型号", false, (WorkstationVO vo, int? value) => vo.tool_device_model_id = value, new() {});
            _workstationGridView.AddTextBox("力臂名称", false, (WorkstationVO vo, string? value) => vo.arm_name = value);
            _workstationGridView.AddComboBox("力臂型号", false, (WorkstationVO vo, int? value) => vo.arm_device_model_id = value, new() {{"测试1", 10}, {"测试2", 20}, {"测试3", 30}});
            _workstationGridView.QueryData = (vo) => {
                return new() {
                    new() {
                        id = 111,
                        name = "ddafsdfa",
                    }
                };
            };
            _workstationGridView.AddNewClick = OpenAddNewPopUpForm;

            // Initialization
            InitializeGridView();
        }
        #endregion

        #region Initialize methods
        private void InitializeGridView() {
            List<WorkstationVO> vos = new() {
                new() {
                    id = 1,
                    name = "test",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.YES,
                },
                new() {
                    id = 1,
                    name = "test",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.YES,
                },
                new() {
                    id = 2,
                    name = "test22",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.NO,
                },
                new() {
                    id = 3,
                    name = "test3",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.YES,
                },
                new() {
                    id = 4,
                    name = "test4",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.YES,
                },
                new() {
                    id = 1,
                    name = "test",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.YES,
                },
                new() {
                    id = 2,
                    name = "test22",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.NO,
                },
                new() {
                    id = 3,
                    name = "test3",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.YES,
                },
                new() {
                    id = 4,
                    name = "test4",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.YES,
                },
                new() {
                    id = 1,
                    name = "test",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.YES,
                },
                new() {
                    id = 2,
                    name = "test22",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.NO,
                },
                new() {
                    id = 3,
                    name = "test3",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.YES,
                },
                new() {
                    id = 4,
                    name = "test4",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.YES,
                },
                new() {
                    id = 1,
                    name = "test",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.YES,
                },
                new() {
                    id = 2,
                    name = "test22",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.NO,
                },
                new() {
                    id = 3,
                    name = "test3",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.YES,
                },
                new() {
                    id = 4,
                    name = "test4",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.YES,
                },
                new() {
                    id = 1,
                    name = "test",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.YES,
                },
                new() {
                    id = 2,
                    name = "test22",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.NO,
                },
                new() {
                    id = 3,
                    name = "test3",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.YES,
                },
                new() {
                    id = 4,
                    name = "test4",
                    tool_name = "aaaagasdfasfasdfasdfasfasdf",
                    arm_name = "asdfasgasgsadfsadfsadfasdfasdf",
                    enabled = (int) YesOrNo.YES,
                },
            };
            _workstationGridView.DataSource = vos;
        }
        #endregion

        #region Reusable methods
        private void OpenAddNewPopUpForm(Action callBackAction) {
            _addNewPopUpForm = new() {
                Title = "新增站点",
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            // 添加按钮
            CommonButton confirmButton = _addNewPopUpForm.AddButton("保存");
            confirmButton.Click += (s, e) => {
                callBackAction();
                _addNewPopUpForm.HideForm();
            };
            CommonButton cancelButton = _addNewPopUpForm.AddButton("取消");
            cancelButton.Click += (s, e) => {
                _addNewPopUpForm.HideForm();
            };
            // Show form but make it transparent to create handles for its children
            _addNewPopUpForm.PretendToShowToCreateHandlesForChildren();
            // Resize all widgets
            ResizePopUpForm();
            // Real show
            _addNewPopUpForm.Show();
            // Set current pop up form
            EventFuncs.CurrentPopUpForm = _addNewPopUpForm;
        }
        private void ResizePopUpForm() {
            if (_addNewPopUpForm != null) {
                _addNewPopUpForm.CalculateDetailProperties();

                // Control mainPanel = WidgetUtils.MainPanel;
                // TableLayoutPanel tablePanel = _addNewPopUpForm.TablePanel;
                // Padding contentPadding = _addNewPopUpForm.ContentPanel.Padding;
                // int boxHeight = WidgetUtils.TextOrComboBoxHeight();
                // int boxMargin = boxHeight / 5;
                // int tableHeight = tablePanel.Controls.Count / tablePanel.ColumnCount * (boxHeight + boxMargin * 2);
                // Size contentSize = new((int) (mainPanel.Width * .75), tableHeight + contentPadding.Size.Height);
                // int tableWidth = contentSize.Width - contentPadding.Size.Width;
                // _addNewPopUpForm.BoxHeight = boxHeight;
                // _addNewPopUpForm.BoxMargin = boxMargin;
                // _addNewPopUpForm.TablePanel.Size = new(tableWidth, tableHeight);
                //
                // _addNewPopUpForm.SetContentSizeAndSelfSize(contentSize);
                if (_addNewPopUpForm.Visible) {
                    _addNewPopUpForm.Invalidate();
                }
            }
        }
        #endregion

        #region Override methods
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            Size contentSize = new(Width - Padding.Size.Width, Height - Padding.Size.Height);
            _workstationGridView.Size = contentSize;
        }
        public override void VisibleToTrue() {
            base.VisibleToTrue();
        }
        #endregion
    }

    public class AddNewPopUpForm: CustomPopUpForm {
        #region Fields
        // private 
        #endregion
    }
}
