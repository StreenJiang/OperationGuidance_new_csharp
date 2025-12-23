using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.ComboBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Constants;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;
using CustomLibrary.TextBoxes;
using CustomLibrary.Panels.AbstractClasses;

namespace OperationGuidance_new.Views
{
    public class DeviceArmView: ACustomDataGridViewOuterPanel<DeviceArmDTO, ViewObjects.DeviceArmVO> {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        private List<DeviceArmDTO> _dataDTOList;
        // DataGridView panel
        private DataGridViewGroup<DeviceArmVO> _dataGridView;
        // Add new pop up form
        private EditEntityPopUpForm<DeviceArmDTO> _editEntityPopUpForm;
        #endregion

        #region Constructors
        public DeviceArmView() {
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
            _dataGridView.AddTextBox("力臂名称", false, (DeviceArmVO vo, string? value) => vo.name = value);
            _dataGridView.AddTextBox("力臂描述", false, (DeviceArmVO vo, string? value) => vo.description = value);
            CustomComboBoxGroup<int?> toolTypeComboBox = _dataGridView.AddComboBox("力臂类型", (DeviceArmVO vo, int? value) => vo.type = value, new());
            foreach (DeviceTypeBase type in DeviceType_Arm.Elements) {
                toolTypeComboBox.AddItem(type.Name, type.Id);
            }
            
            // 按钮逻辑
            _dataGridView.QueryData = (vo) => {
                List<DeviceArmVO> workstationVOs = QueryList();
                return workstationVOs
                    .Where(o => vo.name == null || o.name != null && o.name.Contains(vo.name))
                    .Where(o => vo.description == null || o.description != null && o.description.Contains(vo.description))
                    .Where(o => vo.type == null || vo.type.Value == 0 || o.type != null && o.type == vo.type)
                    .ToList();
            };
            _dataGridView.AddNewClick = (action) => {
                DeviceArmDTO dto = new();
                OpenEditEntityPopUpForm("新增力臂", dto, action);
            };
            _dataGridView.ModifyClick = (ids, action) => {
                if (ids.Count <= 0) {
                    WidgetUtils.ShowNoticePopUp("请选择要编辑的数据。");
                } else if (ids.Count > 1) {
                    WidgetUtils.ShowNoticePopUp("只能选择一条数据进行编辑操作。");
                } else {
                    if (_dataDTOList.Count > 0) {
                        DeviceArmDTO dto = _dataDTOList.Single(dto => dto.id == ids[0]);
                        OpenEditEntityPopUpForm("更新力臂信息", dto, action);
                    }
                }
            };
            _dataGridView.DeleteClick = (ids, action) => {
                // 删除选择的数据
                Delete(ids);
                // 删除后再触发一次查询操作
                action();
            };

            // _dataGridView.AddExtraButton("导出");
            // _dataGridView.AddExtraButton("导入");
        }
        #endregion

