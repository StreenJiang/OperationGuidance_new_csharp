using System.Text;
using log4net;

namespace OperationGuidance_new.Utils.IIPSC {
    /// <summary>
    /// ZPL指令生成工具（用于二维码打印）
    /// </summary>
    public class ZplQrCodePrinter: IDisposable {
        private ILog log = LogManager.GetLogger(typeof(ZplQrCodePrinter));
        private const int PART_CODE_LENGTH = 10;       // 零部件编码长度
        private const int SUPPLIER_CODE_LENGTH = 6;    // 供应商编码长度
        private const int BATCH_CODE_LENGTH = 8;       // 批次编码长度
        private const int SERIAL_NUM_LENGTH = 8;       // 生产流水号长度（前补0）
        private const int SOFTWARE_VERSION_LENGTH = 16;// 软件版本号长度（前补0）
        private const int HARDWARE_VERSION_LENGTH = 16;// 硬件版本号长度（前补0）

        /// <summary>
        /// 生成64位零部件完整编码
        /// </summary>
        public string Generate64BitCode(
            string partCode,
            string supplierCode,
            string batchCode,
            string serialNumber,
            string softwareVersion,
            string hardwareVersion) {
            // 验证固定长度字段
            ValidateFixedLength(partCode, PART_CODE_LENGTH, "零部件编码");
            ValidateFixedLength(supplierCode, SUPPLIER_CODE_LENGTH, "供应商编码");
            ValidateFixedLength(batchCode, BATCH_CODE_LENGTH, "批次编码");

            // 可变长度字段前补0
            string paddedSerial = PadLeft(serialNumber, SERIAL_NUM_LENGTH);
            string paddedSoftware = PadLeft(softwareVersion, SOFTWARE_VERSION_LENGTH);
            string paddedHardware = PadLeft(hardwareVersion, HARDWARE_VERSION_LENGTH);

            // 拼接64位编码
            return $"{partCode}{supplierCode}{batchCode}{paddedSerial}{paddedSoftware}{paddedHardware}";
        }

        /// <summary>
        /// 生成ZPL指令字符串（用于打印二维码）
        /// </summary>
        /// <param name="qrContent">64位编码内容</param>
        /// <param name="moduleSize">二维码模块大小（1-10，数值越大二维码越大）</param>
        /// <returns>ZPL指令</returns>
        public string GenerateZplCommand(SciiXtPrinterConfig sProfile, string qrContent, int moduleSize = 5) {
            if (string.IsNullOrEmpty(qrContent) || qrContent.Length != 64) {
                throw new ArgumentException("二维码内容必须是64位编码", nameof(qrContent));
            }
            // 核心参数：确保二维码居中且足够大
            int labelWidthMm = 110;   // 标签宽度（mm）
            int labelHeightMm = 50;   // 标签高度（mm）

            var zpl = new StringBuilder();
            zpl.AppendLine("^XA");

            zpl.AppendLine($"{sProfile.text_1}{qrContent.Substring(0, 16)}^FS");
            zpl.AppendLine($"{sProfile.text_2}{qrContent.Substring(16, 16)}^FS");
            zpl.AppendLine($"{sProfile.text_3}{qrContent.Substring(32, 16)}^FS");
            zpl.AppendLine($"{sProfile.text_4}{qrContent.Substring(48, 16)}^FS");
            zpl.AppendLine($"{sProfile.location_y}");

            // 设置标签尺寸（必须与物理标签一致，否则居中失效）
            zpl.AppendLine($"^LH0,0");                          // 标签原点
            zpl.AppendLine($"^PW{labelWidthMm * 8}");           // 标签宽度（点）
            zpl.AppendLine($"^LL{labelHeightMm * 8}");          // 标签长度（点）

            // 二维码指令（居中+放大）
            zpl.AppendLine($"^FO{sProfile.sn_pos_x},{sProfile.sn_pos_y}^BQN,2,{moduleSize}");
            zpl.AppendLine($"^FDQA,{qrContent}^FS");                       // 二维码内容
            zpl.AppendLine("^XZ");

            return zpl.ToString();
        }

