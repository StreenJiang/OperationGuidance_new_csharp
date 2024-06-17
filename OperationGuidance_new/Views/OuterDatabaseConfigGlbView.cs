using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using CustomLibrary.TextBoxes;
using OperationGuidance_service.Models.Requests;
using CustomLibrary.ComboBoxes;

namespace OperationGuidance_new.Views {
    public class OuterDatabaseConfigGlbView : CustomDataGridViewOuterPanel<OuterDatabaseConfigGlbDTO, OuterDatabaseConfigGlbVO> {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        private List<OuterDatabaseConfigGlbDTO> _dataDTOList;
        // DataGridView panel
        private DataGridViewGroup<OuterDatabaseConfigGlbVO> _dataGridView;
        // Add new pop up form
        private EditEntityPopUpForm<OuterDatabaseConfigGlbDTO> _editEntityPopUpForm;
        private string _blockingPassword = "******";
        #endregion

        #region Constructors
        public OuterDatabaseConfigGlbView() {
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
            _dataGridView.AddTextBox("IP/HOST地址", false, (OuterDatabaseConfigGlbVO vo, string? value) => vo.host = value);
            _dataGridView.AddTextBox("用户名", false, (OuterDatabaseConfigGlbVO vo, string? value) => vo.username = value);

            // Btns
            _dataGridView.QueryData = (vo) => {
                List<OuterDatabaseConfigGlbVO> vos = QueryList();
                return vos
                    .Where(o => vo.host == null || o.host != null && o.host.Contains(vo.host))
                    .Where(o => vo.username == null || o.username != null && o.username.Contains(vo.username))
                    .ToList();
            };
            _dataGridView.AddNewClick = (action) => {
                OuterDatabaseConfigGlbDTO dto = new();
                // OpenEditEntityPopUpForm("新增配置", dto, action);
            };
            _dataGridView.ModifyClick = (ids, action) => {
                if (ids.Count <= 0) {
                    WidgetUtils.ShowNoticePopUp("请选择要编辑的数据。");
                } else if (ids.Count > 1) {
                    WidgetUtils.ShowNoticePopUp("只能选择一条数据进行编辑操作。");
                } else {
                    if (_dataDTOList.Count > 0) {
                        OuterDatabaseConfigGlbDTO dto = _dataDTOList.Single(dto => dto.id == ids[0]);
                        // OpenEditEntityPopUpForm("更新配置", dto, action);
                        // Query once right after updating
                        action();
                    }
                }
            };
            _dataGridView.DeleteClick = (ids, action) => {
                // Do delete
                Delete(ids);
                // Query once right after deleting
                action();
            };
        }
        #endregion

        #region Reusable methods
        // private void OpenEditEntityPopUpForm(string title, OuterDatabaseConfigGlbDTO dto, Action callBackAction) {
        //     _editEntityPopUpForm = new(dto) {
        //         Title = title,
        //         BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
        //     };
        //     // Fields
        //     CustomTextBoxGroup host = _editEntityPopUpForm.AddTextBox("IP/HOST地址", false,
        //         (OuterDatabaseConfigGlbDTO dto, string? value) => dto.host = value ?? null);
        //     host.PositiveIntOnly = true;
        //     if (!string.IsNullOrEmpty(dto.host)) {
        //         host.SetValue(0, dto.host + "");
        //     }
        //     host.GetTextBox(0).TextChanged += (sender, eventArgs) => {
        //         host.GetTextBox(0).IsError = string.IsNullOrEmpty(host.GetTextBox(0).Box.Text);
        //     };
        //     CustomTextBoxGroup port = _editEntityPopUpForm.AddTextBox("端口号", false,
        //         (OuterDatabaseConfigGlbDTO dto, int? value) => dto.port = value ?? null);
        //     if (dto.port != null && dto.port > 0) {
        //         port.SetValue(0, dto.port + "");
        //     }
        //     CustomTextBoxGroup database_name = _editEntityPopUpForm.AddTextBox("数据库名", false,
        //         (OuterDatabaseConfigGlbDTO dto, string? value) => dto.database_name = value ?? null);
        //     if (dto.database_name != null) {
        //         database_name.SetValue(0, dto.database_name);
        //     }
        //     database_name.GetTextBox(0).TextChanged += (sender, eventArgs) => {
        //         database_name.GetTextBox(0).IsError = string.IsNullOrEmpty(database_name.GetTextBox(0).Box.Text);
        //     };
        //     CustomTextBoxGroup username = _editEntityPopUpForm.AddTextBox("用户名", false,
        //         (OuterDatabaseConfigGlbDTO dto, string? value) => dto.username = value ?? null);
        //     if (dto.username != null) {
        //         username.SetValue(0, dto.username);
        //     }
        //     username.GetTextBox(0).Box.ImeMode = ImeMode.Disable; // 禁用输入法
        //     CustomTextBoxGroup password = _editEntityPopUpForm.AddTextBox("密码", false,
        //         (OuterDatabaseConfigGlbDTO dto, string? value) => dto.password = value ?? null);
        //     password.GetTextBox(0).Box.PasswordChar = '*';
        //     string? passwordCache = dto.password;
        //     if (dto.password != null) {
        //         password.SetValue(0, _blockingPassword);
        //     }

