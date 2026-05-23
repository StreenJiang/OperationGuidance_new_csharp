using log4net;
using System.Text;

namespace OperationGuidance_new.Utils.IIPSC {
    /// <summary>
    /// ZPL指令生成工具（用于二维码打印）
    /// </summary>
    public class ZplQrCodePrinter: IDisposable {
        private ILog log = LogManager.GetLogger(typeof(ZplQrCodePrinter));
        private const int TRACE_CODE_LENGTH = 24;

        public const double DPMM_203DPI = 8;   // 203 dpi ≈ 8 dots/mm
        public const double DPMM_300DPI = 12;  // 300 dpi ≈ 12 dots/mm
        public const double DPMM_600DPI = 24;  // 600 dpi ≈ 24 dots/mm

        /// <summary>QR版本字母数字模式容量（M级纠错），索引即版本号（1-based）</summary>
        private static readonly int[] QrAlphanumericCapacity = {
            0,   // 占位（版本从1开始）
            25, 47, 77, 114, 154, 195, 224, 279, 352, 430,
            496, 574, 653, 738, 829, 922, 1017, 1117, 1224, 1332,
        };

        /// <summary>
        /// 生成24位追溯码
        /// 格式: 00(2) + 制造地(2) + 年份尾号(1) + 自然日(3) + 流水号(4) + 0000(4) + 零件号(8)
        /// </summary>
        public string Generate24BitTraceCode(string manufactureLocation, string partNumber, int serialNumber) {
            if (string.IsNullOrEmpty(manufactureLocation) || manufactureLocation.Length != 2)
                throw new ArgumentException("制造地代码必须为2位", nameof(manufactureLocation));
            if (string.IsNullOrEmpty(partNumber) || partNumber.Length != 8)
                throw new ArgumentException("零件号必须为8位", nameof(partNumber));
            if (serialNumber < 0 || serialNumber > 9999)
                throw new ArgumentOutOfRangeException(nameof(serialNumber), "流水号必须在0-9999之间");

            var now = DateTime.Now;
            string yearDigit = (now.Year % 10).ToString();
            string dayOfYear = now.DayOfYear.ToString().PadLeft(3, '0');
            string serial = serialNumber.ToString().PadLeft(4, '0');

            return $"00{manufactureLocation}{yearDigit}{dayOfYear}{serial}0000{partNumber}";
        }

        public string GenerateZplCommand(SciiXtPrinterConfig sProfile, string traceCode, int moduleSize = 5) {
            if (string.IsNullOrEmpty(traceCode) || traceCode.Length != TRACE_CODE_LENGTH)
                throw new ArgumentException($"追溯码必须是{TRACE_CODE_LENGTH}位", nameof(traceCode));

            var zpl = new StringBuilder();
            zpl.AppendLine("^XA");

            zpl.AppendLine($"{sProfile.text_1}{sProfile.supplier_name}^FS");
            zpl.AppendLine($"{sProfile.text_2}{sProfile.project_name}^FS");
            zpl.AppendLine($"{sProfile.text_3}{DateTime.Now:yyyy/MM/dd}^FS");
            zpl.AppendLine($"{sProfile.text_4}{sProfile.sn.ToString().PadLeft(4, '0')}^FS");
            zpl.AppendLine($"{sProfile.text_5}{traceCode}^FS");

            int labelWidthMm = 110;
            int labelHeightMm = 50;
            zpl.AppendLine($"^LH0,0");
            zpl.AppendLine($"^PW{MmToDots(labelWidthMm, DPMM_203DPI)}");
            zpl.AppendLine($"^LL{MmToDots(labelHeightMm, DPMM_203DPI)}");

            zpl.AppendLine($"^FO{sProfile.sn_pos_x},{sProfile.sn_pos_y}^BQN,2,{moduleSize}");
            zpl.AppendLine($"^FDQA,{traceCode}^FS");
            zpl.AppendLine("^XZ");

            return zpl.ToString();
        }

        public string GenerateQrZpl(string qrContent,
                                    double dpmm = DPMM_300DPI,
                                    double labelSizeMm = 9,
                                    double qrSizeMm = 5.4,
                                    double marginXFactor = 1,
                                    double marginYFactor = 1) {
            int labelDots = MmToDots(labelSizeMm, dpmm);
            int targetQrDots = MmToDots(qrSizeMm, dpmm);

            int version = GetMinQrVersion(qrContent);
            int modules = GetModuleCountForVersion(version);

            int moduleWidth = Math.Clamp(targetQrDots / modules, 1, 10);
            int actualQrDots = modules * moduleWidth;

            int centeredMargin = (labelDots - actualQrDots) / 2;
            int marginX = Math.Max(0, (int)(centeredMargin * marginXFactor));
            int marginY = Math.Max(0, (int)(centeredMargin * marginYFactor));

            return $"^XA^PW{labelDots}^LL{labelDots}^FO{marginX},{marginY}^BQN,2,{moduleWidth},0,{version}^FDMA,{qrContent}^FS^XZ";
        }

        private static int GetMinQrVersion(string content) {
            int len = content.Length;
            for (int v = 1; v < QrAlphanumericCapacity.Length; v++) {
                if (len <= QrAlphanumericCapacity[v])
                    return v;
            }
            return QrAlphanumericCapacity.Length - 1;
        }

        private static int GetModuleCountForVersion(int version) {
            return (version - 1) * 4 + 21;
        }

        private static int MmToDots(double mm, double dpmm) {
            return (int) Math.Round(mm * dpmm);
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
            try {
                printerName = config.printer_name;
                string traceCode = Generate24BitTraceCode(
                    config.manufacture_location, config.part_number, config.sn);
                string zpl = GenerateZplCommand(config, traceCode);
                return PrintViaZpl(printerName, zpl);
            } catch (Exception ex) {
                log.Error($"Print fails! PrinterName = [{printerName}]：{ex.Message}");
                return false;
            }
        }

        public bool PrintWithSn(SciiXtPrinterConfig config, int sn, string printerName) {
            try {
                config.sn = sn;
                string traceCode = Generate24BitTraceCode(
                    config.manufacture_location, config.part_number, sn);
                string zpl = GenerateZplCommand(config, traceCode);
                return PrintViaZpl(printerName, zpl);
            } catch (Exception ex) {
                log.Error($"PrintWithSn fails! PrinterName = [{printerName}], SN = [{sn}]：{ex.Message}");
                return false;
            }
        }

        public bool PrintQrContent(string content, string printerName,
            double dpmm, double labelSizeMm, double qrSizeMm, double marginXFactor, double marginYFactor) {
            try {
                string zpl = GenerateQrZpl(content, dpmm, labelSizeMm, qrSizeMm, marginXFactor, marginYFactor);
                return PrintViaZpl(printerName, zpl);
            } catch (Exception ex) {
                log.Error($"PrintQrContent fails! PrinterName = [{printerName}]：{ex.Message}");
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

        public void Dispose() {
            // TODO: 添加需要销毁的，被持有的实例（无法销毁自身）
        }
    }
}
