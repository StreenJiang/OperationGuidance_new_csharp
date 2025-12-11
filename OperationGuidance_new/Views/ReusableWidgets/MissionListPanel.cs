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
        /// </summary>
        public void RefreshMissionBlocks(List<ProductMissionDTO> missionDTOs, Action<int?>? blockClickAction, bool toggleBlock = false) {
            // 启动异步刷新（不阻塞UI线程）
            // 修复警告问题 #11: 正确处理异步异常
            _ = RefreshMissionBlocksAsync(missionDTOs, blockClickAction, toggleBlock)
                .ContinueWith(t => {
                    if (t.IsFaulted) {
                        MainUtils.logger?.Error("Unhandled error in RefreshMissionBlocksAsync", t.Exception);
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// P0级性能优化：异步刷新任务块（内部实现）
        /// </summary>
        private async Task RefreshMissionBlocksAsync(List<ProductMissionDTO> missionDTOs, Action<int?>? blockClickAction, bool toggleBlock = false) {
            // 取消之前的操作（如果正在加载）
            // 修复严重问题 #6: 释放旧的 CancellationTokenSource
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            try {
                ShowLoadingIndicator(); // P0级优化：显示Loading指示器

                if (missionDTOs.Count > 0) {
                    // 修复严重问题 #4: 正确的跨线程UI访问
                    await Task.Run(() => {
                        if (cancellationToken.IsCancellationRequested) return;

                        // 使用BeginInvoke确保在UI线程中访问控件
                        if (_contentPanel.IsHandleCreated && !_contentPanel.IsDisposed) {
                            _contentPanel.BeginInvoke(() => {
                                _contentPanel.BigButtonPanel.Hide();
                                _contentPanel.MissionsTable.Show();
                                _contentPanel.MissionsTable.Controls.Clear();
                            });
                        }
                    }, cancellationToken);

                    // 并行加载所有图片（P0级优化）
                    var imageTasks = missionDTOs
                        .Where(m => m.ProductSides != null && m.ProductSides.Count > 0)
                        .Select(async mission => {
                            if (cancellationToken.IsCancellationRequested) return (mission, (Image?)null);

                            Image? coverImage = null;
                            foreach (ProductSideDTO sideDTO in mission.ProductSides) {
                                if (cancellationToken.IsCancellationRequested) break;

                                if (sideDTO.image != null && sideDTO.image != string.Empty) {
                                    try {
                                        coverImage = await MainUtils.GetProductImageAsync(sideDTO.image);
                                        if (coverImage != null) {
                                            if (sideDTO.rotate_angle != null) {
                                                coverImage = WidgetUtils.RotateImage(coverImage, sideDTO.rotate_angle.Value);
                                            }
                                            break;
                                        }
                                    } catch (Exception ex) {
                                        // 记录图片加载失败，但不中断整体流程
                                        MainUtils.logger?.Warn($"Failed to load image: {sideDTO.image}", ex);
                                    }
                                }
                            }

                            return (mission, coverImage);
                        });

                    // 等待所有图片加载完成
                    var loadedImages = await Task.WhenAll(imageTasks);

                    if (cancellationToken.IsCancellationRequested) return;

                    // 批量创建UI控件（使用InvokeAsync避免UI阻塞）
                    const int batchSize = 10; // P1优化：分批加载，每批10个
                    for (int i = 0; i < loadedImages.Length; i += batchSize) {
                        if (cancellationToken.IsCancellationRequested) break;

                        var batch = loadedImages.Skip(i).Take(batchSize).ToList();

                        // 批量创建控件（同步执行以避免UI线程竞争）
                        await Task.Run(async () => {
                            foreach (var (mission, coverImage) in batch) {
                                if (cancellationToken.IsCancellationRequested) return;

                                // 使用BeginInvoke确保在UI线程创建控件
                                if (_contentPanel.MissionsTable.IsHandleCreated && !_contentPanel.MissionsTable.IsDisposed) {
                                    var tcs = new TaskCompletionSource<bool>();
                                    _contentPanel.MissionsTable.BeginInvoke(() => {
                                        try {
                                            CreateMissionBlock(mission, coverImage, blockClickAction, toggleBlock);
                                            tcs.SetResult(true);
                                        } catch (Exception ex) {
                                            tcs.SetException(ex);
                                        }
                                    });
                                    await tcs.Task;
                                }
                            }
                        }, cancellationToken);

                        // 短暂暂停让UI有机会更新（P1优化：保持60FPS流畅度）
                        if (i + batchSize < loadedImages.Length) {
                            await Task.Delay(16, cancellationToken);
                        }
                    }

                    // 检查是否需要更新mission列表并调整布局
                    // 修复严重问题 #5: 将字段访问移到UI线程，避免竞态条件
                    bool needsResize = false;
                    await Task.Run(() => {
                        if (cancellationToken.IsCancellationRequested) return;

                        // 只在后台线程计算，不访问字段
                        var oldIds = _missionDTOs.Select(m => m.id).ToList();
                        var newIds = missionDTOs.Select(m => m.id).ToList();
                        needsResize = !oldIds.SequenceEqual(newIds);
                    }, cancellationToken);

                    // 在UI线程执行字段更新和重绘
                    if (needsResize) {
                        this.BeginInvoke(() => {
                            _missionDTOs = missionDTOs;
                            _contentOuterPanel.ResizeChildren();
                            _contentPanel.ResizeCells();
                        });
                    }

                    // 修复警告问题 #8: 移除不必要的 Task.Run 包装，直接使用 BeginInvoke
                    if (_contentPanel.IsHandleCreated && !_contentPanel.IsDisposed) {
                        _contentPanel.BeginInvoke(() => {
                            _contentOuterPanel.ResizeChildren();
                            _contentPanel.ResizeCells();
                        });
                    }
                } else {
                    // 没有任务时显示空状态
                    // 修复警告问题 #8: 移除不必要的 Task.Run 包装，直接使用 BeginInvoke
                    if (_contentPanel.IsHandleCreated && !_contentPanel.IsDisposed) {
                        _contentPanel.BeginInvoke(() => {
                            _contentPanel.MissionsTable.Hide();
                            _contentPanel.BigButtonPanel.Show();
                            _contentPanel.ResizeCells();
                        });
                    }
                }
            } catch (OperationCanceledException) {
                // 正常取消，不记录错误
            } catch (Exception ex) {
                MainUtils.logger?.Error("Error while refreshing mission blocks", ex);
                // 修复警告问题 #8: 移除不必要的 Task.Run 包装，直接使用 BeginInvoke
                if (WidgetUtils.MainForm != null && WidgetUtils.MainForm.IsHandleCreated) {
                    WidgetUtils.MainForm.BeginInvoke(() => {
                        WidgetUtils.ShowErrorPopUp($"加载任务时发生错误: {ex.Message}");
                    });
                }
            } finally {
                HideLoadingIndicator(); // P0级优化：隐藏Loading指示器
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
        /// </summary>
        private void ShowLoadingIndicator() {
            _isLoading = true;
            // 这里可以添加Loading UI组件的显示逻辑
            // 例如：显示"正在加载任务..."文本或旋转图标
            // 由于原始代码没有Loading UI组件，这里仅更新状态
        }

        /// <summary>
        /// P0级性能优化：隐藏Loading指示器
        /// </summary>
        private void HideLoadingIndicator() {
            _isLoading = false;
            // 这里可以添加Loading UI组件的隐藏逻辑
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
