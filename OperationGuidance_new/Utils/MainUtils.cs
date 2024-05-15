using System.Data.Common;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using CustomLibrary.Constants;
using CustomLibrary.Utils;
using log4net;
using log4net.Config;
using Newtonsoft.Json;
using OperationGuidance_new.Attributes;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Database;
using OperationGuidance_service.Exceptions;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using RJCP.IO.Ports;

namespace OperationGuidance_new.Utils {
    public static class MainUtils {
        public static readonly int DBRetryTimes = 2;
        public static AppVersion Version { get; set; } = AppVersion.STANDARD;

        public static LoginView LoginView { get; set; }
        public static bool LoginFlag { get; set; } = true;
        public static Action? ActionAfterLogout { get; set; }
        public static string? LastProductBatch { get; set; }

        public static readonly string DATETIME_FORMAT_YYYY_MM_DD_CHINESE = "yyyy年MM月dd ddd HH:mm:ss";
        public static readonly string DATETIME_FORMAT_FULL_NO_PUNCTUATION = "yyyyMMddHHmmss";

        public static readonly string DATETIME_FORMAT_YYYY_MM_DD_HH_MM_SS = "yyyy-MM-dd HH:mm:ss";
        public static readonly string DATETIME_FORMAT_YYYY_MM_DD_HH_MM_SS_FFF = "yyyy-MM-dd HH:mm:ss.fff";

        public static readonly string DATETIME_FORMAT_YYYY_MM = "yyyy-MM";
        public static readonly string DATETIME_FORMAT_YYYY_MM_2 = "yyyy/MM";

        public static readonly string DATETIME_FORMAT_YYYY_MM_DDD = "yyyy-MM_ddd";
        public static readonly string DATETIME_FORMAT_YYYY_MM_DDD_2 = "yyyy/MM_ddd";

        public static readonly string DATETIME_FORMAT_YYYY_MM_DD = "yyyy-MM-dd";
        public static readonly string DATETIME_FORMAT_YYYY_MM_DD_2 = "yyyy/MM/dd";

        public static readonly string DATETIME_FORMAT_YYYY_MM_DD_DDD = "yyyy-MM-dd_ddd";
        public static readonly string DATETIME_FORMAT_YYYY_MM_DD_DDD_2 = "yyyy/MM/dd_ddd";

        static MainUtils() {
            XmlConfigurator.Configure();
        }
        public static bool AppRunning { get; internal set; } = true;
        public static ILog GetLogger(Type type) => LogManager.GetLogger(type);

        public static void CheckDBConnection() {
            Form formPopup = new Form() {
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.None,
                Size = new(300, 100),
            };
            Label label = new() {
                Parent = formPopup,
                Text = "正在连接数据库，请稍后...",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
            };
            formPopup.Show();

            DbConnection? dbConnection = null;
            int tryTimes = 0;

            while (tryTimes <= DBRetryTimes) {
                try {
                    dbConnection = DbConnector.GetConnection();
                    if (dbConnection != null) {
                        break;
                    }
                } catch (DatabaseException de) {
                    GetLogger(typeof(MainUtils)).Error($"Can not connect to DB, please check DB config or network status. Error message: {de}");
                    continue;
                } finally {
                    tryTimes++;
                }
            }

            formPopup.Dispose();
            if (dbConnection == null) {
                throw new DatabaseException("数据库连接失败，请检查数据库配置或网络连接状态");
            }
        }

        private static IniFileUtil Settings { get; } = new();
        public static List<string> InvalidCharacters { get; } = new() {
            "\u0000","\u0001","\u0002","\u0003","\u0004","\u0005","\u0006","\u0007","\u0008",
            "\u000B","\u000C",
            "\u000E","\u000F","\u0010","\u0011","\u0012","\u0013","\u0014","\u0015","\u0016",
            "\u0017","\u0018","\u0019","\u001A","\u001B","\u001C","\u001D","\u001E","\u001F"
        };

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

