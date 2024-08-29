using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Views.AbstractViews;
using OperationGuidance_new.Views.ReusableWidgets;

namespace OperationGuidance_new.Views {
    public class WorkplaceMissionView: AWorkplaceMissionView<WorkplaceContentPanel, WorkplaceTopBar> {
        public WorkplaceMissionView() { }
        public WorkplaceMissionView(bool operatorOpenning) : base(operatorOpenning) { }

        protected override WorkplaceContentPanel GetWrokplacePanel(int? missionId, WorkplaceTopBar topBar) {
            return new(missionId, missionName => {
                topBar.Title = missionName;
            }) {
                BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND_2,
                Margin = new Padding(0),
            };
        }
    }

    public class WorkplaceContentPanel: AWorkplaceContentPanel {
        // 上方
        protected CustomContentPanel _top;
        // 上方左边
        protected CustomContentPanel _topLeft;
        // 上方左边上面
        protected WorkplacePiece _barCodeOuter;
        // 上方左边下面
        protected WorkplacePiece _imageDisplayOuter;
        // 上方右边
        protected CustomContentPanel _topRight;
        // 上方右边的上面
        protected WorkplacePiece _topRightTop;
        // 上方右边的中间
        protected CustomContentPanel _topRightMiddle;
        // 上方右边的中间的左边
        protected WorkplacePiece _topRightMiddleTop;
        // 上方右边的中间的右边
        protected WorkplacePiece _topRightMiddleBottom;
        // 上方右边的下面
        protected WorkplacePiece _topRightBottom;

        // 下方
        protected WorkplacePiece _bottom;


        // private Label _productSideTitle;
        // private List<Image?> _smallSideImagesForShowing;
        // private PictureBox _smallSideImage;
        // private TableLayoutPanel _buttonPanel;
        // private PageSwitchButton _first;
        // private PageSwitchButton _backward;
        // private PageSwitchButton _forward;
        // private PageSwitchButton _last;
        // private Label _pageInfo;

        public WorkplaceContentPanel() { }
        public WorkplaceContentPanel(int? missionId, Action<string> resetMissionName) : base(missionId, resetMissionName) {
            // 初始化所有组件
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
                ConerRadius = WidgetUtils.ContainerRadius(),
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
            };
            _barCodeOuter.Controls.Add(_barCodePictureBox);
            _barCodeOuter.Controls.Add(_barCodeTextBox);
            _barCodeOuter.Click += barCodePopUp;

            // 上方左边下面
            _imageDisplayOuter = new() {
                Parent = _topLeft,
                Margin = new(0),
                ConerRadius = WidgetUtils.ContainerRadius(),
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
                FlowDirection = FlowDirection.TopDown,
                ConerRadius = WidgetUtils.ContainerRadius(),
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
            };
            _topRightTop.Controls.Add(_operatorInfoTitle);
            _topRightTop.Controls.Add(_operatorName);
            _topRightTop.Controls.Add(_operatorId);

            // 上方右边的中间
            _topRightMiddle = new() {
                Parent = _topRight,
                Padding = new(0),
            };
            // 上方右边的中间的上面
            _topRightMiddleTop = new() {
                Parent = _topRightMiddle,
                Padding = new(0),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            _topRightMiddleTop.Controls.Add(_workingProcessPanel);

            // 上方右边的中间的下面
            _topRightMiddleBottom = new() {
                Parent = _topRightMiddle,
                Padding = new(0),
                OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
            };
            _topRightMiddleBottom.Controls.Add(_torquePanel);
            _topRightMiddleBottom.Controls.Add(_anglePanel);

            // 上方右边的下面
            _topRightBottom = new() {
                Parent = _topRight,
                Padding = new(0),
                ConerRadius = WidgetUtils.ContainerRadius(),
                BackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
            };
            _missionSelectedName.Ratio = 7.5;
            _pset.Ratio = 7.5;
            _currentSideName.Ratio = 7.5;
            _topRightBottom.Controls.Add(_missionDetailTitle);
            _topRightBottom.Controls.Add(_missionSelectedName);
            _topRightBottom.Controls.Add(_currentSideName);
            _topRightBottom.Controls.Add(_pset);

            // 下方
            _bottom = new() {
                Parent = this,
                Padding = new(0),
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = ColorConfigs.COLOR_WORKPLACE_BOTTOM_BAR_BACKGROUND,
            };
            foreach (DeviceBlock block in _deviceBlocks) {
                block.ConerRadius = WidgetUtils.ControlRadius();
                _bottom.Controls.Add(block);
            }
            _bottom.Controls.Add(_timeDisplayerOuter);
        }

        protected override void RefreshImageDisplayPanel() => ResizeTopLeftBottom();

