using System.Reflection;
using CustomLibrary.Buttons;
using CustomLibrary.DateTimePickers;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using Newtonsoft.Json;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Extensions;
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
                    MainUtils.Settings.Write(IniFileKeys.DataStorageFieldsSortCurr, JsonConvert.SerializeObject(sortConfigCurr));
                    headers = fieldsConfig.Where(f => f.Visible).Select(f => f.FieldName).ToList();
                }
                // 组装数据
                List<Dictionary<int, object?>> dataWithConfigFields = new();
                List<OperationDataVO> dataFormatted = new();
                CommonUtils.ObjectConverter<OperationDataDTO, OperationDataVO>(_dataDTOList, dataFormatted);
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

            // 按钮逻辑
            _dataGridView.QueryData = (vo) => {
                List<OperationDataVO> vos = QueryList();
                return vos
                    .Where(o => vo.filter_create_time_min == null || vo.filter_create_time_max == null || o.create_time == null
                            || (DateTime.Compare(o.create_time.Value.Date, vo.filter_create_time_min.Value.Date) >= 0 
                                && DateTime.Compare(o.create_time.Value.Date, vo.filter_create_time_max.Value.Date) <= 0))
                    .ToList();
            };
            // 隐藏不需要的按钮 
            _dataGridView.AddNewButtonVisible = false;
            _dataGridView.ModifyButtonVisible = false;
            _dataGridView.DeleteButtonVisible = false;
        }
        #endregion

        #region Reusable methods
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
        #endregion

        #region Override methods
        protected override List<OperationDataVO> QueryList() {
            QueryOperationDataListRsp rsp = apis.QueryOperationDataList(new() {
                UserId = SystemUtils.LoggedUserId,
            });
            _dataDTOList = rsp.OperationDataDTOs;
            List<OperationDataVO> vos = new();
            CommonUtils.ObjectConverter<OperationDataDTO, OperationDataVO>(_dataDTOList, vos);
            vos.AddRange(vos);
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
