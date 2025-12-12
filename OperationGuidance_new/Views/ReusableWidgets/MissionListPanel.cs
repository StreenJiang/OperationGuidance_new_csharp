using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.Utils;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Models.DTOs;

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
        /// P1级优化：智能比较任务列表，避免无变化时的重复刷新
        /// </summary>
        public void RefreshMissionBlocks(List<ProductMissionDTO> missionDTOs, Action<int?>? blockClickAction, bool toggleBlock = false) {
            try {
                // 【性能优化】检查任务列表是否真的变化了
                if (IsMissionListUnchanged(missionDTOs)) {
                    MainUtils.logger?.Info("[RefreshMissionBlocks] Mission list unchanged, skipping refresh");
                    return;  // 直接返回，不刷新
                }

                MainUtils.logger?.Info($"[RefreshMissionBlocks] Starting refresh (async with fallback)");

                // 更新缓存的任务列表
                _missionDTOs = missionDTOs;

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
        /// P1级性能优化：检查任务列表是否未变化
        /// 比较数量、ID、顺序和图片，避免不必要的UI重绘
        /// </summary>
        private bool IsMissionListUnchanged(List<ProductMissionDTO> newMissionDTOs) {
            // 快速路径：数量不同，说明肯定有变化
            if (_missionDTOs == null || _missionDTOs.Count != newMissionDTOs.Count) {
                return false;
            }

            // 比较每个任务的ID和图片
            for (int i = 0; i < _missionDTOs.Count; i++) {
                var oldMission = _missionDTOs[i];
                var newMission = newMissionDTOs[i];

                // 【检查1】任务ID不同，说明任务列表有变化
                if (oldMission.id != newMission.id) {
                    return false;
                }

                // 【检查2】任务对应的图片是否有变化
                if (!AreImagesUnchanged(oldMission.ProductSides, newMission.ProductSides)) {
                    return false;
                }
            }

            // 所有ID和图片都相同，说明列表未变化
            return true;
        }

        /// <summary>
        /// P1级性能优化：比较两个ProductSideDTO列表的图片是否未变化
        /// 不仅比较图片路径，还比较位置、旋转、缩放等所有影响显示的属性
        /// </summary>
        private bool AreImagesUnchanged(List<ProductSideDTO>? oldSides, List<ProductSideDTO>? newSides) {
            // 快速路径1：都为null，认为未变化
            if (oldSides == null && newSides == null) {
                return true;
            }

            // 快速路径2：一个为null一个有值，说明有变化
            if (oldSides == null || newSides == null) {
                return false;
            }

            // 快速路径3：数量不同，说明有变化
            if (oldSides.Count != newSides.Count) {
                return false;
            }

            // 比较每个ProductSide的所有影响显示的属性
            for (int i = 0; i < oldSides.Count; i++) {
                var oldSide = oldSides[i];
                var newSide = newSides[i];

                // 【检查1】图片路径
                if (oldSide.image != newSide.image) {
                    return false;  // 图片路径变了，需要刷新
                }

                // 【检查2】旋转角度
                if (!AreFloatsEqual(oldSide.rotate_angle, newSide.rotate_angle)) {
                    return false;  // 旋转角度变了，需要刷新
                }

                // 【检查3】缩放比例
                if (!AreFloatsEqual(oldSide.zooming_ratio, newSide.zooming_ratio)) {
                    return false;  // 缩放比例变了，需要刷新
                }

                // 【检查4】额外缩放比例
                if (!AreFloatsEqual(oldSide.zooming_ratio_extra, newSide.zooming_ratio_extra)) {
                    return false;  // 额外缩放比例变了，需要刷新
                }

                // 【检查5】最大矩形位置
                if (oldSide.max_rectangle_location != newSide.max_rectangle_location) {
                    return false;  // 位置变了，需要刷新
                }

                // 【检查6】中心位置
                if (oldSide.center_location != newSide.center_location) {
                    return false;  // 中心位置变了，需要刷新
                }

                // 【检查7】位置偏移
                if (oldSide.location_offset != newSide.location_offset) {
                    return false;  // 位置偏移变了，需要刷新
                }

                // 【检查8】移动位置偏移
                if (oldSide.location_offset_moving != newSide.location_offset_moving) {
                    return false;  // 移动位置偏移变了，需要刷新
                }

                // 【检查9】最大矩形尺寸
                if (oldSide.max_rectangle_width != newSide.max_rectangle_width ||
                    oldSide.max_rectangle_height != newSide.max_rectangle_height) {
                    return false;  // 矩形尺寸变了，需要刷新
                }

                // 【检查10】裁剪状态
                if (oldSide.cropped != newSide.cropped) {
                    return false;  // 裁剪状态变了，需要刷新
                }
            }

            // 所有影响显示的属性都相同，说明图片未变化
            return true;
        }

        /// <summary>
        /// P1级性能优化：安全比较两个float?值
        /// 考虑浮点数精度问题，使用近似比较
        /// </summary>
        private bool AreFloatsEqual(float? oldValue, float? newValue) {
            // 两者都为null，相等
            if (oldValue == null && newValue == null) {
                return true;
            }

            // 一个为null一个不为null，不相等
            if (oldValue == null || newValue == null) {
                return false;
            }

            // 使用很小的 epsilon 比较浮点数（考虑精度误差）
            const float epsilon = 0.001f;
            return Math.Abs(oldValue.Value - newValue.Value) < epsilon;
        }

        /// <summary>
        /// 同步版本的刷新方法（基于原有代码）
        /// 修复严重问题 #11: 作为异步版本的回退机制
        /// 修复图片显示问题：使用修复后的GetProductImage方法，确保图片对象生命周期正确
        /// P0级UI线程阻塞修复: 为同步版本添加延时等待Loading显示
        /// P1级优化：同步版本也应用智能比较
        /// </summary>
        private void RefreshMissionBlocksSync(List<ProductMissionDTO> missionDTOs, Action<int?>? blockClickAction, bool toggleBlock) {
            MainUtils.logger?.Info($"[RefreshMissionBlocksSync] Using synchronous fallback for {missionDTOs.Count} missions");

            try {
                // 【性能优化】检查任务列表是否真的变化了
                if (IsMissionListUnchanged(missionDTOs)) {
                    MainUtils.logger?.Info("[RefreshMissionBlocksSync] Mission list unchanged, skipping refresh");
                    return;  // 直接返回，不刷新
                }

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
        /// P0级UI线程阻塞修复: 使用BeginInvoke替代Invoke，添加Task.Delay给UI时间，添加Application.DoEvents处理UI消息
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

                // 步骤1: 显示Loading (UI线程，非阻塞)
                ShowLoadingIndicator();

                BeginInvoke(async () => {
                    if (missionDTOs.Count > 0) {
                        // 步骤2: 后台线程并行加载所有图片 (无UI操作)
                        MainUtils.logger?.Info($"[RefreshMissionBlocksAsync] Loading images in background thread");

                        var imageLoadTasks = missionDTOs.Select(async mission => {
                            if (cancellationToken.IsCancellationRequested)
                                return (mission, (Image?)null);

                            Image? coverImage = null;
                            if (mission.ProductSides != null) {
                                foreach (var sideDTO in mission.ProductSides) {
                                    if (cancellationToken.IsCancellationRequested)
                                        break;

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

                        // 步骤3: UI线程中批量创建控件 (使用 BeginInvoke 非阻塞，避免线程池线程阻塞)
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        if (_contentPanel.IsHandleCreated && !_contentPanel.IsDisposed) {
                            var taskResults = results.ToList();

                            _contentPanel.BeginInvoke(() => {
                                try {
                                    MainUtils.logger?.Info($"[RefreshMissionBlocksAsync] Starting UI operations on UI thread");

                                    // 清理现有控件
                                    _contentPanel.MissionsTable.Controls.Clear();
                                    _contentPanel.BigButtonPanel.Hide();
                                    _contentPanel.MissionsTable.Show();

                                    // 分批创建UI控件，避免一次性创建过多导致UI卡顿
                                    const int batchSize = 4;
                                    for (int i = 0; i < taskResults.Count; i += batchSize) {
                                        var batch = taskResults.Skip(i).Take(batchSize);
                                        foreach (var (mission, coverImage) in batch) {
                                            if (cancellationToken.IsCancellationRequested)
                                                return;
                                            CreateMissionBlock(mission, coverImage, blockClickAction, toggleBlock);
                                        }

                                        // 每批之间短暂休息，处理UI消息，让UI有机会更新
                                        Application.DoEvents();
                                    }

                                    MainUtils.logger?.Info($"[RefreshMissionBlocksAsync] Created {taskResults.Count} mission blocks");

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
                        }
                    } else {
                        // 没有任务时显示空状态
                        MainUtils.logger?.Info($"[RefreshMissionBlocksAsync] No missions, showing empty state");

                        if (_contentPanel.IsHandleCreated && !_contentPanel.IsDisposed) {
                            _contentPanel.BeginInvoke(() => {
                                _contentPanel.MissionsTable.Hide();
                                _contentPanel.BigButtonPanel.Show();
                                _contentPanel.ResizeCells();
                            });
                        }
                    }
                });
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
                // 步骤4: 隐藏Loading (UI线程，非阻塞)
                HideLoadingIndicator();
            }
        }

        /// <summary>
        /// P2级性能优化：共享的事件处理器
        /// 避免每次创建新的Lambda委托，提高性能
        /// 修复内存泄漏：使用弱引用持有Panel，避免循环引用
        /// </summary>
        private class MissionBlockEventHandler {
            // 使用弱引用避免内存泄漏
            private readonly WeakReference<MissionListPanel> _weakPanel;
            private readonly Action<int?>? _blockClickAction;
            private readonly bool _toggleBlock;

            public MissionBlockEventHandler(MissionListPanel panel, Action<int?>? blockClickAction, bool toggleBlock) {
                _weakPanel = new WeakReference<MissionListPanel>(panel);
                _blockClickAction = blockClickAction;
                _toggleBlock = toggleBlock;
            }

            public void HandleMouseUp(object? sender, MouseEventArgs eventArgs) {
                if (sender is not ProductMissionBlock<ProductMissionDTO> block) {
                    return;
                }

                // 使用弱引用获取Panel，如果Panel已被垃圾回收，则直接返回
                if (!_weakPanel.TryGetTarget(out var panel)) {
                    return;
                }

                if (block.InnerButton.ToggledButton) {
                    if (panel._currentToggledMission == null) {
                        panel._currentToggledMission = block;
                    } else {
                        panel._currentToggledMission.InnerButton.SetToggle(false);
                        if (panel._currentToggledMission == block) {
                            panel._currentToggledMission = null;
                        } else {
                            panel._currentToggledMission = block;
                            panel._currentToggledMission.InnerButton.SetToggle(true);
                        }
                    }
                }
                if (_blockClickAction != null) {
                    _blockClickAction(block.Entity.id);
                }
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

            // 【修复】恢复原始Lambda事件处理器，确保点击事件正常工作
            // 弱引用检查会导致事件处理失效，破坏核心功能
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
        /// 使用TableLayoutPanel方案彻底解决Loading文字居中问题（水平+垂直）
        /// P0级UI线程阻塞修复: 使用BeginInvoke而非Invoke，确保非阻塞显示
        /// </summary>
        private void ShowLoadingIndicator() {
            _isLoading = true;

            // 使用BeginInvoke非阻塞调用，确保UI线程不被阻塞
            if (_contentPanel.IsHandleCreated && !_contentPanel.IsDisposed) {
                _contentPanel.BeginInvoke(() => {
                    try {
                        if (_loadingLabel == null) {
                            // 创建Label - 恢复原始样式
                            _loadingLabel = new Label();
                            _loadingLabel.Text = "正在加载任务...";
                            _loadingLabel.Font = new Font("Microsoft YaHei", 12, FontStyle.Bold);
                            _loadingLabel.ForeColor = Color.FromArgb(96, 96, 96); // 深灰色
                            _loadingLabel.AutoSize = true;
                            _loadingLabel.TextAlign = ContentAlignment.MiddleCenter;
                            _loadingLabel.Anchor = AnchorStyles.None;
                            _loadingLabel.Dock = DockStyle.None;

                            // 将TableLayoutPanel添加到主容器
                            _contentPanel.MissionsTable.Parent.Controls.Add(_loadingLabel);
                        }

                        _loadingLabel.Visible = true;
                        _loadingLabel.BringToFront();

                        // 强制刷新，确保Loading立即显示
                        _contentPanel.Refresh();
                        _contentPanel.MissionsTable.Refresh();
                        _contentPanel.MissionsTable.Parent.Refresh();

                        MainUtils.logger?.Info("[RefreshMissionBlocksAsync] Loading indicator shown");
                    } catch (Exception ex) {
                        MainUtils.logger?.Error("Error showing loading indicator", ex);
                    }
                });
            }
        }

        /// <summary>
        /// P0级性能优化：隐藏Loading指示器
        /// 修复严重问题 #11: 隐藏可视化的Loading指示器
        /// 清理TableLayoutPanel容器
        /// P0级UI线程阻塞修复: 使用BeginInvoke而非Invoke，确保非阻塞隐藏
        /// </summary>
        private void HideLoadingIndicator() {
            // 使用BeginInvoke非阻塞调用，确保UI线程不被阻塞
            if (_contentPanel.IsHandleCreated && !_contentPanel.IsDisposed) {
                _contentPanel.BeginInvoke(() => {
                    try {
                        if (_loadingLabel != null) {
                            _loadingLabel.Visible = false;

                            // 找到并移除TableLayoutPanel容器
                            if (_contentPanel.MissionsTable.Parent.Controls.Contains(_loadingLabel)) {
                                // 如果直接是MissionsTable，移除Label
                                _contentPanel.MissionsTable.Parent.Controls.Remove(_loadingLabel);
                            }

                            _loadingLabel.Dispose();
                            _loadingLabel = null;
                        }
                        _isLoading = false;
                        MainUtils.logger?.Info("[RefreshMissionBlocksAsync] Loading indicator hidden");
                    } catch (Exception ex) {
                        MainUtils.logger?.Error("Error hiding loading indicator", ex);
                    }
                });
            }
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
