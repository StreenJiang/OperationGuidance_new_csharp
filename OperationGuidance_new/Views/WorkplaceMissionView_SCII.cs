using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views {
    public class WorkplaceMissionView_SCII: AWorkplaceMissionView<WorkplaceContentPanel_SCII, WorkplaceTopBar_SCII> {
        public WorkplaceMissionView_SCII() { }
        public WorkplaceMissionView_SCII(bool operatorOpenning) : base(operatorOpenning) { }

        protected override WorkplaceContentPanel_SCII GetWrokplacePanel(int? missionId, WorkplaceTopBar topBar) {
            return new(missionId, missionName => {
                topBar.Title = missionName;
            }) {
                BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND_2,
                Margin = new Padding(0),
                PaddingWithoutBorder = true,
            };
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

        public WorkplaceContentPanel_SCII() { }
        public WorkplaceContentPanel_SCII(int? missionId, Action<string> resetMissionName) : base(missionId, resetMissionName) {
            _actionAfterSendingPset = SetPset;

            // 初始化所有组件
            InitializeOuters();
            InitializeTopRightMiddleRight();
            InitializeTopRightBottom();
            InitializeMiddle();
            InitializeBottom();

            _checkRedo = true;
            _toolControlNeedAdminPasswor = true;
        }

        protected override void ActionAfterAllInitialized() {
            CommonButton terminateMissionBtn = _missionSelectedName.AddButton<CommonButton>("中断");
            terminateMissionBtn.Enabled = true;
            terminateMissionBtn.Click += (s, e) => {
                if (_activated) {
                    _adminConfirmed = false;
                    OpenAdminPasswordPopUpForm("任务异常重置任务，请管理员输入权限密码", false);
                    if (_adminConfirmed.Value) {
                        _adminConfirmed = null;
                        TerminateMission(WorkplaceProcessStatus.FINISHED_NG);
                    }
                } else {
                    WidgetUtils.ShowNoticePopUp("任务未激活");
                }
            };
        }

        protected override void ActivateMissionAutomatically() { }

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
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                ConerRadius = WidgetUtils.ContainerRadius() / 2,
            };
            _barCodeOuter.Controls.Add(_barCodePictureBox);
            _barCodeOuter.Controls.Add(_barCodeTextBox);
            _barCodeOuter.Click += barCodePopUp;

            // 上方左边下面
            _imageDisplayOuter = new() {
                Parent = _topLeft,
                Margin = new(0),
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                ConerRadius = WidgetUtils.ContainerRadius() / 2,
            };
            _imageDisplayOuter.Controls.Add(_productImageDisplayPanel);

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
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                ConerRadius = WidgetUtils.ContainerRadius() / 2,
            };
            _topRightTop.Controls.Add(_operatorInfoTitle);
            _topRightTop.Controls.Add(_operatorName);
            _topRightTop.Controls.Add(_operatorId);

            // 上方右边的中间
            _topRightMiddle = new() {
                Parent = _topRight,
                Padding = new(0),
            };
            // 上方右边的中间的左边
            _topRightMiddleLeft = new() {
                Parent = _topRightMiddle,
                Padding = new(0),
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                ConerRadius = WidgetUtils.ContainerRadius() / 2,
            };
            _workingProcessPanel.ConerRadius = 0;
            _topRightMiddleLeft.Controls.Add(_workingProcessPanel);

            // 上方右边的中间的右边
            _topRightMiddleRight = new() {
                Parent = _topRightMiddle,
                Padding = new(0),
                FlowDirection = FlowDirection.TopDown,
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                ConerRadius = WidgetUtils.ContainerRadius() / 2,
            };
            // 上方右边的下面
            _topRightBottom = new() {
                Parent = _topRight,
                Padding = new(0),
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                ConerRadius = WidgetUtils.ContainerRadius() / 2,
            };

            // 中间
            _middle = new() {
                Parent = this,
                Padding = new(0),
                FlowDirection = FlowDirection.TopDown,
                ConerRadius = WidgetUtils.ContainerRadius() / 2,
            };

            // 下方
            _bottom = new() {
                Parent = this,
                Padding = new(0),
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                ConerRadius = WidgetUtils.ContainerRadius() / 2,
            };
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

            if (_barCodePopUpForm == null || _barCodePopUpForm.IsDisposed) {
                _barCodePopUpForm = new BarCodeInputPopUpForm_SCII(this, ConfigsVariables.BAR_CODE_NOTE, _mission, _activated,
                        _productBarCodeMatchingRules, _partsBarCodeMatchingRules, barCode) {
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
            _topRightBottom.Controls.Add(_missionDetailTitle);
            _topRightBottom.Controls.Add(_missionSelectedName);
            _topRightBottom.Controls.Add(_productBatch);
            _topRightBottom.Controls.Add(_productSumPerDay);
            _topRightBottom.Controls.Add(_okSumPerDay);
            _topRightBottom.Controls.Add(_ngRatePerDay);
            _topRightBottom.Controls.Add(_pset);
        }

        protected override void ActionAfterSwitchMission() {
            base.ActionAfterSwitchMission();
            ResetMissionDetails();
        }

        private void ResetMissionDetails() {
            SetTodayData();
            SetPset();
        }
        private void SetTodayData() {
            int sum = 0;
            int okSum = 0;
            double ngRate = 0;

            if (_mission.id > 0) {
                List<MissionRecordDTO> missionRecordDTOs = _apis.QueryMissionRecordList(new() {
                    Date = DateTime.Now,
                    MissionId = _mission.id,
                }).MissionRecordDTOs;
                sum = missionRecordDTOs.Count;
                okSum = missionRecordDTOs.Where(dto => dto.mission_result == (int) TighteningStatus.OK).Count();
                if (sum > 0) {
                    ngRate = (sum - okSum) / (double) sum * 100;
                }
            }

            _productSumPerDay.SetValue(0, sum + "");
            _okSumPerDay.SetValue(0, okSum + "");
            _ngRatePerDay.SetValue(0, $"{ngRate.ToString("F2")}%");
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
        protected override void RefreshImageDisplayPanel() => ResizeTopLeftBottom();

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
                block.BorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER;
                _bottom.Controls.Add(block);
            }
            _bottom.Controls.Add(_timeDisplayerOuter);
        }

        protected override async void SetMissionDetails() {
            _missionSelectedName.SetValue(0, _mission.name);
            ResetMissionDetails();

            // Don't need this anymore
            // await Task.Run(async () => {
            //     while (!IsHandleCreated) {
            //         await Task.Delay(200);
            //     }
            //     BeginInvoke(() => {
            //         MissionRecordDTO? missionRecordDTO = _apis.QueryLatestMissionRecord(new(SystemUtils.LoggedUserId)).MissionRecordDTO;
            //         // 存在可以回填的数据
            //         if (missionRecordDTO != null) {
            //             // 刚登录
            //             if (MainUtils.LoginFlag) {
            //                 // 需要回填确认
            //                 if (MainUtils.IsProductBatchNoticeEnabled()) {
            //                     // 弹出提示确认是否回填
            //                     if (WidgetUtils.ShowConfirmPopUp($"是否继续批次【{missionRecordDTO.product_batch}】？")) {
            //                         MainUtils.LastProductBatch = missionRecordDTO.product_batch;
            //                     } else {
            //                         MainUtils.LastProductBatch = null;
            //                     }
            //                 }
            //                 // 不需要提示则直接回填
            //                 else {
            //                     MainUtils.LastProductBatch = missionRecordDTO.product_batch;
            //                 }
            //             }
            //             // 最新查到的批次信息与缓存的不一致，则换掉
            //             else if (MainUtils.LastProductBatch != missionRecordDTO.product_batch) {
            //                 MainUtils.LastProductBatch = missionRecordDTO.product_batch;
            //             }
            //             // 不管是否回填，登录标识都要改
            //             MainUtils.LoginFlag = false;
            //             // 不为空就回填
            //             if (!string.IsNullOrEmpty(MainUtils.LastProductBatch)) {
            //                 _productBatch.SetValue(0, MainUtils.LastProductBatch);
            //             }
            //         }
            //     });
            // });
        }

        protected override void ActionAfterArmDataReceived(int maxValue, Coordinates3D armCoordinates) {
            Task.Run(() => {
                BeginInvoke(() => {
                    if (_activated && _currentWorkingBolt != null) {
                        ProductBoltDTO boltDTO = _currentWorkingBolt.BoltDTO;
                        int? toolId = _workstationsDTOs.Single(dto => dto.id == boltDTO.workstation_id).tool_id;
                        if (toolId != null) {
                            ToolTask toolTask = _toolTasks[toolId.Value];
                            Coordinates3D boltCoordinates = Coordinates3D.FromString(boltDTO.position);
                            _realTimeArmCoordinates = armCoordinates;

                            // Can't lock/unlock tools manually while arm is running (Only for SCII)
                            RemoveLockMsg(WorkingProcessPanel.UnlockedManually);
                            RemoveLockMsg(WorkingProcessPanel.LockedManually);
                            if (CheckArmPosition(maxValue, armCoordinates, boltCoordinates)) {
                                // Location ok, so remove locked reason of position
                                RemoveLockMsg(WorkingProcessPanel.LockedArmPosition);
                            } else {
                                // Location because of position
                                AddLockMsg(WorkingProcessPanel.LockedArmPosition);
                            }

                            // 需要管理员输入密码并确认
                            if (_adminConfirmed != null) {
                                // 管理员已确认
                                if (_adminConfirmed.Value) {
                                    RemoveLockMsg(WorkingProcessPanel.AdminConfirmation);
                                    _adminConfirmed = null;
                                }
                                // 管理员未确认
                                else {
                                    AddLockMsg(WorkingProcessPanel.AdminConfirmation);
                                    if (_adminPasswordPopUpForm == null || _adminPasswordPopUpForm.IsDisposed) {
                                        _adminConfirmed = false;
                                        BoltNGConfirmPopUp();
                                    }
                                }
                            } else {
                                RemoveLockMsg(WorkingProcessPanel.AdminConfirmation);
                            }

                            // 当前点位没有设置程序号
                            if (_currentWorkingBolt.CurrentParameterSet == null) {
                                // 如果是没有配置就显示对应错误信息，否则可能是下发失败
                                if (_currentWorkingBolt.BoltDTO.parameters_set == null) {
                                    AddLockMsg(WorkingProcessPanel.LockedPsetNull);
                                } else {
                                    RemoveLockMsg(WorkingProcessPanel.LockedPsetNull);
                                }
                            } else {
                                RemoveLockMsg(WorkingProcessPanel.LockedPsetNull);
                            }
                        }
                    }
                });
            });
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
            Rectangle? imageRange = _productImageFiles[_currentSideIndex].ImageRange;

            // 重新计算螺栓点位按钮的大小和位置
            int btnSide = (int) (newPanelSize.Height * .085) + (int) (Math.Abs(newPanelSize.Width - newPanelSize.Height) * .02);
            foreach (KeyValuePair<int, List<BoltButton>> pair in _allBolts) {
                foreach (BoltButton boltButton in pair.Value) {
                    boltButton.Size = new(btnSide, btnSide);
                    int newX;
                    int newY;
                    if (imageRange != null) {
                        newX = imageRange.Value.Location.X + (int) (imageRange.Value.Width * boltButton.BoltDTO.location_x_percent / 100) - btnSide / 2;
                        newY = imageRange.Value.Y + (int) (imageRange.Value.Height * boltButton.BoltDTO.location_y_percent / 100) - btnSide / 2;
                    } else {
                        newX = _productImageDisplayPanel.MaxRectLocation.X + (int) (_productImageDisplayPanel.MaxRectWidth * boltButton.BoltDTO.location_x_percent / 100) - btnSide / 2;
                        newY = _productImageDisplayPanel.MaxRectLocation.Y + (int) (_productImageDisplayPanel.MaxRectHeight * boltButton.BoltDTO.location_y_percent / 100) - btnSide / 2;
                    }
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
        //     _last.ClicSk += (sender, eventArgs) => {
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

        protected override void ToolOperationPopUpFormExtraActions(ToolOperationPopUpForm popUpForm) {
            if (_activated) {
                popUpForm.BtnLock.Enabled = false;
                popUpForm.BtnUnlock.Enabled = false;
            }
        }

        protected override void AdminPopUpExtraActions() {
            if (_adminPasswordPopUpForm != null && !_adminPasswordPopUpForm.IsDisposed) {
                _adminPasswordPopUpForm.CloseButton.Enabled = false;
                _adminPasswordPopUpForm.Buttons[1].Enabled = false;
            }
        }

        protected void MissionNGConfirmPopUp(string msg) => OpenAdminPasswordPopUpForm(msg, true);

        public override void ActivateMission() {
            base.ActivateMission();

            if (_activated) {
                // Clear data grid view
                _tighteningDataVOs.Clear();
                RefreshTighteningDataPanel();

                if (_missionRecord != null) {
                    _missionRecord.product_batch = _productBatch.GetTextBox(0).Box.Text;
                    _apis.AddOrUpdateMissionRecord(new(_missionRecord));
                }
            }
        }

        protected override bool ValidationBeforeActivatingMission() {
            if (base.ValidationBeforeActivatingMission()) {
                // Count screw bit used time
                ScrewBitCounterDTO screwBitCounter;
                if (!CountScrewBitUsedTime(out screwBitCounter)) {
                    _adminConfirmed = false;
                    OpenAdminPasswordPopUpForm($"({screwBitCounter.bit_position})号位批头将超过使用上限【{screwBitCounter.max_num}次】，需更换批头。更换批头后，请输入管理员密码", false);
                    if (_adminConfirmed.Value) {
                        _adminConfirmed = null;
                        screwBitCounter.current_counts = 0;
                        _apis.AddOrUpdateScrewBitCounter(new(screwBitCounter));

                        // Check again to ensure no more screw bit needs to be replaced
                        return ValidationBeforeActivatingMission();
                    }
                    return false;
                }
                return true;
            }
            return false;
        }

        private bool CountScrewBitUsedTime(out ScrewBitCounterDTO screwBitCounter) {
            List<ScrewBitCounterDTO> screwBitCounterDTOs = _apis.FindScrewBitCounterByMissionId(new(_mission.id)).ScrewBitCounterDTOs;

            // Check first
            foreach (ScrewBitCounterDTO sbc in screwBitCounterDTOs) {
                if (sbc.current_counts + sbc.count_each_time > sbc.max_num) {
                    screwBitCounter = sbc;
                    return false;
                }
            }

            // Update
            foreach (ScrewBitCounterDTO sbc in screwBitCounterDTOs) {
                sbc.current_counts += sbc.count_each_time;
                _apis.AddOrUpdateScrewBitCounter(new(sbc));
            }

            screwBitCounter = new();
            return true;
        }

        protected override async void DoAfterRecevingTighteningDataAsync(TighteningData data, int deviceId) {
            await Task.Run(() => {
                BeginInvoke(() => {
                    // Nonactivated or finished will not handle any received data
                    if (!_activated) {
                        return;
                    }

                    try {
                        ToolTask toolTask = _toolTasks[deviceId];
                        // Lock first
                        toolTask.SendLock();
                        if (toolTask.WorkstationId != null && _currentWorkingBolt != null) {
                            logger.Info($"Action running after received tightening data...");

                            int workstationId = toolTask.WorkstationId.Value;

                            List<int> workstationIds = new();
                            foreach (List<BoltButton> bolts in _allBolts.Values) {
                                workstationIds.AddRange(bolts.Select(b => b.BoltDTO.workstation_id));
                            }
                            workstationIds = workstationIds.Distinct().ToList();
                            List<WorkstationDTO> workstationDTOs = _workstationsDTOs.Where(dto => workstationIds.Contains(dto.id) && dto.arm_id != null).ToList();
                            List<int?> toolIds = workstationDTOs.Select(dto => dto.tool_id).ToList();

                            // Main display
                            _torque.Text = data.torque + "";
                            _angle.Text = data.angle + "";

                            // Get current bolt
                            BoltButton currentBolt = _currentWorkingBolt;
                            ProductBoltDTO boltDTO = currentBolt.BoltDTO;
                            OperationDataDTO dataDTO = new();
                            CommonUtils.ObjectConverter<TighteningData, OperationDataDTO>(data, dataDTO);
                            // Set pset manualy if tool type is sudong x7
                            if (toolTask.ToolType is ToolSudongX7 toolX7) {
                                dataDTO.parameter_set_number = currentBolt.CurrentParameterSet;
                            }

                            WorkstationDTO workstationDTO = _workstationsDTOs.Single(dto => dto.id == workstationId);
                            dataDTO.workstation_id = workstationDTO.id;
                            dataDTO.workstation_name = workstationDTO.name;

                            DeviceToolDTO toolDTO = _tools.Single(t => t.id == deviceId);
                            dataDTO.tool_name = toolDTO.name;
                            dataDTO.tool_ip = $"{toolDTO.ip}:{toolDTO.port}";
                            dataDTO.tool_type = DeviceType_Tool.GetById(toolDTO.type).Name;
                            dataDTO.product_sied_id = _sides[_currentSideIndex].id;
                            dataDTO.bolt_serial_num = boltDTO.serial_num;
                            dataDTO.mission_record_id = _missionRecord.id;
                            dataDTO.vin_number = _missionRecord.product_bar_code;
                            if (_realTimeArmCoordinates != null) {
                                dataDTO.arm_position = _realTimeArmCoordinates.ToString();
                            }

                            // If result type is tightening
                            if (data.result_type == (int) TightenOrLoosen.TIGHTENING) {
                                bool tighteningOK = true;
                                string errorMsg = "";
                                // Initialize color to ok
                                _torque.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_OK;
                                _angle.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_OK;

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
                                    RemoveInformationMsg(_workingProcessPanel.NGReasons);
                                    _workingProcessPanel.NGReasons = null;

                                    currentBolt.BoltStatus = BoltStatus.DONE;
                                    currentBolt.Label = data.torque.ToString("0.00");

                                    // Check next index
                                    List<BoltButton> currentSideBolts = _allBolts[_sides[_currentSideIndex].id];
                                    int nextIndex = currentSideBolts.IndexOf(currentBolt) + 1;
                                    // 检查是否存在跳点的情况
                                    while (nextIndex < currentSideBolts.Count && currentSideBolts[nextIndex].BoltStatus == BoltStatus.DONE) {
                                        nextIndex++;
                                    }

                                    // Store data
                                    dataDTO.tightening_status = (int) TighteningStatus.OK;
                                    StoreTighteningData(dataDTO);

                                    if (nextIndex < currentSideBolts.Count) {
                                        _currentWorkingBolt = SwitchBolt(nextIndex);
                                        ChangeBoltStatusToWorking(_currentWorkingBolt);
                                    } else {
                                        // Update mission result to ok
                                        _missionRecord.mission_result = (int) TighteningStatus.OK;
                                        _apis.AddOrUpdateMissionRecord(new(_missionRecord));

                                        // 重置任务信息
                                        ResetMissionDetails();

                                        TerminateMission(WorkplaceProcessStatus.FINISHED_OK);
                                    }
                                } else {
                                    // Change bolt status
                                    currentBolt.BoltStatus = BoltStatus.ERROR;

                                    // Count ng times
                                    currentBolt.NgTimes++;

                                    // Set custom error message
                                    _workingProcessPanel.NGReasons = errorMsg;
                                    AddInformationMsg(_workingProcessPanel.NGReasons);

                                    // Mission failed
                                    if (_mission.max_ng_num != 0 && currentBolt.NgTimes >= _mission.max_ng_num) {
                                        // 重置任务信息
                                        ResetMissionDetails();

                                        // 记录数据
                                        StoreTighteningData(dataDTO);

                                        // Stop the mission
                                        TerminateMission(WorkplaceProcessStatus.FINISHED_NG);

                                        // 先记录数据再弹出提示
                                        // WidgetUtils.ShowErrorPopUp($"同一点位NG次数已达到{_mission.max_ng_num}次，任务失败");
                                        MissionNGConfirmPopUp($"同一点位NG次数已达到{_mission.max_ng_num}次，任务失败。请输入管理员密码");
                                    } else {
                                        _needLoosening = true;
                                        _workingProcessPanel.TightenOrLoosen = TightenOrLoosen.LOOSENING;

                                        // 记录数据
                                        StoreTighteningData(dataDTO);

                                        // 需要管理员密码弹窗
                                        if (_mission.password_need_time != 0 && currentBolt.NgTimes >= _mission.password_need_time) {
                                            AddLockMsg(WorkingProcessPanel.AdminConfirmation);
                                            _adminConfirmed = false;

                                            // 先记录数据再打开弹窗
                                            BoltNGConfirmPopUp();
                                        }
                                    }

                                    dataDTO.tightening_status = (int) TighteningStatus.NG;
                                }
                            } else {
                                _needLoosening = false;

                                // 反松结束后把扭矩角度改回黑色
                                _torque.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NORMAL;
                                _angle.ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NORMAL;

                                // Remove error message
                                RemoveLockMsg(_workingProcessPanel.NGReasons);
                                _workingProcessPanel.NGReasons = null;

                                if (MainUtils.GetStoreLooseningData()) {
                                    // 记录数据
                                    StoreTighteningData(dataDTO);
                                }
                            }
                        }
                    } catch (Exception e) {
                        logger.Error($"Error occurred while handling tightening data, e: {e}");
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
