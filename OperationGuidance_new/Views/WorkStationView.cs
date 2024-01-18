using CustomLibrary.Buttons;
using CustomLibrary.Panels;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Utils;
using CustomLibrary.Configs;
using OperationGuidance_service.Models.DTOs;
using CustomLibrary.TextBoxes;
using OperationGuidance_service.Models.Responses;
using CustomLibrary.Utils;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.Views {
    public class WorkStationView: CustomDataGridViewOuterPanel<WorkstationDTO, WorkstationVO> {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        private List<WorkstationDTO> _dataDTOList;
        // DataGridView panel
        private DataGridViewGroup<WorkstationVO> _dataGridView;
        // Add new pop up form
        private EditEntityPopUpForm<WorkstationDTO> _editEntityPopUpForm;
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
            _dataGridView = new() {
                Parent = this,
            };
            _dataGridView.AddTextBox("站点名称", false, (WorkstationVO vo, string? value) => vo.name = value);
            _dataGridView.AddTextBox("工具名称", false, (WorkstationVO vo, string? value) => vo.tool_name = value);
            CustomComboBoxGroup<int?> toolModelOptions = _dataGridView.AddComboBox("工具型号", (WorkstationVO vo, int? value) => vo.tool_device_model_id = value, new() {});
            _dataGridView.AddTextBox("力臂名称", false, (WorkstationVO vo, string? value) => vo.arm_name = value);
            CustomComboBoxGroup<int?> armModelOptions = _dataGridView.AddComboBox("力臂型号", (WorkstationVO vo, int? value) => vo.arm_device_model_id = value, new() {});

            // 工具型号和力臂型号的选项完善
            QueryDeviceModelListRsp queryDeviceModelListRsp = apis.QueryDeviceModelList(new() {
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

            // 按钮逻辑
            _dataGridView.QueryData = (vo) => {
                List<WorkstationVO> vos = QueryList();
                return vos
                    .Where(o => vo.name == null || o.name != null && o.name.Contains(vo.name))
                    .Where(o => vo.tool_name == null || o.tool_name != null && o.tool_name.Contains(vo.tool_name))
                    .Where(o => vo.tool_device_model_id == null || o.tool_device_model_id != null && o.tool_device_model_id == vo.tool_device_model_id)
                    .Where(o => vo.arm_name == null || o.arm_name != null && o.arm_name.Contains(vo.arm_name))
                    .Where(o => vo.arm_device_model_id == null || o.arm_device_model_id != null && o.arm_device_model_id == vo.arm_device_model_id)
                    .ToList();
            };
            _dataGridView.AddNewClick = (action) => {
                WorkstationDTO dto = new();
                OpenEditEntityPopUpForm("新增站点", dto, action);
            };
            _dataGridView.ModifyClick = (ids, action) => {
                if (ids.Count <= 0) {
                    WidgetUtils.ShowNoticePopUp("请选择要编辑的数据。");
                } else if (ids.Count > 1) {
                    WidgetUtils.ShowNoticePopUp("只能选择一条数据进行修改操作。");
                } else {
                    if (_dataDTOList.Count > 0) {
                        WorkstationDTO dto = _dataDTOList.Single(dto => dto.id == ids[0]);
                        OpenEditEntityPopUpForm("更新站点信息", dto, action);
                        // 更新后再触发一次查询操作
                        action();
                    }
                }
            };
            _dataGridView.DeleteClick = (ids, action) => {
                // 删除选择的数据
                Delete(ids);
                // 删除后再触发一次查询操作
                action();
            };
        }
        #endregion

        #region Reusable methods
        private void OpenEditEntityPopUpForm(string title, WorkstationDTO dto, Action callBackAction) {
            _editEntityPopUpForm = new(dto) {
                Title = title,
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            QueryDeviceListRsp deviceRsp1 = apis.QueryDeviceList(new());
            List<DeviceDTO> deviceDTOs = deviceRsp1.DeviceDTOs;
            // 添加字段
            CustomTextBoxGroup stationName = _editEntityPopUpForm.AddTextBox("站点名称", false, 
                (WorkstationDTO dto, string? value) => dto.name = value ?? "");
            if (dto.name != null) {
                stationName.SetValue(0, dto.name);
            }
            ToggleButtonGroup toggleButtonGroup = _editEntityPopUpForm.AddToggleButton("是否启用", 
                    (WorkstationDTO dto, bool value) => dto.enabled = value ? (int) YesOrNo.YES : (int) YesOrNo.NO);
            if (dto.enabled != null) {
                toggleButtonGroup.Checked = dto.enabled == (int) YesOrNo.YES;
            } else {
                toggleButtonGroup.Checked = true;
            }
            // 工具部分
            SubPanel<WorkstationDTO> toolSubPanel = _editEntityPopUpForm.AddSubPanel("工具");
            // 工具选择
            ToggleButton toolToggle = toolSubPanel.TitlePanel.AddRightButton<ToggleButton>();
            Dictionary<string, int> toolIds = deviceDTOs.Where(dto => dto.category_id == 1).ToDictionary(dto => CommonUtils.CannotBeNull(dto.name), dto => dto.id);
            CustomComboBoxGroup<int> toolOptions = toolSubPanel.AddComboBox("请选择工具", (WorkstationDTO dto, int value) => dto.tool_id = value, toolIds);
            toolSubPanel.TablePanel.SetColumnSpan(toolOptions, 2);
            // 工具名称
            CustomTextBoxGroup toolNameTextBox = toolSubPanel.AddTextBox("工具名称", false, 
                (WorkstationDTO dto, string? value) => dto.tool_name = value ?? "");
            toolNameTextBox.Enabled = false;
            // 工具型号
            QueryDeviceModelListRsp deviceModelRsp = apis.QueryDeviceModelList(new());
            List<DeviceModelDTO> deviceModelDTOs = deviceModelRsp.DeviceModelDTOs;
            Dictionary<string, int> toolModelOptions = deviceModelDTOs.Where(dto => dto.category_id == 1).ToDictionary(dto => CommonUtils.CannotBeNull(dto.name), dto => dto.id);
            CustomComboBoxGroup<int> toolModelNameTextBox = toolSubPanel.AddComboBox("工具型号", 
                (WorkstationDTO dto, int value) => dto.tool_device_model_id = value, toolModelOptions);
            toolModelNameTextBox.Enabled = false;
            // 工具IP
            CustomTextBoxGroup toolIPTextBox = toolSubPanel.AddTextBox("工具IP", false, 
                (WorkstationDTO dto, string? value) => dto.tool_ip = value ?? "");
            toolIPTextBox.Enabled = false;
            // 工具端口
            CustomTextBoxGroup toolPortTextBox = toolSubPanel.AddTextBox("工具端口", false, 
                (WorkstationDTO dto, int? value) => dto.tool_port = value ?? 0);
            toolPortTextBox.Enabled = false;
            if (dto.tool_id != null) {
                toolOptions.SetCurrent(toolOptions.IndexOf(dto.tool_id.Value));
                SetToolValues();
                toolToggle.Checked = true;
            } else {
                toolSubPanel.TablePanel.Hide();
                ResetToolValues();
            }
            // 工具选择事件：选择后自动填入
            toolOptions.ItemSelected += () => {
                if (!toolOptions.IsDefaultValue()) {
                    SetToolValues();
                } else {
                    ResetToolValues();
                }
            };
            void SetToolValues() {
                DeviceDTO deviceDTO = deviceDTOs.Single(dto => dto.id == toolOptions.Value);
                DeviceModelDTO deviceModelDTO = deviceModelDTOs.Single(dto => dto.id == deviceDTO.model_id);
                toolNameTextBox.SetValue(0, deviceDTO.name);
                toolModelNameTextBox.SetCurrent(toolModelNameTextBox.IndexOf(deviceModelDTO.id));
                toolIPTextBox.SetValue(0, deviceDTO.ip);
                toolPortTextBox.SetValue(0, deviceDTO.port + "");
            }
            void ResetToolValues() {
                toolNameTextBox.SetValue(0, null);
                toolModelNameTextBox.Reset();
                toolIPTextBox.SetValue(0, null);
                toolPortTextBox.SetValue(0, null);
            }
            // 是否显示工具开关事件
            int toolChosenIndexCache = -1;
            toolToggle.CheckedChanged += (sender, eventArgs) => {
                if (toolToggle.Checked) {
                    toolSubPanel.TablePanel.Show();
                    toolOptions.SetCurrent(toolChosenIndexCache);
                } else {
                    toolSubPanel.TablePanel.Hide();
                    toolChosenIndexCache = toolOptions.GetCurrentIndex();
                    toolOptions.Reset();
                }
                ResizePopUpForm();
                toolNameTextBox.ResizeChildren();
                toolModelNameTextBox.ResizeChildren();
                toolIPTextBox.ResizeChildren();
                toolPortTextBox.ResizeChildren();
                toolOptions.ResizeChildren();
            };
            // 力臂部分
            SubPanel<WorkstationDTO> armSubPanel = _editEntityPopUpForm.AddSubPanel("力臂");
            // 力臂选择
            ToggleButton armToggle = armSubPanel.TitlePanel.AddRightButton<ToggleButton>();
            Dictionary<string, int> armIds = deviceDTOs.Where(dto => dto.category_id == 2).ToDictionary(dto => CommonUtils.CannotBeNull(dto.name), dto => dto.id);
            CustomComboBoxGroup<int> armOptions = armSubPanel.AddComboBox<int>("请选择力臂", (WorkstationDTO dto, int value) => dto.arm_id = value, armIds);
            armSubPanel.TablePanel.SetColumnSpan(armOptions, 2);
            // 力臂名称
            CustomTextBoxGroup armNameTextBox = armSubPanel.AddTextBox("力臂名称", false, 
                (WorkstationDTO dto, string? value) => dto.arm_name = value == null ? "" : value);
            armNameTextBox.Enabled = false;
            // 力臂型号
            Dictionary<string, int> armModelOptions = deviceModelDTOs.Where(dto => dto.category_id == 2).ToDictionary(dto => CommonUtils.CannotBeNull(dto.name), dto => dto.id);
            CustomComboBoxGroup<int> armModelNameTextBox = armSubPanel.AddComboBox("力臂型号", 
                (WorkstationDTO dto, int value) => dto.arm_device_model_id = value, armModelOptions);
            armModelNameTextBox.Enabled = false;
            // 力臂IP
            CustomTextBoxGroup armIPTextBox = armSubPanel.AddTextBox("力臂IP", false, 
                (WorkstationDTO dto, string? value) => dto.arm_ip = value == null ? "" : value);
            armIPTextBox.Enabled = false;
            // 力臂端口
            CustomTextBoxGroup armPortTextBox = armSubPanel.AddTextBox("力臂端口", false, 
                (WorkstationDTO dto, int? value) => dto.arm_port = value == null ? 0 : value);
            armPortTextBox.Enabled = false;
            if (dto.arm_id != null) {
                armOptions.SetCurrent(armOptions.IndexOf(dto.arm_id.Value));
                SetArmValues();
                armToggle.Checked = true;
            } else {
                armSubPanel.TablePanel.Hide();
                ResetArmValues();
            }
            // 力臂选择事件：选择后自动填入
            armOptions.ItemSelected += () => {
                if (!armOptions.IsDefaultValue()) {
                    SetArmValues();
                } else {
                    ResetArmValues();
                }
            };
            void SetArmValues() {
                DeviceDTO deviceDTO = deviceDTOs.Single(dto => dto.id == armOptions.Value);
                DeviceModelDTO deviceModelDTO = deviceModelDTOs.Single(dto => dto.id == deviceDTO.model_id);
                armNameTextBox.SetValue(0, deviceDTO.name);
                armModelNameTextBox.SetCurrent(armModelNameTextBox.IndexOf(deviceModelDTO.id));
                armIPTextBox.SetValue(0, deviceDTO.ip);
                armPortTextBox.SetValue(0, deviceDTO.port + "");
            }
            void ResetArmValues() {
                armNameTextBox.SetValue(0, null);
                armModelNameTextBox.Reset();
                armIPTextBox.SetValue(0, null);
                armPortTextBox.SetValue(0, null);
            }
            // 是否显示力臂开关事件
            int armChosenIndexCache = -1;
            armToggle.CheckedChanged += (sender, eventArgs) => {
                if (armToggle.Checked) {
                    armSubPanel.TablePanel.Show();
                    armOptions.SetCurrent(armChosenIndexCache);
                } else {
                    armSubPanel.TablePanel.Hide();
                    armChosenIndexCache = armOptions.GetCurrentIndex();
                    armOptions.Reset();
                }
                ResizePopUpForm();
                armNameTextBox.ResizeChildren();
                armModelNameTextBox.ResizeChildren();
                armIPTextBox.ResizeChildren();
                armPortTextBox.ResizeChildren();
                armOptions.ResizeChildren();
            };

            // 添加按钮
            CommonButton confirmButton = _editEntityPopUpForm.AddButton("保存");
            confirmButton.Click += (s, e) => {
                callBackAction += _editEntityPopUpForm.DisposeForm;
                AddOrUpdate(dto, callBackAction);
            };
            CommonButton cancelButton = _editEntityPopUpForm.AddButton("取消");
            cancelButton.Click += (s, e) => {
                _editEntityPopUpForm.DisposeForm();
            };
            // Show form but make it transparent to create handles for its children
            _editEntityPopUpForm.PretendToShowToCreateHandlesForChildren();
            // Resize all widgets
            ResizePopUpForm();
            // Real show
            _editEntityPopUpForm.Show();
        }
        private void ResizePopUpForm() {
            if (_editEntityPopUpForm != null) {
                _editEntityPopUpForm.ResizeTablePanelAndItsChildren();
                _editEntityPopUpForm.Invalidate();
            }
        }
        #endregion

        #region Override methods
        protected override List<WorkstationVO> QueryList() {
            QueryWorkstationListRsp rsp = apis.QueryWorkstationList(new() {
                UserId = SystemUtils.LoggedUserId(),
            });
            _dataDTOList = rsp.WorkstationsDTOs;
            List<WorkstationVO> workstationVOs = new();
            CommonUtils.ObjectConverter<WorkstationDTO, WorkstationVO>(_dataDTOList, workstationVOs);
            // TODO: can use BackgroundWorker to do this
            // 后续再优化数据加载时的延迟、卡顿问题，现在先不管
            // for (int i = 0; i < 5000; i++) {
            //     workstationVOs.Add(workstationVOs[0]);
            // }
            return workstationVOs;
        }
        protected override void AddOrUpdate(WorkstationDTO dto, Action action) {
            AddOrUpdateWorkstationRsp rsp = apis.AddOrUpdateWorkstation(new(dto));
            if (rsp.RsponseCode == HttpResponseCode.OK) {
                WidgetUtils.ShowNoticePopUp("保存成功！");
            } else {
                WidgetUtils.ShowErrorPopUp($"保存失败！错误信息：{rsp.RsponseMessage}");
            }
            action();
        }
        protected override void Delete(List<int> ids) {
            if (ids.Count <= 0) {
                WidgetUtils.ShowNoticePopUp("请选择要删除的数据。");
            } else if (WidgetUtils.ShowConfirmPopUp($"确认要删除已选择的{ids.Count}条数据吗？")) {
                DeleteWorkstationByIdsRsp rsp = apis.DeleteWorkstation(new(ids));
                if (rsp.RsponseCode == HttpResponseCode.OK) {
                    WidgetUtils.ShowNoticePopUp($"成功删除{ids.Count}条数据！");
                } else {
                    WidgetUtils.ShowErrorPopUp($"删除失败！错误信息：{rsp.RsponseMessage}");
                }
            }
        }
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            _dataGridView.DataSource = QueryList();
        }
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            Size contentSize = new(Width - Padding.Size.Width, Height - Padding.Size.Height);
            _dataGridView.Size = contentSize;
        }
        public override void VisibleToTrue() {
            base.VisibleToTrue();
        }
        #endregion
    }
}