        protected override void SetMissionDetails() {
            _missionSelectedName.SetValue(0, _mission.name);
            if (_sides.Count > 0 && _currentSideIndex >= 0) {
                _currentSideName.SetValue(0, _sides[_currentSideIndex].name);
            }
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            if (IsHandleCreated && !IsDisposed) {
                Padding contentPadding = WidgetUtils.ContentPadding();
                int panelPadding = WidgetUtils.ContentInnerBorderMargin() * 2;

                int boxHeight = (int) (WidgetUtils.MainSize.Height * .038);
                int titleHeight = (int) (boxHeight * 1.2);
                int contentVPadding = (int) (boxHeight * .4);
                int contentHPadding = contentVPadding;
                Font titleFont = new Font(WidgetsConfigs.SystemFontFamily, titleHeight * .45f, FontStyle.Bold, GraphicsUnit.Pixel);

                ResizeOuters(panelPadding, boxHeight, titleHeight, contentVPadding);
                ResizeTopLeftTop();
                ResizeTopLeftBottom();
                ResizeTopRightTop(boxHeight, titleHeight, contentVPadding, contentHPadding, titleFont);
                ResizeTopRightMiddleLeft();
                ResizeTopRightMiddleRight(panelPadding);
                ResizeTopRightBottom(boxHeight, titleHeight, contentVPadding, contentHPadding, titleFont);
                ResizeBottom();
                Invalidate();
            }
        }

        // 计算尺寸： 外框
        protected virtual void ResizeOuters(int panelPadding, int boxHeight, int titleHeight, int contentVPadding) {
            int workplaceWidth = Width - panelPadding * 2;
            int workplaceHeight = Height - panelPadding; // Bottom panel has to dock at bottom
            int bottomHeight = (int) (workplaceHeight * .055);
            int topHeight = workplaceHeight - bottomHeight - panelPadding;
            int barCodeHeight = (int) (workplaceHeight * .06);
            int imagePanelHeight = topHeight - barCodeHeight - panelPadding;
            int topLeftWidth = (int) (workplaceWidth * .65) + (int) (Math.Abs(workplaceWidth - workplaceHeight) * .25);
            int topRightWidth = workplaceWidth - topLeftWidth - panelPadding;

            int topRightTopHeight = titleHeight + boxHeight * (_topRightTop.Controls.Count - 1) + contentVPadding * (_topRightTop.Controls.Count + 1);
            int topRightBottomHeight = titleHeight + boxHeight * (_topRightBottom.Controls.Count - 1) + contentVPadding * (_topRightBottom.Controls.Count + 1);
            int topRightMiddleHeight = topHeight - topRightTopHeight - topRightBottomHeight - panelPadding * 2;
            int topRightMiddleTopHeight = (int) (topRightMiddleHeight * .65);
            int topRightMiddleBottomHeight = topRightMiddleHeight - topRightMiddleTopHeight - panelPadding;

            // 上方
            _top.Size = new(workplaceWidth, topHeight);
            _top.Margin = new(panelPadding);
            // 上方左边
            _topLeft.Size = new(topLeftWidth, topHeight);
            // 上方左边上面
            _barCodeOuter.Size = new(topLeftWidth, barCodeHeight);
            _barCodeOuter.Margin = new(0, 0, 0, panelPadding);
            // 上方左边下面
            _imageDisplayOuter.Size = new(topLeftWidth, imagePanelHeight);
            // 上方右边
            _topRight.Size = new(topRightWidth, topHeight);
            _topRight.Margin = new(panelPadding, 0, 0, 0);
            // 上方右边的上面
            _topRightTop.Size = new(topRightWidth, topRightTopHeight);
            _topRightTop.Margin = new(0, 0, 0, panelPadding);
            // 上方右边的中间
            _topRightMiddle.Size = new(topRightWidth, topRightMiddleHeight);
            _topRightMiddle.Margin = new(0, 0, 0, panelPadding);
            // 上方右边的中间的上面
            _topRightMiddleTop.Size = new(topRightWidth, topRightMiddleTopHeight);
            _topRightMiddleTop.Margin = new(0, 0, 0, panelPadding);
            // 上方右边的中间的下面
            _topRightMiddleBottom.Size = new(topRightWidth, topRightMiddleBottomHeight);
            _topRightMiddleBottom.Margin = new(0, 0, 0, panelPadding);
            // 上方右边的下面
            _topRightBottom.Size = new(topRightWidth, topRightBottomHeight);

            // 下方
            _bottom.Size = new(Width, bottomHeight);
        }

