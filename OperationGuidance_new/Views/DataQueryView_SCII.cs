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
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Requests;
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
        private CustomComboBoxGroup<bool?> _isChallengMissionComboBox;
        private List<ProductMissionDTO> _missions;
        private Dictionary<int, Dictionary<int, string>> _workstationInfoCache;
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
            _dataGridView.AddTextBox("总成码/追溯码", false, (MissionRecordVO vo, string? value) => vo.product_bar_code = value);
            _dataGridView.AddTextBox("物料码", false, (MissionRecordVO vo, string? value) => vo.parts_bar_code = value);
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
            _dataGridView.AddTextBox("任务名称", false, (MissionRecordVO vo, string? value) => vo.mission_name = value);
            _workstationNameComboBox = _dataGridView.AddComboBox("站点名称", (MissionRecordVO vo, List<int?>? value) => {
                vo.ids = new();
                if (value != null) {
                    value.ForEach(v => vo.ids.Add(v));
                } else {
                    vo.ids.Add(null);
                }
            }, new());
            Dictionary<String, bool?> yesOrNos = new() {
                { "是", true }, { "否", false }
            };
            _isChallengMissionComboBox = _dataGridView.AddComboBox("是否挑战任务", (MissionRecordVO vo, bool? value) => vo.is_challenge_mission = value, yesOrNos);
            _isChallengMissionComboBox.SelectedTop = false;
            int indexTemp = 0;
            for (; indexTemp < _isChallengMissionComboBox.Items.Count; indexTemp++) {
                if (_isChallengMissionComboBox.Items[indexTemp] == false) {
                    break;
                }
            }
            _isChallengMissionComboBox.SetCurrent(indexTemp);
            RefreshWorkstationOptions();

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
            _dataGridView.QueryData = (vo) => {
                _missions = apis.QueryProductMissions(new(SystemUtils.MacAddressesDTO.id) {
                    Role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId)
                }).ProductMissionsDTOs;
                _workstationInfoCache = new();
                _dataGridView.VoGridView.ServerFetch = (page, pageSize) => {
                    var pageReq = BuildQueryMissionRecordListReq(page, pageSize, vo);
                    var pageRsp = apis.QueryMissionRecordList(pageReq);
                    var pageVos = new List<MissionRecordVO>();
                    CommonUtils.ObjectConverter<MissionRecordDTO, MissionRecordVO>(pageRsp.MissionRecordDTOs, pageVos);
                    EnrichMissionRecordVOs(pageVos);
                    return (pageVos, pageRsp.TotalCount);
                };
                var req = BuildQueryMissionRecordListReq(1, _dataGridView.VoGridView.PageSize, vo);
                var rsp = apis.QueryMissionRecordList(req);
                _dataDTOList = rsp.MissionRecordDTOs;
                var vos = new List<MissionRecordVO>();
                CommonUtils.ObjectConverter<MissionRecordDTO, MissionRecordVO>(_dataDTOList, vos);
                EnrichMissionRecordVOs(vos);
                _dataGridView.VoGridView.SetServerDataSource(vos, rsp.TotalCount);
                return vos;
            };
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
        private QueryMissionRecordListReq BuildQueryMissionRecordListReq(int? page, int? pageSize, MissionRecordVO vo) {
            return new() {
                Page = page,
                PageSize = pageSize,
                ProductBarCode = vo.product_bar_code,
                PartsBarCode = vo.parts_bar_code,
                CreateTimeMin = vo.filter_create_time_min,
                CreateTimeMax = vo.filter_create_time_max,
                MissionName = vo.mission_name,
                IsChallengeMission = vo.is_challenge_mission,
                Ids = (vo.ids != null && vo.ids.Count > 0 && vo.ids[0] != null)
                    ? vo.ids.Where(i => i.HasValue).Select(i => i.Value).ToList()
                    : null,
            };
        }
        private void EnrichMissionRecordVOs(List<MissionRecordVO> vos) {
            if (vos.Count == 0) return;
            // 查询站点信息（优先从缓存取，未命中的批量查询后写入缓存）
            List<int> missionRecordIds = vos.Select(vo => (int) vo.id).Distinct().ToList();
            List<int> uncachedIds = missionRecordIds.Where(id => !_workstationInfoCache.ContainsKey(id)).ToList();
            if (uncachedIds.Count > 0) {
                var fetched = apis.QueryWorkstationInfoByMissionRecordIds(new(uncachedIds)).WorkstationInfos;
                foreach (var kv in fetched) {
                    _workstationInfoCache[kv.Key] = kv.Value;
                }
            }
            vos.ForEach(vo => {
                if (_workstationInfoCache.TryGetValue(vo.id.Value, out var dict)) {
                    vo.workstation_id = dict.Keys.ToList()[0];
                    vo.workstation_name = dict.Values.ToList()[0];
                }
            });
            // 填充任务名称和挑战任务标识
            var missionIds = vos.Select(v => v.mission_id).Distinct().ToList();
            var missions = _missions.Where(m => missionIds.Contains(m.id)).ToList();
            vos.ForEach(vo => {
                ProductMissionDTO? mission = missions.SingleOrDefault(m => m.id == vo.mission_id);
                if (mission != null) {
                    vo.mission_name = mission.name;
                    vo.is_challenge_mission = mission.is_challenge_mission == (int) YesOrNo.YES;
                }
            });
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

            _missions = apis.QueryProductMissions(new(SystemUtils.MacAddressesDTO.id) { Role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId) }).ProductMissionsDTOs;
            _missions = _missions.Where(m => vos.Select(v => v.mission_id).Distinct().ToList().Contains(m.id)).ToList();
            vos.ForEach(vo => {
                ProductMissionDTO? productMissionDTO = _missions.SingleOrDefault(m => m.id == vo.mission_id);
                if (productMissionDTO != null) {
                    vo.mission_name = productMissionDTO.name;
                    vo.is_challenge_mission = productMissionDTO.is_challenge_mission == (int) YesOrNo.YES;
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
