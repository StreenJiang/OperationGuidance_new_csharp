using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Constants;
using CustomLibrary.DateTimePickers;
using CustomLibrary.Panels;
using CustomLibrary.ComboBoxes;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using CustomLibrary.TextBoxes;
using CustomLibrary.Panels.BaseClasses;
using log4net.Config;
using log4net;
using System.Collections.Concurrent;

namespace CustomLibrary.Utils {
    public static class WidgetUtils {
        private static readonly Object _imageLocker = new();
        private static Dictionary<int, CustomMainMenuButton> _mainMenus = new();
        private static Dictionary<int, CustomChildMenuFirstButton> _childMenus = new();
        private static List<CustomContentPanel> _views = new();

        static WidgetUtils() {
            XmlConfigurator.Configure();
        }
        public static ILog GetLogger(Type type) => LogManager.GetLogger(type);

        public static Form MainForm { get; set; }
        public static CustomTabPanel? MainPanel { get; set; }
        public static CustomMainMenuPanel MainMenuPanel { get; set; }
        public static Size MainSize { get; private set; }
        public static Dictionary<int, CustomMainMenuButton> MainMenus { get => _mainMenus; set => _mainMenus = value; }
        public static CustomContentPanelBase CurrentPanel { get; set; }
        public static Func<bool>? CheckSavedFunc = null;
        private static bool _checkSaved = true;
        public static bool CheckSaved {
            get {
                _checkSaved = !(CheckSavedFunc != null && !CheckSavedFunc());
                return _checkSaved;
            }
            set {
                _checkSaved = value;
            }
        }

        /// <summary>
        /// P0级性能优化：基于MainSize的尺寸计算缓存
        /// 缓存所有基于MainSize的计算结果，当MainSize变化时自动清空
        /// </summary>
        private static readonly ConcurrentDictionary<string, object> _mainSizeCache = new();
        private static Size _cachedMainSize = Size.Empty;

        public static void RefreshMainSize(string resolution) {
            Size screenSize = WidgetUtils.GetScreenResolution();
            if (!string.IsNullOrEmpty(resolution)) {
                string[] strings = resolution.Split(",");
                int width = int.Parse(strings[0].Trim());
                int height = int.Parse(strings[1].Trim());
                if (width == screenSize.Width && height == screenSize.Height) {
                    MainSize = screenSize;
                } else {
                    MainSize = new(width, height);
                }
            } else {
                MainSize = screenSize;
            }

            // P0级优化：MainSize变化时清空缓存
            ClearMainSizeCache();
        }

        public static void RefreshMainSize(Size size) {
            MainSize = size;
            // P0级优化：MainSize变化时清空缓存
            ClearMainSizeCache();
        }

        /// <summary>
        /// P0级性能优化：清空MainSize相关缓存
        /// </summary>
        private static void ClearMainSizeCache() {
            _mainSizeCache.Clear();
            _cachedMainSize = MainSize;
        }

        /// <summary>
        /// P0级性能优化：获取缓存的尺寸值
        /// </summary>
        private static T GetCachedSizeValue<T>(string cacheKey, Func<T> calculateFunc) {
            // 如果MainSize发生变化，清空缓存
            if (_cachedMainSize != MainSize) {
                ClearMainSizeCache();
            }

            if (_mainSizeCache.TryGetValue(cacheKey, out var cachedValue)) {
                return (T) cachedValue;
            }

            T value = calculateFunc();
            _mainSizeCache[cacheKey] = value!;
            return value;
        }

        public static Size GetLoginViewSize(Size mainFormSize) {
            SizeRatioNRectColor sixteenNine = WidthHeightRatio.SixteenNine;
            Size loginViewSize;
            int widthPiece = mainFormSize.Width / sixteenNine.WidthRatio;
            int heightPiece = mainFormSize.Height / sixteenNine.HeightRatio;
            if (widthPiece > heightPiece) {
                loginViewSize = new(heightPiece * sixteenNine.WidthRatio, mainFormSize.Height);
            } else if (widthPiece < heightPiece) {
                loginViewSize = new(mainFormSize.Width, widthPiece * sixteenNine.HeightRatio);
            } else {
                loginViewSize = mainFormSize;
            }
            return loginViewSize;
        }
        public static Action<string>? RefreshLoginUserName;
        public static Action<bool>? BackToLoginView;

        public static void ClearViews() => _views.Clear();
        public static void AddView(CustomContentPanel view) => _views.Add(view);
        public static V GetView<V>() where V : CustomContentPanel {
            foreach (CustomContentPanel view in _views) {
                if (view.GetType() == typeof(V)) {
                    return (V) view;
                }
            }
            throw new NullReferenceException("Can not find view type <" + typeof(V) + ">, please check system config.");
        }

        public static void ClearMainMenus() => _mainMenus.Clear();
        public static void AddMainMenu(int menuKey, CustomMainMenuButton mainMenuButton) => _mainMenus.Add(menuKey, mainMenuButton);
        public static CustomMainMenuButton GetMainMenu(int menuKey) {
            if (_mainMenus.ContainsKey(menuKey)) {
                return _mainMenus[menuKey];
            }
            throw new NullReferenceException("Can not find main menu by key <" + menuKey + ">, please check system config.");
        }

        public static void ClearChildMenus() => _childMenus.Clear();
        public static void AddChildMenu(int menuKey, CustomChildMenuFirstButton childMenuButton) => _childMenus.Add(menuKey, childMenuButton);
        public static CustomChildMenuFirstButton GetChildMenu(int menuKey) {
            if (_childMenus.ContainsKey(menuKey)) {
                return _childMenus[menuKey];
            }
            throw new NullReferenceException("Can not find main menu by key <" + menuKey + ">, please check system config.");
        }

        public static Rectangle GetScreenWorkingArea() {
            return Screen.FromHandle(MainForm.Handle).WorkingArea;
        }
        public static Size GetScreenResolution() {
            return GetScreenWorkingArea().Size;
        }

        public static GraphicsPath RoundedRect(Rectangle bounds, int radius) {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0) {
                path.AddRectangle(bounds);
                return path;
            }

            // top left arc  
            path.AddArc(arc, 180, 90);

