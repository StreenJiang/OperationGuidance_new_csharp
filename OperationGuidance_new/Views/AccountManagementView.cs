using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Utils;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;
using CustomLibrary.TextBoxes;
using OperationGuidance_service.Models.Requests;
using CustomLibrary.ComboBoxes;
using CustomLibrary.Panels.AbstractClasses;

namespace OperationGuidance_new.Views
{
    public class AccountManagementView: ACustomDataGridViewOuterPanel<UserAccountInfoDTO, UserAccountInfoVO> {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        private List<UserAccountInfoDTO> _dataDTOList;
        // DataGridView panel
        private DataGridViewGroup<UserAccountInfoVO> _dataGridView;
        // Add new pop up form
        private EditEntityPopUpForm<UserAccountInfoDTO> _editEntityPopUpForm;
        private string _blockingPassword = "******";
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
                    WidgetUtils.ShowNoticePopUp("只能选择一条数据进行编辑操作。");
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
                (UserAccountInfoDTO dto, int value) => dto.staff_id = value);
            staffId.PositiveIntOnly = true;
            if (dto.staff_id > 0) {
                staffId.SetValue(0, dto.staff_id + "");
            }
            CustomTextBoxGroup name = _editEntityPopUpForm.AddTextBox("姓名", false, 
                (UserAccountInfoDTO dto, string? value) => dto.name = value ?? "");
            if (dto.name != null) {
                name.SetValue(0, dto.name);
            }
            name.GetTextBox(0).TextChanged += (sender, eventArgs) => {
                name.GetTextBox(0).IsError = string.IsNullOrEmpty(name.GetTextBox(0).Box.Text);
            };
            CustomTextBoxGroup position = _editEntityPopUpForm.AddTextBox("职位", false, 
                (UserAccountInfoDTO dto, string? value) => dto.position = value ?? null);
            if (dto.position != null) {
                position.SetValue(0, dto.position);
            }
            CustomTextBoxGroup account = _editEntityPopUpForm.AddTextBox("账户名", false, 
                (UserAccountInfoDTO dto, string? value) => dto.account = value ?? "");
            if (dto.account != null) {
                account.SetValue(0, dto.account);
            }
            account.GetTextBox(0).Box.ImeMode = ImeMode.Disable;
            account.GetTextBox(0).TextChanged += (sender, eventArgs) => {
                account.GetTextBox(0).IsError = string.IsNullOrEmpty(account.GetTextBox(0).Box.Text);
            };
            CustomTextBoxGroup password = _editEntityPopUpForm.AddTextBox("密码", false, 
                (UserAccountInfoDTO dto, string? value) => dto.password = value ?? null);
            // 暂时先用这个代替，后续再做进一步的完善，使CustomTextBox能够支持密码模式并提供按钮可以开关屏蔽功能
            password.GetTextBox(0).Box.PasswordChar = '*';
            string? passwordCache = dto.password;
            if (dto.password != null) {
                password.SetValue(0, _blockingPassword);
            }
            Dictionary<string, int> roleOptions = new();
            Roles[] roleValues = Enum.GetValues<Roles>();
            UserAccountInfoDTO userInfo = SystemUtils.UserInfo;
            foreach (Roles value in roleValues) {
                if (value == Roles.DEVELOPER) {
                    continue;
                } else if (value == Roles.ADMIN) {
                    if (userInfo.role_type != (int) Roles.DEVELOPER && userInfo.role_type != (int) Roles.ADMIN) {
                        continue;
                    }
                }
                roleOptions.Add(value.ToString(), (int) value);
            }
            CustomComboBoxGroup<int> roleType = _editEntityPopUpForm.AddComboBox("权限角色", 
                (UserAccountInfoDTO dto, int value) => dto.role_type = value, roleOptions);
            roleType.SetCurrent(roleType.IndexOf(dto.role_type));
            roleType.ItemSelected += () => {
                roleType.SetError(roleType.IsDefaultValue());
            };
            CustomTextBoxGroup operation_password = _editEntityPopUpForm.AddTextBox("操作密码", false, 
                (UserAccountInfoDTO dto, string? value) => dto.operation_password = value ?? null);
            _editEntityPopUpForm.ShowOrHideControl(operation_password, roleType.Value == (int) Roles.DEVELOPER || roleType.Value == (int) Roles.ADMIN);
            operation_password.VisibleChanged += (s, e) => {
                if (operation_password.Visible) {
                    operation_password.ResizeChildren();
                }
            };
            operation_password.GetTextBox(0).Box.PasswordChar = '*';
            string? operationPasswordCache = dto.operation_password;
            if (dto.operation_password != null) {
                operation_password.SetValue(0, _blockingPassword);
            }
            operation_password.GetTextBox(0).TextChanged += (sender, eventArgs) => {
                operation_password.GetTextBox(0).IsError = string.IsNullOrEmpty(operation_password.GetTextBox(0).Box.Text);
            };
            roleType.ItemSelected += () => {
                _editEntityPopUpForm.ShowOrHideControl(operation_password, roleType.Value == (int) Roles.DEVELOPER || roleType.Value == (int) Roles.ADMIN);
                _editEntityPopUpForm.ResizeTablePanelAndItsChildren();
                operation_password.ResizeChildren();
            };