        private static string GetProductImagesPath() {
            string productImagesPath = GetBaseDirectory() + "\\ProductImages";
            if (!Directory.Exists(productImagesPath)) {
                DirectoryInfo directoryInfo = Directory.CreateDirectory(productImagesPath);
                directoryInfo.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }
            return productImagesPath;
        }
        public static string GenerateProductImageName() {
            return $"ProductSideImage_{DateTime.Now.ToString(DATETIME_FORMAT_FULL_NO_PUNCTUATION)}.png";
        }
        public static Image? GetProductImage(string? fileName) {
            if (string.IsNullOrEmpty(fileName)) {
                return null;
            }
            string imageFilePath = GetProductImagesPath() + "\\" + fileName;
            if (!File.Exists(imageFilePath)) {
                return null;
            }
            // 这个很奇怪，只会画出图片的一部分，真奇葩
            // 将图片转化成字节，然后再将字节转化为一个图片对象，防止对图片文件本身锁死
            // using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(imageFilePath)))
            // using (Bitmap bitmap = new Bitmap(ms)) {
            //     Bitmap newBitmap = new(bitmap.Width, bitmap.Height, bitmap.PixelFormat);
            //     using (Graphics g = Graphics.FromImage(newBitmap)) {
            //         g.DrawImage(bitmap, Point.Empty);
            //         g.Flush();
            //     }
            //     return newBitmap;
            // }

            Bitmap bitmap = new Bitmap(imageFilePath);
            Image? image = CommonUtils.ImageBase64ToImage(CommonUtils.ImageToBase64(bitmap));
            bitmap.Dispose();
            return image;
        }
        public static void SaveProductImage(Image? image, string? fileName) {
            if (image == null || string.IsNullOrEmpty(fileName)) {
                return;
            }
            string imageFilePath = GetProductImagesPath() + "\\" + fileName;

            // 如果图片已存在则替换旧图片
            if (File.Exists(imageFilePath)) {
                File.Delete(imageFilePath);
            }
            image.Save(imageFilePath);
        }

