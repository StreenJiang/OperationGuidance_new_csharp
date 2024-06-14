using CustomLibrary.Buttons;
using CustomLibrary.ComboBoxes;
using CustomLibrary.Configs;
using CustomLibrary.DateTimePickers;
using CustomLibrary.Forms;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using log4net;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class DataQueryView_SCII: CustomDataGridViewOuterPanel<MissionRecordDTO, MissionRecordVO> {
        private ILog logger = MainUtils.GetLogger(typeof(DataQueryView_SCII));

        #region Fields
        // Apis
        private OperationGuidanceApis apis;
        private List<MissionRecordDTO> _dataDTOList;
        private List<OperationDataField> _operationDataFields;
        // DataGridView panel
        private DataGridViewGroup<MissionRecordVO> _dataGridView;
        // Add new pop up form
        private EditEntityPopUpForm<MissionRecordDTO> _editEntityPopUpForm;
        private List<WorkstationDTO> _workstations;
        private CustomComboBoxGroup<List<int?>> _workstationNameComboBox;
        #endregion

        #region Constructors
        public DataQueryView_SCII() {
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
            _dataGridView = new() {
                Parent = this,
            };
            _dataGridView.VoGridView.GridView.CellDoubleClick += (s, e) => {
                if (e.RowIndex >= 0) {
                    MissionRecordVO record = (MissionRecordVO) _dataGridView.VoGridView.GridView.Rows[e.RowIndex].DataBoundItem;
                    OpenDetailPopUp(record.id.Value);
                }
            };

            // 搜索条件
            _dataGridView.AddTextBox("总成码/追溯码", false, (MissionRecordVO vo, string? value) => vo.product_bar_code = value).Ratio = 6.25;
            _workstationNameComboBox = _dataGridView.AddComboBox("站点名称", (MissionRecordVO vo, List<int?>? value) => {
                vo.ids = new();
                if (value != null) {
                    value.ForEach(v => vo.ids.Add(v));
                } else {
                    vo.ids.Add(null);
                }
            }, new());
            RefreshWorkstationOptions();
            CustomDatePickerGroup dateFitler = _dataGridView.AddSeparateDatePicker("日期", "~",
                    (MissionRecordVO vo, DateTime? value) => vo.filter_create_time_min = value,
                    (MissionRecordVO vo, DateTime? value) => vo.filter_create_time_max = value);
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

            // 添加详情按钮
            CommonButton detailBtn = _dataGridView.AddExtraButton("详情");
            detailBtn.Click += (s, e) => {
                List<int> ids = _dataGridView.GetSelectedIds();
                if (ids.Count <= 0) {
                    WidgetUtils.ShowNoticePopUp("请选择要查看详情的数据。");
                } else if (ids.Count > 1) {
                    WidgetUtils.ShowNoticePopUp("每次只能查看一条数据的详情信息。");
                } else {
                    OpenDetailPopUp(ids[0]);
                }
            };

            // 按钮逻辑
            _dataGridView.QueryData = (vo) => DataFiltering(QueryList(), vo);
            // 隐藏不需要的按钮 
            _dataGridView.AddNewButtonVisible = false;
            _dataGridView.ModifyButtonVisible = false;
            _dataGridView.DeleteButtonVisible = false;

            void OpenDetailPopUp(int recordId) {
                List<OperationDataDTO> dataDTOs = apis.QueryOperationDataList(new() { MissionRecordId = recordId }).OperationDataDTOs;
                List<OperationDataVO> vos = new();
                CommonUtils.ObjectConverter<OperationDataDTO, OperationDataVO>(dataDTOs, vos);
                OpenOperationDataDetailsPopUpForm(vos);
            }
        }
        #endregion

        #region Reusable methods
        private void RefreshWorkstationOptions() {
            _workstations = apis.QueryWorkstationList(new(SystemUtils.MacAddressesDTO.id)).WorkstationsDTOs;
            Dictionary<int, List<int>> missionRecordIds = new();
            if (_workstations.Count > 0) {
                missionRecordIds = apis.QueryMissionRecordsByWorkstationIds(new(_workstations.Select(w => w.id).ToList())).MissionRecordsDict;
                _workstationNameComboBox.ClearItem();
                foreach (WorkstationDTO workstation in _workstations) {
                    if (missionRecordIds.ContainsKey(workstation.id)) {
                        List<int?> ids = new();
                        missionRecordIds[workstation.id].ForEach(id => ids.Add(id));
                        _workstationNameComboBox.AddItem(workstation.name, ids);
                    }
                }
                _workstationNameComboBox.AddItem("无", null);
            }
        }
        // 数据过滤（同时兼顾条件查询和数据导出）
        private List<MissionRecordVO> DataFiltering(List<MissionRecordVO> vos, MissionRecordVO vo) {
            vos = vos.Where(o => vo.filter_create_time_min == null || vo.filter_create_time_max == null || o.create_time == null
                        || (DateTime.Compare(o.create_time.Value.Date, vo.filter_create_time_min.Value.Date) >= 0
                        && DateTime.Compare(o.create_time.Value.Date, vo.filter_create_time_max.Value.Date) <= 0))
                    .Where(o => vo.product_bar_code == null || o.product_bar_code != null && o.product_bar_code.Contains(vo.product_bar_code))
                    .ToList();
            if (vo.ids != null) {
                vos = vos.Where(o => {
                    if (vo.ids.Count > 0 && vo.ids[0] == null && o.workstation_id == null) {
                        return true;
                    } else if (vo.ids.Contains(o.id)) {
                        return true;
                    }
                    return false;
                }).ToList();
            }
            return vos;
        }
        private void OpenOperationDataDetailsPopUpForm(List<OperationDataVO> vos) {
            CustomPopUpForm form = new() {
                Title = "数据详情 - 各个螺栓点位拧紧数据",
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            CommonButton closeButton = form.AddButton("关闭");
            closeButton.Click += (s, e) => {
                form.Dispose();
            };
            form.PretendToShowToCreateHandlesForChildren();

            DataGridViewGroup<OperationDataVO> gridViewGroup = new(gridView => {
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
                Parent = form.ContentPanel,
            };
            gridViewGroup.VoGridView.GridView.CellDoubleClick += (s, e) => {
                if (e.RowIndex >= 0) {
                    OperationDataVO record = (OperationDataVO) gridViewGroup.VoGridView.GridView.Rows[e.RowIndex].DataBoundItem;
                    CheckCurveData(record.id.Value);
                }
            };

            // 按钮逻辑
            gridViewGroup.QueryData = (vo) => OperationDataFiltering(vos, vo);
            // 隐藏不需要的按钮 
            gridViewGroup.SearchButtonVisible = false;
            gridViewGroup.ResetButtonVisible = false;
            gridViewGroup.AddNewButtonVisible = false;
            gridViewGroup.ModifyButtonVisible = false;
            gridViewGroup.DeleteButtonVisible = false;

            gridViewGroup.AddExtraButton("查看曲线").Click += (s, e) => {
                List<int> ids = gridViewGroup.GetSelectedIds();
                if (ids.Count <= 0) {
                    WidgetUtils.ShowNoticePopUp("请选择要查看曲线的数据。");
                } else if (ids.Count > 1) {
                    WidgetUtils.ShowNoticePopUp("每次只能查看一条数据的曲线信息。");
                } else {
                    CheckCurveData(ids[0]);
                }
            };

            int contentWidth = (int) (WidgetUtils.MainSize.Width * .85);
            int gridViewHeight = (int) (WidgetUtils.MainSize.Height * .65);
            // 感觉关闭按钮上面太空了，加一点高度
            gridViewGroup.Size = new(contentWidth - form.ContentPanel.Padding.Size.Width, gridViewHeight + form.ContentPanel.Padding.Size.Height / 4);
            form.SetContentSizeAndSelfSize(new(contentWidth, gridViewHeight + form.ContentPanel.Padding.Size.Height));
            form.Show();

            List<OperationDataVO> OperationDataFiltering(List<OperationDataVO> vos, OperationDataVO vo) {
                return vos;
            }

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
        protected override List<MissionRecordVO> QueryList() {
            QueryMissionRecordListRsp rsp = apis.QueryMissionRecordList(new());
            _dataDTOList = rsp.MissionRecordDTOs;
            List<MissionRecordVO> vos = new();
            CommonUtils.ObjectConverter<MissionRecordDTO, MissionRecordVO>(_dataDTOList, vos);

            // 查询站点信息，并给每个任务记录填上对应的站点
            List<int> missionRecordIds = vos.Select(vo => (int) vo.id).Distinct().ToList();
            Dictionary<int, Dictionary<int, string>> workstationInfos = apis.QueryWorkstationInfoByMissionRecordIds(new(missionRecordIds)).WorkstationInfos;
            vos.ForEach(vo => {
                if (workstationInfos.ContainsKey(vo.id.Value)) {
                    Dictionary<int, string> dict = workstationInfos[vo.id.Value];
                    vo.workstation_id = dict.Keys.ToList()[0];
                    vo.workstation_name = dict.Values.ToList()[0];
                }
            });

            // TODO: can use BackgroundWorker to do this
            // 后续再优化数据加载时的延迟、卡顿问题，现在先不管
            // for (int i = 0; i < 5000; i++) {
            //     workstationVOs.Add(workstationVOs[0]);
            // }
            return vos;
        }
        protected override void AddOrUpdate(MissionRecordDTO dto, Action action) { }
        protected override void Delete(List<int> ids) { }
        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            Size contentSize = new(Width - Padding.Size.Width, Height - Padding.Size.Height);
            _dataGridView.Size = contentSize;
        }
        public override void VisibleToTrue() {
            List<OperationDataField> operationDataFields = MainUtils.GetOperationDataFields();
            if (!_operationDataFields.SequenceEqual(operationDataFields)) {
                _operationDataFields = operationDataFields;
            }
            RefreshWorkstationOptions();
            base.VisibleToTrue();
        }
        #endregion
    }
}
