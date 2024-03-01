using CustomLibrary.Buttons;
using CustomLibrary.DateTimePickers;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Utils;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class DataQueryView: CustomDataGridViewOuterPanel<OperationDataDTO, OperationDataVO> {
        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        private List<OperationDataDTO> _dataDTOList;
        private List<OperationDataField> _operationDataFields;
        // DataGridView panel
        private DataGridViewGroup<OperationDataVO> _dataGridView;
        // Add new pop up form
        private EditEntityPopUpForm<OperationDataDTO> _editEntityPopUpForm;
        #endregion

        #region Constructors
        public DataQueryView() {
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
            _operationDataFields = MainUtils.GetOperationDataFields();
            _dataGridView = new(gridView => {
                DataGridViewColumn[] columnRange = {};
                foreach (OperationDataField field in _operationDataFields) {
                    if (field.Visible) {
                        DataGridViewTextBoxColumn column = new() {
                            DataPropertyName = field.PropertyName,
                            HeaderText = field.FieldName,
                            ReadOnly = true,
                        };
                        columnRange = columnRange.Append(column).ToArray();
                    } 
                }
                gridView.Columns.Clear();
                gridView.Columns.AddRange(columnRange);
                gridView.Columns[0].Frozen = true;
            }) {
                Parent = this,
                FiltersTableColumnNums = 2,
            };
            // 搜索条件
            CustomDatePickerGroup dateFitler = _dataGridView.AddSeparateDatePicker("日期", "~", 
                    (OperationDataVO vo, DateTime? value) => vo.filter_create_time_min = value, 
                    (OperationDataVO vo, DateTime? value) => vo.filter_create_time_max = value);
            CustomDatePicker date_min = dateFitler.GetPicker(0);
            CustomDatePicker date_max = dateFitler.GetPicker(1);
            date_min.ValueChanged += (sender, eventArgs) => {
                if (date_max.Value != null && date_min.Value > date_max.Value) {
                    date_min.Value = null;
                    WidgetUtils.ShowErrorPopUp("日期范围应为左早右晚！");
                }
            };
            date_max.ValueChanged += (sender, eventArgs) => {
                if (date_min.Value != null && date_max.Value < date_min.Value) {
                    date_max.Value = null;
                    WidgetUtils.ShowErrorPopUp("日期范围应为左早右晚！");
                }
            };

            CommonButton commonButton = _dataGridView.AddExtraButton("导出");
            commonButton.Click += (sender, eventArgs) => {
                WidgetUtils.ShowNoticePopUp("Export button has not been set.");
            };

            // 按钮逻辑
            _dataGridView.QueryData = (vo) => {
                List<OperationDataVO> vos = QueryList();
                return vos
                    .Where(o => vo.filter_create_time_min == null || vo.filter_create_time_max == null || o.create_time == null
                            || (DateTime.Compare(o.create_time.Value, vo.filter_create_time_min.Value) >= 0 
                                && DateTime.Compare(o.create_time.Value, vo.filter_create_time_max.Value) <= 0))
                    .ToList();
            };
            // 隐藏不需要的按钮 
            _dataGridView.AddNewButtonVisible = false;
            _dataGridView.ModifyButtonVisible = false;
            _dataGridView.DeleteButtonVisible = false;
        }
        #endregion

        #region Reusable methods
        #endregion

        #region Override methods
        protected override List<OperationDataVO> QueryList() {
            QueryOperationDataListRsp rsp = apis.QueryOperationDataList(new() {
                UserId = SystemUtils.LoggedUserId(),
            });
            _dataDTOList = rsp.OperationDataDTOs;
            List<OperationDataVO> vos = new();
            CommonUtils.ObjectConverter<OperationDataDTO, OperationDataVO>(_dataDTOList, vos);
            // TODO: can use BackgroundWorker to do this
            // 后续再优化数据加载时的延迟、卡顿问题，现在先不管
            // for (int i = 0; i < 5000; i++) {
            //     workstationVOs.Add(workstationVOs[0]);
            // }
            return vos;
        }
        protected override void AddOrUpdate(OperationDataDTO dto, Action action) {}
        protected override void Delete(List<int> ids) {}
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            Size contentSize = new(Width - Padding.Size.Width, Height - Padding.Size.Height);
            _dataGridView.Size = contentSize;
        }
        public override void VisibleToTrue() {
            System.Console.WriteLine($"========================================== VisibleToTrue");
            List<OperationDataField> operationDataFields = MainUtils.GetOperationDataFields();
            if (!_operationDataFields.SequenceEqual(operationDataFields)) {
                _operationDataFields = operationDataFields;
                _dataGridView.ResetColumnHeaders();
            }
            base.VisibleToTrue();
        }
        #endregion
    }
}