            // top right arc  
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom right arc  
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left arc 
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// P1级性能优化：图片缩放缓存
        /// 缓存缩放后的图片，避免重复的缩放计算
        /// </summary>
        private static readonly ConcurrentDictionary<string, Bitmap> _resizeImageCache = new();
        private const int MaxResizeCacheSize = 200;

        /// <summary>
        /// 生成图片缩放的唯一缓存键
        /// 结合图片的尺寸、像素格式和对象标识确保键的唯一性
        /// </summary>
        private static string GetResizeCacheKey(Image image, int newWidth, int newHeight) {
            // 使用对象ID和像素格式确保唯一性
            // 结合 Width、Height、PixelFormat 和 GetHashCode 确保缓存键唯一
            return $"{image.Width}x{image.Height}_{newWidth}x{newHeight}_{image.PixelFormat}_{image.GetHashCode():X8}";
        }

        /// <summary>
        /// Rescale image without losing quality
        /// </summary>
        /// <param name="image">Image will be rescaled.</param>
        /// <param name="newWidth">New width of new Image.</param>
        /// <param name="newHeight">New height of new Image.</param>
        /// <returns>New image witdh new size.</returns>
        public static Image ResizeImage(Image image, int newWidth, int newHeight) {
            // P1级优化：参数验证
            if (image == null) {
                throw new ArgumentNullException(nameof(image));
            }

            if (newWidth <= 0 || newHeight <= 0) {
                return image;
            }

            // 如果尺寸相同，直接返回原图（避免不必要的工作）
            if (image.Width == newWidth && image.Height == newHeight) {
                return image;
            }

            // P1级优化：使用缓存避免重复缩放
            string cacheKey = GetResizeCacheKey(image, newWidth, newHeight);

            if (_resizeImageCache.TryGetValue(cacheKey, out var cachedBitmap)) {
                try {
                    // 返回缓存图片的深拷贝，避免修改原始缓存
                    return DeepCopyBitmap(cachedBitmap);
                } catch {
                    // 如果深拷贝失败，继续执行正常流程
                }
            }

            // 执行缩放操作
            Bitmap resultImage = new Bitmap(newWidth, newHeight);
            try {
                using (Graphics g = Graphics.FromImage(resultImage)) {
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    // g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.InterpolationMode = InterpolationMode.Bilinear; // 用这个效率高很多，并且图片质量也不错
                    // g.InterpolationMode = InterpolationMode.NearestNeighbor; // 用这个效率更高，只是质量差些
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(image, new Rectangle(0, 0, newWidth, newHeight));
                }

                // P1级优化：缓存结果（限制大小避免内存泄漏）
                lock (_imageLocker) {
                    if (_resizeImageCache.Count >= MaxResizeCacheSize && !_resizeImageCache.ContainsKey(cacheKey)) {
                        // 移除最旧的缓存项
                        var oldestKey = _resizeImageCache.Keys.First();
                        _resizeImageCache.TryRemove(oldestKey, out var oldBitmap);
                        oldBitmap?.Dispose();
                    }
                }

                // 添加到缓存
                _resizeImageCache[cacheKey] = resultImage;

                // 返回图片的深拷贝，避免修改
                return DeepCopyBitmap(resultImage);
            } catch {
                // 如果缩放失败，清理资源并抛出
                resultImage.Dispose();
                throw;
            }
        }

        public static Image ResizeImage(Image image, Size newSize) {
            return ResizeImage(image, newSize.Width, newSize.Height);
        }

        /// <summary>
        /// P1级性能优化：深拷贝Bitmap
        /// </summary>
        private static Bitmap DeepCopyBitmap(Bitmap bitmap) {
            if (bitmap == null) {
                return null;
            }

            try {
                using (MemoryStream ms = new MemoryStream()) {
                    // 【增强健壮性】默认使用PNG格式（最安全，最兼容）
                    // PNG是无损格式，支持透明度，且所有.NET环境都支持
                    bitmap.Save(ms, ImageFormat.Png);

                    ms.Position = 0;
                    return new Bitmap(ms);
                }
            } catch (Exception ex) when (ex is ArgumentNullException || ex is ExternalException || ex is ArgumentException) {
                // 【多重回退】处理各种可能的异常

                try {
                    // 回退1：尝试使用Clone方法
                    var cloned = bitmap.Clone() as Bitmap;
                    if (cloned != null) {
                        return cloned;
                    }
                } catch {
                    // Clone失败，继续回退
                }

                try {
                    // 回退2：创建新的空白位图（保留尺寸和像素格式）
                    var newBitmap = new Bitmap(Math.Max(1, bitmap.Width), Math.Max(1, bitmap.Height), bitmap.PixelFormat);
                    using (var g = Graphics.FromImage(newBitmap)) {
                        g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                    }
                    return newBitmap;
                } catch {
                    // 所有回退都失败，返回最小尺寸的位图
                    return new Bitmap(1, 1);
                }
            }
        }

        /// <summary>
        /// P1级性能优化：清空图片缩放缓存
        /// </summary>
        public static void ClearResizeImageCache() {
            lock (_imageLocker) {
                foreach (var bitmap in _resizeImageCache.Values) {
                    bitmap?.Dispose();
                }
                _resizeImageCache.Clear();
            }
        }

        /// <summary>
        /// Get zooming ratio
        /// </summary>
        /// <param name="imageSize">Size of image.</param>
        /// <param name="size">Size of content.</param>
        /// <returns>Zooming ratio float value.</returns>        
        public static float GetZoomingRatio(Size imageSize, Size size) {
            int newWidth = size.Width;
            float originalRatio = (float) newWidth / imageSize.Width;
            int newHeight = (int) (imageSize.Height * originalRatio);
            if (newHeight > size.Height) {
                newHeight = size.Height;
                originalRatio = (float) newHeight / imageSize.Height;
                newWidth = (int) (imageSize.Width * originalRatio);
            }
            return originalRatio;
        }

