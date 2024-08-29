using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;
using CustomLibrary.TextBoxes;

namespace OperationGuidance_new.Views {
    public class MatCodeMapWhycView_WHYC: CustomDataGridViewOuterPanel<MatCodeMapWhycDTO, MatCodeMapWhycVO> {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        private List<MatCodeMapWhycDTO> _dataDTOList;
        // DataGridView panel
        private DataGridViewGroup<MatCodeMapWhycVO> _dataGridView;
        // Add new pop up form
        private EditEntityPopUpForm<MatCodeMapWhycDTO> _editEntityPopUpForm;
        #endregion

        #region Constructors
        public MatCodeMapWhycView_WHYC() {
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
            _dataGridView.AddTextBox("MatCode", false, (MatCodeMapWhycVO vo, string? value) => vo.mat_code = value);

            // 按钮逻辑
            _dataGridView.QueryData = (vo) => {
                List<MatCodeMapWhycVO> workstationVOs = QueryList();
                return workstationVOs
                    .Where(o => vo.mat_code == null || o.mat_code != null && o.mat_code.Contains(vo.mat_code))
                    .ToList();
            };
            _dataGridView.AddNewClick = (action) => {
                MatCodeMapWhycDTO dto = new();
                OpenEditEntityPopUpForm("新增MatCode映射", dto, action);
            };
            _dataGridView.ModifyClick = (ids, action) => {
                if (ids.Count <= 0) {
                    WidgetUtils.ShowNoticePopUp("请选择要编辑的数据。");
                } else if (ids.Count > 1) {
                    WidgetUtils.ShowNoticePopUp("只能选择一条数据进行编辑操作。");
                } else {
                    if (_dataDTOList.Count > 0) {
                        MatCodeMapWhycDTO dto = _dataDTOList.Single(dto => dto.id == ids[0]);
                        OpenEditEntityPopUpForm("更新MatCode映射", dto, action);
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
        private void OpenEditEntityPopUpForm(string title, MatCodeMapWhycDTO dto, Action callBackAction) {
            _editEntityPopUpForm = new(dto) {
                Title = title,
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            // 添加字段
            CustomTextBoxGroup mat_code = _editEntityPopUpForm.AddTextBox("MatCode", false,
                (MatCodeMapWhycDTO dto, string? value) => dto.mat_code = value ?? "");
            if (dto.mat_code != null) {
                mat_code.SetValue(0, dto.mat_code);
            }
            mat_code.GetTextBox(0).TextChanged += (sender, eventArgs) => {
                mat_code.GetTextBox(0).IsError = string.IsNullOrEmpty(mat_code.GetTextBox(0).Box.Text);
            };
            CustomTextBoxGroup parameter_set = _editEntityPopUpForm.AddTextBox("端口号", false,
                (MatCodeMapWhycDTO dto, int? value) => dto.parameter_set = value ?? 0);
            CustomTextBox parameter_setBox = parameter_set.GetTextBox(0);
            parameter_setBox.PositiveIntOnly = true;
            if (dto.parameter_set > 0) {
                parameter_set.SetValue(0, dto.parameter_set + "");
            }

            // 添加按钮
            CommonButton confirmButton = _editEntityPopUpForm.AddButton("保存");
            confirmButton.Click += (s, e) => {
                bool check = true;
                string warningMsg = "";
                int warningIndex = 1;
                List<MatCodeMapWhycDTO> allData = apis.QueryMatCodeMapWhycList(new(SystemUtils.MacAddressesDTO.id)).MatCodeMapWhycDTOs;
                if (string.IsNullOrEmpty(mat_code.GetTextBox(0).Box.Text)) {
                    check = false;
                    mat_code.GetTextBox(0).IsError = true;
                    warningMsg += $"{warningIndex++}. MatCode不能为空\r\n";
                }
                if (allData.Exists(d => d.id != dto.id && d.mat_code == dto.mat_code)) {
                    check = false;
                    mat_code.GetTextBox(0).IsError = true;
                    warningMsg += $"{warningIndex++}. MatCode[{dto.mat_code}]已配置过了\r\n";
                }
                if (string.IsNullOrEmpty(parameter_setBox.Box.Text)) {
                    check = false;
                    parameter_setBox.IsError = true;
                    warningMsg += $"{warningIndex++}. 程序号不能为空\r\n";
                }
                if (allData.Exists(d => d.id != dto.id && d.parameter_set == dto.parameter_set)) {
                    check = false;
                    warningMsg += $"{warningIndex++}. 程序号[{dto.parameter_set}]已配置过了\r\n";
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
        protected override List<MatCodeMapWhycVO> QueryList() {
            QueryMatCodeMapWhycListRsp rsp = apis.QueryMatCodeMapWhycList(new(SystemUtils.MacAddressesDTO.id));
            _dataDTOList = rsp.MatCodeMapWhycDTOs.Where(dto => dto.deleted == (int) YesOrNo.NO).ToList();
            List<MatCodeMapWhycVO> vos = new();
            CommonUtils.ObjectConverter<MatCodeMapWhycDTO, MatCodeMapWhycVO>(_dataDTOList, vos);
            // TODO: can use BackgroundWorker to do this
            // 后续再优化数据加载时的延迟、卡顿问题，现在先不管
            // for (int i = 0; i < 5000; i++) {
            //     workstationVOs.Add(workstationVOs[0]);
            // }
            return vos;
        }
        protected override void AddOrUpdate(MatCodeMapWhycDTO dto, Action action) {
            AddOrUpdateMatCodeMapWhycRsp rsp = apis.AddOrUpdateMatCodeMapWhyc(new(dto));
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
                DeleteMatCodeMapWhycByIdsRsp rsp = apis.DeleteMatCodeMapWhyc(new(ids));
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