        // Settings
        // Resolution
        public static Size GetSettingResolution() {
            Size size;
            string resolution = Settings.Read(IniFileKeys.Resolution);
            if (!string.IsNullOrEmpty(resolution)) {
                string[] strings = resolution.Split(",");
                int width = int.Parse(strings[0].Trim());
                int height = int.Parse(strings[1].Trim());
                size = new(width, height);
            } else {
                size = GetDefaultSettingResolution();
                SetSettingResolution(size);
            }
            return size;
        }
        public static Size GetDefaultSettingResolution() => WidgetUtils.GetScreenWorkingArea().Size;
        public static void SetSettingResolution(Size newSize) => Settings.Write(IniFileKeys.Resolution, $"{newSize.Width}, {newSize.Height}");
        // Storage file name format
        public static string GetStorageFileName() {
            string nameFormat = Settings.Read(IniFileKeys.DataStorageNameFormat);
            if (string.IsNullOrEmpty(nameFormat)) {
                nameFormat = GetDefaultStorageFileName();
                SetStorageFileName(nameFormat);
            }
            return nameFormat;
        }
        public static string GetDefaultStorageFileName() => DATETIME_FORMAT_YYYY_MM_DD;
        public static void SetStorageFileName(string nameFormat) => Settings.Write(IniFileKeys.DataStorageNameFormat, nameFormat);
        public static string GetStorageFormattedName() {
            DateTime now = DateTime.Now;
            string nameFormatted = GetStorageFileName();
            if (Replace(DATETIME_FORMAT_YYYY_MM_DD_DDD)) { } else if (Replace(DATETIME_FORMAT_YYYY_MM_DD)) { } else if (Replace(DATETIME_FORMAT_YYYY_MM_DDD)) { } else if (Replace(DATETIME_FORMAT_YYYY_MM)) { }
            return nameFormatted;

            bool Replace(string formatPattern) {
                if (nameFormatted.Contains(formatPattern)) {
                    nameFormatted = nameFormatted.Replace(formatPattern, now.ToString(formatPattern)).Replace(" ", "");
                    return true;
                }
                return false;
            }
        }
        // Storage path
        public static string GetStoragePath() {
            string dataStoragePath = Settings.Read(IniFileKeys.DataStoragePath);
            if (string.IsNullOrEmpty(dataStoragePath)) {
                dataStoragePath = GetDefaultStoragePath();
                SetStoragePath(dataStoragePath);
            }
            return dataStoragePath;
        }
        public static string GetDefaultStoragePath() {
            string defaultPath = GetBaseDirectory() + "OperationDataStorage\\";
            // 如果文件夹不存在，则创建文件夹
            if (!Directory.Exists(defaultPath)) {
                Directory.CreateDirectory(defaultPath);
            }
            return defaultPath;
        }
        public static void SetStoragePath(string newPath) => Settings.Write(IniFileKeys.DataStoragePath, newPath);
        // Fields sort config
        public static List<int> GetSortConfig() {
            List<int>? sortConfig = null;
            string dataStorageFields = Settings.Read(IniFileKeys.DataStorageFieldsSort);
            sortConfig = JsonConvert.DeserializeObject<List<int>>(dataStorageFields);
            if (sortConfig == null) {
                sortConfig = GetDefaultSortConfig();
                SetSortConfig(sortConfig);
            }
            return sortConfig;
        }
        public static List<int> GetDefaultSortConfig() => new List<int>() { 44, 14, 20, 18, 17, 15, 24, 22, 21, 16, 13, 11, 10, 45, 46, 47, 48 };
        public static void SetSortConfig(List<int> fieldsSortConfig) => Settings.Write(IniFileKeys.DataStorageFieldsSort, JsonConvert.SerializeObject(fieldsSortConfig));
        // Fields sort config current
        public static List<int>? GetSortConfigCurr() => JsonConvert.DeserializeObject<List<int>>(Settings.Read(IniFileKeys.DataStorageFieldsSortCurr));
        public static void SetSortConfigCurr(List<int>? fieldsSortConfigCurr) => Settings.Write(IniFileKeys.DataStorageFieldsSortCurr, JsonConvert.SerializeObject(fieldsSortConfigCurr));
        public static List<OperationDataField> GetOperationDataFields(List<int>? sortConfig = null) {
            List<PropertyInfo> props = typeof(OperationDataVO).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
            List<OperationDataField> fields = new();
            int index = 1;
            props.ForEach(p => {
                IEnumerable<Attribute> enumerable = p.GetCustomAttributes();
                foreach (Attribute attribute in enumerable) {
                    if (attribute is GridColumnAttribute gridColumn) {
                        string fieldName;
                        if (gridColumn.ColumnName != null && gridColumn.ColumnName != string.Empty) {
                            fieldName = gridColumn.ColumnName;
                        } else {
                            fieldName = p.Name;
                        }
                        string propertyName = p.Name;
                        fields.Add(new(index++, fieldName, propertyName, false));
                    }
                }
            });
            // Get config
            if (sortConfig == null) {
                sortConfig = GetSortConfig();
            }
            fields = fields.OrderBy(f => {
                int indexTemp = sortConfig.IndexOf(f.Id);
                if (indexTemp == -1) {
                    indexTemp = fields.Count;
                }
                return indexTemp;
            }).ToList();
            fields.ForEach(f => {
                if (sortConfig.IndexOf(f.Id) != -1) {
                    f.Visible = true;
                }
            });
            return fields;
        }
        // Store loosening data
        public static bool GetStoreLooseningData() {
            string storeLooseningData = Settings.Read(IniFileKeys.DataStorageStoreLooseningData);
            if (string.IsNullOrEmpty(storeLooseningData)) {
                bool flag = GetDefaultStoreLooseningData();
                SetStoreLooseningData(flag);
                return flag;
            }
            return int.Parse(storeLooseningData) == (int) YesOrNo.YES;
        }
        public static bool GetDefaultStoreLooseningData() => true;
        public static void SetStoreLooseningData(bool flag) {
            if (flag) {
                Settings.Write(IniFileKeys.DataStorageStoreLooseningData, (int) YesOrNo.YES + "");
            } else {
                Settings.Write(IniFileKeys.DataStorageStoreLooseningData, (int) YesOrNo.NO + "");
            }
        }
        // Arm locating enabled
        public static bool IsArmLocatingEnabled() {
            string armLocatingEnabled = Settings.Read(IniFileKeys.MissionArmLocatingEnabled);
            if (string.IsNullOrEmpty(armLocatingEnabled)) {
                bool flag = DefaultIsArmLocatingEnabled();
                SetArmLocatingEnabled(flag);
                return flag;
            }
            return int.Parse(armLocatingEnabled) == (int) YesOrNo.YES;
        }
        public static bool DefaultIsArmLocatingEnabled() => true;
        public static void SetArmLocatingEnabled(bool flag) {
            if (flag) {
                Settings.Write(IniFileKeys.MissionArmLocatingEnabled, (int) YesOrNo.YES + "");
            } else {
                Settings.Write(IniFileKeys.MissionArmLocatingEnabled, (int) YesOrNo.NO + "");
            }
        }
        // Arm locating accuracy
        public static int GetArmLocatingAccuracy() {
            string armLocatingAccuracy = Settings.Read(IniFileKeys.MissionArmLocatingAccuracy);
            if (string.IsNullOrEmpty(armLocatingAccuracy)) {
                int accuracy = GetDefaultArmLocatingAccuracy();
                SetArmLocatingAccuracy(accuracy);
                return accuracy;
            }
            return int.Parse(armLocatingAccuracy);
        }
        public static int GetDefaultArmLocatingAccuracy() => 100;
        public static void SetArmLocatingAccuracy(int accuracy) => Settings.Write(IniFileKeys.MissionArmLocatingAccuracy, accuracy + "");
        // Prodcut batch notice
        public static bool IsProductBatchNoticeEnabled() {
            string productBatchNoticeEnabled = Settings.Read(IniFileKeys.MissionProductBatchNotice);
            if (string.IsNullOrEmpty(productBatchNoticeEnabled)) {
                bool flag = DefaultIsProductBatchNoticeEnabled();
                SetProductBatchNoticeEnabled(flag);
                return flag;
            }
            return int.Parse(productBatchNoticeEnabled) == (int) YesOrNo.YES;
        }
        public static bool DefaultIsProductBatchNoticeEnabled() => true;
        public static void SetProductBatchNoticeEnabled(bool flag) {
            if (flag) {
                Settings.Write(IniFileKeys.MissionProductBatchNotice, (int) YesOrNo.YES + "");
            } else {
                Settings.Write(IniFileKeys.MissionProductBatchNotice, (int) YesOrNo.NO + "");
            }
        }

