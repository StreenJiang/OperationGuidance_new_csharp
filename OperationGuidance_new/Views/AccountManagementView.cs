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
    public class AccountManagementView: CustomDataGridViewOuterPanel<UserAccountInfoDTO, UserAccountInfoVO> {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        private List<UserAccountInfoDTO> _dataDTOList;
        // DataGridView panel
        private DataGridViewGroup<UserAccountInfoVO> _dataGridView;
        // Add new pop up form
        private EditEntityPopUpForm<UserAccountInfoDTO> _editEntityPopUpForm;
        #endregion

        #region Constructors
        public AccountManagementView() {
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
            _dataGridView.AddTextBox("姓名", false, (UserAccountInfoVO vo, string? value) => vo.name = value);
            _dataGridView.AddTextBox("账户名", false, (UserAccountInfoVO vo, string? value) => vo.account = value);

            // 按钮逻辑
            _dataGridView.QueryData = (vo) => {
                List<UserAccountInfoVO> vos = QueryList();
                return vos
                    .Where(o => vo.name == null || o.name != null && o.name.Contains(vo.name))
                    .Where(o => vo.account == null || o.account != null && o.account.Contains(vo.account))
                    .ToList();
            };
            _dataGridView.AddNewClick = (action) => {
                UserAccountInfoDTO dto = new();
                OpenEditEntityPopUpForm("新增用户", dto, action);
            };
            _dataGridView.ModifyClick = (ids, action) => {
                if (ids.Count <= 0) {
                    WidgetUtils.ShowNoticePopUp("请选择要编辑的数据。");
                } else if (ids.Count > 1) {
                    WidgetUtils.ShowNoticePopUp("只能选择一条数据进行修改操作。");
                } else {
                    if (_dataDTOList.Count > 0) {
                        UserAccountInfoDTO dto = _dataDTOList.Single(dto => dto.id == ids[0]);
                        OpenEditEntityPopUpForm("更新用户账户信息", dto, action);
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
        private void OpenEditEntityPopUpForm(string title, UserAccountInfoDTO dto, Action callBackAction) {
            _editEntityPopUpForm = new(dto) {
                Title = title,
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            // 添加字段
            CustomTextBoxGroup staffId = _editEntityPopUpForm.AddTextBox("员工ID", false, 
                (UserAccountInfoDTO dto, int? value) => dto.staff_id = value ?? 0);
            staffId.NumberOnly = true;
            if (dto.staff_id != null) {
                staffId.SetValue(0, dto.staff_id.Value + "");
            }
            CustomTextBoxGroup name = _editEntityPopUpForm.AddTextBox("姓名", false, 
                (UserAccountInfoDTO dto, string? value) => dto.name = value ?? "");
            if (dto.name != null) {
                name.SetValue(0, dto.name);
            }
            CustomTextBoxGroup position = _editEntityPopUpForm.AddTextBox("角色", false, 
                (UserAccountInfoDTO dto, string? value) => dto.position = value ?? "");
            if (dto.position != null) {
                position.SetValue(0, dto.position);
            }
            CustomTextBoxGroup account = _editEntityPopUpForm.AddTextBox("账户名", false, 
                (UserAccountInfoDTO dto, string? value) => dto.account = value ?? "");
            if (dto.account != null) {
                account.SetValue(0, dto.account);
            }
            CustomTextBoxGroup password = _editEntityPopUpForm.AddTextBox("密码", false, 
                (UserAccountInfoDTO dto, string? value) => dto.password = value ?? "");
            if (dto.password != null) {
                password.SetValue(0, dto.password);
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
        protected override List<UserAccountInfoVO> QueryList() {
            QueryUserAccountInfoListRsp rsp = apis.QueryUserAccountInfoList(new() {
                UserId = SystemUtils.LoggedUserId(),
            });
            _dataDTOList = rsp.UserAccountInfoDTOs;
            List<UserAccountInfoVO> userVOs = new();
            CommonUtils.ObjectConverter<UserAccountInfoDTO, UserAccountInfoVO>(_dataDTOList, userVOs);
            // TODO: can use BackgroundWorker to do this
            // 后续再优化数据加载时的延迟、卡顿问题，现在先不管
            // for (int i = 0; i < 5000; i++) {
            //     workstationVOs.Add(workstationVOs[0]);
            // }
            return userVOs;
        }
        protected override void AddOrUpdate(UserAccountInfoDTO dto, Action action) {
            AddOrUpdateUserAccountInfoRsp rsp = apis.AddOrUpdateUserAccountInfo(new(dto));
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
                DeleteUserAccountInfoByIdsRsp rsp = apis.DeleteUserAccountInfo(new(ids));
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