        /// <summary>
        /// Resize image by zooming ratio
        /// </summary>
        /// <param name="image">Image that needs to be resized.</param>
        /// <param name="originalRatio">Zooming ratio.</param>
        /// <returns>New Image with new size.</returns>        
        public static Image ResizeImageByZoomingRatio(Image image, float originalRatio) {
            Size newSize = (image.Size * originalRatio).ToSize();
            if (newSize.Width <= 0) {
                newSize.Width = 1;
            }
            if (newSize.Height <= 0) {
                newSize.Height = 1;
            }
            return WidgetUtils.ResizeImage(image, newSize);
        }

        /// <summary>
        /// Crop image
        /// </summary>
        /// <param name="sourceImage">Image that needs to be resized.</param>
        /// <param name="width">Target width.</param>
        /// <param name="height">Target height.</param>
        /// <param name="offsetX">Offset x direction.</param>
        /// <param name="offsetY">Offset y direction.</param>
        /// <returns>New image after cropping.</returns>        
        public static Image CropImage(Image sourceImage, int width, int height, int offsetX, int offsetY) {
            Bitmap resultImage = new(width, height);
            using (Graphics g = Graphics.FromImage(resultImage)) {
                Rectangle resultRect = new(0, 0, width, height);
                Rectangle sourceRect = new(offsetX, offsetY, width, height);
                g.DrawImage(sourceImage, resultRect, sourceRect, GraphicsUnit.Pixel);
            }
            return resultImage;
        }

        /// <summary>
        /// Crop image
        /// </summary>
        /// <param name="sourceImage">Image that needs to be resized.</param>
        /// <param name="size">Target size.</param>
        /// <param name="offsetPoint">Offset point.</param>
        /// <returns>New image after cropping.</returns>        
        public static Image CropImage(Image sourceImage, Size size, Point offsetPoint) {
            return CropImage(sourceImage, size.Width, size.Height, offsetPoint.X, offsetPoint.Y);
        }

        /// <summary>
        /// Crop image
        /// </summary>
        /// <param name="sourceImage">Image that needs to be resized.</param>
        /// <param name="targetRect">Target size and point.</param>
        /// <returns>New image after cropping.</returns>        
        public static Image CropImage(Image sourceImage, Rectangle targetRect) {
            return CropImage(sourceImage, targetRect.Size, targetRect.Location);
        }

        private static void GetMaxSizeOfSizeRatio(out int maxWidthRatio, out int maxHeightRatio) {
            maxWidthRatio = 0;
            maxHeightRatio = 0;
            List<SizeRatioNRectColor>.Enumerator enumerator = WidthHeightRatio.GetEnumerator();
            while (enumerator.MoveNext()) {
                SizeRatioNRectColor current = enumerator.Current;
                int widthRatio = current.WidthRatio;
                if (widthRatio > maxWidthRatio) {
                    maxWidthRatio = widthRatio;
                }
                int heightRatio = current.HeightRatio;
                if (heightRatio > maxHeightRatio) {
                    maxHeightRatio = heightRatio;
                }
            }
        }

        public static Size GetMaxSizeOfSizeRatioByWidth(int contentWidth) {
            int maxWidthRatio = 0;
            int maxHeightRatio = 0;
            GetMaxSizeOfSizeRatio(out maxWidthRatio, out maxHeightRatio);

            int maxWidth = contentWidth;
            int maxHeight = (int) (maxWidth / (decimal) maxWidthRatio * maxHeightRatio);
            return new(maxWidth, maxHeight);
        }

        public static Size GetMaxSizeOfSizeRatioByHeight(int contentHeight) {
            int maxWidthRatio = 0;
            int maxHeightRatio = 0;
            GetMaxSizeOfSizeRatio(out maxWidthRatio, out maxHeightRatio);

            int maxWidth = (int) (contentHeight / (decimal) maxHeightRatio * maxWidthRatio);
            return new(maxWidth, contentHeight);
        }

        public static Rectangle ResizeRectangle(Rectangle rect, int newWidth, int newHeight) {
            return new(rect.Location, new(newWidth, newHeight));
        }
        public static Rectangle ResizeRectangle(Rectangle rect, Size newSize) {
            return new(rect.Location, newSize);
        }
        public static Rectangle ResizeRectangleByRatio(Rectangle rect, float ratio) {
            Size newSize = (rect.Size * ratio).ToSize();
            return new(rect.Location, newSize);
        }

        /// <summary>
        /// P0级性能优化：旋转图片缓存
        /// 缓存旋转后的图片，避免重复计算旋转操作
        /// </summary>
        internal static class RotatedImageCache {
            private static readonly ConcurrentDictionary<string, Image> _cache = new();
            private const int MaxCacheSize = 100;
            private static readonly LinkedList<string> _accessOrder = new();
            private static readonly object _lock = new();

            // 缓存命中统计
            private static int _hitCount = 0;
            private static int _missCount = 0;

            public static (int HitCount, int MissCount, double HitRate) Statistics {
                get {
                    int total = _hitCount + _missCount;
                    return (_hitCount, _missCount, total > 0 ? (double)_hitCount / total : 0.0);
                }
            }

            private static string GetCacheKey(Image image, float angle) {
                // 修复缓存混乱问题: 使用更唯一、更稳定的键生成方式
                // 添加图片的内存地址标识，确保即使尺寸和格式相同也不同对象
                // 使用 GetHashCode() 结合对象标识，确保唯一性
                int imageHash = image.GetHashCode();
                return $"{image.Width}x{image.Height}_{image.PixelFormat}_{imageHash}_{angle:F1}";
            }