        // Ping util method
        public static bool PingHost(string nameOrAddress) {
            Ping? pinger = null;
            try {
                pinger = new();
                PingReply pingReply = pinger.Send(IPAddress.Parse(nameOrAddress), 2500);
                bool pingResult = pingReply.Status == IPStatus.Success;
                return pingResult;
            } catch (PingException pe) {
                System.Console.WriteLine($"Ping error while pinging to [{nameOrAddress}]: {pe}");
            } finally {
                if (pinger != null) {
                    pinger.Dispose();
                }
            }
            return false;
        }

        private static Dictionary<int, ArmTask> _armTasks = new();
        public static Dictionary<int, ArmTask> ArmTasks => _armTasks;
        public static void NewArmTask(int armId, string? armName, string ip, int port, DeviceTypeArm arm) {
            ArmTask task = new(armId, armName, ip, port, arm);
            task.Connect();
            _armTasks.Add(armId, task);
        }
        public static async Task<ArmTask> NewArmTaskAsync(int armId, string? armName, string ip, int port, DeviceTypeArm arm) {
            ArmTask task = new(armId, armName, ip, port, arm);
            await task.ConnectAsync();
            _armTasks.Add(armId, task);
            return task;
        }
        public static ArmTask GetArmTask(int armId) {
            if (_armTasks.ContainsKey(armId)) {
                return _armTasks[armId];
            }
            throw new ArgumentException($"ArmTask for armId<{armId}> has not been created.");
        }
        public static ArmTask? TryGetArmTask(int armId) {
            if (_armTasks.ContainsKey(armId)) {
                return _armTasks[armId];
            }
            return null;
        }

        private static Dictionary<int, ToolTask> _toolTasks = new();
        public static Dictionary<int, ToolTask> ToolTasks => _toolTasks;
        public static void NewToolTask(int toolId, string? toolName, string ip, int port, DeviceTypeTool tool) {
            ToolTask task = new(toolId, toolName, ip, port, tool);
            task.Connect();
            _toolTasks.Add(toolId, task);
        }
        public static async Task<ToolTask> NewToolTaskAsync(int toolId, string? toolName, string ip, int port, DeviceTypeTool tool) {
            ToolTask task = new(toolId, toolName, ip, port, tool);
            await task.ConnectAsync();
            _toolTasks.Add(toolId, task);
            return task;
        }
        public static ToolTask GetToolTask(int toolId) {
            if (_toolTasks.ContainsKey(toolId)) {
                return _toolTasks[toolId];
            }
            throw new ArgumentException($"ToolTask for toolId<{toolId}> has not been created.");
        }
        public static ToolTask? TryGetToolTask(int toolId) {
            if (_toolTasks.ContainsKey(toolId)) {
                return _toolTasks[toolId];
            }
            return null;
        }