        #region Reusable methods
        private void OpenEditEntityPopUpForm(string title, DeviceArmDTO dto, Action callBackAction) {
            _editEntityPopUpForm = new(dto) {
                Title = title,
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            // 添加字段
            CustomTextBoxGroup name = _editEntityPopUpForm.AddTextBox("力臂名称", false, 
                (DeviceArmDTO dto, string? value) => dto.name = value ?? "");
            if (dto.name != null) {
                name.SetValue(0, dto.name);
            }
            name.GetTextBox(0).TextChanged += (sender, eventArgs) => {
                name.GetTextBox(0).IsError = string.IsNullOrEmpty(name.GetTextBox(0).Box.Text);
            };
            CustomTextBoxGroup description = _editEntityPopUpForm.AddTextBox("力臂描述", false, 
                (DeviceArmDTO dto, string? value) => dto.description = value ?? "");
            if (dto.description != null) {
                description.SetValue(0, dto.description);
            }
            CustomTextBoxGroup ip = _editEntityPopUpForm.AddTextBox("IP地址", false, 
                (DeviceArmDTO dto, string? value) => dto.ip = value ?? "");
            CustomTextBox ipBox = ip.GetTextBox(0);
            ipBox.TextChanged += async (sender, eventArgs) => {
                while (ipBox.TimerTicking) {
                    await Task.Delay(100);
                }
                ipBox.IsError = !ArgumentValidator.ValidateIPv4(ipBox.Text);
            };
            if (dto.ip != null) {
                ip.SetValue(0, dto.ip);
            }
            CustomTextBoxGroup port = _editEntityPopUpForm.AddTextBox("端口号", false, 
                (DeviceArmDTO dto, int? value) => dto.port = value ?? 0);
            CustomTextBox portBox = port.GetTextBox(0);
            portBox.PositiveIntOnly = true;
            portBox.TextChanged += async (sender, eventArgs) => {
                while (portBox.TimerTicking) {
                    await Task.Delay(100);
                }
                portBox.IsError = !ArgumentValidator.ValidatePortInWindows(portBox.Text);
            };
            if (dto.port > 0) {
                port.SetValue(0, dto.port + "");
            }
            Dictionary<string, int> armTypes = DeviceType_Arm.Elements.ToDictionary(e => e.Name, e => e.Id);
            CustomComboBoxGroup<int> type = _editEntityPopUpForm.AddComboBox("力臂类型", 
                (DeviceArmDTO dto, int value) => dto.type = value, armTypes);
            type.SetCurrent(type.IndexOf(dto.type));
            type.ItemSelected += () => {
                type.SetError(type.IsDefaultValue());
            };

            // 添加按钮
            CommonButton confirmButton = _editEntityPopUpForm.AddButton("保存");
            confirmButton.Click += (s, e) => {
                bool check = true;
                string warningMsg = "";
                int warningIndex = 1;
                List<DeviceArmDTO> allData = apis.QueryDeviceArmList(new(SystemUtils.MacAddressesDTO.id)).DeviceArmDTOs;
                if (string.IsNullOrEmpty(name.GetTextBox(0).Box.Text)) {
                    check = false;
                    name.GetTextBox(0).IsError = true;
                    warningMsg += $"{warningIndex++}. 力臂名称不能为空\r\n";
                }
                if (allData.Exists(d => d.id != dto.id && d.name == dto.name)) {
                    check = false;
                    name.GetTextBox(0).IsError = true;
                    warningMsg += $"{warningIndex++}. 力臂名称已存在\r\n";
                }
                if (string.IsNullOrEmpty(ipBox.Box.Text)) {
                    check = false;
                    ipBox.IsError = true;
                    warningMsg += $"{warningIndex++}. IP地址不能为空\r\n";
                } else if (ipBox.IsError) {
                    check = false;
                    warningMsg += $"{warningIndex++}. IP地址格式错误\r\n";
                }
                if (string.IsNullOrEmpty(portBox.Box.Text)) {
                    check = false;
                    portBox.IsError = true;
                    warningMsg += $"{warningIndex++}. 端口号不能为空\r\n";
                } else if (portBox.IsError) {
                    check = false;
                    warningMsg += $"{warningIndex++}. 端口设置出错\r\n";
                }
                if (allData.Exists(d => d.id != dto.id && d.ip == dto.ip && d.port == dto.port)) {
                    check = false;
                    ipBox.IsError = true;
                    portBox.IsError = true;
                    warningMsg += $"{warningIndex++}. 此IP地址及对应端口已存在，不可重复配置\r\n";
                }
                if (type.IsDefaultValue()) {
                    type.SetError(true);
                    check = false;
                    warningMsg += $"{warningIndex++}. 没有选择力臂类型\r\n";
                }
                if (!check) {
                    WidgetUtils.ShowWarningPopUp($"保存失败：\r\n{warningMsg}");
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
        protected override List<DeviceArmVO> QueryList() {
            QueryDeviceArmListRsp rsp = apis.QueryDeviceArmList(new(SystemUtils.MacAddressesDTO.id));
            _dataDTOList = rsp.DeviceArmDTOs.Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
            List<DeviceArmVO> vos = new();
            CommonUtils.ObjectConverter<DeviceArmDTO, DeviceArmVO>(_dataDTOList, vos);
            // TODO: can use BackgroundWorker to do this
            // 后续再优化数据加载时的延迟、卡顿问题，现在先不管
            // for (int i = 0; i < 5000; i++) {
            //     workstationVOs.Add(workstationVOs[0]);
            // }
            return vos;
        }
        protected override void AddOrUpdate(DeviceArmDTO dto, Action action) {
            AddOrUpdateDeviceArmRsp rsp = apis.AddOrUpdateDeviceArm(new(dto));
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
                DeleteDeviceArmByIdsRsp rsp = apis.DeleteDeviceArm(new(ids));
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
