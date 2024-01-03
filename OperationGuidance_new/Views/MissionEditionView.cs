using System.Drawing.Drawing2D;
using CustomLibrary.Buttons;
using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Configs;
using CustomLibrary.Constants;
using CustomLibrary.Events;
using CustomLibrary.Panels;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Apis;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;
using Timer = System.Windows.Forms.Timer;

namespace OperationGuidance_new.Views {
    public partial class MissionEditionView: CustomContentPanel {
        private ProductMissionDTO _missionDTO;
        private MissionEditionPage? _editionPage;

        public MissionEditionView() {
            CreateANewOne();
        }

        public void CreateANewOne() {
            _missionDTO = new() {
                name = "任务名称",
                ProductSides = new(),
            };
            OpenEditionPage(_missionDTO);
        }

        public void OpenEditionPage(ProductMissionDTO missionDTO) {
            // Clear all child controls
            Controls.Clear();
            // Create a new page according to missionbody and show
            _editionPage = new(this, missionDTO);
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            if (_editionPage != null) {
                _editionPage.Size = Size;
            }
        }

        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            return false;
        }

        // Class: inner page panel
        public class MissionEditionPage: CustomContentPanel {
            private OperationGuidanceApis _apis;
            private MissionEditionView _parentView;
            private ProductMissionDTO _missionDTO;
            private bool _saved;

            // Contents
            private CustomContentPanel _left;
            private CustomContentPanel _leftTop;
            private WorkplacePiece _leftBottom;
            private WorkplacePiece _right;
            private int? _littleTitleHeight;

            // Left top
            private CustomTextBoxGroup _missionName;
            private CommonButton _buttonSave;
            private CommonButton _buttonNew;
            private CommonButton _buttonDelete;
            private CommonButton _buttonPublish;
            private ImageButton _imageButtonChoose;
            private ImageButton _imageButtonZoomIn;
            private ImageButton _imageButtonZoomOut;
            private ImageButton _imageButtonRotateAntiClockwise;
            private ImageButton _imageButtonRotateClockwise;
            private ImageButton _imageButtonCrop;
            private ImageButton _imageButtonUndo;
            private ImageButton _imageButtonReset;

            // Left side title panel: needs to be alone, don't need any margin
            private CustomContentPanel _sideTitlePanel;
            private List<SideButton> _sideButtons;
            private SideButton _currentSideButton;
            private SideButton _addNewSideButton;

            // Left bottom
            private LeftBottomContentPanel _leftBottomContentPanel;
            private ProductImageFile _currentProductImageFile;
            private int _imageOperationBufferLength;
            private Point _mouseDownLocation;
            private bool _mouseLeftDown;
            private bool _controlDown;
            private bool _needSaveBuffer;
            private BoltEditionPopUpForm _boltPopUpForm;

            // Right
            private CustomContentPanel _boltTitlePanel;
            private Label _boltTitleLabel;
            private RightContentPanel _rightContentPanel;
            private CustomVScrollingContentPanel _autoScrollContentOuterPanel;
            // private BoltDetialPopUpForm _boltPopUpForm;

            public MissionEditionPage(MissionEditionView parent, ProductMissionDTO missionDTO) : base() {
                _apis = SystemUtils.GetApis();
                _saved = false;
                _parentView = parent;
                Parent = parent;
                _missionDTO = missionDTO;

                InitializeContent();
                InitializeLeftTop();
                InitializeLeftBottom();
                InitializeRight();
            }

            private void InitializeContent() {
                _left = new() {
                    Parent = this,
                    Padding = new(0),
                    FlowDirection = FlowDirection.TopDown,
                };
                _leftTop = new() {
                    Parent = _left,
                    Padding = new(0),
                };
                // Place side title panel here to ensure ratio of bottom is correct
                _sideTitlePanel = new() {
                    Parent = _left,
                    Padding = new(0),
                    BackColor = ConfigsVariables.COLOR_MISSION_EDITION_IMAGE_TITLE_PANEL_BACK,
                };
                _leftBottom = new() {
                    Parent = _left,
                    Padding = new(0),
                    OuterPenBorderColor = ConfigsVariables.COLOR_CONTENT_PANEL_INNER_BORDER,
                };

                _right = new() {
                    Parent = this,
                    Padding = new(0),
                    FlowDirection = FlowDirection.TopDown,
                    OuterPenBorderColor = ConfigsVariables.COLOR_CONTENT_PANEL_INNER_BORDER,
                    ForeColor = ConfigsVariables.COLOR_MISSION_EDITION_TEXT,
                    BackColor = ConfigsVariables.COLOR_EMPTY_PRODUCT_CONTENT_BACKGROUND,
                };
            }