        private static Dictionary<int, SerialPortTask> _serialPortTasks = new();
        public static Dictionary<int, SerialPortTask> SerialPortTasks => _serialPortTasks;
        public static void NewSerialPortTask(int serialPortId, string fullName,
                string portName, int baudRate, Parity parity, int dataBits,
                StopBits stopBits, DataTypes dataType, DeviceTypeSerialPort serialPort) {
            SerialPortTask task = new(serialPortId, fullName, portName, baudRate, parity, dataBits, stopBits, dataType, serialPort);
            task.Connect();
            _serialPortTasks.Add(serialPortId, task);
        }
        public static async Task<SerialPortTask> NewSerialPortTaskAsync(int serialPortId, string fullName,
                string portName, int baudRate, Parity parity, int dataBits,
                StopBits stopBits, DataTypes dataType, DeviceTypeSerialPort serialPort) {
            SerialPortTask task = new(serialPortId, fullName, portName, baudRate, parity, dataBits, stopBits, dataType, serialPort);
            await task.ConnectAsync();
            _serialPortTasks.Add(serialPortId, task);
            return task;
        }
        public static SerialPortTask GetSerialPortTask(int serialPortId) {
            if (_serialPortTasks.ContainsKey(serialPortId)) {
                return _serialPortTasks[serialPortId];
            }
            throw new ArgumentException($"SerialPortTask for serialPortId<{serialPortId}> has not been created.");
        }
        public static SerialPortTask? TryGetSerialPortTask(int serialPortId) {
            if (_serialPortTasks.ContainsKey(serialPortId)) {
                return _serialPortTasks[serialPortId];
            }
            return null;
        }

        private static Dictionary<int, CommunicationTask> _communicationTasks = new();
        public static Dictionary<int, CommunicationTask> CommunicationTasks => _communicationTasks;
        public static void NewCommunicationTask(int communicationId, string? communicationName, string ip, int port, DeviceTypeCommunication communication) {
            CommunicationTask task = new(communicationId, communicationName, ip, port, communication);
            task.Connect();
            _communicationTasks.Add(communicationId, task);
        }
        public static async Task<CommunicationTask> NewCommunicationTaskAsync(int communicationId, string? communicationName, string ip, int port, DeviceTypeCommunication communication) {
            CommunicationTask task = new(communicationId, communicationName, ip, port, communication);
            await task.ConnectAsync();
            _communicationTasks.Add(communicationId, task);
            return task;
        }
        public static CommunicationTask GetCommunicationTask(int communicationId) {
            if (_communicationTasks.ContainsKey(communicationId)) {
                return _communicationTasks[communicationId];
            }
            throw new ArgumentException($"CommunicationTask for communicationId<{communicationId}> has not been created.");
        }
        public static CommunicationTask? TryGetCommunicationTask(int communicationId) {
            if (_communicationTasks.ContainsKey(communicationId)) {
                return _communicationTasks[communicationId];
            }
            return null;
        }

        public static List<string> LogCache { get; } = new();
        private static TextBox? _textArea = null;
        public static TextBox? EventLogTextArea {
            get => _textArea;
            set {
                _textArea = value;
                if (_textArea != null && LogCache.Count > 0) {
                    WidgetUtils.MainForm.BeginInvoke(() => {
                        LogCache.ForEach(message => {
                            _textArea.AppendText(message + "\r\n");
                        });
                        LogCache.Clear();
                    });
                }
            }
        }

