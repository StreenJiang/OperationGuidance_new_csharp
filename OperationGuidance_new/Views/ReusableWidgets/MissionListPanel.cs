using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.Utils;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Models.DTOs;
using System.Threading;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class MissionListPanel: CustomContentPanel {
        private TitlePanel? _titlePanel;
        private CustomVScrollingContentPanel _contentOuterPanel;
        private ContentPanel _contentPanel;
        private List<ProductMissionDTO> _missionDTOs;
        private ProductMissionBlock<ProductMissionDTO>? _currentToggledMission = null;
        private int _titleHeight;
        private bool _isLoading = false; // P0级优化：Loading状态指示器
        private CancellationTokenSource? _cancellationTokenSource; // P0级优化：取消令牌
        private Label? _loadingLabel; // 修复严重问题 #11: 可视化Loading指示器

        public int TitleHeight { get => _titleHeight; set => _titleHeight = value; }
        public ProductMissionBlock<ProductMissionDTO>? CurrentToggledMission { get => _currentToggledMission; set => _currentToggledMission = value; }
        public TitlePanel? TitlePanel { get => _titlePanel; set => _titlePanel = value; }
        public bool IsLoading { get => _isLoading; } // P0级优化：Loading状态属性
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

        /// <summary>
        /// P0级性能优化：异步刷新任务块
        /// 使用并行加载图片，避免UI阻塞
        /// 修复严重问题 #11: 添加回退机制
        /// </summary>
        public void RefreshMissionBlocks(List<ProductMissionDTO> missionDTOs, Action<int?>? blockClickAction, bool toggleBlock = false) {
            try {
                MainUtils.logger?.Info($"[RefreshMissionBlocks] Starting refresh (async with fallback)");

                // 启动异步刷新（不阻塞UI线程）
                _ = RefreshMissionBlocksAsync(missionDTOs, blockClickAction, toggleBlock)
                    .ContinueWith(t => {
                        if (t.IsFaulted) {
                            MainUtils.logger?.Error("Async refresh failed, falling back to sync", t.Exception);
                            try {
                                RefreshMissionBlocksSync(missionDTOs, blockClickAction, toggleBlock);
                            } catch (Exception syncEx) {
                                MainUtils.logger?.Error("Sync fallback also failed", syncEx);
                            }
                        } else {
                            MainUtils.logger?.Info("Async refresh completed successfully");
                        }
                    }, TaskContinuationOptions.OnlyOnFaulted);
            } catch (Exception ex) {
                MainUtils.logger?.Error("Error calling async refresh", ex);
                // 直接降级到同步版本
                RefreshMissionBlocksSync(missionDTOs, blockClickAction, toggleBlock);
            }
        }

        /// <summary>
        /// 同步版本的刷新方法（基于原有代码）
        /// 修复严重问题 #11: 作为异步版本的回退机制
        /// 修复图片显示问题：使用修复后的GetProductImage方法，确保图片对象生命周期正确
        /// </summary>
        private void RefreshMissionBlocksSync(List<ProductMissionDTO> missionDTOs, Action<int?>? blockClickAction, bool toggleBlock) {
            MainUtils.logger?.Info($"[RefreshMissionBlocksSync] Using synchronous fallback for {missionDTOs.Count} missions");

            try {
                ShowLoadingIndicator();

                _contentPanel.BigButtonPanel.Hide();
                _contentPanel.MissionsTable.Show();
                _contentPanel.MissionsTable.Controls.Clear();

                int imageLoadCount = 0;
                int imageLoadSuccessCount = 0;

                foreach (var mission in missionDTOs) {
                    Image? coverImage = null;
                    if (mission.ProductSides != null) {
                        foreach (var sideDTO in mission.ProductSides) {
                            if (!string.IsNullOrEmpty(sideDTO.image)) {
                                imageLoadCount++;
                                coverImage = MainUtils.GetProductImage(sideDTO.image);
                                if (coverImage != null) {
                                    imageLoadSuccessCount++;
                                    MainUtils.logger?.Debug($"[RefreshMissionBlocksSync] Image loaded successfully: {sideDTO.image}, Size: {coverImage.Size}");
                                } else {
                                    MainUtils.logger?.Warn($"[RefreshMissionBlocksSync] Failed to load image: {sideDTO.image}");
                                }

                                if (coverImage != null && sideDTO.rotate_angle.HasValue) {
                                    MainUtils.logger?.Debug($"[RefreshMissionBlocksSync] Rotating image {sideDTO.image} by {sideDTO.rotate_angle.Value} degrees");
                                    coverImage = WidgetUtils.RotateImage(coverImage, sideDTO.rotate_angle.Value);
                                }
                                break;
                            }
                        }
                    }

                    CreateMissionBlock(mission, coverImage, blockClickAction, toggleBlock);
                }

                _missionDTOs = missionDTOs;
                _contentOuterPanel.ResizeChildren();
                _contentPanel.ResizeCells();
                _contentPanel.MissionsTable.Invalidate();
                _contentPanel.MissionsTable.Refresh();

                MainUtils.logger?.Info($"[RefreshMissionBlocksSync] Sync refresh completed. Images: {imageLoadSuccessCount}/{imageLoadCount} loaded successfully");
            } catch (Exception ex) {
                MainUtils.logger?.Error($"[RefreshMissionBlocksSync] Error during sync refresh", ex);
                throw;
            } finally {
                HideLoadingIndicator();
            }
        }

        /// <summary>
        /// P0级性能优化：异步刷新任务块（内部实现）
        /// 修复严重问题 #11: 简化异步流程，移除嵌套 BeginInvoke，使用同步 Invoke 进行关键UI操作
        /// </summary>
        private async Task RefreshMissionBlocksAsync(List<ProductMissionDTO> missionDTOs, Action<int?>? blockClickAction, bool toggleBlock = false) {
            // 取消之前的操作（如果正在加载）
            // 修复严重问题 #6: 释放旧的 CancellationTokenSource
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            try {
                MainUtils.logger?.Info($"[RefreshMissionBlocksAsync] Starting refresh for {missionDTOs.Count} missions");

                // 步骤1: 显示Loading (UI线程)
                ShowLoadingIndicator();

                if (missionDTOs.Count > 0) {
                    // 步骤2: 后台线程并行加载所有图片 (无UI操作)
                    MainUtils.logger?.Info($"[RefreshMissionBlocksAsync] Loading images in background thread");

                    var imageLoadTasks = missionDTOs.Select(async mission => {
                        if (cancellationToken.IsCancellationRequested) return (mission, (Image?)null);

                        Image? coverImage = null;
                        if (mission.ProductSides != null) {
                            foreach (var sideDTO in mission.ProductSides) {
                                if (cancellationToken.IsCancellationRequested) break;

                                if (!string.IsNullOrEmpty(sideDTO.image)) {
                                    try {
                                        coverImage = await MainUtils.GetProductImageAsync(sideDTO.image);
                                        if (coverImage != null && sideDTO.rotate_angle.HasValue) {
                                            coverImage = WidgetUtils.RotateImage(coverImage, sideDTO.rotate_angle.Value);
                                        }
                                        break;
                                    } catch (Exception ex) {
                                        // 记录图片加载失败，但不中断整体流程
                                        MainUtils.logger?.Warn($"Failed to load image: {sideDTO.image}", ex);
                                    }
                                }
                            }
                        }
                        return (mission, coverImage);
                    });

                    var results = await Task.WhenAll(imageLoadTasks);
                    MainUtils.logger?.Info($"[RefreshMissionBlocksAsync] Images loaded, creating UI controls");

                    // 步骤3: UI线程中批量创建控件 (直接使用 Invoke，避免线程池线程阻塞)
                    _contentPanel.Invoke(() => {
                        try {
                            MainUtils.logger?.Info($"[RefreshMissionBlocksAsync] Starting UI operations on UI thread");

                            // 清理现有控件
                            _contentPanel.MissionsTable.Controls.Clear();
                            _contentPanel.BigButtonPanel.Hide();
                            _contentPanel.MissionsTable.Show();

                            // 批量创建新控件
                            foreach (var (mission, coverImage) in results) {
                                if (cancellationToken.IsCancellationRequested) return;
                                CreateMissionBlock(mission, coverImage, blockClickAction, toggleBlock);
                            }

                            MainUtils.logger?.Info($"[RefreshMissionBlocksAsync] Created {results.Length} mission blocks");

                            // 更新字段
                            _missionDTOs = missionDTOs;

                            // 强制重绘
                            _contentOuterPanel.ResizeChildren();
                            _contentPanel.ResizeCells();
                            _contentPanel.MissionsTable.Invalidate();
                            _contentPanel.MissionsTable.Refresh();

                            MainUtils.logger?.Info($"[RefreshMissionBlocksAsync] UI refresh completed");
                        } catch (Exception ex) {
                            MainUtils.logger?.Error($"[RefreshMissionBlocksAsync] Error in UI thread operations", ex);
                            throw;
                        }
                    });
                } else {
                    // 没有任务时显示空状态
                    MainUtils.logger?.Info($"[RefreshMissionBlocksAsync] No missions, showing empty state");

                    if (_contentPanel.IsHandleCreated && !_contentPanel.IsDisposed) {
                        _contentPanel.Invoke(() => {
                            _contentPanel.MissionsTable.Hide();
                            _contentPanel.BigButtonPanel.Show();
                            _contentPanel.ResizeCells();
                        });
                    }
                }
            } catch (OperationCanceledException) {
                // 正常取消，不记录错误
                MainUtils.logger?.Info($"[RefreshMissionBlocksAsync] Operation cancelled");
            } catch (Exception ex) {
                MainUtils.logger?.Error($"[RefreshMissionBlocksAsync] Error during refresh", ex);
                // 修复警告问题 #8: 移除不必要的 Task.Run 包装，直接使用 BeginInvoke
                if (WidgetUtils.MainForm != null && WidgetUtils.MainForm.IsHandleCreated) {
                    WidgetUtils.MainForm.BeginInvoke(() => {
                        WidgetUtils.ShowErrorPopUp($"加载任务时发生错误: {ex.Message}");
                    });
                }
                throw;
            } finally {
                // 步骤4: 隐藏Loading (UI线程)
                HideLoadingIndicator();
            }
        }

        /// <summary>
        /// 创建单个任务块
        /// </summary>
        private void CreateMissionBlock(ProductMissionDTO mission, Image? coverImage, Action<int?>? blockClickAction, bool toggleBlock) {
            ProductMissionBlock<ProductMissionDTO> block = new(
                mission,
                coverImage,
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

        /// <summary>
        /// P0级性能优化：显示Loading指示器
        /// 修复严重问题 #11: 添加可视化的Loading指示器
        /// </summary>
        private void ShowLoadingIndicator() {
            MainUtils.logger?.Info("[RefreshMissionBlocksAsync] Showing loading indicator");

            _isLoading = true;

            _contentPanel.Invoke(() => {
                try {
                    // 如果Loading标签不存在，创建它
                    if (_loadingLabel == null) {
                        _loadingLabel = new Label();
                        _loadingLabel.Text = "正在加载任务...";
                        _loadingLabel.Font = new Font("Microsoft YaHei", 12, FontStyle.Bold);
                        _loadingLabel.ForeColor = Color.Blue;
                        _loadingLabel.AutoSize = true;
                        _loadingLabel.TextAlign = ContentAlignment.MiddleCenter;
                        _loadingLabel.Anchor = AnchorStyles.None;

                        // 添加到容器
                        _contentPanel.MissionsTable.Controls.Add(_loadingLabel);
                    }

                    // 修复优化建议 #5: 确保Label尺寸已计算
                    _loadingLabel.Refresh();

                    // 居中定位
                    _loadingLabel.Location = new Point(
                        (_contentPanel.MissionsTable.Width - _loadingLabel.Width) / 2,
                        (_contentPanel.MissionsTable.Height - _loadingLabel.Height) / 2
                    );

                    _loadingLabel.Visible = true;
                    _loadingLabel.BringToFront();
                    MainUtils.logger?.Info("[RefreshMissionBlocksAsync] Loading indicator shown");
                } catch (Exception ex) {
                    MainUtils.logger?.Error("Error showing loading indicator", ex);
                }
            });
        }

        /// <summary>
        /// P0级性能优化：隐藏Loading指示器
        /// 修复严重问题 #11: 隐藏可视化的Loading指示器
        /// </summary>
        private void HideLoadingIndicator() {
            MainUtils.logger?.Info("[RefreshMissionBlocksAsync] Hiding loading indicator");

            _contentPanel.Invoke(() => {
                try {
                    if (_loadingLabel != null) {
                        _loadingLabel.Visible = false;
                    }
                    _isLoading = false;
                    MainUtils.logger?.Info("[RefreshMissionBlocksAsync] Loading indicator hidden");
                } catch (Exception ex) {
                    MainUtils.logger?.Error("Error hiding loading indicator", ex);
                }
            });
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
                    Visible = true, // 修复严重问题 #11: 确保表格初始可见
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
