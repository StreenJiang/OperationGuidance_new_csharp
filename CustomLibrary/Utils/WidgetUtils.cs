using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Constants;
using CustomLibrary.DateTimePickers;
using CustomLibrary.Panels;
using CustomLibrary.ComboBoxes;
using System.Drawing.Drawing2D;
using System.Reflection;
using CustomLibrary.TextBoxes;
using CustomLibrary.Panels.BaseClasses;
using log4net.Config;
using log4net;

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
        }
        public static void RefreshMainSize(Size size) => MainSize = size;

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
        /// Rescale image without losing quality
        /// </summary>
        /// <param name="image">Image will be rescaled.</param>
        /// <param name="newWidth">New width of new Image.</param>
        /// <param name="newHeight">New height of new Image.</param>
        /// <returns>New image witdh new size.</returns>        
        public static Image ResizeImage(Image image, int newWidth, int newHeight, bool dispose = false) {
            lock (_imageLocker) {
                if (newWidth <= 0 || newHeight <= 0) {
                    Bitmap bitmap = new Bitmap(image);
                    if (dispose) {
                        image.Dispose();
                    }
                    return bitmap;
                }

                Bitmap resultImage = new Bitmap(newWidth, newHeight);
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

                if (dispose) {
                    image.Dispose();
                }
                return resultImage;
            }
        }
        public static Image ResizeImage(Image image, Size newSize, bool dispose = false) {
            return ResizeImage(image, newSize.Width, newSize.Height, dispose);
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

        public static Image RotateImage(Image image, float angle, ILog? logger = null, bool dispose = true) {
            // 原图的宽和高
            int w = image.Width;
            int h = image.Height;
            Image dsImage = new Bitmap(w, h);

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

                // 目标位图
                dsImage = new Bitmap(W, H);
            } catch (Exception e) {
                if (logger != null) {
                    logger.Error($"Error while rotating image, e = {e}");
                }
                throw e;
            } finally {
                try {
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
                } catch (Exception e) {
                    if (logger != null) {
                        logger.Error($"Error while rotating image in finally block, e = {e}");
                    }
                    throw e;
                }
            }

            if (dispose) {
                image.Dispose();
            }
            return dsImage;
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
            int thickness = MainSize.Height / 46;
            if (thickness < 12) {
                thickness = 12;
            }
            return thickness;
        }
        public static int ContentTitleHeight() => (int) (MainSize.Height * .06);
        public static int ContentInnerBorderMargin() => (MainSize.Width + MainSize.Height) / 350;
        public static int ContentInnerBorderMargin(int width, int height) => (width + height) / 350;
        public static Padding ContentPadding() {
            int hPadding = (int) (MainSize.Width * .015);
            int vPadding = (int) (MainSize.Height * .03);
            return new(hPadding, vPadding, hPadding, vPadding);
        }
        public static int ContainerRadius() => (int) (MainSize.Height * .015);
        public static int ControlRadius() => (int) (MainSize.Height * .00925);
        public static int TextOrComboBoxHeight() => (int) (MainSize.Height * .0425);
        public static int CommonButtonHeight() => (int) (MainSize.Height * .0425);
        public static int PictureBoxGroupBaseHeight() => (int) (MainSize.Height * .125);
        public static int BorderThickness() {
            int thickness = (MainSize.Width + MainSize.Height) / 1200;
            return thickness > 0 ? thickness : 1;
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

        public static Color LightColor(Color color, double ratio) {
            if (ratio < 0 || ratio > 1) {
                throw new ArgumentException("Ratio must be between 0 ~ 1");
            }
            int newR = (int) Math.Round(color.R + (255 - color.R) * ratio);
            int newG = (int) Math.Round(color.G + (255 - color.G) * ratio);
            int newB = (int) Math.Round(color.B + (255 - color.B) * ratio);
            return Color.FromArgb(newR, newG, newB);
        }

        public static Color DarkenColor(Color color, double ratio) {
            if (ratio < 0 || ratio > 1) {
                throw new ArgumentException("Ratio must be between 0 ~ 1");
            }
            int newR = (int) Math.Round(color.R - color.R * ratio);
            int newG = (int) Math.Round(color.G - color.G * ratio);
            int newB = (int) Math.Round(color.B - color.B * ratio);
            return Color.FromArgb(newR, newG, newB);
        }

        internal const int PopUpDefaultCountdownSeconds = 2;

        private static Control? SafeMainForm =>
            MainForm != null && MainForm.IsHandleCreated && !MainForm.IsDisposed ? MainForm : null;

        public static bool ShowConfirmPopUp(string message) => ShowConfirmPopUp(SafeMainForm, message);
        public static DialogResult ShowNoticePopUp(string message, int? countdownSeconds = null) => ShowNoticePopUp(SafeMainForm, message, countdownSeconds);
        public static DialogResult ShowWarningPopUp(string message, int? countdownSeconds = null) => ShowWarningPopUp(SafeMainForm, message, countdownSeconds);
        public static DialogResult ShowErrorPopUp(string message, int? countdownSeconds = null) => ShowErrorPopUp(SafeMainForm, message, countdownSeconds);

        public static bool ShowConfirmPopUp(Control? mainForm, string message) {
            if (mainForm != null) {
                IAsyncResult asyncResult = mainForm.BeginInvoke(() => {
                    return MessageBox.Show(message, "请确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
                });
                return (bool) mainForm.EndInvoke(asyncResult);
            }
            return MessageBox.Show(message, "请确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        public static DialogResult ShowNoticePopUp(Control? mainForm, string message, int? countdownSeconds = null)
            => ShowPopUp(mainForm, message, "提示", MessageBoxIcon.Information, countdownSeconds);
        public static DialogResult ShowWarningPopUp(Control? mainForm, string message, int? countdownSeconds = null)
            => ShowPopUp(mainForm, message, "警告", MessageBoxIcon.Warning, countdownSeconds);
        public static DialogResult ShowErrorPopUp(Control? mainForm, string message, int? countdownSeconds = null)
            => ShowPopUp(mainForm, message, "错误", MessageBoxIcon.Error, countdownSeconds);

        private static DialogResult ShowPopUp(Control? mainForm, string message, string title, MessageBoxIcon icon, int? countdownSeconds) {
            if (countdownSeconds != null && countdownSeconds > 0) {
                return ShowCountdownPopUp(mainForm, message, title, icon, countdownSeconds.Value);
            }
            if (mainForm != null) {
                IAsyncResult asyncResult = mainForm.BeginInvoke(() => {
                    return MessageBox.Show(mainForm, message, title, MessageBoxButtons.OK, icon);
                });
                return (DialogResult) mainForm.EndInvoke(asyncResult);
            }
            return MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
        }

        private static DialogResult ShowCountdownPopUp(Control? mainForm, string message, string title, MessageBoxIcon icon, int countdownSeconds) {
            if (mainForm != null) {
                IAsyncResult asyncResult = mainForm.BeginInvoke(() => {
                    using (var form = new CountdownPopUpForm(message, title, icon, countdownSeconds)) {
                        return form.ShowDialog(mainForm);
                    }
                });
                return (DialogResult) mainForm.EndInvoke(asyncResult);
            }
            using (var form = new CountdownPopUpForm(message, title, icon, countdownSeconds)) {
                return form.ShowDialog();
            }
        }

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

        public static string GetBaseDirectory() {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string visualStudioDebugPath = "\\OperationGuidance_new\\bin\\Debug\\net6.0-windows";
            if (baseDirectory.Contains(visualStudioDebugPath)) {
                baseDirectory = baseDirectory.Replace(visualStudioDebugPath, "");
            }
            string visualStudioDebugPath2 = "\\bin\\Debug\\net6.0-windows";
            if (baseDirectory.Contains(visualStudioDebugPath2)) {
                baseDirectory = baseDirectory.Replace(visualStudioDebugPath2, "");
            }
            return baseDirectory;
        }
    }
}
