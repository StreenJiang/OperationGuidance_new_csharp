using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Constants;
using CustomLibrary.Forms;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Extensions;
using System.Reflection;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views.AbstractViews;

namespace OperationGuidance_new.Views {
    public class WorkplaceMissionView_SCII: CustomContentPanel {
        private MissionListPanel? _missionListPanel;
        private List<ProductMissionDTO>? _productMissionDTOs;
        private OperationGuidanceApis? apis;
        private bool _operatorOpenning = false;

        private CustomTabPanel? _pagePanel;
        private TopBar? _topBar;
        private AWorkplaceContentPanel? _workplacePanel;

        public WorkplaceMissionView_SCII() : base() => OpenMissionListView();
        public WorkplaceMissionView_SCII(bool operatorOpenning) : base() {
            _operatorOpenning = operatorOpenning;
            // 如果是操作员登录，则直接打开工作台
            if (operatorOpenning) {
                OpenWorkplaceViewDirectly();
            } else {
                OpenMissionListView();
            }
        }

        private void OpenMissionListView() {
            // Get apis
            apis = SystemUtils.GetApis();
            // Initialize
            _missionListPanel = new("选择任务", "直接进入工作台", (s, e) => OpenWorkplaceViewDirectly()) {
                Margin = new Padding(0),
                Parent = this,
            };
        }
        private void OpenWorkplaceViewDirectly() => OpenWorkplaceView(null);

        private void CheckAndDisplay() {
            if (_missionListPanel != null) {
                // Fetch data
                FetchData();
                // If there is no any mission, so show the big button
                if (_productMissionDTOs != null) {
                    _missionListPanel.RefreshMissionBlocks(_productMissionDTOs, OpenWorkplaceView);
                }
            }
        }

        public override void VisibleToTrue() {
            if (_workplacePanel != null && !_workplacePanel.IsDisposed) {
                System.Console.WriteLine($"_workplacePanel.Activated: {_workplacePanel.Activated}");
                // TODO: 这里或许可以做一个“任务中断”的效果，即不是每次进入都打开一个新的任务
            }
            // Check and display view
            CheckAndDisplay();
            // Invoke base, it will resize all children
            base.VisibleToTrue();
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            // Resize mission list panel
            if (_missionListPanel != null) {
                _missionListPanel.Size = new(Width, Height);
                _missionListPanel.ResizeChildren(eventArgs);
                if (_missionListPanel.Visible) {
                    _missionListPanel.Invalidate();
                }
            }
        }

        private void OpenWorkplaceView(int? missionId) {
            if (_pagePanel != null && !_pagePanel.IsDisposed) {
                _pagePanel.Dispose();
            }
            if (_topBar != null && !_topBar.IsDisposed) {
                _topBar.Dispose();
            }
            if (_workplacePanel != null && !_workplacePanel.IsDisposed) {
                _workplacePanel.Dispose();
            }
            // Create a new view
            _pagePanel = new() {
                Parent = WidgetUtils.MainForm,
                Size = WidgetUtils.MainForm.ClientSize,
            };
            _topBar = new(_operatorOpenning) {
                Parent = _pagePanel,
                BackColor = ColorConfigs.COLOR_MAIN_MENU_BACKGROUND,
                MainMenuLogo = Properties.Resources.logo,
                Margin = new Padding(0),
                PanelDirection = MenuPanelDirection.TOP,
                TitleColor = ColorConfigs.COLOR_WORKPLACE_TITLE,
            };
            _workplacePanel = new WorkplaceContentPanel_SCII(missionId, missionName => {
                _topBar.Title = missionName;
            }) {
                Parent = _pagePanel,
                BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND,
                Margin = new Padding(0),
                PenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            _topBar.Workplace = _workplacePanel;
            _pagePanel.ResizeChildren();
            // Hide main panel
            if (WidgetUtils.MainPanel != null) {
                WidgetUtils.MainPanel.Visible = false;
            }
            if (_operatorOpenning) {
                WidgetUtils.MainForm.SizeChanged += (s, e) => {
                    _pagePanel.Size = WidgetUtils.MainSize;
                };
            }
            _pagePanel.Size = new(WidgetUtils.MainSize.Width - 2, WidgetUtils.MainSize.Height - 2);
            _pagePanel.Location = new(1, 1);
        }

        private void FetchData() {
            if (apis != null) {
                _productMissionDTOs = apis.QueryProductMissionList(new(SystemUtils.MacAddressesDTO.id)).ProductMissionDTOs;
            }
        }
    }

    public class WorkplaceContentPanel_SCII: AWorkplaceContentPanel {
        // 上方
        private CustomContentPanel _top;
        // 上方左边
        private CustomContentPanel _topLeft;
        // 上方左边上面
        private WorkplacePiece _barCodeOuter;
        // 上方左边下面
        private WorkplacePiece _imageDisplayOuter;
        // 上方右边
        private CustomContentPanel _topRight;
        // 上方右边的上面
        private WorkplacePiece _topRightTop;
        // 上方右边的中间
        private CustomContentPanel _topRightMiddle;
        // 上方右边的中间的左边
        private WorkplacePiece _topRightMiddleLeft;
        // 上方右边的中间的右边
        private WorkplacePiece _topRightMiddleRight;
        // 上方右边的下面
        private WorkplacePiece _topRightBottom;

        // 中间
        private WorkplacePiece _middle;

        // 下方
        private WorkplacePiece _bottom;


        // private Label _productSideTitle;
        // private List<Image?> _smallSideImagesForShowing;
        // private PictureBox _smallSideImage;
        // private TableLayoutPanel _buttonPanel;
        // private PageSwitchButton _first;
        // private PageSwitchButton _backward;
        // private PageSwitchButton _forward;
        // private PageSwitchButton _last;
        // private Label _pageInfo;


        public WorkplaceContentPanel_SCII(int? missionId, Action<string> resetMissionName) : base(missionId, resetMissionName) {
            _actionAfterSendingPset = SetPset;

            // 初始化所有组件
            InitializeOuters();
            InitializeTopLeftTop();
            InitializeTopLeftBottom();
            InitializeTopRightTop();
            InitializeTopRightMiddleLeft();
            InitializeTopRightMiddleRight();
            InitializeTopRightBottom();
            InitializeMiddle();
            InitializeBottom();

            _checkRedo = true;
        }

