using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Constants;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class DeviceToolView: CustomDataGridViewOuterPanel<DeviceToolDTO, ViewObjects.DeviceToolVO> {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        private List<DeviceToolDTO> _dataDTOList;
        // DataGridView panel
        private DataGridViewGroup<DeviceToolVO> _dataGridView;
        // Add new pop up form
        private EditEntityPopUpForm<DeviceToolDTO> _editEntityPopUpForm;
        #endregion

        #region Constructors
        public DeviceToolView() {
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
            _dataGridView.AddTextBox("工具名称", false, (DeviceToolVO vo, string? value) => vo.name = value);
            _dataGridView.AddTextBox("工具描述", false, (DeviceToolVO vo, string? value) => vo.description = value);
            CustomComboBoxGroup<int?> toolTypeComboBox = _dataGridView.AddComboBox("工具类型", (DeviceToolVO vo, int? value) => vo.type = value, new());
            foreach (DeviceTypeBase type in DeviceType_Tool.Elements) {
                toolTypeComboBox.AddItem(type.Name, type.Id);
            }
            
            // 按钮逻辑
            _dataGridView.QueryData = (vo) => {
                List<DeviceToolVO> workstationVOs = QueryList();
                return workstationVOs
                    .Where(o => vo.name == null || o.name != null && o.name.Contains(vo.name))
                    .Where(o => vo.description == null || o.description != null && o.description.Contains(vo.description))
                    .Where(o => vo.type == null || vo.type.Value == 0 || o.type != null && o.type == vo.type)
                    .ToList();
            };
            _dataGridView.AddNewClick = (action) => {
                DeviceToolDTO dto = new();
                OpenEditEntityPopUpForm("新增工具", dto, action);
            };
            _dataGridView.ModifyClick = (ids, action) => {
                if (ids.Count <= 0) {
                    WidgetUtils.ShowNoticePopUp("请选择要编辑的数据。");
                } else if (ids.Count > 1) {
                    WidgetUtils.ShowNoticePopUp("只能选择一条数据进行修改操作。");
                } else {
                    if (_dataDTOList.Count > 0) {
                        DeviceToolDTO dto = _dataDTOList.Single(dto => dto.id == ids[0]);
                        OpenEditEntityPopUpForm("更新工具信息", dto, action);
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
        private void OpenEditEntityPopUpForm(string title, DeviceToolDTO dto, Action callBackAction) {
            _editEntityPopUpForm = new(dto) {
                Title = title,
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            // 添加字段
            CustomTextBoxGroup brandName = _editEntityPopUpForm.AddTextBox("工具名称", false, 
                (DeviceToolDTO dto, string? value) => dto.name = value ?? "");
            brandName.Ratio = 6;
            if (dto.name != null) {
                brandName.SetValue(0, dto.name);
            }
            CustomTextBoxGroup description = _editEntityPopUpForm.AddTextBox("工具描述", false, 
                (DeviceToolDTO dto, string? value) => dto.description = value ?? "");
            description.Ratio = 6;
            if (dto.description != null) {
                description.SetValue(0, dto.description);
            }
            CustomTextBoxGroup ip = _editEntityPopUpForm.AddTextBox("IP地址", false, 
                (DeviceToolDTO dto, string? value) => dto.ip = value ?? "");
            CustomTextBox ipBox = ip.GetTextBox(0);
            ipBox.TextChanged += (sender, eventArgs) => {
                ipBox.IsError = !ArgumentValidator.ValidateIPv4(ipBox.Text);
            };
            ip.Ratio = 6;
            if (dto.description != null) {
                ip.SetValue(0, dto.ip);
            }
            CustomTextBoxGroup port = _editEntityPopUpForm.AddTextBox("端口号", false, 
                (DeviceToolDTO dto, int? value) => dto.port = value ?? 0);
            CustomTextBox portBox = port.GetTextBox(0);
            portBox.TextChanged += (sender, eventArgs) => {
                portBox.IsError = !ArgumentValidator.ValidatePortInWindows(portBox.Text);
            };
            port.Ratio = 6;
            if (dto.port != null) {
                port.SetValue(0, dto.port + "");
            }
            Dictionary<string, int> toolTypes = DeviceType_Tool.Elements.ToDictionary(e => e.Name, e => e.Id);
            CustomComboBoxGroup<int> type = _editEntityPopUpForm.AddComboBox("工具类型", 
                (DeviceToolDTO dto, int value) => dto.type = value, toolTypes);
            type.Ratio = 6;
            if (dto.type != null) {
                type.SetCurrent(type.IndexOf(dto.type.Value));
            }


            // 添加按钮
            CommonButton confirmButton = _editEntityPopUpForm.AddButton("保存");
            confirmButton.Click += (s, e) => {
                if (ipBox.IsError) {
                    WidgetUtils.ShowErrorPopUp("IP地址格式错误！");
                } else if (portBox.IsError) {
                    WidgetUtils.ShowErrorPopUp("端口设置出错！");
                } else {
                    AddOrUpdate(dto, callBackAction);
                    _editEntityPopUpForm.Hide();
                }
            };
            CommonButton cancelButton = _editEntityPopUpForm.AddButton("取消");
            cancelButton.Click += (s, e) => {
                _editEntityPopUpForm.Dispose();
            };
            // Show form but make it transparent to create handles for its children
            _editEntityPopUpForm.PretendToShowToCreateHandlesForChildren();
            // Resize all widgets
            ResizePopUpForm();
            // Real show
            _editEntityPopUpForm.Show();
            callBackAction += _editEntityPopUpForm.Dispose;
        }
        private void ResizePopUpForm() {
            if (_editEntityPopUpForm != null) {
                _editEntityPopUpForm.ResizeTablePanelAndItsChildren();
                _editEntityPopUpForm.Invalidate();
            }
        }
        #endregion

        #region Override methods
        protected override List<DeviceToolVO> QueryList() {
            QueryDeviceToolListRsp rsp = apis.QueryDeviceToolList(new() {
                UserId = SystemUtils.LoggedUserId(),
            });
            _dataDTOList = rsp.DeviceToolDTOs;
            List<DeviceToolVO> vos = new();
            CommonUtils.ObjectConverter<DeviceToolDTO, DeviceToolVO>(_dataDTOList, vos);
            // TODO: can use BackgroundWorker to do this
            // 后续再优化数据加载时的延迟、卡顿问题，现在先不管
            // for (int i = 0; i < 5000; i++) {
            //     workstationVOs.Add(workstationVOs[0]);
            // }
            return vos;
        }
        protected override void AddOrUpdate(DeviceToolDTO dto, Action action) {
            AddOrUpdateDeviceToolRsp rsp = apis.AddOrUpdateDeviceTool(new(dto));
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
                DeleteDeviceToolByIdsRsp rsp = apis.DeleteDeviceTool(new(ids));
                if (rsp.RsponseCode == HttpResponseCode.OK) {
                    WidgetUtils.ShowNoticePopUp($"成功删除{ids.Count}条数据！");
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
