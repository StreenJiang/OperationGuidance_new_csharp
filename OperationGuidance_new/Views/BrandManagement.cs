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
    public class BrandManagementView: CustomDataGridViewOuterPanel<BrandDTO, BrandVO> {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        private List<BrandDTO> _dataDTOList;
        // DataGridView panel
        private DataGridViewGroup<BrandVO> _dataGridView;
        // Add new pop up form
        private EditEntityPopUpForm<BrandDTO> _editEntityPopUpForm;
        #endregion

        #region Constructors
        public BrandManagementView() {
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
            _dataGridView.AddTextBox("品牌名称", false, (BrandVO vo, string? value) => vo.name = value);
            _dataGridView.AddTextBox("品牌描述", false, (BrandVO vo, string? value) => vo.description = value);

            // 按钮逻辑
            _dataGridView.QueryData = (vo) => {
                List<BrandVO> vos = QueryList();
                return vos
                    .Where(o => vo.name == null || o.name != null && o.name.Contains(vo.name))
                    .Where(o => vo.description == null || o.description != null && o.description.Contains(vo.description))
                    .ToList();
            };
            _dataGridView.AddNewClick = (action) => {
                BrandDTO dto = new();
                OpenEditEntityPopUpForm("新增品牌", dto, action);
            };
            _dataGridView.ModifyClick = (ids, action) => {
                if (ids.Count <= 0) {
                    WidgetUtils.ShowNoticePopUp("请选择要编辑的数据。");
                } else if (ids.Count > 1) {
                    WidgetUtils.ShowNoticePopUp("只能选择一条数据进行修改操作。");
                } else {
                    if (_dataDTOList.Count > 0) {
                        BrandDTO dto = _dataDTOList.Single(dto => dto.id == ids[0]);
                        OpenEditEntityPopUpForm("更新品牌信息", dto, action);
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
        private void OpenEditEntityPopUpForm(string title, BrandDTO dto, Action callBackAction) {
            _editEntityPopUpForm = new(dto) {
                Title = title,
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            // 添加字段
            CustomTextBoxGroup brandName = _editEntityPopUpForm.AddTextBox("品牌名称", false, 
                (BrandDTO dto, string? value) => dto.name = value ?? "");
            if (dto.name != null) {
                brandName.SetValue(0, dto.name);
            }
            CustomTextBoxGroup brandShortName = _editEntityPopUpForm.AddTextBox("品牌简称", false, 
                (BrandDTO dto, string? value) => dto.short_name = value ?? "");
            if (dto.short_name != null) {
                brandShortName.SetValue(0, dto.short_name);
            }
            CustomTextBoxGroup brandEnglishName = _editEntityPopUpForm.AddTextBox("品牌英文名称", false, 
                (BrandDTO dto, string? value) => dto.english_name = value ?? "");
            if (dto.english_name != null) {
                brandEnglishName.SetValue(0, dto.english_name);
            }
            CustomTextBoxGroup brandEnglishShortName = _editEntityPopUpForm.AddTextBox("品牌英文简称", false, 
                (BrandDTO dto, string? value) => dto.english_short_name = value ?? "");
            if (dto.english_short_name != null) {
                brandEnglishShortName.SetValue(0, dto.english_short_name);
            }
            CustomTextBoxGroup description = _editEntityPopUpForm.AddTextBox("品牌描述", false, 
                (BrandDTO dto, string? value) => dto.description = value ?? "");
            if (dto.description != null) {
                description.SetValue(0, dto.description);
            }

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
        protected override List<BrandVO> QueryList() {
            QueryBrandListRsp rsp = apis.QueryBrandList(new() {
                UserId = SystemUtils.LoggedUserId(),
            });
            _dataDTOList = rsp.BrandDTOs;
            List<BrandVO> brandVOs = new();
            CommonUtils.ObjectConverter<BrandDTO, BrandVO>(_dataDTOList, brandVOs);
            // TODO: can use BackgroundWorker to do this
            // 后续再优化数据加载时的延迟、卡顿问题，现在先不管
            // for (int i = 0; i < 5000; i++) {
            //     workstationVOs.Add(workstationVOs[0]);
            // }
            return brandVOs;
        }
        protected override void AddOrUpdate(BrandDTO dto, Action action) {
            AddOrUpdateBrandRsp rsp = apis.AddOrUpdateBrand(new(dto));
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
                DeleteBrandByIdsRsp rsp = apis.DeleteBrand(new(ids));
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
