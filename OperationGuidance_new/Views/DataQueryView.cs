using CustomLibrary.Buttons;
using CustomLibrary.ComboBoxes;
using CustomLibrary.DateTimePickers;
using CustomLibrary.Panels.AbstractClasses;
using CustomLibrary.Utils;
using log4net;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Extensions;
using OperationGuidance_new.Utils;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;
using System.Reflection;

namespace OperationGuidance_new.Views {
    public class DataQueryView: ACustomDataGridViewOuterPanel<OperationDataDTO, OperationDataVO> {
        private ILog logger = MainUtils.GetLogger(typeof(DataQueryView));

        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        private List<OperationDataDTO> _dataDTOList;
        private int _totalCount;
        private List<OperationDataField> _operationDataFields;
        // DataGridView panel
        private DataGridViewGroup<OperationDataVO> _dataGridView;
        // Add new pop up form
        private EditEntityPopUpForm<OperationDataDTO> _editEntityPopUpForm;
        private List<WorkstationDTO> _workstations;
        private CustomComboBoxGroup<int?> _workstationNameComboBox;
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
                DataGridViewColumn[] columnRange = { };
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
            };
            _dataGridView.VoGridView.GridView.CellDoubleClick += (s, e) => {
                if (e.RowIndex >= 0) {
                    OperationDataVO record = (OperationDataVO) _dataGridView.VoGridView.GridView.Rows[e.RowIndex].DataBoundItem;
                    CheckCurveData(record.id.Value);
                }
            };

            // 搜索条件
            // 搜索条件 - 条码
            _dataGridView.AddTextBox("条码", false, (OperationDataVO vo, string? value) => vo.vin_number = value);

            // 搜索条件 - 站点名称
            _workstationNameComboBox = _dataGridView.AddComboBox("站点名称", (OperationDataVO vo, int? value) => vo.workstation_id = value, new());
            RefreshWorkstationOptions();

            // 搜索条件 - 日期
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

            CommonButton exportBtn = _dataGridView.AddExtraButton("导出");
            exportBtn.Click += (sender, eventArgs) => {
                string filePath = ShowSaveFileDialog();
                List<string>? headers = null;
                // 检查当前文件是否存在
                bool excelFileExists = File.Exists(filePath);
                // 从配置文件读取配置
                List<int> sortConfig = MainUtils.GetSortConfig();
                List<int>? sortConfigCurr = MainUtils.GetSortConfigCurr();
                List<OperationDataField> fieldsConfig = MainUtils.GetOperationDataFields(sortConfigCurr);
                List<string> propertyNames = fieldsConfig.Where(f => f.Visible).Select(f => f.PropertyName).ToList();
                // 检查当前是否存在正在使用的字段配置
                if (sortConfigCurr == null || !sortConfig.SequenceEqual(sortConfigCurr) || !excelFileExists) {
                    sortConfigCurr = sortConfig;
                    MainUtils.SetSortConfigCurr(sortConfigCurr);
                    headers = fieldsConfig.Where(f => f.Visible).Select(f => f.FieldName).ToList();
                }
                // 组装数据 
                List<Dictionary<int, object?>> dataWithConfigFields = new();
                List<OperationDataVO> dataFormatted = new();
                CommonUtils.ObjectConverter<OperationDataDTO, OperationDataVO>(QueryList(), dataFormatted);
                // 根据过滤条件过滤数据
                dataFormatted = DataFiltering(dataFormatted, _dataGridView.FilterParametersVO);
                // 先根据每个字段的排序，将排序值和数据值作为一个dictionary存入一个集合
                dataFormatted.ForEach(dto => {
                    Dictionary<int, object?> record = new();
                    for (int i = 0; i < propertyNames.Count; i++) {
                        string pName = propertyNames[i];
                        PropertyInfo? propertyInfo = dto.GetType().GetProperty(pName);
                        if (propertyInfo != null) {
                            record.Add(i, propertyInfo.GetValue(CommonUtils.CannotBeNull(dto)));
                        }
                    }
                    dataWithConfigFields.Add(record);
                });
                // 组装最终数据
                List<List<object?>> finalData = new();
                dataWithConfigFields.ForEach(dict => {
                    IOrderedEnumerable<KeyValuePair<int, object?>> orderedEnumerable = from pair in dict orderby pair.Key select pair;
                    finalData.Add(orderedEnumerable.Select(pair => pair.Value).ToList());
                });
                // 写入数据
                finalData.ExportToExcelFile(headers, filePath, excelFileExists);
            };

