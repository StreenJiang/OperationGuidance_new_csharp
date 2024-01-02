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

        public MissionListPanel(string title, int tableColumns, string buttonLabel, EventHandler rightButtonClick) {
            FlowDirection = FlowDirection.TopDown;
            _titlePanel = new(title) {
                Parent = this,
            };
            TitlePanel.RightButton rightButton =  _titlePanel.AddRightButton(buttonLabel);
            rightButton.Click += rightButtonClick;

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
    }
}
