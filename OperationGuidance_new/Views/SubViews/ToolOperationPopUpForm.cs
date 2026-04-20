using CustomLibrary.Buttons;
using CustomLibrary.ComboBoxes;
using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using log4net;
using OperationGuidance_new.Extensions;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using System.Collections;

namespace OperationGuidance_new.Views.SubViews {
    public class ToolOperationPopUpForm: CustomPopUpForm {
        private ILog logger = MainUtils.GetLogger(typeof(ToolOperationPopUpForm));

        private List<WorkstationDTO> _workstationDTOs;
        private Dictionary<int, ToolTask> _toolTasks;
        private AWorkplaceContentPanel _workplace;
        private Action? _setPset;
        private BoltButton? _currentWorkingBolt;
        private Dictionary<int, BoltButton> _currentWorkingBoltIndependence;
        private bool _isMultiDeviceIndependenceMode;

        private TableLayoutPanel _tablePanel;
        private int _boxHeight;
        private int _boxMargin;
        private CustomComboBoxGroup<int> _stationComboBox;
        private CustomTextBoxGroup _parameterSetTextBox;
        private FunctionButton _btnLock;
        private FunctionButton _btnUnlock;
        private CommonButton _btnPSet;

        // 重试配置
        private readonly int _maxRetryTimes = 5;
        private readonly int _baseRetryDelayMs = 1000;

        public TableLayoutPanel TablePanel { get => _tablePanel; set => _tablePanel = value; }
        public Action? SetPset { get => _setPset; set => _setPset = value; }
        public int BoxHeight { get => _boxHeight; set => _boxHeight = value; }
        public int BoxMargin { get => _boxMargin; set => _boxMargin = value; }
        public FunctionButton BtnLock { get => _btnLock; set => _btnLock = value; }
        public FunctionButton BtnUnlock { get => _btnUnlock; set => _btnUnlock = value; }
        public CommonButton BtnPSet { get => _btnPSet; set => _btnPSet = value; }