        //     // 添加按钮
        //     CommonButton confirmButton = _editEntityPopUpForm.AddButton("保存");
        //     confirmButton.Click += (s, e) => {
        //         bool check = true;
        //         string warningMsg = "";
        //         int warningIndex = 1;
        //         string usernameText = username.GetTextBox(0).Box.Text;
        //         string staffIdText = host.GetTextBox(0).Box.Text;
        //         int? staffIdInt = string.IsNullOrEmpty(staffIdText) ? null : int.Parse(staffIdText);
        //         FindUserByConditionForCheckingReq req = new() {
        //             Id = dto.id,
        //             StaffId = staffIdInt,
        //             username = usernameText,
        //         };
        //         OuterDatabaseConfigGlbDTO? user = apis.FindUserByConditionForChecking(req).OuterDatabaseConfigGlbDTO;
        //         if (string.IsNullOrEmpty(staffIdText)) {
        //             check = false;
        //             host.GetTextBox(0).IsError = true;
        //             warningMsg += $"{warningIndex++}. 员工号不能为空\r\n";
        //         }
        //         if (user?.staff_id == dto.staff_id) {
        //             check = false;
        //             host.GetTextBox(0).IsError = true;
        //             warningMsg += $"{warningIndex++}. 员工号已存在\r\n";
        //         }
        //         if (string.IsNullOrEmpty(usernameText)) {
        //             check = false;
        //             username.GetTextBox(0).IsError = true;
        //             warningMsg += $"{warningIndex++}. 账户名不能为空\r\n";
        //         }
        //         if (user?.username == dto.username) {
        //             check = false;
        //             username.GetTextBox(0).IsError = true;
        //             warningMsg += $"{warningIndex++}. 此账户名已存在\r\n";
        //         }
        //         if (string.IsNullOrEmpty(name.GetTextBox(0).Box.Text)) {
        //             check = false;
        //             name.GetTextBox(0).IsError = true;
        //             warningMsg += $"{warningIndex++}. 姓名不能为空\r\n";
        //         }
        //         if (roleType.IsDefaultValue()) {
        //             roleType.SetError(true);
        //             check = false;
        //             warningMsg += $"{warningIndex++}. 没有选择权限角色\r\n";
        //         } else if (roleType.Value == (int)Roles.DEVELOPER || roleType.Value == (int)Roles.DEVELOPER) {
        //             if (string.IsNullOrEmpty(operation_password.GetTextBox(0).Box.Text)) {
        //                 check = false;
        //                 operation_password.GetTextBox(0).IsError = true;
        //                 warningMsg += $"{warningIndex++}. 操作密码不能为空\r\n";
        //             }
        //         }
        //         // 检查密码
        //         string passwordText = password.GetTextBox(0).Box.Text;

