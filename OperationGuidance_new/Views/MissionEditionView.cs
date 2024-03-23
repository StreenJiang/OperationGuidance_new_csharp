using System.Drawing.Drawing2D;
using CustomLibrary.Buttons;
using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Configs;
using CustomLibrary.Events;
using CustomLibrary.Panels;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.Utils;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;
using Timer = System.Windows.Forms.Timer;
using CustomLibrary.TextBoxes;
using CustomLibrary.Forms;
using OperationGuidance_new.Constants;

namespace OperationGuidance_new.Views {
    public partial class MissionEditionView: CustomContentPanel {
        private ProductMissionDTO _missionDTO;
        private MissionEditionPage? _editionPage;

        public ProductMissionDTO MissionDTO { get => _missionDTO; set => _missionDTO = value; }
        public MissionEditionPage? EditionPage { get => _editionPage; set => _editionPage = value; }

        public MissionEditionView() {
            CreateANewOne();
        }

        public MissionEditionPage CreateANewOne() {
            _missionDTO = new() {
                name = "新建任务",
                ProductSides = new(),
            };
            return OpenEditionPage(_missionDTO);
        }

        public MissionEditionPage OpenEditionPage(ProductMissionDTO missionDTO) {
            // Clear all child controls
            Controls.Clear();
            // Create a new page according to missionbody and show
            if (_editionPage != null) {
                _editionPage.Dispose();
            }
            _missionDTO = missionDTO;
            _editionPage = new(this, missionDTO);
            _editionPage.ResizeChildren();
            return _editionPage;
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            if (_editionPage != null) {
                _editionPage.Size = Size;
            }
        }

        public override void VisibleToTrue() {
            if ((_editionPage != null && _editionPage.IsDisposed) 
                || (_missionDTO.id > 0 && _missionDTO.deleted == (int) YesOrNo.YES)) {
                CreateANewOne();
            }
        }

        // Class: inner page panel
        public class MissionEditionPage: CustomContentPanel {
            private OperationGuidanceApis _apis;
            private MissionEditionView _parentView;
            private ProductMissionDTO _missionDTO;

            // Contents
            private CustomContentPanel _top;
            private CustomContentPanel _bottom;
            private WorkplacePiece _bottomLeft;
            private WorkplacePiece _bottomRight;
            private int _littleTitleHeight;

            // top
            private CustomTextBoxGroup _missionName;
            private CustomTextBoxGroup _missionPnCode;
            private CustomContentPanel _buttonsOuter;
            private CommonButton _editDetail;
            private MissionDetailPopUpForm _detialPopUpForm;
            private CommonButton _buttonSave;
            private CommonButton _buttonNew;
            private CommonButton _buttonDelete;
            private ImageButton _imageButtonChoose;
            private ImageButton _imageButtonZoomIn;
            private ImageButton _imageButtonZoomOut;
            private ImageButton _imageButtonRotateAntiClockwise;
            private ImageButton _imageButtonRotateClockwise;
            private ImageButton _imageButtonMoveUp;
            private ImageButton _imageButtonMoveDown;
            private ImageButton _imageButtonMoveLeft;
            private ImageButton _imageButtonMoveRight;
            private ImageButton _imageButtonCrop;
            private ImageButton _imageButtonUndo;
            private ImageButton _imageButtonReset;

            // Left side title panel: needs to be alone, don't need any margin
            private CustomContentPanel _sideTitlePanel;
            private List<SideButton> _sideButtons;
            private SideButton _currentSideButton;
            private AddNewSideButton _addNewSideButton;
            private readonly float _sideButtonWidthRatio = 1.4F;

            // Bottom left
            private LeftBottomContentPanel _leftBottomContentPanel;
            private ProductImageFile _currentProductImageFile;
            private int _imageOperationBufferLength;
            private Point _mouseDownLocation;
            private bool _mouseLeftDown;
            private bool _controlDown;
            private bool _needSaveBuffer;
            private BoltEditionPopUpForm _boltPopUpForm;

            // Bottom right
            private CustomContentPanel _boltTitlePanel;
            private Label _boltTitleLabel;
            private RightContentPanel _rightContentPanel;
            private CustomVScrollingContentPanel _autoScrollContentOuterPanel;

            public bool Modified { get; set; } = false;

            public MissionEditionPage(MissionEditionView parent, ProductMissionDTO missionDTO) : base() {
                _apis = SystemUtils.GetApis();
                _parentView = parent;
                Parent = parent;
                _missionDTO = missionDTO;

                InitializeContent();
                InitializeTop();
                InitializeBottomLeft();
                InitializeBottomRight();
            }

            private void InitializeContent() {
                _top = new() {
                    Parent = this,
                    Padding = new(0),
                };
                _bottom = new() {
                    Parent = this,
                    Padding = new(0),
                };
                _bottomLeft = new() {
                    Parent = _bottom,
                    Padding = new(0),
                    FlowDirection = FlowDirection.TopDown,
                    OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
                };
                _bottomRight = new() {
                    Parent = _bottom,
                    Padding = new(0),
                    FlowDirection = FlowDirection.TopDown,
                    OuterPenBorderColor = ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER,
                    ForeColor = ColorConfigs.COLOR_MISSION_EDITION_TEXT,
                    BackColor = ColorConfigs.COLOR_EMPTY_PRODUCT_CONTENT_BACKGROUND,
                };
            }

