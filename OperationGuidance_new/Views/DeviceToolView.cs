using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class DeviceToolView: CustomDataGridViewOuterPanel<DeviceDTO, DeviceVO> {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        private List<DeviceDTO> _dataDTOList;
        // DataGridView panel
        private DataGridViewGroup<DeviceVO> _dataGridView;
        // Add new pop up form
        private EditEntityPopUpForm<DeviceDTO> _editEntityPopUpForm;
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
            _dataGridView.AddTextBox("设备名称", false, (DeviceVO vo, string? value) => vo.name = value);
            _dataGridView.AddTextBox("设备描述", false, (DeviceVO vo, string? value) => vo.description = value);
            // 处理设备型号、品牌和类型的查询条件
            QueryDeviceModelListRsp queryDeviceModelListRsp = apis.QueryDeviceModelList(new());
            Dictionary<string, int> deviceModelIds = queryDeviceModelListRsp.DeviceModelDTOs.ToDictionary(dto => CommonUtils.CannotBeNull(dto.name), dto => dto.id);
            _dataGridView.AddComboBox("设备型号", (DeviceVO vo, int value) => vo.model_id = value, deviceModelIds);
            QueryDeviceCategoryListRsp queryDeviceCategoryListRsp = apis.QueryDeviceCategoryList(new());
            Dictionary<string, int> deviceCategoryIds = queryDeviceCategoryListRsp.DeviceCategoryDTOs.ToDictionary(dto => CommonUtils.CannotBeNull(dto.name), dto => dto.id);
            _dataGridView.AddComboBox("设备类型", (DeviceVO vo, int value) => vo.category_id = value, deviceCategoryIds);
            QueryBrandListRsp queryBrandListRsp = apis.QueryBrandList(new());
            Dictionary<string, int> brandIds = queryBrandListRsp.BrandDTOs.ToDictionary(dto => CommonUtils.CannotBeNull(dto.name), dto => dto.id);
            _dataGridView.AddComboBox("设备品牌", (DeviceVO vo, int value) => vo.brand_id = value, brandIds);

            // 按钮逻辑
            _dataGridView.QueryData = (vo) => {
                List<DeviceVO> vos = QueryList();
                return vos
                    .Where(o => vo.name == null || o.name != null && o.name.Contains(vo.name))
                    .Where(o => vo.description == null || o.description != null && o.description.Contains(vo.description))
                    .Where(o => vo.model_id == null || vo.model_id.Value == 0 || o.model_id != null && o.model_id == vo.model_id)
                    .Where(o => vo.category_id == null || vo.category_id.Value == 0 || o.category_id != null && o.category_id == vo.category_id)
                    .Where(o => vo.brand_id == null || vo.brand_id.Value == 0 || o.brand_id != null && o.brand_id == vo.brand_id)
                    .ToList();
            };
            _dataGridView.AddNewClick = (action) => {
                DeviceDTO dto = new();
                OpenEditEntityPopUpForm("新增设备", dto, action);
            };
            _dataGridView.ModifyClick = (ids, action) => {
                if (ids.Count <= 0) {
                    WidgetUtils.ShowNoticePopUp("请选择要编辑的数据。");
                } else if (ids.Count > 1) {
                    WidgetUtils.ShowNoticePopUp("只能选择一条数据进行修改操作。");
                } else {
                    if (_dataDTOList.Count > 0) {
                        DeviceDTO dto = _dataDTOList.Single(dto => dto.id == ids[0]);
                        OpenEditEntityPopUpForm("更新设备信息", dto, action);
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
        private void OpenEditEntityPopUpForm(string title, DeviceDTO dto, Action callBackAction) {
            _editEntityPopUpForm = new(dto) {
                Title = title,
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            // 添加字段
            CustomTextBoxGroup brandName = _editEntityPopUpForm.AddTextBox("设备名称", false, 
                (DeviceDTO dto, string? value) => dto.name = value ?? "");
            brandName.Ratio = 7;
            if (dto.name != null) {
                brandName.SetValue(0, dto.name);
            }
            CustomTextBoxGroup description = _editEntityPopUpForm.AddTextBox("设备描述", false, 
                (DeviceDTO dto, string? value) => dto.description = value ?? "");
            description.Ratio = 7;
            if (dto.description != null) {
                description.SetValue(0, dto.description);
            }
            // 处理设备型号、品牌和设备类型
            QueryDeviceModelListRsp deviceModelListRsp = apis.QueryDeviceModelList(new());
            List<DeviceModelDTO> deviceModelDTOs = deviceModelListRsp.DeviceModelDTOs;
            Dictionary<string, int> deviceModelIds = deviceModelDTOs.ToDictionary(dto => CommonUtils.CannotBeNull(dto.name), dto => dto.id);
            CustomComboBoxGroup<int> deviceModel = _editEntityPopUpForm.AddComboBox("设备型号", ((DeviceDTO dto, int value) => dto.model_id = value), deviceModelIds);
            QueryBrandListRsp brandListRsp = apis.QueryBrandList(new());
            Dictionary<string, int> brandIds = brandListRsp.BrandDTOs.ToDictionary(dto => CommonUtils.CannotBeNull(dto.name), dto => dto.id);
            CustomComboBoxGroup<int> brand = _editEntityPopUpForm.AddComboBox("设备品牌", ((DeviceDTO dto, int value) => dto.brand_id = value), brandIds);
            brand.Enabled = false;
            QueryDeviceCategoryListRsp deviceCategoryListRsp = apis.QueryDeviceCategoryList(new());
            Dictionary<string, int> deviceCategoryIds = deviceCategoryListRsp.DeviceCategoryDTOs.ToDictionary(dto => CommonUtils.CannotBeNull(dto.name), dto => dto.id);
            CustomComboBoxGroup<int> deviceCategory = _editEntityPopUpForm.AddComboBox("设备类型", ((DeviceDTO dto, int value) => dto.category_id = value), deviceCategoryIds);
            deviceCategory.Enabled = false;
            deviceModel.ItemSelected += () => {
                if (!deviceModel.IsDefaultValue()) {
                    DeviceModelDTO deviceModelDTO = deviceModelDTOs.Single(dto => dto.id == deviceModel.Value);
                    if (deviceModelDTO.brand_id != null) {
                        brand.SetCurrent(brand.IndexOf(deviceModelDTO.brand_id.Value));
                    }
                    if (deviceModelDTO.category_id != null) {
                        deviceCategory.SetCurrent(deviceCategory.IndexOf(deviceModelDTO.category_id.Value));
                    }
                } else {
                    brand.Reset();
                    deviceCategory.Reset();
                }
            };
            if (dto.model_id != null) {
                deviceModel.SetCurrent(deviceModel.IndexOf(dto.model_id.Value));
            }

            // 添加按钮
            CommonButton confirmButton = _editEntityPopUpForm.AddButton("保存");
            confirmButton.Click += (s, e) => {
                callBackAction += _editEntityPopUpForm.Dispose;
                AddOrUpdate(dto, callBackAction);
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
        }
        private void ResizePopUpForm() {
            if (_editEntityPopUpForm != null) {
                _editEntityPopUpForm.ResizeTablePanelAndItsChildren();
                _editEntityPopUpForm.Invalidate();
            }
        }
        #endregion

        #region Override methods
        protected override List<DeviceVO> QueryList() {
            QueryDeviceListRsp rsp = apis.QueryDeviceList(new() {
                UserId = SystemUtils.LoggedUserId(),
            });
            _dataDTOList = rsp.DeviceDTOs;
            List<DeviceVO> vos = new();
            CommonUtils.ObjectConverter<DeviceDTO, DeviceVO>(_dataDTOList, vos);

            // TODO: can use BackgroundWorker to do this
            // 后续再优化数据加载时的延迟、卡顿问题，现在先不管
            // for (int i = 0; i < 5000; i++) {
            //     workstationVOs.Add(workstationVOs[0]);
            // }
            return vos;
        }
        protected override void AddOrUpdate(DeviceDTO dto, Action action) {
            AddOrUpdateDeviceRsp rsp = apis.AddOrUpdateDevice(new(dto));
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
                DeleteDeviceByIdsRsp rsp = apis.DeleteDevice(new(ids));
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
