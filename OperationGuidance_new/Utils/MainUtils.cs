using System.IO.Ports;
using CustomLibrary.Constants;
using CustomLibrary.Utils;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.Utils {
    public static class MainUtils {
        private static Dictionary<int, ArmTask> _armTasks = new();
        public static Dictionary<int, ArmTask> ArmTasks => _armTasks;
        private static Dictionary<int, ToolTask> _toolTasks = new();
        public static Dictionary<int, ToolTask> ToolTasks => _toolTasks;
        private static Dictionary<int, SerialPortTask> _serialPortTasks = new();
        public static Dictionary<int, SerialPortTask> SerialPortTasks => _serialPortTasks;

        public static void NewArmTask(int armId, string ip, int port, DeviceArm arm) {
            ArmTask task = new(ip, port, arm);
            task.Connect();
            _armTasks.Add(armId, task);
        }
        public static async Task<ArmTask> NewArmTaskAsync(int armId, string ip, int port, DeviceArm arm) {
            ArmTask task = new(ip, port, arm);
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

        public static void NewToolTask(int toolId, string ip, int port, DeviceTool tool) {
            ToolTask task = new(ip, port, tool);
            task.Connect();
            _toolTasks.Add(toolId, task);
        }
        public static async Task<ToolTask> NewToolTaskAsync(int toolId, string ip, int port, DeviceTool tool) {
            ToolTask task = new(ip, port, tool);
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
        
        public static void NewSerialPortTask(int serialPortId, string fullName, 
                string portName, int baudRate, Parity parity, int dataBits, 
                StopBits stopBits, DataTypes dataType, DeviceSerialPort serialPort) {
            SerialPortTask task = new(fullName, portName, baudRate, parity, dataBits, stopBits, dataType, serialPort);
            task.Connect();
            _serialPortTasks.Add(serialPortId, task);
        }
        public static async Task<SerialPortTask> NewSerialPortTaskAsync(int serialPortId, string fullName, 
                string portName, int baudRate, Parity parity, int dataBits, 
                StopBits stopBits, DataTypes dataType, DeviceSerialPort serialPort) {
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
    }
}