            public static Image? GetRotatedImage(Image image, float angle, ILog? logger = null) {
                try {
                    // 验证图片是否有效
                    if (image == null) {
                        logger?.Warn("[GetRotatedImage] Null image provided for rotation");
                        return null;
                    }

                    if (image.Width <= 0 || image.Height <= 0) {
                        logger?.Warn($"[GetRotatedImage] Invalid image dimensions: {image.Width}x{image.Height}");
                        return null;
                    }

                    if (image.PixelFormat == System.Drawing.Imaging.PixelFormat.Undefined) {
                        logger?.Warn($"[GetRotatedImage] Invalid pixel format: {image.PixelFormat}");
                        return null;
                    }

                    string key = GetCacheKey(image, angle);
                    logger?.Debug($"[GetRotatedImage] Cache key: {key}");

                    // 尝试从缓存获取
                    if (_cache.TryGetValue(key, out var cachedImage)) {
                        // 修复警告问题 #7: 使用原子操作增加计数器
                        Interlocked.Increment(ref _hitCount);
                        UpdateAccessOrder(key);
                        logger?.Debug($"[GetRotatedImage] Cache hit for key: {key}");

                        // 修复缓存混乱问题: 返回缓存图片的深拷贝，避免原始图片被修改
                        if (cachedImage != null) {
                            return DeepCopyImage(cachedImage);
                        }
                    }

                    // 缓存未命中，执行旋转
                    // 修复警告问题 #7: 使用原子操作增加计数器
                    Interlocked.Increment(ref _missCount);
                    logger?.Debug($"[GetRotatedImage] Cache miss, rotating image {image.Size} by {angle} degrees");

                    var rotatedImage = RotateImageInternal(image, angle, logger);

                    if (rotatedImage != null) {
                        // 修复缓存混乱问题: 添加旋转后图片的深拷贝到缓存，确保独立性
                        var cachedCopy = DeepCopyImage(rotatedImage);
                        if (cachedCopy != null) {
                            AddToCache(key, cachedCopy);
                            logger?.Debug($"[GetRotatedImage] Rotation completed: {rotatedImage.Size}, added to cache");
                            // 返回原始旋转图片的深拷贝
                            return DeepCopyImage(rotatedImage);
                        }
                    } else {
                        logger?.Warn($"[GetRotatedImage] Rotation failed for image {image.Size}");
                    }

                    return rotatedImage;
                } catch (Exception ex) {
                    logger?.Error($"[GetRotatedImage] Error in GetRotatedImage: {ex.Message}", ex);
                    return null;
                }
            }

            /// <summary>
            /// 深拷贝图片，确保图片对象独立
            /// </summary>
            private static Image? DeepCopyImage(Image image) {
                if (image == null) {
                    return null;
                }

                try {
                    // 使用MemoryStream创建图片的独立副本
                    using (MemoryStream ms = new MemoryStream()) {
                        // 保存图片到流
                        image.Save(ms, image.RawFormat);
                        ms.Position = 0;
                        // 从流中重新创建图片
                        return Image.FromStream(ms);
                    }
                } catch {
                    // 如果深拷贝失败，返回null而不是原始图片引用
                    return null;
                }
            }

            private static Image? RotateImageInternal(Image image, float angle, ILog? logger) {
                try {
                    // 原图的宽和高
                    int w = image.Width;
                    int h = image.Height;

                    int W = w;
                    int H = h;

                    angle = angle % 360; // 弧度转换
                    double radian = angle * Math.PI / 180.0;
                    double cos = Math.Cos(radian);
                    double sin = Math.Sin(radian);

                    // INFO: need to varify
                    cos = Math.Round(cos, 10); // 保留 10 位小数
                    sin = Math.Round(sin, 10);

                    // Check for values
                    if (double.IsNaN(cos) || double.IsInfinity(cos) || double.IsNaN(sin) || double.IsInfinity(sin)) {
                        throw new ArgumentException("Cosine or sine value is invalid.");
                    }

                    long W_long = (long) (Math.Max(Math.Abs(w * cos - h * sin), Math.Abs(w * cos + h * sin)));
                    long H_long = (long) (Math.Max(Math.Abs(w * sin - h * cos), Math.Abs(w * sin + h * cos)));

                    // Check for values again
                    if (W_long > int.MaxValue || H_long > int.MaxValue) {
                        throw new ArgumentException("Calculated dimensions are too large.");
                    }

                    W = (int) W_long;
                    H = (int) H_long;

                    // Check for final values
                    if (W <= 0 || H <= 0) {
                        throw new ArgumentException("Calculated dimensions must be positive.");
                    }

                    // 修复严重问题 #2: 只有在计算成功后才创建Bitmap，避免内存泄漏
                    Image? dsImage = null;
                    try {
                        dsImage = new Bitmap(W, H);

                        using (Graphics g = Graphics.FromImage(dsImage)) {
                            g.InterpolationMode = InterpolationMode.Bilinear;
                            g.SmoothingMode = SmoothingMode.HighQuality;

                            // 计算偏移量
                            Point Offset = new Point((W - w) / 2, (H - h) / 2);

                            // 构造图像显示区域：让图像的中心与窗口的中心点一致
                            Rectangle rect = new Rectangle(Offset.X, Offset.Y, w, h);
                            Point center = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
                            g.TranslateTransform(center.X, center.Y);
                            g.RotateTransform(360 + angle);

                            // 恢复图像在水平和垂直方向的平移
                            g.TranslateTransform(-center.X, -center.Y);
                            g.DrawImage(image, rect);

                            // 重置绘图的所有变换
                            g.ResetTransform();
                            g.Save();
                        }

                        return dsImage;
                    } catch {
                        dsImage?.Dispose(); // 确保失败时释放
                        throw;
                    }
                } catch (Exception e) {
                    if (logger != null) {
                        logger.Error($"Error while rotating image, e = {e}");
                    }
                    return null;
                }
            }

            private static void AddToCache(string key, Image image) {
                lock (_lock) {
                    // 如果缓存已满，移除最久未使用的项
                    if (_cache.Count >= MaxCacheSize && !_cache.ContainsKey(key)) {
                        RemoveOldestItem();
                    }

                    _cache[key] = image;
                    _accessOrder.AddLast(key);
                }
            }

            private static void UpdateAccessOrder(string key) {
                lock (_lock) {
                    _accessOrder.Remove(key);
                    _accessOrder.AddLast(key);
                }
            }

            private static void RemoveOldestItem() {
                lock (_lock) {
                    if (_accessOrder.First != null) {
                        var oldest = _accessOrder.First.Value;
                        _accessOrder.RemoveFirst();
                        _cache.TryRemove(oldest, out var cachedImage);
                        cachedImage?.Dispose();
                    }
                }
            }

