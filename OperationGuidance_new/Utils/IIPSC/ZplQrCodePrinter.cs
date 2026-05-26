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

        private string GenerateZplCommand(SciiXtPrinterConfig sProfile, string traceCode, DateTime date, int moduleSize = 5) {
            if (string.IsNullOrEmpty(traceCode) || traceCode.Length != TRACE_CODE_LENGTH)
                throw new ArgumentException($"追溯码必须是{TRACE_CODE_LENGTH}位", nameof(traceCode));

            var zpl = new StringBuilder();
            zpl.AppendLine("^XA");

            zpl.AppendLine($"{sProfile.text_1}{sProfile.supplier_name}^FS");
            zpl.AppendLine($"{sProfile.text_2}{sProfile.project_name}^FS");
            zpl.AppendLine($"{sProfile.text_3}{date:yyyy/MM/dd}^FS");
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

        /// <summary>
        /// 解析24位追溯码，提取流水号和日期。校验失败抛 ArgumentException。
        /// 格式: 00(2) + 制造地(2) + 年份尾号(1) + 自然日(3) + 流水号(4) + 0000(4) + 零件号(8)
        /// </summary>
        private static (int serialNumber, DateTime date) ParseTraceCode(string traceCode) {
            if (string.IsNullOrEmpty(traceCode) || traceCode.Length != TRACE_CODE_LENGTH)
                throw new ArgumentException($"追溯码必须为{TRACE_CODE_LENGTH}位");

            if (traceCode.Substring(0, 2) != "00")
                throw new ArgumentException("追溯码前两位必须为\"00\"");
            if (traceCode.Substring(12, 4) != "0000")
                throw new ArgumentException("追溯码第13-16位必须为\"0000\"");

            if (!int.TryParse(traceCode.Substring(4, 1), out int yearDigit) || yearDigit < 0 || yearDigit > 9)
                throw new ArgumentException("追溯码第5位年份尾号格式不正确");

            if (!int.TryParse(traceCode.Substring(5, 3), out int dayOfYear) || dayOfYear < 1 || dayOfYear > 366)
                throw new ArgumentException("追溯码第6-8位自然日格式不正确");

            if (!int.TryParse(traceCode.Substring(8, 4), out int serialNumber) || serialNumber < 0 || serialNumber > 9999)
                throw new ArgumentException("追溯码第9-12位流水号格式不正确");

            int baseYear = DateTime.Now.Year / 10 * 10 + yearDigit;
            if (baseYear > DateTime.Now.Year)
                baseYear -= 10;

            DateTime date;
            try {
                date = new DateTime(baseYear, 1, 1).AddDays(dayOfYear - 1);
            } catch (ArgumentOutOfRangeException) {
                throw new ArgumentException($"追溯码中日期无效：年份={baseYear}，日序={dayOfYear}");
            }

            if (date > DateTime.Now)
                throw new ArgumentException($"追溯码中日期({date:yyyy/MM/dd})不能是未来日期");

            return (serialNumber, date);
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

        /// <summary>
        /// 使用 API 返回的24位追溯码打印标签。解析追溯码、校验格式、提取流水号、组装ZPL并打印。
        /// </summary>
        public bool PrintWithTraceCode(SciiXtPrinterConfig config, string traceCode) {
            try {
                var (serialNumber, date) = ParseTraceCode(traceCode);

                config.sn = serialNumber;

                string zpl = GenerateZplCommand(config, traceCode, date);
                return PrintViaZpl(config.printer_name, zpl);
            } catch (Exception ex) {
                log.Error($"PrintWithTraceCode 失败，traceCode = [{traceCode}]：{ex.Message}");
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
