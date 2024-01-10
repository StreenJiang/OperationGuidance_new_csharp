using CustomLibrary.Buttons;
using CustomLibrary.Panels;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace CustomLibrary.Utils {
    public static class WidgetUtils {
        private static Dictionary<int, CustomMainMenuButton> _mainMenus = new();
        private static Dictionary<int, CustomChildMenuFirstButton> _childMenus = new();
        private static List<CustomContentPanel> _views = new();

        public static CustomTabPanel MainPanel { get; set; }
        public static CustomMainMenuPanel MainMenuPanel { get; set; }

        public static void AddView(CustomContentPanel view) => _views.Add(view);
        public static V GetView<V>() where V : CustomContentPanel {
            foreach (CustomContentPanel view in _views) {
                if (view.GetType() == typeof(V)) {
                    return (V) view;
                }
            }
            throw new NullReferenceException("Can not find view type <" + typeof(V) + ">, please check system config.");
        }

        public static void AddMainMenu(int menuKey, CustomMainMenuButton mainMenuButton) => _mainMenus.Add(menuKey, mainMenuButton);
        public static CustomMainMenuButton GetMainMenu(int menuKey) {
            if (_mainMenus.ContainsKey(menuKey)) {
                return _mainMenus[menuKey];
            }
            throw new NullReferenceException("Can not find main menu by key <" + menuKey + ">, please check system config.");
        }

        public static void AddChildMenu(int menuKey, CustomChildMenuFirstButton childMenuButton) => _childMenus.Add(menuKey, childMenuButton);
        public static CustomChildMenuFirstButton GetChildMenu(int menuKey) {
            if (_childMenus.ContainsKey(menuKey)) {
                return _childMenus[menuKey];
            }
            throw new NullReferenceException("Can not find main menu by key <" + menuKey + ">, please check system config.");
        }

        public static Size GetScreenResolution() {
            return Screen.PrimaryScreen.Bounds.Size;
        }

        /// <summary>
        /// Rescale image without losing quality
        /// </summary>
        /// <param name="image">Image will be rescaled.</param>
        /// <param name="newWidth">New width of new Image.</param>
        /// <param name="newHeight">New height of new Image.</param>
        /// <returns>New image witdh new size.</returns>        
        public static Image ResizeImageWithoutLosingQuality(Image image, int newWidth, int newHeight) {
            if (newWidth <= 0 || newHeight <= 0) {
                return image;
            }
            Bitmap resultImage = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(resultImage)) {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(image, new Rectangle(0, 0, newWidth, newHeight));
            }
            return resultImage;
        }
        public static Image ResizeImageWithoutLosingQuality(Image image, Size newSize) {
            return ResizeImageWithoutLosingQuality(image, newSize.Width, newSize.Height);
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

        public static Image RotateImage(Image image, float angle) {
            angle = angle % 360; // 弧度转换
            double radian = angle * Math.PI / 180.0;
            double cos = Math.Cos(radian);
            double sin = Math.Sin(radian);
            // 原图的宽和高
            int w = image.Width;
            int h = image.Height;
            int W = (int)(Math.Max(Math.Abs(w * cos - h * sin), Math.Abs(w * cos + h * sin)));
            int H = (int)(Math.Max(Math.Abs(w * sin - h * cos), Math.Abs(w * sin + h * cos)));
            // 目标位图
            Image dsImage = new Bitmap(W, H);
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
                            Object? isSubClassObj = methodItself.Invoke(utilItself, new object[]{ types[i] });
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

        // Content configs 
        public static int ContentTitle() => (int) (MainPanel.Height * .052);
        public static int ContentPadding(int width, int height) => (width + height) / 350;
        public static int TextOrComboBoxHeight() => (int) (MainPanel.Height * .036);
        public static int CommonButtonHeight() => (int) (MainPanel.Height * .036);
        public static int BorderThickness() {
            Control mainControl = MainPanel.Parent;
            int thickness = (mainControl.Width + mainControl.Height) / 1200;
            return thickness > 0 ? thickness : 1;
        }
        // Pop up form configs 
        public static int PopUpFormTitle() => (int) (MainPanel.Height * .04);
        public static Padding PopUpFormContentPadding() {
            int hPadding = (int) (MainPanel.Width * .015);
            int vPadding = (int) (MainPanel.Height * .03);
            return new(hPadding, vPadding, hPadding, vPadding);
        }
        public static Padding PopUpFormButtonsPadding() {
            int hPadding = (int) (MainPanel.Width * .008);
            int vPadding = (int) (MainPanel.Height * .008);
            return new(hPadding, 0, hPadding, vPadding);
        }
        // Grid view configs
        public static int GridViewHeaderRowHeight() => (int) (MainPanel.Height * .036);
        public static int GridViewContentRowHeight() => (int) (MainPanel.Height * .034);
        public static int GridViewPageInfoHeight() => (int) (MainPanel.Height * .03);

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
        /// <param name="realHeight">需要用到滚动条的content的高度差（像素值）</param>
        /// <param name="sliderRatio">滚动块占整个滚动条的比例</param>
        /// <returns></returns>
        public static int CalculateScrollBarSlider(int realHeight, double sliderRatio) {
            return (int) (realHeight * sliderRatio / (1 - sliderRatio));
        }

        public static Color LighterColor(Color color, double ratio) {
            if (ratio < 0 || ratio > 1) {
                throw new ArgumentException("Ratio must be between 0 ~ 1");
            }
            int newR = (int) Math.Round(color.R + (255 - color.R) * ratio);
            int newG = (int) Math.Round(color.G + (255 - color.G) * ratio);
            int newB = (int) Math.Round(color.B + (255 - color.B) * ratio);
            return Color.FromArgb(newR, newG, newB);
        }

        public static Color DarkerColor(Color color, double ratio) {
            if (ratio < 0 || ratio > 1) {
                throw new ArgumentException("Ratio must be between 0 ~ 1");
            }
            int newR = (int) Math.Round(color.R - color.R * ratio);
            int newG = (int) Math.Round(color.G - color.G * ratio);
            int newB = (int) Math.Round(color.B - color.B * ratio);
            return Color.FromArgb(newR, newG, newB);
        }

        public static void ShowNoticePopUp(string message) {
            MessageBox.Show(null, message, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
