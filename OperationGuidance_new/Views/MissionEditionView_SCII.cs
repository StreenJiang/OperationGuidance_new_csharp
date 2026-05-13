using CustomLibrary.Buttons;
using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.ComboBoxes;
using CustomLibrary.Configs;
using CustomLibrary.Events;
using CustomLibrary.Forms;
using CustomLibrary.Panels;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using log4net;
using Newtonsoft.Json;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.ReusableWidgets;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;
using System.Drawing.Drawing2D;
using Timer = System.Windows.Forms.Timer;

namespace OperationGuidance_new.Views {
    public partial class MissionEditionView_SCII: CustomContentPanel {
        private readonly OperationGuidanceApis apis;
        private ProductMissionDTO? _missionDTO;
        private MissionEditionPage_SCII? _editionPage;

        public ProductMissionDTO? MissionDTO { get => _missionDTO; set => _missionDTO = value; }
        public MissionEditionPage_SCII? EditionPage { get => _editionPage; set => _editionPage = value; }

        public MissionEditionView_SCII() {
            apis = SystemUtils.GetApis();
            CreateANewOne();
        }

        public MissionEditionPage_SCII CreateANewOne() {
            return OpenEditionPage(null);
        }

        public MissionEditionPage_SCII OpenEditionPage(int? missionId) {
            ProductMissionDTO NewMission() {
                return new() {
                    name = "新建任务",
                    ProductSides = new() {
                        new() {
                            name = "产品面1",
                        },
                    },
                };
            }
            if (missionId == null) {
                _missionDTO = NewMission();
            } else {
                _missionDTO = apis.QueryProductMissionDetail(new(missionId.Value)).ProductMissionDTO;
                if (_missionDTO == null) {
                    _missionDTO = NewMission();
                }
            }
            // Clear all child controls
            Controls.Clear();
            // Create a new page according to missionbody and show
            if (_editionPage != null) {
                _editionPage.Dispose();
            }
            _editionPage = new(this, _missionDTO);
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
        public class MissionEditionPage_SCII: CustomContentPanel {
            protected ILog logger = MainUtils.GetLogger(typeof(MissionEditionPage_SCII));

            private OperationGuidanceApis _apis;
            private MissionEditionView_SCII _parentView;
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
            private List<ScrewBitCounterDTO> _screwBitCounterDTOs;
            private MissionDetailPopUpForm _detialPopUpForm;
            private CommonButton _buttonSave;
            private CommonButton _buttonNew;
            private CommonButton _buttonDelete;
            private CommonButton _buttonDuplicate;
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
            private readonly float _boltButtonRadiusRatio = .05F;

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

            public MissionEditionPage_SCII(MissionEditionView_SCII parent, ProductMissionDTO missionDTO) : base() {
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

                _buttonsOuter = new() {
                    Parent = _top,
                    Padding = new(0),
                };
                _editDetail = new() {
                    Parent = _buttonsOuter,
                    Label = "任务详情",
                    BlockHoverUp = true,
                };
                _screwBitCounterDTOs = _missionDTO.id > 0
                    ? _apis.FindScrewBitCounterByMissionId(new(_missionDTO.id)).ScrewBitCounterDTOs
                    : new();
                _editDetail.Click += (s, e) => {
                    List<BarCodeMatchingRuleDTO> barCodeMatchingRuleDTOs = _apis.QueryBarCodeMatchingRuleList(new(SystemUtils.MacAddressesDTO.id) { MissionId = _missionDTO.id }).BarCodeMatchingRuleDTOs;
                    List<ProductMissionDTO> allOtherMissions = _apis.QueryProductMissions(new()).ProductMissionsDTOs.Where(m => m.id != _missionDTO.id).ToList();
                    _detialPopUpForm = new(_missionDTO, allOtherMissions, barCodeMatchingRuleDTOs, _screwBitCounterDTOs) {
                        Title = "编辑任务详情",
                    };
                    _detialPopUpForm.MissionName.GetTextBox(0).Box.Select(0, 0);
                    _detialPopUpForm.AddButton("确定").Click += (s, e) => {
                        bool check = true;
                        string warningMsg = "";
                        int warningIndex = 1;

                        string missionName = _detialPopUpForm.MissionName.GetTextBox(0).Box.Text;
                        if (string.IsNullOrEmpty(missionName)) {
                            check = false;
                            _detialPopUpForm.MissionName.GetTextBox(0).IsError = true;
                            warningMsg += $"{warningIndex++}. 任务名称不能为空\r\n";
                        }

                        string maxNGNum = _detialPopUpForm.MaxNGNum.GetTextBox(0).Box.Text;
                        if (string.IsNullOrEmpty(maxNGNum)) {
                            check = false;
                            _detialPopUpForm.MaxNGNum.GetTextBox(0).IsError = true;
                            warningMsg += $"{warningIndex++}. 最大NG数不能为空\r\n";
                        }

                        string passwordNeedTime = _detialPopUpForm.PasswordNeedTime.GetTextBox(0).Box.Text;
                        if (string.IsNullOrEmpty(passwordNeedTime)) {
                            check = false;
                            _detialPopUpForm.PasswordNeedTime.GetTextBox(0).IsError = true;
                            warningMsg += $"{warningIndex++}. 第几次起需密码不能为空\r\n";
                        }

                        int int_maxNGNum = int.Parse(maxNGNum);
                        int int_passwordNeedTime = int.Parse(passwordNeedTime);
                        if (int_maxNGNum > 0 && int_passwordNeedTime >= int_maxNGNum) {
                            check = false;
                            _detialPopUpForm.PasswordNeedTime.GetTextBox(0).IsError = true;
                            warningMsg += $"{warningIndex++}. 第几次起需密码必须小于最大NG数，NG次数达到最大时任务已经失败，无法再弹窗输入管理员密码\r\n";
                        }

                        // Check challenge mission settings
                        if (_detialPopUpForm.IsChallengeMission.Checked) {
                            // Check challenge mission is empty or not
                            if (_detialPopUpForm.ChallengMission.IsDefaultValue()) {
                                check = false;
                                _detialPopUpForm.ChallengMission.SetError(true);
                                warningMsg += $"{warningIndex++}. 挑战任务必须对应一个普通任务\r\n";
                            } else {
                                int selectedMissionId = _detialPopUpForm.ChallengMission.Value;

                                // Check if current selected challenge mission is already a challenge mission
                                if (allOtherMissions.SingleOrDefault(m => m.challenge_mission_id == selectedMissionId) != null) {
                                    check = false;
                                    _detialPopUpForm.ChallengMission.SetError(true);
                                    warningMsg += $"{warningIndex++}. 当前选择的对应普通任务已存在挑战任务\r\n";
                                } else {
                                    ProductMissionDTO selectedMission = allOtherMissions.Single(m => m.id == selectedMissionId);

                                    // Check if setting isFirstMission = true
                                    if (_detialPopUpForm.IsFirstMission.Checked) {
                                        if (selectedMission.predecessor_mission_id != null || selectedMission.predecessor_mission_id > 0) {
                                            check = false;
                                            _detialPopUpForm.ChallengMission.SetError(true);
                                            warningMsg += $"{warningIndex++}. 当前选择的对应普通任务存在前置任务，无法设置成首道岗位\r\n";
                                        }
                                    }
                                }
                            }
                        }

                        // Check part predecessor mission
                        if (_detialPopUpForm.PredecessorPartMissionMaps.Count > 0) {
                            List<int> ids = new();
                            bool flag = false;
                            foreach (PredecessorPartMissionMap map in _detialPopUpForm.PredecessorPartMissionMaps) {
                                if (map.BarCodeRuleId.IsDefaultValue() && !map.MissionId.IsDefaultValue()) {
                                    flag = true;
                                    map.BarCodeRuleId.SetError(true);
                                } else {
                                    map.BarCodeRuleId.SetError(false);
                                }

                                if (!map.BarCodeRuleId.IsDefaultValue() && map.MissionId.IsDefaultValue()) {
                                    flag = true;
                                    map.MissionId.SetError(true);
                                } else {
                                    map.MissionId.SetError(false);
                                }

                                if (map.BarCodeRuleId.IsDefaultValue() && map.MissionId.IsDefaultValue()) {
                                    map.BarCodeRuleId.SetError(false);
                                    map.MissionId.SetError(false);
                                }
                            }

                            if (flag) {
                                check = false;
                                warningMsg += $"{warningIndex++}. 物料前置任务需要选择要限制的物料匹配规则ID\r\n";
                            }

                            List<int> missionIds = _detialPopUpForm.PredecessorPartMissionMaps.Where(map => !map.MissionId.IsDefaultValue()).Select(map => map.MissionId.Value).ToList();
                            if (!_detialPopUpForm.PredecessorMission.IsDefaultValue()) {
                                missionIds.Add(_detialPopUpForm.PredecessorMission.Value);
                            }

                            Dictionary<int, int> idsDict = missionIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
                            List<int> idKeys = idsDict.Where(pair => pair.Value > 1).Select(pair => pair.Key).ToList();
                            if (idKeys.Count > 0) {
                                check = false;
                                _detialPopUpForm.PredecessorPartMissionMaps.ForEach(map => {
                                    if (idKeys.Contains(map.MissionId.Value)) {
                                        map.MissionId.SetError(true);
                                    } else if (!map.MissionId.IsError) {
                                        map.MissionId.SetError(false);
                                    }
                                });
                                if (!_detialPopUpForm.PredecessorMission.IsDefaultValue()) {
                                    _detialPopUpForm.PredecessorMission.SetError(true);
                                }
                                warningMsg += $"{warningIndex++}. 任务的“前置任务”与“物料前置任务”无法选择重复的任务\r\n";
                            } else {
                                if (_detialPopUpForm.IsFirstMission.Checked && !_detialPopUpForm.PredecessorMission.IsDefaultValue()) {
                                    check = false;
                                    _detialPopUpForm.PredecessorMission.SetError(true);
                                    warningMsg += $"{warningIndex++}. 首道岗位挑战任务无法设置前置任务\r\n";
                                } else {
                                    _detialPopUpForm.PredecessorMission.SetError(false);
                                }
                            }

                            List<int> barCodeRuleIds = _detialPopUpForm.PredecessorPartMissionMaps.Where(map => !map.BarCodeRuleId.IsDefaultValue()).Select(map => map.BarCodeRuleId.Value).ToList();
                            Dictionary<int, int> barCodeRulesDict = barCodeRuleIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
                            List<int> ruleKeys = barCodeRulesDict.Where(pair => pair.Value > 1).Select(pair => pair.Key).ToList();
                            if (ruleKeys.Count > 0) {
                                check = false;
                                _detialPopUpForm.PredecessorPartMissionMaps.ForEach(map => {
                                    if (ruleKeys.Contains(map.BarCodeRuleId.Value)) {
                                        map.BarCodeRuleId.SetError(true);
                                    } else if (!map.BarCodeRuleId.IsError) {
                                        map.BarCodeRuleId.SetError(false);
                                    }
                                });
                                warningMsg += $"{warningIndex++}. 物料条码规则不能重复设置\r\n";
                            }
                        }

                        // Check screw bit counter boxes
                        List<CustomTextBox> bitPositions = new();
                        List<CustomTextBox> counters = new();
                        _detialPopUpForm.ScrewBitCounters.ForEach(box => {
                            if (!box.GetTextBox(0).IsEmpty()) {
                                if (int.Parse(box.GetTextBox(0).Box.Text) <= 0) {
                                    bitPositions.Add(box.GetTextBox(0));
                                }

                                if (box.GetTextBox(1).IsEmpty()) {
                                    counters.Add(box.GetTextBox(1));
                                }

                                if (box.GetTextBox(2).IsEmpty()) {
                                    counters.Add(box.GetTextBox(2));
                                }
                            }
                        });
                        if (bitPositions.Count > 0) {
                            check = false;
                            foreach (CustomTextBox box in bitPositions) {
                                box.IsError = true;
                            }
                            warningMsg += $"{warningIndex++}. 套筒位不能小于0\r\n";
                        }
                        if (counters.Count > 0) {
                            check = false;
                            foreach (CustomTextBox box in counters) {
                                box.IsError = true;
                            }
                            warningMsg += $"{warningIndex++}. 套筒位不为空时，批头使用上限及每次任务计数也不能为空\r\n";
                        }

                        // Check if can save
                        if (!check) {
                            WidgetUtils.ShowWarningPopUp($"保存失败：\r\n{warningMsg}");
                        } else {
                            _missionName.SetValue(0, missionName);
                            _missionDTO.name = missionName;
                            _missionDTO.is_challenge_mission = (int) (_detialPopUpForm.IsChallengeMission.Checked ? YesOrNo.YES : YesOrNo.NO);
                            _missionDTO.is_first_mission = (int) (_detialPopUpForm.IsFirstMission.Checked ? YesOrNo.YES : YesOrNo.NO);
                            _missionDTO.challenge_mission_id = _detialPopUpForm.ChallengMission.Value;
                            _missionDTO.max_ng_num = int.Parse(maxNGNum);
                            _missionDTO.password_need_time = int.Parse(passwordNeedTime);
                            if (!_detialPopUpForm.PredecessorMission.IsDefaultValue()) {
                                _missionDTO.predecessor_mission_id = _detialPopUpForm.PredecessorMission.Value;
                            } else {
                                _missionDTO.predecessor_mission_id = null;
                            }
                            Dictionary<int, int> idsDict = new();
                            foreach (PredecessorPartMissionMap map in _detialPopUpForm.PredecessorPartMissionMaps) {
                                if (!map.BarCodeRuleId.IsDefaultValue() && !map.MissionId.IsDefaultValue()) {
                                    idsDict.Add(map.BarCodeRuleId.Value, map.MissionId.Value);
                                }
                            }

                            if (idsDict.Count > 0) {
                                _missionDTO.predecessor_part_mission_ids = JsonConvert.SerializeObject(idsDict);
                            } else {
                                _missionDTO.predecessor_part_mission_ids = null;
                            }

                            _detialPopUpForm.ScrewBitCounters.ForEach(box => {
                                if (!box.GetTextBox(0).IsEmpty()
                                        && !box.GetTextBox(1).IsEmpty()
                                        && !box.GetTextBox(2).IsEmpty()) {
                                    int bitPosition = int.Parse(box.GetTextBox(0).Box.Text);
                                    int maxNum = int.Parse(box.GetTextBox(1).Box.Text);
                                    int countEachTime = int.Parse(box.GetTextBox(2).Box.Text);

                                    ScrewBitCounterDTO? temp = _screwBitCounterDTOs.Find(dto => dto.bit_position == bitPosition);
                                    if (temp == null) {
                                        temp = new() {
                                            mission_id = _missionDTO.id,
                                        };
                                        _screwBitCounterDTOs.Add(temp);
                                    }

                                    temp.bit_position = bitPosition;
                                    temp.max_num = maxNum;
                                    temp.count_each_time = countEachTime;
                                }
                            });
                            ScrewBitCounterDTO? screwBitCounterDTO = _screwBitCounterDTOs.Find(dto =>
                                    _detialPopUpForm.ScrewBitCounters.Find(box =>
                                        !box.GetTextBox(0).IsEmpty() && dto.bit_position == int.Parse(box.GetTextBox(0).Box.Text)) == null);
                            if (screwBitCounterDTO != null) {
                                screwBitCounterDTO.deleted = (int) YesOrNo.YES;
                            }

                            _detialPopUpForm.Hide();

                            // Reset all serial numbers of all bolts
                            _sideButtons.ForEach(side => side.ResetSerialNumbers());
                            ForceResizeRight();
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
                        // Save screw bit counters
                        _screwBitCounterDTOs.ForEach(dto => _apis.AddOrUpdateScrewBitCounter(new(dto)));

                        Modified = false;
                        _missionDTO = rsp.ProductMissionDTO;

                        // 数据保存成功后，保存图片到本地（需要循环保存每一个side的图片）
                        foreach (SideButton sideBtn in _sideButtons) {
                            MainUtils.SaveProductImage(sideBtn.ProductImageFileNew.Image, sideBtn.ProductImageFileNew.ImageFileName);
                            ProductImageCache.Invalidate(sideBtn.ProductImageFileNew.ImageFileName);
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
                                Modified = false;
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

                _buttonDuplicate = new() {
                    Parent = _buttonsOuter,
                    Label = "复制",
                    BlockHoverUp = true,
                };
                _buttonDuplicate.Click += (sender, eventArgs) => {
                    if (_missionDTO.id > 0) {
                        DialogResult result = MessageBox.Show(null, "是否执行复制操作？", "复制任务", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result == DialogResult.Yes) {
                            ProductMissionDTO duplicatedMissionDTO = new() {
                                name = _missionDTO.name + "_copy",
                                pn_code = _missionDTO.pn_code,
                                max_ng_num = _missionDTO.max_ng_num,
                                password_need_time = _missionDTO.password_need_time,
                                enabled = _missionDTO.enabled,
                                predecessor_mission_id = _missionDTO.predecessor_mission_id,
                                predecessor_part_mission_ids = _missionDTO.predecessor_part_mission_ids,
                                multi_device_independence = _missionDTO.multi_device_independence,
                            };

                            if (_missionDTO.ProductSides != null && _missionDTO.ProductSides.Count > 0) {
                                List<ProductSideDTO> duplicatedSideDTOs = new();
                                for (int i = 0; i < _missionDTO.ProductSides.Count; i++) {
                                    ProductSideDTO side = _missionDTO.ProductSides[i];
                                    SideButton sideButton = _sideButtons[i];

                                    // Use new name to avoid affecting original images
                                    string newImageName = MainUtils.GenerateProductImageName();
                                    sideButton.ProductImageFileNew.ImageFileName = newImageName;

                                    ProductSideDTO sideTemp = new() {
                                        name = side.name,
                                        image = newImageName,
                                        max_rectangle_width = side.max_rectangle_width,
                                        max_rectangle_height = side.max_rectangle_height,
                                        max_rectangle_location = side.max_rectangle_location,
                                        center_location = side.center_location,
                                        location_offset = side.location_offset,
                                        location_offset_moving = side.location_offset_moving,
                                        zooming_ratio = side.zooming_ratio,
                                        zooming_ratio_extra = side.zooming_ratio_extra,
                                        rotate_angle = side.rotate_angle,
                                        cropped = side.cropped,
                                    };

                                    if (side.Bolts != null && side.Bolts.Count > 0) {
                                        List<ProductBoltDTO> duplicatedBoltDTOs = new();
                                        side.Bolts.ForEach(bolt => {
                                            duplicatedBoltDTOs.Add(new() {
                                                serial_num = bolt.serial_num,
                                                arranger_id = bolt.arranger_id,
                                                specification = bolt.specification,
                                                arranger_id2 = bolt.arranger_id2,
                                                specification2 = bolt.specification2,
                                                workstation_id = bolt.workstation_id,
                                                workstation_name = bolt.workstation_name,
                                                workstation_description = bolt.workstation_description,
                                                position = bolt.position,
                                                location_x_percent = bolt.location_x_percent,
                                                location_y_percent = bolt.location_y_percent,
                                                setter_selector_id = bolt.setter_selector_id,
                                                bit_specification = bolt.bit_specification,
                                                parameters_set = bolt.parameters_set,
                                                torque_min = bolt.torque_min,
                                                torque_max = bolt.torque_max,
                                                angle_min = bolt.angle_min,
                                                angle_max = bolt.angle_max,
                                                parts_bar_code_ids = bolt.parts_bar_code_ids,
                                                enabled = bolt.enabled,
                                            });
                                        });

                                        sideTemp.Bolts = duplicatedBoltDTOs;
                                    }

                                    duplicatedSideDTOs.Add(sideTemp);
                                }

                                duplicatedMissionDTO.ProductSides = duplicatedSideDTOs;
                            }

                            AddOrUpdateProductMissionReq req = new(duplicatedMissionDTO);
                            AddOrUpdateProductMissionRsp rsp = _apis.AddOrUpdateProductMission(req);
                            if (rsp.RsponseCode == HttpResponseCode.OK) {
                                Modified = false;
                                _missionDTO = rsp.ProductMissionDTO;
                                // 数据复制并保存成功后，保存图片到本地（需要循环保存每一个side的图片）
                                foreach (SideButton sideBtn in _sideButtons) {
                                    MainUtils.SaveProductImage(sideBtn.ProductImageFileNew.Image, sideBtn.ProductImageFileNew.ImageFileName);
                                    ProductImageCache.Invalidate(sideBtn.ProductImageFileNew.ImageFileName);
                                }
                                MessageBox.Show(null, "复制成功！", "复制任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                // 复制成功后跳转至任务列表界面
                                WidgetUtils.GetChildMenu(101).TriggerClick(EventArgs.Empty);
                                Dispose();
                            } else {
                                MessageBox.Show(null, "删除失败！错误信息：" + rsp.RsponseMessage, "删除任务", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    } else {
                        WidgetUtils.ShowNoticePopUp("此任务没有保存至数据库，无法执行复制操作");
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
                ImageButton button = new() {
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
                // Initialize left bottom content panel
                _leftBottomContentPanel.MouseLeave += (sender, eventArgs) => {
                    Cursor = Cursors.Arrow;
                };
                // Click event within left bottom content panel
                _leftBottomContentPanel.SingleClickDelegate += (eventArgs) => {
                    if (_leftBottomContentPanel.CanTriggerClick()) {
                        _currentProductImageFile.ImageSelect(() => Modified = true);
                        _currentSideButton.ProductImageFile = _currentProductImageFile.Copy();
                    }
                };
                // Double click event within left bottom content panel
                _leftBottomContentPanel.DoubleClickDelegate += (eventArgs) => {
                    if (!_leftBottomContentPanel.CanTriggerClick()) {
                        // 检查sideDTO是否为空，如果为空则抛出异常，因为在这里不能为空
                        ProductSideDTO sideDTO = CommonUtils.CannotBeNull(_currentSideButton.SideDTO);
                        // Check if image cropped
                        if (!_currentProductImageFile.Cropped) {
                            WidgetUtils.ShowWarningPopUp("产品图片需要裁剪过后才可进行点位增加操作（裁剪是为了将图片当前位置存入数据库）");
                            return;
                        }

                        ProductBoltDTO boltDTO = new() {
                            side_id = sideDTO.id,
                        };
                        // Calculate the location of new bolt
                        Rectangle maxRect = _leftBottomContentPanel.MaxRect;
                        boltDTO.location_x_percent = (float) (eventArgs.Location.X - maxRect.X) / _leftBottomContentPanel.MaxRectWidth * 100;
                        boltDTO.location_y_percent = (float) (eventArgs.Location.Y - maxRect.Y) / _leftBottomContentPanel.MaxRectHeight * 100;
                        // Set serial number, if deleted serial number(s) exit(s), dequeue a serial number from queue and use it
                        int serialNumTemp = 0;

                        foreach (List<BoltButton> btnList in _currentSideButton.BoltButtons.Values) {
                            if (btnList.Count > 0) {
                                serialNumTemp = Math.Max(serialNumTemp, btnList.Select(btn => btn.BoltDTO.serial_num).Max());
                            }
                        }
                        boltDTO.serial_num = serialNumTemp + 1;
                        boltDTO.name = $"BOLT_" + boltDTO.serial_num;
                        OpenNewBoltPopUpForm(boltDTO, () => {
                            // Add new buttons
                            BoltButton boltButton = AddNewBoltButton(_currentSideButton, boltDTO, true);
                            BoltEditionButton boltEditionButton = _rightContentPanel.AddNewBoltEditionButton(_currentSideButton, boltDTO, OpenBoltPopUpForm);
                            boltEditionButton.Deleted += () => ForceResizeRight();

                            // Save serial num
                            int workstationId = boltDTO.workstation_id;

                            // Add buttons into side button
                            if (!_currentSideButton.BoltButtons.ContainsKey(workstationId)) {
                                _currentSideButton.BoltButtons.Add(workstationId, new());
                                _currentSideButton.BoltEditionButtons.Add(workstationId, new());
                            }
                            _currentSideButton.BoltButtons[workstationId].Add(boltButton);
                            _currentSideButton.BoltEditionButtons[workstationId].Add(boltEditionButton);

                            // Do this to force fire SizeChanged event
                            ResizeBottomLeft();
                            ForceResizeRight();

                            // Save new boltDto to sideDto
                            if (sideDTO.Bolts == null) {
                                sideDTO.Bolts = new();
                            }
                            sideDTO.Bolts.Add(boltDTO);
                        }, null);
                    }
                };

                // Initialize variables
                _mouseLeftDown = false;
                _controlDown = false;
                _needSaveBuffer = false;

                // Make left bottom content panel can be auto focus
                EventFuncs.AddClickActivateControl(_leftBottomContentPanel);

                // Other events
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
            }

            private SideButton NewSideButton(ProductSideDTO sideDTO) {
                ProductImageFile productImageFile = new(_leftBottomContentPanel, sideDTO, _imageOperationBufferLength);
                ProductImageFile productImageFileNew = new(_leftBottomContentPanel, sideDTO, _imageOperationBufferLength);

                // Initialzie side button
                SideButton sideButton = new(_missionDTO, sideDTO, _leftBottomContentPanel, productImageFile, productImageFileNew, (sideId, visible) => {
                    if (_rightContentPanel != null && _rightContentPanel.Panels.ContainsKey(sideId)) {
                        _rightContentPanel.Panels[sideId].Visible = visible;
                    }
                }) {
                    Parent = _sideTitlePanel,
                    BackColor = Color.Transparent,
                    ForeColor = ColorConfigs.COLOR_MISSION_EDITION_TEXT,
                    ToggleBarColor = ColorConfigs.COLOR_MISSION_EDITION_IMAGE_SIDE_BUTTON_TOGGLED,
                    BoltButtonRadius = (int) (_leftBottomContentPanel.MaxRectHeight * _boltButtonRadiusRatio),
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
                        int workstationId = boltDTO.workstation_id;
                        if (!sideButton.BoltButtons.ContainsKey(workstationId)) {
                            sideButton.BoltButtons.Add(workstationId, new());
                        }
                        sideButton.BoltButtons[workstationId].Add(AddNewBoltButton(sideButton, boltDTO));
                    }
                }
                return sideButton;
            }

            private BoltButton AddNewBoltButton(SideButton sideButton, ProductBoltDTO boltDTO, bool visible = false) {
                BoltButton boltButton = new(boltDTO) {
                    Parent = _leftBottomContentPanel,
                    Visible = visible,
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
                        boltDTO.location_x_percent = (float) (location.X - maxRect.X + _currentSideButton.BoltButtonRadius) / _leftBottomContentPanel.MaxRectWidth * 100;
                        boltDTO.location_y_percent = (float) (location.Y - maxRect.Y + _currentSideButton.BoltButtonRadius) / _leftBottomContentPanel.MaxRectHeight * 100;

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
                            sideButton.CurrentWorkstationId = boltDTO.workstation_id;
                            OpenBoltPopUpForm(boltDTO);
                        }
                    }
                    Cursor.Show();
                };
                return boltButton;
            }

            private void OpenNewBoltPopUpForm(ProductBoltDTO boltDTO, Action? addNewBoltBtns, Action? cancelToAdd) {
                bool added = false;
                List<BarCodeMatchingRuleDTO> barCodeMatchingRuleDTOs = _apis.QueryBarCodeMatchingRuleList(new(SystemUtils.MacAddressesDTO.id) { MissionId = _missionDTO.id }).BarCodeMatchingRuleDTOs;
                _boltPopUpForm = new BoltEditionPopUpForm_SCII(boltDTO, barCodeMatchingRuleDTOs) {
                    Title = $"螺栓点位 - {boltDTO.name}",
                    BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
                    MaxContentHeight = WidgetUtils.PopUpOrFloatingFormMaxHeight(),
                };
                _boltPopUpForm.HandleDestroyed += (s, e) => {
                    if (!added && cancelToAdd != null) {
                        cancelToAdd();
                    }
                };
                // 添加按钮
                CommonButton confirmButton = _boltPopUpForm.AddButton("确定信息");
                confirmButton.Click += (s, e) => {
                    if (saveBoltInfo(boltDTO)) {
                        if (addNewBoltBtns != null) {
                            addNewBoltBtns();
                        }
                        added = true;
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
                BoltEditionPopUpForm_SCII popUpForm = (BoltEditionPopUpForm_SCII) _boltPopUpForm;

                bool check = true;
                string warningMsg = "";
                int warningIndex = 1;
                string serialNum = popUpForm.SerialNumBox.GetTextBox(0).Box.Text;
                if (string.IsNullOrEmpty(serialNum) || int.Parse(serialNum) <= 0) {
                    check = false;
                    popUpForm.SerialNumBox.GetTextBox(0).IsError = true;
                    warningMsg += $"{warningIndex++}. 点位编号不能为空\r\n";
                }
                if (popUpForm.Workstation.Value == null) {
                    check = false;
                    popUpForm.Workstation.SetError(true);
                    warningMsg += $"{warningIndex++}. 站点不能为空\r\n";
                } else {
                    foreach (KeyValuePair<int, List<BoltButton>> pair in _currentSideButton.BoltButtons) {
                        BoltButton? boltButton = pair.Value.Find(btn => btn.BoltDTO.serial_num == int.Parse(serialNum));
                        if (boltButton != null && boltButton.BoltDTO.id != boltDTO.id) {
                            check = false;
                            popUpForm.SerialNumBox.GetTextBox(0).IsError = true;
                            warningMsg += $"{warningIndex++}. 存在重复的点位编号\r\n";
                            break;
                        }
                    }
                }
                if (MainUtils.IsArmLocatingEnabled() && !popUpForm.PositionToggle.Checked) {
                    check = false;
                    warningMsg += $"{warningIndex++}. 已开启【力臂定位】，必须配置点位坐标\r\n";
                }
                if (popUpForm.PositionToggle.Checked) {
                    string x = popUpForm.PositionBox.GetTextBox(0).Box.Text;
                    string y = popUpForm.PositionBox.GetTextBox(1).Box.Text;
                    string z = popUpForm.PositionBox.GetTextBox(2).Box.Text;
                    if (string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y) || string.IsNullOrEmpty(z)) {
                        check = false;
                        popUpForm.PositionBox.GetTextBox(0).IsError = true;
                        popUpForm.PositionBox.GetTextBox(1).IsError = true;
                        popUpForm.PositionBox.GetTextBox(2).IsError = true;
                        warningMsg += $"{warningIndex++}. 点位坐标字段开启后，不能为空\r\n";
                    }
                } else {
                    boltDTO.position = null;
                }
                if (popUpForm.ParameterSetToggle.Checked) {
                    string pset = popUpForm.ParameterSetBox.GetTextBox(0).Box.Text;
                    if (string.IsNullOrEmpty(pset) || int.Parse(pset) <= 0) {
                        check = false;
                        popUpForm.ParameterSetBox.GetTextBox(0).IsError = true;
                        warningMsg += $"{warningIndex++}. 程序号字段开启后，不能为空且必须大于0\r\n";
                    }
                } else {
                    boltDTO.parameters_set = null;
                }
                if (popUpForm.SpecificationToggle.Checked) {
                    // Check arrangers
                    DeviceIoDTO? ioDTO = popUpForm.ArrangerType.Value;
                    DeviceIoDTO? ioDTO2 = popUpForm.ArrangerType2.Value;
                    if (ioDTO == null && ioDTO2 == null) {
                        check = false;
                        popUpForm.ArrangerType.SetError(true);
                        popUpForm.ArrangerType2.SetError(true);
                        warningMsg += $"{warningIndex++}. 螺钉序号字段开启后，排列机组至少填一个\r\n";
                    }

                    // Check specifications
                    string specification = popUpForm.SpecificationBox.GetTextBox(0).Box.Text;
                    string specification2 = popUpForm.SpecificationBox2.GetTextBox(0).Box.Text;
                    bool specificationIsNull = (string.IsNullOrEmpty(specification) || int.Parse(specification) <= 0);
                    bool specificationIsNull2 = (string.IsNullOrEmpty(specification2) || int.Parse(specification2) <= 0);
                    if (specificationIsNull && specificationIsNull2) {
                        check = false;
                        popUpForm.SpecificationBox.GetTextBox(0).IsError = true;
                        popUpForm.SpecificationBox2.GetTextBox(0).IsError = true;
                        warningMsg += $"{warningIndex++}. 螺钉序号字段开启后，螺钉序号至少有一个不能为空且必须大于0\r\n";
                    }

                    // Check if them matches
                    if (ioDTO != null && specificationIsNull) {
                        check = false;
                        popUpForm.SpecificationBox.GetTextBox(0).IsError = true;
                        warningMsg += $"{warningIndex++}. 排列机组1有选中的值时，螺钉序号1不能为空且必须大于0\r\n";
                    }
                    if (ioDTO2 != null && specificationIsNull2) {
                        check = false;
                        popUpForm.SpecificationBox2.GetTextBox(0).IsError = true;
                        warningMsg += $"{warningIndex++}. 排列机组2有选中的值时，螺钉序号2不能为空且必须大于0\r\n";
                    }
                    if (ioDTO == null && !specificationIsNull) {
                        check = false;
                        popUpForm.ArrangerType.SetError(true);
                        warningMsg += $"{warningIndex++}. 螺钉序号1不为空且大于0时，排列机组1不能为空\r\n";
                    }
                    if (ioDTO2 == null && !specificationIsNull2) {
                        check = false;
                        popUpForm.ArrangerType2.SetError(true);
                        warningMsg += $"{warningIndex++}. 螺钉序号2不为空且大于0时，排列机组2不能为空\r\n";
                    }
                    if (ioDTO != null && ioDTO2 != null && ioDTO.id == ioDTO2.id && specification == specification2) {
                        check = false;
                        popUpForm.SpecificationBox.GetTextBox(0).IsError = true;
                        popUpForm.SpecificationBox2.GetTextBox(0).IsError = true;
                        warningMsg += $"{warningIndex++}. 选择同一个排列机组时，螺钉序号不能重复\r\n";
                    }

                    if (check) {
                        popUpForm.ArrangerType.SetError(false);
                        popUpForm.ArrangerType2.SetError(false);
                        popUpForm.SpecificationBox.GetTextBox(0).IsError = false;
                        popUpForm.SpecificationBox2.GetTextBox(0).IsError = false;
                    }
                } else {
                    boltDTO.arranger_id = null;
                    boltDTO.arranger_id2 = null;
                    boltDTO.specification = null;
                    boltDTO.specification2 = null;
                }
                if (popUpForm.BitSpecificationToggle.Checked) {
                    DeviceIoDTO? ioDTO = popUpForm.SetterSelectorType.Value;
                    if (ioDTO == null) {
                        check = false;
                        popUpForm.SetterSelectorType.SetError(true);
                        warningMsg += $"{warningIndex++}. 套筒位数字段开启后，套筒选择器不能为空\r\n";
                    }
                    string bitSpecification = popUpForm.BitSpecificationBox.GetTextBox(0).Box.Text;
                    if (string.IsNullOrEmpty(bitSpecification) || int.Parse(bitSpecification) <= 0) {
                        check = false;
                        popUpForm.BitSpecificationBox.GetTextBox(0).IsError = true;
                        warningMsg += $"{warningIndex++}. 套筒位数字段开启后，不能为空且必须大于0\r\n";
                    }
                } else {
                    boltDTO.setter_selector_id = null;
                    boltDTO.bit_specification = null;
                }
                if (popUpForm.TorqueToggle.Checked) {
                    string torqueMin = popUpForm.TorqueBox.GetTextBox(0).Box.Text;
                    string torqueMax = popUpForm.TorqueBox.GetTextBox(1).Box.Text;
                    if (string.IsNullOrEmpty(torqueMin) || string.IsNullOrEmpty(torqueMax)) {
                        check = false;
                        popUpForm.TorqueBox.GetTextBox(0).IsError = true;
                        popUpForm.TorqueBox.GetTextBox(1).IsError = true;
                        warningMsg += $"{warningIndex++}. 扭矩上下限字段开启后，不能为空\r\n";
                    }
                } else {
                    boltDTO.torque_min = null;
                    boltDTO.torque_max = null;
                }
                if (popUpForm.AngleToggle.Checked) {
                    string AngleMin = popUpForm.AngleBox.GetTextBox(0).Box.Text;
                    string AngleMax = popUpForm.AngleBox.GetTextBox(1).Box.Text;
                    if (string.IsNullOrEmpty(AngleMin) || string.IsNullOrEmpty(AngleMax)) {
                        check = false;
                        popUpForm.AngleBox.GetTextBox(0).IsError = true;
                        popUpForm.AngleBox.GetTextBox(1).IsError = true;
                        warningMsg += $"{warningIndex++}. 扭矩上下限不能为空\r\n";
                    }
                } else {
                    boltDTO.angle_min = null;
                    boltDTO.angle_max = null;
                }

                // Check for parts bar code bindings
                if (popUpForm.PartsBarCodesToggle.Checked) {
                    List<int> ids = new();
                    List<CustomComboBoxGroup<int>> idBoxes = popUpForm.PartsBarCodeIdBoxes;
                    for (int i = 0; i < idBoxes.Count; i++) {
                        CustomComboBoxGroup<int> combo = idBoxes[i];
                        if (!combo.IsDefaultValue() && combo.Value > 0) {
                            if (ids.IndexOf(combo.Value) >= 0) {
                                check = false;
                                warningMsg += $"{warningIndex++}. 存在重复的物条码匹配规则ID\r\n";
                                break;
                            }
                            ids.Add(combo.Value);
                        }
                    }
                } else {
                    popUpForm.ModifiedBoltDTO.parts_bar_code_ids = null;
                }


                if (!check) {
                    WidgetUtils.ShowWarningPopUp($"信息暂存失败：\r\n{warningMsg}");
                } else {
                    // 根据校验结果判断可以保存
                    Modified = true;
                    popUpForm.SaveTo(boltDTO);
                    WidgetUtils.ShowNoticePopUp("信息暂存成功！");
                    popUpForm.Dispose();
                }

                return check;
            }

            private void OpenBoltPopUpForm(ProductBoltDTO boltDTO) {
                List<BarCodeMatchingRuleDTO> barCodeMatchingRuleDTOs = _apis.QueryBarCodeMatchingRuleList(new(SystemUtils.MacAddressesDTO.id) { MissionId = _missionDTO.id }).BarCodeMatchingRuleDTOs;
                _boltPopUpForm = new BoltEditionPopUpForm_SCII(boltDTO, barCodeMatchingRuleDTOs) {
                    Title = boltDTO.serial_num + " - " + boltDTO.name,
                    BorderColor = ColorConfigs.COLOR_POP_UP_BORDER,
                    MaxContentHeight = WidgetUtils.PopUpOrFloatingFormMaxHeight(),
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
                _rightContentPanel = new() {
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
                    foreach (KeyValuePair<int, List<BoltButton>> pair in sideButton.BoltButtons) {
                        List<BoltEditionButton> boltEditionBtns = new();
                        foreach (BoltButton boltBtns in pair.Value) {
                            BoltEditionButton boltEditionButton = _rightContentPanel.AddNewBoltEditionButton(sideButton, boltBtns.BoltDTO, OpenBoltPopUpForm);
                            boltEditionButton.Deleted += () => ForceResizeRight();
                            boltEditionBtns.Add(boltEditionButton);
                        }
                        sideButton.BoltEditionButtons.Add(pair.Key, boltEditionBtns);
                    }
                }
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
                    // WARN: have to figure out why this happen
                    return;
                }
                if (Parent != null && Parent.IsHandleCreated) {
                    CustomVScrollingContentPanel? outerVScrollPanel = ((MissionEditionView_SCII) Parent).OuterVScrollPanel;
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
                int topHeight = (int) (Height * .1625);
                int bottomHeight = Height - topHeight;
                int bottomLeftWidth = (int) (Width * .8);

                _top.Size = new(_bottom.Width, topHeight);

                _bottom.Size = new(Width, bottomHeight);
                _bottomLeft.Size = new(bottomLeftWidth, bottomHeight);

                _bottomRight.Size = new(Width - bottomLeftWidth - (outerPadding.Left - 1) / 2, bottomHeight);
                _bottomRight.Margin = new((outerPadding.Left - 1) / 2, 0, 0, 0);
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
            }

            private void ResizeTop() {
                // Recalculate some variables
                int textBoxWidth = (int) (_top.Width * .275);
                int textBoxHeight = WidgetUtils.TextOrComboBoxHeight();
                int boxGap = (int) (textBoxHeight * .5);
                int buttonsHeight = WidgetUtils.CommonButtonHeight();
                int buttonGap = (int) (buttonsHeight * .5);

                // Resize mission name box
                _missionName.Size = new(textBoxWidth, textBoxHeight);
                // _missionPnCode.Size = new(textBoxWidth, textBoxHeight);
                // _missionPnCode.Margin = new(boxGap, 0, 0, 0);

                // Resize common buttons
                _buttonsOuter.Size = new(_top.Width - textBoxWidth, buttonsHeight);
                foreach (Control c in _buttonsOuter.Controls) {
                    if (c is CommonButton btn) {
                        btn.Height = buttonsHeight;
                        // 先设置高度获得自动调整的字体大小
                        int newWidth = WidgetUtils.MeasureString(btn.Label, btn.Font).Width + buttonsHeight * 2;
                        btn.Width = newWidth;
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
                Image? productImage = _leftBottomContentPanel.ProductImage;
                Point? imageLocation = _leftBottomContentPanel.ImageLocation;

                // Resize bolt buttons
                int boltButtonRadius = (int) (_leftBottomContentPanel.MaxRectHeight * _boltButtonRadiusRatio);
                foreach (SideButton sideButton in _sideButtons) {
                    sideButton.BoltButtonRadius = boltButtonRadius;
                    sideButton.ReCalculateProductImageRatio();
                    foreach (KeyValuePair<int, List<BoltButton>> pair in sideButton.BoltButtons) {
                        foreach (BoltButton boltButton in pair.Value) {
                            boltButton.Size = new(boltButtonRadius * 2, boltButtonRadius * 2);
                            // Recalculate bolt button location
                            int newX;
                            int newY;
                            if (productImage != null && imageLocation != null) {
                                newX = imageLocation.Value.X + (int) (productImage.Width * boltButton.BoltDTO.location_x_percent / 100) - boltButtonRadius;
                                newY = imageLocation.Value.Y + (int) (productImage.Height * boltButton.BoltDTO.location_y_percent / 100) - boltButtonRadius;
                            } else {
                                newX = _leftBottomContentPanel.MaxRectLocation.X + (int) (_leftBottomContentPanel.MaxRectWidth * boltButton.BoltDTO.location_x_percent / 100) - boltButtonRadius;
                                newY = _leftBottomContentPanel.MaxRectLocation.Y + (int) (_leftBottomContentPanel.MaxRectHeight * boltButton.BoltDTO.location_y_percent / 100) - boltButtonRadius;
                            }
                            boltButton.Location = new(newX, newY);
                        }
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
                _boltTitleLabel.Font = new Font(WidgetsConfigs.SystemFontFamily, _boltTitleLabel.Height * .425F, FontStyle.Bold, GraphicsUnit.Pixel);

                int contentHeight = _bottomRight.Height - _boltTitlePanel.Height - 2;
                int boltBtnHeight = (int) (contentHeight * .055);
                int boltBtnMargin = boltBtnHeight / 7;
                _rightContentPanel.BoltSize = new(controlWidth - boltBtnMargin * 2, boltBtnHeight);
                _rightContentPanel.BoltMargin = boltBtnMargin;

                if (_currentSideButton != null && _currentSideButton.SideDTO != null) {
                    _rightContentPanel.CalNewHeightAdnResizeChildren(_currentSideButton.SideDTO.id, contentHeight);
                }

                _autoScrollContentOuterPanel.Size = new(controlWidth, contentHeight);
            }

            private void ForceResizeRight() {
                _autoScrollContentOuterPanel.Width -= 1;
                ResizeBottomRight();
            }
        }

        public class MissionDetailPopUpForm: CustomPopUpForm {
            private readonly int _columnCount = 2;
            private readonly double _boxRatioOneLine = 7.9;
            private readonly double _boxRatio = 5.75;
            private readonly int _screwBitCounterMax = 10;
            private ProductMissionDTO _missionDTO;
            private TableLayoutPanel _tablePanel;
            private CustomTextBoxGroup _missionName;
            private ToggleButtonGroup _isChallengeMission;
            private ToggleButtonGroup _isFirstMission;
            private CustomComboBoxGroup<int> _challengMission;
            private CustomTextBoxGroup _maxNGNum;
            private CustomTextBoxGroup _passwordNeedTime;
            private CustomTextBoxGroup _productsBarCodeNum;
            private CustomComboBoxGroup<int> _predecessorMission;
            private CustomTextBoxGroup _partsBarCodeNum;
            private List<ProductMissionDTO> _allOtherMissions;
            private List<BarCodeMatchingRuleDTO> _barCodeMatchingRuleDTOs;
            private List<ScrewBitCounterDTO> _screwBitCounterDTOs;
            private List<PredecessorPartMissionMap> _predecessorPartMissionMaps;
            private List<CustomTextBoxButtonGroup> _screwBitCounters;

            public TableLayoutPanel TablePanel { get => _tablePanel; set => _tablePanel = value; }
            public ProductMissionDTO MissionDTO { get => _missionDTO; set => _missionDTO = value; }
            public CustomTextBoxGroup MissionName { get => _missionName; set => _missionName = value; }
            public CustomTextBoxGroup MaxNGNum { get => _maxNGNum; set => _maxNGNum = value; }
            public CustomTextBoxGroup PasswordNeedTime { get => _passwordNeedTime; set => _passwordNeedTime = value; }
            public CustomTextBoxGroup ProductsBarCodeNum { get => _productsBarCodeNum; set => _productsBarCodeNum = value; }
            public CustomComboBoxGroup<int> PredecessorMission { get => _predecessorMission; set => _predecessorMission = value; }
            public CustomTextBoxGroup PartsBarCodeNum { get => _partsBarCodeNum; set => _partsBarCodeNum = value; }
            public List<PredecessorPartMissionMap> PredecessorPartMissionMaps { get => _predecessorPartMissionMaps; set => _predecessorPartMissionMaps = value; }
            public List<CustomTextBoxButtonGroup> ScrewBitCounters { get => _screwBitCounters; set => _screwBitCounters = value; }
            public ToggleButtonGroup IsChallengeMission { get => _isChallengeMission; set => _isChallengeMission = value; }
            public ToggleButtonGroup IsFirstMission { get => _isFirstMission; set => _isFirstMission = value; }
            public CustomComboBoxGroup<int> ChallengMission { get => _challengMission; set => _challengMission = value; }
            public MissionDetailPopUpForm(ProductMissionDTO missionDTO, List<ProductMissionDTO> allOtherMissions, List<BarCodeMatchingRuleDTO> barCodeMatchingRuleDTOs, List<ScrewBitCounterDTO> screwBitCounterDTOs) {
                _missionDTO = missionDTO;
                _allOtherMissions = allOtherMissions;
                _barCodeMatchingRuleDTOs = barCodeMatchingRuleDTOs;
                _screwBitCounterDTOs = screwBitCounterDTOs;
                _tablePanel = new() {
                    Parent = ContentPanel,
                    ColumnCount = _columnCount,
                };

                _missionName = new("任务名称") {
                    Parent = _tablePanel,
                    Ratio = _boxRatioOneLine,
                    NameAlignment = HorizontalAlignment.Right,
                };
                _isChallengeMission = new("是否挑战任务") {
                    Parent = _tablePanel,
                    Ratio = _boxRatio,
                    NameAlignment = HorizontalAlignment.Right,
                };
                _isFirstMission = new("是否首道岗位") {
                    Parent = _tablePanel,
                    Ratio = _boxRatio,
                    NameAlignment = HorizontalAlignment.Right,
                    Enabled = false,
                };
                _challengMission = new("挑战对应任务") {
                    Parent = _tablePanel,
                    Ratio = _boxRatioOneLine,
                    NameAlignment = HorizontalAlignment.Right,
                    Enabled = false,
                };
                _maxNGNum = new("最大NG数") {
                    Parent = _tablePanel,
                    Ratio = _boxRatio,
                    NameAlignment = HorizontalAlignment.Right,
                    PositiveIntOnly = true,
                };
                _passwordNeedTime = new("第几次起需密码") {
                    Parent = _tablePanel,
                    Ratio = _boxRatio,
                    NameAlignment = HorizontalAlignment.Right,
                    PositiveIntOnly = true,
                };
                _productsBarCodeNum = new("产品条码") {
                    Parent = _tablePanel,
                    Ratio = _boxRatioOneLine,
                    NameAlignment = HorizontalAlignment.Right,
                    Enabled = false,
                };
                _predecessorMission = new("前置任务") {
                    Parent = _tablePanel,
                    Ratio = _boxRatioOneLine,
                    NameAlignment = HorizontalAlignment.Right,
                };

                _allOtherMissions.ForEach(m => {
                    _challengMission.AddItem(m.name, m.id);
                    _predecessorMission.AddItem(m.name, m.id);
                });
                _isChallengeMission.CheckedChanged += (s, e) => {
                    if (_isChallengeMission.Checked) {
                        _isFirstMission.Enabled = true;
                        _challengMission.Enabled = true;

                        _isFirstMission.Checked = _missionDTO.is_first_mission == (int) YesOrNo.YES;
                        if (_missionDTO.challenge_mission_id != null) {
                            _challengMission.SetCurrent(_challengMission.IndexOf(_missionDTO.challenge_mission_id.Value));
                        }
                    } else {
                        _isFirstMission.Checked = false;
                        _isFirstMission.Enabled = false;
                        _challengMission.Reset();
                        _challengMission.Enabled = false;
                    }
                    _isFirstMission.Invalidate();
                    _challengMission.Invalidate();
                };
                _challengMission.ItemSelected += () => _challengMission.SetError(false);
                _predecessorMission.ItemSelected += () => _predecessorMission.SetError(false);
                _partsBarCodeNum = new("物料条码") {
                    Parent = _tablePanel,
                    Ratio = _boxRatioOneLine,
                    NameAlignment = HorizontalAlignment.Right,
                    Enabled = false,
                };
                int productsBarCodeNum = 0;
                int partsBarCodeNum = 0;
                List<int> partBarCodeIds = new();
                foreach (BarCodeMatchingRuleDTO rule in _barCodeMatchingRuleDTOs) {
                    if (rule.type == BarCodeTypes.PRODUCT.Id) {
                        productsBarCodeNum++;
                    } else {
                        partsBarCodeNum++;
                        partBarCodeIds.Add(rule.id);
                    }
                }

                // Show combo box if product bar code is set
                if (productsBarCodeNum > 0) {
                    _predecessorMission.Show();
                    _productsBarCodeNum.SetValue(0, "已配置");
                } else {
                    _predecessorMission.Hide();
                    _productsBarCodeNum.SetValue(0, "未配置");
                }
                // Show same number of combo boxes as the numbers of part bar code
                _predecessorPartMissionMaps = new();
                if (partsBarCodeNum > 0) {
                    _partsBarCodeNum.SetValue(0, $"已配置{partsBarCodeNum}个");
                    AddPredecessorPartMissions(partBarCodeIds);

                    if (_missionDTO.predecessor_part_mission_ids != null) {
                        Dictionary<int, int>? idsDict = JsonConvert.DeserializeObject<Dictionary<int, int>>(_missionDTO.predecessor_part_mission_ids);
                        if (idsDict != null) {
                            int index = 0;
                            int errorIndex = 1;
                            string barCodeRuleOrMissionIdNotFound = "";
                            foreach (KeyValuePair<int, int> pair in idsDict) {
                                CustomComboBoxGroup<int> barCodeRuleId = _predecessorPartMissionMaps[index].BarCodeRuleId;
                                int ruleIdIndex = barCodeRuleId.IndexOf(pair.Key);
                                if (ruleIdIndex >= 0) {
                                    barCodeRuleId.SetCurrent(ruleIdIndex);
                                } else {
                                    barCodeRuleId.SetError(true);
                                    barCodeRuleOrMissionIdNotFound += $"{errorIndex++}. 条码匹配规则ID[{pair.Key}]，不存在或被删除\r\n";
                                }

                                CustomComboBoxGroup<int> missionId = _predecessorPartMissionMaps[index].MissionId;
                                int missionIdIndex = missionId.IndexOf(pair.Value);
                                if (missionIdIndex >= 0) {
                                    missionId.SetCurrent(missionIdIndex);
                                } else {
                                    missionId.SetError(true);
                                    barCodeRuleOrMissionIdNotFound += $"{errorIndex++}. 条码匹配规则ID[{pair.Key}]对应的任务不存在或被删除\r\n";
                                }

                                index++;
                            }

                            ShowWarningIfHasAsync(barCodeRuleOrMissionIdNotFound);
                        }
                    }
                } else {
                    _partsBarCodeNum.SetValue(0, "未配置");
                }

                // This declaration should be placed here then we can have correct ordered boxes
                _screwBitCounters = new() {
                    new("批头计数器1") {
                        Parent = _tablePanel,
                        Ratio = _boxRatioOneLine,
                        NameAlignment = HorizontalAlignment.Right,
                        PositiveIntOnly = true,
                        Separator = "->",
                    },
                };
                _screwBitCounters[0].AddTextBox();
                _screwBitCounters[0].AddTextBox();
                _screwBitCounters[0].SetDefaultText(0, "套筒位");
                _screwBitCounters[0].SetDefaultText(1, "批头使用上限");
                _screwBitCounters[0].SetDefaultText(2, "单次计数量");
                _screwBitCounters[0].GetTextBox(0).Box.TextChanged += (s, e) => NotErrorIfNotEmpty(_screwBitCounters[0].GetTextBox(0));
                _screwBitCounters[0].GetTextBox(1).Box.TextChanged += (s, e) => NotErrorIfNotEmpty(_screwBitCounters[0].GetTextBox(1));
                _screwBitCounters[0].GetTextBox(2).Box.TextChanged += (s, e) => NotErrorIfNotEmpty(_screwBitCounters[0].GetTextBox(2));
                SignButton addButton = _screwBitCounters[0].AddButton<SignButton>();
                addButton.Icon = Properties.Resources.sign_plus;
                addButton.Click += (s, e) => AddScrewBitCounter();

                _tablePanel.SetColumnSpan(_missionName, _columnCount);
                _tablePanel.SetColumnSpan(_challengMission, _columnCount);
                _tablePanel.SetColumnSpan(_productsBarCodeNum, _columnCount);
                _tablePanel.SetColumnSpan(_predecessorMission, _columnCount);
                _tablePanel.SetColumnSpan(_partsBarCodeNum, _columnCount);
                _tablePanel.SetColumnSpan(_screwBitCounters[0], _columnCount);

                async void ShowWarningIfHasAsync(string errorMsg) {
                    if (!string.IsNullOrEmpty(errorMsg)) {
                        await Task.Run(() => {
                            WidgetUtils.MainForm.BeginInvoke(async () => {
                                await Task.Delay(500);
                                WidgetUtils.ShowWarningPopUp($"物料前置任务配置有误：\r\n{errorMsg}");
                            });
                        });
                    }
                }
            }

            protected override void AfterShown() {
                _missionName.SetValue(0, _missionDTO.name);
                _isChallengeMission.Checked = _missionDTO.is_challenge_mission == (int) YesOrNo.YES;
                _maxNGNum.SetValue(0, _missionDTO.max_ng_num + "");
                _passwordNeedTime.SetValue(0, _missionDTO.password_need_time + "");
                if (_missionDTO.predecessor_mission_id != null) {
                    _predecessorMission.SetCurrent(_predecessorMission.IndexOf(_missionDTO.predecessor_mission_id.Value));
                }

                int countToAdd = Math.Min(_screwBitCounterDTOs.Count - 1, _screwBitCounterMax - 1);
                for (int i = 0; i < countToAdd; i++) {
                    AddScrewBitCounter();
                }

                // Data backfill
                int backfillCount = Math.Min(_screwBitCounterDTOs.Count, _screwBitCounters.Count);
                for (int i = 0; i < backfillCount; i++) {
                    ScrewBitCounterDTO sbc = _screwBitCounterDTOs[i];
                    _screwBitCounters[i].SetValue(0, sbc.bit_position + "");
                    _screwBitCounters[i].SetValue(1, sbc.max_num + "");
                    _screwBitCounters[i].SetValue(2, sbc.count_each_time + "");
                }
            }

            public void AddPredecessorPartMissions(List<int> partBarCodeIds) {
                for (int i = 0; i < partBarCodeIds.Count; i++) {

                    CustomComboBoxGroup<int> idBox = new("条码规则ID" + (i + 1)) {
                        Parent = _tablePanel,
                        Ratio = _boxRatio,
                        NameAlignment = HorizontalAlignment.Right,
                    };
                    partBarCodeIds.ForEach(id => idBox.AddItem(id + "", id));

                    CustomComboBoxGroup<int> missionBox = new("前置物料任务" + (i + 1)) {
                        Parent = _tablePanel,
                        Ratio = _boxRatio,
                        NameAlignment = HorizontalAlignment.Right,
                    };
                    _allOtherMissions.ForEach(m => missionBox.AddItem(m.name, m.id));

                    _predecessorPartMissionMaps.Add(new(i, idBox, missionBox));
                }
            }

            private void AddScrewBitCounter() {
                int currentCount = _screwBitCounters.Count;
                if (currentCount >= _screwBitCounterMax) {
                    WidgetUtils.ShowWarningPopUp($"批头计数器每个任务最多支持配置{_screwBitCounterMax}个");
                    return;
                }

                CustomTextBoxButtonGroup box = new($"批头计数器{currentCount + 1}") {
                    Parent = _tablePanel,
                    Ratio = _boxRatioOneLine,
                    NameAlignment = HorizontalAlignment.Right,
                    PositiveIntOnly = true,
                    Separator = "->",
                };
                box.AddTextBox();
                box.AddTextBox();

                box.GetTextBox(0).Box.TextChanged += (s, e) => NotErrorIfNotEmpty(box.GetTextBox(0));
                box.GetTextBox(1).Box.TextChanged += (s, e) => NotErrorIfNotEmpty(box.GetTextBox(1));
                box.GetTextBox(2).Box.TextChanged += (s, e) => NotErrorIfNotEmpty(box.GetTextBox(2));
                box.SetDefaultText(0, "套筒位");
                box.SetDefaultText(1, "批头使用上限");
                box.SetDefaultText(2, "单次计数量");

                SignButton minusButton = box.AddButton<SignButton>();
                minusButton.Icon = Properties.Resources.sign_minus;
                minusButton.Click += (s, e) => {
                    _screwBitCounters.Remove(box);
                    box.Dispose();
                    for (int i = 0; i < _screwBitCounters.Count; i++) {
                        _screwBitCounters[i].TextName = $"批头计数器{i + 1}";
                        _screwBitCounters[i].ResizeChildren();
                    }
                    ResizeSelf();
                };

                _screwBitCounters.Add(box);
                for (int i = 0; i < _screwBitCounters.Count; i++) {
                    _screwBitCounters[i].TextName = $"批头计数器{i + 1}";
                }
                _tablePanel.SetColumnSpan(box, _columnCount);

                ResizeSelf();
            }

            private void NotErrorIfNotEmpty(CustomTextBox box) {
                if (!string.IsNullOrEmpty(box.Box.Text)) {
                    box.IsError = false;
                }
            }

            public void ResizeSelf() {
                ResizeTablePanelAndItsChildren();
                Invalidate();
            }

            public void ResizeTablePanelAndItsChildren() {
                CalculateDetailProperties();

                Padding contentPadding = ContentPanel.Padding;
                int boxHeight = WidgetUtils.PopUpOrFloatingFormTextOrComboBoxHeight();
                int boxMargin = boxHeight / 5;
                int subTitleHeight = WidgetUtils.PopUpOrFloatingFormSubTitle();
                int subTitleMargin = subTitleHeight / 5;
                int tableHeight = 0;
                int previousRowIndex = -1;
                int cntentWidth = (int) (WidgetUtils.MainSize.Width * .5);
                int tableWidth = cntentWidth - contentPadding.Size.Width;
                int contentPieceWidth = (tableWidth - boxMargin * (_columnCount + 1)) / _columnCount;
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
                        control.Margin = new(boxMargin, boxMargin, 0, boxMargin);

                        int columnSpan = _tablePanel.GetColumnSpan(control);
                        if (columnSpan > 1) {
                            control.Size = new(contentPieceWidth * columnSpan + boxMargin * (columnSpan - 1), boxHeight);
                        } else {
                            control.Size = new(contentPieceWidth, boxHeight);
                        }
                    }
                }

                SetContentSizeAndSelfSize(contentSize);
            }
        }

        public class PredecessorPartMissionMap {
            private int _index;
            private CustomComboBoxGroup<int> _barCodeRuleId;
            private CustomComboBoxGroup<int> _missionId;

            public int Index { get => _index; set => _index = value; }
            public CustomComboBoxGroup<int> BarCodeRuleId { get => _barCodeRuleId; set => _barCodeRuleId = value; }
            public CustomComboBoxGroup<int> MissionId { get => _missionId; set => _missionId = value; }

            public PredecessorPartMissionMap(int index, CustomComboBoxGroup<int> barCodeRuleId, CustomComboBoxGroup<int> missionId) {
                _index = index;
                _barCodeRuleId = barCodeRuleId;
                _missionId = missionId;

                _barCodeRuleId.ItemSelected += () => _barCodeRuleId.SetError(false);
                _missionId.ItemSelected += () => _missionId.SetError(false);
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
                    this.ImageShowing = WidgetUtils.ResizeImage(this.Icon, newImageSide, newImageSide);
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
            protected override void OnSizeChanged(EventArgs e) {
                base.OnSizeChanged(e);
                ConerRadius = 0;
            }
            protected override void ResizeTextLabel() {
                if (this.Label != null) {
                    Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (Height * .4), FontStyle.Regular, GraphicsUnit.Pixel);
                    using (Graphics g = CreateGraphics()) {
                        this.LabelX = (int) ((this.Width - g.MeasureString(this.Label, this.Font).Width) / 2 + this.Width * .01);
                    }
                    this.LabelY = (int) ((this.Height - this.Font.Height * 1.1) / 2);
                }
            }
        }
        public class SideButton: CommonButton {
            private ILog logger = MainUtils.GetLogger(typeof(SideButton));

            private Color? _originalBackColor;
            private Color? _triggeredBackColor;
            private ProductMissionDTO _missionDTO;
            private ProductSideDTO _sideDTO;
            private LeftBottomContentPanel _container;
            private ProductImageFile _productImageFile;
            private ProductImageFile _productImageFileNew;
            // Radius of rounded bolt buttons
            private int _boltButtonRadius;
            // All rounded bolt buttons above the product image
            private SortedList<int, List<BoltButton>> _boltButtons;
            // All edition buttons in the right panel
            private SortedList<int, List<BoltEditionButton>> _boltEditionButtons;
            // Serial number of current chosen bolt
            private int? _currentWorkstationId;
            private int? _currentSerialNum;
            // RightContentPanel
            private Action<int, bool> _changeVisibleOfBoltEditionButtons;
            // Tip of each side button, to tell that it can be double clicked to rename
            private ToolTip? _toolTip;

            public ProductMissionDTO MissionDTO { get => _missionDTO; set => _missionDTO = value; }
            public ProductSideDTO SideDTO { get => _sideDTO; set => _sideDTO = value; }
            public ProductImageFile ProductImageFile { set => _productImageFile = value; }
            public ProductImageFile ProductImageFileNew { get => _productImageFileNew; }
            // Separate by workstation id, key of inner sorted list is serial num of each bolt
            public SortedList<int, List<BoltButton>> BoltButtons { get => _boltButtons; }
            public SortedList<int, List<BoltEditionButton>> BoltEditionButtons { get => _boltEditionButtons; }
            public int BoltButtonRadius { get => _boltButtonRadius; set => _boltButtonRadius = value; }
            public int? CurrentWorkstationId { get => _currentWorkstationId; set => _currentWorkstationId = value; }
            public int? CurrentSerialNum { get => _currentSerialNum; set => _currentSerialNum = value; }
            public Action<int, bool> ChangeVisibleOfBoltEditionButtons { get => _changeVisibleOfBoltEditionButtons; set => _changeVisibleOfBoltEditionButtons = value; }

            // Properties for distinguishing single click and double click
            public int ClickTimes { get; set; }
            public int Milliseconds { get; set; }
            public Timer ClickTimer { get; set; }
            private bool Fired { get; set; }
            public EventArgs? EventArgs { get; set; }

            public Action<EventArgs>? SingleClickDelegate;
            public Action<EventArgs>? DoubleClickDelegate;

            public SideButton(ProductMissionDTO missionDTO, ProductSideDTO sideDTO, LeftBottomContentPanel leftBottomContentPanel,
                    ProductImageFile productImageFile, ProductImageFile productImageFileNew, Action<int, bool> changeVisibleOfBoltEditionButtons) {
                _missionDTO = missionDTO;
                _sideDTO = sideDTO;
                Label = sideDTO.name;
                _container = leftBottomContentPanel;
                _productImageFile = productImageFile;
                _productImageFileNew = productImageFileNew;
                _changeVisibleOfBoltEditionButtons = changeVisibleOfBoltEditionButtons;
                _boltButtons = new();
                _boltButtonRadius = 1;
                _boltEditionButtons = new();

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

                foreach (List<BoltButton> buttons in _boltButtons.Values) {
                    foreach (BoltButton button in buttons) {
                        button.Visible = flag;
                    }
                }
                if (SideDTO != null) {
                    _changeVisibleOfBoltEditionButtons(SideDTO.id, flag);
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
                // Check for null variables
                if (_currentWorkstationId == null || _currentSerialNum == null) {
                    string errorMsg = $"Workstation id or Current serial Num can not be null, please check.";
                    logger.Error(errorMsg);
                    throw new NullReferenceException(errorMsg);
                }

                // Delete bolt buttons (both in pictrue and right panel)
                BoltButton boltButton = _boltButtons[_currentWorkstationId.Value].Single(b => b.BoltDTO.serial_num == _currentSerialNum.Value);
                BoltEditionButton boltEditionButton = _boltEditionButtons[_currentWorkstationId.Value].Single(b => b.BoltDTO.serial_num == _currentSerialNum.Value);

                // Delete boltDto from sideDto
                boltButton.BoltDTO.deleted = (int) (YesOrNo.YES);

                // Do deletion
                _boltButtons[_currentWorkstationId.Value].Remove(boltButton);
                _boltEditionButtons[_currentWorkstationId.Value].Remove(boltEditionButton);

                // Resort and reorder all bolt buttons
                ResetSerialNumbers();

                // Reset current bolt index
                _currentWorkstationId = null;
                _currentSerialNum = null;

                // Dispose deleted button
                boltButton.Dispose();
                boltEditionButton.Dispose();
            }

            public void ResetSerialNumbers() {
                // Sort by serial numbers
                foreach (int key in _boltButtons.Keys.ToList()) {
                    _boltButtons[key] = _boltButtons[key].OrderBy(b => b.BoltDTO.serial_num).ToList();
                    _boltEditionButtons[key] = _boltEditionButtons[key].OrderBy(b => b.BoltDTO.serial_num).ToList();
                }

                // Reorder buttons and reset serial numbers
                int currentSerialNum = 1;
                foreach (KeyValuePair<int, List<BoltButton>> pair in _boltButtons) {
                    for (int i = 0; i < pair.Value.Count; i++) {
                        // Reset serial num of bolt button
                        BoltButton btn = pair.Value[i];
                        btn.BoltDTO.serial_num = currentSerialNum;
                        btn.UpperNum = null;
                        btn.Label = btn.BoltDTO.serial_num + "";
                        btn.Invalidate();

                        // Reset serial num of bolt edition button
                        BoltEditionButton btn2 = _boltEditionButtons[pair.Key][i];
                        btn2.BoltDTO.serial_num = currentSerialNum;
                        btn2.UpperNum = null;
                        btn2.Label = btn2.BoltDTO.serial_num + ". " + btn2.BoltDTO.name;
                        btn2.Invalidate();

                        currentSerialNum++;
                    }
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
                this.Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (Height * .4), Toggled ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
            }
            protected override void OnMouseUp(MouseEventArgs mevent) {
                base.OnMouseUp(mevent);
                if (mevent.Button == MouseButtons.Left) {
                    if (ClickTimes == 0) {
                        EventArgs = mevent;
                        ClickTimer.Start();
                    }
                    ClickTimes++;
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
                int workPlacePadding = WidgetUtils.ContentInnerBorderMargin() * 2 + 1;
                int workPlaceWidth = mainFormWidth - workPlacePadding * 2;
                int workPlaceHeight = mainFormHeight - (int) (mainFormHeight * WidgetUtils.WorkplaceTopBarHeightRatio()) - workPlacePadding * 2;
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
                    ProductDefaultImageShowing = WidgetUtils.ResizeImage(ProductDefaultImage, newImageSide, newImageSide);
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
                    // g.DrawRectangle(new Pen(Color.Black), new Rectangle(ImageLocation.Value, ProductImage.Size));

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
                    Pen pen = new Pen(ColorConfigs.COLOR_TEXT_BOX_FOREGROUND, 1) {
                        DashPattern = new float[] { 9, 6, 9, 6 },
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
            protected ILog logger = MainUtils.GetLogger(typeof(RightContentPanel));

            private Size _boltSize;
            private int _boltMargin;
            private int _workstationMargin;
            private Dictionary<int, SideButtonsPanel> _panels = new();

            public Size BoltSize { get => _boltSize; set => _boltSize = value; }
            public int BoltMargin {
                get => _boltMargin;
                set {
                    _boltMargin = value;
                    _workstationMargin = value * 5;
                }
            }
            public Dictionary<int, SideButtonsPanel> Panels { get => _panels; set => _panels = value; }

            public BoltEditionButton AddNewBoltEditionButton(SideButton sideButton, ProductBoltDTO boltDTO, Action<ProductBoltDTO> openBoltPopUpForm) {
                if (sideButton.SideDTO != null) {
                    // Get panel that corresponding to current side button
                    SideButtonsPanel sidePanel;
                    if (!_panels.ContainsKey(sideButton.SideDTO.id)) {
                        sidePanel = new(sideButton) {
                            Parent = this,
                        };
                        _panels.Add(sideButton.SideDTO.id, sidePanel);
                    } else {
                        sidePanel = _panels[sideButton.SideDTO.id];
                    }
                    WorkstationButtonsPanel btnPanel;
                    if (!sidePanel.BtnPanels.ContainsKey(boltDTO.workstation_id)) {
                        btnPanel = new(boltDTO.workstation_id) {
                            Parent = sidePanel,
                        };
                        sidePanel.BtnPanels.Add(boltDTO.workstation_id, btnPanel);
                    } else {
                        btnPanel = sidePanel.BtnPanels[boltDTO.workstation_id];
                    }

                    // Create new bolt edit button
                    BoltEditionButton boltEditionButton = new(boltDTO) {
                        Parent = btnPanel,
                        ForeColor = ColorConfigs.COLOR_MISSION_EDITION_TEXT,
                        BackColor = ColorConfigs.COLOR_MISSION_EDITION_BUTTON_BACK,
                    };
                    btnPanel.Btns.Add(boltEditionButton);
                    // Delete button
                    boltEditionButton.Deleted += () => {
                        sideButton.CurrentWorkstationId = boltEditionButton.BoltDTO.workstation_id;
                        sideButton.CurrentSerialNum = boltEditionButton.BoltDTO.serial_num;
                        sideButton.DeleteBolt();
                    };
                    // Click and open bolt info edition pop up form
                    boltEditionButton.SingleClickDelegate += (eventArgs) => {
                        sideButton.CurrentSerialNum = boltEditionButton.BoltDTO.serial_num;
                        sideButton.CurrentWorkstationId = boltEditionButton.BoltDTO.workstation_id;
                        openBoltPopUpForm(boltDTO);
                    };
                    // Double click to rename bolt
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
                    // Reorder buttons in display
                    btnPanel.ReorderButtons();

                    // Return created bolt edit button
                    return boltEditionButton;
                } else {
                    // Throw exception if side dto is null
                    string errorMsg = $"SideDTO can not be null, please check for side button = {sideButton.Label}";
                    logger.Error(errorMsg);
                    throw new NullReferenceException(errorMsg);
                }
            }

            public void CalNewHeightAdnResizeChildren(int sideId, int parentNewHeight) {
                if (!_panels.ContainsKey(sideId)) {
                    NewHeight = 0;
                    return;
                }

                int sideBtnCount = 0;

                foreach (KeyValuePair<int, WorkstationButtonsPanel> innerPair in _panels[sideId].BtnPanels) {
                    // Resize buttons
                    foreach (Control control in innerPair.Value.Controls) {
                        if (control is BoltEditionButton btn) {
                            btn.Size = _boltSize;
                            btn.Margin = new(_boltMargin, _boltMargin, _boltMargin, 0);
                        }
                    }

                    // Count buttons
                    sideBtnCount += innerPair.Value.Controls.Count;

                    // Resize workstation buttons panel
                    innerPair.Value.Size = new(_boltSize.Width + _boltMargin * 2, (_boltSize.Height + _boltMargin) * innerPair.Value.Controls.Count);
                }

                // Resize side buttons panel
                int panelNewHeight = (_boltSize.Height + _boltMargin) * sideBtnCount + _boltMargin;
                _panels[sideId].Size = new(_boltSize.Width + _boltMargin * 2, panelNewHeight);

                // Reset new height of the whole content
                NewHeight = panelNewHeight;

                // If needs scroll bar
                if (NewHeight > parentNewHeight) {
                    int btnWidth = _boltSize.Width - WidgetUtils.ScrollBarThickness();

                    foreach (KeyValuePair<int, WorkstationButtonsPanel> innerPair in _panels[sideId].BtnPanels) {
                        // Resize buttons
                        foreach (Control control in innerPair.Value.Controls) {
                            if (control is BoltEditionButton btn) {
                                btn.Width = btnWidth;
                            }
                        }

                        // Resize workstation buttons panel
                        innerPair.Value.Width = btnWidth + _boltMargin * 2;
                    }
                }
            }

            public override bool CheckNeedsScrollBar(int parentNewHeight) {
                return NewHeight > parentNewHeight;
            }

            public class SideButtonsPanel: CustomContentPanel {
                private SideButton _sideBtn;
                private Dictionary<int, WorkstationButtonsPanel> _btnPanels = new();

                public SideButton SideBtn { get => _sideBtn; set => _sideBtn = value; }
                public Dictionary<int, WorkstationButtonsPanel> BtnPanels { get => _btnPanels; set => _btnPanels = value; }

                public SideButtonsPanel(SideButton sideBtn) => _sideBtn = sideBtn;
            }

            public class WorkstationButtonsPanel: CustomContentPanel {
                private int _workstationId;
                private List<BoltEditionButton> _btns = new();

                public WorkstationButtonsPanel(int workstationId) => _workstationId = workstationId;
                public List<BoltEditionButton> Btns { get => _btns; set => _btns = value; }

                internal void ReorderButtons() {
                    _btns = _btns.Where(b => !b.IsDisposed).OrderBy(b => b.BoltDTO.serial_num).ToList();
                    for (int i = 0; i < _btns.Count; i++) {
                        Controls.SetChildIndex(_btns[i], i);
                    }
                }
            }
        }

        public class BoltEditionButton: DeletableButton {
            private ProductBoltDTO _boltDTO;
            private string? _label;
            private int? _upperNum;

            public ProductBoltDTO BoltDTO { get => _boltDTO; set => _boltDTO = value; }
            // Properties for distinguishing single click and double click
            public int ClickTimes { get; set; }
            public int Milliseconds { get; set; }
            public Timer ClickTimer { get; set; }
            private bool Fired { get; set; }
            public MouseEventArgs? EventArgs { get; set; }
            public Action<MouseEventArgs>? SingleClickDelegate;
            public Action<MouseEventArgs>? DoubleClickDelegate;
            public new string? Label {
                get => base.Label;
                set {
                    _label = value;
                    SetLabel();
                }
            }
            public int? UpperNum {
                get => _upperNum;
                set {
                    _upperNum = value;
                    SetLabel();
                }
            }

            public BoltEditionButton(ProductBoltDTO boltDTO) {
                _boltDTO = boltDTO;
                _label = _boltDTO.serial_num + ". " + _boltDTO.name;
                SetLabel();

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

            private void SetLabel() {
                if (_upperNum != null) {
                    base.Label = $"{_upperNum}-{_label}";
                } else {
                    base.Label = _label;
                }
            }

            protected override void OnSizeChanged(EventArgs e) {
                ConerRadius = WidgetUtils.ControlRadius();
                base.OnSizeChanged(e);
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
