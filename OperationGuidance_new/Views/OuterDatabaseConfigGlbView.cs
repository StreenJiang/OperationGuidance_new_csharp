using CustomLibrary.Buttons;
using CustomLibrary.ComboBoxes;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Configurations;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;

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
                OpenEditEntityPopUpForm("新增配置", dto, action);
            };
            _dataGridView.ModifyClick = (ids, action) => {
                if (ids.Count <= 0) {
                    WidgetUtils.ShowNoticePopUp("请选择要编辑的数据。");
                } else if (ids.Count > 1) {
                    WidgetUtils.ShowNoticePopUp("只能选择一条数据进行编辑操作。");
                } else {
                    if (_dataDTOList.Count > 0) {
                        OuterDatabaseConfigGlbDTO dto = _dataDTOList.Single(dto => dto.id == ids[0]);
                        OpenEditEntityPopUpForm("更新配置", dto, action);
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
        private void OpenEditEntityPopUpForm(string title, OuterDatabaseConfigGlbDTO dto, Action callBackAction) {
            _editEntityPopUpForm = new(dto) {
                Title = title,
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            // Host
            CustomTextBoxGroup host = _editEntityPopUpForm.AddTextBox("IP/HOST地址", false,
                (OuterDatabaseConfigGlbDTO dto, string? value) => dto.host = value ?? null);
            CustomTextBox ipBox = host.GetTextBox(0);
            ipBox.TextChanged += async (sender, eventArgs) => {
                while (ipBox.TimerTicking) {
                    await Task.Delay(100);
                }
                ipBox.IsError = string.IsNullOrEmpty(host.GetTextBox(0).Box.Text) || !ArgumentValidator.ValidateIPv4(ipBox.Text);
            };
            if (!string.IsNullOrEmpty(dto.host)) {
                host.SetValue(0, dto.host);
            }

            // Port
            CustomTextBoxGroup port = _editEntityPopUpForm.AddTextBox("端口号", false,
                (OuterDatabaseConfigGlbDTO dto, int? value) => dto.port = value ?? null);
            CustomTextBox portBox = port.GetTextBox(0);
            portBox.PositiveIntOnly = true;
            portBox.TextChanged += async (sender, eventArgs) => {
                while (portBox.TimerTicking) {
                    await Task.Delay(100);
                }
                portBox.IsError = string.IsNullOrEmpty(port.GetTextBox(0).Box.Text) || !ArgumentValidator.ValidatePortInWindows(portBox.Text);
            };
            if (dto.port != null && dto.port > 0) {
                port.SetValue(0, dto.port + "");
            }

            // Database name
            CustomTextBoxGroup database_name = _editEntityPopUpForm.AddTextBox("数据库名", false,
                (OuterDatabaseConfigGlbDTO dto, string? value) => dto.database_name = value ?? null);
            if (dto.database_name != null) {
                database_name.SetValue(0, dto.database_name);
            }
            database_name.GetTextBox(0).TextChanged += (sender, eventArgs) => {
                database_name.GetTextBox(0).IsError = string.IsNullOrEmpty(database_name.GetTextBox(0).Box.Text);
            };

            // Database type
            Dictionary<string, int> databaseTypes = new();
            DBTypes[] dBTypes = Enum.GetValues<DBTypes>();
            foreach (DBTypes type in dBTypes) {
                databaseTypes.Add(type.ToString(), (int)type);
            }
            CustomComboBoxGroup<int> database_type = _editEntityPopUpForm.AddComboBox("数据库类型",
                    (OuterDatabaseConfigGlbDTO dto, int value) => dto.database_type = value, databaseTypes);
            database_type.ItemSelected += () => {
                if (!database_type.IsDefaultValue()) {
                    database_type.SetError(false);
                }
            };
            if (dto.database_type != null && dto.database_type > 0) {
                database_type.SetCurrent(database_type.IndexOf(dto.database_type.Value));
            }

            // User name
            CustomTextBoxGroup username = _editEntityPopUpForm.AddTextBox("用户名", false,
                (OuterDatabaseConfigGlbDTO dto, string? value) => dto.username = value ?? null);
            if (dto.username != null) {
                username.SetValue(0, dto.username);
            }
            username.GetTextBox(0).Box.ImeMode = ImeMode.Disable; // 禁用输入法

            // Password
            CustomTextBoxGroup password = _editEntityPopUpForm.AddTextBox("密码", false,
                (OuterDatabaseConfigGlbDTO dto, string? value) => dto.password = value ?? null);
            password.GetTextBox(0).Box.PasswordChar = '*';
            string? passwordCache = dto.password;
            if (dto.password != null) {
                password.SetValue(0, _blockingPassword);
            }

            // Workstation name
            CustomTextBoxGroup workstation_name = _editEntityPopUpForm.AddTextBox("工位号", false,
                (OuterDatabaseConfigGlbDTO dto, string? value) => dto.workstation_name = value ?? null);
            if (dto.database_name != null) {
                workstation_name.SetValue(0, dto.database_name);
            }
            workstation_name.GetTextBox(0).TextChanged += (sender, eventArgs) => {
                workstation_name.GetTextBox(0).IsError = string.IsNullOrEmpty(workstation_name.GetTextBox(0).Box.Text);
            };

            // 添加按钮
            CommonButton confirmButton = _editEntityPopUpForm.AddButton("保存");
            confirmButton.Click += (s, e) => {
                bool check = true;
                string warningMsg = "";
                int warningIndex = 1;
                string hostText = host.GetTextBox(0).Box.Text;
                string portText = port.GetTextBox(0).Box.Text;
                string database_nameText = database_name.GetTextBox(0).Box.Text;
                int database_typeId = database_type.Value;
                string usernameText = username.GetTextBox(0).Box.Text;
                string passwordText = password.GetTextBox(0).Box.Text;
                string workstation_nameText = workstation_name.GetTextBox(0).Box.Text;

                if (string.IsNullOrEmpty(hostText)) {
                    check = false;
                    host.GetTextBox(0).IsError = true;
                    warningMsg += $"{warningIndex++}. IP/HOST地址不能为空\r\n";
                }
                if (string.IsNullOrEmpty(portText)) {
                    check = false;
                    port.GetTextBox(0).IsError = true;
                    warningMsg += $"{warningIndex++}. 端口号不能为空\r\n";
                }
                if (string.IsNullOrEmpty(database_nameText)) {
                    check = false;
                    database_name.GetTextBox(0).IsError = true;
                    warningMsg += $"{warningIndex++}. 数据库名不能为空\r\n";
                }
                if (database_type.IsDefaultValue() || database_typeId < 0) {
                    check = false;
                    database_type.SetError(true);
                    warningMsg += $"{warningIndex++}. 数据库类型不能为空\r\n";
                }
                if (string.IsNullOrEmpty(workstation_nameText)) {
                    check = false;
                    workstation_name.GetTextBox(0).IsError = true;
                    warningMsg += $"{warningIndex++}. 工位号不能为空\r\n";
                }

                if (check) {
                    int portInt = int.Parse(portText);
                    FindOuterDatabaseConfigGlbForCheckingReq req = new(dto.id, hostText, portInt, database_nameText, database_typeId, workstation_nameText);
                    List<OuterDatabaseConfigGlbDTO> outerDTOs = apis.FindOuterDatabaseConfigGlbForChecking(req).outerDTOs;

                    // IP + port + databaseName can not be the same as any exist
                    if (outerDTOs.Find(o => o.host == hostText && o.port == portInt && o.database_name == database_nameText && o.database_type == database_typeId && o.macs_id == SystemUtils.MacAddressesDTO.id) != null) {
                        check = false;
                        host.GetTextBox(0).IsError = true;
                        port.GetTextBox(0).IsError = true;
                        database_name.GetTextBox(0).IsError = true;
                        database_type.SetError(true);
                        warningMsg += $"{warningIndex++}. 已存在数据库配置：IP/HOST = {hostText}, 端口号 = {portInt}, 数据库名 = {database_nameText}, 数据库类型 = {Enum.GetName(typeof(DBTypes), database_typeId)}\r\n";
                    } else {
                        host.GetTextBox(0).IsError = false;
                        port.GetTextBox(0).IsError = false;
                        database_name.GetTextBox(0).IsError = false;
                        database_type.SetError(false);
                    }
                    // WorkstationName can't be the same as any exist
                    if (outerDTOs.Find(o => o.workstation_name == workstation_nameText) != null) {
                        check = false;
                        workstation_name.GetTextBox(0).IsError = true;
                        warningMsg += $"{warningIndex++}. 工位号已存在，不能重复\r\n";
                    } else {
                        workstation_name.GetTextBox(0).IsError = false;
                    }
                }

                if (!check) {
                    WidgetUtils.ShowWarningPopUp($"保存失败：\r\n{warningMsg}");
                } else {
                    if (password.GetTextBox(0).Box.Text == _blockingPassword) {
                        password.SetValue(0, passwordCache);
                    }
                    callBackAction += _editEntityPopUpForm.Dispose;
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
        }
        private void ResizePopUpForm() {
            if (_editEntityPopUpForm != null) {
                _editEntityPopUpForm.ResizeTablePanelAndItsChildren();
                _editEntityPopUpForm.Invalidate();
            }
        }
        #endregion

        #region Override methods
        protected override List<OuterDatabaseConfigGlbVO> QueryList() {
            QueryOuterDatabaseConfigGlbListRsp rsp = apis.QueryOuterDatabaseConfigGlbList(new(SystemUtils.MacAddressesDTO.id));
            _dataDTOList = rsp.OuterDatabaseConfigGlbDTOs;
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
            AddOrUpdateOuterDatabaseConfigGlbRsp rsp = apis.AddOrUpdateOuterDatabaseConfigGlb(new(dto));
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
                DeleteOuterDatabaseConfigGlbByIdsRsp rsp = apis.DeleteOuterDatabaseConfigGlb(new(ids));
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