        public static void Log(string message, bool printToView = true) {
            if (printToView) {
                if (_textArea != null) {
                    _textArea.BeginInvoke(() => {
                        _textArea.AppendText(message + "\r\n");
                    });
                } else {
                    LogCache.Add(message);
                }
            }
        }
        public static void Info(ILog logger, string message, bool printToView = true) {
            Log(message, printToView);
            logger.Info(message);
        }
        public static void Warn(ILog logger, string message, bool printToView = true) {
            Log(message, printToView);
            logger.Info(message);
        }
        public static void Error(ILog logger, string message, bool printToView = true) {
            Log(message, printToView);
            logger.Info(message);
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
        public static Size GetProperSizeAccordingToSizeRatio(Size contentSize, Size size)
            => GetProperSizeAccordingToSizeRatio(contentSize, size.Width, size.Height);
        public static Size GetProperSizeAccordingToSizeRatio(Size contentSize, int width, int height) {
            int newWidth = contentSize.Width;
            int newHeight = (int) (height / ((decimal) width / newWidth));
            if (newHeight > contentSize.Height) {
                newHeight = contentSize.Height;
                newWidth = (int) (width / ((decimal) height / newHeight));
            }
            return new(newWidth, newHeight);
        }

        public static string CheckKeyPosition(string keyPosition) {
            string errorMsg = "";
            for (int i = 0; i < keyPosition.Length; i++) {
                char c = keyPosition[i];
                if (!char.IsDigit(c) && c != ',' && c != '-' && c != ' ') {
                    errorMsg += "条码关键位匹配格式错误。请使用','或'-'隔开";
                    break;
                }
                if (c == '-') {
                    if (i == 0 || i == keyPosition.Length - 1) {
                        errorMsg += "符号'-'不能在开头或结尾";
                        break;
                    } else if (!char.IsDigit(keyPosition[i - 1]) || !char.IsDigit(keyPosition[i + 1])) {
                        errorMsg += "符号'-'前后必须是数字";
                        break;
                    } else {
                        int prevIndex = i - 1;
                        string prev = keyPosition[prevIndex].ToString();
                        while (prevIndex != 0) {
                            prevIndex--;
                            if (!char.IsDigit(keyPosition[prevIndex])) {
                                break;
                            }
                            prev += keyPosition[prevIndex].ToString();
                        }
                        int prevNum = int.Parse(prev);

                        int nextIndex = i + 1;
                        string follow = keyPosition[nextIndex].ToString();
                        while (nextIndex != keyPosition.Length - 1) {
                            nextIndex++;
                            if (!char.IsDigit(keyPosition[nextIndex])) {
                                break;
                            }
                            follow += keyPosition[nextIndex].ToString();
                        }
                        int followNum = int.Parse(follow);

                        if (prevNum >= followNum) {
                            errorMsg += "符号'-'前面的数字必须小于后面的数字";
                            break;
                        }
                    }
                }
            }
            List<int> keyPositionList = GetKeyPositionList(keyPosition);
            var enumerable = keyPositionList.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key);
            if (enumerable.Count() > 0) {
                errorMsg += $"存在重复关键位：{string.Join(", ", enumerable)}";
            }
            return errorMsg;
        }
        public static List<int> GetKeyPositionList(string keyPosition) {
            keyPosition = keyPosition.Replace(" ", "");
            Dictionary<string, string> temp = new();
            string[] parts = keyPosition.Split(',');
            foreach (string part in parts) {
                if (part.Contains('-')) {
                    string[] partTemp = part.Split('-');
                    int prev = int.Parse(partTemp[0]);
                    int follow = int.Parse(partTemp[1]);

                    string newString = "";
                    for (int j = prev; j <= follow; j++) {
                        if (j != prev) {
                            newString += ",";
                        }
                        newString += $"{j}";
                    }
                    temp.Add(prev + "-" + follow, newString);
                }
            }
            foreach (KeyValuePair<string, string> pair in temp) {
                keyPosition = keyPosition.Replace(pair.Key, pair.Value);
            }
            return keyPosition.Split(',').Select(int.Parse).ToList();
        }
        public static List<char> GetKeyCharList(string keyChar) {
            List<char> listTemp = new();
            string[] strings = keyChar.Replace(" ", "").Split(',');
            foreach (string s in strings) {
                if (s.Length == 1) {
                    listTemp.Add(char.Parse(s));
                } else {
                    listTemp.AddRange(s.ToList());
                }
            }
            return listTemp;
        }
        public static Dictionary<int, char>? GetKeyMatchingRule(string? keyPosition, string? keyChar) {
            if (keyPosition == null || keyChar == null) {
                return null;
            }
            List<int> keyPositionList = GetKeyPositionList(keyPosition);
            List<char> keyCharList = GetKeyCharList(keyChar);
            if (keyPositionList.Count != keyCharList.Count) {
                return null;
            }
            Dictionary<int, char> matchingRule = new();
            for (int i = 0; i < keyPositionList.Count; i++) {
                matchingRule.Add(keyPositionList[i], keyCharList[i]);
            }
            return matchingRule;
        }
        public static bool CheckBarCodeIsMatched(string barCode, BarCodeMatchingRuleDTO dto) {
            return CheckBarCodeIsMatched(barCode, dto.end_char, dto.length, dto.key_position, dto.key_char);
        }
        public static bool CheckBarCodeIsMatched(string barCode, string? endChar, int? length, string? keyPosition, string? keyChar) {
            return CheckBarCodeIsMatched(barCode, endChar, length, GetKeyMatchingRule(keyPosition, keyChar));
        }
        public static bool CheckBarCodeIsMatched(string barCode, string? endChar, int? length, Dictionary<int, char>? matchingRules) {
            if (string.IsNullOrEmpty(barCode)) {
                return false;
            }
            if (!string.IsNullOrEmpty(endChar)) {
                barCode = barCode.Substring(0, barCode.IndexOf(endChar) + 1);
            }
            if (length != null && length > 0 && barCode.Length != length) {
                return false;
            }
            if (matchingRules != null) {
                foreach (KeyValuePair<int, char> pair in matchingRules) {
                    if (barCode.Length < pair.Key || barCode[pair.Key - 1] != pair.Value) {
                        return false;
                    }
                }
            }
            return true;
        }

