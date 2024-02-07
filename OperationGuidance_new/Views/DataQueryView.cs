using CustomLibrary.Buttons;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
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
            _dataGridView = new() {
                Parent = this,
            };
            // 搜索条件
            CustomTextBoxGroup dateFitler = _dataGridView.AddSeparateTextBox("日期", "~", false, 
                    (OperationDataVO vo, DateTime? value) => vo.filter_create_time_min = value, 
                    (OperationDataVO vo, DateTime? value) => vo.filter_create_time_max = value);
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