            private void InitializeLeftTop() {
                _missionName = new("任务名称") {
                    Parent = _leftTop,
                    BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                    ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                    BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                    BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
                    NameAlignment = HorizontalAlignment.Left,
                };
                CustomTextBox textBox = _missionName.GetTextBox(0);
                textBox.TextChanged += (sender, eventArgs) => {
                    if (!_missionName.HasError) {
                        _missionDTO.name = textBox.Text;
                    }
                };
                textBox.Text = _missionDTO.name;

                _buttonSave = new() {
                    Parent = _leftTop,
                    Label = "保存",
                    BlockHoverUp = true,
                };
                _buttonSave.Click += (sender, eventArgs) => {
                    // Store to database
                    AddOrUpdateProductMissionReq req = new(_missionDTO);
                    AddOrUpdateProductMissionRsp rsp = _apis.AddOrUpdateProductMission(req);
                    if (rsp.RsponseCode == HttpResponseCode.OK) {
                        _saved = true;
                        _missionDTO = rsp.ProductMissionDTO;
                        MessageBox.Show(null, "保存成功！", "保存任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    } else {
                        MessageBox.Show(null, "保存失败！错误信息：" + rsp.RsponseMessage, "保存任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                };
                _buttonPublish = new() {
                    Parent = _leftTop,
                    Label = "发布",
                    BlockHoverUp = true,
                };
                _buttonPublish.Click += (sender, eventArgs) => {
                    // TODO: publish it (store tatus to database)
                };
                _buttonNew = new() {
                    Parent = _leftTop,
                    Label = "新增",
                    BlockHoverUp = true,
                };
                _buttonNew.Click += (sender, eventArgs) => {
                    if (!_saved) {
                        DialogResult result = MessageBox.Show(null, "当前还有未保存内容，确定新增任务？", "新增任务", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result == DialogResult.Yes) {
                            _parentView.CreateANewOne();
                        }
                    } else {
                        _parentView.CreateANewOne();
                    }
                };
                _buttonDelete = new() {
                    Parent = _leftTop,
                    Label = "删除",
                    BlockHoverUp = true,
                };
                _buttonDelete.Click += (sender, eventArgs) => {
                    DialogResult result = MessageBox.Show(null, "确定删除任务？", "删除任务", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes) {
                        DeleteProductMissionReq req = new(_missionDTO);
                        DeleteProductMissionRsp rsp = _apis.DeleteProductMission(req);
                        if (rsp.RsponseCode == (int) HttpResponseCode.OK) {
                            MessageBox.Show(null, "删除成功！", "删除任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            // 删除后跳转至任务列表界面
                            CustomChildMenuFirstButton missionListButton = WidgetUtils.GetChildMenu(101);
                            missionListButton.TriggerClick(EventArgs.Empty);
                        } else {
                            MessageBox.Show(null, "删除失败！错误信息：" + rsp.RsponseMessage, "删除任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                };

                // 设置图片编辑时可撤回的次数（即可以回溯多少次操作）
                _imageOperationBufferLength = 20;
                _imageButtonChoose = GenerateImageButton("选择图片", Properties.Resources.image_choose, (sender, eventArgs) => {
                    _currentProductImageFile.ClearBuffer();
                    _currentProductImageFile.ImageSelect();
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
                    Parent = _leftTop,
                    Label = label,
                    BlockHoverUp = true,
                    Icon = icon,
                };
                button.Click += eventHandler;
                return button;
            }

            private void InitializeLeftBottom() {
                _leftBottomContentPanel = new(Properties.Resources.image_choose, "点击添加产品图片", "（请确保所有螺栓点位在最小范围内，以免分辨率很小时显示不全）") {
                    Parent = _leftBottom,
                    Margin = new(1, 1, 0, 0),
                    Padding = new(0),
                    BackColor = ConfigsVariables.COLOR_EMPTY_PRODUCT_CONTENT_BACKGROUND,
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
                        _currentProductImageFile.ClearBuffer();
                        _currentProductImageFile.ImageSelect();
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
                            boltDTO.serial_num = _currentSideButton.BoltButtons.Count + 1;
                        }
                        _currentSideButton.BoltSerialNums.Add(boltDTO.serial_num);
                        _currentSideButton.BoltSerialNums.Sort();
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
                        ResizeLeftBottom();
                        ForceResizeRight();

                        // Save new boltDto to sideDto
                        sideDTO.Bolts.Add(boltDTO);

                        // Trigger rename logic of bolt
                        if (boltEditionButton.DoubleClickDelegate != null) {
                            boltEditionButton.DoubleClickDelegate(eventArgs);
                        }
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
                        locationOffset.X += _currentProductImageFile.LocationOffsetMoving.X;
                        locationOffset.Y += _currentProductImageFile.LocationOffsetMoving.Y;
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
                    BackColor = ConfigsVariables.COLOR_MISSION_EDITION_IMAGE_SIDE_BUTTON_NEW,
                    ForeColor = ConfigsVariables.COLOR_MISSION_EDITION_TEXT,
                    ToggleBarColor = ConfigsVariables.COLOR_MISSION_EDITION_IMAGE_SIDE_BUTTON_TOGGLED,
                };
                _addNewSideButton.SingleClickDelegate += (eventArgs) => {
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
                };
            }

            private SideButton NewSideButton(ProductSideDTO sideDTO) {
                ProductImageFile productImageFile = new(_leftBottomContentPanel, sideDTO, _imageOperationBufferLength);
                ProductImageFile productImageFileNew = new(_leftBottomContentPanel, sideDTO, _imageOperationBufferLength);

                // Initialzie side button
                SideButton sideButton = new(sideDTO, _leftBottomContentPanel, productImageFile, productImageFileNew) {
                    Parent = _sideTitlePanel,
                    BackColor = Color.Transparent,
                    ForeColor = ConfigsVariables.COLOR_MISSION_EDITION_TEXT,
                    ToggleBarColor = ConfigsVariables.COLOR_MISSION_EDITION_IMAGE_SIDE_BUTTON_TOGGLED,
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
                            sideButton.Label = box.Text;
                            sideDTO.name = box.Text;
                            using (Graphics g = CreateGraphics()) {
                                int btnLabelWidth = (int) g.MeasureString(sideButton.Label, sideButton.Font).Width;
                                sideButton.Width = (int) (btnLabelWidth + sideButton.Height * 1.2);
                            }
                        }
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

            private void OpenBoltPopUpForm(ProductBoltDTO boltDTO) {
                _boltPopUpForm = new(boltDTO) {
                    Title = boltDTO.serial_num + " - " + boltDTO.name,
                    BorderColor = ConfigsVariables.COLOR_POP_UP_BORDER,
                };
                // 添加按钮
                CommonButton confirmButton = _boltPopUpForm.AddButton("保存信息");
                confirmButton.Click += (s, e) => {
                    _boltPopUpForm.HideForm();
                };
                CommonButton deleteButton = _boltPopUpForm.AddButton("删除点位");
                deleteButton.Click += (s, e) => {
                    _currentSideButton.DeleteBolt();
                    _boltPopUpForm.HideForm();
                    ForceResizeRight();
                };
                CommonButton cancelButton = _boltPopUpForm.AddButton("取消");
                cancelButton.Click += (s, e) => {
                    _boltPopUpForm.HideForm();
                };
                // Show form but make it transparent to create handles for its children
                _boltPopUpForm.FakeShowToCreateHandlesForChildren();
                // Resize all widgets
                ResizePopUpForm();
                // Real show
                _boltPopUpForm.Show();
                // Set current pop up form
                EventFuncs.CurrentPopUpForm = _boltPopUpForm;
            }

            private void SideButonClick(SideButton sideButton) {
                if (sideButton != _currentSideButton) {
                    _currentSideButton.SetToggle(false);
                    sideButton.SetToggle(true);
                    _currentSideButton = sideButton;
                    _currentProductImageFile = _currentSideButton.ProductImageFileNew;

                    ForceResizeRight();
                }
            }

            private void InitializeRight() {
                _boltTitlePanel = new() {
                    Parent = _right,
                    Padding = new(0),
                    Margin = new(1, 1, 0, 0),
                    BackColor = ConfigsVariables.COLOR_MISSION_EDITION_IMAGE_TITLE_PANEL_BACK,
                };
                _rightContentPanel = new(_sideButtons) {
                    Padding = new(0),
                };
                _autoScrollContentOuterPanel = new(null, _rightContentPanel) {
                    Parent = _right,
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
                    ForeColor = _right.ForeColor,
                    BackColor = ConfigsVariables.COLOR_MISSION_EDITION_BUTTON_BACK,
                    Visible = false,
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

            protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
                if (Size == new Size(200, 100)) {
                    // TODO: have to figure out why this happen
                    return;
                }
                Padding outerPadding = Parent.Parent.Parent.Padding;
                ResizeContent(outerPadding);
                ResizeSideButtons();
                ResizeLeftTop();
                ResizeLeftBottom();
                ResizeRight();
            }

            private void ResizeContent(Padding outerPadding) {
                int leftBottomHeight = (int) (Height * .805);
                int leftWidth = (int) (Width * .8);
                int rightWidth = Width - leftWidth - (outerPadding.Left - 1) / 2;

                _left.Size = new(leftWidth, Height);
                _left.Margin = new(0, 0, (outerPadding.Left - 1) / 2, 0);

                _leftBottom.Size = new(_left.Width, leftBottomHeight);
                _leftTop.Size = new(_left.Width, (int) ((_left.Height - _leftBottom.Height) * .76));
                _littleTitleHeight = _left.Height - _leftBottom.Height - _leftTop.Height;
                _sideTitlePanel.Size = new(_leftTop.Width, _littleTitleHeight.Value);

                _right.Size = new(rightWidth, Height);
            }

            private void ResizeSideButtons() {
                int newHeight = _sideTitlePanel.Height;
                foreach (SideButton sideButton in _sideButtons) {
                    // Height must be set first then ResizeTextLabel can be invoked, then the Font can be set
                    sideButton.Height = newHeight;
                    using (Graphics g = CreateGraphics()) {
                        int btnLabelWidth = (int) g.MeasureString(sideButton.Label, sideButton.Font).Width;
                        sideButton.Width = (int) (btnLabelWidth + newHeight * 1.2);
                    }
                }
                _addNewSideButton.Size = new((int) (_sideTitlePanel.Width * .08), newHeight);
            }

            private void ResizeLeftTop() {
                // Recalculate some variables
                int missionNameWidth = _leftTop.Width / 2;
                int buttonsWidth = missionNameWidth;
                int buttonsHeight = WidgetUtils.CommonButtonHeight();
                int hGap = (int) (buttonsHeight * .5);

                // Resize mission name box
                _missionName.Size = new(missionNameWidth, WidgetUtils.TextOrComboBoxHeight());

                // Resize common buttons
                Size buttonSize =  new((int) ((buttonsWidth - hGap * 4) / 4), buttonsHeight);
                HandleCommonButton(_buttonSave);
                HandleCommonButton(_buttonPublish);
                HandleCommonButton(_buttonNew);
                HandleCommonButton(_buttonDelete);

                // Resize image buttons
                int imageButtonSide = _leftTop.Height - buttonsHeight;
                int imageMargin = (int) (imageButtonSide * .1);
                Size imageButtonSize = new(imageButtonSide - imageMargin * 2, imageButtonSide - imageMargin * 2);
                HandleImageButton(_imageButtonChoose);
                HandleImageButton(_imageButtonZoomOut);
                HandleImageButton(_imageButtonZoomIn);
                HandleImageButton(_imageButtonRotateClockwise);
                HandleImageButton(_imageButtonRotateAntiClockwise);
                HandleImageButton(_imageButtonCrop);
                HandleImageButton(_imageButtonUndo);
                HandleImageButton(_imageButtonReset);

                // Inner method for reuse
                void HandleCommonButton(CommonButton button) {
                    button.Size = buttonSize;
                    button.Margin = new(hGap, 0, 0, 0);
                }
                // Inner method for reuse
                void HandleImageButton(ImageButton button) {
                    button.Size = imageButtonSize;
                    button.Margin = new(imageMargin, imageMargin, imageMargin, imageMargin);
                }
            }

            private void ResizeLeftBottom() {
                _leftBottomContentPanel.Size = new(_leftBottom.Width - 2, _leftBottom.Height - 2);

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
                if (_boltPopUpForm != null) {
                    Control mainForm = WidgetUtils.MainPanel.Parent;
                    _boltPopUpForm.CalculateDetailProperties(mainForm);

                    TableLayoutPanel tablePanel = _boltPopUpForm.TablePanel;
                    Padding contentPadding = _boltPopUpForm.ContentPanel.Padding;
                    int boxHeight = WidgetUtils.TextOrComboBoxHeight();
                    int boxMargin = boxHeight / 8;
                    int tableHeight = tablePanel.Controls.Count / tablePanel.ColumnCount * (boxHeight + boxMargin * 2);
                    Size contentSize = new((int) (mainForm.Width * .75), tableHeight + contentPadding.Size.Height);
                    int tableWidth = contentSize.Width - contentPadding.Size.Width;
                    _boltPopUpForm.BoxHeight = boxHeight;
                    _boltPopUpForm.BoxMargin = boxMargin;
                    _boltPopUpForm.TablePanel.Size = new(tableWidth, tableHeight);

                    _boltPopUpForm.SetContentSizeAndSelfSize(contentSize);
                    if (_boltPopUpForm.Visible) {
                        _boltPopUpForm.Invalidate();
                    }
                }
            }

            private void ResizeRight() {
                int controlWidth = _right.Width - 2;
                _boltTitlePanel.Size = new(controlWidth, _littleTitleHeight.Value);
                _boltTitleLabel.Size = _boltTitlePanel.Size;
                _boltTitleLabel.Font = new Font(WidgetsConfigs.SystemFontFamily, _boltTitleLabel.Height * .55F, FontStyle.Bold, GraphicsUnit.Pixel);

                int contentHeight = _right.Height - _boltTitlePanel.Height - 1;
                int boltButtonHeight = (int) (contentHeight * .06);
                int boltButtonMargin = boltButtonHeight / 7;

                _rightContentPanel.NewHeight = (boltButtonHeight + boltButtonMargin) * _currentSideButton.BoltEditionButtons.Count;
                _autoScrollContentOuterPanel.Size = new(controlWidth, contentHeight);
            }

            private void ForceResizeRight() {
                _autoScrollContentOuterPanel.Width -= 1;
                ResizeRight();
            }
        }

        public class ImageButton: CustomImageTextButtonBase {
            private const float _imageSideRatio = 0.4F;
            private int _gapBetweenImageAndText;

            public ImageButton() {
                ForeColor = ConfigsVariables.COLOR_MENU_FOREGROUND;
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

        public class SideButton: CommonButton {
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
            public MouseEventArgs? EventArgs { get; set; }

            public Action<MouseEventArgs>? SingleClickDelegate;
            public Action<MouseEventArgs>? DoubleClickDelegate;

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
                _toolTip.SetToolTip(this, "双击修改产品面名称");

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
                        this.LabelX = (int) ((this.Width - g.MeasureString(this.Label, this.Font).Width) / 2 + this.Width * .02);
                    }
                    this.LabelY = (int) ((this.Height - this.Font.Height * 1.1) / 2);
                }
            }

            private void ChangeFontStyle() {
                this.Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (Height * .5), Toggled ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
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
                for (int i = 0; i < DifferentRects.Count; i++) {
                    _rectColors.Add(new());
                    _ratioInfos.Add("");
                }

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
                // Make maximum width equals to 85% of parent width to ensure all retangles can be seen
                MaxRectSize = MainUtils.GetMaxSizeOfSizeRatioByWidth((int) (Width * .95));
                if (MaxRectSize.Height > Height * .9) {
                    MaxRectSize = MainUtils.GetMaxSizeOfSizeRatioByHeight((int) (Height * .9));
                }
                MaxRectWidth = MaxRectSize.Width;
                MaxRectHeight = MaxRectSize.Height;
                // Calculate location of max rectangle depends on size
                MaxRectLocation = new((Width - MaxRectWidth) / 2, (Height - MaxRectHeight) / 2);
                MaxRect = new(MaxRectLocation, MaxRectSize);
                // Get enumerator again and iterate over it to resize all rectangles
                int index = 0;
                List<SizeRatioNRectColor>.Enumerator enumerator = WidthHeightRatio.GetEnumerator();
                while (enumerator.MoveNext()) {
                    SizeRatioNRectColor current = enumerator.Current;
                    Rectangle rect = DifferentRects[index];

                    int width = MaxRectWidth;
                    int height = (int) (width / (decimal) current.WidthRatio * current.HeightRatio);
                    if (height > MaxRectHeight) {
                        height = MaxRectHeight;
                        width = (int) (height / (decimal) current.HeightRatio * current.WidthRatio);
                    }

                    rect.Size = new(width, height);
                    rect.Location = new((Width - rect.Width) / 2, (Height - rect.Height) / 2);

                    DifferentRects[index] = rect;
                    _rectColors[index] = current.RectColor;
                    _ratioInfos[index] = current.WidthRatio + ":" + current.HeightRatio;
                    index++;
                }
            }

            protected override void InvokePaint(Graphics g) {
                if (DifferentRects[0].Width == 0) {
                    throw new Exception("出现了！是 DifferentRects[0].Width == 0！");
                    // return;
                }
                g.SmoothingMode = SmoothingMode.HighSpeed;
                if (ProductImage == null || ImageLocation == null) {
                    int newImageSide = Height / 20;
                    ProductDefaultImageShowing = WidgetUtils.ResizeImageWithoutLosingQuality(ProductDefaultImage, newImageSide, newImageSide);
                    int gapBetweenImageAndText = newImageSide / 4;

                    Font = new(WidgetsConfigs.SystemFontFamily, newImageSide * .6F, FontStyle.Regular, GraphicsUnit.Pixel);
                    int textWidth = (int) (g.MeasureString(_defaultText, Font).Width);
                    int imageX = (Width - ProductDefaultImageShowing.Width - textWidth - gapBetweenImageAndText) / 2;
                    g.DrawImage(ProductDefaultImageShowing, new Point(imageX, (Height - newImageSide) / 2));

                    Brush brush = new SolidBrush(ConfigsVariables.COLOR_EMPTY_PRODUCT_IMAGE_NOTICE_TEXT);
                    Point point = new Point(imageX + ProductDefaultImageShowing.Width + gapBetweenImageAndText, (Height - Font.Height) / 2 - Font.Height / 10);
                    g.DrawString(_defaultText, Font, brush, point);
                } else {
                    g.DrawImage(ProductImage, ImageLocation.Value);
                    for (int i = 0; i < DifferentRects.Count; i++) {
                        Pen pen = new Pen(_rectColors[i], 1) {
                            DashPattern = new float[] {9, 6, 9, 6},
                        };
                        g.DrawRectangle(pen, DifferentRects[i]);
                    }
                    Font ratioTextFont = new(WidgetsConfigs.SystemFontFamily, Height * .025F, FontStyle.Bold, GraphicsUnit.Pixel);
                    int x = 0;
                    int verticalGap = ratioTextFont.Height / 2;
                    for (int i = 0; i < _ratioInfos.Count; i++) {
                        Brush brush = new SolidBrush(_rectColors[i]);
                        if (i == 0) {
                            x = verticalGap;
                        } else {
                            x += verticalGap + (int) g.MeasureString(_ratioInfos[i - 1], ratioTextFont).Width;
                        }
                        Point point = new Point(x, (int) (Height - ratioTextFont.Height * 1.1));
                        g.DrawString(_ratioInfos[i], ratioTextFont, brush, point);
                    }
                    Font noticeFont = new Font(WidgetsConfigs.SystemFontFamily, Height * .025F, FontStyle.Regular, GraphicsUnit.Pixel);
                    x += verticalGap + (int) g.MeasureString(_ratioInfos[_ratioInfos.Count - 1], noticeFont).Width;
                    Point p = new Point(x, (int) (Height - noticeFont.Height * 1.1));
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

            public RightContentPanel(List<SideButton> sideButtons) {
                _sideButtons = sideButtons;
            }

            protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
                if (OuterVScrollPanel != null) {
                    int boltButtonHeight = (int) (OuterVScrollPanel.Height * .06);
                    int boltButtonMargin = boltButtonHeight / 7;
                    int boltButtonWidth = Width - boltButtonMargin * 2;
                    foreach (SideButton sideButton in _sideButtons) {
                        foreach (BoltEditionButton boltEditionButton in sideButton.BoltEditionButtons.Values) {
                            boltEditionButton.Size = new(boltButtonWidth, boltButtonHeight);
                            boltEditionButton.Margin = new(boltButtonMargin, boltButtonMargin, boltButtonMargin, 0);
                        }
                    }
                }
            }

            public override bool CheckNeedsScrollBar(int parentNewHeight) {
                return NewHeight > parentNewHeight;
            }
        }

        public class BoltEditionButton: CommonButton {
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

            protected override void ResizeTextLabel() {
                if (this.Label != null) {
                    this.Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (Height * .4), FontStyle.Regular, GraphicsUnit.Pixel);
                    using (Graphics g = CreateGraphics()) {
                        this.LabelX = (int) ((this.Width - g.MeasureString(this.Label, this.Font).Width) / 2 + this.Width * .02);
                    }
                    this.LabelY = (int) ((this.Height - this.Font.Height * 1.1) / 2);
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

        public class BoltLocationInfo {
            private Point _boltLocation;

            public Point GetDisplayLocation() {
                // TODO: do some calculation here
                return new();
            }
        }
    }
}
