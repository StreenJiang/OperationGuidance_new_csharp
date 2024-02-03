using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Constants;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
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
            return WidgetUtils.ResizeImageWithoutLosingQuality(image, newSize);
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
        public static int ContentInnerBorderMargin(int width, int height) => (width + height) / 350;
        public static Padding ContentPadding() {
            int hPadding = (int) (MainPanel.Width * .015);
            int vPadding = (int) (MainPanel.Height * .03);
            return new(hPadding, vPadding, hPadding, vPadding);
        }
        public static int TextOrComboBoxHeight() => (int) (MainPanel.Height * .036);
        public static int CommonButtonHeight() => (int) (MainPanel.Height * .036);
        public static int PictureBoxGroupBaseHeight() => (int) (MainPanel.Height * .125);
        public static int BorderThickness() {
            Control mainControl = MainPanel.Parent;
            int thickness = (mainControl.Width + mainControl.Height) / 1200;
            return thickness > 0 ? thickness : 1;
        }
        // Pop up / floating form configs 
        public static int PopUpOrFloatingFormTitle() => (int) (MainPanel.Height * .04);
        public static int PopUpOrFloatingFormSubTitle() => (int) (MainPanel.Height * .0475);
        public static Padding PopUpOrFloatingFormContentPadding() {
            int hPadding = (int) (MainPanel.Width * .015);
            int vPadding = (int) (MainPanel.Height * .03);
            return new(hPadding, vPadding, hPadding, vPadding);
        }
        public static Padding PopUpOrFloatingFormButtonsPadding() {
            int hPadding = (int) (MainPanel.Width * .008);
            int vPadding = (int) (MainPanel.Height * .008);
            return new(hPadding, 0, hPadding, vPadding);
        }
        // Grid view configs
        public static int GridViewHeaderRowHeight() => (int) (MainPanel.Height * .036);
        public static int GridViewContentRowHeight() => (int) (MainPanel.Height * .034);
        public static int GridViewContentColumnMaxWidth() => (int) (MainPanel.Width * .2);
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

        public static bool ShowConfirmPopUp(string message) {
            DialogResult result = MessageBox.Show(null, message, "请确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            return result == DialogResult.Yes;
        }
        public static DialogResult ShowNoticePopUp(string message) {
            return MessageBox.Show(null, message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        public static DialogResult ShowWarningPopUp(string message) {
            return MessageBox.Show(null, message, "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        public static DialogResult ShowErrorPopUp(string message) {
            return MessageBox.Show(null, message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static Point controlOriginalLocation;
        private static Point mouseDownLocation;
        private static bool mouseLeftDown = false;
        public static void MakeControlDraggable(Control dragControl, Control moveControl) {
            dragControl.MouseDown += (sender, eventArgs) => {
                if (!mouseLeftDown && eventArgs.Button == MouseButtons.Left) {
                    mouseDownLocation = eventArgs.Location;
                    controlOriginalLocation = moveControl.Location;
                    mouseLeftDown = true;
                }
            };
            dragControl.MouseMove += (sender, eventArgs) => {
                if (mouseLeftDown && eventArgs.Button == MouseButtons.Left) {
                    Point locationOffsetExtra = new(eventArgs.Location.X - mouseDownLocation.X, eventArgs.Location.Y - mouseDownLocation.Y);
                    controlOriginalLocation.Offset(locationOffsetExtra);
                    moveControl.Location = controlOriginalLocation;
                }
            };
            dragControl.MouseUp += (sender, eventArgs) => {
                if (mouseLeftDown && eventArgs.Button == MouseButtons.Left) {
                    mouseLeftDown = false;
                }
            };
        }

        public static CustomTextBoxGroup AddTextBox<T, V>(Control parent, T t, string boxName, bool numberOnly, Action<T, V?> propertySetter) {
            CustomTextBoxGroup boxGroup = new(boxName) {
                Parent = parent,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
                NumberOnly = numberOnly,
            };
            boxGroup.GetTextBox(0).Box.TextChanged += (sender, eventArgs) => HandleTextChanged(t, boxGroup, 0, propertySetter);
            return boxGroup;
        }
        public static CustomTextBoxGroup AddSeparateTextBox<T, V>(Control parent, T t, string boxName, string separator, bool numberOnly, Action<T, V?> propertySetter1, Action<T, V?> propertySetter2) {
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
            boxGroup.GetTextBox(0).Box.TextChanged += (sender, eventArgs) => HandleTextChanged(t, boxGroup, 0, propertySetter1);
            boxGroup.GetTextBox(1).Box.TextChanged += (sender, eventArgs) => HandleTextChanged(t, boxGroup, 1, propertySetter2);
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
            } catch (InvalidCastException e) {
                System.Console.WriteLine($"{boxGroup.TextName}. Can not convert string[{valueStr}] to type<{typeof(V)}>. Exception: {e}");
            }
        }
        public static CustomComboBoxGroup<V> AddComboBox<T, V>(Control parent, T t, string boxName, Action<T, V?> propertySetter, Dictionary<string, V> items) {
            CustomComboBoxGroup<V> boxGroup = new(boxName) {
                Parent = parent,
                BorderColor = ColorConfigs.COLOR_TEXT_BOX_BORDER,
                ForeColor = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ColorConfigs.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ColorConfigs.COLOR_TEXT_BOX_BORDER_ERROR,
            };
            boxGroup.ItemSelected += () => propertySetter(t, boxGroup.Value);
            Dictionary<string, V>.Enumerator enumerator = items.GetEnumerator();
            while (enumerator.MoveNext()) {
                KeyValuePair<string, V> current = enumerator.Current;
                boxGroup.AddItem(current.Key, current.Value);
            }
            return boxGroup;
        }
        public static ToggleButtonGroup AddToggleButton<T>(Control parent, T t, string toggleButtonName, Action<T, bool> propertySetter) {
            ToggleButtonGroup toggleButton = new(toggleButtonName) {
                Parent = parent,
            };
            toggleButton.CheckedChanged += (sender, eventArgs) => propertySetter(t, toggleButton.Checked);
            return toggleButton;
        }
        public static PictureBoxGroup AddPictureBox<T>(Control parent, T t, string boxName, Action<T, Image> imageSetter, Action<T, string> fileNameSetter) {
            PictureBoxGroup pictureBoxGroup = new(boxName) {
                Parent = parent,
                ForeColorExpectButton = ColorConfigs.COLOR_TEXT_BOX_FOREGROUND,
            };
            pictureBoxGroup.ImageChanged += () => {
                imageSetter(t, pictureBoxGroup.Image);
                fileNameSetter(t, pictureBoxGroup.FileName);
            };
            return pictureBoxGroup;
        }
    }
}
