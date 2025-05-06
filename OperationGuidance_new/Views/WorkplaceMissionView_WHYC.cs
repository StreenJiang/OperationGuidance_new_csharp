using CustomLibrary.Configs;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using Newtonsoft.Json;
using OperationGuidance_new.Constants;
using OperationGuidance_new.HttpObjects.Requests;
using OperationGuidance_new.HttpObjects.Response;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Models.DTOs;
using Timer = System.Windows.Forms.Timer;

namespace OperationGuidance_new.Views {
    public class WorkplaceMissionView_WHYC: AWorkplaceMissionView<WorkplaceContentPanel_WHYC, WorkplaceTopBar> {
        public WorkplaceMissionView_WHYC() { }
        public WorkplaceMissionView_WHYC(bool operatorOpenning) : base(operatorOpenning) { }

        protected override WorkplaceContentPanel_WHYC GetWrokplacePanel(int? missionId, WorkplaceTopBar topBar) {
            return new(missionId, missionName => {
                topBar.Title = missionName;
            }) {
                BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND_2,
                Margin = new Padding(0),
            };
        }
    }

    public class WorkplaceContentPanel_WHYC: WorkplaceContentPanel {
        private CustomTextBoxGroup _lineBox;
        private CustomTextBoxGroup _operatorBox;

        private const int _delay = 1500;
        private Timer _lineTimer;
        private Timer _operatorTimer;

        public WorkplaceContentPanel_WHYC() { }
        public WorkplaceContentPanel_WHYC(int? missionId, Action<string> resetMissionName) : base(missionId, resetMissionName) {
            _lineBox = new("线体") {
                Ratio = 7.5,
                NameAlignment = HorizontalAlignment.Right,
            };
            _operatorBox = new("操作人员") {
                Ratio = 7.5,
                NameAlignment = HorizontalAlignment.Right,
            };

            _lineBox.SetValue(0, MainUtils.GetLine_WHYC());
            _operatorBox.SetValue(0, MainUtils.GetOperator_WHYC());

            _lineTimer = new();
            _lineTimer.Interval = _delay;
            _lineTimer.Tick += (s, e) => {
                string lineText = _lineBox.GetTextBox(0).Box.Text;
                if (!string.IsNullOrEmpty(lineText)) {
                    MainUtils.SetLine_WHYC(lineText);
                }
                _lineTimer.Stop();
            };
            _operatorTimer = new();
            _operatorTimer.Interval = _delay;
            _operatorTimer.Tick += (s, e) => {
                string operatorText = _operatorBox.GetTextBox(0).Box.Text;
                if (!string.IsNullOrEmpty(operatorText)) {
                    MainUtils.SetOperator_WHYC(operatorText);
                }
                _operatorTimer.Stop();
            };

            _lineBox.GetTextBox(0).Box.TextChanged += (s, e) => {
                // Debounce
                if (_lineTimer.Enabled) {
                    _lineTimer.Stop();
                }
                _lineTimer.Start();
            };
            _operatorBox.GetTextBox(0).Box.TextChanged += (s, e) => {
                // Debounce
                if (_operatorTimer.Enabled) {
                    _operatorTimer.Stop();
                }
                _operatorTimer.Start();
            };

            _topRightBottom.Controls.Add(_lineBox);
            _topRightBottom.Controls.Add(_operatorBox);
        }

        protected override void OpenBarCodePopUpForm(string? barCode = null) {
            if (!_activated) {
                string line = _lineBox.GetTextBox(0).Box.Text;
                if (string.IsNullOrEmpty(line)) {
                    WidgetUtils.ShowErrorPopUp("线体还没有填写");
                    if (_barCodePopUpForm != null && !_barCodePopUpForm.IsDisposed) {
                        _barCodePopUpForm.Hide();
                    }
                    _lineBox.GetTextBox(0).IsError = true;
                    _lineBox.GetTextBox(0).Box.Focus();
                    return;
                }

                string operatorName = _operatorBox.GetTextBox(0).Box.Text;
                if (string.IsNullOrEmpty(operatorName)) {
                    WidgetUtils.ShowErrorPopUp("操作人员还没有填写");
                    if (_barCodePopUpForm != null && !_barCodePopUpForm.IsDisposed) {
                        _barCodePopUpForm.Hide();
                    }
                    _operatorBox.GetTextBox(0).IsError = true;
                    _operatorBox.GetTextBox(0).Box.Focus();
                    return;
                }
            }
            _lineBox.GetTextBox(0).IsError = false;
            _operatorBox.GetTextBox(0).IsError = false;

            base.OpenBarCodePopUpForm();
        }

