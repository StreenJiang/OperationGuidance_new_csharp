using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.Utils;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Models.DTOs;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class MissionListPanel: CustomContentPanel {
        private TitlePanel? _titlePanel;
        private CustomVScrollingContentPanel _contentOuterPanel;
        private ContentPanel _contentPanel;
        private List<ProductMissionDTO> _missionDTOs;
        private ProductMissionBlock<ProductMissionDTO>? _currentToggledMission = null;
        private int _titleHeight;
        private CancellationTokenSource? _loadCts;
        private readonly SemaphoreSlim _loadSemaphore = new(4);

        public int TitleHeight { get => _titleHeight; set => _titleHeight = value; }
        public ProductMissionBlock<ProductMissionDTO>? CurrentToggledMission { get => _currentToggledMission; set => _currentToggledMission = value; }
        public TitlePanel? TitlePanel { get => _titlePanel; set => _titlePanel = value; }
        public List<ProductMissionBlock<ProductMissionDTO>> MissionBlocks {
            get {
                List<ProductMissionBlock<ProductMissionDTO>> _missionBlocks = new();
                foreach (Control ctrl in _contentPanel.MissionsTable.Controls) {
                    _missionBlocks.Add((ProductMissionBlock<ProductMissionDTO>) ctrl);
                }
                return _missionBlocks;
            }
        }

        public MissionListPanel() : this(null, null, null) { }
        public MissionListPanel(string title) : this(title, null, null) { }
        public MissionListPanel(string? title, string? buttonLabel, EventHandler? rightButtonClick) {
            FlowDirection = FlowDirection.TopDown;
            BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND;
            if (title != null) {
                _titlePanel = new(title) {
                    Parent = this,
                };
                if (buttonLabel != null) {
                    TitlePanel.RightButton rightButton = _titlePanel.AddRightButton<TitlePanel.RightButton>(buttonLabel);
                    rightButton.Click += rightButtonClick;
                }
            }

            _missionDTOs = new();
            _contentPanel = new(CalculateAndCheckScrollBar);
            _contentOuterPanel = new(null, _contentPanel) {
                Parent = this,
                NeedsPadding = false,
            };
        }

        private bool CalculateAndCheckScrollBar(int parentNewHeight) {
            if (_titlePanel != null) {
                _titleHeight = WidgetUtils.ContentTitleHeight();
            } else {
                _titleHeight = 0;
            }
            // If there is no any mission, then don't need scroll bar
            if (_missionDTOs.Count == 0) {
                NewHeight = 0;
                return false;
            }
            // Calculate height of cells
            int cellHeight = (int) (_contentOuterPanel.Height * _contentPanel.CellHightRatio);
            _contentPanel.CellVerticalMargin = cellHeight / 15;
            _contentPanel.CellSize = new(0, cellHeight);
            // Calculate table's size, depends on all cells
            int rowsCount = (int) Math.Ceiling(_missionDTOs.Count / (double) _contentPanel.TableColumns);
            _contentPanel.NewHeight = (rowsCount + 1) * _contentPanel.CellVerticalMargin + rowsCount * _contentPanel.CellSize.Height;
            if (_contentPanel.NewHeight > parentNewHeight) {
                return true;
            } else {
                _contentPanel.NewHeight = parentNewHeight;
                return false;
            }
        }

        public void RefreshMissionBlocks(List<ProductMissionDTO> missionDTOs, Action<int?>? blockClickAction, bool toggleBlock = false) {
            // Skip rebuild if data is identical
            if (_missionDTOs.Count > 0 && missionDTOs.Select(m => m.id).SequenceEqual(_missionDTOs.Select(m => m.id)))
                return;

            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = new CancellationTokenSource();
            var ct = _loadCts.Token;

            if (missionDTOs.Count > 0) {
                _contentPanel.BigButtonPanel.Hide();
                _contentPanel.MissionsTable.Show();

                // Dispose old controls to prevent memory leaks (ToList avoids collection-modified-during-enumeration)
                _currentToggledMission = null;
                var oldControls = _contentPanel.MissionsTable.Controls.Cast<Control>().ToList();
                foreach (Control ctrl in oldControls) {
                    ctrl.Dispose();
                }
                _contentPanel.MissionsTable.Controls.Clear();

                for (int i = 0; i < missionDTOs.Count; i++) {
                    ProductMissionDTO mission = missionDTOs[i];
                    if (mission.ProductSides != null && mission.ProductSides.Count > 0) {
                        ProductMissionBlock<ProductMissionDTO> block = new(
                            mission,
                            null,
                            Properties.Resources.image_choose,
                            mission.name,
                            ColorConfigs.COLOR_MISSION_BLOCK_BORDER,
                            ColorConfigs.COLOR_MISSION_BLOCK_BACKGROUND,
                            ColorConfigs.COLOR_MISSION_BLOCK_IMAGE_BORDER
                        ) {
                            Parent = _contentPanel.MissionsTable,
                        };
                        block.InnerButton.ToggledButton = toggleBlock;
                        block.InnerButton.ToggledColor = WidgetUtils.DarkenColor(block.BackColor, .2);
                        block.InnerButton.MouseUp += (sender, eventArgs) => {
                            if (block.InnerButton.ToggledButton) {
                                if (_currentToggledMission == null) {
                                    _currentToggledMission = block;
                                } else {
                                    _currentToggledMission.InnerButton.SetToggle(false);
                                    if (_currentToggledMission == block) {
                                        _currentToggledMission = null;
                                    } else {
                                        _currentToggledMission = block;
                                        _currentToggledMission.InnerButton.SetToggle(true);
                                    }
                                }
                            }
                            if (blockClickAction != null) {
                                blockClickAction(block.Entity.id);
                            }
                        };
                    }
                }
                _missionDTOs = missionDTOs;
                _contentOuterPanel.ResizeChildren();

                StartLoadingCoverImages(ct);
            } else {
                _contentPanel.MissionsTable.Hide();
                _contentPanel.BigButtonPanel.Show();
            }
            _contentPanel.ResizeCells();
        }

        private void StartLoadingCoverImages(CancellationToken ct) {
            var blocks = MissionBlocks;
            foreach (var block in blocks) {
                _ = LoadOneCoverAsync(block, ct);
            }
        }

        private async Task LoadOneCoverAsync(ProductMissionBlock<ProductMissionDTO> block, CancellationToken ct) {
            await _loadSemaphore.WaitAsync(ct);
            try {
                Image? image = await Task.Run(() => {
                    ct.ThrowIfCancellationRequested();
                    Image? loaded = null;
                    if (block.Entity.ProductSides != null) {
                        foreach (var side in block.Entity.ProductSides) {
                            if (!string.IsNullOrEmpty(side.image)) {
                                loaded = ProductImageCache.GetOrLoad(side.image);
                                if (loaded != null) {
                                    if (side.rotate_angle != null) {
                                        loaded = WidgetUtils.RotateImage(loaded, side.rotate_angle.Value);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    return loaded;
                }, ct);

                if (image != null && !ct.IsCancellationRequested && !IsDisposed) {
                    BeginInvoke(() => {
                        if (!block.IsDisposed && block.Parent != null) {
                            block.CoverImage = image;
                        }
                    });
                }
            } catch (OperationCanceledException) {
            } finally {
                _loadSemaphore.Release();
            }
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _loadCts?.Cancel();
                _loadCts?.Dispose();
                // _loadSemaphore intentionally NOT disposed — pending async tasks may Release() after Cancel
            }
            base.Dispose(disposing);
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            if (_titlePanel != null) {
                // Resize title panel
                _titlePanel.Size = new(Width, _titleHeight);
            }
            // Resize content panel
            _contentOuterPanel.Size = new(Width, Height - _titleHeight);
        }

        private class ContentPanel: CustomContentPanel {
            private readonly int _tableColumns = 4;
            private readonly float _cellGapRatio = 0.02F;
            private readonly float _cellHightRatio = 0.25F;

            private Func<int, bool> _calculateSizes;
            private TableLayoutPanel _missionsTable;
            private MissionNewButtonPanel _bigButtonPanel;
            private Size _cellSize;
            private int _cellHorizontalMargin;
            private int _cellVerticalMargin;

            public int TableColumns => _tableColumns;
            public float CellHightRatio => _cellHightRatio;
            public TableLayoutPanel MissionsTable { get => _missionsTable; set => _missionsTable = value; }
            public MissionNewButtonPanel BigButtonPanel { get => _bigButtonPanel; set => _bigButtonPanel = value; }
            public Size CellSize { get => _cellSize; set => _cellSize = value; }
            public int CellHorizontalMargin { get => _cellHorizontalMargin; set => _cellHorizontalMargin = value; }
            public int CellVerticalMargin { get => _cellVerticalMargin; set => _cellVerticalMargin = value; }

            public ContentPanel(Func<int, bool> calculateSizes) {
                _calculateSizes = calculateSizes;
                _missionsTable = new() {
                    Margin = new(0),
                    Parent = this,
                    ColumnCount = _tableColumns,
                    Padding = new(0),
                    Visible = false,
                };
                _bigButtonPanel = new() {
                    Margin = new Padding(0),
                    Parent = this,
                    Visible = false,
                };
            }

            public override bool CheckNeedsScrollBar(int parentNewHeight) {
                return _calculateSizes(parentNewHeight);
            }

            protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
                _missionsTable.Size = Size;
                _bigButtonPanel.Size = Size;
                // Rezie blocks
                ResizeCells();
            }

            public void ResizeCells() {
                // Calculate width of cells
                _cellHorizontalMargin = (int) (Width * _cellGapRatio);
                int gapNum = _tableColumns + 1; // Including outer margin
                _cellSize.Width = (Width - _cellHorizontalMargin * gapNum) / _tableColumns;
                foreach (Control control in _missionsTable.Controls) {
                    control.Size = _cellSize;
                    control.Margin = new(_cellHorizontalMargin, _cellVerticalMargin, 0, 0);
                }
            }
        }
    }
}