            private void InitializeTop() {
                // 任务名称输入框
                _missionName = new("任务名称") {
                    Parent = _top,
                    BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                    ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                    BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                    BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                    NameAlignment = HorizontalAlignment.Left,
                };
                CustomTextBox missionNameBox = _missionName.GetTextBox(0);
                missionNameBox.Text = _missionDTO.name;
                missionNameBox.SizeChanged += (sender, eventArgs) => missionNameBox.Box.SelectionStart = 0;
                missionNameBox.TextChanged += (sender, eventArgs) => {
                    if (!_missionName.HasError) {
                        _missionDTO.name = missionNameBox.Text;
                        Modified = true;
                    }
                };
                // // PN码输入框
                // _missionPnCode = new("PN码") {
                //     Parent = _top,
                //     BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                //     ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                //     BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                //     BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                //     NameAlignment = HorizontalAlignment.Left,
                //     NumberOnly = true,
                //     Visible = false,
                // };
                // CustomTextBox missionPnCodeBox = _missionPnCode.GetTextBox(0);
                // missionPnCodeBox.Text = _missionDTO.pn_code;
                // missionPnCodeBox.SizeChanged += (sender, eventArgs) => missionPnCodeBox.Box.SelectionStart = 0;
                // missionPnCodeBox.TextChanged += (sender, eventArgs) => {
                //     if (!_missionPnCode.HasError) {
                //         _missionDTO.pn_code = missionPnCodeBox.Text;
                //         Modified = true;
                //     }
                // };

                _buttonsOuter = new() {
                    Parent = _top,
                    Padding = new(0),
                };
                _editDetail = new() {
                    Parent = _buttonsOuter,
                    Label = "编辑详情",
                    BlockHoverUp = true,
                };
                _editDetail.Click += (s, e) => {
                    List<BarCodeMatchingRuleDTO> barCodeMatchingRuleDTOs = _apis.QueryBarCodeMatchingRuleList(new() { MissionId = _missionDTO.id }).BarCodeMatchingRuleDTOs;
                    _detialPopUpForm = new(_missionDTO, barCodeMatchingRuleDTOs) {
                        Title = "编辑任务详情",
                    };
                    _detialPopUpForm.AddButton("确定").Click += (s, e) => {
                        bool check = true;
                        string warningMsg = "";
                        int warningIndex = 1;
                        string missionName = _detialPopUpForm.MissionName.GetTextBox(0).Box.Text;
                        if (string.IsNullOrEmpty(missionName)) {
                            check = false;
                            _detialPopUpForm.MissionName.GetTextBox(0).IsError = true;
                            warningMsg += $"{warningIndex++}. 站点名称不能为空\r\n";
                        }
                        string maxNGNum = _detialPopUpForm.MaxNGNum.GetTextBox(0).Box.Text;
                        if (string.IsNullOrEmpty(maxNGNum)) {
                            check = false;
                            _detialPopUpForm.MaxNGNum.GetTextBox(0).IsError = true;
                            warningMsg += $"{warningIndex++}. 最大NG数不能为空\r\n";
                        }

                        if (!check) {
                            WidgetUtils.ShowWarningPopUp($"保存失败：\r\n{warningMsg}");
                        } else {
                            _missionName.SetValue(0, missionName);
                            _missionDTO.name = missionName;
                            _missionDTO.max_ng_num = int.Parse(maxNGNum);
                            _detialPopUpForm.Hide();
                        }
                    };
                    _detialPopUpForm.AddButton("关闭").Click += (s, e) => {
                        _detialPopUpForm.Hide();
                    };
                    _detialPopUpForm.PretendToShowToCreateHandlesForChildren();
                    _detialPopUpForm.ResizeSelf();
                    _detialPopUpForm.Show();
                };
                _buttonSave = new() {
                    Parent = _buttonsOuter,
                    Label = "保存",
                    BlockHoverUp = true,
                };
                _buttonSave.Click += (sender, eventArgs) => {
                    _currentProductImageFile.SaveSideInfo();
                    // Store to database
                    AddOrUpdateProductMissionReq req = new(_missionDTO);
                    AddOrUpdateProductMissionRsp rsp = _apis.AddOrUpdateProductMission(req);
                    if (rsp.RsponseCode == HttpResponseCode.OK) {
                        Modified = false;
                        _missionDTO = rsp.ProductMissionDTO;
                        // 数据保存成功后，保存图片到本地（需要循环保存每一个side的图片）
                        foreach (SideButton sideBtn in _sideButtons) {
                            MainUtils.SaveProductImage(sideBtn.ProductImageFileNew.Image, sideBtn.ProductImageFileNew.ImageFileName);
                        }
                        MessageBox.Show(null, "保存成功！", "保存任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        // 保存后跳转至任务列表界面
                        WidgetUtils.GetChildMenu(101).TriggerClick(EventArgs.Empty);
                        Dispose();
                    } else {
                        MessageBox.Show(null, "保存失败！错误信息：" + rsp.RsponseMessage, "保存任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                };
                _buttonNew = new() {
                    Parent = _buttonsOuter,
                    Label = "新增",
                    BlockHoverUp = true,
                };
                _buttonNew.Click += (sender, eventArgs) => {
                    if (Modified) {
                        DialogResult result = MessageBox.Show(null, "当前还有未保存内容，确定新增任务？", "新增任务", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result == DialogResult.Yes) {
                            _parentView.CreateANewOne();
                        }
                    } else {
                        _parentView.CreateANewOne();
                    }
                };
                _buttonDelete = new() {
                    Parent = _buttonsOuter,
                    Label = "删除",
                    BlockHoverUp = true,
                };
                _buttonDelete.Click += (sender, eventArgs) => {
                    if (_missionDTO.id > 0) {
                        DialogResult result = MessageBox.Show(null, "确定删除任务？", "删除任务", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result == DialogResult.Yes) {
                            DeleteProductMissionReq req = new(_missionDTO);
                            DeleteProductMissionRsp rsp = _apis.DeleteProductMission(req);
                            if (rsp.RsponseCode == (int) HttpResponseCode.OK) {
                                MessageBox.Show(null, "删除成功！", "删除任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                _parentView.MissionDTO.deleted = (int) YesOrNo.YES;
                                // 删除后跳转至任务列表界面
                                WidgetUtils.GetChildMenu(101).TriggerClick(EventArgs.Empty);
                                Dispose();
                            } else {
                                MessageBox.Show(null, "删除失败！错误信息：" + rsp.RsponseMessage, "删除任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    } else {
                        WidgetUtils.ShowNoticePopUp("此任务没有保存至数据库，无法执行删除操作");
                    }
                };

                // 设置图片编辑时可撤回的次数（即可以回溯多少次操作）
                _imageOperationBufferLength = 20;
                _imageButtonChoose = GenerateImageButton("选择图片", Properties.Resources.image_choose, (sender, eventArgs) => {
                    _currentProductImageFile.ImageSelect(() => Modified = true);
                    _currentSideButton.ProductImageFile = _currentProductImageFile.Copy();
                });
                _imageButtonZoomIn = GenerateImageButton("放大图片", Properties.Resources.image_zoom_in, (sender, eventArgs) => {
                    _currentProductImageFile.SaveCurrent();
                    _currentProductImageFile.ImageZoomIn();
                });
                _imageButtonZoomOut = GenerateImageButton("缩小图片", Properties.Resources.image_zoom_out, (sender, eventArgs) => {
                    _currentProductImageFile.SaveCurrent();
                    _currentProductImageFile.ImageZoomOut();
                });
                _imageButtonRotateClockwise = GenerateImageButton("顺时旋转", Properties.Resources.image_rotate_clockwise, (sender, eventArgs) => {
                    _currentProductImageFile.SaveCurrent();
                    _currentProductImageFile.ImageRotateClockwise();
                });
                _imageButtonRotateAntiClockwise = GenerateImageButton("逆时旋转", Properties.Resources.image_rotate_anticlockwise, (sender, eventArgs) => {
                    _currentProductImageFile.SaveCurrent();
                    _currentProductImageFile.ImageRotateAntiClockwise();
                });
                _imageButtonMoveUp = GenerateImageButton("向上移动", Properties.Resources.direction_up, (sender, eventArgs) => {
                    _currentProductImageFile.SaveCurrent();
                    _currentProductImageFile.ImageMoveUp();
                });
                _imageButtonMoveDown = GenerateImageButton("向下移动", Properties.Resources.direction_down, (sender, eventArgs) => {
                    _currentProductImageFile.SaveCurrent();
                    _currentProductImageFile.ImageMoveDown();
                });
                _imageButtonMoveLeft = GenerateImageButton("向左移动", Properties.Resources.direction_left, (sender, eventArgs) => {
                    _currentProductImageFile.SaveCurrent();
                    _currentProductImageFile.ImageMoveLeft();
                });
                _imageButtonMoveRight = GenerateImageButton("向右移动", Properties.Resources.direction_right, (sender, eventArgs) => {
                    _currentProductImageFile.SaveCurrent();
                    _currentProductImageFile.ImageMoveRight();
                });
                _imageButtonCrop = GenerateImageButton("裁剪图片", Properties.Resources.image_crop, (sender, eventArgs) => {
                    _currentProductImageFile.SaveCurrent();
                    _currentProductImageFile.ImageCrop();
                });
                _imageButtonUndo = GenerateImageButton("撤回操作", Properties.Resources.image_undo, (sender, eventArgs) => _currentProductImageFile.ImageUndo());
                _imageButtonReset = GenerateImageButton("重置图片", Properties.Resources.image_reset, (sender, eventArgs) => {
                    _currentProductImageFile.ClearBuffer();
                    _currentSideButton.ImageReset();
                });
            }

            private ImageButton GenerateImageButton(string label, Image icon, EventHandler eventHandler) {
                ImageButton button =new() {
                    Parent = _top,
                    Label = label,
                    BlockHoverUp = true,
                    Icon = icon,
                };
                button.Click += eventHandler;
                return button;
            }

            private void InitializeBottomLeft() {
                _sideTitlePanel = new() {
                    Parent = _bottomLeft,
                    Margin = new(1, 1, 0, 0),
                    Padding = new(0),
                    BackColor = ColorConfigs.COLOR_MISSION_EDITION_IMAGE_TITLE_PANEL_BACK,
                };
                // _leftBottomContentPanel = new(Properties.Resources.image_choose, "点击添加产品图片", "（请确保所有螺栓点位在最小范围内，以免分辨率很小时显示不全）") {
                //     Parent = _bottomLeft,
                //     Margin = new(1, 0, 0, 0),
                //     Padding = new(0),
                //     BackColor = ColorConfigs.COLOR_EMPTY_PRODUCT_CONTENT_BACKGROUND,
                // };
                _leftBottomContentPanel = new(Properties.Resources.image_choose, "点击添加产品图片", "工作台界面以虚线框内的显示部分为准。可使用裁剪功能裁剪掉虚线外的部分。") {
                    Parent = _bottomLeft,
                    Margin = new(1, 0, 0, 0),
                    Padding = new(0),
                    BackColor = ColorConfigs.COLOR_EMPTY_PRODUCT_CONTENT_BACKGROUND,
                };
                BindEventsForLeftBottomContentPanel();
                GenerateSideButtons();
            }

            private void BindEventsForLeftBottomContentPanel() {
                _leftBottomContentPanel.MouseLeave += (sender, eventArgs) => {
                    Cursor = Cursors.Arrow;
                };
                _mouseLeftDown = false;
                _leftBottomContentPanel.SingleClickDelegate += (eventArgs) => {
                    if (_leftBottomContentPanel.CanTriggerClick()) {
                        _currentProductImageFile.ImageSelect(() => Modified = true);
                        _currentSideButton.ProductImageFile = _currentProductImageFile.Copy();
                    }
                };
                _leftBottomContentPanel.DoubleClickDelegate += (eventArgs) => {
                    if (!_leftBottomContentPanel.CanTriggerClick()) {
                        // 检查sideDTO是否为空，如果为空则抛出异常，因为在这里不能为空
                        ProductSideDTO sideDTO = CommonUtils.CannotBeNull(_currentSideButton.SideDTO);
                        ProductBoltDTO boltDTO = new() {
                            side_id = sideDTO.id,
                        };
                        // Calculate the location of new bolt
                        Rectangle maxRect = _leftBottomContentPanel.MaxRect;
                        boltDTO.location_x_percent = (float) ((decimal) (eventArgs.Location.X - maxRect.X - _currentSideButton.BoltButtonRadius) / _leftBottomContentPanel.MaxRectWidth * 100);
                        boltDTO.location_y_percent = (float) ((decimal) (eventArgs.Location.Y - maxRect.Y - _currentSideButton.BoltButtonRadius) / _leftBottomContentPanel.MaxRectHeight * 100);
                        // Set serial number, if deleted serial number(s) exit(s), dequeue a serial number from queue and use it
                        if (_currentSideButton.DeletedSerialNum.Count > 0) {
                            boltDTO.serial_num = _currentSideButton.DeletedSerialNum.Dequeue();
                        } else {
                            for (int serialNum = 1; serialNum <= _currentSideButton.BoltSerialNums.Count + 1; serialNum++) {
                                if (!_currentSideButton.BoltSerialNums.Contains(serialNum)) {
                                    boltDTO.serial_num = serialNum;
                                    break;
                                }
                            }
                        }
                        
                        boltDTO.name = $"BOLT" + boltDTO.serial_num;
                        OpenNewBoltPopUpForm(boltDTO, () => {
                            // Add new buttons
                            BoltButton boltButton = AddNewBoltButton(_currentSideButton, boltDTO);
                            BoltEditionButton boltEditionButton = AddNewBoltEditionButton(_currentSideButton, boltDTO);
                            boltButton.Visible = true;
                            boltEditionButton.Visible = true;
                            // Add buttons into side button
                            _currentSideButton.BoltButtons.Add(boltDTO.serial_num, boltButton);
                            _currentSideButton.BoltEditionButtons.Add(boltDTO.serial_num, boltEditionButton);

                            // Reorder the edition buttons
                            boltEditionButton.Parent.Controls.SetChildIndex(boltEditionButton, _currentSideButton.BoltSerialNums.IndexOf(boltDTO.serial_num));
                            // Do this to force fire SizeChanged event
                            ResizeBottomLeft();
                            ForceResizeRight();

                            // Save new boltDto to sideDto
                            if (sideDTO.Bolts == null) {
                                sideDTO.Bolts = new();
                            }
                            sideDTO.Bolts.Add(boltDTO);
                            // Save serial num
                            _currentSideButton.BoltSerialNums.Add(boltDTO.serial_num);
                            _currentSideButton.BoltSerialNums.Sort();
                        });
                    }
                };
                _controlDown = false;
                _needSaveBuffer = false;
                EventFuncs.AddAutoActivatingControl(_leftBottomContentPanel);
                _leftBottomContentPanel.KeyDown += (sender, eventArgs) => {
                    if (!_controlDown && eventArgs.Control) {
                        _controlDown = true;
                        _currentProductImageFile.SaveCurrent();
                        Cursor = Cursors.Hand;
                    }
                };
                _leftBottomContentPanel.KeyUp += (sender, eventArgs) => {
                    if (_controlDown) {
                        _controlDown = false;
                        if (!_needSaveBuffer) {
                            _currentProductImageFile.ImageUndo();
                        }
                        Modified = _needSaveBuffer;
                        _needSaveBuffer = false;
                        Cursor = Cursors.Arrow;
                    }
                };
                _leftBottomContentPanel.MouseWheel += (sender, eventArgs) => {
                    if (_controlDown) {
                        if (eventArgs.Delta > 0) {
                            _currentProductImageFile.ImageZoomIn();
                        } else {
                            _currentProductImageFile.ImageZoomOut();
                        }
                        _needSaveBuffer = true;
                    }
                };
                _leftBottomContentPanel.MouseDown += (sender, eventArgs) => {
                    if (_controlDown && _currentProductImageFile.ImageRange != null) {
                        if (eventArgs.Button == MouseButtons.Left) {
                            _mouseDownLocation = eventArgs.Location;
                            Cursor = Cursors.NoMove2D;
                            _mouseLeftDown = true;
                            _currentProductImageFile.SaveCurrent();
                        }
                    }
                };
                _leftBottomContentPanel.MouseMove += (sender, eventArgs) => {
                    if (_currentProductImageFile.ImageRange != null && _controlDown) {
                        if (_mouseLeftDown && eventArgs.Button == MouseButtons.Left) {
                            Point locationOffsetExtra = new(eventArgs.Location.X - _mouseDownLocation.X, eventArgs.Location.Y - _mouseDownLocation.Y);
                            _currentProductImageFile.LocationOffsetMoving = locationOffsetExtra;
                            _currentProductImageFile.RefreshImage();
                        } else {
                            Cursor = Cursors.Hand;
                        }
                    } else {
                        if (_currentProductImageFile.ImageRange == null) {
                            Cursor = Cursors.Hand;
                        } else {
                            Cursor = Cursors.Arrow;
                        }
                    }
                };
                _leftBottomContentPanel.MouseUp += (sender, eventArgs) => {
                    if (_mouseLeftDown && eventArgs.Button == MouseButtons.Left) {
                        Point locationOffset = _currentProductImageFile.LocationOffset;
                        locationOffset.Offset(_currentProductImageFile.LocationOffsetMoving);
                        // Use Offset method to replace two lines code as bellow
                        // locationOffset.X += _currentProductImageFile.LocationOffsetMoving.X;
                        // locationOffset.Y += _currentProductImageFile.LocationOffsetMoving.Y;
                        _currentProductImageFile.LocationOffset = locationOffset;
                        _currentProductImageFile.LocationOffsetMoving = new(0, 0);
                        Cursor = Cursors.Arrow;
                        _mouseLeftDown = false;
                        _needSaveBuffer = true;
                    }
                };
            }

            private void GenerateSideButtons() {
                _sideButtons = new();

                if (_missionDTO.ProductSides != null) {
                    if (_missionDTO.ProductSides.Count == 0) {
                        ProductSideDTO sideDTO = new() {
                            name = "产品图片1",
                            Bolts = new(),
                        };
                        _missionDTO.ProductSides.Add(sideDTO);
                    }
                    foreach (ProductSideDTO sideDTO in _missionDTO.ProductSides) {
                        _sideButtons.Add(NewSideButton(sideDTO));
                    }
                    _currentSideButton = _sideButtons[0];
                    _currentProductImageFile = _currentSideButton.ProductImageFileNew;
                    _currentSideButton.SetToggle(true);
                }

                _addNewSideButton = new("新增") {
                    Parent = _sideTitlePanel,
                    BackColor = ColorConfigs.COLOR_MISSION_EDITION_IMAGE_SIDE_BUTTON_NEW,
                    ForeColor = ColorConfigs.COLOR_MISSION_EDITION_TEXT,
                    ToggleBarColor = ColorConfigs.COLOR_MISSION_EDITION_IMAGE_SIDE_BUTTON_TOGGLED,
                    BlockHoverUp = true,
                };
                _addNewSideButton.Click += (sender, eventArgs) => {
                    ProductSideDTO sideDTO = new() {
                        name = "产品图片" + (_sideButtons.Count + 1),
                        Bolts = new(),
                    };
                    // Add new sideDto to sideDto
                    _missionDTO.ProductSides.Add(sideDTO);
                    // Create a new side button
                    SideButton sideButton = NewSideButton(sideDTO);
                    _sideButtons.Add(sideButton);
                    // Send "new side button" to back
                    _addNewSideButton.SendToBack();

                    ResizeSideButtons();
                    // Toggle new button right after creating
                    SideButonClick(sideButton);
                    // Change state
                    Modified = true;
                };
            }

            private SideButton NewSideButton(ProductSideDTO sideDTO) {
                ProductImageFile productImageFile = new(_leftBottomContentPanel, sideDTO, _imageOperationBufferLength);
                ProductImageFile productImageFileNew = new(_leftBottomContentPanel, sideDTO, _imageOperationBufferLength);

                // Initialzie side button
                SideButton sideButton = new(sideDTO, _leftBottomContentPanel, productImageFile, productImageFileNew) {
                    Parent = _sideTitlePanel,
                    BackColor = Color.Transparent,
                    ForeColor = ColorConfigs.COLOR_MISSION_EDITION_TEXT,
                    ToggleBarColor = ColorConfigs.COLOR_MISSION_EDITION_IMAGE_SIDE_BUTTON_TOGGLED,
                    BoltButtonRadius = _leftBottomContentPanel.MaxRectHeight / 24,
                };
                sideButton.Deleted += () => {
                    sideDTO.deleted = (int) YesOrNo.YES;
                    if (sideDTO.id == -1) {
                        // 将没有存入数据库的数据直接从缓存中去掉，已存入数据库的数据需要修改deleted字段使其变成已删除
                        CommonUtils.CannotBeNull(_missionDTO.ProductSides).Remove(sideDTO);
                    }
                    int index = _sideButtons.IndexOf(sideButton);
                    if (_sideButtons.Count == 1) {
                        _sideButtons.Remove(sideButton);
                        // close first then create a new one
                        _addNewSideButton.PerformClick();
                    } else if (index < _sideButtons.Count - 1) {
                        SideButonClick(_sideButtons[index + 1]);
                        // click first then close
                        _sideButtons.Remove(sideButton);
                    } else {
                        SideButonClick(_sideButtons[index - 1]);
                        // click first then close
                        _sideButtons.Remove(sideButton);
                    }
                    Modified = true;
                };
                sideButton.SingleClickDelegate += (eventArgs) => SideButonClick(sideButton);
                sideButton.DoubleClickDelegate += (eventArgs) => {
                    TextBox box = new() {
                        Parent = sideButton,
                        BorderStyle = BorderStyle.None,
                        Size = (sideButton.Size * .75F).ToSize(),
                        Text = sideButton.Label,
                        ImeMode = ImeMode.On,
                    };
                    box.Location = new((sideButton.Width - box.Width) / 2, (int) (((sideButton.Height - box.Height) / 2) * .9));
                    box.KeyUp += (sender, eventArgs) => {
                        if (eventArgs.KeyCode == Keys.Enter) {
                            RenameAndResizeCurrent();
                            box.Dispose();
                        } else if (eventArgs.KeyCode == Keys.Escape) {
                            box.Dispose();
                        }
                    };
                    box.LostFocus += (sender, eventArgs) => {
                        RenameAndResizeCurrent();
                        box.Dispose();
                    };
                    box.Focus();
                    EventFuncs.CurrentActiveControl = box;
                    void RenameAndResizeCurrent() {
                        if (box.Text != null && box.Text != string.Empty) {
                            sideButton.Label = box.Text;
                            sideDTO.name = box.Text;
                            using (Graphics g = CreateGraphics()) {
                                int btnLabelWidth = (int) g.MeasureString(sideButton.Label, sideButton.Font).Width;
                                sideButton.Width = (int) (btnLabelWidth + sideButton.Height * _sideButtonWidthRatio);
                            }
                        }
                        Modified = true;
                    }
                };

                // Initialize bolts buttons
                if (sideDTO.Bolts != null && sideDTO.Bolts.Count > 0) {
                    foreach (ProductBoltDTO boltDTO in sideDTO.Bolts) {
                        sideButton.BoltButtons.Add(boltDTO.serial_num, AddNewBoltButton(sideButton, boltDTO));
                        sideButton.BoltSerialNums.Add(boltDTO.serial_num);
                    }
                }
                return sideButton;
            }

            private BoltButton AddNewBoltButton(SideButton sideButton, ProductBoltDTO boltDTO) {
                BoltButton boltButton = new(boltDTO) {
                    Parent = _leftBottomContentPanel,
                    Visible = false,
                };
                boltButton.MouseDown += (sender, eventArgs) => {
                    if (eventArgs.Button == MouseButtons.Left) {
                        _mouseDownLocation = eventArgs.Location;
                        Cursor.Hide();
                        boltButton.MouseLeftDown = true;
                    }
                };
                boltButton.MouseMove += (sender, eventArgs) => {
                    if (boltButton.MouseLeftDown && eventArgs.Button == MouseButtons.Left) {
                        // Set offset
                        Point locationOffset = new(eventArgs.Location.X - _mouseDownLocation.X, eventArgs.Location.Y - _mouseDownLocation.Y);
                        Point location = boltButton.Location;
                        location.Offset(locationOffset);
                        boltButton.Location = location;

                        // Recalculate bolt location
                        Rectangle maxRect = _leftBottomContentPanel.MaxRect;
                        boltDTO.location_x_percent = (float) ((decimal) (location.X - maxRect.X) / _leftBottomContentPanel.MaxRectWidth * 100);
                        boltDTO.location_y_percent = (float) ((decimal) (location.Y - maxRect.Y) / _leftBottomContentPanel.MaxRectHeight * 100);

                        boltButton.Moved = true;
                    }
                };
                boltButton.MouseUp += (sender, eventArgs) => {
                    if (boltButton.MouseLeftDown && eventArgs.Button == MouseButtons.Left) {
                        boltButton.MouseLeftDown = false;
                        if (boltButton.Moved) {
                            boltButton.Moved = false;
                        } else {
                            sideButton.CurrentSerialNum = boltDTO.serial_num;
                            OpenBoltPopUpForm(boltDTO);
                        }
                    }
                    Cursor.Show();
                };
                return boltButton;
            }
            
            private void OpenNewBoltPopUpForm(ProductBoltDTO boltDTO, Action addNewBoltBtns) {
                _boltPopUpForm = new(boltDTO) {
                    Title = boltDTO.serial_num + " - " + boltDTO.name,
                    BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
                };
                // 添加按钮
                CommonButton confirmButton = _boltPopUpForm.AddButton("确定信息");
                confirmButton.Click += (s, e) => {
                    if (saveBoltInfo(boltDTO)) {
                        addNewBoltBtns();
                    }
                };
                CommonButton cancelButton = _boltPopUpForm.AddButton("关闭");
                cancelButton.Click += (s, e) => {
                    _boltPopUpForm.Dispose();
                };
                // Show form but make it transparent to create handles for its children
                _boltPopUpForm.PretendToShowToCreateHandlesForChildren();
                // Resize all widgets
                ResizePopUpForm();
                // Real yhow_editionPage
                _boltPopUpForm.Show();
            }

            private bool saveBoltInfo(ProductBoltDTO boltDTO) {
                bool check = true;
                string warningMsg = "";
                int warningIndex = 1;
                if (_boltPopUpForm.Workstation.Value == null) {
                    check = false;
                    _boltPopUpForm.Workstation.SetError(true); 
                    warningMsg += $"{warningIndex++}. 站点不能为空\r\n";
                }
                if (MainUtils.IsArmLocatingEnabled() && !_boltPopUpForm.PositionToggle.Checked) {
                    check = false;
                    warningMsg += $"{warningIndex++}. 已开启【力臂定位】，必须配置点位坐标\r\n";
                }
                if (_boltPopUpForm.PositionToggle.Checked) {
                    string x = _boltPopUpForm.PositionBox.GetTextBox(0).Box.Text;
                    string y = _boltPopUpForm.PositionBox.GetTextBox(1).Box.Text;
                    string z = _boltPopUpForm.PositionBox.GetTextBox(2).Box.Text;
                    if (string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y) || string.IsNullOrEmpty(z)) {
                        check = false;
                        _boltPopUpForm.PositionBox.GetTextBox(0).IsError = true;
                        _boltPopUpForm.PositionBox.GetTextBox(1).IsError = true;
                        _boltPopUpForm.PositionBox.GetTextBox(2).IsError = true;
                        warningMsg += $"{warningIndex++}. 点位坐标字段开启后，不能为空\r\n";
                    }
                } else {
                    boltDTO.position = null;
                }
                if (_boltPopUpForm.ParameterSetToggle.Checked) {
                    string pset = _boltPopUpForm.ParameterSetBox.GetTextBox(0).Box.Text;
                    if (string.IsNullOrEmpty(pset) || int.Parse(pset) <= 0) {
                        check = false;
                        _boltPopUpForm.ParameterSetBox.GetTextBox(0).IsError = true;
                        warningMsg += $"{warningIndex++}. 程序号字段开启后，不能为空且必须大于0\r\n";
                    }
                } else {
                    boltDTO.parameters_set = null;
                }
                if (_boltPopUpForm.SpecificationToggle.Checked) {
                    string specification = _boltPopUpForm.SpecificationBox.GetTextBox(0).Box.Text;
                    if (string.IsNullOrEmpty(specification) || int.Parse(specification) <= 0) {
                        check = false;
                        _boltPopUpForm.SpecificationBox.GetTextBox(0).IsError = true;
                        warningMsg += $"{warningIndex++}. 螺栓规格字段开启后，不能为空且必须大于0\r\n";
                    }
                } else {
                    boltDTO.specification= null;
                }
                if (_boltPopUpForm.BitSpecificationToggle.Checked) {
                    string bitSpecification = _boltPopUpForm.BitSpecificationBox.GetTextBox(0).Box.Text;
                    if (string.IsNullOrEmpty(bitSpecification) || int.Parse(bitSpecification) <= 0) {
                        check = false;
                        _boltPopUpForm.BitSpecificationBox.GetTextBox(0).IsError = true;
                        warningMsg += $"{warningIndex++}. 套筒位数字段开启后，不能为空且必须大于0\r\n";
                    }
                } else {
                    boltDTO.bit_specification = null;
                }
                if (_boltPopUpForm.TorqueToggle.Checked) {
                    string torqueMin = _boltPopUpForm.TorqueBox.GetTextBox(0).Box.Text;
                    string torqueMax = _boltPopUpForm.TorqueBox.GetTextBox(1).Box.Text;
                    if (string.IsNullOrEmpty(torqueMin) || string.IsNullOrEmpty(torqueMax)) {
                        check = false;
                        _boltPopUpForm.TorqueBox.GetTextBox(0).IsError = true;
                        _boltPopUpForm.TorqueBox.GetTextBox(1).IsError = true;
                        warningMsg += $"{warningIndex++}. 扭矩上下限字段开启后，不能为空\r\n";
                    }
                } else {
                    boltDTO.torque_min = null;
                    boltDTO.torque_max = null;
                }
                if (_boltPopUpForm.AngleToggle.Checked) {
                    string AngleMin = _boltPopUpForm.AngleBox.GetTextBox(0).Box.Text;
                    string AngleMax = _boltPopUpForm.AngleBox.GetTextBox(1).Box.Text;
                    if (string.IsNullOrEmpty(AngleMin) || string.IsNullOrEmpty(AngleMax)) {
                        check = false;
                        _boltPopUpForm.AngleBox.GetTextBox(0).IsError = true;
                        _boltPopUpForm.AngleBox.GetTextBox(1).IsError = true;
                        warningMsg += $"{warningIndex++}. 扭矩上下限不能为空\r\n";
                    }
                } else {
                    boltDTO.angle_min = null;
                    boltDTO.angle_max = null;
                }

                if (!check) {
                    WidgetUtils.ShowWarningPopUp($"信息暂存失败：\r\n{warningMsg}");
                } else {
                    // 根据校验结果判断是否可以保存
                    Modified = true;
                    _boltPopUpForm.SaveTo(boltDTO);
                    WidgetUtils.ShowNoticePopUp("信息暂存成功！");
                    _boltPopUpForm.Dispose();
                }

                return check;
            }

            private void OpenBoltPopUpForm(ProductBoltDTO boltDTO) {
                _boltPopUpForm = new(boltDTO) {
                    Title = boltDTO.serial_num + " - " + boltDTO.name,
                    BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
                };
                // 添加按钮
                CommonButton confirmButton = _boltPopUpForm.AddButton("确定信息");
                confirmButton.Click += (s, e) => {
                    saveBoltInfo(boltDTO);
                };
                CommonButton deleteButton = _boltPopUpForm.AddButton("删除点位");
                deleteButton.Click += (s, e) => {
                    if (WidgetUtils.ShowConfirmPopUp("确定要删除当前点位？")) {
                        Modified = true;
                        _currentSideButton.DeleteBolt();
                        _boltPopUpForm.Dispose();
                        ForceResizeRight();
                    }
                };
                CommonButton cancelButton = _boltPopUpForm.AddButton("关闭");
                cancelButton.Click += (s, e) => {
                    _boltPopUpForm.Dispose();
                };
                // Show form but make it transparent to create handles for its children
                _boltPopUpForm.PretendToShowToCreateHandlesForChildren();
                // Resize all widgets
                ResizePopUpForm();
                // Real yhow_editionPage
                _boltPopUpForm.Show();
            }

            private void SideButonClick(SideButton sideButton) {
                if (sideButton != _currentSideButton) {
                    _currentSideButton.ProductImageFileNew.SaveSideInfo();
                    _currentSideButton.SetToggle(false);
                    sideButton.SetToggle(true);
                    _currentSideButton = sideButton;
                    _currentProductImageFile = _currentSideButton.ProductImageFileNew;

                    ForceResizeRight();
                }
            }

            private void InitializeBottomRight() {
                _boltTitlePanel = new() {
                    Parent = _bottomRight,
                    Padding = new(0),
                    Margin = new(1, 1, 0, 0),
                    BackColor = ColorConfigs.COLOR_MISSION_EDITION_IMAGE_TITLE_PANEL_BACK,
                };
                _rightContentPanel = new(_sideButtons) {
                    Padding = new(0),
                };
                _autoScrollContentOuterPanel = new(null, _rightContentPanel) {
                    Parent = _bottomRight,
                    Margin = new(1, 0, 0, 0),
                    NeedsPadding = false,
                };

                _boltTitleLabel = new() {
                    Parent = _boltTitlePanel,
                    Margin = new(0),
                    Padding = new(0),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Text = "工艺流程",
                };

                // Create all bolt edition buttons and set them invisible
                foreach (SideButton sideButton in _sideButtons) {
                    foreach (BoltButton button in sideButton.BoltButtons.Values) {
                        sideButton.BoltEditionButtons.Add(button.BoltDTO.serial_num, AddNewBoltEditionButton(sideButton, button.BoltDTO));
                    }
                }

                // Show bolt edition buttons of current side
                foreach (BoltEditionButton boltEdition in _currentSideButton.BoltEditionButtons.Values) {
                    boltEdition.Visible = true;
                }
            }

            private BoltEditionButton AddNewBoltEditionButton(SideButton sideButton, ProductBoltDTO boltDTO) {
                BoltEditionButton boltEditionButton = new(boltDTO) {
                    Parent = _rightContentPanel,
                    ForeColor = _bottomRight.ForeColor,
                    BackColor = ColorConfigs.COLOR_MISSION_EDITION_BUTTON_BACK,
                    Visible = false,
                };
                boltEditionButton.Deleted += () => {
                    sideButton.CurrentSerialNum = boltEditionButton.BoltDTO.serial_num;
                    _currentSideButton.DeleteBolt();
                };
                boltEditionButton.SingleClickDelegate += (eventArgs) => {
                    sideButton.CurrentSerialNum = boltEditionButton.BoltDTO.serial_num;
                    OpenBoltPopUpForm(boltDTO);
                };
                boltEditionButton.DoubleClickDelegate += (eventArgs) => {
                    TextBox box = new() {
                        Parent = boltEditionButton,
                        BorderStyle = BorderStyle.None,
                        Size = (boltEditionButton.Size * .9F).ToSize(),
                        Text = boltDTO.name,
                        ImeMode = ImeMode.On,
                    };
                    box.Location = new((boltEditionButton.Width - box.Width) / 2, (int) (((boltEditionButton.Height - box.Height) / 2) * .9));
                    box.KeyUp += (sender, eventArgs) => {
                        if (eventArgs.KeyCode == Keys.Enter) {
                            RenameAndResize();
                            box.Dispose();
                        } else if (eventArgs.KeyCode == Keys.Escape) {
                            box.Dispose();
                        }
                    };
                    box.LostFocus += (sender, eventArgs) => {
                        RenameAndResize();
                        box.Dispose();
                    };
                    box.Focus();
                    EventFuncs.CurrentActiveControl = box;

                    void RenameAndResize() {
                        if (box.Text != null && box.Text != string.Empty) {
                            boltEditionButton.Label = boltDTO.serial_num + ". " + box.Text;
                            boltDTO.name = box.Text;
                            // Do this to force fire SizeChange event to relocate the label
                            boltEditionButton.Width += 1;
                            boltEditionButton.Width -= 1;
                        }
                    }
                };
                return boltEditionButton;
            }

            protected override void OnHandleCreated(EventArgs e) {
                base.OnHandleCreated(e);
                BeginInvoke(new(ResizeChildrenAfterAllHandlesCreated));
            }

            protected void ResizeChildrenAfterAllHandlesCreated() {
                bool checkAllHandlesCreated = false;
                while (!checkAllHandlesCreated) {
                    checkAllHandlesCreated = AllControlHandlesCreated(this);
                }
                Size = Parent.Size;
                ResizeChildren();
            }

            private bool AllControlHandlesCreated(Control parent) {
                bool result = true;
                foreach (Control control in parent.Controls) {
                    if (!control.Visible) {
                        continue;
                    }
                    if (control.IsHandleCreated) {
                        if (control.Controls.Count > 0) {
                            result = AllControlHandlesCreated(control);
                        } else {
                            result = true;
                        }
                    } else {
                        result = false;
                        break;
                    }
                }
                return result;
            }

            protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
                if (Size == new Size(200, 100)) {
                    // TODO: have to figure out why this happen
                    return;
                }
                if (Parent != null && Parent.IsHandleCreated) {
                    CustomVScrollingContentPanel? outerVScrollPanel = ((MissionEditionView) Parent).OuterVScrollPanel;
                    if (outerVScrollPanel != null) {
                        ResizeContent(outerVScrollPanel.OuterPanel.Padding);
                        ResizeSideButtons();
                        ResizeTop();
                        ResizeBottomLeft();
                        ResizeBottomRight();
                    }
                }
            }

            private void ResizeContent(Padding outerPadding) {
                int topHeight = (int) (Height * .15);
                int bottomHeight = Height - topHeight;
                int bottomLeftWidth = (int) (Width * .8);

                _top.Size = new(_bottom.Width, topHeight);

                _bottom.Size = new(Width, bottomHeight);
                _bottomLeft.Size = new(bottomLeftWidth, bottomHeight);

                _bottomRight.Size = new(Width - bottomLeftWidth - (outerPadding.Left - 1) / 2, bottomHeight);
                _bottomRight.Margin = new((outerPadding.Left) / 2, 0, 0, 0);
            }

            private void ResizeSideButtons() {
                int newHeight = _sideTitlePanel.Height;
                foreach (SideButton sideButton in _sideButtons) {
                    // Height must be set first then ResizeTextLabel can be invoked, then the Font can be set
                    sideButton.Height = newHeight;
                    using (Graphics g = CreateGraphics()) {
                        int btnLabelWidth = (int) g.MeasureString(sideButton.Label, sideButton.Font).Width;
                        sideButton.Width = (int) (btnLabelWidth + newHeight * _sideButtonWidthRatio);
                    }
                }
                _addNewSideButton.Size = new((int) (_sideTitlePanel.Width * .08), newHeight);
            }

            private void ResizeTop() {
                // Recalculate some variables
                int textBoxWidth = (int) (_top.Width / 2.75);
                int textBoxHeight = WidgetUtils.TextOrComboBoxHeight();
                int boxGap = (int) (textBoxHeight * .5);
                int buttonsHeight = WidgetUtils.CommonButtonHeight();
                int buttonGap = (int) (buttonsHeight * .5);

                // Resize mission name box
                _missionName.Size = new(textBoxWidth, textBoxHeight);
                // _missionPnCode.Size = new(textBoxWidth, textBoxHeight);
                // _missionPnCode.Margin = new(boxGap, 0, 0, 0);

                // Resize common buttons
                _buttonsOuter.Size = new(_top.Width - textBoxWidth - boxGap, buttonsHeight);
                foreach (Control c in _buttonsOuter.Controls) {
                    if (c is CommonButton btn) {
                        btn.Height = buttonsHeight;
                        // 先设置高度获得自动调整的字体大小
                        int width = TextRenderer.MeasureText(btn.Label, btn.Font).Width;
                        btn.Width = (int) (width * 1.8);
                        btn.Margin = new(buttonGap, 0, 0, 0);
                    }
                }

                // Resize image buttons
                int imageButtonSide = _top.Height - buttonsHeight;
                int imageMargin = (int) (imageButtonSide * .1);
                Size imageButtonSize = new(imageButtonSide - imageMargin * 2, imageButtonSide - imageMargin * 2);
                HandleImageButton(_imageButtonChoose);
                HandleImageButton(_imageButtonZoomOut);
                HandleImageButton(_imageButtonZoomIn);
                HandleImageButton(_imageButtonRotateClockwise);
                HandleImageButton(_imageButtonRotateAntiClockwise);
                HandleImageButton(_imageButtonMoveUp);
                HandleImageButton(_imageButtonMoveDown);
                HandleImageButton(_imageButtonMoveLeft);
                HandleImageButton(_imageButtonMoveRight);
                HandleImageButton(_imageButtonCrop);
                HandleImageButton(_imageButtonUndo);
                HandleImageButton(_imageButtonReset);

                // Inner method for reuse
                void HandleImageButton(ImageButton button) {
                    button.Size = imageButtonSize;
                    button.Margin = new(0, imageMargin, imageMargin, imageMargin);
                }
            }

            private void ResizeBottomLeft() {
                _littleTitleHeight = (int) (WidgetUtils.TextOrComboBoxHeight() * 1.1);
                _sideTitlePanel.Size = new(_bottomLeft.Width - 2, _littleTitleHeight);
                _leftBottomContentPanel.Size = new(_bottomLeft.Width - 2, _bottomLeft.Height - _littleTitleHeight - 2);

                // Resize bolt buttons
                int boltButtonRadius = _leftBottomContentPanel.MaxRectHeight / 24;
                foreach (SideButton sideButton in _sideButtons) {
                    sideButton.BoltButtonRadius = boltButtonRadius;
                    sideButton.ReCalculateProductImageRatio();
                    foreach (BoltButton boltButton in sideButton.BoltButtons.Values) {
                        boltButton.Size = new(boltButtonRadius * 2, boltButtonRadius * 2);
                        // Recalculate bolt button location
                        int newX = _leftBottomContentPanel.MaxRectLocation.X + (int) (_leftBottomContentPanel.MaxRectWidth * boltButton.BoltDTO.location_x_percent / 100);
                        int newY = _leftBottomContentPanel.MaxRectLocation.Y + (int) (_leftBottomContentPanel.MaxRectHeight * boltButton.BoltDTO.location_y_percent / 100);
                        boltButton.Location = new(newX, newY);
                    }
                }
                // Refresh current product image
                _currentProductImageFile.RefreshImage();
                // Resize popup form
                ResizePopUpForm();
            }

            private void ResizePopUpForm() {
                if (_boltPopUpForm != null && !_boltPopUpForm.IsDisposed) {
                    _boltPopUpForm.ResizeSelf();
                    // _boltPopUpForm.CalculateDetailProperties();
                    //
                    // Control mainForm = WidgetUtils.MainPanel.Parent;
                    // TableLayoutPanel tablePanel = _boltPopUpForm.TablePanel;
                    // Padding contentPadding = _boltPopUpForm.ContentPanel.Padding;
                    // int boxHeight = WidgetUtils.TextOrComboBoxHeight();
                    // int boxMargin = boxHeight / 5;
                    // int tableHeight = (int) Math.Ceiling((decimal) tablePanel.Controls.Count / tablePanel.ColumnCount) * (boxHeight + boxMargin * 2);
                    // Size contentSize = new((int) (mainForm.Width * .75), tableHeight + contentPadding.Size.Height);
                    // int tableWidth = contentSize.Width - contentPadding.Size.Width;
                    // _boltPopUpForm.BoxHeight = boxHeight;
                    // _boltPopUpForm.ButtonHeight = WidgetUtils.CommonButtonHeight();
                    // _boltPopUpForm.BoxMargin = boxMargin;
                    // _boltPopUpForm.TablePanel.Size = new(tableWidth, tableHeight);
                    //
                    // _boltPopUpForm.SetContentSizeAndSelfSize(contentSize);
                    // if (_boltPopUpForm.Visible) {
                    //     _boltPopUpForm.Invalidate();
                    // }
                }
            }

            private void ResizeBottomRight() {
                int controlWidth = _bottomRight.Width - 2;
                _boltTitlePanel.Size = new(controlWidth, _littleTitleHeight);
                _boltTitleLabel.Size = _boltTitlePanel.Size;
                _boltTitleLabel.Font = new Font(WidgetsConfigs.SystemFontFamily, _boltTitleLabel.Height * .55F, FontStyle.Bold, GraphicsUnit.Pixel);

                int contentHeight = _bottomRight.Height - _boltTitlePanel.Height - 2;
                int boltButtonHeight = (int) (contentHeight * .055);
                int boltButtonMargin = boltButtonHeight / 7;
                _rightContentPanel.BoltButtonHeight = boltButtonHeight;
                _rightContentPanel.BoltButtonMargin = boltButtonMargin;
                _rightContentPanel.BoltButtonWidth = controlWidth - boltButtonMargin * 2;

                _rightContentPanel.NewHeight = (boltButtonHeight + boltButtonMargin) * _currentSideButton.BoltEditionButtons.Count;
                _autoScrollContentOuterPanel.Size = new(controlWidth, contentHeight);
            }

            private void ForceResizeRight() {
                _autoScrollContentOuterPanel.Width -= 1;
                ResizeBottomRight();
            }
        }

        public class MissionDetailPopUpForm: CustomPopUpForm {
            private int _tableColumns = 2;
            private ProductMissionDTO _missionDTO;
            private TableLayoutPanel _tablePanel;
            private CustomTextBoxGroup _missionName;
            private CustomTextBoxGroup _maxNGNum;
            private CustomTextBoxGroup _productsBarCodeNum;
            private CustomTextBoxGroup _partsBarCodeNum;

            public TableLayoutPanel TablePanel { get => _tablePanel; set => _tablePanel = value; }
            public ProductMissionDTO MissionDTO { get => _missionDTO; set => _missionDTO = value; }
            public CustomTextBoxGroup MissionName { get => _missionName; set => _missionName = value; }
            public CustomTextBoxGroup MaxNGNum { get => _maxNGNum; set => _maxNGNum = value; }
            public CustomTextBoxGroup ProductsBarCodeNum { get => _productsBarCodeNum; set => _productsBarCodeNum = value; }
            public CustomTextBoxGroup PartsBarCodeNum { get => _partsBarCodeNum; set => _partsBarCodeNum = value; }

            public MissionDetailPopUpForm(ProductMissionDTO missionDTO, List<BarCodeMatchingRuleDTO> barCodeMatchingRuleDTOs) {
                _missionDTO = missionDTO;
                _tablePanel = new() {
                    Parent = ContentPanel,
                    ColumnCount = _tableColumns,
                };

                _missionName = new("任务名称") {
                    Parent = _tablePanel,
                    Ratio = 6.75,
                    NameAlignment = HorizontalAlignment.Right,
                };
                _maxNGNum = new("最大NG数") {
                    Parent = _tablePanel,
                    Ratio = 6.75,
                    NameAlignment = HorizontalAlignment.Right,
                    NumberOnly = true,
                };
                _productsBarCodeNum = new("产品条码") {
                    Parent = _tablePanel,
                    Ratio = 6.75,
                    NameAlignment = HorizontalAlignment.Right,
                    Enabled = false,
                };
                _partsBarCodeNum = new("物料条码") {
                    Parent = _tablePanel,
                    Ratio = 6.75,
                    NameAlignment = HorizontalAlignment.Right,
                    Enabled = false,
                };

                // 数据回填
                _missionName.SetValue(0, missionDTO.name);
                _maxNGNum.SetValue(0, missionDTO.max_ng_num + "");

                int productsBarCodeNum = 0;
                int partsBarCodeNum = 0;
                foreach (BarCodeMatchingRuleDTO rule in barCodeMatchingRuleDTOs) {
                    if (rule.type == BarCodeTypes.PRODUCT.Id) {
                        productsBarCodeNum++;
                    } else {
                        partsBarCodeNum++;
                    }
                }
                _productsBarCodeNum.SetValue(0, productsBarCodeNum > 0 ? "已配置" : "未配置");
                _partsBarCodeNum.SetValue(0, partsBarCodeNum > 0 ? $"已配置{partsBarCodeNum}个" : "未配置");
            }

            public void ResizeSelf() {
                ResizeTablePanelAndItsChildren();
                Invalidate();
            }

            public void ResizeTablePanelAndItsChildren() {
                CalculateDetailProperties();

                Padding contentPadding = ContentPanel.Padding;
                int boxHeight = WidgetUtils.TextOrComboBoxHeight();
                int boxMargin = boxHeight / 5;
                int subTitleHeight = WidgetUtils.PopUpOrFloatingFormSubTitle();
                int subTitleMargin = subTitleHeight / 5;
                int tableHeight = 0;
                int previousRowIndex = -1;
                int cntentWidth = (int) (WidgetUtils.MainSize.Width * .65);
                int tableWidth = cntentWidth - contentPadding.Size.Width;
                int contentPieceWidth = tableWidth / _tablePanel.ColumnCount - boxMargin * 2;
                foreach (Control control in _tablePanel.Controls) {
                    if (control.Visible) {
                        int currentRowIndex = _tablePanel.GetPositionFromControl(control).Row;
                        if (currentRowIndex != previousRowIndex) {
                            previousRowIndex = currentRowIndex;
                            if (control is TitlePanel titlePanel) {
                                tableHeight += subTitleHeight + subTitleMargin * 2;
                            } else if (control is SubPanel<ProductBoltDTO> subPanel) {
                                subPanel.ResizeSelf(tableWidth);
                                tableHeight += subPanel.Height;
                            } else if (control is PictureBoxGroup pictureBox) {
                                pictureBox.SetSize(contentPieceWidth, boxHeight, WidgetUtils.PictureBoxGroupBaseHeight(), 1, contentPieceWidth + boxMargin * 2);
                                pictureBox.Margin = new(boxMargin);
                                tableHeight += pictureBox.Height + subTitleMargin * 2;
                            } else {
                                tableHeight += boxHeight + boxMargin * 2;
                            }
                        }
                    }
                }
                Size contentSize = new(cntentWidth, tableHeight + contentPadding.Size.Height);
                _tablePanel.Size = new(tableWidth, tableHeight);
                foreach (Control control in _tablePanel.Controls) {
                    if (control is TitlePanel titlePanel) {
                        titlePanel.Margin = new(0, boxMargin, 0, boxMargin);
                        titlePanel.Size = new(_tablePanel.Width, subTitleHeight);
                    } else if (control is SubPanel<ProductBoltDTO> subPanel) {
                        continue;
                    } else if (control is PictureBoxGroup pictureBox) {
                        continue;
                    } else {
                        control.Margin = new(boxMargin);
                        control.Size = new(contentPieceWidth, boxHeight);
                    }
                }

                SetContentSizeAndSelfSize(contentSize);
            }
        }

        public class ImageButton: CustomImageTextButtonBase {
            private const float _imageSideRatio = 0.4F;
            private int _gapBetweenImageAndText;

            public ImageButton() {
                ForeColor = ColorConfigs.COLOR_MENU_FOREGROUND;
            }

            protected override void OnSizeChanged(EventArgs e) {
                _gapBetweenImageAndText = (int) (this.Height * .1);
                base.OnSizeChanged(e);
            }

            protected override void ResizeIconImage() {
                if (this.Icon != null) {
                    int newImageSide = (int) (Height * _imageSideRatio);
                    this.ImageShowing = WidgetUtils.ResizeImageWithoutLosingQuality(this.Icon, newImageSide, newImageSide);
                    // Recalculate image location
                    this.ImageX = (this.Width - newImageSide) / 2;
                    this.ImageY = (this.Height - newImageSide - this.Font.Height - _gapBetweenImageAndText) / 2;
                }
            }

            protected override void ResizeTextLabel() {
                if (this.Label != null) {
                    this.Font = new Font(WidgetsConfigs.SystemFontFamily, this.Height * .225F, FontStyle.Bold, GraphicsUnit.Pixel);
                    // Recalculate label location
                    int newImageSide = (int) (Height * _imageSideRatio);
                    using (Graphics g = CreateGraphics()) {
                        this.LabelX = (int) ((this.Width - g.MeasureString(this.Label, this.Font).Width) / 2 + this.Width * .02);
                    }
                    this.LabelY = (this.Height - this.Font.Height - newImageSide) / 2 + newImageSide;
                }
            }
        }


        public class AddNewSideButton: CommonButton {
            public AddNewSideButton(string buttonName) {
                Label = buttonName;
                ConerRadius = 0;
            }
            protected override void ResizeTextLabel() {
                if (this.Label != null) {
                    Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (Height * .45), FontStyle.Regular, GraphicsUnit.Pixel);
                    using (Graphics g = CreateGraphics()) {
                        this.LabelX = (int) ((this.Width - g.MeasureString(this.Label, this.Font).Width) / 2 + this.Width * .01);
                    }
                    this.LabelY = (int) ((this.Height - this.Font.Height * 1.1) / 2);
                }
            }
        }
        public class SideButton: DeletableButton {
            private Color? _originalBackColor;
            private Color? _triggeredBackColor;
            private ProductSideDTO? _sideDTO;
            private LeftBottomContentPanel _container;
            private ProductImageFile _productImageFile;
            private ProductImageFile _productImageFileNew;
            // All rounded bolt buttons above the product image
            private SortedList<int, BoltButton> _boltButtons;
            // Radius of rounded bolt buttons
            private int _boltButtonRadius;
            // All edition buttons in the right panel
            private SortedList<int, BoltEditionButton> _boltEditionButtons;
            // List of serial numbers of all bolts
            private List<int> _boltSerialNums;
            // Queue of deleted serial numbers, used for adding new bolt, make sure they have consecutive serial numbers
            private Queue<int> _deletedSerialNum;
            // Serial number of current chosen bolt
            private int? _currentSerialNum;
            // Tip of each side button, to tell that it can be double clicked to rename
            private ToolTip? _toolTip;

            public ProductSideDTO? SideDTO { get => _sideDTO; set => _sideDTO = value; }
            public ProductImageFile ProductImageFile { set => _productImageFile = value; }
            public ProductImageFile ProductImageFileNew { get => _productImageFileNew; }
            public SortedList<int, BoltButton> BoltButtons { get => _boltButtons; }
            public int BoltButtonRadius { get => _boltButtonRadius; set => _boltButtonRadius = value; }
            public SortedList<int, BoltEditionButton> BoltEditionButtons { get => _boltEditionButtons; }
            public List<int> BoltSerialNums { get => _boltSerialNums; }
            public Queue<int> DeletedSerialNum { get => _deletedSerialNum; }
            public int? CurrentSerialNum { get => _currentSerialNum; set => _currentSerialNum = value; }

            // Properties for distinguishing single click and double click
            public int ClickTimes { get; set; }
            public int Milliseconds { get; set; }
            public Timer ClickTimer { get; set; }
            private bool Fired { get; set; }
            public EventArgs? EventArgs { get; set; }

            public Action<EventArgs>? SingleClickDelegate;
            public Action<EventArgs>? DoubleClickDelegate;

            public SideButton(string buttonName) {
                Label = buttonName;
                ConerRadius = 0;

                InitializeTimer();
            }

            public SideButton(ProductSideDTO sideDTO, LeftBottomContentPanel leftBottomContentPanel,
                    ProductImageFile productImageFile, ProductImageFile productImageFileNew) {
                _sideDTO = sideDTO;
                Label = sideDTO.name;
                _container = leftBottomContentPanel;
                _productImageFile = productImageFile;
                _productImageFileNew = productImageFileNew;
                _boltButtons = new();
                _boltButtonRadius = 1;
                _boltEditionButtons = new();
                _boltSerialNums = new();
                _deletedSerialNum = new();

                ConerRadius = 0;
                GroupMode = true;
                BlockHoverUp = true;
                ToggledButton = true;
                ToggleBar = true;
                ToggleBarDirection = ToggleBarDirectionEnum.BOTTOM;
                _toolTip = new() {
                    InitialDelay = 400,
                };
                _toolTip.SetToolTip(this, "双击编辑产品面名称");

                InitializeTimer();
            }

            private void InitializeTimer() {
                ClickTimes = 0;
                Milliseconds = 0;
                Fired = false;
                ClickTimer = new();
                ClickTimer.Interval = 50;
                ClickTimer.Tick += (sender, eventArgs) => {
                    Milliseconds += ClickTimer.Interval;
                    if (Milliseconds >= 500) {
                        ClickTimer.Stop();
                        ClickTimes = 0;
                        Milliseconds = 0;
                        Fired = false;
                    } else if (!Fired && Milliseconds >= 200) {
                        switch (ClickTimes) {
                            case 1:
                                if (SingleClickDelegate != null && EventArgs != null) {
                                    SingleClickDelegate(EventArgs);
                                }
                                Fired = true;
                                break;
                            case 2:
                                if (DoubleClickDelegate != null && EventArgs != null) {
                                    DoubleClickDelegate(EventArgs);
                                }
                                Fired = true;
                                break;
                        }
                    }
                };
            }

            public new void SetToggle(bool flag) {
                if (_originalBackColor == null) {
                    _originalBackColor = BackColor;
                }
                if (_triggeredBackColor == null) {
                    _triggeredBackColor = WidgetUtils.LightColor(BackColor, .5F);
                }
                if (flag) {
                    BackColor = _triggeredBackColor.Value;
                } else {
                    BackColor = _originalBackColor.Value;
                }
                base.SetToggle(flag);

                _productImageFileNew.RefreshImage();

                foreach (BoltButton button in _boltButtons.Values) {
                    button.Visible = flag;
                }
                foreach (BoltEditionButton button in _boltEditionButtons.Values) {
                    button.Visible = flag;
                }
                ChangeFontStyle();
            }

            public void ReCalculateProductImageRatio() {
                if (SideDTO != null) {
                    SideDTO.max_rectangle_width = _container.MaxRectWidth;
                    SideDTO.max_rectangle_height = _container.MaxRectHeight;
                    SideDTO.max_rectangle_location = _container.MaxRectLocation.ToString();
                }
                _productImageFile.RecalculateZoomingRatio();
                _productImageFileNew.RecalculateZoomingRatio();
            }

            public void ImageReset() {
                _productImageFileNew.CopyFrom(_productImageFile);
                _productImageFileNew.RefreshImage();
            }

            public void DeleteBolt() {
                if (_currentSerialNum != null && SideDTO != null) {
                    // Delete bolt buttons (both in pictrue and right panel)
                    BoltButton boltButton = _boltButtons[_currentSerialNum.Value];
                    BoltEditionButton boltEditionButton = _boltEditionButtons[_currentSerialNum.Value];
                    _boltButtons.Remove(_currentSerialNum.Value);
                    _boltEditionButtons.Remove(_currentSerialNum.Value);
                    // Enqueue serial number and wait for a new bolt
                    _deletedSerialNum.Enqueue(_currentSerialNum.Value);
                    _boltSerialNums.Remove(_currentSerialNum.Value);
                    // Reset current bolt index
                    _currentSerialNum = null;
                    // Delete boltDto from sideDto
                    boltButton.BoltDTO.deleted = (int) (YesOrNo.YES);
                    // Dispose deleted buttons
                    boltButton.Dispose();
                    boltEditionButton.Dispose();
                }
            }

            protected override void ResizeTextLabel() {
                if (this.Label != null) {
                    ChangeFontStyle();
                    using (Graphics g = CreateGraphics()) {
                        this.LabelX = (int) ((this.Width - g.MeasureString(this.Label, this.Font).Width) / 2 + this.Width * .01);
                    }
                    this.LabelY = (int) ((this.Height - this.Font.Height * 1.1) / 2);
                }
            }

            private void ChangeFontStyle() {
                this.Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (Height * .45), Toggled ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
            }
            protected override void OnMouseUp(MouseEventArgs mevent) {
                base.OnMouseUp(mevent);
                if (!PressingClose) {
                    if (mevent.Button == MouseButtons.Left) {
                        if (ClickTimes == 0) {
                            EventArgs = mevent;
                            ClickTimer.Start();
                        }
                        ClickTimes++;
                    }
                }
            }
        }

        public class LeftBottomContentPanel: AProductImageDisplayPanel {
            private string _defaultText;
            private List<Color> _rectColors;
            private List<string> _ratioInfos;
            private string _notice;

            // Properties for distinguishing single click and double click
            public int ClickTimes { get; set; }
            public int Milliseconds { get; set; }
            public Timer ClickTimer { get; set; }
            private bool Fired { get; set; }
            public MouseEventArgs? EventArgs { get; set; }

            public Action<MouseEventArgs>? SingleClickDelegate;
            public Action<MouseEventArgs>? DoubleClickDelegate;

            public LeftBottomContentPanel(Image productDefaultImage, string defaultText, string notice) : base() {
                ProductDefaultImage = productDefaultImage;
                _defaultText = defaultText;
                _notice = notice;
                _rectColors = new();
                _ratioInfos = new();
                // for (int i = 0; i < DifferentRects.Count; i++) {
                //     _rectColors.Add(new());
                //     _ratioInfos.Add("");
                // }

                ClickTimes = 0;
                Milliseconds = 0;
                Fired = false;
                ClickTimer = new();
                ClickTimer.Interval = 50;
                ClickTimer.Tick += (sender, eventArgs) => {
                    Milliseconds += ClickTimer.Interval;
                    if (Milliseconds >= 500) {
                        ClickTimer.Stop();
                        ClickTimes = 0;
                        Milliseconds = 0;
                        Fired = false;
                    } else if (!Fired && Milliseconds >= 200) {
                        switch (ClickTimes) {
                            case 1:
                                Fired = true;
                                if (SingleClickDelegate != null && EventArgs != null) {
                                    SingleClickDelegate(EventArgs);
                                }
                                break;
                            case 2:
                                Fired = true;
                                if (DoubleClickDelegate != null && EventArgs != null) {
                                    DoubleClickDelegate(EventArgs);
                                }
                                break;
                        }
                    }
                };
            }

            protected override void InvokeResizing() {
                // Make maximum width equals to 95% of parent width to ensure all retangles can be seen

                int mainFormWidth = WidgetUtils.MainForm.Width;
                int mainFormHeight = WidgetUtils.MainForm.Height;
                int workPlacePadding = WidgetUtils.ContentInnerBorderMargin(WidgetUtils.MainForm.Size) * 2 + 1;
                int workPlaceWidth = mainFormWidth - workPlacePadding * 2;
                int workPlaceHeight =  mainFormHeight - (int) (mainFormHeight * WidgetUtils.WorkplaceTopBarHeightRatio()) - workPlacePadding * 2;
                Size workPlaceImageDisplayPanelSize = new((int) (workPlaceWidth * WidgetUtils.WorkplaceLeftWidthRatio()), (int) (workPlaceHeight * WidgetUtils.WorkplaceImagePanelHeightRatio()));

                MaxRectSize = MainUtils.GetProperSizeAccordingToSizeRatio((Size * .95F).ToSize(), workPlaceImageDisplayPanelSize);
                MaxRectWidth = MaxRectSize.Width;
                MaxRectHeight = MaxRectSize.Height;
                // Calculate location of max rectangle depends on size
                MaxRectLocation = new((Width - MaxRectWidth) / 2, (Height - MaxRectHeight) / 2);
                MaxRect = new(MaxRectLocation, MaxRectSize);
                // Get enumerator again and iterate over it to resize all rectangles
                // int index = 0;
                // List<SizeRatioNRectColor>.Enumerator enumerator = WidthHeightRatio.GetEnumerator();
                // while (enumerator.MoveNext()) {
                //     SizeRatioNRectColor current = enumerator.Current;
                //     Rectangle rect = DifferentRects[index];
                //
                //     int width = MaxRectWidth;
                //     int height = (int) (width / (decimal) current.WidthRatio * current.HeightRatio);
                //     if (height > MaxRectHeight) {
                //         height = MaxRectHeight;
                //         width = (int) (height / (decimal) current.HeightRatio * current.WidthRatio);
                //     }
                //
                //     rect.Size = new(width, height);
                //     rect.Location = new((Width - rect.Width) / 2, (Height - rect.Height) / 2);
                //
                //     DifferentRects[index] = rect;
                //     _rectColors[index] = current.RectColor;
                //     _ratioInfos[index] = current.WidthRatio + ":" + current.HeightRatio;
                //     index++;
                // }
            }

            protected override void InvokePaint(Graphics g) {
                // if (DifferentRects[0].Width == 0) {
                //     throw new Exception("出现了！是 DifferentRects[0].Width == 0！");
                //     // return;
                // }
                g.SmoothingMode = SmoothingMode.HighSpeed;
                if (ProductImage == null || ImageLocation == null) {
                    int newImageSide = Height / 20;
                    ProductDefaultImageShowing = WidgetUtils.ResizeImageWithoutLosingQuality(ProductDefaultImage, newImageSide, newImageSide);
                    int gapBetweenImageAndText = newImageSide / 4;

                    Font = new(WidgetsConfigs.SystemFontFamily, newImageSide * .6F, FontStyle.Regular, GraphicsUnit.Pixel);
                    int textWidth = (int) (g.MeasureString(_defaultText, Font).Width);
                    int imageX = (Width - ProductDefaultImageShowing.Width - textWidth - gapBetweenImageAndText) / 2;
                    g.DrawImage(ProductDefaultImageShowing, new Point(imageX, (Height - newImageSide) / 2));

                    Brush brush = new SolidBrush(ColorConfigs.COLOR_EMPTY_PRODUCT_IMAGE_NOTICE_TEXT);
                    Point point = new Point(imageX + ProductDefaultImageShowing.Width + gapBetweenImageAndText, (Height - Font.Height) / 2 - Font.Height / 10);
                    g.DrawString(_defaultText, Font, brush, point);
                } else {
                    // 画产品图片
                    g.DrawImage(ProductImage, ImageLocation.Value);

                    // for (int i = 0; i < DifferentRects.Count; i++) {
                    //     Pen pen = new Pen(_rectColors[i], 1) {
                    //         DashPattern = new float[] {9, 6, 9, 6},
                    //     };
                    //     g.DrawRectangle(pen, DifferentRects[i]);
                    // }
                    // Font ratioTextFont = new(WidgetsConfigs.SystemFontFamily, Height * .025F, FontStyle.Bold, GraphicsUnit.Pixel);
                    // int x = 0;
                    // int verticalGap = ratioTextFont.Height / 2;
                    // for (int i = 0; i < _ratioInfos.Count; i++) {
                    //     Brush brush = new SolidBrush(_rectColors[i]);
                    //     if (i == 0) {
                    //         x = verticalGap;
                    //     } else {
                    //         x += verticalGap + (int) g.MeasureString(_ratioInfos[i - 1], ratioTextFont).Width;
                    //     }
                    //     Point point = new Point(x, (int) (Height - ratioTextFont.Height * 1.1));
                    //     g.DrawString(_ratioInfos[i], ratioTextFont, brush, point);
                    // }
                    // Font noticeFont = new Font(WidgetsConfigs.SystemFontFamily, Height * .025F, FontStyle.Regular, GraphicsUnit.Pixel);
                    // x += verticalGap + (int) g.MeasureString(_ratioInfos[_ratioInfos.Count - 1], noticeFont).Width;
                    // Point p = new Point(x, (int) (Height - noticeFont.Height * 1.1));
                    // g.DrawString(_notice, noticeFont, new SolidBrush(Color.Red), p);

                    // 只画最大的范围
                    Pen pen = new Pen(ColorConfigs.COLOR_MISSION_BLOCK_BORDER, 1) {
                        DashPattern = new float[] {9, 6, 9, 6},
                    };
                    g.DrawRectangle(pen, MaxRect);
                    Font noticeFont = new Font(WidgetsConfigs.SystemFontFamily, Height * .025F, FontStyle.Regular, GraphicsUnit.Pixel);
                    Point p = new Point(noticeFont.Height / 3, (int) (Height - noticeFont.Height * 1.1));
                    g.DrawString(_notice, noticeFont, new SolidBrush(Color.Red), p);
                }
            }

            protected override void OnMouseUp(MouseEventArgs mevent) {
                if (mevent.Button == MouseButtons.Left) {
                    if (ClickTimes == 0) {
                        EventArgs = mevent;
                        ClickTimer.Start();
                    }
                    ClickTimes++;
                }
                base.OnMouseUp(mevent);
            }
        }

        public class RightContentPanel: CustomContentPanel {
            private List<SideButton> _sideButtons;
            private int _boltButtonHeight;
            private int _boltButtonMargin;
            private int _boltButtonWidth;

            public int BoltButtonHeight { get => _boltButtonHeight; set => _boltButtonHeight = value; }
            public int BoltButtonMargin { get => _boltButtonMargin; set => _boltButtonMargin = value; }
            public int BoltButtonWidth { get => _boltButtonWidth; set => _boltButtonWidth = value; }

            public RightContentPanel(List<SideButton> sideButtons) {
                _sideButtons = sideButtons;
            }

            protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
                if (OuterVScrollPanel != null) {
                    foreach (SideButton sideButton in _sideButtons) {
                        foreach (BoltEditionButton boltEditionButton in sideButton.BoltEditionButtons.Values) {
                            boltEditionButton.Size = new(_boltButtonWidth, _boltButtonHeight);
                            boltEditionButton.Margin = new(_boltButtonMargin, _boltButtonMargin, _boltButtonMargin, 0);
                        }
                    }
                }
            }

            public override bool CheckNeedsScrollBar(int parentNewHeight) {
                return NewHeight > parentNewHeight;
            }
        }

        public class BoltEditionButton: DeletableButton {
            private ProductBoltDTO _boltDTO;
            public ProductBoltDTO BoltDTO { get => _boltDTO; set => _boltDTO = value; }

            // Properties for distinguishing single click and double click
            public int ClickTimes { get; set; }
            public int Milliseconds { get; set; }
            public Timer ClickTimer { get; set; }
            private bool Fired { get; set; }
            public MouseEventArgs? EventArgs { get; set; }
            public Action<MouseEventArgs>? SingleClickDelegate;
            public Action<MouseEventArgs>? DoubleClickDelegate;

            public BoltEditionButton(ProductBoltDTO boltDTO) {
                _boltDTO = boltDTO;
                Label = boltDTO.serial_num + ". " + boltDTO.name;

                ConerRadius = 0;
                GroupMode = true;
                BlockHoverUp = true;
                ToggledButton = true;

                ClickTimes = 0;
                Milliseconds = 0;
                Fired = false;
                ClickTimer = new();
                ClickTimer.Interval = 50;
                ClickTimer.Tick += (sender, eventArgs) => {
                    Milliseconds += ClickTimer.Interval;
                    if (Milliseconds >= 500) {
                        ClickTimer.Stop();
                        ClickTimes = 0;
                        Milliseconds = 0;
                        Fired = false;
                    } else if (!Fired && Milliseconds >= 200) {
                        switch (ClickTimes) {
                            case 1:
                                Fired = true;
                                if (SingleClickDelegate != null && EventArgs != null) {
                                    SingleClickDelegate(EventArgs);
                                }
                                break;
                            case 2:
                                Fired = true;
                                if (DoubleClickDelegate != null && EventArgs != null) {
                                    DoubleClickDelegate(EventArgs);
                                }
                                break;
                        }
                    }
                };
            }

            protected override void ResizeTextLabel() {
                if (this.Label != null && Height > 0) {
                    this.Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (Height * .45), FontStyle.Regular, GraphicsUnit.Pixel);
                    using (Graphics g = CreateGraphics()) {
                        this.LabelX = (int) ((this.Width - g.MeasureString(this.Label, this.Font).Width) / 2 + this.Width * .02);
                    }
                    this.LabelY = (int) ((this.Height - this.Font.Height) / 2);
                }
            }

            protected override void OnMouseUp(MouseEventArgs mevent) {
                base.OnMouseUp(mevent);
                if (!PressingClose) {
                    if (mevent.Button == MouseButtons.Left) {
                        if (ClickTimes == 0) {
                            EventArgs = mevent;
                            ClickTimer.Start();
                        }
                        ClickTimes++;
                    }
                }
            }
        }
    }
}