        protected override async Task<bool> ValidationBeforeActivatingMission() {
            if (!await base.ValidationBeforeActivatingMission()) {
                return false;
            } else {
                string getMatCodeUri = MainUtils.GetMatCodeApi();

                // Check uri
                if (!string.IsNullOrEmpty(getMatCodeUri)) {
                    // Send http request to get mat code
                    HttpRequestGetMatCode request = new(_barCodeObj.ProductBarCode);
                    HttpResponseGetMatCode response = await HttpUtils.SendPost<HttpRequestGetMatCode, HttpResponseGetMatCode>(getMatCodeUri, request);
                    logger.Info($"ucData = [{JsonConvert.SerializeObject(response.ucData)}]");

                    if (response.unStatus == HttpStatus_WHYC.FAILURE) {
                        WidgetUtils.ShowErrorPopUp($"请求MatCode失败，返回信息：{response.ucMsg}");
                        return false;
                    } else if (response.ucData == null || response.ucData.MatCode == null) {
                        WidgetUtils.ShowErrorPopUp($"获取到的MatCode为空！");
                        return false;
                    }

                    _matCode = response.ucData.MatCode;
                }
            }

            return true;
        }

        private async void UploadDataToMES(OperationDataDTO operationDataDTO) {
            string uploadDataUri = MainUtils.GetUploadDataApi();

            // Check uri
            if (!string.IsNullOrEmpty(uploadDataUri)) {
                WorkstationDTO workstationDTO = _workstationsDTOs.Single(dto => dto.id == operationDataDTO.workstation_id);

                // Send http request to upload data to MES
                HttpRequestUploadData request = new() {
                    QrCode = _barCodeObj.ProductBarCode,
                    Line = _lineBox.GetTextBox(0).Box.Text,
                    StationName = workstationDTO.name,
                    StaffName = _operatorBox.GetTextBox(0).Box.Text,
                    MatCode = _matCode,
                    Trosion = $"{_round_3(operationDataDTO.torque)}N.m",
                    TrosionStd = $"{_round_3(operationDataDTO.torque_final_target)}N.m",
                    TrosionUp = $"{_round_3(operationDataDTO.torque_max_limit)}N.m",
                    TrosionDow = $"{_round_3(operationDataDTO.torque_min_limit)}N.m",
                    Time = $"{_round_3((float) _rundownTime / 1000)}s",
                    Circle = $"{_round_3((float) operationDataDTO.angle / 360)}圈",
                    Angle = $"{operationDataDTO.angle}°",
                    Result = ((TighteningStatus) operationDataDTO.tightening_status.Value).ToString(),
                    Error = _errorMsg,
                    CreateTime = DateTime.Now.ToString(MainUtils.DATETIME_FORMAT_YYYY_MM_DD_HH_MM_SS_FFF),
                    Seq = operationDataDTO.bolt_serial_num,
                    SumQty = _sumBoltDone,
                };

                HttpResponseUploadData response = await HttpUtils.SendPost<HttpRequestUploadData, HttpResponseUploadData>(uploadDataUri, request);
                if (response.unStatus == HttpStatus_WHYC.FAILURE) {
                    WidgetUtils.ShowErrorPopUp($"上传数据失败，返回信息：{response.ucMsg}");
                }
            }

            string _round_3<T>(T? num) {
                if (num != null && num is int numInt) {
                    return Math.Round((decimal) numInt, 3).ToString("0.000");
                }

                if (num != null && num is float numFloat) {
                    return Math.Round((decimal) numFloat, 3).ToString("0.000");
                }

                return "0.000";
            }
        }

        protected override void StoreTighteningData(OperationDataDTO operationDataDTO) {
            base.StoreTighteningData(operationDataDTO);
            UploadDataToMES(operationDataDTO);
        }

        // Action after receving bar code msg
        private void ActionAfterRecevingBarCode(string msg) {
            if (!IsDisposed && !_activated) {
                // 交给弹窗处理
                if (_barCodePopUpForm == null || _barCodePopUpForm.IsDisposed) {
                    OpenBarCodePopUpForm(msg);
                } else {
                    _barCodePopUpForm.ValidateBarCode(msg);
                }
            }
        }
    }
}