            // 添加按钮
            CommonButton confirmButton = _editEntityPopUpForm.AddButton("保存");
            confirmButton.Click += (s, e) => {
                bool check = true;
                string warningMsg = "";
                int warningIndex = 1;
                string accountText = account.GetTextBox(0).Box.Text;
                string staffIdText = staffId.GetTextBox(0).Box.Text;
                int? staffIdInt = string.IsNullOrEmpty(staffIdText) ? null : int.Parse(staffIdText);
                FindUserByConditionForCheckingReq req = new() {
                    Id = dto.id,
                    StaffId = staffIdInt,
                    Account = accountText,
                };
                UserAccountInfoDTO? user = apis.FindUserByConditionForChecking(req).UserAccountInfoDTO;
                if (string.IsNullOrEmpty(staffIdText)) {
                    check = false;
                    staffId.GetTextBox(0).IsError = true;
                    warningMsg += $"{warningIndex++}. 员工号不能为空\r\n";
                }
                if (user?.staff_id == dto.staff_id) {
                    check = false;
                    staffId.GetTextBox(0).IsError = true;
                    warningMsg += $"{warningIndex++}. 员工号已存在\r\n";
                }
                if (string.IsNullOrEmpty(accountText)) {
                    check = false;
                    account.GetTextBox(0).IsError = true;
                    warningMsg += $"{warningIndex++}. 账户名不能为空\r\n";
                }
                if (user?.account == dto.account) {
                    check = false;
                    account.GetTextBox(0).IsError = true;
                    warningMsg += $"{warningIndex++}. 此账户名已存在\r\n";
                }
                if (string.IsNullOrEmpty(name.GetTextBox(0).Box.Text)) {
                    check = false;
                    name.GetTextBox(0).IsError = true;
                    warningMsg += $"{warningIndex++}. 姓名不能为空\r\n";
                }
                if (roleType.IsDefaultValue()) {
                    roleType.SetError(true);
                    check = false;
                    warningMsg += $"{warningIndex++}. 没有选择权限角色\r\n";
                } else if (roleType.Value == (int) Roles.DEVELOPER || roleType.Value == (int) Roles.DEVELOPER) {
                    if (string.IsNullOrEmpty(operation_password.GetTextBox(0).Box.Text)) {
                        check = false;
                        operation_password.GetTextBox(0).IsError = true;
                        warningMsg += $"{warningIndex++}. 操作密码不能为空\r\n";
                    }
                }
                // 检查密码
                string passwordText = password.GetTextBox(0).Box.Text;

                if (!check) {
                    WidgetUtils.ShowWarningPopUp($"保存失败：\r\n{warningMsg}");
                } else {
                    // 在这设置保证保存时不会将不需要的数据保存进去
                    if (!operation_password.Visible) {
                        operation_password.SetValue(0, null);
                    }
                    if (password.GetTextBox(0).Box.Text == _blockingPassword) {
                        password.SetValue(0, passwordCache);
                    }
                    if (operation_password.GetTextBox(0).Box.Text == _blockingPassword) {
                        operation_password.SetValue(0, operationPasswordCache);
                    }
                    callBackAction += _editEntityPopUpForm.Dispose;
                    AddOrUpdate(dto, callBackAction);
                    _editEntityPopUpForm.Hide();
                    // 如果修改的是当前用户，则更新
                    if (dto.id == SystemUtils.LoggedUserId) {
                        SystemUtils.UserInfo = dto;
                    }
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
                UserId = SystemUtils.LoggedUserId,
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
