using System.IO.Ports;
using System.Reflection;
using CustomLibrary.Constants;
using CustomLibrary.Utils;
using Newtonsoft.Json;
using OperationGuidance_new.Attributes;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.Utils {
    public static class MainUtils {
        public static LoginView LoginView { get; set; }

        public static readonly string DATETIME_FORMAT_YYYY_MM_DD_CHINESE = "yyyy年MM月dd ddd HH:mm:ss";

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

        public static IniFile Settings { get; } = new();
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
            return baseDirectory;
        }

        public static string GetStorageFileName() {
            string nameFormat = Settings.Read(IniFileKeys.DataStorageNameFormat);
            if (string.IsNullOrEmpty(nameFormat)) {
                nameFormat = MainUtils.DATETIME_FORMAT_YYYY_MM_DD;
                Settings.Write(IniFileKeys.DataStorageNameFormat, nameFormat);
            }
            return nameFormat;
        }
        public static string GetStorageFormattedName() {
            DateTime now = DateTime.Now;
            string nameFormatted = GetStorageFileName();
            if (Replace(DATETIME_FORMAT_YYYY_MM_DD_DDD)) {}
            else if (Replace(DATETIME_FORMAT_YYYY_MM_DD_DDD_2)) {}
            else if (Replace(DATETIME_FORMAT_YYYY_MM_DD)) {}
            else if (Replace(DATETIME_FORMAT_YYYY_MM_DD_2)) {}
            else if (Replace(DATETIME_FORMAT_YYYY_MM_DDD)) {}
            else if (Replace(DATETIME_FORMAT_YYYY_MM_DDD_2)) {}
            else if (Replace(DATETIME_FORMAT_YYYY_MM)) {}
            else if (Replace(DATETIME_FORMAT_YYYY_MM_2)) {}
            return nameFormatted;

            bool Replace(string formatPattern) {
                if (nameFormatted.Contains(formatPattern)) {
                    nameFormatted = nameFormatted.Replace(formatPattern, now.ToString(formatPattern));
                    return true;
                }
                return false;
            }
        }
        public static string GetStoragePath() {
            string dataStoragePath = Settings.Read(IniFileKeys.DataStoragePath);
            if (string.IsNullOrEmpty(dataStoragePath)) {
                dataStoragePath = GetBaseDirectory() + "OperationDataStorage\\";
                Settings.Write(IniFileKeys.DataStoragePath, dataStoragePath);
            }
            return dataStoragePath;
        }
        public static List<int> GetSortConfig() {
            List<int>? sortConfig = null;
            string dataStorageFields = MainUtils.Settings.Read(IniFileKeys.DataStorageFieldsSort);
            sortConfig = JsonConvert.DeserializeObject<List<int>>(dataStorageFields);
            if (sortConfig == null) {
                sortConfig = new() { 1, 2, 14, 33, 34, 35, 36, 37, 40, 43, 44, 17, 15, 21, 16 };
            }
            return sortConfig;
        }
        public static List<int>? GetSortConfigCurr() {
            string dataStorageFieldsCurr = MainUtils.Settings.Read(IniFileKeys.DataStorageFieldsSortCurr);
            return JsonConvert.DeserializeObject<List<int>>(dataStorageFieldsCurr);
        }
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
            string dataStorageFields = MainUtils.Settings.Read(IniFileKeys.DataStorageFieldsSort);
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
        public static bool GetStoreLooseningData() {
            string storeLooseningData = MainUtils.Settings.Read(IniFileKeys.DataStorageStoreLooseningData);
            if (string.IsNullOrEmpty(storeLooseningData)) {
                MainUtils.Settings.Write(IniFileKeys.DataStorageStoreLooseningData, "1");
                return true;
            }
            return int.Parse(storeLooseningData) == (int) YesOrNo.YES;
        }
        public static bool IsArmLocatingEnabled() {
            string armLocatingEnabled = MainUtils.Settings.Read(IniFileKeys.MissionArmLocatingEnabled);
            if (string.IsNullOrEmpty(armLocatingEnabled)) {
                MainUtils.Settings.Write(IniFileKeys.MissionArmLocatingEnabled, "1");
                return true;
            }
            return int.Parse(armLocatingEnabled) == (int) YesOrNo.YES;
        }
        public static int GetArmLocatingAccuracy() {
            string armLocatingAccuracy = MainUtils.Settings.Read(IniFileKeys.MissionArmLocatingAccuracy);
            if (string.IsNullOrEmpty(armLocatingAccuracy)) {
                MainUtils.Settings.Write(IniFileKeys.MissionArmLocatingAccuracy, "20");
                return 20;
            }
            return int.Parse(armLocatingAccuracy);
        }

        private static Dictionary<int, ArmTask> _armTasks = new();
        public static Dictionary<int, ArmTask> ArmTasks => _armTasks;
        public static void NewArmTask(int armId, string? armName, string ip, int port, DeviceTypeArm arm) {
            ArmTask task = new(armName, ip, port, arm);
            task.Connect();
            _armTasks.Add(armId, task);
        }
        public static async Task<ArmTask> NewArmTaskAsync(int armId, string? armName, string ip, int port, DeviceTypeArm arm) {
            ArmTask task = new(armName, ip, port, arm);
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
            ToolTask task = new(toolName, ip, port, tool);
            task.Connect();
            _toolTasks.Add(toolId, task);
        }
        public static async Task<ToolTask> NewToolTaskAsync(int toolId, string? toolName, string ip, int port, DeviceTypeTool tool) {
            ToolTask task = new(toolName, ip, port, tool);
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
            SerialPortTask task = new(fullName, portName, baudRate, parity, dataBits, stopBits, dataType, serialPort);
            task.Connect();
            _serialPortTasks.Add(serialPortId, task);
        }
        public static async Task<SerialPortTask> NewSerialPortTaskAsync(int serialPortId, string fullName, 
                string portName, int baudRate, Parity parity, int dataBits, 
                StopBits stopBits, DataTypes dataType, DeviceTypeSerialPort serialPort) {
            SerialPortTask task = new(fullName, portName, baudRate, parity, dataBits, stopBits, dataType, serialPort);
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
            CommunicationTask task = new(communicationName, ip, port, communication);
            task.Connect();
            _communicationTasks.Add(communicationId, task);
        }
        public static async Task<CommunicationTask> NewCommunicationTaskAsync(int communicationId, string? communicationName, string ip, int port, DeviceTypeCommunication communication) {
            CommunicationTask task = new(communicationName, ip, port, communication);
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

        public static TextBox? EventLogTextArea { get; set; }
        public static void Log(string message, bool printToView = true) {
            System.Console.WriteLine(message);
            if (EventLogTextArea != null && printToView) {
                EventLogTextArea.BeginInvoke(() => {
                    EventLogTextArea.AppendText(message + "\r\n");
                });
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
            return errorMsg;
        }
        public static List<int> GetKeyPositionList(string keyPosition) {
            keyPosition = keyPosition.Replace(" ", "");
            if (keyPosition.Contains('-')) {
                Dictionary<string, string> temp = new();
                for (int i = 0; i < keyPosition.Length; i++) {
                    if (keyPosition[i] == '-') {
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

                        string newString = "";
                        for (int j = prevNum; j <= followNum; j++) {
                            if (j != prevNum) {
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
        public static bool CheckBarCodeIsMatched(string barCode, string? endChar, int? length, Dictionary<int, char>? matchingRules) {
            if (string.IsNullOrEmpty(barCode)) {
                return false;
            }
            if (!string.IsNullOrEmpty(endChar)) {
                barCode = barCode.Substring(0, barCode.IndexOf(endChar) + 1);
            }
            if (length != null && barCode.Length != length) {
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
    }
}
