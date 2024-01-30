using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.DataGridViewRelateds;
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
    public class DeviceCategoryView: CustomDataGridViewOuterPanel<DeviceCategoryDTO, DeviceCategoryVO> {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        private List<DeviceCategoryDTO> _dataDTOList;
        // DataGridView panel
        private DataGridViewGroup<DeviceCategoryVO> _dataGridView;
        // Add new pop up form
        private EditEntityPopUpForm<DeviceCategoryDTO> _editEntityPopUpForm;
        #endregion

        #region Constructors
        public DeviceCategoryView() {
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
            _dataGridView.AddTextBox("设备类型名称", false, (DeviceCategoryVO vo, string? value) => vo.name = value);
            _dataGridView.AddTextBox("设备类型描述", false, (DeviceCategoryVO vo, string? value) => vo.description = value);
            _dataGridView.AddComboBox("是否运行手动控制", (DeviceCategoryVO vo, int? value) => vo.can_manipulate = value, new() { { "是", (int) YesOrNo.YES }, { "否", (int) YesOrNo.NO } });
            
            // 按钮逻辑
            _dataGridView.QueryData = (vo) => {
                List<DeviceCategoryVO> workstationVOs = QueryList();
                return workstationVOs
                    .Where(o => vo.name == null || o.name != null && o.name.Contains(vo.name))
                    .Where(o => vo.description == null || o.description != null && o.description.Contains(vo.description))
                    .Where(o => vo.can_manipulate == null || vo.can_manipulate.Value == 0 || o.can_manipulate != null && o.can_manipulate == vo.can_manipulate)
                    .ToList();
            };
            _dataGridView.AddNewClick = (action) => {
                DeviceCategoryDTO dto = new();
                OpenEditEntityPopUpForm("新增设备类型", dto, action);
            };
            _dataGridView.ModifyClick = (ids, action) => {
                if (ids.Count <= 0) {
                    WidgetUtils.ShowNoticePopUp("请选择要编辑的数据。");
                } else if (ids.Count > 1) {
                    WidgetUtils.ShowNoticePopUp("只能选择一条数据进行修改操作。");
                } else {
                    if (_dataDTOList.Count > 0) {
                        DeviceCategoryDTO dto = _dataDTOList.Single(dto => dto.id == ids[0]);
                        OpenEditEntityPopUpForm("更新设备类型信息", dto, action);
                    }
                }
            };
            _dataGridView.DeleteClick = (ids, action) => {
                // 删除选择的数据
                Delete(ids);
                // 删除后再触发一次查询操作
                action();
            };

            // Toggle button check changed
            _dataGridView.VoGridView.GridView.CellValueChanged += (sender, eventArgs) => {
                if (sender != null && sender is DataGridView view) {
                    DataGridViewCell cell = view.Rows[eventArgs.RowIndex].Cells[eventArgs.ColumnIndex];
                    if (cell is DataGridViewToggleButtonCell tCell) {
                        DataGridViewRow row = view.Rows[eventArgs.RowIndex];
                        DeviceCategoryVO vo = (DeviceCategoryVO) row.DataBoundItem;
                        DeviceCategoryDTO dto = _dataDTOList.Single(dto => vo.id != null && dto.id == vo.id.Value);
                        bool value = (bool) cell.Value;
                        dto.can_manipulate = value ? (int) YesOrNo.YES : (int) YesOrNo.NO;
                        apis.AddOrUpdateDeviceCategory(new(dto));
                    }
                }
            };
        }
        #endregion

        #region Reusable methods
        private void OpenEditEntityPopUpForm(string title, DeviceCategoryDTO dto, Action callBackAction) {
            _editEntityPopUpForm = new(dto) {
                Title = title,
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            // 添加字段
            CustomTextBoxGroup brandName = _editEntityPopUpForm.AddTextBox("设备类型名称", false, 
                (DeviceCategoryDTO dto, string? value) => dto.name = value ?? "");
            brandName.Ratio = 6;
            if (dto.name != null) {
                brandName.SetValue(0, dto.name);
            }
            CustomTextBoxGroup description = _editEntityPopUpForm.AddTextBox("设备类型描述", false, 
                (DeviceCategoryDTO dto, string? value) => dto.description = value ?? "");
            description.Ratio = 6;
            if (dto.description != null) {
                description.SetValue(0, dto.description);
            }

            ToggleButtonGroup canManipulate = _editEntityPopUpForm.AddToggleButton("是否允许手动控制", 
                    (DeviceCategoryDTO dto, bool value) => dto.can_manipulate = value ? (int) YesOrNo.YES : (int) YesOrNo.NO);
            canManipulate.Ratio = 6;
            if (dto.can_manipulate != null) {
                canManipulate.Checked = dto.can_manipulate == (int) YesOrNo.YES;
            } else {
                canManipulate.Checked = true;
            }
            PictureBoxGroup iconNormal = _editEntityPopUpForm.AddPictureBox("状态正常图标", 
                (DeviceCategoryDTO dto, Image value) => dto.icon_normal = CommonUtils.ImageToBase64(value), 
                (DeviceCategoryDTO dto, string value) => dto.icon_normal_name = value
            );
            iconNormal.Ratio = 6;
            if (dto.icon_normal_name != null) {
                iconNormal.FileName = dto.icon_normal_name;
            }
            if (dto.icon_normal != null) {
                Image? image = CommonUtils.ImageBase64ToImage(dto.icon_normal);
                if (image != null) {
                    iconNormal.Image = image;
                }
            }
            PictureBoxGroup iconError = _editEntityPopUpForm.AddPictureBox("状态错误图标", 
                (DeviceCategoryDTO dto, Image value) => dto.icon_error = CommonUtils.ImageToBase64(value), 
                (DeviceCategoryDTO dto, string value) => dto.icon_error_name = value
            );
            iconError.Ratio = 6;
            if (dto.icon_error_name != null) {
                iconError.FileName = dto.icon_error_name;
            }
            if (dto.icon_error != null) {
                Image? image = CommonUtils.ImageBase64ToImage(dto.icon_error);
                if (image != null) {
                    iconError.Image = image;
                }
            }

            // 添加按钮
            CommonButton confirmButton = _editEntityPopUpForm.AddButton("保存");
            confirmButton.Click += (s, e) => {
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
        protected override List<DeviceCategoryVO> QueryList() {
            QueryDeviceCategoryListRsp rsp = apis.QueryDeviceCategoryList(new() {
                UserId = SystemUtils.LoggedUserId(),
            });
            _dataDTOList = rsp.DeviceCategoryDTOs;
            List<DeviceCategoryVO> vos = new();
            CommonUtils.ObjectConverter<DeviceCategoryDTO, DeviceCategoryVO>(_dataDTOList, vos);
            // TODO: can use BackgroundWorker to do this
            // 后续再优化数据加载时的延迟、卡顿问题，现在先不管
            // for (int i = 0; i < 5000; i++) {
            //     workstationVOs.Add(workstationVOs[0]);
            // }
            return vos;
        }
        protected override void AddOrUpdate(DeviceCategoryDTO dto, Action action) {
            System.Console.WriteLine($"dtoicon_normal_name.: {dto.icon_normal_name}");
            AddOrUpdateDeviceCategoryRsp rsp = apis.AddOrUpdateDeviceCategory(new(dto));
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
                DeleteDeviceCategoryByIdsRsp rsp = apis.DeleteDeviceCategory(new(ids));
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
