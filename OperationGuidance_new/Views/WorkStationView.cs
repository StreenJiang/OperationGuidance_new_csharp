using CustomLibrary.Buttons;
using CustomLibrary.Events;
using CustomLibrary.Forms;
using CustomLibrary.Panels;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Utils;
using CustomLibrary.Configs;
using OperationGuidance_service.Models.DTOs;
using CustomLibrary.TextBoxes;
using OperationGuidance_service.Models.Responses;

namespace OperationGuidance_new.Views {
    public class WorkStationView: CustomContentPanel {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        private List<WorkstationVO> _dataList;
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

            // Initialization
            InitializeGridView();
        }
        #endregion

        #region Initialize methods
        private void InitializeGridView() {
            _workstationGridView = new() {
                Parent = this,
            };
            _workstationGridView.AddTextBox("站点名称", false, (WorkstationVO vo, string? value) => vo.name = value);
            _workstationGridView.AddTextBox("工具名称", false, (WorkstationVO vo, string? value) => vo.tool_name = value);
            CustomComboBoxGroup<int?> toolModelOptions = _workstationGridView.AddComboBox("工具型号", false, (WorkstationVO vo, int? value) => vo.tool_device_model_id = value, new() {});
            _workstationGridView.AddTextBox("力臂名称", false, (WorkstationVO vo, string? value) => vo.arm_name = value);
            CustomComboBoxGroup<int?> armModelOptions = _workstationGridView.AddComboBox("力臂型号", false, (WorkstationVO vo, int? value) => vo.arm_device_model_id = value, new() {});

            // 工具型号和力臂型号的选项完善
            QueryDeviceModelListRsp queryDeviceModelListRsp = apis.queryDeviceModel(new() {
                UserId = SystemUtils.LoggedUserId(),
            });
            List<DeviceModelDTO> deviceModelDTOs = queryDeviceModelListRsp.DeviceModelDTOs;
            deviceModelDTOs.Where(dto => dto.id == 1).ToList().ForEach(dto => {
                if (dto.name != null) {
                    toolModelOptions.AddItem(dto.name, dto.id);
                }
            });
            deviceModelDTOs.Where(dto => dto.id == 2).ToList().ForEach(dto => {
                if (dto.name != null) {
                    armModelOptions.AddItem(dto.name, dto.id);
                }
            });

            // 查询按钮逻辑
            _workstationGridView.QueryData = (vo) => {
                List<WorkstationVO> workstationVOs = QueryDataList();
                return workstationVOs
                    .Where(o => vo.name == null || o.name != null && o.name.Contains(vo.name))
                    .Where(o => vo.tool_name == null || o.tool_name != null && o.tool_name.Contains(vo.tool_name))
                    .Where(o => vo.tool_device_model_id == null || o.tool_device_model_id != null && o.tool_device_model_id == vo.tool_device_model_id)
                    .Where(o => vo.arm_name == null || o.arm_name != null && o.arm_name.Contains(vo.arm_name))
                    .Where(o => vo.name == null || o.name != null && o.name.Contains(vo.name))
                    .ToList();
            };
            // _workstationGridView.AddNewClick = OpenAddNewPopUpForm;
        }
        #endregion

        #region Reusable methods
        private List<WorkstationVO> QueryDataList() {
            QueryWorkstationListRsp rsp = apis.QueryWorkstationList(new() {
                UserId = SystemUtils.LoggedUserId(),
            });
            List<WorkstationDTO> workstationsDTOs = rsp.WorkstationsDTOs;
            List<WorkstationVO> workstationVOs = new();
            CommonUtils.ObjectConverter<WorkstationDTO, WorkstationVO>(workstationsDTOs, workstationVOs);
            return workstationVOs;
        }
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
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            _workstationGridView.DataSource = QueryDataList();
        }
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
        private WorkstationDTO dto = new();
        private readonly int _tableColumnsCount = 2;
        // Work station panel
        private CustomContentPanel _workStationPanel;
        private TableLayoutPanel _workStationTablePanel;
        private CustomTextBoxGroup _workStationNameTextBox;
        // Tool panel
        private CustomContentPanel _toolPanel;
        private TitlePanel _toolTitlePanel;
        private TableLayoutPanel _toolTablePanel;
        // Arm panel
        private CustomContentPanel _armPanel;
        private TitlePanel _armTitlePanel;
        private TableLayoutPanel _armTablePanel;
        #endregion
    }
}