        //         if (!check) {
        //             WidgetUtils.ShowWarningPopUp($"保存失败：\r\n{warningMsg}");
        //         } else {
        //             // 在这设置保证保存时不会将不需要的数据保存进去
        //             if (!operation_password.Visible) {
        //                 operation_password.SetValue(0, null);
        //             }
        //             if (password.GetTextBox(0).Box.Text == _blockingPassword) {
        //                 password.SetValue(0, passwordCache);
        //             }
        //             if (operation_password.GetTextBox(0).Box.Text == _blockingPassword) {
        //                 operation_password.SetValue(0, operationPasswordCache);
        //             }
        //             callBackAction += _editEntityPopUpForm.Dispose;
        //             AddOrUpdate(dto, callBackAction);
        //             _editEntityPopUpForm.Hide();
        //             // 如果修改的是当前用户，则更新
        //             if (dto.id == SystemUtils.LoggedUserId) {
        //                 SystemUtils.UserInfo = dto;
        //             }
        //         }
        //     };
        //     CommonButton cancelButton = _editEntityPopUpForm.AddButton("取消");
        //     cancelButton.Click += (s, e) => {
        //         _editEntityPopUpForm.Dispose();
        //     };
        //     // Show form but make it transparent to create handles for its children
        //     _editEntityPopUpForm.PretendToShowToCreateHandlesForChildren();
        //     // Resize all widgets
        //     ResizePopUpForm();
        //     // Real show
        //     _editEntityPopUpForm.Show();
        // }
        private void ResizePopUpForm() {
            if (_editEntityPopUpForm != null) {
                _editEntityPopUpForm.ResizeTablePanelAndItsChildren();
                _editEntityPopUpForm.Invalidate();
            }
        }
        #endregion

        #region Override methods
        protected override List<OuterDatabaseConfigGlbVO> QueryList() {
            // QueryOuterDatabaseConfigGlbListRsp rsp = apis.QueryOuterDatabaseConfigGlbList(new() {
            //     UserId = SystemUtils.LoggedUserId,
            // });
            // _dataDTOList = rsp.OuterDatabaseConfigGlbDTOs;
            List<OuterDatabaseConfigGlbVO> userVOs = new();
            CommonUtils.ObjectConverter<OuterDatabaseConfigGlbDTO, OuterDatabaseConfigGlbVO>(_dataDTOList, userVOs);
            // TODO: can use BackgroundWorker to do this
            // 后续再优化数据加载时的延迟、卡顿问题，现在先不管
            // for (int i = 0; i < 5000; i++) {
            //     workstationVOs.Add(workstationVOs[0]);
            // }
            return userVOs;
        }
        protected override void AddOrUpdate(OuterDatabaseConfigGlbDTO dto, Action action) {
            // AddOrUpdateOuterDatabaseConfigGlbRsp rsp = apis.AddOrUpdateOuterDatabaseConfigGlb(new(dto));
            // if (rsp.RsponseCode == HttpResponseCode.OK) {
            //     WidgetUtils.ShowNoticePopUp("保存成功！");
            // } else {
            //     WidgetUtils.ShowErrorPopUp($"保存失败！错误信息：{rsp.RsponseMessage}");
            // }
            action();
        }
        protected override void Delete(List<int> ids) {
            if (ids.Count <= 0) {
                WidgetUtils.ShowNoticePopUp("请选择要删除的数据。");
            } else if (WidgetUtils.ShowConfirmPopUp($"确认要删除已选择的{ids.Count}条数据吗？")) {
                // DeleteOuterDatabaseConfigGlbByIdsRsp rsp = apis.DeleteOuterDatabaseConfigGlb(new(ids));
                // if (rsp.RsponseCode == HttpResponseCode.OK) {
                //     WidgetUtils.ShowNoticePopUp($"成功删除{ids.Count}条数据！");
                // }
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
