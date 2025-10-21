
using CustomLibrary.Configs;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Views.ReusableWidgets;

namespace OperationGuidance_new.Views {
    public class WorkplaceMissionView_SCII_XT: AWorkplaceMissionView<WorkplaceContentPanel_SCII_XT, WorkplaceTopBar_SCII> {
        public WorkplaceMissionView_SCII_XT() { }
        public WorkplaceMissionView_SCII_XT(bool operatorOpenning) : base(operatorOpenning) { }

        protected override WorkplaceContentPanel_SCII_XT GetWrokplacePanel(int? missionId, WorkplaceTopBar topBar) {
            return new(missionId, missionName => {
                topBar.Title = missionName;
            }) {
                BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND_2,
                Margin = new Padding(0),
                PaddingWithoutBorder = true,
                View = this,
            };
        }
    }

    public class WorkplaceContentPanel_SCII_XT: WorkplaceContentPanel_SCII {
        private WorkplaceMissionView_SCII_XT _view;
        public new WorkplaceMissionView_SCII_XT View { get => _view; set => _view = value; }

        public WorkplaceContentPanel_SCII_XT() { }
        public WorkplaceContentPanel_SCII_XT(int? missionId, Action<string> resetMissionName) : base(missionId, resetMissionName) { }

        protected override void OpenBarCodePopUpForm(string? barCode = null) {
            string batchNum = "";
            if (!_activated) {
                batchNum = _productBatch.GetTextBox(0).Box.Text;
                if (string.IsNullOrEmpty(batchNum)) {
                    WidgetUtils.ShowErrorPopUp("产品批次还没有填写");
                    if (_barCodePopUpForm != null && !_barCodePopUpForm.IsDisposed) {
                        _barCodePopUpForm.Hide();
                    }
                    _productBatch.GetTextBox(0).IsError = true;
                    _productBatch.GetTextBox(0).Box.Focus();
                    return;
                }
            }

            if (_barCodePopUpForm == null || _barCodePopUpForm.IsDisposed) {
                if (_activated && _currentWorkingBolt != null) {
                    _rulesExcluded = GetCurrentExcludedRules(_currentWorkingBolt.BoltDTO);
                } else {
                    _rulesExcluded = GetCurrentExcludedRules();
                }

                _barCodePopUpForm = new BarCodeInputPopUpForm_SCII_XT(this,
                                                                      ConfigsVariables.BAR_CODE_NOTE,
                                                                      _mission,
                                                                      _activated,
                                                                      _productBarCodeMatchingRules,
                                                                      _partsBarCodeMatchingRules,
                                                                      barCode,
                                                                      _rulesExcluded,
                                                                      CheckLockMsg(WorkingProcessPanel.LockedBoltBarCode),
                                                                      batchNum) {
                    Title = "录入条码",
                    BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
                };
                if (!_activated) {
                    _barCodePopUpForm.AddButton("激活任务").Click += (sender, eventArgs) => {
                        if (!_activated) {
                            if (!_barCodePopUpForm.CheckCanActivateMission()) {
                                CustomTextBox customTextBox = _barCodePopUpForm.ProductBarCodeBox.GetTextBox(0);
                                if (string.IsNullOrEmpty(_barCodeObj.ProductBarCode)) {
                                    customTextBox.IsError = true;
                                }
                                for (int i = 0; i < _barCodePopUpForm.PartsBarCodeContentPanel.Controls.Count; i++) {
                                    if (i >= _barCodeObj.PartsBarCodes.Count) {
                                        ((CustomTextBoxButtonGroup) _barCodePopUpForm.PartsBarCodeContentPanel.Controls[i]).GetTextBox(0).IsError = true;
                                    }
                                }
                                WidgetUtils.ShowWarningPopUp("条码录入完成后才可激活任务");
                            } else {
                                ActivateMission();
                                _barCodePopUpForm.Dispose();
                            }
                        } else {
                            _barCodePopUpForm.Dispose();
                        }
                    };
                }
                _barCodePopUpForm.AddButton("关闭").Click += (sender, eventArgs) => _barCodePopUpForm.Dispose();
                _barCodePopUpForm.PretendToShowToCreateHandlesForChildren();
                _barCodePopUpForm.ResizeSelf();
            }
            _barCodePopUpForm.Show();
        }
    }
}