            /// <summary>
            /// 清空缓存
            /// </summary>
            public static void Clear() {
                lock (_lock) {
                    foreach (var image in _cache.Values) {
                        image?.Dispose();
                    }
                    _cache.Clear();
                    _accessOrder.Clear();
                    _hitCount = 0;
                    _missCount = 0;
                }
            }

            /// <summary>
            /// 修复缓存混乱问题: 清除所有旋转图片缓存
            /// 当源图片被修改时，需要清除所有相关的旋转缓存
            /// </summary>
            public static void ClearRotatedImageCache() {
                lock (_lock) {
                    foreach (var image in _cache.Values) {
                        image?.Dispose();
                    }
                    _cache.Clear();
                    _accessOrder.Clear();
                    // 注意: RotatedImageCache类中没有logger字段，仅清除缓存不记录日志
                }
            }
        }

        /// <summary>
        /// 修复缓存混乱问题: 清空旋转图片缓存
        /// 当源图片被修改时调用，清除所有旋转缓存
        /// </summary>
        public static void ClearRotatedImageCache() {
            RotatedImageCache.Clear();
        }

        /// <summary>
        /// P0级性能优化：旋转图片（使用缓存）
        /// 优先使用缓存机制，避免重复计算旋转操作
        /// </summary>
        public static Image RotateImage(Image image, float angle, ILog? logger = null) {
            // 标准化角度（处理360度倍数）
            angle = angle % 360;
            if (angle < 0) {
                angle += 360;
            }

            // 常见角度（0, 90, 180, 270）优先使用缓存优化
            if (angle == 0 || angle == 90 || angle == 180 || angle == 270) {
                return RotatedImageCache.GetRotatedImage(image, angle, logger) ?? image;
            }

            // 其他角度直接计算（不缓存）
            return RotateImageInternal(image, angle, logger) ?? image;
        }

        /// <summary>
        /// 内部方法：执行图片旋转（无缓存）
        /// </summary>
        private static Image RotateImageInternal(Image image, float angle, ILog? logger = null) {
            // 原图的宽和高
            int w = image.Width;
            int h = image.Height;

            int W = w;
            int H = h;
            try {
                angle = angle % 360; // 弧度转换
                double radian = angle * Math.PI / 180.0;
                double cos = Math.Cos(radian);
                double sin = Math.Sin(radian);

                // INFO: need to varify
                cos = Math.Round(cos, 10); // 保留 10 位小数
                sin = Math.Round(sin, 10);

                // Check for values
                if (double.IsNaN(cos) || double.IsInfinity(cos) || double.IsNaN(sin) || double.IsInfinity(sin)) {
                    throw new ArgumentException("Cosine or sine value is invalid.");
                }

                long W_long = (long) (Math.Max(Math.Abs(w * cos - h * sin), Math.Abs(w * cos + h * sin)));
                long H_long = (long) (Math.Max(Math.Abs(w * sin - h * cos), Math.Abs(w * sin + h * cos)));

                // Check for values again
                if (W_long > int.MaxValue || H_long > int.MaxValue) {
                    throw new ArgumentException("Calculated dimensions are too large.");
                }

                W = (int) W_long;
                H = (int) H_long;

                // Check for final values
                if (W <= 0 || H <= 0) {
                    throw new ArgumentException("Calculated dimensions must be positive.");
                }

                // 修复严重问题 #3: 只有在计算成功后才创建Bitmap，避免内存泄漏
                Bitmap? dsImage = null;
                try {
                    dsImage = new Bitmap(W, H);
                    using (Graphics g = Graphics.FromImage(dsImage)) {
                        g.InterpolationMode = InterpolationMode.Bilinear;
                        g.SmoothingMode = SmoothingMode.HighQuality;

                        // 计算偏移量
                        Point Offset = new Point((W - w) / 2, (H - h) / 2);

                        // 构造图像显示区域：让图像的中心与窗口的中心点一致
                        Rectangle rect = new Rectangle(Offset.X, Offset.Y, w, h);
                        Point center = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
                        g.TranslateTransform(center.X, center.Y);
                        g.RotateTransform(360 + angle);

                        // 恢复图像在水平和垂直方向的平移
                        g.TranslateTransform(-center.X, -center.Y);
                        g.DrawImage(image, rect);

                        // 重置绘图的所有变换
                        g.ResetTransform();
                        g.Save();
                    }
                    return dsImage;
                } catch (Exception e) {
                    logger?.Error($"Error rotating image, e = {e}");
                    dsImage?.Dispose(); // 确保失败时释放
                    throw;
                }
            } catch (Exception e) {
                if (logger != null) {
                    logger.Error($"Error while rotating image, e = {e}");
                }
                throw; // 修复严重问题 #3: 使用 throw 保持原始异常堆栈跟踪
            }
        }

        // Check if type if a sub class of T
        public static bool IsSubClass<T>(Type? type) {
            if (type == null)
                // Null it's not a sub class of any type of courese
                return false;
            Type superType = typeof(T);
            if (type.Name == superType.Name) {
                Type[] superGenericTypes = superType.GenericTypeArguments;
                Type[] types = type.GenericTypeArguments;
                if (superGenericTypes.Length == types.Length) {
                    for (int i = 0; i < superGenericTypes.Length; i++) {
                        // Check generic type
                        Type utilItself = typeof(WidgetUtils);
                        string methodName = "IsSubClass";
                        MethodInfo? methodItself = utilItself.GetMethod(methodName);
                        if (methodItself != null) {
                            methodItself = methodItself.MakeGenericMethod(superGenericTypes[i]);
                            // Call self recursively to check generic types
                            Object? isSubClassObj = methodItself.Invoke(utilItself, new object[] { types[i] });
                            if (isSubClassObj != null) {
                                bool isSubClass = (bool) isSubClassObj;
                                if (!isSubClass) {
                                    // Found any difference, then it's not a sub class of T
                                    return false;
                                }
                            }
                        } else {
                            throw new MethodAccessException("Method <" + methodName + "> not found, please check the code.");
                        }
                    }
                    // All types are the same, make it a sub class of T
                    return true;
                }
                // Generic types' length must be then same, otherwise it's not a sub class of T
                return false;
            }
            // If current type if not the same as T, should check it's base type by recursion
            return IsSubClass<T>(type.BaseType);
        }