        public string GenerateQrZpl(string qrContent, int moduleSize = 5) {
            // 标签尺寸（点）：110mm × 50mm @ 203DPI
            const int WIDTH = 880, HEIGHT = 400;

            // 保守估算二维码尺寸并居中
            int qrSize = 45 * moduleSize;
            int x = (WIDTH - qrSize) / 2;
            int y = (HEIGHT - qrSize) / 2;

            return $"^XA^LH0,0^PW{WIDTH}^LL{HEIGHT}^FO{x},{y}^BQN,2,{moduleSize}^FDMA,{qrContent}^FS^XZ";
        }

        /// <summary>
        /// 发送ZPL指令到打印机
        /// </summary>
        /// <param name="printerName">打印机名称</param>
        /// <param name="zplCommand">ZPL指令字符串</param>
        /// <returns>打印是否成功</returns>
        public bool PrintViaZpl(string printerName, string zplCommand) {
            if (string.IsNullOrEmpty(printerName)) {
                throw new ArgumentException("打印机名称不能为空", nameof(printerName));
            }
            if (string.IsNullOrEmpty(zplCommand)) {
                throw new ArgumentException("ZPL指令不能为空", nameof(zplCommand));
            }

            // 使用RawPrinterHelper发送ZPL字符串（ZPL是文本指令，直接发送字符串即可）
            return RawPrinterHelper.SendStringToPrinter(printerName, zplCommand);
        }

        /// <summary>
        /// 快捷打印方法（一步完成编码→ZPL→打印）
        /// </summary>
        public bool QuickPrint(SciiXtPrinterConfig config) {
            string printerName = string.Empty;
            string partCode = string.Empty;
            string supplierCode = string.Empty;
            string batchCode = string.Empty;
            string serialNumber = string.Empty;
            string softwareVersion = string.Empty;
            string hardwareVersion = string.Empty;

            try {
                printerName = config.printer_name;
                partCode = config.part_code;
                supplierCode = config.vender_code;
                batchCode = config.batch_code;
                serialNumber = config.sn.ToString();
                softwareVersion = config.soft_version;
                hardwareVersion = config.hard_version;

                // 生成64位编码
                string qrContent = Generate64BitCode(partCode, supplierCode, batchCode, serialNumber, softwareVersion, hardwareVersion);
                // 生成ZPL指令
                string zpl = GenerateZplCommand(config, qrContent);
                // 发送到打印机
                return PrintViaZpl(printerName, zpl);
            } catch (Exception ex) {
                log.Error($"Print fails! PrinterName = [{printerName}]：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取系统中所有可用打印机
        /// </summary>
        public List<string> GetAvailablePrinters() {
            var printers = new List<string>();
            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters) {
                printers.Add(printer);
            }
            return printers;
        }

        #region 辅助方法
        /// <summary>
        /// 验证固定长度编码
        /// </summary>
        private void ValidateFixedLength(string code, int requiredLength, string codeName) {
            if (string.IsNullOrEmpty(code) || code.Length != requiredLength) {
                throw new ArgumentException($"{codeName}必须为{requiredLength}位，实际为{code?.Length ?? 0}位", nameof(code));
            }
        }

        /// <summary>
        /// 左补0到指定长度
        /// </summary>
        private string PadLeft(string value, int totalLength) {
            if (value == null)
                value = string.Empty;
            if (value.Length > totalLength) {
                throw new ArgumentException($"{value}长度超过{totalLength}位", nameof(value));
            }
            return value.PadLeft(totalLength, '0');
        }

        public void Dispose() {
            // TODO: 添加需要销毁的，被持有的实例（无法销毁自身）
        }
        #endregion
    }
}