            _dataGridView.VoGridView.QueryPagedData = true;
            _dataGridView.VoGridView.QueryList = QueryListWithPagination;
            _dataGridView.VoGridView.GetTotalFromDB = () => _totalCount;

            // 按钮逻辑
            _dataGridView.QueryData = (vo) => new();
            // 隐藏不需要的按钮 
            _dataGridView.AddNewButtonVisible = false;
            _dataGridView.ModifyButtonVisible = false;
            _dataGridView.DeleteButtonVisible = false;

            _dataGridView.AddExtraButton("查看曲线").Click += (s, e) => {
                List<int> ids = _dataGridView.GetSelectedIds();
                if (ids.Count <= 0) {
                    WidgetUtils.ShowNoticePopUp("请选择要查看曲线的数据。");
                } else if (ids.Count > 1) {
                    WidgetUtils.ShowNoticePopUp("每次只能查看一条数据的曲线信息。");
                } else {
                    CheckCurveData(ids[0]);
                }
            };

            void CheckCurveData(int operationDataId) {
                OperationDataDTO? operationDataDTO = apis.FindOperationDataById(new(operationDataId)).OperationDataDTO;
                if (operationDataDTO != null) {
                    List<CurveDataDTO> curveDataDTOs = apis.FindCurveDataByOperationDataId(new(operationDataId)).CurveDataDTOs;
                    CurveDataDTO? angleCurve = curveDataDTOs.Find(c => c.data_type == (int) CurveDataType.ANGLE);
                    CurveDataDTO? torqueCurve = curveDataDTOs.Find(c => c.data_type == (int) CurveDataType.TORQUE);
                    OpenOperationDataDetailsPopUpForm(operationDataDTO.bolt_serial_num.Value, angleCurve, torqueCurve);
                } else {
                    string errorMsg = $"Can't find operation data by id = {operationDataId}, please check";
                    logger.Error(errorMsg);
                    throw new NullReferenceException(errorMsg);
                }
            }
        }
        #endregion

        #region Reusable methods
        private void RefreshWorkstationOptions() {
            _workstations = apis.QueryWorkstationList(new(SystemUtils.MacAddressesDTO.id)).WorkstationsDTOs;
            _workstationNameComboBox.ClearItem();
            foreach (WorkstationDTO workstation in _workstations) {
                _workstationNameComboBox.AddItem(workstation.name, workstation.id);
            }
        }
        // 数据过滤（同时兼顾条件查询和数据导出）
        private List<OperationDataVO> DataFiltering(List<OperationDataVO> vos, OperationDataVO vo) {
            return vos
                .Where(o => vo.vin_number == null || o.vin_number != null && o.vin_number.Contains(vo.vin_number))
                .Where(o => vo.filter_create_time_min == null || vo.filter_create_time_max == null || o.create_time == null
                        || (DateTime.Compare(o.create_time.Value.Date, vo.filter_create_time_min.Value.Date) >= 0
                            && DateTime.Compare(o.create_time.Value.Date, vo.filter_create_time_max.Value.Date) <= 0))
                .ToList();
        }
        // 选择保存路径
        private string ShowSaveFileDialog() {
            string localFilePath = "";
            //string localFilePath, fileNameExt, newFileName, FilePath; 
            SaveFileDialog sfd = new SaveFileDialog();
            // 设置默认文件名
            sfd.FileName = $"OperationData_{DateTime.Now.ToString(MainUtils.DATETIME_FORMAT_FULL_NO_PUNCTUATION)}";
            // 设置文件类型 
            sfd.Filter = "Excel File（*.xlsx）|*.xlsx";
            // 设置默认文件类型显示顺序 
            sfd.FilterIndex = 1;
            // 保存对话框是否记忆上次打开的目录 
            sfd.RestoreDirectory = true;
            // 点了保存按钮进入 
            if (sfd.ShowDialog() == DialogResult.OK) {
                localFilePath = sfd.FileName.ToString(); // 获得文件路径 
                string fileNameExt = localFilePath.Substring(localFilePath.LastIndexOf("\\") + 1); // 获取文件名，不带路径

                // 获取文件路径，不带文件名 
                //FilePath = localFilePath.Substring(0, localFilePath.LastIndexOf("\\")); 

                // 给文件名前加上时间 
                //newFileName = DateTime.Now.ToString("yyyyMMdd") + fileNameExt; 

                // 在文件名里加字符 
                //saveFileDialog1.FileName.Insert(1,"dameng"); 

                // System.IO.FileStream fs = (System.IO.FileStream)sfd.OpenFile();//输出文件 

                ////fs输出带文字或图片的文件，就看需求了 
            }
            return localFilePath;
        }
        private void OpenOperationDataDetailsPopUpForm(int boltSerialNum, CurveDataDTO? angleCurve, CurveDataDTO? torqueCurve) {
            CurvePopUpForm curveForm = new(boltSerialNum, angleCurve, torqueCurve);

            curveForm.PretendToShowToCreateHandlesForChildren();
            int contentWidth = (int) (WidgetUtils.MainSize.Width * .85);
            int gridViewHeight = (int) (WidgetUtils.MainSize.Height * .65);
            // 感觉关闭按钮上面太空了，加一点高度
            curveForm.Chart.Size = new(contentWidth - curveForm.ContentPanel.Padding.Size.Width, gridViewHeight + curveForm.ContentPanel.Padding.Size.Height / 4);
            curveForm.SetContentSizeAndSelfSize(new(contentWidth, gridViewHeight + curveForm.ContentPanel.Padding.Size.Height));
            curveForm.Show();
        }
        #endregion

        #region Override methods
        protected override List<OperationDataVO> QueryList() {
            QueryOperationDataListRsp rsp = apis.QueryOperationDataList(new());
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

        // 【分页查询】新增分页查询方法，支持搜索条件
        private List<OperationDataVO> QueryListWithPagination() {
            var req = new QueryOperationDataListReq {
                PageNumber = _dataGridView.VoGridView.CurrentPage,
                PageSize = _dataGridView.VoGridView.PageSize,
            };

            var VinNumber = _dataGridView.FilterParametersVO.vin_number;
            var WorkstationId = _dataGridView.FilterParametersVO.workstation_id;
            var StartDate = ((OperationDataVO) _dataGridView.FilterParametersVO).filter_create_time_min;
            var EndDate = ((OperationDataVO) _dataGridView.FilterParametersVO).filter_create_time_max;

            if (!string.IsNullOrEmpty(VinNumber)) {
                req.VinNumber = VinNumber;
            }
            if (WorkstationId.HasValue) {
                req.WorkstationId = WorkstationId;
            }
            if (!string.IsNullOrEmpty(StartDate?.ToString(MainUtils.DATETIME_FORMAT_YYYY_MM_DD))) {
                req.StartDate = StartDate;
            }
            if (!string.IsNullOrEmpty(EndDate?.ToString(MainUtils.DATETIME_FORMAT_YYYY_MM_DD))) {
                req.EndDate = EndDate;
            }

            QueryOperationDataListRsp rsp = apis.QueryOperationDataList(req);
            _dataDTOList = rsp.OperationDataDTOs;
            _totalCount = rsp.TotalCount;
            List<OperationDataVO> vos = new();
            CommonUtils.ObjectConverter<OperationDataDTO, OperationDataVO>(_dataDTOList, vos);
            return vos;
        }

        // 【分页查询】新增全量查询方法，用于导出功能
        private List<OperationDataVO> QueryListForExport(string? vinNumber = null, int? workstationId = null, DateTime? startDate = null, DateTime? endDate = null) {
            var req = new QueryOperationDataListReq {
                PageNumber = 1,
                PageSize = int.MaxValue, // 获取所有数据
                VinNumber = vinNumber,
                WorkstationId = workstationId,
                StartDate = startDate,
                EndDate = endDate
            };

            QueryOperationDataListRsp rsp = apis.QueryOperationDataList(req);
            _dataDTOList = rsp.OperationDataDTOs;
            List<OperationDataVO> vos = new();
            CommonUtils.ObjectConverter<OperationDataDTO, OperationDataVO>(_dataDTOList, vos);
            return vos;
        }
        protected override void AddOrUpdate(OperationDataDTO dto, Action action) { }
        protected override void Delete(List<int> ids) { }
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            Size contentSize = new(Width - Padding.Size.Width, Height - Padding.Size.Height);
            _dataGridView.Size = contentSize;
        }
        public override void VisibleToTrue() {
            List<OperationDataField> operationDataFields = MainUtils.GetOperationDataFields();
            if (!_operationDataFields.SequenceEqual(operationDataFields)) {
                _operationDataFields = operationDataFields;
                _dataGridView.ResetColumnHeaders();
            }
            RefreshWorkstationOptions();
            // base.VisibleToTrue();
        }
        #endregion
    }
}