        // 初始化所有外框
        private void InitializeOuters() {
            // 上方
            _top = new() {
                Parent = this,
                Padding = new(0),
            };
            // 上方左边
            _topLeft = new() {
                Parent = _top,
                Padding = new(0),
                FlowDirection = FlowDirection.TopDown,
            };
            // 上方左边上面
            _barCodeOuter = new() {
                Parent = _topLeft,
                Margin = new(0),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            // 上方左边下面
            _imageDisplayOuter = new() {
                Parent = _topLeft,
                Margin = new(0),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            // 上方右边
            _topRight = new() {
                Parent = _top,
                Padding = new(0),
                FlowDirection = FlowDirection.TopDown,
            };
            // 上方右边的上面
            _topRightTop = new() {
                Parent = _topRight,
                Padding = new(0),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            // 上方右边的中间
            _topRightMiddle = new() {
                Parent = _topRight,
                Padding = new(0),
            };
            // 上方右边的中间的左边
            _topRightMiddleLeft = new() {
                Parent = _topRightMiddle,
                Padding = new(0),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            // 上方右边的中间的右边
            _topRightMiddleRight = new() {
                Parent = _topRightMiddle,
                Padding = new(0),
                FlowDirection = FlowDirection.TopDown,
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            // 上方右边的下面
            _topRightBottom = new() {
                Parent = _topRight,
                Padding = new(0),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };

            // 中间
            _middle = new() {
                Parent = this,
                Padding = new(0),
                FlowDirection = FlowDirection.TopDown,
            };

            // 下方
            _bottom = new() {
                Parent = this,
                Padding = new(0),
                FlowDirection = FlowDirection.RightToLeft,
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
        }

        // 初始化顶部左侧顶部
        private void InitializeTopLeftTop() {
            _barCodeOuter.Controls.Add(_barCodePictureBox);
            _barCodeOuter.Controls.Add(_barCodeTextBox);
            _barCodeOuter.Click += barCodePopUp;
        }
        protected override void OpenBarCodePopUpForm(string? barCode = null) {
            if (!_activated) {
                string batchNum = _productBatch.GetTextBox(0).Box.Text;
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
            base.OpenBarCodePopUpForm(barCode);
        }

        // 初始化顶部左侧底部
        private void InitializeTopLeftBottom() {
            _productImageDisplayPanel = new(_defaultImage) {
                Parent = _imageDisplayOuter,
                Margin = new(1, 1, 0, 0),
                Padding = new(0),
                BackColor = ColorConfigs.COLOR_EMPTY_PRODUCT_CONTENT_BACKGROUND,
            };
            _missionImages = new();
            _productImageFiles = new();
            _allBolts = new();
            _armLocatingAccuracy = MainUtils.GetArmLocatingAccuracy();

            SetProductImagePanel();
        }

        // 初始化顶部右侧的顶部
        private void InitializeTopRightTop() {
            _operatorInfoTitle = new() {
                Parent = _topRightTop,
                Margin = new(1),
                Padding = new(0),
                Text = "操作员信息",
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = ColorConfigs.COLOR_WORKPLACE_SUB_TITLE,
            };
            _operatorName = new("操作员") {
                Parent = _topRightTop,
                ReadOnly = true,
                Enabled = false,
            };
            _operatorId = new("员工号") {
                Parent = _topRightTop,
                ReadOnly = true,
                Enabled = false,
            };
            SetOperatorInfo();
        }
        private void SetOperatorInfo() {
            _operatorName.SetValue(0, SystemUtils.LoggedUserName);
            _operatorId.SetValue(0, SystemUtils.LoggedUserId + "");
        }

        // 初始化顶部中间的左侧
        private void InitializeTopRightMiddleLeft() {
            // 初始化实时状态显示框
            _workingProcessPanel = new() {
                Parent = _topRightMiddleLeft,
                Margin = new(0),
                Padding = new(0),
            };
        }

        // 初始化顶部中间的右侧
        private void InitializeTopRightMiddleRight() {
            // 初始化实时螺钉拧紧数据框
            _torqueTitle = new() {
                Parent = _topRightMiddleRight,
                Margin = new(1),
                Padding = new(0),
                Text = "扭矩（N*m）",
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = ColorConfigs.COLOR_WORKPLACE_SUB_TITLE,
            };
            _torque = new() {
                Parent = _topRightMiddleRight,
                Margin = new(1),
                Padding = new(0),
                Text = "0.0",
                TextAlign = ContentAlignment.MiddleRight,
            };
            _angleTitle = new() {
                Parent = _topRightMiddleRight,
                Margin = new(1),
                Padding = new(0),
                Text = "角度（°）",
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = ColorConfigs.COLOR_WORKPLACE_SUB_TITLE,
            };
            _angle = new() {
                Parent = _topRightMiddleRight,
                Margin = new(1),
                Padding = new(0),
                Text = "0",
                TextAlign = ContentAlignment.MiddleRight,
            };
        }

        // 初始化顶部右侧的底部
        private void InitializeTopRightBottom() {
            _missionDetailTitle = new() {
                Parent = _topRightBottom,
                Margin = new(1),
                Padding = new(0),
                Text = "任务信息",
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = ColorConfigs.COLOR_WORKPLACE_SUB_TITLE,
            };
            _productBatch = new("产品批次") {
                Parent = _topRightBottom,
            };
            _productBatch.GetTextBox(0).Box.TextChanged += (s, e) => {
                _productBatch.GetTextBox(0).IsError = false;
                if (_activated && _missionRecord != null && _productBatch.GetTextBox(0).Box.Text != _missionRecord.product_batch) {
                    WidgetUtils.ShowErrorPopUp("任务已激活，无法修改产品批次");
                    _productBatch.GetTextBox(0).Box.Text = _missionRecord.product_batch;
                }
            };
            _missionSelectedName = new("任务名称") {
                Parent = _topRightBottom,
                ReadOnly = true,
                Enabled = false,
            };
            CommonButton missionSelectBtn = _missionSelectedName.AddButton("切换");
            missionSelectBtn.Enabled = true;
            missionSelectBtn.Click += (s, e) => {
                if (_activated) {
                    WidgetUtils.ShowWarningPopUp("任务已激活，无法切换任务");
                } else {
                    List<ProductMissionDTO> missions = _apis.QueryProductMissionList(new(SystemUtils.MacAddressesDTO.id)).ProductMissionDTOs;
                    if (missions.Count > 0) {
                        PopUpMissionListForm(missions);
                    } else {
                        WidgetUtils.ShowWarningPopUp("没有可选任务，请前往任务管理添加");
                    }
                }
            };
            _productSumPerDay = new("今日生产") {
                Parent = _topRightBottom,
                ReadOnly = true,
                Enabled = false,
            };
            _okSumPerDay = new("合格数") {
                Parent = _topRightBottom,
                ReadOnly = true,
                Enabled = false,
            };
            _ngRatePerDay = new("不良品率") {
                Parent = _topRightBottom,
                ReadOnly = true,
                Enabled = false,
            };
            _pset = new("程序号") {
                Parent = _topRightBottom,
                ReadOnly = true,
                Enabled = false,
            };

            SetMissionDetails();
        }
        private async void SetMissionDetails() {
            _missionSelectedName.SetValue(0, _mission.name);
            ResetMissionDetails();

            await Task.Run(async () => {
                while (!IsHandleCreated) {
                    await Task.Delay(200);
                }
                BeginInvoke(() => {
                    MissionRecordDTO? missionRecordDTO = _apis.QueryLatestMissionRecord(new(SystemUtils.LoggedUserId)).MissionRecordDTO;
                    // 存在可以回填的数据
                    if (missionRecordDTO != null) {
                        // 刚登录
                        if (MainUtils.LoginFlag) {
                            // 需要回填确认
                            if (MainUtils.IsProductBatchNoticeEnabled()) {
                                // 弹出提示确认是否回填
                                if (WidgetUtils.ShowConfirmPopUp($"是否继续批次【{missionRecordDTO.product_batch}】？")) {
                                    MainUtils.LastProductBatch = missionRecordDTO.product_batch;
                                } else {
                                    MainUtils.LastProductBatch = null;
                                }
                            }
                            // 不需要提示则直接回填
                            else {
                                MainUtils.LastProductBatch = missionRecordDTO.product_batch;
                            }
                        }
                        // 最新查到的批次信息与缓存的不一致，则换掉
                        else if (MainUtils.LastProductBatch != missionRecordDTO.product_batch) {
                            MainUtils.LastProductBatch = missionRecordDTO.product_batch;
                        }
                        // 不管是否回填，登录标识都要改
                        MainUtils.LoginFlag = false;
                        // 不为空就回填
                        if (!string.IsNullOrEmpty(MainUtils.LastProductBatch)) {
                            _productBatch.SetValue(0, MainUtils.LastProductBatch);
                        }
                    }
                });
            });
        }
        private void ResetMissionDetails() {
            SetTodayData();
            SetPset();
        }
        private void SetTodayData() {
            List<MissionRecordDTO> missionRecordDTOs = _apis.QueryMissionRecordList(new() { UserId = SystemUtils.LoggedUserId, Date = DateTime.Now }).MissionRecordDTOs;
            int sum = missionRecordDTOs.Count;
            int okSum = missionRecordDTOs.Where(dto => dto.mission_result == (int) TighteningStatus.OK).Count();
            double ngRate;
            if (sum == 0) {
                ngRate = 0;
            } else {
                ngRate = Math.Round((sum - okSum) / (double) sum, 4) * 100;
            }

            _productSumPerDay.SetValue(0, sum + "");
            _okSumPerDay.SetValue(0, okSum + "");
            _ngRatePerDay.SetValue(0, ngRate + "%");
        }
        private void SetPset() => SetPset(null);
        private void SetPset(string? customMsg) {
            if (!string.IsNullOrEmpty(customMsg)) {
                _pset.SetValue(0, customMsg);
            } else if (_currentWorkingBolt != null) {
                if (_currentWorkingBolt.CurrentParameterSet != null) {
                    _pset.SetValue(0, _currentWorkingBolt.CurrentParameterSet + "");
                } else {
                    _pset.SetValue(0, "未配置程序号");
                }
            } else {
                _pset.SetValue(0, null);
            }
        }
        private void PopUpMissionListForm(List<ProductMissionDTO> missions) {
            Size contentSize = new((int) (WidgetUtils.MainSize.Width * .7), (int) (WidgetUtils.MainSize.Height * .7));
            CustomPopUpForm popUpForm = new() {
                Title = "选择任务",
                BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
            };
            MissionListPanel? missionListPanel = null;
            popUpForm.AddButton("确定").Click += (s, e) => {
                if (missionListPanel == null || missionListPanel.CurrentToggledMission == null) {
                    WidgetUtils.ShowErrorPopUp("请选择一个任务");
                } else {
                    // 手动切换任务需要清空一下条码缓存
                    _barCodeObj.Reset();
                    _barCodeTextBox.Text = ConfigsVariables.BAR_CODE_NOTE;
                    SwitchToMission(_apis.QueryProductMissionDetail(new(missionListPanel.CurrentToggledMission.Entity.id)).ProductMissionDTO);
                    popUpForm.Hide();
                }
            };
            popUpForm.AddButton("关闭").Click += (s, e) => {
                popUpForm.Hide();
            };
            popUpForm.PretendToShowToCreateHandlesForChildren();
            popUpForm.SetContentSizeAndSelfSize(contentSize);

            Padding contentPadding = popUpForm.ContentPanel.Padding;
            int innerContentWidth = contentSize.Width - contentPadding.Size.Width;
            int innerContentHeight = contentSize.Height - contentPadding.Size.Height;
            missionListPanel = new() {
                Parent = popUpForm.ContentPanel,
                Margin = new Padding(0),
                Size = new(innerContentWidth, innerContentHeight),
            };
            missionListPanel.RefreshMissionBlocks(missions, null, true);

            popUpForm.Show();
        }
        public override void SwitchToMission(ProductMissionDTO mission) {
            _mission = mission;
            _resetMissionName(_mission.name);
            _missionSelectedName.SetValue(0, _mission.name);
            SetProductImagePanel();
            ResizeTopLeftBottom();
        }

        // 初始化中间
        private void InitializeMiddle() {
            _tighteningDataPanel = new(gridView => {
                DataGridViewColumn[] columnRange = { };
                List<OperationDataField> operationDataFields = MainUtils.GetOperationDataFields();
                foreach (OperationDataField field in operationDataFields) {
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
                Parent = _middle,
                HeaderHeight = WidgetUtils.WorkplaceGridViewHeaderHeight(),
                RowsHeight = WidgetUtils.WorkplaceGridViewContentRowHeight(),
                PageHeight = WidgetUtils.WorkplaceGridViewPageInfoHeight(),
                ColumnsPaddingRatio = WidgetUtils.WorkplaceGridViewColumnsPaddingRatio(),
                AutoDown = true,
            };
            _tighteningDataPanel.HandleCreated += (s, e) => {
                _tighteningDataPanel.DataSource = _tighteningDataVOs;
            };
        }

        // 初始化底部
        private void InitializeBottom() {
            foreach (DeviceBlock block in _deviceBlocks) {
                _bottom.Controls.Add(block);
            }
            _bottom.Controls.Add(_timeDisplayerOuter);
        }


        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            if (IsHandleCreated && !IsDisposed) {
                int boxHeight = WidgetUtils.WorkplaceBoxOrButtonHeightRatio();
                int titleHeight = (int) (boxHeight * 1.1);
                int contentVPadding = (int) (boxHeight * .35);
                int contentHPadding = contentVPadding;
                Font titleFont = new Font(WidgetsConfigs.SystemFontFamily, titleHeight * .55f, FontStyle.Bold, GraphicsUnit.Pixel);

                ResizeOuters(boxHeight, titleHeight, contentVPadding);
                ResizeTopLeftTop();
                ResizeTopLeftBottom();
                ResizeTopRightTop(boxHeight, titleHeight, contentVPadding, contentHPadding, titleFont);
                ResizeTopRightMiddleLeft();
                ResizeTopRightMiddleRight();
                ResizeTopRightBottom(boxHeight, titleHeight, contentVPadding, contentHPadding, titleFont);
                ResizeMiddle();
                ResizeBottom();
                Invalidate();
            }
        }

        // 计算尺寸： 外框
        private void ResizeOuters(int boxHeight, int titleHeight, int contentVPadding) {
            int padding = Padding.Left / 2;
            int workplaceWidth = Width - Padding.Left * 2;
            int workplaceHeight = Height - Padding.Top * 2;
            int barCodeHeight = (int) (workplaceHeight * WidgetUtils.WorkplaceBarCodeHeightRatio());
            int imagePanelHeight = (int) (workplaceHeight * WidgetUtils.WorkplaceImagePanelHeightRatio());
            int topHeight = barCodeHeight + imagePanelHeight + padding;
            int bottomHeight = (int) (workplaceHeight * .045);
            int middleHeight = workplaceHeight - topHeight - bottomHeight - padding * 2; // 为了取整
            int topLeftWidth = (int) (workplaceWidth * WidgetUtils.WorkplaceLeftWidthRatio());
            int topRightWidth = workplaceWidth - topLeftWidth - padding;
            int topRightTopHeight = titleHeight + boxHeight + contentVPadding * 2;
            int topRightBottomHeight = titleHeight + boxHeight * 4 + contentVPadding * 5;
            int topRightMiddleHeight = topHeight - topRightTopHeight - topRightBottomHeight - padding * 2;
            int topRightMiddleLeftWidth = (int) (topRightWidth * .55);
            int topRightMiddleRightWidth = topRightWidth - topRightMiddleLeftWidth - padding;

            // 上方
            _top.Size = new(workplaceWidth, topHeight);
            _top.Margin = new(0, 0, 0, padding);
            // 上方左边
            _topLeft.Size = new(topLeftWidth, topHeight);
            _topLeft.Margin = new(0, 0, padding, 0);
            // 上方左边上面
            _barCodeOuter.Size = new(topLeftWidth, barCodeHeight);
            _barCodeOuter.Margin = new(0, 0, 0, padding);
            // 上方左边下面
            _imageDisplayOuter.Size = new(topLeftWidth, imagePanelHeight);
            // 上方右边
            _topRight.Size = new(topRightWidth, topHeight);
            // 上方右边的上面
            _topRightTop.Size = new(topRightWidth, topRightTopHeight);
            _topRightTop.Margin = new(0, 0, 0, padding);
            // 上方右边的中间
            _topRightMiddle.Size = new(topRightWidth, topRightMiddleHeight);
            _topRightMiddle.Margin = new(0, 0, 0, padding);
            // 上方右边的中间的左边
            _topRightMiddleLeft.Size = new(topRightMiddleLeftWidth, topRightMiddleHeight);
            _topRightMiddleLeft.Margin = new(0, 0, padding, 0);
            // 上方右边的中间的右边
            _topRightMiddleRight.Size = new(topRightMiddleRightWidth, topRightMiddleHeight);
            // 上方右边的下面
            _topRightBottom.Size = new(topRightWidth, topRightBottomHeight);

            // 中间
            _middle.Size = new(workplaceWidth, middleHeight);
            _middle.Margin = new(0, 0, 0, padding);

            // 下方
            _bottom.Size = new(workplaceWidth, bottomHeight);
            _bottom.Padding = new(0, 0, 1, 0);
        }

        // 计算尺寸： 条码框
        private void ResizeTopLeftTop() {
            // icon的边长
            int side = (int) (_barCodePictureBox.Parent.Height * .675);
            // 重设icon
            _barCodePictureBox.Image = WidgetUtils.ResizeImage(_barCodeImage, side, side);
            _barCodePictureBox.Margin = new((_barCodePictureBox.Parent.Height - side) / 2);
            _barCodePictureBox.Size = new(side, side);

            // 重设输入框
            int newH = (int) (_barCodePictureBox.Parent.Height * .875);
            _barCodeTextBox.Size = new(_barCodePictureBox.Parent.Width - side * 2, newH);
            _barCodeTextBox.Margin = new(0, (_barCodePictureBox.Parent.Height - newH) / 2, 0, 0);

            // 重新计算弹框的大小
            ResizeBarCodePopUpForm();
        }
        private void ResizeBarCodePopUpForm() {
            if (_barCodePopUpForm != null) {
                _barCodePopUpForm.CalculateDetailProperties();

                Control mainForm = WidgetUtils.MainForm;
                Padding contentPadding = _barCodePopUpForm.ContentPanel.Padding;
                int boxHeight = (int) (mainForm.Height * .05);
                Size contentSize = new((int) (mainForm.Width * .75), boxHeight + contentPadding.Size.Height);
                int boxWidth = contentSize.Width - contentPadding.Size.Width;
                // _barCodePopUpForm.TextBox.Size = new(boxWidth, boxHeight);
                _barCodePopUpForm.ResizeSelf();

                _barCodePopUpForm.SetContentSizeAndSelfSize(contentSize);
            }
        }

        // 计算尺寸： 产品图片展示区域
        private void ResizeTopLeftBottom() {
            // Image panel 要比 _leftMiddle 小2，是为了显示出后者的边框
            Size newPanelSize = new(_productImageDisplayPanel.Parent.Width - 2, _productImageDisplayPanel.Parent.Height - 2);
            _productImageDisplayPanel.Size = newPanelSize;

            foreach (ProductImageFile productImageFile in _productImageFiles) {
                productImageFile.RecalculateZoomingRatio();
            }
            _productImageFiles[_currentSideIndex].RefreshImage();

            // 重新计算螺栓点位按钮的大小和位置
            int btnSide = (int) (newPanelSize.Height * .125);
            foreach (KeyValuePair<int, List<BoltButton>> pair in _allBolts) {
                foreach (BoltButton boltButton in pair.Value) {
                    boltButton.Size = new(btnSide, btnSide);
                    int newX = _productImageDisplayPanel.MaxRectLocation.X + (int) (_productImageDisplayPanel.MaxRectWidth * boltButton.BoltDTO.location_x_percent / 100) - btnSide / 2;
                    int newY = _productImageDisplayPanel.MaxRectLocation.Y + (int) (_productImageDisplayPanel.MaxRectHeight * boltButton.BoltDTO.location_y_percent / 100) - btnSide / 2;
                    boltButton.Location = new(newX, newY);
                }
            }

            // 重新计算弹框的大小和位置
            ResizeBoltPopUpForm();
        }

        // 计算尺寸： 员工信息框
        private void ResizeTopRightTop(int boxHeight, int titleHeight, int contentVPadding,
                int contentHPadding, Font titleFont) {
            // Resize title and font
            _operatorInfoTitle.Size = new(_operatorInfoTitle.Parent.Width, titleHeight);
            _operatorInfoTitle.Font = titleFont;
            // Resize content size and font
            int boxWidth = (_operatorInfoTitle.Parent.Width - contentHPadding * 3) / 2;
            _operatorName.Size = new(boxWidth, boxHeight);
            _operatorName.Margin = new(contentHPadding, contentVPadding, 0, 0);
            _operatorId.Size = new(boxWidth, boxHeight);
            _operatorId.Margin = new(contentHPadding, contentVPadding, 0, 0);
        }

        // 计算尺寸： 实时状态框
        private void ResizeTopRightMiddleLeft() {
            _workingProcessPanel.Size = _workingProcessPanel.Parent.Size;
        }

        // 计算尺寸： 实时扭矩、角度框
        private void ResizeTopRightMiddleRight() {
            // Resize titles
            _torqueTitle.Size = new(_torqueTitle.Parent.Width - 2, (int) (_torqueTitle.Parent.Height * .225));
            _angleTitle.Size = _torqueTitle.Size;
            // Reset font size
            _torqueTitle.Font = new Font(WidgetsConfigs.SystemFontFamily, _torqueTitle.Height * .55f, FontStyle.Bold, GraphicsUnit.Pixel);
            _angleTitle.Font = _torqueTitle.Font;
            // Resize data text
            int heightRemain = _torqueTitle.Parent.Height - _torqueTitle.Height - _angleTitle.Height - 6; // 2 vertical border, 2 vertical margin of each title
            if (heightRemain > 0) {
                _torque.Size = new(_torqueTitle.Parent.Width - 2, (int) (heightRemain * .6) - 2);
                _angle.Size = new(_torqueTitle.Parent.Width - 2, heightRemain - _torque.Height - 2);
                // Reset font size depends on theirs height
                _torque.Font = new(WidgetsConfigs.SystemFontFamily, _torque.Height * .8F, FontStyle.Bold, GraphicsUnit.Pixel);
                _angle.Font = new(WidgetsConfigs.SystemFontFamily, _angle.Height * .8F, FontStyle.Bold, GraphicsUnit.Pixel);
            }
        }

        // 计算尺寸： 任务信息框
        private void ResizeTopRightBottom(int boxHeight, int titleHeight, int contentVPadding,
                int contentHPadding, Font titleFont) {
            // Resize title and font
            _missionDetailTitle.Size = new(_operatorInfoTitle.Parent.Width, titleHeight);
            _missionDetailTitle.Font = titleFont;
            // Resize content size and font
            int boxWidth = (_operatorInfoTitle.Parent.Width - contentHPadding * 3) / 2;
            int boxWidth2 = _operatorInfoTitle.Parent.Width - contentHPadding * 2;
            _productBatch.Size = new(boxWidth2, boxHeight);
            _productBatch.Margin = new(contentHPadding, contentVPadding, 0, 0);
            _missionSelectedName.Size = new(boxWidth2, boxHeight);
            _missionSelectedName.Margin = new(contentHPadding, contentVPadding, 0, 0);
            _productSumPerDay.Size = new(boxWidth, boxHeight);
            _productSumPerDay.Margin = new(contentHPadding, contentVPadding, 0, 0);
            _okSumPerDay.Size = new(boxWidth, boxHeight);
            _okSumPerDay.Margin = new(contentHPadding, contentVPadding, 0, 0);
            _ngRatePerDay.Size = new(boxWidth, boxHeight);
            _ngRatePerDay.Margin = new(contentHPadding, contentVPadding, 0, 0);
            _pset.Size = new(boxWidth, boxHeight);
            _pset.Margin = new(contentHPadding, contentVPadding, 0, 0);
        }

        // 计算尺寸： 数据展示列表区域
        private void ResizeMiddle() {
            _tighteningDataPanel.Size = _tighteningDataPanel.Parent.Size;
        }

        // 计算尺寸： 底部横框
        private void ResizeBottom() {
            int blocksWidth = 0;
            foreach (Control control in _bottom.Controls) {
                if (control is DeviceBlock) {
                    control.Size = new(_bottom.Height, _bottom.Height - 1);
                    blocksWidth += _bottom.Height;
                }
            }
            int timeDisplayerWidth = _bottom.Width - blocksWidth;
            _timeDisplayerOuter.Size = new(timeDisplayerWidth - 2, _bottom.Height - 2);
            _timeDisplayer.Font = new Font(WidgetsConfigs.SystemFontFamily, _bottom.Height * .4f, FontStyle.Regular, GraphicsUnit.Pixel);
            _timeDisplayer.Margin = new(_timeDisplayer.Height / 3, (_timeDisplayerOuter.Height - _timeDisplayer.Height) / 2, 0, 0);
        }


        // private void InitializeMiddleBottom() {
        //     _productSideTitle = new() {
        //         Parent = _middleBottom,
        //         Margin = new(1),
        //         Padding = new(0),
        //         TextAlign = ContentAlignment.MiddleCenter,
        //         ForeColor = ColorConfigs.COLOR_WORKPLACE_SIDE_TITLE_TEXT,
        //         BackColor = ColorConfigs.COLOR_WORKPLACE_SIDE_TITLE_BACK,
        //     };
        //     _smallSideImage = new() {
        //         Parent = _middleBottom,
        //         Margin = new(0),
        //         Padding = new(0),
        //     };
        //     int totalPages = 0;
        //     List<ProductSideDTO>? productSides = _mission.ProductSides;
        //     if (productSides != null) {
        //         _productSideTitle.Text = productSides[0].name;
        //         totalPages = productSides.Count;
        //     }
        //     if (_missionImages.Count > 0) {
        //         _smallSideImagesForShowing = new();
        //         foreach (Image? image in _missionImages) {
        //             if (image == null) {
        //                 _smallSideImagesForShowing.Add(_defaultImage);
        //             } else {
        //                 _smallSideImagesForShowing.Add(image);
        //             }
        //         }
        //     }
        //     int currentPage = _currentSideIndex + 1;
        //     _first = new() {
        //         Icon = Properties.Resources.page_btn_backward_fast,
        //         TotalPages = totalPages,
        //         CurrentPage = currentPage,
        //     };
        //     _backward = new() {
        //         Icon = Properties.Resources.page_btn_backward,
        //         TotalPages = totalPages,
        //         CurrentPage = currentPage,
        //     };
        //     _forward = new() {
        //         Icon = Properties.Resources.page_btn_forward,
        //         TotalPages = totalPages,
        //         CurrentPage = currentPage,
        //     };
        //     _last = new() {
        //         Icon = Properties.Resources.page_btn_forward_fast,
        //         TotalPages = totalPages,
        //         CurrentPage = currentPage,
        //     };
        //     _pageInfo = new() {
        //         Margin = new(0),
        //         Padding = new(0),
        //         TextAlign = ContentAlignment.MiddleCenter,
        //         ForeColor = ColorConfigs.COLOR_WORKPLACE_SIDE_PAGE_TEXT,
        //     };
        //     _pageInfo.Text = currentPage + "/" + totalPages;
        //     _buttonPanel = new() {
        //         Parent = _middleBottom,
        //         Margin = new(1),
        //         Padding = new(0),
        //         ColumnCount = 5,
        //     };
        //     _buttonPanel.Controls.Add(_first);
        //     _buttonPanel.Controls.Add(_backward);
        //     _buttonPanel.Controls.Add(_pageInfo);
        //     _buttonPanel.Controls.Add(_forward);
        //     _buttonPanel.Controls.Add(_last);
        //
        //     _first.Click += (sender, eventArgs) => {
        //         _currentSideIndex = 0;
        //         changeCurrentPageAndInvalidate();
        //     };
        //     _backward.Click += (sender, eventArgs) => {
        //         if (_currentSideIndex <= 0) {
        //             _currentSideIndex = 0;
        //         } else {
        //             _currentSideIndex -= 1;
        //         }
        //         changeCurrentPageAndInvalidate();
        //     };
        //     _forward.Click += (sender, eventArgs) => {
        //         if (_currentSideIndex >= _missionImages.Count - 1) {
        //             _currentSideIndex = _missionImages.Count - 1;
        //         } else {
        //             _currentSideIndex += 1;
        //         }
        //         changeCurrentPageAndInvalidate();
        //     };
        //     _last.Click += (sender, eventArgs) => {
        //         _currentSideIndex = _missionImages.Count - 1;
        //         changeCurrentPageAndInvalidate();
        //     };
        //     void changeCurrentPageAndInvalidate() {
        //         if (_currentWorkingBolt != null) {
        //             if (_currentWorkingBolt.BoltDTO.side_id != _sides[_currentSideIndex].id) {
        //                 _currentWorkingBolt.ShowingWhileWorking = false;
        //             } else {
        //                 _currentWorkingBolt.ShowingWhileWorking = true;
        //             }
        //         }
        //         int newCurrentPage = _currentSideIndex + 1;
        //         _first.CurrentPage = newCurrentPage;
        //         _backward.CurrentPage = newCurrentPage;
        //         _forward.CurrentPage = newCurrentPage;
        //         _last.CurrentPage = newCurrentPage;
        //         // 切换side后也切换点位
        //         _showingBoltButtons.ForEach(btn => btn.Visible = false);
        //         _showingBoltButtons = _allBolts.Where(btn => btn.BoltDTO.side_id == _sides[_currentSideIndex].id).ToList();
        //         _showingBoltButtons.ForEach(btn => btn.Visible = true);
        //         // 切换产品图片
        //         _productImageDisplayPanel.SetImage(_productImageFiles[_currentSideIndex].Image, _productImageFiles[_currentSideIndex].CenterLocation);
        //         _productImageFiles[_currentSideIndex].RefreshImage();
        //         ResizeSmallSideImageBox(_smallSideImagesForShowing[_currentSideIndex]);
        //         _pageInfo.Text = newCurrentPage + "/" + totalPages;
        //         _productSideTitle.Text = _productImageFiles[_currentSideIndex].SideDTO.name;
        //         ResetRightBottomTitleFont();
        //     }
        // }

        protected override void OnHandleDestroyed(EventArgs e) {
            base.OnHandleDestroyed(e);
            foreach (KeyValuePair<int, ArmTask> pair in _armTasks) {
                // Clear all delegates once this workplace handle has been destroyed to ensure running performance
                pair.Value.ActionAfterReceiving = new(c => { });
            }
            _serialPortTasks = MainUtils.SerialPortTasks;
            foreach (KeyValuePair<int, SerialPortTask> pair in _serialPortTasks) {
                // Clear all delegates once this workplace handle has been destroyed to make sure it won't throw any exception
                pair.Value.ActionAfterDataReceived = new(c => { });
            }
        }

        private async void StoreTighteningData(OperationDataDTO operationDataDTO) {
            await Task.Run(() => {
                lock (DataStorageLockObj) {
                    List<OperationDataDTO> data = new() { operationDataDTO };
                    List<string>? headers = null;
                    string textFileName = $"{MainUtils.GetStorageFormattedName()}.txt";
                    string excelFileName = $"{MainUtils.GetStorageFormattedName()}.xlsx";
                    string textFilePath = MainUtils.GetStoragePath() + textFileName;
                    string excelFilePath = MainUtils.GetStoragePath() + excelFileName;
                    // 检查当前文件是否存在
                    bool textFileExists = File.Exists(textFilePath);
                    bool excelFileExists = File.Exists(excelFilePath);
                    // 从配置文件读取配置
                    List<int> sortConfig = MainUtils.GetSortConfig();
                    List<int>? sortConfigCurr = MainUtils.GetSortConfigCurr();
                    List<OperationDataField> fieldsConfig = MainUtils.GetOperationDataFields(sortConfigCurr);
                    List<string> propertyNames = fieldsConfig.Where(f => f.Visible).Select(f => f.PropertyName).ToList();
                    // 检查当前是否存在正在使用的字段配置
                    if (sortConfigCurr == null || !sortConfig.SequenceEqual(sortConfigCurr) || !textFileExists || !excelFileExists) {
                        sortConfigCurr = sortConfig;
                        MainUtils.SetSortConfigCurr(sortConfigCurr);
                        headers = fieldsConfig.Where(f => f.Visible).Select(f => f.FieldName).ToList();
                    }
                    // 组装数据
                    List<Dictionary<int, object?>> dataWithConfigFields = new();
                    List<OperationDataVO> dataFormatted = new();
                    CommonUtils.ObjectConverter<OperationDataDTO, OperationDataVO>(data, dataFormatted);
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
                    // bool succeed = finalData.ExportToExcelFile(headers, excelFilePath, excelFileExists);
                    // // 由于 excel 文件如果打开后没有关闭会导致数据存储出错，因此先判断是否成功再进行后续操作
                    // if (succeed) {
                    //     _apis.BatchAddOperationData(new(data));
                    //     finalData.ExportToTextFile(headers, textFilePath, textFileExists);
                    // } else {
                    //     WidgetUtils.ShowWarningPopUp("Excel文件被占用，无法执行数据存储操作，本次数据已保留，请在下次任务完成以前或关闭工作台前释放被占用的数据文件，以免造成数据丢失！");
                    // }

                    // 先将组装好的VOs加入到实时显示数据列表中
                    _tighteningDataVOs.AddRange(dataFormatted);
                    RefreshTighteningDataPanel();
                    // 显示完后立马存入数据库
                    _apis.BatchAddOperationData(new(data));
                    // 最后再存进本地文件
                    finalData.ExportToTextFile(headers, textFilePath, textFileExists);
                    finalData.ExportToExcelFile(headers, excelFilePath, excelFileExists);
                }
            });
        }
        private void RefreshTighteningDataPanel() {
            _tighteningDataPanel.DataSource = _tighteningDataVOs;
        }

        public override void ActivateMission() {
            base.ActivateMission();

            if (_activated) {
                // Clear data grid view
                _tighteningDataVOs.Clear();
                RefreshTighteningDataPanel();

                _missionRecord.product_batch = _productBatch.GetTextBox(0).Box.Text;
            }
        }

        // 下发程序号，失败时持续尝试
        protected override async void SendPSet(BoltButton boltButton, ToolTask task, int? pset) {
            // Initialize pset text box first
            SetPset();

            // If pset is null, show error message in working proccess panel
            if (pset == null) {
                _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_DISABLE;
                _workingProcessPanel.RemoveDesc(_workingProcessPanel.TighteningDesc);
                _workingProcessPanel.AppendDesc(_workingProcessPanel.PsetNullError);
                return;
            }

            // Do send pset to controller
            await Task.Run(() => {
                BeginInvoke(async () => {
                    int sendTimes = 0;
                    while (!IsDisposed && !(await task.SendPSetAsync(pset.Value))) {
                        if (boltButton.CurrentParameterSet != null) {
                            return;
                        }

                        // Count failure times
                        sendTimes++;

                        // If sending times reaches maximun, show pop up form
                        if (_resendPsetMaxTimes > 0 && sendTimes >= _resendPsetMaxTimes) {
                            WidgetUtils.ShowWarningPopUp($"同一个点位下发程序号达到{_resendPsetMaxTimes}次，请检查任务配置");
                            return;
                        }

                        // Show reason of sending failure
                        _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_DISABLE;
                        _workingProcessPanel.RemoveDesc(_workingProcessPanel.TighteningDesc);
                        _workingProcessPanel.AppendDesc(_workingProcessPanel.PsetFailedError);

                        // 实时显示pset到任务信息框
                        SetPset("程序号下发失败");

                        // Confirm if retry needed
                        if (!WidgetUtils.ShowConfirmPopUp($"程序号{pset}下发失败，是否重发？")) {
                            return;
                        }
                    }

                    // Send successfully if step to here
                    _workingProcessPanel.RemoveDesc(_workingProcessPanel.PsetFailedError);
                    boltButton.CurrentParameterSet = pset;

                    SetPset();
                });
            });
        }

        protected override async void DoAfterRecevingTighteningDataAsync(TighteningData data, int deviceId) {
            await Task.Run(() => {
                BeginInvoke(() => {
                    ToolTask toolTask = _toolTasks[deviceId];
                    if (toolTask.WorkstationId != null) {
                        int workstationId = toolTask.WorkstationId.Value;

                        List<WorkstationDTO> workstationDTOs;
                        if (CheckIfIsMultiDeviceIndependenceMode()) {
                            workstationDTOs = _workstationsDTOs.Where(dto => _currentWorkingBoltIndependence.Keys.Contains(dto.id)).ToList();
                        } else {
                            List<int> workstationIds = new();
                            foreach (List<BoltButton> bolts in _allBolts.Values) {
                                workstationIds.AddRange(bolts.Select(b => b.BoltDTO.workstation_id));
                            }
                            workstationIds = workstationIds.Distinct().ToList();
                            workstationDTOs = _workstationsDTOs.Where(dto => workstationIds.Contains(dto.id) && dto.arm_id != null).ToList();
                        }
                        List<int?> toolIds = workstationDTOs.Select(dto => dto.tool_id).ToList();

                        // Main display
                        _torque.Text = data.torque + "";
                        _angle.Text = data.angle + "";

                        // Get current bolt
                        BoltButton currentBolt;
                        if (CheckIfIsMultiDeviceIndependenceMode()) {
                            currentBolt = _currentWorkingBoltIndependence[workstationId];
                        } else {
                            currentBolt = CommonUtils.CannotBeNull(_currentWorkingBolt);
                        }

                        ProductBoltDTO boltDTO = currentBolt.BoltDTO;
                        OperationDataDTO dataDTO = new();
                        CommonUtils.ObjectConverter<TighteningData, OperationDataDTO>(data, dataDTO);

                        WorkstationDTO workstationDTO = _workstationsDTOs.Single(dto => dto.id == workstationId);
                        dataDTO.workstation_id = workstationDTO.id;
                        dataDTO.workstation_name = workstationDTO.name;

                        DeviceToolDTO toolDTO = _tools.Single(t => t.id == deviceId);
                        dataDTO.tool_name = toolDTO.name;
                        dataDTO.tool_ip = $"{toolDTO.ip}:{toolDTO.port}";
                        dataDTO.tool_type = DeviceType_Tool.GetById(toolDTO.type).Name;
                        dataDTO.product_sied_id = _sides[_currentSideIndex].id;
                        dataDTO.bolt_serial_num = boltDTO.serial_num;
                        MissionRecordDTO missionRecord = CommonUtils.CannotBeNull(_missionRecord);
                        dataDTO.mission_record_id = missionRecord.id;
                        dataDTO.vin_number = missionRecord.product_bar_code;
                        if (_realTimeArmCoordinates != null) {
                            dataDTO.arm_position = _realTimeArmCoordinates.ToString();
                        }

                        // If result type is tightening
                        if (data.result_type == (int) TightenOrLoosen.TIGHTENING) {
                            bool tighteningOK = true;
                            string errorMsg = "";

                            // Check tightening status
                            if (data.tightening_status != (int) TighteningStatus.OK) {
                                tighteningOK = false;
                                if (data.torque_status != (int) TighteningCommonStatus.OK) {
                                    _torque.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                    if (!string.IsNullOrEmpty(errorMsg)) {
                                        errorMsg += "\r\n";
                                    }
                                    errorMsg += $"扭矩未达标：{Enum.GetName(typeof(TighteningCommonStatus), data.torque_status)}";
                                }
                                if (data.angle_status != (int) TighteningCommonStatus.OK) {
                                    _angle.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                    if (!string.IsNullOrEmpty(errorMsg)) {
                                        errorMsg += "\r\n";
                                    }
                                    errorMsg += $"角度未达标：{Enum.GetName(typeof(TighteningCommonStatus), data.angle_status)}";
                                }
                            }

                            // Check torque
                            if (boltDTO.torque_max > 0 && (data.torque < boltDTO.torque_min || data.torque > boltDTO.torque_max)) {
                                tighteningOK = false;
                                _torque.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                if (!string.IsNullOrEmpty(errorMsg)) {
                                    errorMsg += "\r\n";
                                }
                                errorMsg += "扭矩与配置范围不符";
                            }

                            // Check angle
                            if (boltDTO.angle_max > 0 && (data.angle < boltDTO.angle_min || data.angle > boltDTO.angle_max)) {
                                tighteningOK = false;
                                _angle.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                if (!string.IsNullOrEmpty(errorMsg)) {
                                    errorMsg += "\r\n";
                                }
                                errorMsg += "角度与配置范围不符";
                            }

                            // Switch to next bolt
                            if (tighteningOK) {
                                // Reset tightening type to tightening in case somewhere did some changes
                                _needLoosening = false;
                                _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.TIGHTENING;

                                // Tightening ok, data color change to green
                                _torque.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_GREEN;
                                _angle.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_GREEN;

                                // Lock the device
                                if (_locating_enabled) {
                                    toolTask.SendLock();
                                }

                                // Check next index
                                currentBolt.BoltStatus = BoltStatus.DONE;
                                currentBolt.Label = _torque.Text;
                                int nextIndex;
                                if (CheckIfIsMultiDeviceIndependenceMode()) {
                                    nextIndex = _allBoltsIndependence[_currentSideIndex][workstationId].IndexOf(currentBolt) + 1;
                                    // 检查是否存在跳点的情况
                                    while (nextIndex < _allBoltsIndependence[_currentSideIndex][workstationId].Count
                                            && _allBoltsIndependence[_currentSideIndex][workstationId][nextIndex].BoltStatus == BoltStatus.DONE) {
                                        nextIndex++;
                                    }
                                } else {
                                    nextIndex = _allBolts[_currentSideIndex].IndexOf(currentBolt) + 1;
                                    // 检查是否存在跳点的情况
                                    while (nextIndex < _allBolts[_currentSideIndex].Count && _allBolts[_currentSideIndex][nextIndex].BoltStatus == BoltStatus.DONE) {
                                        nextIndex++;
                                    }
                                }

                                if (nextIndex < _allBolts.Count) {
                                    if (CheckIfIsMultiDeviceIndependenceMode()) {
                                        _currentWorkingBoltIndependence[workstationId] = SwitchBolt(workstationId, nextIndex);
                                    } else {
                                        _currentWorkingBolt = SwitchBolt(nextIndex);
                                    }
                                } else {
                                    bool allDone = true;
                                    if (CheckIfIsMultiDeviceIndependenceMode()) {
                                        foreach (int id in _allBoltsIndependence[_currentSideIndex].Keys) {
                                            if (id != workstationId) {
                                                BoltButton? boltButton = _allBoltsIndependence[_currentSideIndex][id].Find(b => b.BoltStatus != BoltStatus.DONE);
                                                if (boltButton != null) {
                                                    allDone = false;
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    if (allDone) {
                                        // All ok
                                        _activated = false;
                                        _finished = true;
                                        _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.FINISHED_OK;
                                        _workingProcessPanel.CustomError = null;
                                        _workingProcessPanel.BoltSerialNum = null;

                                        // Change color back to black
                                        _torque.ForeColor = Color.Black;
                                        _angle.ForeColor = Color.Black;

                                        // Stop retrieve coordinates data
                                        if (_locating_enabled) {
                                            // Lock again in case _locating disabled
                                            _toolTasks.Values.Where(t => toolIds.Contains(t.DeviceId)).ToList().ForEach(toolTask => toolTask.SendLock());

                                            // Stop listening coordinates
                                            workstationDTOs.ForEach(dto => {
                                                int? armId = dto.arm_id;
                                                if (armId != null) {
                                                    ArmTask armTask = _armTasks[armId.Value];
                                                    armTask.RetrieveResult = false;
                                                    armTask.OnActionAfterReceiving -= ActionAfterArmDataReceived;
                                                }
                                            });
                                        }

                                        // Update mission result to ok
                                        _missionRecord.mission_result = (int) TighteningStatus.OK;
                                        _apis.AddOrUpdateMissionRecord(new(_missionRecord));

                                        // Clear all cached bar codes
                                        _barCodeObj.Reset();

                                        // // 重置任务信息
                                        // ResetMissionDetails();
                                    }
                                }

                                // Store data
                                dataDTO.tightening_status = (int) TighteningStatus.OK;
                                StoreTighteningData(dataDTO);
                            } else {
                                // Lock first
                                if (_locating_enabled) {
                                    // Lock all tools here
                                    _toolTasks.Values.Where(t => toolIds.Contains(t.DeviceId)).ToList().ForEach(toolTask => toolTask.SendLock());
                                }

                                // Change bolt status
                                currentBolt.BoltStatus = BoltStatus.ERROR;

                                // Count ng times
                                currentBolt.NgTimes++;

                                // Mission failed
                                if (currentBolt.NgTimes >= _mission.max_ng_num) {
                                    // Lock again in case _locating disabled
                                    _toolTasks.Values.Where(t => toolIds.Contains(t.DeviceId)).ToList().ForEach(toolTask => toolTask.SendLock());

                                    // Change mission status
                                    _activated = false;
                                    _finished = true;

                                    if (CheckIfIsMultiDeviceIndependenceMode()) {
                                        foreach (BoltButton bolt in _currentWorkingBoltIndependence.Values) {
                                            bolt.StopFlickering();
                                        }
                                        _currentWorkingBoltIndependence.Clear();
                                    } else {
                                        currentBolt.StopFlickering();
                                        _currentWorkingBolt = null;
                                    }

                                    _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.FINISHED_NG;
                                    _workingProcessPanel.CustomError = errorMsg;
                                    _workingProcessPanel.AppendDesc(_workingProcessPanel.CustomError);
                                    _workingProcessPanel.BoltSerialNum = null;

                                    // Change color back to black
                                    _torque.ForeColor = Color.Black;
                                    _angle.ForeColor = Color.Black;

                                    // Stop retrieve coordinates data
                                    if (_locating_enabled) {
                                        // Lock again in case _locating disabled
                                        _toolTasks.Values.Where(t => toolIds.Contains(t.DeviceId)).ToList().ForEach(toolTask => toolTask.SendLock());

                                        // Stop listening coordinates
                                        workstationDTOs.ForEach(dto => {
                                            int? armId = dto.arm_id;
                                            if (armId != null) {
                                                ArmTask armTask = _armTasks[armId.Value];
                                                armTask.RetrieveResult = false;
                                                armTask.OnActionAfterReceiving -= ActionAfterArmDataReceived;
                                            }
                                        });
                                    }

                                    // Clear all cached bar codes
                                    _barCodeObj.Reset();

                                    // 重置任务信息
                                    ResetMissionDetails();

                                    // 记录数据
                                    StoreTighteningData(dataDTO);

                                    // 先记录数据再弹出提示
                                    WidgetUtils.ShowErrorPopUp($"同一点位NG次数已达到{_mission.max_ng_num}次，任务失败");
                                } else {
                                    // 扭矩角度数据颜色改成红色
                                    _torque.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                    _angle.ForeColor = ColorConfigs.COLOR_WORKING_PROCESS_RED;
                                    _needLoosening = true;
                                    _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.LOOSENING;
                                    // 记录数据
                                    StoreTighteningData(dataDTO);
                                    // 需要管理员密码弹窗
                                    if (_mission.password_need_time != 0 && currentBolt.NgTimes >= _mission.password_need_time) {
                                        _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_DISABLE;
                                        _workingProcessPanel.AppendDesc(_workingProcessPanel.AdminConfirmation);
                                        _adminConfirmed = false;
                                        // 先记录数据再打开弹窗
                                        NGConfirmPopUp();
                                    } else {
                                        _workingProcessPanel.WorkplaceProcessStatus = WorkplaceProcessStatus.OPERATION_ENABLE;
                                        _workingProcessPanel.CustomError = errorMsg;
                                        _workingProcessPanel.AppendDesc(_workingProcessPanel.CustomError);
                                    }
                                }
                                dataDTO.tightening_status = (int) TighteningStatus.NG;
                            }
                        } else {
                            // 反松时把扭矩角度改回黑色
                            _torque.ForeColor = Color.Black;
                            _angle.ForeColor = Color.Black;
                            _needLoosening = false;
                            _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.TIGHTENING;
                            _workingProcessPanel.CustomError = null;
                            if (MainUtils.GetStoreLooseningData()) {
                                // 记录数据
                                StoreTighteningData(dataDTO);
                            }
                        }
                    }
                });
            });
        }

        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            return false;
        }

        public override void VisibleToTrue() {
            SetOperatorInfo();
        }
    }

}
