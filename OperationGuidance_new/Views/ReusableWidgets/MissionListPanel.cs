using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using OperationGuidance_new.Configs;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class MissionListPanel: CustomContentPanel {
        private TitlePanel _titlePanel;
        private TableLayoutPanel _missionsTable;
        private int _titleHeight;
        private Size _cellSize;
        private int _cellHorizontalMargin;
        private int _cellVerticalMargin;

        public int TitleHeight { get => _titleHeight; set => _titleHeight = value; }
        public Size CellSize { get => _cellSize; set => _cellSize = value; }
        public int CellHorizontalMargin { get => _cellHorizontalMargin; set => _cellHorizontalMargin = value; }
        public int CellVerticalMargin { get => _cellVerticalMargin; set => _cellVerticalMargin = value; }

        public MissionListPanel(string title, TitlePanel.RightButton rightButton, int tableColumns) {
            FlowDirection = FlowDirection.TopDown;
            _titlePanel = new(title, rightButton, ConfigsVariables.COLOR_TITLE_UNDERLINE) {
                Margin = new(0),
                Parent = this,
            };

            _missionsTable = new() {
                Margin = new(0),
                Parent = this,
                ColumnCount = tableColumns,
                Padding = new(0),
            };
        }

        public void RefreshMissionBlocks(List<ProductMissionDTO> missionDTOs, Action<ProductMissionDTO> blockClickAction) {
            _missionsTable.Controls.Clear();
            for (int i = 0; i < missionDTOs.Count; i++) {
                ProductMissionDTO mission = missionDTOs[i];
                if (mission.ProductSides != null && mission.ProductSides.Count > 0) {
                    Image? coverImage = null;
                    foreach (ProductSideDTO sideDTO in mission.ProductSides) {
                        if (sideDTO.image != null && sideDTO.image != string.Empty) {
                            coverImage = CommonUtils.ImageBase64ToImage(sideDTO.image);
                            if (coverImage != null) {
                                break;
                            }
                        }
                    }
                    // 创建一个任务展示块
                    ProductMissionBlock<ProductMissionDTO> block = new(
                            mission,
                            coverImage,
                            Properties.Resources.image_choose,
                            mission.name,
                            ConfigsVariables.COLOR_MISSION_BLOCK_BORDER,
                            ConfigsVariables.COLOR_MISSION_BLOCK_BACKGROUND,
                            ConfigsVariables.COLOR_MISSION_BLOCK_IMAGE_BORDER
                        )
                    {
                        Parent = _missionsTable,
                    };
                    block.Click += (sender, eventArgs) => {
                        blockClickAction(block.Entity);
                    };
                }
            }
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            // Resize title panel
            _titlePanel.Size = new(Width, _titleHeight);
            // Resize table panel
            _missionsTable.Size = new(Width, Height - _titleHeight);
            // Rezie blocks
            foreach (Control control in _missionsTable.Controls) {
                control.Size = _cellSize;
                control.Margin = new(_cellHorizontalMargin, _cellVerticalMargin, 0, 0);
            }
        }

        public class TitlePanel: Panel {
            private Label _title;
            private RightButton _rightButton;
            private Color _underlineColor;
            public Color UnderlineColor { get => _underlineColor; set => _underlineColor = value; }

            public TitlePanel(string title, RightButton rightButton, Color underlineColor) {
                _title = new() {
                    Text = title,
                    Parent = this,
                    BackColor = Color.Transparent,
                };
                _rightButton = rightButton;
                _rightButton.Parent = this;
                _underlineColor = underlineColor;
            }

            protected override void OnHandleCreated(EventArgs e) {
                base.OnHandleCreated(e);
                SizeChanged += InvokeResizing;
            }

            private void InvokeResizing(object? sender, EventArgs eventArgs) {
                if (Width <= 0 || Height <= 0) {
                    return;
                }
                // Resize title and right button
                using (Graphics g = CreateGraphics()) {
                    // Resize title label
                    _title.Height = (int) (Height * .7);
                    _title.Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (_title.Height * .65), FontStyle.Regular, GraphicsUnit.Pixel);
                    int labelWidth = (int)(g.MeasureString(_title.Text, _title.Font).Width * 1.2);
                    _title.Width = labelWidth;
                    _title.Location = new(0, (int) ((Height - _title.Height) / 1.25));

                    // Resize and location right button
                    int rightButtonHeight = (int)(Height * .65);
                    // Set height first to get new Font
                    _rightButton.Height = rightButtonHeight;
                    // Calculate new width
                    int btnLabelWidth = (int)g.MeasureString(_rightButton.Label, _rightButton.Font).Width;
                    _rightButton.Width = (int) (btnLabelWidth + rightButtonHeight * 1.2);
                    _rightButton.Location = new(Width - _rightButton.Width, (Height - rightButtonHeight) / 2);
                }
            }

            protected override void OnPaint(PaintEventArgs e) {
                base.OnPaint(e);
                int penBorder = (int)Math.Ceiling((double)((Parent.Width + Parent.Height) / 400D));
                e.Graphics.DrawLine(new(_underlineColor, penBorder), new(0, Height), new(Width, Height));
            }

            public class RightButton: CommonButton {
                protected override void ResizeTextLabel() {
                    if (Label != null) {
                        Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (Height * .55), FontStyle.Bold, GraphicsUnit.Pixel);
                        using (Graphics g = CreateGraphics()) {
                            LabelX = (int) ((Width - g.MeasureString(Label, Font).Width) / 2 + Width * .02);
                        }
                        LabelY = (Height - Font.Height) / 2;
                    }
                }
            }
        }
    }
}