        // 计算尺寸： 条码框
        protected virtual void ResizeTopLeftTop() {
            // icon的边长
            int side = (int) (_barCodePictureBox.Parent.Height * .5);
            Padding iconMargin = new(side, (_barCodePictureBox.Parent.Height - side) / 2, 0, 0);
            // Size of text box
            int newH = (int) (_barCodePictureBox.Parent.Height * .875);
            Size textBoxSize = new(_barCodePictureBox.Parent.Width - side * 2 - iconMargin.Left, newH);
            Padding textBoxMargin = new(0, (_barCodePictureBox.Parent.Height - newH) / 2, 0, 0);

            if (_barCodePictureBox.Parent is CustomContentPanel parent) {
                if (parent.ConerRadius > 0) {
                    iconMargin.Left += 1;

                    textBoxSize.Width -= 1;
                    textBoxSize.Height -= 1;
                }
            }

            // 重设icon
            _barCodePictureBox.Image = WidgetUtils.ResizeImage(_barCodeImage, side, side);
            _barCodePictureBox.Margin = iconMargin;
            _barCodePictureBox.Size = new(side, side);

            // 重设输入框
            _barCodeTextBox.Size = textBoxSize;
            _barCodeTextBox.Margin = textBoxMargin;

            // 重新计算弹框的大小
            ResizeBarCodePopUpForm();
        }
        protected virtual void ResizeBarCodePopUpForm() {
            if (_barCodePopUpForm != null && !_barCodePopUpForm.IsDisposed) {
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
        protected virtual void ResizeTopLeftBottom() {
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
        protected virtual void ResizeTopRightTop(int boxHeight, int titleHeight, int contentVPadding,
                int contentHPadding, Font titleFont) {
            // Resize title and font
            _operatorInfoTitle.Size = new(_operatorInfoTitle.Parent.Width, titleHeight);
            _operatorInfoTitle.Margin = new(contentHPadding, contentVPadding, 0, 0);
            _operatorInfoTitle.Font = titleFont;
            // Resize content size and font
            int boxWidth = _operatorInfoTitle.Width - contentHPadding * 2;
            _operatorName.Size = new(boxWidth, boxHeight);
            _operatorName.Margin = new(contentHPadding, contentVPadding, 0, 0);
            _operatorId.Size = new(boxWidth, boxHeight);
            _operatorId.Margin = new(contentHPadding, contentVPadding, 0, 0);
        }

        // 计算尺寸： 实时状态框
        protected virtual void ResizeTopRightMiddleLeft() {
            _workingProcessPanel.Size = _workingProcessPanel.Parent.Size;
        }

        // 计算尺寸： 实时扭矩、角度框
        protected virtual void ResizeTopRightMiddleRight(int panelPadding) {
            Size panelSize = new((_topRightMiddleBottom.Width - panelPadding) / 2, _topRightMiddleBottom.Height);
            _torquePanel.Size = panelSize;
            _torquePanel.Margin = new(0, 0, panelPadding, 0);
            _anglePanel.Size = panelSize;
        }

        // 计算尺寸： 任务信息框
        protected virtual void ResizeTopRightBottom(int boxHeight, int titleHeight, int contentVPadding,
                int contentHPadding, Font titleFont) {
            // Resize title and font
            _missionDetailTitle.Size = new(_operatorInfoTitle.Parent.Width, titleHeight);
            _missionDetailTitle.Margin = new(contentHPadding, contentVPadding, 0, 0);
            _missionDetailTitle.Font = titleFont;
            // Resize content size and font
            int boxWidth = _operatorInfoTitle.Parent.Width - contentHPadding * 2;
            foreach (Control ctrl in _topRightBottom.Controls) {
                if (ctrl is CustomTextBoxGroup box) {
                    box.Size = new(boxWidth, boxHeight);
                    box.Margin = new(contentHPadding, contentVPadding, 0, 0);
                }
            }
        }

        // 计算尺寸： 底部横框
        protected virtual void ResizeBottom() {
            int blocksWidth = 0;
            int blockSide = (int) (_bottom.Height * .85);
            int padding = (_bottom.Height - blockSide) / 2;
            int blockCount = 0;
            foreach (Control control in _bottom.Controls) {
                if (control is DeviceBlock) {
                    control.Size = new(blockSide, blockSide);
                    control.Margin = new(0, padding, padding, 0);
                    blocksWidth += blockSide;
                    blockCount++;
                }
            }
            blocksWidth += padding * blockCount;
            int timeDisplayerWidth = _bottom.Width - blocksWidth;
            _timeDisplayerOuter.Size = new(timeDisplayerWidth - 2, _bottom.Height - 2);
            _timeDisplayer.Font = new Font(WidgetsConfigs.SystemFontFamily, _bottom.Height * .325f, FontStyle.Regular, GraphicsUnit.Pixel);
            _timeDisplayer.Margin = new(_timeDisplayer.Height / 3, (_timeDisplayerOuter.Height - _timeDisplayer.Height) / 2, 0, 0);
        }

        // private void ResizeMiddleBottom() {
        //     // Resize title
        //     _productSideTitle.Size = new(_middleBottom.Width - 2, (int) (_middleBottom.Height * .2));
        //     // Reset font size
        //     ResetRightBottomTitleFont();
        //     // Resize product side image
        //     int imageWholeHeight = (int) ((_middleBottom.Height - 2 - _productSideTitle.Height) * .815);
        //     int vPadding = (int) (imageWholeHeight * .1);
        //     int imageHeight = imageWholeHeight - vPadding * 2;
        //     if (_missionImages.Count > 0) {
        //         for (int i = 0 ; i < _missionImages.Count ; i++) {
        //             Image? image = _missionImages[i];
        //             Size newISize;
        //             if (image == null) {
        //                 image = _defaultImage;
        //                 newISize = new((int) (imageHeight / (decimal) _defaultImage.Height * _defaultImage.Width), imageHeight);
        //                 _smallSideImagesForShowing[i] = WidgetUtils.ResizeImageWithoutLosingQuality(_defaultImage, newISize);
        //             }
        //             newISize = new((int) (imageHeight / (decimal) image.Height * image.Width), imageHeight);
        //             Image imageNew = WidgetUtils.ResizeImageWithoutLosingQuality(image, newISize);
        //             _smallSideImagesForShowing[i] = imageNew;
        //             if (i == _currentSideIndex) {
        //                 ResizeSmallSideImageBox(imageNew);
        //             }
        //         }
        //     }
        //     // Resize table panel 
        //     int tablePanelHeight = _middleBottom.Height - 4 - _productSideTitle.Height - imageWholeHeight;
        //     int buttonSide = (int) (tablePanelHeight * .725);
        //     int buttonVPadding = (tablePanelHeight - buttonSide) / 2;
        //     int buttonHPdding = (int) (buttonSide * .45);
        //     _buttonPanel.Size = new(_middleBottom.Width - 2 - buttonHPdding * 2, tablePanelHeight);
        //     _buttonPanel.Margin = new(buttonHPdding, 0, buttonHPdding, 0);
        //     // Resize icon button
        //     _first.Size = new(buttonSide, buttonSide);
        //     _first.Margin = new(buttonHPdding, buttonVPadding, buttonHPdding, buttonVPadding);
        //     _backward.Size = new(buttonSide, buttonSide);
        //     _backward.Margin = new(buttonHPdding, buttonVPadding, buttonHPdding, buttonVPadding);
        //     _forward.Size = new(buttonSide, buttonSide);
        //     _forward.Margin = new(buttonHPdding, buttonVPadding, buttonHPdding, buttonVPadding);
        //     _last.Size = new(buttonSide, buttonSide);
        //     _last.Margin = new(buttonHPdding, buttonVPadding, buttonHPdding, buttonVPadding);
        //     // Resize page info label
        //     _pageInfo.Size = new(_buttonPanel.Width - 4 * buttonSide - buttonHPdding * 8, tablePanelHeight);
        //     _pageInfo.Margin = new(0, 0, 0, 0);
        //     _pageInfo.Font = new(WidgetsConfigs.SystemFontFamily, _pageInfo.Height * .675F, FontStyle.Bold, GraphicsUnit.Pixel);
        // }
        //
        // private void ResizeSmallSideImageBox(Image? newImage) {
        //     if (newImage != null) {
        //         int imageWholeHeight = (int) ((_middleBottom.Height - 2 - _productSideTitle.Height) * .8);
        //         int vPadding = (int) (imageWholeHeight * .1);
        //         int hPadding = (_middleBottom.Width - 2 - newImage.Width) / 2;
        //         _smallSideImage.Size = newImage.Size;
        //         _smallSideImage.Image = newImage;
        //         _smallSideImage.Margin = new(hPadding, vPadding, hPadding, vPadding);
        //     }
        // }
        //
        // private void ResetRightBottomTitleFont(float fontRatio = .55f) {
        //     Font font = new Font(WidgetsConfigs.SystemFontFamily, _productSideTitle.Height * fontRatio, FontStyle.Bold, GraphicsUnit.Pixel);
        //     using (Graphics g = CreateGraphics()) {
        //         if (g.MeasureString(_productSideTitle.Text, font).Width >= _productSideTitle.Width * .9) {
        //             ResetRightBottomTitleFont(fontRatio -= .025f);
        //         } else {
        //             _productSideTitle.Font = font;
        //         }
        //     }
        // }

        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            return false;
        }

        public override void VisibleToTrue() {
            SetOperatorInfo();
        }
    }

}