        public ToolOperationPopUpForm(BoltButton? currentWorkingBolt, Dictionary<int, BoltButton> currentWorkingBoltIndependence,
                bool isMultiDeviceIndependenceMode, string categoryName, AWorkplaceContentPanel workplace,
                List<WorkstationDTO> workstationDTOs, Dictionary<int, ToolTask> toolTasks, int? currentWorkstationId, Action? setPset) {
            _currentWorkingBolt = currentWorkingBolt;
            _currentWorkingBoltIndependence = currentWorkingBoltIndependence;
            _isMultiDeviceIndependenceMode = isMultiDeviceIndependenceMode;
            _workstationDTOs = workstationDTOs;
            _toolTasks = toolTasks;
            _workplace = workplace;
            _setPset = setPset;

            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "手动操作 - " + categoryName + "";

            _tablePanel = new() {
                Margin = new(0),
                Padding = new(0),
                ColumnCount = 2,
                Parent = ContentPanel,
            };

            Dictionary<string, int> workstationOptions = _workstationDTOs.ToDictionary(dto => CommonUtils.CannotBeNull(dto.name), dto => dto.id);
            _stationComboBox = new("站点") {
                Parent = _tablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
            };
            foreach (KeyValuePair<string, int> pair in workstationOptions) {
                _stationComboBox.AddItem(pair.Key, pair.Value);
            }
            _parameterSetTextBox = new("程序") {
                Parent = _tablePanel,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                PositiveIntOnly = true,
            };
            _parameterSetTextBox.SetValue(0, "1");

            _btnLock = AddButton("锁枪");
            _btnLock.Click += (s, e) => {
                SendCommand(async toolTask => {
                    _workplace.RemoveInformationMsg(WorkingProcessPanel.UnlockedManually);
                    _workplace.AddLockMsg(WorkingProcessPanel.LockedManually);
                    toolTask.ForceSendLock();

                    await Task.Delay(500);
                    if (toolTask.Locked) {
                        WidgetUtils.ShowNoticePopUp("操作成功！");
                    } else {
                        WidgetUtils.ShowErrorPopUp($"操作失败！可能原因：\r\n1. 设备未连接\r\n2. 未给当前工具型号配置命令");
                    }
                });
            };
            _btnUnlock = AddButton("解锁");
            _btnUnlock.Click += (s, e) => {
                SendCommand(async toolTask => {
                    _workplace.ClearLockMsgs();
                    _workplace.AddInformationMsg(WorkingProcessPanel.UnlockedManually);
                    toolTask.ForceSendUnlock();

                    await Task.Delay(500);
                    if (!toolTask.Locked) {
                        WidgetUtils.ShowNoticePopUp("操作成功！");
                    } else {
                        string parameterSet = _parameterSetTextBox.GetTextBox(0).Text;
                        WidgetUtils.ShowErrorPopUp($"操作失败！可能原因：\r\n1. 设备未连接\r\n2. 未给当前工具型号配置命令\r\n3. 控制器未配置【程序{parameterSet}】");
                    }
                });
            };
            _btnPSet = AddButton("下发");
            _btnPSet.Click += (s, e) => {
                if (!IsHandleCreated) {
                    return;
                }

                SendCommand(async toolTask => {
                    string parameterSet = _parameterSetTextBox.GetTextBox(0).Text;
                    int pset = int.Parse(parameterSet);

                    // 禁用按钮，防止重复点击
                    _btnPSet.Enabled = false;

                    try {
                        // === 快速检查设备连接状态 ===
                        if (!toolTask.Connected) {
                            this.SafeInvoke(() => {
                                WidgetUtils.ShowErrorPopUp($"程序号 {pset} 下发失败！\n\n" +
                                    $"设备未连接，无法执行操作");
                            });
                            return;
                        }

                        // === 使用自动重试策略 ===
                        var retryStrategy = RetryStrategy.IncrementalDelay(_maxRetryTimes, _baseRetryDelayMs);

                        bool success = await retryStrategy.ExecuteAsync(
                            async () => {
                                // 执行程序号下发
                                return await toolTask.SendPSetAsync(pset);
                            },
                            null,
                            () => {
                                this.SafeInvoke(() => {
                                    // === 下发成功 ===
                                    WidgetUtils.ShowNoticePopUp($"程序号 {pset} 下发成功！");

                                    // 更新螺栓状态
                                    BoltButton? boltButton = null;
                                    int workstationId = _stationComboBox.Value;
                                    if (_isMultiDeviceIndependenceMode && _currentWorkingBoltIndependence.ContainsKey(workstationId)) {
                                        boltButton = _currentWorkingBoltIndependence[workstationId];
                                    } else {
                                        boltButton = _currentWorkingBolt;
                                    }

                                    if (boltButton != null) {
                                        boltButton.CurrentParameterSet = pset;
                                        _workplace.RemoveLockMsg(WorkingProcessPanel.LockedPsetFailed);
                                        _workplace.RemoveLockMsg(WorkingProcessPanel.LockedPsetNull);
                                        if (_setPset != null) {
                                            _setPset();
                                        }
                                        // 如果当前没有点位，则代表任务未激活，因此不关闭弹窗
                                        Dispose();
                                    }
                                });
                            },
                            null,
                            CancellationToken.None);
                        if (!success) {
                            // === 失败后显示错误提示（保持原有错误提示） ===
                            this.SafeInvoke(() => {
                                WidgetUtils.ShowErrorPopUp($"程序号 {pset} 下发失败！\n\n" +
                                    $"已自动重试 {_maxRetryTimes} 次，可能原因：\n" +
                                    $"1. 未给当前工具型号配置命令\n" +
                                    $"2. 控制器未配置【程序{parameterSet}】\n" +
                                    $"3. 【控制器-虚拟站-任务】未配置为【source tightening】");
                            });
                        }
                    } finally {
                        // 恢复按钮状态
                        this.SafeInvoke(() => {
                            _btnPSet.Enabled = true;
                        });
                    }
                });
            };
            CommonButton btnClose = AddButton("关闭");
            btnClose.Click += (s, e) => {
                Dispose();
            };
        }

        private void SendCommand(Action<ToolTask> aciont) {
            if (_stationComboBox.IsDefaultValue()) {
                WidgetUtils.ShowErrorPopUp("操作失败！请选择需要操作的工具所在的站点！");
            } else {
                int workstationId = _stationComboBox.Value;
                WorkstationDTO workstationDTO = _workstationDTOs.Single(dto => dto.id == workstationId);
                if (workstationDTO.tool_id == null) {
                    WidgetUtils.ShowErrorPopUp("操作失败！当前选择的站点没有配置工具，请检查配置。");
                } else {
                    if (!_toolTasks.ContainsKey(workstationDTO.tool_id.Value)) {
                        WidgetUtils.ShowErrorPopUp($"操作失败！未找到当前站点配置的工具。");
                    } else {
                        ToolTask toolTask = _toolTasks[workstationDTO.tool_id.Value];
                        aciont(toolTask);
                    }
                }
            }
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            _tablePanel.Size = new(ContentPanel.Width - ContentPanel.Padding.Size.Width, ContentPanel.Height - ContentPanel.Padding.Size.Height);

            int boxW = _tablePanel.Width / _tablePanel.ColumnCount - _boxMargin * 2;
            IList list = _tablePanel.Controls;
            for (int i = 0; i < list.Count; i++) {
                Control? control = (Control?) list[i];
                if (control != null) {
                    control.Margin = new(_boxMargin);
                    control.Size = new(boxW, _boxHeight);
                }
            }
        }
    }
}