        // Measure size of string
        public static Size MeasureString(string? text, Font font) {
            if (string.IsNullOrEmpty(text)) {
                return new(0, font.Height);
            }
            return TextRenderer.MeasureText(text, font, new(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
            //
            // GraphicsPath path = new();
            // path.AddString(text, font.FontFamily, (int) font.Style, font.SizeInPoints, new Point(0, 0), StringFormat.GenericDefault);
            // return path.GetBounds().Size.ToSize();
        }
        // Content configs 
        public static Font GetProperFont(Size containerSize, string text, float fontInitRatio) {
            return GetProperFont(containerSize, text, fontInitRatio, .95F);
        }
        public static Font GetProperFont(Size containerSize, string text, float fontInitRatio, float maxRatio) {
            Font font = new Font(WidgetsConfigs.SystemFontFamily, containerSize.Height * fontInitRatio, FontStyle.Bold, GraphicsUnit.Pixel);
            if (MeasureString(text, font).Width >= containerSize.Width * maxRatio) {
                font = GetProperFont(containerSize, text, fontInitRatio -= .005f, maxRatio);
            }
            return font;
        }
        public static int ScrollBarThickness() {
            // P0级优化：使用缓存避免重复计算
            return GetCachedSizeValue("ScrollBarThickness", () => {
                int thickness = MainSize.Height / 46;
                if (thickness < 12) {
                    thickness = 12;
                }
                return thickness;
            });
        }

        public static int ContentTitleHeight() {
            // P0级优化：使用缓存避免重复计算
            return GetCachedSizeValue("ContentTitleHeight", () => (int) (MainSize.Height * .06));
        }

        public static int ContentInnerBorderMargin() {
            // P0级优化：使用缓存避免重复计算
            return GetCachedSizeValue("ContentInnerBorderMargin", () => (MainSize.Width + MainSize.Height) / 350);
        }

        public static int ContentInnerBorderMargin(int width, int height) => (width + height) / 350;

        public static Padding ContentPadding() {
            // P0级优化：使用缓存避免重复计算
            return GetCachedSizeValue("ContentPadding", () => {
                int hPadding = (int) (MainSize.Width * .015);
                int vPadding = (int) (MainSize.Height * .03);
                return new Padding(hPadding, vPadding, hPadding, vPadding);
            });
        }

        public static int ContainerRadius() {
            // P0级优化：使用缓存避免重复计算
            return GetCachedSizeValue("ContainerRadius", () => (int) (MainSize.Height * .015));
        }

        public static int ControlRadius() {
            // P0级优化：使用缓存避免重复计算
            return GetCachedSizeValue("ControlRadius", () => (int) (MainSize.Height * .00925));
        }

        public static int TextOrComboBoxHeight() {
            // P0级优化：使用缓存避免重复计算
            return GetCachedSizeValue("TextOrComboBoxHeight", () => (int) (MainSize.Height * .0425));
        }

        public static int CommonButtonHeight() {
            // P0级优化：使用缓存避免重复计算
            return GetCachedSizeValue("CommonButtonHeight", () => (int) (MainSize.Height * .0425));
        }

        public static int PictureBoxGroupBaseHeight() {
            // P0级优化：使用缓存避免重复计算
            return GetCachedSizeValue("PictureBoxGroupBaseHeight", () => (int) (MainSize.Height * .125));
        }

        public static int BorderThickness() {
            // P0级优化：使用缓存避免重复计算
            return GetCachedSizeValue("BorderThickness", () => {
                int thickness = (MainSize.Width + MainSize.Height) / 1200;
                return thickness > 0 ? thickness : 1;
            });
        }
        // Pop up / floating form configs 
        public static int PopUpOrFloatingFormMaxHeight() => (int) (MainSize.Height * .8);
        public static int PopUpOrFloatingFormTitle() => (int) (MainSize.Height * .0475);
        public static int PopUpOrFloatingFormSubTitle() => (int) (MainSize.Height * .0475);
        public static int PopUpOrFloatingFormTextOrComboBoxHeight() => (int) (MainSize.Height * .035);
        public static int PopUpOrFloatingFormCommonButtonHeight() => (int) (MainSize.Height * .035);
        public static Padding PopUpOrFloatingFormContentPadding() {
            int hPadding = (int) (MainSize.Width * .015);
            int vPadding = (int) (MainSize.Height * .03);
            return new(hPadding, vPadding, hPadding, vPadding);
        }
        public static Padding PopUpOrFloatingFormButtonsPadding() {
            int hPadding = (int) (MainSize.Width * .008);
            int vPadding = (int) (MainSize.Height * .008);
            return new(hPadding, 0, hPadding, vPadding);
        }
        // Grid view configs
        public static int GridViewHeaderHeight() => (int) (MainSize.Height * .0425);
        public static int GridViewContentRowHeight() => (int) (MainSize.Height * .0385);
        public static int GridViewContentColumnMaxWidth() => (int) (MainSize.Width * .2);
        public static int GridViewPageInfoHeight() => (int) (MainSize.Height * .03);
        public static float GridViewColumnsPaddingRatio() => .5F;
        // Workplace configs
        public static float WorkplaceTopBarHeightRatio() => .07F;
        public static float WorkplaceBarCodeHeightRatio() => .05F;
        public static float WorkplaceLeftWidthRatio() => .575F;
        public static float WorkplaceMiddleWidthRatio() => .2F;
        public static float WorkplaceImagePanelHeightRatio() => .5F;
        public static int WorkplaceBoxOrButtonHeightRatio() => (int) (MainSize.Height * .034);
        public static int WorkplaceGridViewHeaderHeight() => (int) (MainSize.Height * .035);
        public static int WorkplaceGridViewContentRowHeight() => (int) (MainSize.Height * .0325);
        public static int WorkplaceGridViewPageInfoHeight() => (int) (MainSize.Height * .025);
        public static float WorkplaceGridViewColumnsPaddingRatio() => .2F;

        /// <summary>
        /// 得到一个等差数列
        /// 等差数列求和公式: S = (a1 + an) * n / 2
        /// </summary>
        /// <param name="sum">等差数列的和</param>
        /// <param name="step">一共有几项</param>
        /// <param name="a1">首项</param>
        /// <returns>返回一个等差数列</returns>
        public static List<int> ArithmeticProgression(double sum, int step, double a1) {
            List<int> result = new();

            // 计算尾项
            double an = Math.Round(sum * 2 / step - a1);
            // 计算公差
            double d = Math.Round((an - a1) / (step - 1));
            // 计算等差数列中的每一项
            for (int i = 1; i <= step; i++) {
                double a_n = Math.Round(a1 + (i - 1) * d);
                result.Add((int) a_n);
            }
            // 返回数列
            return result;
        }

        /// <summary>
        /// 根据给定的滚动条实际需要的值及滚动条的滚动块占滚动条的比率，求出滚动块的值
        /// </summary>
        /// <param name="heightDiff">需要用到滚动条的content的高度差（像素值）</param>
        /// <param name="sliderRatio">滚动块占整个滚动条的比例</param>
        /// <returns></returns>
        public static int CalculateScrollBarSlider(int heightDiff, double sliderRatio) {
            return (int) (heightDiff * sliderRatio / (1 - sliderRatio));
        }
        public static void CalculateScrollBar(ScrollBar scrollBar, int scrollBarLength, int contentLength) {
            int heightDiff = contentLength - scrollBarLength;
            if (heightDiff > 0) {
                double sliderRatio = scrollBarLength / (double) contentLength;
                int sliderHeight = CalculateScrollBarSlider(heightDiff, sliderRatio);
                scrollBar.Maximum = heightDiff + sliderHeight;
                scrollBar.SmallChange = sliderHeight / 15;
                scrollBar.LargeChange = sliderHeight;
            }
        }

        /// <summary>
        /// P0级性能优化：颜色计算缓存
        /// 缓存颜色变暗计算结果，避免重复计算
        /// </summary>
        private static readonly ConcurrentDictionary<string, Color> _darkenColorCache = new();
        /// <summary>
        /// P0级性能优化：颜色计算缓存
        /// 缓存颜色变亮计算结果，避免重复计算
        /// </summary>
        private static readonly ConcurrentDictionary<string, Color> _lightenColorCache = new();

        /// <summary>
        /// 颜色缓存的最大大小限制
        /// 防止缓存无限增长导致内存泄漏
        /// </summary>
        private const int MaxColorCacheSize = 500;

        /// <summary>
        /// 添加项到颜色缓存，超过限制时移除最旧的项
        /// </summary>
        private static void AddToColorCache<T>(ConcurrentDictionary<string, T> cache, string key, T value) {
            // 如果缓存已满，移除最旧的项（使用TryRemove的第一个键）
            if (cache.Count >= MaxColorCacheSize) {
                var oldestKey = cache.Keys.FirstOrDefault();
                if (oldestKey != null) {
                    cache.TryRemove(oldestKey, out _);
                }
            }
            cache[key] = value;
        }

        public static Color LightColor(Color color, double ratio) {
            if (ratio < 0 || ratio > 1) {
                throw new ArgumentException("Ratio must be between 0 ~ 1");
            }

            // P0级优化：使用缓存避免重复计算
            string cacheKey = $"{color.A}_{color.R}_{color.G}_{color.B}_{ratio:F3}";
            if (_lightenColorCache.TryGetValue(cacheKey, out var cachedColor)) {
                return cachedColor;
            }

            int newR = (int) Math.Round(color.R + (255 - color.R) * ratio);
            int newG = (int) Math.Round(color.G + (255 - color.G) * ratio);
            int newB = (int) Math.Round(color.B + (255 - color.B) * ratio);
            Color result = Color.FromArgb(newR, newG, newB);

            // 缓存结果（使用固定大小限制，避免内存泄漏）
            AddToColorCache(_lightenColorCache, cacheKey, result);

            return result;
        }

        public static Color DarkenColor(Color color, double ratio) {
            if (ratio < 0 || ratio > 1) {
                throw new ArgumentException("Ratio must be between 0 ~ 1");
            }

            // P0级优化：使用缓存避免重复计算
            string cacheKey = $"{color.A}_{color.R}_{color.G}_{color.B}_{ratio:F3}";
            if (_darkenColorCache.TryGetValue(cacheKey, out var cachedColor)) {
                return cachedColor;
            }

            int newR = (int) Math.Round(color.R - color.R * ratio);
            int newG = (int) Math.Round(color.G - color.G * ratio);
            int newB = (int) Math.Round(color.B - color.B * ratio);
            Color result = Color.FromArgb(newR, newG, newB);

            // 缓存结果（使用固定大小限制，避免内存泄漏）
            AddToColorCache(_darkenColorCache, cacheKey, result);

            return result;
        }

        public static bool ShowConfirmPopUp(string message) => MessageBox.Show(MainForm != null && MainForm.IsHandleCreated && !MainForm.IsDisposed ? MainForm : null, message, "请确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        public static DialogResult ShowNoticePopUp(string message) => MessageBox.Show(MainForm != null && MainForm.IsHandleCreated && !MainForm.IsDisposed ? MainForm : null, message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        public static DialogResult ShowWarningPopUp(string message) => MessageBox.Show(MainForm != null && MainForm.IsHandleCreated && !MainForm.IsDisposed ? MainForm : null, message, "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        public static DialogResult ShowErrorPopUp(string message) => MessageBox.Show(MainForm != null && MainForm.IsHandleCreated && !MainForm.IsDisposed ? MainForm : null, message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);

        private static Point controlOriginalLocation;
        private static Point mouseDownLocation;
        private static bool mouseLeftDown = false;
        public static void MakeControlDraggable(Control dragControl, Control moveControl) {
            dragControl.MouseDown += (sender, eventArgs) => {
                if (dragControl.IsDisposed || moveControl.IsDisposed) {
                    return;
                }
                if (!mouseLeftDown && eventArgs.Button == MouseButtons.Left) {
                    mouseDownLocation = eventArgs.Location;
                    controlOriginalLocation = moveControl.Location;
                    mouseLeftDown = true;
                }
            };
            dragControl.MouseMove += (sender, eventArgs) => {
                if (dragControl.IsDisposed || moveControl.IsDisposed) {
                    return;
                }
                if (mouseLeftDown && eventArgs.Button == MouseButtons.Left) {
                    Point locationOffsetExtra = new(eventArgs.Location.X - mouseDownLocation.X, eventArgs.Location.Y - mouseDownLocation.Y);
                    controlOriginalLocation.Offset(locationOffsetExtra);
                    moveControl.Location = controlOriginalLocation;
                }
            };
            dragControl.MouseUp += (sender, eventArgs) => {
                if (dragControl.IsDisposed || moveControl.IsDisposed) {
                    return;
                }
                if (mouseLeftDown && eventArgs.Button == MouseButtons.Left) {
                    mouseLeftDown = false;
                }
            };
        }

        public static CustomTextBoxGroup AddTextBox<T, V>(Control parent, T t, string boxName, bool numberOnly, Action<T, V?>? propertySetter) {
            CustomTextBoxGroup boxGroup = new(boxName) {
                Parent = parent,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                NumberOnly = numberOnly,
            };
            if (propertySetter != null) {
                boxGroup.GetTextBox(0).Box.TextChanged += (sender, eventArgs) => HandleTextChanged(t, boxGroup, 0, propertySetter);
            }
            return boxGroup;
        }
        public static CustomTextBoxGroup AddSeparateTextBox<T, V>(Control parent, T t, string boxName, string separator, bool numberOnly, Action<T, V?>? propertySetter1, Action<T, V?>? propertySetter2) {
            CustomTextBoxGroup boxGroup = new(boxName) {
                Parent = parent,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                Separator = separator,
                NumberOnly = numberOnly,
            };
            // Need two boxes
            boxGroup.AddTextBox();
            if (propertySetter1 != null) {
                boxGroup.GetTextBox(0).Box.TextChanged += (sender, eventArgs) => HandleTextChanged(t, boxGroup, 0, propertySetter1);
            }
            if (propertySetter2 != null) {
                boxGroup.GetTextBox(1).Box.TextChanged += (sender, eventArgs) => HandleTextChanged(t, boxGroup, 1, propertySetter2);
            }
            return boxGroup;
        }
        public static void HandleTextChanged<T, V>(T t, CustomTextBoxGroup boxGroup, int index, Action<T, V?> propertySetter) {
            string valueStr = boxGroup.GetTextBox(index).Text;
            try {
                V? value;
                if (valueStr != null && valueStr != string.Empty && valueStr != "") {
                    Type? type = Nullable.GetUnderlyingType(typeof(V?));
                    if (type != null) {
                        value = (V?) Convert.ChangeType(valueStr, type);
                    } else {
                        value = (V?) Convert.ChangeType(valueStr, typeof(V?));
                    }
                } else {
                    value = default(V?);
                }
                propertySetter(t, value);
            } catch (Exception e) {
                System.Console.WriteLine($"{boxGroup.TextName}. Can not convert string[{valueStr}] to type<{typeof(V)}>. Exception: {e}");
            }
        }
        public static CustomComboBoxGroup<V> AddComboBox<T, V>(Control parent, T t, string boxName, Action<T, V?>? propertySetter, Dictionary<string, V> items) {
            CustomComboBoxGroup<V> boxGroup = new(boxName) {
                Parent = parent,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
            };
            if (propertySetter != null) {
                boxGroup.ItemSelected += () => propertySetter(t, boxGroup.Value);
            }
            Dictionary<string, V>.Enumerator enumerator = items.GetEnumerator();
            while (enumerator.MoveNext()) {
                KeyValuePair<string, V> current = enumerator.Current;
                boxGroup.AddItem(current.Key, current.Value);
            }
            return boxGroup;
        }
        public static CustomDatePickerGroup AddDatePicker<T>(Control parent, T t, string boxName, Action<T, DateTime?>? propertySetter) {
            CustomDatePickerGroup pickerGroup = new(boxName) {
                Parent = parent,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
            };
            if (propertySetter != null) {
                pickerGroup.GetPicker(0).ValueChanged += (sender, eventArgs) => propertySetter(t, pickerGroup.GetPicker(0).Value);
            }
            return pickerGroup;
        }
        public static CustomDatePickerGroup AddSeparateDatePicker<T>(Control parent, T t, string boxName, string separator, Action<T, DateTime?>? propertySetter1, Action<T, DateTime?>? propertySetter2) {
            CustomDatePickerGroup pickerGroup = new(boxName) {
                Parent = parent,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
            };
            pickerGroup.AddPicker();
            if (propertySetter1 != null) {
                pickerGroup.GetPicker(0).ValueChanged += (sender, eventArgs) => propertySetter1(t, pickerGroup.GetPicker(0).Value);
            }
            if (propertySetter2 != null) {
                pickerGroup.GetPicker(1).ValueChanged += (sender, eventArgs) => propertySetter2(t, pickerGroup.GetPicker(1).Value);
            }
            return pickerGroup;
        }
        public static ToggleButtonGroup AddToggleButton<T>(Control parent, T t, string toggleButtonName, Action<T, bool>? propertySetter) {
            ToggleButtonGroup toggleButton = new(toggleButtonName) {
                Parent = parent,
            };
            if (propertySetter != null) {
                toggleButton.CheckedChanged += (sender, eventArgs) => propertySetter(t, toggleButton.Checked);
            }
            return toggleButton;
        }
        public static PictureBoxGroup AddPictureBox<T>(Control parent, T t, string boxName, Action<T, Image>? imageSetter, Action<T, string>? fileNameSetter) {
            PictureBoxGroup pictureBoxGroup = new(boxName) {
                Parent = parent,
                ForeColorExpectButton = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
            };
            pictureBoxGroup.ImageChanged += () => {
                if (imageSetter != null) {
                    imageSetter(t, pictureBoxGroup.Image);
                }
                if (fileNameSetter != null) {
                    fileNameSetter(t, pictureBoxGroup.FileName);
                }
            };
            return pictureBoxGroup;
        }


        public static double GetTimeMillisec(DateTime time) {
            return time.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }
    }
}