        public static byte[] ToBytes(string hexString) {
            if (hexString.Length % 2 != 0) {
                string errorMsg = $"Value[{hexString}] can not convert to bytes because its length is not an even number.";
                throw new InvalidCastException(errorMsg);
            }
            return Enumerable.Range(0, hexString.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
                             .ToArray();
        }

        public static byte[] ToBytes(int intValue) {
            int maxToByte = 256 * 256 - 1;
            if (intValue > maxToByte) {
                string errorMsg = $"Value[{intValue}] too large for 2 bytes value, can not larger than {maxToByte}.";
                throw new InvalidCastException(errorMsg);
            }
            return ToBytes(ToHexString(intValue));
        }
        public static byte[] ToSingleBytes(int intValue) {
            int maxToByte = 256 - 1;
            if (intValue > maxToByte) {
                string errorMsg = $"Value[{intValue}] too large for 1 bytes value, can not larger than {maxToByte}.";
                throw new InvalidCastException(errorMsg);
            }
            return ToBytes(ToSingleHexString(intValue));
        }

        public static byte[] ToBytesByBinaryString(string binaryString) {
            if (binaryString.Length % 8 != 0) {
                string errorMsg = $"Value[{binaryString}] can not convert to bytes because its length is not an even number.";
                throw new InvalidCastException(errorMsg);
            }
            int byteNum = binaryString.Length / 8;
            byte[] bytes = new byte[byteNum];
            for (int i = 0; i < byteNum; i++) {
                bytes[i] = Convert.ToByte(binaryString.Substring(i * 8, 8), 2);
            }
            return bytes;
        }

        public static string ToHexString(int intValue) {
            int maxToByte = 256 * 256 - 1;
            if (intValue > maxToByte) {
                string errorMsg = $"Value[{intValue}] too large for 2 bytes value, can not larger than {maxToByte}.";
                throw new InvalidCastException(errorMsg);
            }
            return Convert.ToString(intValue, 16).PadLeft(4, '0');
        }
        public static string ToSingleHexString(int intValue) {
            int maxToByte = 256 - 1;
            if (intValue > maxToByte) {
                string errorMsg = $"Value[{intValue}] too large for 1 bytes value, can not larger than {maxToByte}.";
                throw new InvalidCastException(errorMsg);
            }
            return Convert.ToString(intValue, 16).PadLeft(2, '0');
        }

        public static string ToHexString(byte[] hexBytes) {
            return BitConverter.ToString(hexBytes).Replace("-", "");
        }

        public static string ToHexString(string binaryString) {
            return ToHexString(ToBytesByBinaryString(binaryString));
        }

        public static string ToBinaryString(byte[] hexBytes) {
            return String.Join(String.Empty, ToHexString(hexBytes).Select(c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')));
        }

        public static string ToBinaryString(string hexString) {
            return String.Join(String.Empty, hexString.Select(c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')));
        }

        public static int[] ToIntsByHexString(string hexString) {
            return ToIntsByBinaryString(ToBinaryString(hexString));
        }

        public static int[] ToIntsByBinaryString(string binaryString) {
            int[] intValues = new int[binaryString.Length];
            for (int i = 0; i < binaryString.Length; i++) {
                char c = binaryString[i];
                intValues[i] = int.Parse(c.ToString());
            }
            return intValues;
        }
    }
}
